using System.Security.Claims;
using BulkyBook.DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models.ViewModels;

namespace BulkyBookWeb.Controllers;

[Area("Customer")]
[Authorize]
public class CartController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
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
                includeProperties: "Product")
        };
        foreach (var cart in ShoppingCartVm.ListCart)
        {
            cart.Price = getPriceBasedOnQuantity(cart.Count, cart.Product.Price, cart.Product.Price50,
                cart.Product.Price100);
            ShoppingCartVm.CartTotal += (cart.Price * cart.Count);
        }
        return View(ShoppingCartVm);
    }

    private double getPriceBasedOnQuantity(double quantity, double price, double price50, double price100)
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
}
