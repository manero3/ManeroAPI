using ManeroBackendAPI.Models;
using Microsoft.EntityFrameworkCore;


namespace ManeroBackendAPI.Contexts;

public class ProductContext : DbContext
{
    public ProductContext()
    {
        
    }
    public ProductContext(DbContextOptions<ProductContext> options) : base(options)
    {
    }

    public DbSet<ProductEntity> Products { get; set; }
    public DbSet<Category> Categories { get; set; }
}
