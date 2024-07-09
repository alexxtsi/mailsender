using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using ReleaseNotesEmailSender.Services;

namespace ReleaseNotesEmailSender
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            try
            {
                // Load configuration from appsettings.json and environment variables
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true) // variables for debug
                    .AddEnvironmentVariables(prefix: "RELEASE_EMAIL_")
                    .Build();

                var testProgramService = new TestProgramFilesService(configuration);

                // Compose email
                var composer = new EmailComposer(testProgramService);
                var emailBody = await composer.ComposeEmailBody();

                // Send email
                var sender = new EmailSender(configuration);
                sender.SendEmail(emailBody);

                Console.WriteLine("Email sent successfully.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return 1;
                // Consider logging the error in a real application
            }
        }
    }
}
