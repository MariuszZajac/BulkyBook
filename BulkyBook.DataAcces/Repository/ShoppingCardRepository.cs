using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;

namespace BulkyBook.DataAccess.Repository
{
    public class ShoppingCardRepository : Repository<ShoppingCart>, IShoppingCardRepository
    {
        private ApplicationDbContext _db;
        public ShoppingCardRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }


        public int IncrementCount(ShoppingCart shoppingCart, int count)
        {
            shoppingCart.Count+=count;
            return shoppingCart.Count;
        }

        public int DecrementCount(ShoppingCart shoppingCart, int count)
        {
            shoppingCart.Count -= count;
            return shoppingCart.Count;
        }
    }
}
