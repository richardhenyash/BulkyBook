using System.Security.Claims;
using BulkyBook.DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models;
using Models.ViewModels;
using Stripe.Checkout;
using Utility;

namespace BulkyBookWeb.Controllers;

[Area("Customer")]
[Authorize]
public class CartController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    [BindProperty]
    public ShoppingCartVm ShoppingCartVm { get; set; }

    public int OrderTotal { get; set; }

    public CartController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public IActionResult Index()
    {
        var claimsIdentity = (ClaimsIdentity)User.Identity;
        var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
        ShoppingCartVm = new ShoppingCartVm()
        {
            ListCart = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == claim.Value,
                includeProperties: "Product"),
            OrderHeader = new OrderHeader()
        };
        foreach (var cart in ShoppingCartVm.ListCart)
        {
            cart.Price = GetPriceBasedOnQuantity(cart.Count, cart.Product.Price, cart.Product.Price50,
                cart.Product.Price100);
            ShoppingCartVm.OrderHeader.OrderTotal += (cart.Price * cart.Count);
        }
        return View(ShoppingCartVm);
    }
    
    public IActionResult Summary()
    {
        var claimsIdentity = (ClaimsIdentity)User.Identity;
        var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

        ShoppingCartVm = new ShoppingCartVm()
        {
            ListCart = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == claim.Value,
                includeProperties: "Product"),
            OrderHeader= new()
        };
        ShoppingCartVm.OrderHeader.ApplicationUser = _unitOfWork.ApplicationUser.GetFirstOrDefault(
            u => u.Id == claim.Value);

        ShoppingCartVm.OrderHeader.Name = ShoppingCartVm.OrderHeader.ApplicationUser.Name;
        ShoppingCartVm.OrderHeader.PhoneNumber = ShoppingCartVm.OrderHeader.ApplicationUser.PhoneNumber;
        ShoppingCartVm.OrderHeader.StreetAddress = ShoppingCartVm.OrderHeader.ApplicationUser.StreetAddress;
        ShoppingCartVm.OrderHeader.City = ShoppingCartVm.OrderHeader.ApplicationUser.City;
        ShoppingCartVm.OrderHeader.State = ShoppingCartVm.OrderHeader.ApplicationUser.State;
        ShoppingCartVm.OrderHeader.PostalCode = ShoppingCartVm.OrderHeader.ApplicationUser.PostalCode;

        foreach (var cart in ShoppingCartVm.ListCart)
        {
            cart.Price = GetPriceBasedOnQuantity(cart.Count, cart.Product.Price,
                cart.Product.Price50, cart.Product.Price100);
            ShoppingCartVm.OrderHeader.OrderTotal += (cart.Price * cart.Count);
        }
        return View(ShoppingCartVm);
    }
    
    [HttpPost]
    [ActionName("Summary")]
    [ValidateAntiForgeryToken]
    public IActionResult SummaryPOST()
    {
        var claimsIdentity = (ClaimsIdentity)User.Identity;
        var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

        ShoppingCartVm.ListCart = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == claim.Value,
            includeProperties: "Product");
        
        ShoppingCartVm.OrderHeader.OrderDate = DateTime.Now;
        ShoppingCartVm.OrderHeader.ApplicationUserId = claim.Value;
        
        foreach (var cart in ShoppingCartVm.ListCart)
        {
            cart.Price = GetPriceBasedOnQuantity(cart.Count, cart.Product.Price,
                cart.Product.Price50, cart.Product.Price100);
            ShoppingCartVm.OrderHeader.OrderTotal += (cart.Price * cart.Count);
        }
        
        ApplicationUser applicationUser = _unitOfWork.ApplicationUser.GetFirstOrDefault(u => u.Id == claim.Value);
        if (applicationUser.CompanyId.GetValueOrDefault() == 0)
        {
            ShoppingCartVm.OrderHeader.PaymentStatus = StaticDetails.PaymentStatusPending;
            ShoppingCartVm.OrderHeader.OrderStatus = StaticDetails.StatusPending;
        }
        else
        {
            ShoppingCartVm.OrderHeader.PaymentStatus = StaticDetails.PaymentStatusDelayedPayment;
            ShoppingCartVm.OrderHeader.OrderStatus = StaticDetails.StatusApproved;
        }
        
        _unitOfWork.OrderHeader.Add(ShoppingCartVm.OrderHeader);
        _unitOfWork.Save();
        foreach (var cart in ShoppingCartVm.ListCart)
        {
            OrderDetail orderDetail = new()
            {
                ProductId = cart.ProductId,
                OrderId = ShoppingCartVm.OrderHeader.Id,
                Price = cart.Price,
                Count = cart.Count
            };
            _unitOfWork.OrderDetail.Add(orderDetail);
            _unitOfWork.Save();
        }
        
        // Initialise Stripe settings if not a company user
        if (applicationUser.CompanyId.GetValueOrDefault() == 0)
        {
            var domain = "https://localhost:5001/";
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string>
                {
                    "card",
                },
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
                SuccessUrl = domain + $"customer/cart/OrderConfirmation?id={ShoppingCartVm.OrderHeader.Id}",
                CancelUrl = domain + $"customer/cart/index",
            };
            foreach (var item in ShoppingCartVm.ListCart)
            {
                var sessionLineItem = new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        // Unit amount in cents
                        UnitAmount = (long)(item.Price * 100), //20.00 -> 2000
                        Currency = "usd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.Product.Title
                        },
                    },
                    Quantity = item.Count,
                };
                options.LineItems.Add(sessionLineItem);
            }
            var service = new SessionService();
            Session session = service.Create(options);
            _unitOfWork.OrderHeader.UpdateStripePaymentId(ShoppingCartVm.OrderHeader.Id, session.Id, session.PaymentIntentId);
            _unitOfWork.Save();
            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303);
        }
        return RedirectToAction("OrderConfirmation", "Cart", new { id = ShoppingCartVm.OrderHeader.Id });
    }
    private double GetPriceBasedOnQuantity(double quantity, double price, double price50, double price100)
    {
        if (quantity <= 50)
        {
            return price;
        }

        if (quantity > 50 && quantity <= 100)
        {
            return price50;
        }
        return price100;
    }

    public IActionResult OrderConfirmation(int id)
    {
        OrderHeader orderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == id);
        if (orderHeader.PaymentStatus != StaticDetails.PaymentStatusDelayedPayment)
        {
            var service = new SessionService();
            Session session = service.Get(orderHeader.SessionId);
            // check the stripe status
            if (session.PaymentStatus.ToLower() == "paid")
            {
                _unitOfWork.OrderHeader.UpdateStatus(id, StaticDetails.StatusApproved, StaticDetails.PaymentStatusApproved);
                _unitOfWork.Save();
            }
        }

        List<ShoppingCart> shoppingCarts = _unitOfWork.ShoppingCart
            .GetAll(u => u.ApplicationUserId == orderHeader.ApplicationUserId).ToList();
        _unitOfWork.ShoppingCart.RemoveRange(shoppingCarts);
        _unitOfWork.Save();
        return View(id);
    }
    public IActionResult Plus(int cartId)
    {
        var cart = _unitOfWork.ShoppingCart.GetFirstOrDefault(u => u.Id == cartId);
        _unitOfWork.ShoppingCart.IncrementCount(cart, 1);
        _unitOfWork.Save();
        return RedirectToAction(nameof(Index));
    }
    public IActionResult Minus(int cartId)
    {
        var cart = _unitOfWork.ShoppingCart.GetFirstOrDefault(u => u.Id == cartId);
        if (cart.Count <= 1)
        { 
            _unitOfWork.ShoppingCart.Remove(cart);
        }
        else
        {
            _unitOfWork.ShoppingCart.IncrementCount(cart, -1);
        }
        _unitOfWork.Save();
        return RedirectToAction(nameof(Index));
    }
    public IActionResult Remove(int cartId)
    {
        var cart = _unitOfWork.ShoppingCart.GetFirstOrDefault(u => u.Id == cartId);
        _unitOfWork.ShoppingCart.Remove(cart);
        _unitOfWork.Save();
        return RedirectToAction(nameof(Index));
    }
    
}
