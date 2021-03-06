using System.Security.Claims;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;

namespace BulkyBookWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private  readonly  IEmailSender  _emailSender;

        [BindProperty]
        public ShoppingCardVM ShoppingCardVM { get; set; }
        public int OrderTotal { get; set; }

        public CartController(IUnitOfWork unitOfWork, IEmailSender emailSender)
        {
            _unitOfWork = unitOfWork;
            _emailSender = emailSender;
        }
        public IActionResult Index()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            ShoppingCardVM = new ShoppingCardVM()
            {
                ListCard = _unitOfWork.ShoppingCard.GetAll(u => u.ApplicationUserId == claim.Value,
                    includeProperties:"Product"),
                OrderHeader = new ()

            };
            foreach (var cart in ShoppingCardVM.ListCard)
            {
                cart.Price = GetPriceBasedOnQuantity(cart.Count, cart.Product.Price, cart.Product.Price50,
                    cart.Product.Price100);
                ShoppingCardVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);

            }
            return View(ShoppingCardVM);
        }
        public IActionResult Summary()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            ShoppingCardVM = new ShoppingCardVM()
            {
                ListCard = _unitOfWork.ShoppingCard.GetAll(u => u.ApplicationUserId == claim.Value,
                    includeProperties: "Product"),
                OrderHeader = new OrderHeader()

            };
            ShoppingCardVM.OrderHeader.ApplicationUser =
                _unitOfWork.ApplicationUser.GetFirstOrDefault(u => u.Id == claim.Value);

            ShoppingCardVM.OrderHeader.Name = ShoppingCardVM.OrderHeader.ApplicationUser.Name;
            ShoppingCardVM.OrderHeader.PhoneNumber = ShoppingCardVM.OrderHeader.ApplicationUser.PhoneNumber;
            ShoppingCardVM.OrderHeader.StreetAddress = ShoppingCardVM.OrderHeader.ApplicationUser.StreetAddress;
            ShoppingCardVM.OrderHeader.City = ShoppingCardVM.OrderHeader.ApplicationUser.City;
            ShoppingCardVM.OrderHeader.State = ShoppingCardVM.OrderHeader.ApplicationUser.State;
            ShoppingCardVM.OrderHeader.PostalCode = ShoppingCardVM.OrderHeader.ApplicationUser.PostalCode;
            foreach (var cart in ShoppingCardVM.ListCard)
            {
                cart.Price = GetPriceBasedOnQuantity(cart.Count, cart.Product.Price, cart.Product.Price50,
                    cart.Product.Price100);
                ShoppingCardVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);

            }
            return View(ShoppingCardVM);
        }

        [HttpPost]
        [ActionName("Summary")]
        [ValidateAntiForgeryToken]
        public IActionResult SummaryPOST()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            ShoppingCardVM.ListCard = _unitOfWork.ShoppingCard.GetAll(u => u.ApplicationUserId == claim.Value,
                includeProperties: "Product");

            ShoppingCardVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
            ShoppingCardVM.OrderHeader.OrderStatus = SD.StatusPending;
            ShoppingCardVM.OrderHeader.OrderDate = System.DateTime.UtcNow;
            ShoppingCardVM.OrderHeader.ApplicationUserId = claim.Value;

            foreach (var cart in ShoppingCardVM.ListCard)
            {
                cart.Price = GetPriceBasedOnQuantity(cart.Count, cart.Product.Price, cart.Product.Price50,
                    cart.Product.Price100);
                ShoppingCardVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }
           
            ApplicationUser applicationUser = _unitOfWork.ApplicationUser.GetFirstOrDefault(u => u.Id == claim.Value);

            if (applicationUser.CompanyId.GetValueOrDefault() == 0)
            {
                ShoppingCardVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
                ShoppingCardVM.OrderHeader.OrderStatus = SD.StatusPending;
            }
            else
            {
                ShoppingCardVM.OrderHeader.PaymentStatus = SD.PaymentStatusDelayPayment;
                ShoppingCardVM.OrderHeader.OrderStatus = SD.StatusApproved;
            }
            

            _unitOfWork.OrderHeader.Add(ShoppingCardVM.OrderHeader);
            _unitOfWork.Save();
            
            foreach (var cart in ShoppingCardVM.ListCard)
            {
                OrderDetails orderDetail = new()
                {
                    ProductId = cart.ProductId,
                    OrderId = ShoppingCardVM.OrderHeader.Id,
                    Price = cart.Price,
                    Count = cart.Count
                };
                _unitOfWork.OrderDetail.Add(orderDetail);
                _unitOfWork.Save();
            }

            if (applicationUser.CompanyId.GetValueOrDefault() == 0)
            {

                //Stripe settings
                var domain = "https://localhost:44353/";
                var options = new SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string>
                    {
                        "card",
                    },
                    LineItems = new List<SessionLineItemOptions>(),

                    Mode = "payment",
                    SuccessUrl = domain + $"customer/cart/OrderConfirmation?id={ShoppingCardVM.OrderHeader.Id}",
                    CancelUrl = domain + $"customer/cart/index",
                };
                foreach (var item in ShoppingCardVM.ListCard)
                {

                    var sessionLineItem = new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long) (item.Price * 100), //20.00zł -> convert to 2000
                            Currency = "usd",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = item.Product.Title
                            },

                        },
                        Quantity = item.Count,
                    };
                    options.LineItems.Add(sessionLineItem);

                }

                var service = new SessionService();
                Session session = service.Create(options);
                ShoppingCardVM.OrderHeader.SessionId = session.Id;
                ShoppingCardVM.OrderHeader.PaymentIntentId = session.PaymentIntentId;
                _unitOfWork.OrderHeader.UpdateStripePaymentID(ShoppingCardVM.OrderHeader.Id, session.Id,
                    session.PaymentIntentId);
                _unitOfWork.Save();
                Response.Headers.Add("Location", session.Url);
                return new StatusCodeResult(303);

            }
            else
            {
                return RedirectToAction("OrderConfirmation","Cart",new {id=ShoppingCardVM.OrderHeader.Id});
            }
           
        }


        public IActionResult OrderConfirmation (int id)
        {
            OrderHeader orderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == id, includeProperties:"ApplicationUser");
            if (orderHeader.PaymentStatus != SD.PaymentStatusDelayPayment)
            {
                var service = new SessionService();
                Session session = service.Get(orderHeader.SessionId);
                // check the stripe status
                if (session.PaymentStatus.ToLower() == "paid")
                {
                    _unitOfWork.OrderHeader.UpdateStatus(id, SD.StatusApproved, SD.PaymentStatusApproved);
                    _unitOfWork.Save();

                }
            }

            _emailSender.SendEmailAsync(orderHeader.ApplicationUser.Email, "New Order - bulky book",
                "<p> New order created</p>");// send new email confirmed to order
            List<ShoppingCart> shoppingCarts = _unitOfWork.ShoppingCard.GetAll(u => u.ApplicationUserId == orderHeader.ApplicationUserId).ToList();
           HttpContext.Session.Clear();
            _unitOfWork.ShoppingCard.RemoveRange(shoppingCarts);
            _unitOfWork.Save();
            return View(id) ;
        }

        public IActionResult Plus(int cartId)
        {
            var cart = _unitOfWork.ShoppingCard.GetFirstOrDefault(u => u.Id == cartId);
            _unitOfWork.ShoppingCard.IncrementCount(cart, 1);
            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }
        public IActionResult Minus(int cartId)
        {
            var cart = _unitOfWork.ShoppingCard.GetFirstOrDefault(u => u.Id == cartId);
            if (cart.Count<=1)
            {
                _unitOfWork.ShoppingCard.Remove(cart);
                var count = _unitOfWork.ShoppingCard.GetAll(u => u.ApplicationUserId == cart.ApplicationUserId).ToList().Count-1;
                HttpContext.Session.SetInt32(SD.SessionCart, count);
            }
            else
            {
               _unitOfWork.ShoppingCard.DecrementCount(cart, 1); 
            }
            
            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }
        public IActionResult Remove(int cartId)
        {
            var cart = _unitOfWork.ShoppingCard.GetFirstOrDefault(u => u.Id == cartId);
            _unitOfWork.ShoppingCard.Remove(cart);
            _unitOfWork.Save();
            var count = _unitOfWork.ShoppingCard.GetAll(u => u.ApplicationUserId == cart.ApplicationUserId).ToList()
                .Count;
            HttpContext.Session.SetInt32(SD.SessionCart, count);
            return RedirectToAction(nameof(Index));
        }

        private double GetPriceBasedOnQuantity(double quantity, double price, double price50, double price100)
        {
            if (quantity<=50)
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
