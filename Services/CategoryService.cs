using ManeroBackendAPI.Enums;
using ManeroBackendAPI.Models;
using ManeroBackendAPI.Repositories;
using System.Diagnostics;


namespace ManeroBackendAPI.Services;

public interface ICategoryService
{
    Task<ServiceResponse<Category>> CreateCategoryAsync(ServiceRequest<CategorySchema> request);
    Task<ServiceResponse<List<Product>>> GetProductsByCategoryAsync(int categoryId);

    Task<ServiceResponse<Category>> SearchCategoryAsync(string term);
}
public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;

    public CategoryService(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public Task<ServiceResponse<Category>> CreateCategoryAsync(ServiceRequest<CategorySchema> request)
    {
        throw new NotImplementedException();
    }

    public async Task<ServiceResponse<List<Product>>> GetProductsByCategoryAsync(int categoryId)
    {
        var response = new ServiceResponse<List<Product>>();

        try
        {
            var products = await _categoryRepository.GetProductsByCategoryIdAsync(categoryId);

            if (products != null && products.Any())
            {
                response.Content = products;
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
        }

        return response;
    }



    public async Task<ServiceResponse<Category>> SearchCategoryAsync(string term)
    {
        var response = new ServiceResponse<Category>();

        try
        {
            var categoryEntity = await _categoryRepository.ReadAsync(entity => entity.Name.Contains(term));

            if (categoryEntity != null)
            {
                var category = categoryEntity;
                response.Content = category;
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
