using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Trace.Api.Services.TestProgramCorrelation;
using Trace.Api.Services.TestProgramCorrelation.ItuffData;
using Trace.Api.Services.TestResults.TestTime;
using Trace.Api.Common.Ituff;
using Trace.Api.Services.TestResults;
using Trace.Api.Common.BinSwitch;
using CommunityToolkit.HighPerformance.Helpers;

namespace DataExtractApp
{
    /// <summary>
    /// Provides methods to extract and analyze test time data.
    /// </summary>
    public class TestTimeDataProvider
    {
        private readonly ITestTimeDataCreator _testTimeDataCreator;
        private readonly ITestTimeRawDataAnalyzer _testTimeAnalyzer;

        public TestTimeDataProvider(
            ITestTimeDataCreator testTimeDataCreator = null,
            ITestTimeRawDataAnalyzer testTimeAnalyzer = null)
        {
            var testInstanceToScrum = new TpItemToScrumService();
            var nameExtractor = new RawItuffDataNamesExtractor();

            _testTimeAnalyzer = testTimeAnalyzer ?? new TestTimeRawDataAnalyzer(testInstanceToScrum, nameExtractor);
            _testTimeDataCreator = testTimeDataCreator ?? new TestTimeDataCreator();
        }

        /// <summary>
        /// Retrieves and analyzes test time data.
        /// </summary>
        /// <param name="session">The test session.</param>
        /// <param name="ituff">The Ituff definition.</param>
        /// <returns>A collection of analyzed Ituff data.</returns>
        /// <exception cref="ArgumentNullException">Thrown if session or ituff is null.</exception>
        public IEnumerable<AnalyzedItuffDataContainer> GetData(Session session, ClassItuffDefinition ituff)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (ituff == null) throw new ArgumentNullException(nameof(ituff));

            var testTimeData = _testTimeDataCreator.CalculateTestTime(session, null, CancellationToken.None).FirstOrDefault();
            return AnalyzeData(session, ituff, testTimeData);
        }

        /// <summary>
        /// Retrieves and analyzes test time data filtered by a list of units.
        /// </summary>
        /// <param name="session">The test session.</param>
        /// <param name="ituff">The Ituff definition.</param>
        /// <param name="unitsList">The list of units to filter by.</param>
        /// <returns>A collection of analyzed Ituff data.</returns>
        /// <exception cref="ArgumentNullException">Thrown if session or ituff is null.</exception>
        /// <exception cref="ArgumentException">Thrown if unitsList is null or empty.</exception>
        public IEnumerable<AnalyzedItuffDataContainer> GetData(Session session, ClassItuffDefinition ituff, List<string> unitsList)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (ituff == null) throw new ArgumentNullException(nameof(ituff));
            if (unitsList == null || !unitsList.Any()) throw new ArgumentException("Units list must not be empty.", nameof(unitsList));

            var testTimeData = _testTimeDataCreator.CalculateTestTime(session, null, CancellationToken.None).FirstOrDefault();
            var filteredData = FilterDataByUnits(testTimeData, unitsList);
            return AnalyzeData(session, ituff, filteredData);
        }

        /// <summary>
        /// Calculates the average test time.
        /// </summary>
        /// <returns>The average test time as a string.</returns>
        public string CalculateAvrg()
        {
            // Implement the average calculation logic here.
            return "";
        }

        private ItuffContentRawData FilterDataByUnits(ItuffContentRawData testTimeData, List<string> unitsList)
        {
            var filteredUnitsData = testTimeData.UnitsExtraData
                .Where(unitData => unitsList.Contains(unitData.UnitId))
                .ToList();

            var filteredUnitExtraData = new UnitExtraDataKeyedCollection();
            foreach (var unit in filteredUnitsData)
            {
                filteredUnitExtraData.Add(unit);
            }

            var instanceRawData = new List<TestInstanceRawData>();
            foreach (var instanceTestTime in testTimeData.TestInstancesRawData)
            {
                instanceRawData.Add(new TestInstanceRawData
                {
                    TestInstanceName = instanceTestTime.TestInstanceName,
                    UnitsResult = instanceTestTime.UnitsResult
                        .Where(data => unitsList.Contains(data.UnitId))
                        .ToList(),
                });
            }

            return new ItuffContentRawData
            {
                LotNumber = testTimeData.LotNumber,
                ProcessStep = testTimeData.ProcessStep,
                TestInstancesRawData = instanceRawData,
                UnitsExtraData = filteredUnitExtraData
            };
        }

        private IEnumerable<AnalyzedItuffDataContainer> AnalyzeData(Session session, ClassItuffDefinition ituff, ItuffContentRawData testData)
        {
            var sessionData = new SessionItuffRawData(ituff.BomGroup);
            sessionData.NewItuffRawData.Add(testData.ProcessStep, new List<ItuffContentRawData> { testData });
            return _testTimeAnalyzer.Analyze(new RawDataPerBomCollection { sessionData }, null, EnumUnitsFilter.OnlyPassed, false, true, CancellationToken.None);
        }
    }
}
