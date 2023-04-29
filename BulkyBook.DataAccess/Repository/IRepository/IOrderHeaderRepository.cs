
using BulkyBook.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BulkyBook.DataAccess.Repository.IRepository
{
    public interface IOrderHeaderRepository : IRepository<OrderHeader>
    {
        void Update(OrderHeader obj);
        void UpdateStatus(int id, string orderstatus, string? paymentstatus = null);
        void UpdateStripePaymentID(int id, string sessionId, string paymentItentId);


    }
}
