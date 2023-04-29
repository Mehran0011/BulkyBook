using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace BulkyBook.Areas.Customer.Controllers
{
    public class CartController : Controller
    {

        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailSender _emailSender;
        [BindProperty]
        public ShoppingCartVM shoppingCartVM { get; set; }
        public int OrderTotal { get; set; }

        public CartController(IUnitOfWork unitOfWork, IEmailSender emailSender)
        {
            _unitOfWork = unitOfWork;
            _emailSender = emailSender;
        }

        [Area("Customer")]
        [Authorize]
        public IActionResult Index()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            shoppingCartVM = new ShoppingCartVM()
            {
                ListCart = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == claim.Value, includeProperties: "product"),
                OrderHeader = new()
            };

            foreach (var cart in shoppingCartVM.ListCart)
            {
                cart.Price = GetPriceBasedOnQuantity(cart.Count, cart.product.Price,
                    cart.product.Price50, cart.product.Price100);
                shoppingCartVM.OrderHeader.OrderTotal += (cart.Count * cart.Price);
            }

            return View(shoppingCartVM);
        }

        [Area("Customer")]
        public IActionResult Summary()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            shoppingCartVM = new ShoppingCartVM()
            {
                ListCart = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == claim.Value,
                includeProperties: "product"),
                OrderHeader = new()
            };
            shoppingCartVM.OrderHeader.ApplicationUser = _unitOfWork.ApplicationUser.GetFirstOrDefault(
                u => u.Id == claim.Value);

            shoppingCartVM.OrderHeader.Name = shoppingCartVM.OrderHeader.ApplicationUser.Name;
            shoppingCartVM.OrderHeader.PhoneNumber = shoppingCartVM.OrderHeader.ApplicationUser.PhoneNumber;
            shoppingCartVM.OrderHeader.StreetAddress = shoppingCartVM.OrderHeader.ApplicationUser.StreetAddress;
            shoppingCartVM.OrderHeader.City = shoppingCartVM.OrderHeader.ApplicationUser.City;
            shoppingCartVM.OrderHeader.State = shoppingCartVM.OrderHeader.ApplicationUser.State;
            shoppingCartVM.OrderHeader.PostalCode = shoppingCartVM.OrderHeader.ApplicationUser.PostalCode;


            foreach (var cart in shoppingCartVM.ListCart)
            {
                cart.Price = GetPriceBasedOnQuantity(cart.Count, cart.product.Price,
                    cart.product.Price50, cart.product.Price100);
                shoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }
            return View(shoppingCartVM);
        }



        [HttpPost]
        [ActionName("Summary")]
        [ValidateAntiForgeryToken]
        [Area("Customer")]
        public IActionResult SummaryPOST()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            shoppingCartVM.ListCart = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == claim.Value,
                includeProperties: "product");


            shoppingCartVM.OrderHeader.OrderDate = System.DateTime.Now;
            shoppingCartVM.OrderHeader.ApplicationUserId = claim.Value;


            foreach (var cart in shoppingCartVM.ListCart)
            {
                cart.Price = GetPriceBasedOnQuantity(cart.Count, cart.product.Price,
                    cart.product.Price50, cart.product.Price100);
                shoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }
            ApplicationUser applicationUser = _unitOfWork.ApplicationUser.GetFirstOrDefault(u => u.Id == claim.Value);

            if (applicationUser.CompanyId.GetValueOrDefault() == 0)
            {
                shoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
                shoppingCartVM.OrderHeader.OrderStatus = SD.StatusPending;
            }
            else
            {
                shoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
                shoppingCartVM.OrderHeader.OrderStatus = SD.StatusApproved;
            }

            _unitOfWork.OrderHeader.Add(shoppingCartVM.OrderHeader);
            _unitOfWork.Save();
            foreach (var cart in shoppingCartVM.ListCart)
            {
                OrderDetail orderDetail = new()
                {
                    ProductId = cart.ProductId,
                    OrderId = shoppingCartVM.OrderHeader.Id,
                    Price = cart.Price,
                    Count = cart.Count
                };
                _unitOfWork.OrderDetail.Add(orderDetail);
                _unitOfWork.Save();
            }

            if (applicationUser.CompanyId.GetValueOrDefault() == 0)
            {
                //Stripe Settings
                var domain = "https://localhost:44369/";
                var options = new SessionCreateOptions
                {
                    LineItems = new List<SessionLineItemOptions>(),
                    Mode = "payment",
                    SuccessUrl = domain + $"Customer/Cart/OrderConfirmation?id={shoppingCartVM.OrderHeader.Id}",
                    CancelUrl = domain + $"Customer/Cart/Index",
                };


                foreach (var item in shoppingCartVM.ListCart)
                {

                    var sessionLineItem = new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)(item.Price * 100),
                            Currency = "usd",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = item.product.Title,
                            },
                        },
                        Quantity = item.Count,
                    };

                    options.LineItems.Add(sessionLineItem);
                }


                var service = new SessionService();
                Session session = service.Create(options);
                _unitOfWork.OrderHeader.UpdateStripePaymentID(shoppingCartVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
                _unitOfWork.Save();
                Response.Headers.Add("Location", session.Url);
                return new StatusCodeResult(303);
            }

            else
            {
                return RedirectToAction("OrderConfirmation", "Cart", new { id = shoppingCartVM.OrderHeader.Id });
            }

        }

        [Area("Customer")]
        public IActionResult OrderConfirmation(int id)
        {
            OrderHeader orderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == id, includeProperties: "ApplicationUser");
            if (orderHeader.PaymentStatus != SD.PaymentStatusDelayedPayment)
            {
                var service = new SessionService();
                Session session = service.Get(orderHeader.SessionId);
                //check the stripe status
                if (session.PaymentStatus.ToLower() == "paid")
                {
                    _unitOfWork.OrderHeader.UpdateStatus(id, SD.StatusApproved, SD.PaymentStatusApproved);
                    _unitOfWork.Save();
                }
            }
            _emailSender.SendEmailAsync(orderHeader.ApplicationUser.Email, "New Order - Bulky Book", "<p>New Order Created</p>");
            List<ShoppingCart> shoppingCarts = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId ==
            orderHeader.ApplicationUserId).ToList();
            HttpContext.Session.Clear();
            _unitOfWork.ShoppingCart.RemoveRange(shoppingCarts);
            _unitOfWork.Save();
            return View(id);
        }



        [Area("Customer")]
        public IActionResult Plus(int cartId)
        {
            var cart = _unitOfWork.ShoppingCart.GetFirstOrDefault(u => u.Id == cartId);
            _unitOfWork.ShoppingCart.IncreamentCount(cart, 1);
            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }


        [Area("Customer")]
        public IActionResult Minus(int cartId)
        {
            var cart = _unitOfWork.ShoppingCart.GetFirstOrDefault(u => u.Id == cartId);
            if (cart.Count <= 1)
            {
                _unitOfWork.ShoppingCart.Remove(cart);  
                var count = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == cart.ApplicationUserId).ToList().Count-1;
                HttpContext.Session.SetInt32(SD.SessionCart,count);
                 
            }
            else
            {
                _unitOfWork.ShoppingCart.DecreamentCount(cart, 1);

            }
            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }


        [Area("Customer")]
        public IActionResult Remove(int cartId)
        {
            var cart = _unitOfWork.ShoppingCart.GetFirstOrDefault(u => u.Id == cartId);
            _unitOfWork.ShoppingCart.Remove(cart);
            _unitOfWork.Save();
            var count = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == cart.ApplicationUserId).ToList().Count;
            HttpContext.Session.SetInt32(SD.SessionCart, count);
            return RedirectToAction(nameof(Index));
        }


        private double GetPriceBasedOnQuantity(double quantity, double price, double price50, double price100)
        {
            if (quantity <= 50)
            {
                return price;
            }
            else
            {
                if (quantity <= 100)
                {
                    return price50;
                }
                return price100;
            }
        }

    }
}
