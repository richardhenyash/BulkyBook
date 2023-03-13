using System.Linq.Expressions;
using BulkyBook.DataAccess.Repository.IRepository;
using DataAccess;
using Models;

namespace BulkyBook.DataAccess.Repository;

public class CoverTypeRepository : Repository<CoverType>, ICoverTypeRepository
{
    private ApplicationDBContext _db;
    
    public CoverTypeRepository(ApplicationDBContext db) : base(db)
    {
        _db = db;
    }

    public void Update(CoverType obj)
    {
        _db.CoverTypes.Update(obj);
    }

    public void Save()
    {
        _db.SaveChanges();
    }
}