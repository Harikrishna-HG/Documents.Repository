using Microsoft.AspNetCore.Identity.UI.Services;

namespace Document.Repository.Services;

public class NoOpEmailSender : IEmailSender
{
    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        // For development, log email to the console or just ignore.
        Console.WriteLine($"Email sent to {email} with subject: {subject}");
        return Task.CompletedTask;
    }
}
