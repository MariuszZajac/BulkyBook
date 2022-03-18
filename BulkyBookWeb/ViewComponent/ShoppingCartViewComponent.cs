﻿using System.Security.Claims;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;

namespace BulkyBookWeb.ViewComponent
{
    public class ShoppingCartViewComponent : Microsoft.AspNetCore.Mvc.ViewComponent
    {
        private readonly IUnitOfWork _unitOfWork;

        public ShoppingCartViewComponent(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var claimsIdentity = (ClaimsIdentity) User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            if (claim!=null)
            {
                if (HttpContext.Session.GetInt32(SD.SessionCart) != null)
                {
                    return View(HttpContext.Session.GetInt32(SD.SessionCart));
                }
                else
                {
                    HttpContext.Session.SetInt32(SD.SessionCart,
                        _unitOfWork.ShoppingCard.GetAll(u => u.ApplicationUserId == claim.Value).ToList().Count);
                    return View(HttpContext.Session.GetInt32(SD.SessionCart));
                }
            }
            else
            {
                HttpContext.Session.Clear();
                return View(0);
            }
        }

    }
}
