using ManeroBackendAPI.Contexts;
using ManeroBackendAPI.Models;
using Microsoft.EntityFrameworkCore;


namespace ManeroBackendAPI.Repositories;


public interface ICategoryRepository : IRepo<Category, ProductContext>
{
    Task<Category> GetCategoryByNameAsync(string categoryName);
    Task<Category> CreateCategoryAsync(Category category);
    Task<List<Product>> GetProductsByCategoryIdAsync(int categoryId);


}
public class CategoryRepository : Repo<Category, ProductContext>, ICategoryRepository
{
    private readonly ProductContext _context;
    public CategoryRepository(ProductContext context) : base(context)
    {
        _context = context;
    }

    public async Task<Category> GetCategoryByNameAsync(string categoryName)
    {
        return await _context.Set<Category>().FirstOrDefaultAsync(c => c.Name == categoryName) ?? null!;
    }

    public async Task<Category> GetCategoryByIdAsync(int categoryId)
    {
        return await _context.Set<Category>().FindAsync(categoryId) ?? null!;
    }

    public async Task<Category> CreateCategoryAsync(Category category)
    {
        await _context.Set<Category>().AddAsync(category);
        await _context.SaveChangesAsync();
        return category;
    }

    public async Task<List<Product>> GetProductsByCategoryIdAsync(int categoryId)
    {
        var productEntities = await _context.Products.Where(p => p.CategoryId == categoryId).ToListAsync();

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

}
