namespace Document.Repository.ViewModels;
public class RoleAssignmentViewModel
{
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public List<string>? AvailableRoles { get; set; }
    public List<string>? SelectedRoles { get; set; }
}