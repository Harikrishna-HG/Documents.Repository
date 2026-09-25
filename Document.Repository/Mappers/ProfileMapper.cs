using Document.Repository.Models.Entities;
using Document.Repository.ViewModels;

namespace Document.Repository.Mappers;

public static class ProfileMapper
{
    public static ProfileViewModel ToProfileViewModel(this Student student)
    {
        if ( student == null)
        {
            return null;
        }
        return new ProfileViewModel
        {
            Id = student.UserId,
            StudentId = student.Id,
            Email = student.User?.Email,
            Name = student.Name,
            ProfilePic = student.ProfilePic,
            Semester = student.Semester,
            Programme = student.Programme?.Name,
            College = student.Programme?.Department?.College?.Name,
            Projects = [.. student.Projects.Select(p => new ProjectViewModel
            {
                Id = p.Id,
                Title = p.Title,
                Abstract = p.Abstract,
                Thumbnail = p.Thumbnail,
                Status = p.Status
            })]
        };
    }
}
