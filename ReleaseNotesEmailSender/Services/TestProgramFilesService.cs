using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.Extensions.Configuration;
using ReleaseNotesEmailSender.Helpers;
using ReleaseNotesEmailSender.Models;

namespace ReleaseNotesEmailSender.Services
{
    public class TestProgramFilesService
    {
        public List<TestProgramFiles> TestProgramFiles { get; }

        private readonly string _envFile = "Shared\\BaseInputs\\EnvironmentFile_Common.env";
        private readonly IConfiguration _configuration;
        private readonly string _tpBasePath;

        public TestProgramFilesService(IConfiguration configuration)
        {
            _configuration = configuration;
            _tpBasePath = configuration["ReleaseNotes:TPPath"];
            TestProgramFiles = ParseTestProgramFiles();
        }

        private string FindSocFile(string sharedPath, string subDirName)
        {
            string socFilePath = null;

            if (subDirName.IndexOf("FACT", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                socFilePath = Directory.EnumerateFiles(sharedPath, "*FACT*.soc", SearchOption.TopDirectoryOnly).FirstOrDefault();
            }
            else
            {
                socFilePath = Directory.EnumerateFiles(sharedPath, "*HDMT*.soc", SearchOption.TopDirectoryOnly).FirstOrDefault();
            }

            return socFilePath ?? string.Empty;
        }

        private List<TestProgramFiles> ParseTestProgramFiles()
        {
            string sharedPath = Path.Combine(_tpBasePath, "Shared", "BaseInputs");
            var testPrograms = new List<TestProgramFiles>();
            string porTpPath = Path.Combine(_tpBasePath, "POR_TP");
            var subDirectories = Directory.GetDirectories(porTpPath);

            foreach (var subDir in subDirectories)
            {
                var subDirName = Path.GetFileName(subDir);
                var testProgramFiles = new TestProgramFiles
                {
                    TpType = subDir.GetDirName(),
                    Tpl = Path.Combine(_tpBasePath, "BaseTestPlan.tpl"),
                    Stpl = Path.Combine(subDir, "SubTestPlan.stpl"),
                    SocFile = FindSocFile(sharedPath, subDirName),
                    EnvFile = Path.Combine(subDir, "EnvironmentFile.env"),
                    PlistFile = Path.Combine(subDir, "PLIST_ALL_CLASS.plist.xml"),
                };

                testPrograms.Add(testProgramFiles);
            }

            return testPrograms;
        }

        public List<string> GetBOMGroups(string binMatrixUsrvPath)
        {
            var bomsList = new List<string>();

            using (XmlTextReader reader = new XmlTextReader(binMatrixUsrvPath))
            {
                reader.WhitespaceHandling = WhitespaceHandling.None;
                bool readingBOMs = false;

                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.Name == "BOM")
                    {
                        readingBOMs = true;
                    }
                    else if (reader.NodeType == XmlNodeType.Text && readingBOMs)
                    {
                        bomsList.Add(reader.Value);
                    }
                    else if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "BOM")
                    {
                        readingBOMs = false;
                    }
                }
            }

            return bomsList;
        }

        private Dictionary<string, string> ParseEnvFile(string filePath)
        {
            var variables = new Dictionary<string, string>();
            var variablePattern = new Regex(@"^(\w+)\s*=\s*(.*)$");

            foreach (var line in File.ReadLines(filePath))
            {
                var match = variablePattern.Match(line);
                if (match.Success)
                {
                    string variableName = match.Groups[1].Value;
                    string variableValue = match.Groups[2].Value.TrimEnd(';');
                    variables[variableName] = variableValue;
                }
            }

            return variables;
        }

        public TestProgramSummary GetTestProgramSummary()
        {
            var envVarsPath = Path.Combine(_tpBasePath, _envFile);
            var envVars = ParseEnvFile(envVarsPath);
            var tpName = _tpBasePath.GetDirName();

            var testProgramSummary = new TestProgramSummary
            {
                TpName = tpName,
                ReleaseSites = _configuration["ReleaseNotes:ReleaseSites"],
                TestProgramPath = _tpBasePath,
                TestProgramShortName = tpName.GetTagFromTpName(),
                FusePath = envVars.TryGetValue("FUSE_ROOT_DIR", out string fusePath) ? fusePath : string.Empty,
                PrimeVersion = envVars["PRIME_BASE"],
                TosVersion = envVars["TP_TOS"],
            };

            return testProgramSummary;
        }
    }
}
