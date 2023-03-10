using BulkyBook.DataAccess.Repository.IRepository;
using Models;
using Microsoft.AspNetCore.Mvc;

namespace BulkyBookWeb.Controllers;

public class CategoryController : Controller
{
    // GET
    private readonly IUnitOfWork _unitOfWork;

    public CategoryController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    

    public IActionResult Index()
    {
        IEnumerable<Category> objCategoryList = _unitOfWork.Category.GetAll();
        return View(objCategoryList);
    }
    
    //GET
    public IActionResult Create()
    {
        return View();
    }
    
    //POST
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(Category obj)
    {
        if (obj.Name == obj.DisplayOrder.ToString())
        {
            ModelState.AddModelError("Name", "The DisplayOrder cannot exactly match the name");
        }
        if (ModelState.IsValid)
        {
            _unitOfWork.Category.Add(obj);
            _unitOfWork.Save();
            TempData["success"] = "Category created successfully";
            return RedirectToAction("Index");
        }

        return View(obj);
    }
    
    //GET
    public IActionResult Edit(int? id)
    {
        if (id == null || id == 0)
        {
            return NotFound();
        }
        
        var categoryFromDbFirst = _unitOfWork.Category.GetFirstOrDefault(u=> u.Id == id);
        
        if (categoryFromDbFirst == null)
        {
            return NotFound();
        }
        return View(categoryFromDbFirst);
    }
    
    //POST
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(Category obj)
    {
        if (obj.Name == obj.DisplayOrder.ToString())
        {
            ModelState.AddModelError("Name", "The DisplayOrder cannot exactly match the name");
        }
        if (ModelState.IsValid)
        {
            _unitOfWork.Category.Update(obj);
            _unitOfWork.Save();
            TempData["success"] = "Category updated successfully";
            return RedirectToAction("Index");
        }

        return View(obj);
    }
    
    //GET
    public IActionResult Delete(int? id)
    {
        if (id == null || id == 0)
        {
            return NotFound();
        }
        
        var categoryFromDbFirst = _unitOfWork.Category.GetFirstOrDefault(u=> u.Id == id);
        return View(categoryFromDbFirst);
    }
    
    //POST
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeletePost(int? id)
    {
        var obj = _unitOfWork.Category.GetFirstOrDefault(u=> u.Id == id);
        if (obj == null)
        {
            return NotFound();
        }
        _unitOfWork.Category.Remove(obj);
        _unitOfWork.Save();
        TempData["success"] = "Category deleted successfully";
        return RedirectToAction("Index");
    }
}