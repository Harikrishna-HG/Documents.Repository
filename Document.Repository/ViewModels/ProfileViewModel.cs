namespace Document.Repository.ViewModels;

using Document.Repository.Models.Entities;

public class ProfileViewModel
{
    public string Id { get; set; }
    public string UserName { get; set; }
    public int StudentId { get; set; }
    public string Name { get; set; }
    public string ProfilePic { get; set; }
    public string College { get; set; }
    public string Email { get; set; }
    public string Semester { get; set; }
    public string Programme { get; set; }
    public List<ProjectViewModel> Projects { get; set; } = new List<ProjectViewModel>();
}

public class ProjectViewModel
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Abstract { get; set; }
    public string Thumbnail { get; set; }
    public ProjectStatus Status { get; set; }
}


