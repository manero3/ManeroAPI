using ManeroBackendAPI.Models;
using ManeroBackendAPI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace ManeroBackendAPI.Controllers
{
    [Route("api/products")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;

        public ProductsController(IProductService productService, ICategoryService categoryService)
        {
            _productService = productService;
            _categoryService = categoryService;
        }


        [HttpPost]
        public async Task<IActionResult> Create (ProductSchema schema)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest();

                var requset = new ServiceRequest<ProductSchema> { Content = schema };
                var response = await _productService.CreateAsync(requset);

                if (response.StatusCode == Enums.StatusCode.Conflict)
                {
                    return Conflict();
                }

                return StatusCode((int)response.StatusCode, response);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return Problem();
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var response = await _productService.GetAllAsync();
                return StatusCode((int)response.StatusCode, response);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return Problem();
            }
        }


        [HttpGet("{articleNumber}")]
        public async Task<IActionResult> GetByArticleNumber(int articleNumber)
        {
            try
            {
                var response = await _productService.GetByArticleNumberAsync(articleNumber);
                return StatusCode((int)response.StatusCode, response);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return Problem();
            }
        }



        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, ProductSchema productSchema)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest();

                var response = await _productService.UpdateAsync(id, productSchema);
                return StatusCode((int)response.StatusCode, response);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return Problem();
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var response = await _productService.DeleteAsync(id);
                return StatusCode((int)response.StatusCode, response);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return Problem();
            }
        }


        [HttpGet("byCategory/{categoryId}")]
        public async Task<IActionResult> GetProductsByCategory(int categoryId)
        {
            try
            {
                // Anropa ICategoryService för att hämta produkterna i kategorin med categoryId
                var productsInCategory = await _categoryService.GetProductsByCategoryAsync(categoryId);

                if (productsInCategory == null)
                {
                    return NotFound(); // Om kategorin inte finns, returnera 404 Not Found
                }

                return Ok(productsInCategory); // Returnera listan med produkter i kategorin
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return Problem();
            }
        }




        [HttpGet("search")]
        public async Task<IActionResult> SearchProductAndCategory(string term)
        {
            try
            {
                var response = await _productService.Search(term);
                return StatusCode((int)response.StatusCode, response);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return Problem();
            }
        }


        [HttpGet("searchByName")]
        public async Task<IActionResult> FilterProductsByName([FromQuery] string name)
        {
            try
            {
                var filteredProducts = await _productService.Search(name); // Set minPrice and maxPrice to 0
                return StatusCode((int)filteredProducts.StatusCode, filteredProducts);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return Problem();
            }
        }


        [HttpGet("searchByPrice")]
        public async Task<IActionResult> FilterProductsByPrice([FromQuery] decimal minPrice, [FromQuery] decimal maxPrice)
        {
            try
            {
                var response = await _productService.FilterProductsByPriceAsync(minPrice, maxPrice);
                return StatusCode((int)response.StatusCode, response);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return Problem();
            }
        }

    }
}
