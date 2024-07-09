using System.IO;
using Microsoft.Extensions.Configuration;
//using Newtonsoft.Json.Linq;

namespace ReleaseNotesEmailSender
{
    public class ReleaseNotesRetriever
    {
        private readonly IConfiguration _configuration;

        public ReleaseNotesRetriever(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GetReleaseNotes()
        {
            // var filePath = _configuration["ReleaseNotes:FilePath"];
            return ""; //File.ReadAllText(filePath);
        }
    }
}
