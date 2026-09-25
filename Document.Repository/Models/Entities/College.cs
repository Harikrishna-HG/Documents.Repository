using Document.Repository.Data;
using System.ComponentModel.DataAnnotations;

namespace Document.Repository.Models.Entities;
public class College
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "College Name is required.")]
    public required string Name { get; set; }

    [Required(ErrorMessage = "Contact Number is required.")]
    public required string Contact { get; set; }

    [Required(ErrorMessage = "Campus Chief is required.")]
    public required string CampusChief { get; set; }

    [Required(ErrorMessage = "College Address is required.")]
    public required string Address { get; set; }
    public string? Details { get; set; }
    public List<Department>? Departments { get; set; }

    public string? CreatedByUserId { get; set; }
    public ApplicationUser? CreatedByUser { get; set; }
}
