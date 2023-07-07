using BulkyBook.DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Models;
using Utility;

namespace DataAccess.DbInitializer;

public class DbInitializer : IDbInitializer
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _db;

    public DbInitializer(
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ApplicationDbContext db)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _db = db;
    }

    public void Initialize()
    {
        // apply pending migrations
        try
        {
            if (_db.Database.GetPendingMigrations().Count() > 0)
            {
                _db.Database.Migrate();
            }
        }
        catch (Exception ex)
        {

        }

        // create roles
        if (!_roleManager.RoleExistsAsync(StaticDetails.RoleAdmin).GetAwaiter().GetResult())
        {
            _roleManager.CreateAsync(new IdentityRole(StaticDetails.RoleAdmin)).GetAwaiter().GetResult();
            _roleManager.CreateAsync(new IdentityRole(StaticDetails.RoleEmployee)).GetAwaiter().GetResult();
            _roleManager.CreateAsync(new IdentityRole(StaticDetails.RoleUserIndividual)).GetAwaiter().GetResult();
            _roleManager.CreateAsync(new IdentityRole(StaticDetails.RoleUserCompany)).GetAwaiter().GetResult();

            // create admin user
            _userManager.CreateAsync(new ApplicationUser
            {
                UserName = "admin@test.com",
                Email = "admin@test.com",
                Name = "Admin Test",
                PhoneNumber = "123456789",
                StreetAddress = "1 Test Avenue",
                State = "NY",
                PostalCode = "12345",
                City = "New York"
            }, "Admin123*").GetAwaiter().GetResult();

            ApplicationUser user = _db.ApplicationUsers.FirstOrDefault(u => u.Email == "admin@test.com");
            _userManager.AddToRoleAsync(user, StaticDetails.RoleAdmin).GetAwaiter().GetResult();
        }
        return;
    }
}