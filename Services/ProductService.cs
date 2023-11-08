using ManeroBackendAPI.Enums;
using ManeroBackendAPI.Models;
using ManeroBackendAPI.Repositories;
using System.Diagnostics;

namespace ManeroBackendAPI.Services;

public interface IProductService
{
    Task<ServiceResponse<Product>> CreateAsync(ServiceRequest<ProductSchema> request);
    Task<ServiceResponse<Product>> GetByArticleNumberAsync(int articleNumber);
    Task<ServiceResponse<List<Product>>> GetAllAsync();
    Task<ServiceResponse<Product>> UpdateAsync(int id, ProductSchema productSchema);
    Task<ServiceResponse<bool>> DeleteAsync(int id);

    Task<ServiceResponse<(Product, Category)>> Search(string term);
    Task<ServiceResponse<Product>> FilterProductsByPriceAsync(decimal minPrice, decimal maxPrice);

}
public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;

    public ProductService(IProductRepository productRepository, ICategoryRepository categoryRepository)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
    }

    public async Task<ServiceResponse<Product>> CreateAsync(ServiceRequest<ProductSchema> request)
    {
        var response = new ServiceResponse<Product>();

        try
        {
            if (request.Content != null)
            {
                var productSchema = request.Content;

                // Kolla om produkten redan finns i databasen
                bool productExists = await _productRepository.ExistsAsync(entity => entity.Name == productSchema.Name);

                if (productExists)
                {
                    // Produkten finns redan, returnera konfliktstatus
                    response.StatusCode = StatusCode.Conflict;
                    response.Content = null;
                }
                else
                {
                    Category category = await _categoryRepository.GetCategoryByNameAsync(productSchema.CategoryName);

                    if (category == null)
                    {
                        // Kategorin finns inte, skapa en ny
                        category = new Category { Name = productSchema.CategoryName };
                        category = await _categoryRepository.CreateCategoryAsync(category);
                    }

                    // Skapa produkt och koppla till kategorin
                    var productEntity = new ProductEntity
                    {
                        Name = productSchema.Name,
                        SupplierArticleNumber = productSchema.SupplierArticleNumber,
                        Description = productSchema.Description,
                        ImageUrl = productSchema.ImageUrl,
                        Price = productSchema.Price,
                        CategoryId = category.CategoryId
                    };

                    response.Content = await _productRepository.CreateAsync(productEntity);
                    response.StatusCode = StatusCode.Created;
                }
            }
            else
            {
                response.StatusCode = StatusCode.BadRequest;
                response.Content = null;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            response.StatusCode = StatusCode.InternalServerError;
            response.Content = null;
        }

        return response;
    }



    public async Task<ServiceResponse<List<Product>>> GetAllAsync()
    {
        var response = new ServiceResponse<List<Product>>
        {
            StatusCode = StatusCode.Ok,
            Content = new List<Product>()
        };

        try
        {
            var result = await _productRepository.ReadAllAsync();
            foreach (var entity in result)
               response.Content.Add(entity);
        }

        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            response.StatusCode = StatusCode.InternalServerError;
            response.Content = null;
        }

        return response;

    }

    public async Task<ServiceResponse<Product>> GetByArticleNumberAsync(int articleNumber)
    {
        var response = new ServiceResponse<Product>();

        try
        {
            var productEntity = await _productRepository.ReadAsync(entity => entity.ArticleNumber == articleNumber);

            if (productEntity != null)
            {
                var product = productEntity;

                response.Content = product;
                response.StatusCode = StatusCode.Ok;
            }
            else
            {
                response.StatusCode = StatusCode.NotFound;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            response.StatusCode = StatusCode.InternalServerError;
            response.Content = null;
        }

        return response;
    }


    public async Task<ServiceResponse<Product>> UpdateAsync(int productId, ProductSchema productSchema)
    {
        var response = new ServiceResponse<Product>();

        try
        {
            // Hitta produkten med det angivna productId
            var existingProduct = await _productRepository.ReadAsync(entity => entity.ArticleNumber == productId);

            if (existingProduct == null)
            {
                // Produkten finns inte, returnera NotFound-status
                response.StatusCode = StatusCode.NotFound;
                response.Content = null;
            }
            else
            {
                // Uppdatera produktens egenskaper
                existingProduct.Name = productSchema.Name;
                existingProduct.SupplierArticleNumber = productSchema.SupplierArticleNumber;
                existingProduct.Description = productSchema.Description;
                existingProduct.ImageUrl = productSchema.ImageUrl;
                existingProduct.Price = productSchema.Price;

                // Spara ändringar i databasen
                var updatedProduct = await _productRepository.UpdateAsync(existingProduct);

                response.Content = updatedProduct;
                response.StatusCode = StatusCode.Ok;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            response.StatusCode = StatusCode.InternalServerError;
            response.Content = null;
        }

        return response;
    }


    public async Task<ServiceResponse<bool>> DeleteAsync(int productId)
    {
        var response = new ServiceResponse<bool>();

        try
        {
            // Hitta produkten med det angivna productId
            var existingProduct = await _productRepository.ReadAsync(entity => entity.ArticleNumber == productId);

            if (existingProduct == null)
            {
                // Produkten finns inte, returnera NotFound-status
                response.StatusCode = StatusCode.NotFound;
                response.Content = false;
            }
            else
            {
                // Ta bort produkten från databasen
                bool deleteResult = await _productRepository.DeleteAsync(existingProduct);

                response.Content = deleteResult;
                response.StatusCode = StatusCode.NoContent;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            response.StatusCode = StatusCode.InternalServerError;
            response.Content = false;
        }

        return response;
    }





    //Söker produkter och kategorier
    public async Task<ServiceResponse<(Product, Category)>> Search(string term)
    {
        var response = new ServiceResponse<(Product, Category)>();

        try
        {
            var productEntity = await _productRepository.ReadAsync(entity => entity.Name.Contains(term));
            var categoryEntity = await _categoryRepository.ReadAsync(entity => entity.Name.Contains(term));


            if (productEntity != null || categoryEntity != null)
            {
                var product = productEntity;
                var category = categoryEntity;

                response.Content = (product!, category);
                response.StatusCode = StatusCode.Ok;
            }
            else
            {
                response.StatusCode = StatusCode.NotFound;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            response.StatusCode = StatusCode.InternalServerError;
            response.Content = default;
        }

        return response;
    }


    //Filtrera produkter efter pris
    public async Task<ServiceResponse<Product>> FilterProductsByPriceAsync(decimal minPrice, decimal maxPrice)
    {
        var response = new ServiceResponse<Product>();

        try
        {
            var products = await _productRepository.SearchProductsByPriceAsync(minPrice, maxPrice);

            if (products != null && products.Any())
            {
                response.Content = products.First(); // Assuming you want to return the first product
                response.StatusCode = StatusCode.Ok;
            }
            else
            {
                response.StatusCode = StatusCode.NotFound;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            response.StatusCode = StatusCode.InternalServerError;
            response.Content = null;
        }

        return response;
    }

}
