using System.ComponentModel.DataAnnotations;

namespace Document.Repository.Models;

/// <summary>
/// Used by admins to create a student login account and its matching student profile in one step.
/// Unlike StudentsController.Create (student self-registration), the account is created by the
/// admin and owned by the new student, never by the admin performing the submission.
/// </summary>
public class CreateStudentAccountViewModel
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 8)]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    [Compare(nameof(Password), ErrorMessage = "The password and confirmation password do not match.")]
    public string? ConfirmPassword { get; set; }

    [Required(ErrorMessage = "Name is required.")]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Registration number is required.")]
    [Display(Name = "Registration number")]
    public string? RegistrationNumber { get; set; }

    [Required(ErrorMessage = "Semester is required.")]
    [Display(Name = "Semester")]
    public string Semester { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select a program.")]
    [Display(Name = "Program")]
    public int ProgrammeId { get; set; }

    [Display(Name = "Profile picture")]
    public IFormFile? ProfilePicFile { get; set; }
}
