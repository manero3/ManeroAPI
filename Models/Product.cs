using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;

namespace ManeroBackendAPI.Models
{
    public class Product
    {
        [Key]
        public int ArticleNumber { get; set; }
        public string? SupplierArticleNumber { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }

        [Column(TypeName = "money")]
        public decimal? Price { get; set; }
        public string? ImageUrl { get; set; }

        public static implicit operator Product(ProductEntity product)
        {
            try
            {
                return new Product
                {
                    ArticleNumber = product.ArticleNumber,
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
}
