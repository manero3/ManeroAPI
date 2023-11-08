using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace ManeroBackendAPI.Models;

public class ProductEntity
{
    [Key]
    public int ArticleNumber { get; set; }

    public string? SupplierArticleNumber { get; set; }

    [Required]
    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    [Required]
    [Column(TypeName = "money")]
    public decimal Price { get; set; }

    [Required]
    public DateTime Created {  get; set; } = DateTime.Now;
    public string? ImageUrl { get; set; }

    public int CategoryId { get; set; }

    public Category Category { get; set; } = null!;

    public static implicit operator ProductEntity(ProductSchema product)
    {
        try
        {
            return new ProductEntity
            {
                Name = product.Name,
                SupplierArticleNumber = product.SupplierArticleNumber,
                Description = product.Description,
                ImageUrl = product.ImageUrl,
                Price = product.Price
            };
        }
        catch (Exception ex) 
        {
            Debug.WriteLine(ex.Message);
            return null!;
        }

    }

}
