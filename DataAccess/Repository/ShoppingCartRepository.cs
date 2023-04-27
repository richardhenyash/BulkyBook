using BulkyBook.DataAccess.Repository.IRepository;
using DataAccess;
using Models;

namespace BulkyBook.DataAccess.Repository;

public class ShoppingCartRepository : Repository<ShoppingCart>, IShoppingCartRepository
{
    private ApplicationDbContext _db;
    
    public ShoppingCartRepository(ApplicationDbContext db) : base(db)
    {
        _db = db;
    }
}