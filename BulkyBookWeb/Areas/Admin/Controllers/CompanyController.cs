using BulkyBook.DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Models;
using Models.ViewModels;

namespace BulkyBookWeb.Controllers;
[Area("Admin")]

public class CompanyController : Controller
{
    // GET
    private readonly IUnitOfWork _unitOfWork;

    public CompanyController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    //GET
    public IActionResult Upsert(int? id)
    {
        Company company = new();
            
        if (id == null || id == 0)
        {
            return View(company);
        }
        else
        {
            //update company
            company = _unitOfWork.Company.GetFirstOrDefault(u => u.Id == id);
            return View(company);
        }
    }

    //POST
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Upsert(Company obj)
    {
        if (ModelState.IsValid)
        {
            if (obj.Id == 0)
            {
                _unitOfWork.Company.Add(obj);
                TempData["success"] = "Company created successfully";
            }
            else
            {
                _unitOfWork.Company.Update(obj);
                TempData["success"] = "Company updated successfully";
            }
            _unitOfWork.Save();
            return RedirectToAction("Index");
        }
        return View(obj);
    }
    public IActionResult Index()
    {
        return View();
    }
    
    #region API CALLS

    [HttpGet]
    public IActionResult GetAll()
    {
        var companyList = _unitOfWork.Company.GetAll();
        return Json(new { data = companyList });
    }
    
    [HttpDelete]
    public IActionResult Delete(int? id)
    {
        var obj = _unitOfWork.Company.GetFirstOrDefault(u => u.Id == id);
        if (obj == null)
        {
            return NotFound();
        }
        _unitOfWork.Company.Remove(obj);
        _unitOfWork.Save();
        return Json(new { success = true, message = "Company successfully deleted" });
    }
    
    #endregion
    
}