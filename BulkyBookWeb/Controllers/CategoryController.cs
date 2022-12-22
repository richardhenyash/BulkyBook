using BulkyBookWeb.Data;
using Microsoft.AspNetCore.Mvc;

namespace BulkyBookWeb.Controllers;

public class CategoryController : Controller
{
    // GET
    private readonly ApplicationDBContext _db;

    public CategoryController(ApplicationDBContext db)
    {
        _db = db;
    }
    

    public IActionResult Index()
    {
        var objCategoryList = _db.Categories.ToList();
        return View();
    }
}