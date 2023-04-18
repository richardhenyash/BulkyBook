using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using BulkyBook.DataAccess.Repository.IRepository;
using DataAccess;
using Models;

namespace BulkyBook.DataAccess.Repository;

public class CompanyRepository : Repository<Company>, ICompanyRepository
{
    private ApplicationDbContext _db;

    public CompanyRepository(ApplicationDbContext db) : base(db)
    {
        _db = db;
    }

    public void Update(Company obj)
    { 
        _db.Companies.Update(obj);
    }
}