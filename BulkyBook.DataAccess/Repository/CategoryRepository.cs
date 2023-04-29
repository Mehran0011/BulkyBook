using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BulkyBook.DataAccess.Repository
{
    public class CategoryRepository : Repository<Category>, ICategoryRepository
    {
        private readonly ApplicationDBContext _db;
        public CategoryRepository(ApplicationDBContext db):base(db)
        {
            _db = db;
        }


        public void Update(Category obj)
        {
            _db.Categories.Update(obj);
        }
    }
}
