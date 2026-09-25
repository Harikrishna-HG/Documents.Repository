using System.ComponentModel.DataAnnotations.Schema;
using Document.Repository.Data;

namespace Document.Repository.Models.Entities;
public class Student
{
    public int Id { get; set; }
    public string? RegistrationNumber { get; set; }
    public required string Name { get; set; }
    public string? ProfilePic { get; set; }
    [NotMapped]
    public IFormFile? ProfilePicFile { get; set; }
    public required string Semester { get; set; }

    public required int ProgrammeId { get; set; }
    public Programme? Programme { get; set; }

    public List<Project>? Projects { get; set; }
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }
}