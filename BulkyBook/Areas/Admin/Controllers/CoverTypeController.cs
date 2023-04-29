using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace BulkyBook.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class CoverTypeController : Controller
    {

        private readonly IUnitOfWork _unitOfWork;

        public CoverTypeController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public IActionResult Index()
        {
            IEnumerable<CoverType> ObjCoverTypeList = _unitOfWork.CoverType.GetAll();
            return View(ObjCoverTypeList);
        }

        //GET
        public IActionResult Create()
        {
            return View();
        }

        //POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(CoverType obj)
        {

          
            if (ModelState.IsValid)
            {
                _unitOfWork.CoverType.Add(obj);
                _unitOfWork.Save();
                TempData["successful"] = "CoverType Created Successfuly!";
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

            var CoverTypeFromDb = _unitOfWork.CoverType.GetFirstOrDefault(u => u.Id == id);
            if (CoverTypeFromDb == null)
            {
                return NotFound();
            }

            return View(CoverTypeFromDb);
        }

        //POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(CoverType obj)
        {

           
            if (ModelState.IsValid)
            {
                _unitOfWork.CoverType.Update(obj);
                _unitOfWork.Save();
                TempData["successful"] = "CoverType Updated Successfuly!";
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

            var CoverTypeFromDb = _unitOfWork.CoverType.GetFirstOrDefault(u => u.Id == id);
            if (CoverTypeFromDb == null)
            {
                return NotFound();
            }

            return View(CoverTypeFromDb);
        }

        //POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(CoverType obj)
        {
            if (obj == null)
            {
                NotFound();
            }
            _unitOfWork.CoverType.Remove(obj);
            _unitOfWork.Save();
            TempData["successful"] = "CoverType Deleted Successfuly!";
            return RedirectToAction("Index");





        }
    }
}
