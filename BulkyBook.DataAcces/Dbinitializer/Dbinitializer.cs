using BulkyBook.DataAccess.Data;
using BulkyBook.Models;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BulkyBook.DataAccess.Dbinitializer
{
    public class Dbinitializer: IDbinitializer
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _db;

        public Dbinitializer(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext db)
        {

            _roleManager = roleManager;
            _userManager = userManager;
            _db= db;
        }

        public void Initialize()
        {
            //Apply migrations if not
            try
            {
                if (_db.Database.GetPendingMigrations().Count() >0!)
                {
                    _db.Database.Migrate();
                }
            }
            catch (Exception ex)
            {
                // ignored
            }
            //create roles if not 
            if (!_roleManager.RoleExistsAsync(SD.Role_Admin).GetAwaiter().GetResult())
            {
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Admin)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Employee)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(SD.Role_User_Comp)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(SD.Role_User_Indi)).GetAwaiter().GetResult();
                //if role created, create admin

                _userManager.CreateAsync(new ApplicationUser
                {
                    UserName = "admin@bulky.com",
                    Email = "admin@bulky.com",
                    Name = "Mariusz Zajac",
                    PhoneNumber = "511011043",
                    StreetAddress = "Unknown",
                    State = "Poland",
                    PostalCode = "Unknown",
                    City = "Walinna"
                }, "Admin123*").GetAwaiter().GetResult();

                ApplicationUser user = _db.ApplicationUsers.FirstOrDefault(u => u.Email == "admin@bulky.com");

                _userManager.AddToRoleAsync(user, SD.Role_Admin).GetAwaiter().GetResult();
            }
            return;
        }
    }
}
