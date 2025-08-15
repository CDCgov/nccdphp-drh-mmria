using System.ComponentModel.DataAnnotations;

namespace mmria.blazor.component;
public class ExampleModel
{
    [Required]
    [StringLength(10, ErrorMessage = "Name is too long.")]
    public string Name { get; set; }
}