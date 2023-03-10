using BulkyBook.DataAccess.Repository.IRepository;
using DataAccess;

namespace BulkyBook.DataAccess.Repository;

public class UnitOfWork : IUnitOfWork
{
    private ApplicationDBContext _db;
    
    public UnitOfWork(ApplicationDBContext db)
    {
        _db = db;
        Category = new CategoryRepository(_db);
    }
    
    public ICategoryRepository Category { get; private set; }

    public void Save()
    {
        _db.SaveChanges();
    }
}