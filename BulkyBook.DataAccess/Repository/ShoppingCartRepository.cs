using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BulkyBook.DataAccess.Repository
{
    public class ShoppingCartRepository : Repository<ShoppingCart>, IShoppingCartRepository
    {
        private readonly ApplicationDBContext _db;
        public ShoppingCartRepository(ApplicationDBContext db):base(db)
        {
            _db = db;
        }

        public int DecreamentCount(ShoppingCart shoppingCart, int count)
        {
            shoppingCart.Count -= count;
            return shoppingCart.Count;
        }

        public int IncreamentCount(ShoppingCart shoppingCart, int count)
        {
            shoppingCart.Count += count;
            return shoppingCart.Count;
        }
    }
}
