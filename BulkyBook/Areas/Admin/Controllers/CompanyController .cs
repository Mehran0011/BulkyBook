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
    public class CompanyController : Controller
    {

        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _hostEnvironment;

        public CompanyController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;

        }
        public IActionResult Index()
        {
            return View();
        }



        //GET
        public IActionResult Upsert(int? id)
        {

            Company company = new Company();
            if (id == null || id == 0)
            {
                return View(company);
            }
            else
            {
                company = _unitOfWork.Company.GetFirstOrDefault(x => x.ID == id);
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
               
                if (obj.ID == 0)
                {
                    _unitOfWork.Company.Add(obj);
                    TempData["successful"] = "Company Created Successfuly!";

                }
                else
                {
                    _unitOfWork.Company.Update(obj);
                    TempData["successful"] = "Company Updated Successfuly!";


                }
                _unitOfWork.Save();
                return RedirectToAction("Index");

            }
            return View(obj);

        }

        


        #region API CALLS
        [HttpGet]
        public IActionResult GetAll()
        {
            var companyList = _unitOfWork.Company.GetAll();
            return Json(new { data = companyList });
        }

        //POST
        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var obj = _unitOfWork.Company.GetFirstOrDefault(u => u.ID == id);
            if (obj == null)
            {
                NotFound();
            }
            _unitOfWork.Company.Remove(obj);
            _unitOfWork.Save();
            return Json(new { success = true, message = "Deleted Successfully!" });





        }

        #endregion
    }
}
