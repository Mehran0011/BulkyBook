using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace BulkyBook.DataAccess.Repository
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly ApplicationDBContext _db;
        internal DbSet<T> dbSet;
        public Repository(ApplicationDBContext db)
        {
            _db = db;
            //_db.ShoppingCarts.Include(u => u.product);
            this.dbSet = _db.Set<T>();
        }

        public void Add(T entity)
        {
            dbSet.Add(entity);
        }

        public IEnumerable<T> GetAll(Expression<Func<T, bool>>? filter = null, string? includeProperties = null)
        {

            IQueryable<T> query = dbSet;
            if (filter != null)
            {
                query.Where(filter);
            }
            if (includeProperties != null)
            {
                foreach (var includeProps in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeProps);
                }
            }
            return query.ToList();
        }

        public T GetFirstOrDefault(Expression<Func<T, bool>> filter, string? includeProperties = null, bool tracked = true)
        {
            IQueryable<T> query;


            if (tracked)
            { 
                query=dbSet;
            }
            else
            {
                query = dbSet.AsNoTracking();
            }



                query.Where(filter);
            if (includeProperties != null)
            {
                foreach (var includeProps in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeProps);
                }
            }
            return query.FirstOrDefault(filter);
        }

        public void Remove(T entity)
        {
            dbSet.Remove(entity);
        }

        public void RemoveRange(IEnumerable<T> entities)
        {
            dbSet.RemoveRange(entities);
        }
    }
}
