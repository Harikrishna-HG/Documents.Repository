namespace Document.Repository.Models.Entities;

public class Tag
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required int TagCategoryId { get; set; }
    public TagCategory? TagCategory { get; set; }
}
