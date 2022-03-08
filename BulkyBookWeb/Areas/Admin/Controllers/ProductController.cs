using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BulkyBookWeb.Areas.Admin.Controllers;
[Area("Admin")]
public class ProductController : Controller

{
    private readonly IUnitOfWork _unitOfWork;

    public ProductController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    public IActionResult Index()
    {
        IEnumerable<CoverType> objCoverTypeList = _unitOfWork.CoverType.GetAll();
        return View(objCoverTypeList);
    }
    
    //GET
    public IActionResult Upsert(int? id)
    {
        Product product = new();
        IEnumerable<SelectListItem> CategoryList = _unitOfWork.Category.GetAll().Select(
        
            u=> new SelectListItem
            {
                Text =u.Name,
                Value = u.Id.ToString()
            });

        IEnumerable<SelectListItem> CoverTypeList = _unitOfWork.CoverType.GetAll().Select(

            u => new SelectListItem
            {
                Text = u.Name,
                Value = u.Id.ToString()
            });

        if (id==null || id == 0)
        {
            //create product
            ViewBag.CategoryList = CategoryList;
            ViewData["CoverTypeList"] = CoverTypeList;
            return View(product);
        }
        else
        {
            //update product

        }
       
        return View();
    }
    //Post
    [HttpPost]
    [ValidateAntiForgeryToken]

    public IActionResult Upsert(CoverType obj)
    {
        if (ModelState.IsValid)
        {
            _unitOfWork.CoverType.Update(obj);
            _unitOfWork.Save();
            TempData["success"] = "Cover Type updated successfully";
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

        var coverTypFromDbFirst = _unitOfWork.CoverType.GetFirstOrDefault(u => u.Id == id);

        if (coverTypFromDbFirst == null)
        {
            return NotFound();
        }

        return View(coverTypFromDbFirst);
    }
    //Post
    [HttpPost,ActionName("Delete")] //change action name 
    [ValidateAntiForgeryToken]

    public IActionResult DeletePost(int? id)
    {
        var obj = _unitOfWork.CoverType.GetFirstOrDefault(u => u.Id == id);
        if (obj==null)
        {
            return NotFound();
        }
        _unitOfWork.CoverType.Remove(obj);
            _unitOfWork.Save();
            TempData["success"] = "CoverType deleted successfully";
        return RedirectToAction("Index");

           

    }
}