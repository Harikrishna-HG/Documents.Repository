using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Document.Repository.Models.Entities;
public class Project
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Title Name is required.")]
    public required string Title { get; set; }
    public DateTime CreatedDate { get; set; }
    public ProjectStatus Status { get; set; }

    [Required(ErrorMessage = "Abstract is required.")]
    public required string Abstract { get; set; }
    public string? Thumbnail { get; set; }
    [NotMapped]
    public IFormFile? ThumbnailFile { get; set; }
    public required int StudentId { get; set; }
    public Student? Student { get; set; }
    public List<Document>? Documents { get; set; }
    public List<Tag>? Tags { get; set; }
}