using BulkyBook.DataAccess.Repository.IRepository;
using Models;
using Microsoft.AspNetCore.Mvc;

namespace BulkyBookWeb.Controllers;
[Area("Admin")]

public class ProductController : Controller
{
    // GET
    private readonly IUnitOfWork _unitOfWork;

    public ProductController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    //GET
    public IActionResult Upsert(int? id)
    {
        Product product = new();
        if (id == null || id == 0)
        {
            //create product
            return View(product);
        }
        else
        {
            //update product
        }
        
        return View(product);
    }
    
    public IActionResult Index()
    {
        IEnumerable<Product> objProductList = _unitOfWork.Product.GetAll();
        return View(objProductList);
    }
    
}