using System;
using System.IO;
using System.Text;
using ReleaseNotesEmailSender.Models;
using ReleaseNotesEmailSender.Services;

namespace ReleaseNotesEmailSender
{
    public class EmailComposer
    {
        private const string TemplateLocation = "ReleaseEmailTemplate.html";
        private const string SharedDir = "Shared";
        private readonly TestProgramFilesService _testProgramService;
        private readonly TestProgramMetricsService _testProgramMetricsService;

        public EmailComposer(TestProgramFilesService testProgramFilesService)
        {
            _testProgramService = testProgramFilesService;
            _testProgramMetricsService = new TestProgramMetricsService();

        }

        public async Task<string> ComposeEmailBody()
        {
            var tpSummary = _testProgramService.GetTestProgramSummary();
            var testProgramMetrics = await _testProgramMetricsService.GetTestProgramMetrics(tpSummary.TpName);
            // Read and process the email template
            var emailContent = File.ReadAllText(TemplateLocation);
            var objectiveTable = GenerateObjectiveTable(tpSummary);
            var summaryTable = GenerateSummaryTable(tpSummary);
            var tpFilesTable = GenerateTpFilesTable();
            var bomsTable = GenerateBomsTable(tpSummary.TestProgramPath);
            var testTimeTable = GenerateTestTimeTable(testProgramMetrics);
            // Replace placeholders with generated content
            emailContent = emailContent
                .Replace(Constants.ObjectiveTablePlaceholder, objectiveTable)
                .Replace(Constants.TpFilesTablePlaceholder, tpFilesTable)
                .Replace(Constants.BomsTablePlaceholder, bomsTable)
                .Replace(Constants.TpNamePlaceholder, tpSummary.TpName)
                .Replace(Constants.SummaryTablePlaceholder, summaryTable)
                .Replace(Constants.TestTimeTablePlaceHolder, testTimeTable);

            return emailContent;
        }

        private string GenerateTestTimeTable(TestProgramMetrics tpMetrics)
        {
            var tableHtml = new StringBuilder();
            tableHtml.Append("<table>");
            void AddRow(string fieldName, string value)
            {
                tableHtml.AppendFormat("<tr><td>{0}</td><td>{1}</td></tr>", fieldName, value ?? string.Empty);
            }

            AddRow("Lean Flow TTG", tpMetrics.LeanTestTime.ToString("F3"));
            AddRow("First Flow TTG", tpMetrics.FirstFLowTestTime.ToString("F3"));
            AddRow("TTG", tpMetrics.Testime.ToString("F3"));
            tableHtml.Append("</table>");
            return tableHtml.ToString();
        }

        private string GenerateSummaryTable(TestProgramSummary tpSummary)
        {
            var tableHtml = new StringBuilder();
            tableHtml.Append("<table>");

            void AddRow(string fieldName, string value)
            {
                tableHtml.AppendFormat("<tr><td>{0}</td><td>{1}</td></tr>", fieldName, value ?? string.Empty);
            }

            AddRow("Test Program Name", tpSummary.TpName);
            AddRow("Release Sites", tpSummary.ReleaseSites);
            AddRow("Test Program Short Name", tpSummary.TestProgramShortName);
            AddRow("Product Subfamily", tpSummary.ProductSubfamily);
            AddRow("Test Program Integrator", tpSummary.TestProgramIntegrator);
            AddRow("Test Program Integrator Email", tpSummary.TestProgramIntegratorEmail);
            AddRow("Test Program Execution Product Manager", tpSummary.TestProgramExecutionProductManager);
            AddRow("Test Program Path", tpSummary.TestProgramPath);
            AddRow("Fuse Path", tpSummary.FusePath);

            tableHtml.Append("</table>");
            return tableHtml.ToString();
        }

        private string GenerateBomsTable(string tpBasePath)
        {
            var files = GetXmlFilesContainingMatrix(Path.Combine(tpBasePath, SharedDir)).ToList();
            var bomsList = _testProgramService.GetBOMGroups(files.FirstOrDefault());

            int rowsPerColumn = 20;
            int columnCount = (int)Math.Ceiling((double)bomsList.Count / rowsPerColumn);

            var tableHtml = new StringBuilder();
            tableHtml.AppendFormat("<table><th colspan='{0}' >Devices</th>", columnCount);

            for (int rowIndex = 0; rowIndex < rowsPerColumn; rowIndex++)
            {
                tableHtml.Append("<tr>");
                for (int colIndex = 0; colIndex < columnCount; colIndex++)
                {
                    int itemIndex = (colIndex * rowsPerColumn) + rowIndex;
                    if (itemIndex < bomsList.Count)
                    {
                        tableHtml.AppendFormat("<td>{0}</td>", bomsList[itemIndex]);
                    }
                    else
                    {
                        tableHtml.Append("<td></td>"); // Empty cell if no more items
                    }
                }

                tableHtml.Append("</tr>");
            }

            tableHtml.Append("</table>");
            return tableHtml.ToString();
        }

        private string GenerateObjectiveTable(TestProgramSummary tpSummary)
        {
            var tableHtml = new StringBuilder();
            tableHtml.Append("<table border='1'>");
            tableHtml.Append("<tr><th>Test Program Objective/Special Notes:</th></tr>");
            tableHtml.Append("<tr><td><ul>");
            tableHtml.AppendFormat("<li class=\"bold\"> TOS Version: {0}</li>", tpSummary.TosVersion);
            tableHtml.AppendFormat("<li class=\"bold\" >Prime Version: {0}</li>", tpSummary.PrimeVersion);
            tableHtml.Append("</ul></td></tr>");
            tableHtml.Append("</table>");
            return tableHtml.ToString();
        }

        private string GenerateTpFilesTable()
        {
            var tpFilesList = _testProgramService.TestProgramFiles;
            var headers = tpFilesList.Select(files => files.TpType).ToList();
            var tableHtml = new StringBuilder();
            tableHtml.Append("<table>");
            // Add table headers
            tableHtml.Append("<tr><th>File</th>");
            foreach (var header in headers)
            {
                tableHtml.AppendFormat("<th>{0}</th>", header);
            }
            tableHtml.Append("</tr>");

            // Add rows for each file type
            AddTableRow(tableHtml, "Test Plan File", tpFilesList.Select(tp => tp.Tpl).ToArray());
            AddTableRow(tableHtml, "SubTestPlan File", tpFilesList.Select(tp => tp.Stpl).ToArray());
            AddTableRow(tableHtml, "SOC File", tpFilesList.Select(tp => tp.SocFile).ToArray());
            AddTableRow(tableHtml, "Env File", tpFilesList.Select(tp => tp.EnvFile).ToArray());
            AddTableRow(tableHtml, "PList File", tpFilesList.Select(tp => tp.PlistFile).ToArray());

            tableHtml.Append("</table>");
            return tableHtml.ToString();
        }

        private void AddTableRow(StringBuilder tableHtml, string rowHeader, string[] paths)
        {
            tableHtml.Append("<tr>");
            tableHtml.AppendFormat("<td class=\"rowHeader\">{0}</td>", rowHeader);
            foreach (var path in paths)
            {
                tableHtml.AppendFormat("<td>{0}</td>", path);
            }
            tableHtml.Append("</tr>");
        }

        public static IEnumerable<string> GetXmlFilesContainingMatrix(string directory)
        {
            var files = new List<string>();
            var dirs = new Stack<string>(new[] { directory });

            while (dirs.Count > 0)
            {
                string currentDir = dirs.Pop();
                try
                {
                    // Add files in the current directory that match the pattern
                    files.AddRange(Directory.EnumerateFiles(currentDir, "*matrix*.xml", SearchOption.TopDirectoryOnly));

                    // Add subdirectories to the stack for processing
                    foreach (var subDir in Directory.EnumerateDirectories(currentDir))
                    {
                        dirs.Push(subDir);
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    Console.WriteLine($"Access denied to {currentDir}");
                }
                catch (DirectoryNotFoundException)
                {
                    Console.WriteLine($"Directory not found: {currentDir}");
                }
            }

            return files;
        }
    }
}
