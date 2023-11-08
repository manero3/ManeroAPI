using ManeroBackendAPI.Contexts;
using ManeroBackendAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;


namespace ManeroBackendAPI.Repositories;

public interface IProductRepository : IRepo<ProductEntity, ProductContext>
{
    Task<List<Product>> SearchProductsByPriceAsync(decimal minPrice, decimal maxPrice);
}
public class ProductRepository : Repo<ProductEntity, ProductContext>, IProductRepository
{
    protected readonly ProductContext _context;

    public async Task<List<Product>> SearchProductsByPriceAsync(decimal minPrice, decimal maxPrice)
    {
        try
        {
            var productEntities = await _context.Products
                .Where(p => p.Price >= minPrice || p.Price <= maxPrice)
                .ToListAsync();

            var products = productEntities.Select(pe => new Product
            {
                ArticleNumber = pe.ArticleNumber,
                Name = pe.Name,
                Price = pe.Price,
                Description = pe.Description,
                ImageUrl = pe.ImageUrl,
                SupplierArticleNumber = pe.SupplierArticleNumber
            }).ToList();

            return products;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            return null!;
        }
    }



    public ProductRepository(ProductContext context) : base(context)
    {
        _context = context;
    }
}
