using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BulkyBook.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ProductController : Controller
    {

        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _hostEnvironment;

        public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment hostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _hostEnvironment = hostEnvironment;

        }
        public IActionResult Index()
        {
            return View();
        }



        //GET
        public IActionResult Upsert(int? id)
        {
            ProductVM productVM = new ProductVM()
            {
                product = new Product(),
                CategoryList = _unitOfWork.Category.GetAll().Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                }),
                CoverTypeList = _unitOfWork.CoverType.GetAll().Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                })

            };

            if (id == null || id == 0)
            {
                //Create a Product
                //ViewBag.CategoryList = CategoryList;
                //ViewData["CoverTypeList"] = CoverTypeList;
                return View(productVM);
            }
            else
            {
                productVM.product = _unitOfWork.Product.GetFirstOrDefault(x => x.Id == id);
                return View(productVM);

                //Updates a Product
            }

        }

        //POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(ProductVM obj, IFormFile? file)
        {


            if (ModelState.IsValid)
            {
                string wwwRootPath = _hostEnvironment.WebRootPath;
                if (file != null)
                {
                    string fileName = Guid.NewGuid().ToString();
                    var uploads = Path.Combine(wwwRootPath, @"Images/Products");
                    string extension = Path.GetExtension(file.FileName);

                    if (obj.product.ImageUrl != null)
                    {
                        var oldImage = Path.Combine(wwwRootPath, obj.product.ImageUrl.TrimStart('\\'));
                        if (System.IO.File.Exists(oldImage))
                        {
                            System.IO.File.Delete(oldImage);
                        }
                    }

                    using (var fileStreams = new FileStream(Path.Combine(uploads, fileName + extension), FileMode.Create))
                    {
                        file.CopyTo(fileStreams);
                    }
                    obj.product.ImageUrl = @"Images/Products/" + fileName + extension;

                }

                if (obj.product.Id == 0)
                {
                    _unitOfWork.Product.Add(obj.product);

                }
                else
                {
                    _unitOfWork.Product.Update(obj.product);

                }
                _unitOfWork.Save();
                TempData["successful"] = "Product Created Successfuly!";
                return RedirectToAction("Index");

            }
            return View(obj);

        }

        //GET
        //public IActionResult Delete(int? id)
        //{
        //    if (id == null || id == 0)
        //    {
        //        return NotFound();
        //    }

        //    var ProductFromDb = _unitOfWork.Product.GetFirstOrDefault(u => u.Id == id);
        //    if (ProductFromDb == null)
        //    {
        //        return NotFound();
        //    }

        //    return View(ProductFromDb);
        //}



        #region API CALLS
        [HttpGet]
        public IActionResult GetAll()
        {
            var productList = _unitOfWork.Product.GetAll(includeProperties: "Category,CoverType");
            return Json(new { data = productList });
        }

        //POST
        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var obj = _unitOfWork.Product.GetFirstOrDefault(u => u.Id == id);
            if (obj == null)
            {
                NotFound();
            }

            var oldImage = Path.Combine(_hostEnvironment.WebRootPath, obj.ImageUrl.TrimStart('\\'));
            if (System.IO.File.Exists(oldImage))
            {
                System.IO.File.Delete(oldImage);
            }
            _unitOfWork.Product.Remove(obj);
            _unitOfWork.Save();
            return Json(new { success = true, message = "Deleted Successfully!" });





        }

        #endregion
    }
}
