namespace Document.Repository.Models.Entities;
public class TagCategory
{
    public int Id { get; set; }
    public required string Label { get; set; }
    public string? Description { get; set; }
    public List<Tag>? Tags { get; set; }
}
