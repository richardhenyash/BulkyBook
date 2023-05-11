using BulkyBook.DataAccess.Repository.IRepository;
using DataAccess;
using Models;

namespace BulkyBook.DataAccess.Repository;

public class OrderDetailRepository : Repository<OrderDetail>, IOrderDetailRepository
{
    private ApplicationDbContext _db;
    
    public OrderDetailRepository(ApplicationDbContext db) : base(db)
    {
        _db = db;
    }

    public void Update(OrderDetail obj)
    {
        _db.OrderDetail.Update(obj);
    }
}