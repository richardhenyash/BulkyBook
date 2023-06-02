using BulkyBook.DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Mvc;
using Models;
using Utility;

namespace BulkyBookWeb.Controllers;
[Area("Admin")]

public class OrderController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public OrderController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public IActionResult Index()
    {
        return View();
    }
    
    #region API CALLS
    [HttpGet]
    public IActionResult GetAll(string status)
    {
        var orderHeaders = _unitOfWork.OrderHeader.GetAll(includeProperties: "ApplicationUser");
        switch (status)
        {
            case "pending":
                orderHeaders = orderHeaders.Where(u => u.PaymentStatus == StaticDetails.PaymentStatusDelayedPayment);
                break;
            case "inprocess":
                orderHeaders = orderHeaders.Where(u => u.OrderStatus == StaticDetails.StatusInProgress);
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