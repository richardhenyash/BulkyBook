using System.Security.Claims;
using BulkyBook.DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Models;
using Models.ViewModels;
using Stripe;
using Utility;

namespace BulkyBookWeb.Controllers;
[Area("Admin")]
[Authorize]

public class OrderController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    [BindProperty]
    public OrderVm OrderVm { get; set; }
    public OrderController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public IActionResult Index()
    {
        return View();
    }
    
    public IActionResult Details(int orderId)
    {
        OrderVm = new OrderVm
        {
            OrderHeader =
                _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == orderId, includeProperties: "ApplicationUser"),
            OrderDetail = _unitOfWork.OrderDetail.GetAll(u => u.OrderId == orderId, includeProperties: "Product"),
        };
        return View(OrderVm);
    }
    [HttpPost]
    [Authorize(Roles = StaticDetails.RoleAdmin + "," + StaticDetails.RoleEmployee)]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateOrderDetail()
    {
        var orderHeaderFromDb = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == OrderVm.OrderHeader.Id, tracked: false);
        orderHeaderFromDb.Name = OrderVm.OrderHeader.Name;
        orderHeaderFromDb.PhoneNumber = OrderVm.OrderHeader.PhoneNumber;
        orderHeaderFromDb.StreetAddress = OrderVm.OrderHeader.StreetAddress;
        orderHeaderFromDb.City = OrderVm.OrderHeader.City;
        orderHeaderFromDb.State = OrderVm.OrderHeader.State;
        orderHeaderFromDb.PostalCode = OrderVm.OrderHeader.PostalCode;
        if (OrderVm.OrderHeader.Carrier != null)
        {
            orderHeaderFromDb.Carrier = OrderVm.OrderHeader.Carrier;
        }
        if (OrderVm.OrderHeader.TrackingNumber != null)
        {
            orderHeaderFromDb.TrackingNumber = OrderVm.OrderHeader.TrackingNumber;
        }
        _unitOfWork.OrderHeader.Update(orderHeaderFromDb);
        _unitOfWork.Save();
        TempData["Success"] = "Order details Updated Successfully.";
        return RedirectToAction("Details", "Order", new { orderId = orderHeaderFromDb.Id });
    }
    
    [HttpPost]
    [Authorize(Roles = StaticDetails.RoleAdmin + "," + StaticDetails.RoleEmployee)]
    [ValidateAntiForgeryToken]
    public IActionResult StartProcessing()
    {
        _unitOfWork.OrderHeader.UpdateStatus(OrderVm.OrderHeader.Id, StaticDetails.StatusInProcess);
        _unitOfWork.Save();
        TempData["Success"] = "Order Status Updated Successfully.";
        return RedirectToAction("Details", "Order", new { orderId = OrderVm.OrderHeader.Id });
    }
    
    [HttpPost]
    [Authorize(Roles = StaticDetails.RoleAdmin + "," + StaticDetails.RoleEmployee)]
    [ValidateAntiForgeryToken]
    public IActionResult ShipOrder()
    {
        var orderHeaderFromDb = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == OrderVm.OrderHeader.Id, tracked: false);
        orderHeaderFromDb.TrackingNumber = OrderVm.OrderHeader.TrackingNumber;
        orderHeaderFromDb.Carrier = OrderVm.OrderHeader.Carrier;
        orderHeaderFromDb.OrderStatus = StaticDetails.StatusShipped;
        orderHeaderFromDb.ShippingDate = DateTime.Now;
        if (orderHeaderFromDb.PaymentStatus == StaticDetails.PaymentStatusDelayedPayment)
        {
            orderHeaderFromDb.PaymentDueDate = DateTime.Now.AddDays(30);
        }
        _unitOfWork.OrderHeader.Update(orderHeaderFromDb);
        _unitOfWork.Save();
        TempData["Success"] = "Order Shipped Successfully.";
        return RedirectToAction("Details", "Order", new { orderId = OrderVm.OrderHeader.Id });
    }
    
    [HttpPost]
    [Authorize(Roles = StaticDetails.RoleAdmin + "," + StaticDetails.RoleEmployee)]
    [ValidateAntiForgeryToken]
    public IActionResult CancelOrder()
    {
        var orderHeaderFromDb = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == OrderVm.OrderHeader.Id, tracked: false);
        if (orderHeaderFromDb.PaymentStatus == StaticDetails.PaymentStatusApproved)
        {
            var options = new RefundCreateOptions
            {
                Reason = RefundReasons.RequestedByCustomer,
                PaymentIntent = orderHeaderFromDb.PaymentIntentId
            };
            var service = new RefundService();
            Refund refund = service.Create(options);
            _unitOfWork.OrderHeader.UpdateStatus(orderHeaderFromDb.Id, StaticDetails.StatusCancelled, StaticDetails.StatusRefunded);
        }
        else
        {
            _unitOfWork.OrderHeader.UpdateStatus(orderHeaderFromDb.Id, StaticDetails.StatusCancelled, StaticDetails.StatusRefunded);
        }
        _unitOfWork.Save();
        TempData["Success"] = "Order Cancelled Successfully.";
        return RedirectToAction("Details", "Order", new { orderId = OrderVm.OrderHeader.Id });
    }
    
    #region API CALLS
    [HttpGet]
    public IActionResult GetAll(string status)
    {
        IEnumerable<OrderHeader> orderHeaders;
        if (User.IsInRole(StaticDetails.RoleAdmin) || User.IsInRole(StaticDetails.RoleEmployee))
        {
            orderHeaders = _unitOfWork.OrderHeader.GetAll(includeProperties: "ApplicationUser");
        }
        else
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            orderHeaders = _unitOfWork.OrderHeader.GetAll(u => u.ApplicationUserId == claim.Value, includeProperties: "ApplicationUser");
        }
        switch (status)
        {
            case "pending":
                orderHeaders = orderHeaders.Where(u => u.PaymentStatus == StaticDetails.PaymentStatusDelayedPayment);
                break;
            case "inprocess":
                orderHeaders = orderHeaders.Where(u => u.OrderStatus == StaticDetails.StatusInProcess);
                break;
            case "completed":
                orderHeaders = orderHeaders.Where(u => u.OrderStatus == StaticDetails.StatusShipped);
                break;
            case "approved":
                orderHeaders = orderHeaders.Where(u => u.OrderStatus == StaticDetails.StatusApproved);
                break;
            default:
                break;
        }
        return Json(new { data = orderHeaders });
    }
    #endregion
}