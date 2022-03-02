using BulkyBookWeb.Models;
using BulkyBookWeb.Models.Data;
using Microsoft.AspNetCore.Mvc;

namespace BulkyBookWeb.Controllers;

public class CategoryController : Controller
{
    private readonly ApplicationDbContext _db;

    public CategoryController(ApplicationDbContext db)
    {
        _db = db;
    }
    public IActionResult Index()
    {
        IEnumerable<Category> objCategoryList = _db.Categories;
        return View(objCategoryList);
    }
    //GET
    public IActionResult Create()
    {
        
        return View();
    }
    //Post
    [HttpPost]
    [ValidateAntiForgeryToken]

    public IActionResult Create(Category obj)
    {
        if (obj.Name==obj.DisplayOrder.ToString())
        {
            ModelState.AddModelError("Name","The DisplayOrder cannot exactly math the Name.");
        }
        if (ModelState.IsValid)
        {
            _db.Categories.Add(obj);
            _db.SaveChanges();
            return RedirectToAction("Index");  
        }
        return View(obj);
       
    }
}