using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Trace.Api.Common;
using Trace.Api.Common.Ituff;
using Trace.Api.Configuration;
using Trace.Api.Services.BinSwitch;
using Trace.Api.Services.BinSwitch.Interfaces;
using Trace.Api.Services.Cache;
using Trace.Api.Services.TestResults.ItuffIndex;
using Trace.Api.Common.BinSwitch;
using Trace.Api.Common.BinSwitch.FlowDiagram.UnitTrace;
using Trace.Api.Common.BinSwitch.FlowDiagram;
using ReleaseNotesEmailSender.Models;
using Trace.Api.Common.TP;
using DataExtractApp;

namespace ReleaseNotesEmailSender.Services
{
    public class TestProgramMetricsService
    {
        private const string LEAN_FLOW_FORK_NAME = "TESTTOSPECFORK_SRH";
        private readonly IItuffIndexManager _ituffIndexManager;
        private readonly IFileService _fileService;
        private readonly TestTimeDataProvider _testTimeDataProvider;

        public TestProgramMetricsService()
        {
            var driveMapping = ConfigurationLoader.GetDriveMapping(SiteEnum.IDC, SiteDataSourceEnum.CLASSHDMT);
            _fileService = new PassThroughFileService(driveMapping);
            _ituffIndexManager = new ItuffIndexManager(_fileService);
            _testTimeDataProvider = new TestTimeDataProvider();
        }

        public async Task<TestProgramMetrics> GetTestProgramMetrics(string testProgramName)
        {
            var ituffs = CreateItuffDefenition(testProgramName);

            var metricsTasks = ituffs.Select(ituff => ProcessItuffAsync(ituff)).ToArray();

            var results = await Task.WhenAll(metricsTasks);

            var nonZeroLeanTTResults = results.Where(r => r.LeanTT != 0).ToList();
            var leanTTSum = nonZeroLeanTTResults.Sum(r => r.LeanTT);
            var leanTTCount = nonZeroLeanTTResults.Count;

            var nonZeroFirstFlowTTResults = results.Where(r => r.FirstFlowTT != 0).ToList();
            var firstFlowTTSum = nonZeroFirstFlowTTResults.Sum(r => r.FirstFlowTT);
            var firstFlowTTCount = nonZeroFirstFlowTTResults.Count;

            var nonZeroGTTResults = results.Where(r => r.GTT != 0).ToList();
            var GTTSum = nonZeroGTTResults.Sum(r => r.GTT);
            var GTTCount = nonZeroGTTResults.Count;

            return new TestProgramMetrics
            {
                LeanTestTime = leanTTCount > 0 ? leanTTSum / leanTTCount : 0,
                FirstFLowTestTime = firstFlowTTCount > 0 ? firstFlowTTSum / firstFlowTTCount : 0,
                PassinUnitsTestTime = GTTCount > 0 ? GTTSum / GTTCount : 0,
            };
        }

        private List<ClassItuffDefinition> CreateItuffDefenition(string testProgramName)
        {
            var allItuffDef = _ituffIndexManager.GetAllItuffDefinitions().OfType<ClassItuffDefinition>();
            var ituffs = allItuffDef
                .Where(x => x.ProgramName == testProgramName
                            && x.ExperimentType == "Correlation"
                            && x.IsStaging == false
                            && x.CurrentProcessStep == "CLASSHOT"
                            && x.Operation == "6248")
                .ToList();

            return ituffs;
        }

        private async Task<Session> CreateSessionAsync(ClassItuffDefinition ituff)
        {
            await Console.Out.WriteLineAsync("Waiting for session to start");
            var materialSelectionResult = new SessionMaterial(MaterialType.Hdmt, new[] { ituff });
            var sessionCreator = new SessionCreator(_fileService);
            var session = sessionCreator.CreateSession(materialSelectionResult);
            await session.SessionStartup;
            return session;
        }

        private (List<string> leanFlowUnits, List<string> firstFlowUnits) GetLeanFlowUnits(Session session)
        {
            var runtimeToTraceConverter = new RuntimeToTraceConverter();
            var leanFlowUnits = new List<string>();
            var firstFlowUnits = new List<string>();

            foreach (var unit in session.Units)
            {
                var firstUnitTrace = runtimeToTraceConverter.Convert(session.TestProgram, unit);

                foreach (var traceItemNode in firstUnitTrace.GetFlattenTrace())
                {
                    var testProgramItem = traceItemNode.TestProgramItem;
                    if (testProgramItem.Name.Contains(LEAN_FLOW_FORK_NAME))
                    {
                        var portNumber = traceItemNode.PortNumber;
                        if (portNumber == 1)
                        {
                            firstFlowUnits.Add(unit.VisualId);
                        }
                        else
                        {
                            leanFlowUnits.Add(unit.VisualId);
                        }
                    }
                }
            }

            return (leanFlowUnits, firstFlowUnits);
        }

        private async Task<(double LeanTT, double FirstFlowTT, double GTT)> ProcessItuffAsync(ClassItuffDefinition ituff)
        {
            var session = await CreateSessionAsync(ituff);
            var (leanFlowUnits, firstFlowUnits) = GetLeanFlowUnits(session);

            var leanTT = leanFlowUnits.Any() ? _testTimeDataProvider.GetData(session, ituff, leanFlowUnits).FirstOrDefault()?.AverageNew/100 ?? 0 : 0;
            var firstFlowTT = _testTimeDataProvider.GetData(session, ituff, firstFlowUnits).FirstOrDefault()?.AverageNew/100 ?? 0;
            var GTT = _testTimeDataProvider.GetData(session, ituff).FirstOrDefault()?.AverageNew/100 ?? 0;

            return (leanTT, firstFlowTT, GTT);
        }
    }
}
