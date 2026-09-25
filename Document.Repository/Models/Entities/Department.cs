using System.ComponentModel.DataAnnotations;

namespace Document.Repository.Models.Entities;
public class Department
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Department Name is required.")]
    public required string Name { get; set; }
    public string? Details { get; set; }

    [Required(ErrorMessage = "Department Head is required.")]
    public required string Head { get; set; }
    public string? Extension { get; set; }

    [Required(ErrorMessage = "Please select a valid College.")]
    public required Guid CollegeId { get; set; }

    public College? College { get; set; }

    public List<Programme>? Programmes { get; set; }
}
