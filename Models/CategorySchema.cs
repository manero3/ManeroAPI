using System.ComponentModel.DataAnnotations;

namespace ManeroBackendAPI.Models;

public class CategorySchema
{
    [Required]
    public string Name { get; set; } = null!;
}
