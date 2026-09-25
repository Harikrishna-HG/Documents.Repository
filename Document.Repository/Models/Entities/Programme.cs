using System.ComponentModel.DataAnnotations;

namespace Document.Repository.Models.Entities;
public class Programme
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Program Name is required.")]
    public required string Name { get; set; }
    public string? Details { get; set; }
    [Required(ErrorMessage = "Please select a valid College.")]
    public required int DepartmentId { get; set; }
    public Department? Department { get; set; }

    public List<Student>? Students { get; set; }
}
