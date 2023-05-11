using Models;

namespace BulkyBook.DataAccess.Repository.IRepository;

public interface IOrderHeaderRepository : IRepository<OrderHeader>
{
    void Update(OrderHeader obj);

    void UpdateStatus(int id, string orderStatus, string? paymentStatus = null);
    
}