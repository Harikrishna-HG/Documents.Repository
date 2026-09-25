namespace Document.Repository.Models.Entities;

public class Document
{
    public int Id { get; set; }
    public required string FileName { get; set; }
    public required string FileType { get; set; }
    public required string FilePath { get; set; }

    public required int ProjectId { get; set; }
    public Project? Project { get; set; }
}