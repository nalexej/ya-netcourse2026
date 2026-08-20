using EventMgtApi.UsersService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventMgtApi.UsersService.Infrastructure.Persistence;

public class UserDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public UserDbContext(DbContextOptions<UserDbContext> options) : base(options) { }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UserDbContext).Assembly);
    }
}