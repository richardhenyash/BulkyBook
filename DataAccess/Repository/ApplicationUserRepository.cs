using BulkyBook.DataAccess.Repository.IRepository;
using DataAccess;
using Models;

namespace BulkyBook.DataAccess.Repository;

public class ApplicationUserRepository : Repository<ApplicationUser>, IApplicationUserRepository
{
    private ApplicationDbContext _db;
    
    public ApplicationUserRepository(ApplicationDbContext db) : base(db)
    {
        _db = db;
    }
}