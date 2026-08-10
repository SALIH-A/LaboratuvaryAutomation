// ============================================================================
// INDEPENDENT LABORATORY DATA MANAGEMENT AND AUTOMATION SYSTEM (LDMAS)
// DataAnalyzerTests.cs — Unit Tests for Analytics Module
// ============================================================================
// Author          : Salih
// Date            : 2026-08-10
// Layer           : Test Layer
// Framework       : xUnit
// Requirement Ref : NFR-034 (Testability), FR-033, FR-034
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using LDMAS.Analytics;

namespace LDMAS.UnitTests
{
    /// <summary>
    /// Comprehensive unit tests for the DataAnalyzer BLL service.
    /// Verifies the mathematical accuracy of anomaly detection and averaging algorithms.
    /// </summary>
    public class DataAnalyzerTests
    {
        private readonly DataAnalyzer _analyzer;

        public DataAnalyzerTests()
        {
            // The analyzer has no external dependencies (pure logic), 
            // so we can instantiate it directly without mocking.
            _analyzer = new DataAnalyzer();
        }

        // =====================================================================
        // TEST DATA GENERATION
        // =====================================================================

        private List<TestResultData> GetMockpHData()
        {
            return new List<TestResultData>
            {
                new TestResultData { ResultId = 1, ParameterName = "pH", MeasuredValue = 7.1m, RecordedAt = new DateTime(2026, 8, 1) },
                new TestResultData { ResultId = 2, ParameterName = "pH", MeasuredValue = 7.2m, RecordedAt = new DateTime(2026, 8, 2) },
                new TestResultData { ResultId = 3, ParameterName = "pH", MeasuredValue = 7.15m, RecordedAt = new DateTime(2026, 8, 3) },
                new TestResultData { ResultId = 4, ParameterName = "pH", MeasuredValue = 7.1m, RecordedAt = new DateTime(2026, 8, 4) },
                new TestResultData { ResultId = 5, ParameterName = "pH", MeasuredValue = 7.3m, RecordedAt = new DateTime(2026, 8, 5) },
                
                // Anomaly 1: Extremely high (Contamination or sensor failure)
                new TestResultData { ResultId = 6, ParameterName = "pH", MeasuredValue = 11.5m, RecordedAt = new DateTime(2026, 8, 6) },
                
                new TestResultData { ResultId = 7, ParameterName = "pH", MeasuredValue = 7.2m, RecordedAt = new DateTime(2026, 8, 7) },
                
                // Anomaly 2: Extremely low
                new TestResultData { ResultId = 8, ParameterName = "pH", MeasuredValue = 3.2m, RecordedAt = new DateTime(2026, 8, 8) }
            };
        }

        // =====================================================================
        // TESTS: CalculateAverageForDateRange
        // =====================================================================

        [Fact]
        public void CalculateAverageForDateRange_WithValidData_ReturnsCorrectAverage()
        {
            // Arrange
            var data = GetMockpHData();
            var startDate = new DateTime(2026, 8, 1);
            var endDate = new DateTime(2026, 8, 5); // Includes 5 normal readings

            // Act
            // Expected sum: 7.1 + 7.2 + 7.15 + 7.1 + 7.3 = 35.85
            // Expected avg: 35.85 / 5 = 7.17
            var result = _analyzer.CalculateAverageForDateRange(data, "pH", startDate, endDate);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(7.17m, result.Value);
        }

        [Fact]
        public void CalculateAverageForDateRange_WithNoDataInRange_ReturnsNull()
        {
            // Arrange
            var data = GetMockpHData();
            var startDate = new DateTime(2026, 9, 1); // Future dates
            var endDate = new DateTime(2026, 9, 5);

            // Act
            var result = _analyzer.CalculateAverageForDateRange(data, "pH", startDate, endDate);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void CalculateAverageForDateRange_EmptyDataset_ReturnsNull()
        {
            // Arrange
            var data = new List<TestResultData>();

            // Act
            var result = _analyzer.CalculateAverageForDateRange(data, "pH", DateTime.MinValue, DateTime.MaxValue);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void CalculateAverageForDateRange_WrongParameterName_ReturnsNull()
        {
            // Arrange
            var data = GetMockpHData(); // Only contains "pH"

            // Act
            var result = _analyzer.CalculateAverageForDateRange(data, "Conductivity", DateTime.MinValue, DateTime.MaxValue);

            // Assert
            Assert.Null(result);
        }

        // =====================================================================
        // TESTS: FindAnomalies (Standard Deviation)
        // =====================================================================

        [Fact]
        public void FindAnomalies_WithOutliers_IdentifiesAnomaliesCorrectly()
        {
            // Arrange
            var data = GetMockpHData();
            // Data values: 7.1, 7.2, 7.15, 7.1, 7.3, 11.5, 7.2, 3.2
            // Mean is approx 7.21, SD is approx 2.24
            // 1 SD lower bound = 4.97, upper bound = 9.45
            // The values 3.2 and 11.5 should be flagged as anomalies at 1 SD.

            // Act
            var anomalies = _analyzer.FindAnomalies(data, "pH", stdDevThreshold: 1.0);

            // Assert
            Assert.Equal(2, anomalies.Count);
            
            // Should catch the high anomaly
            Assert.Contains(anomalies, a => a.ResultId == 6 && a.MeasuredValue == 11.5m);
            // Should catch the low anomaly
            Assert.Contains(anomalies, a => a.ResultId == 8 && a.MeasuredValue == 3.2m);
        }

        [Fact]
        public void FindAnomalies_WithHighThreshold_ReturnsNoAnomalies()
        {
            // Arrange
            var data = GetMockpHData();

            // Act
            // Using a threshold of 5 standard deviations should theoretically return nothing 
            // for this small dataset.
            var anomalies = _analyzer.FindAnomalies(data, "pH", stdDevThreshold: 5.0);

            // Assert
            Assert.Empty(anomalies);
        }

        [Fact]
        public void FindAnomalies_WithIdenticalValues_ReturnsNoAnomalies()
        {
            // Arrange (Edge Case: Standard Deviation is 0)
            var uniformData = new List<TestResultData>
            {
                new TestResultData { ResultId = 1, ParameterName = "pH", MeasuredValue = 7.0m },
                new TestResultData { ResultId = 2, ParameterName = "pH", MeasuredValue = 7.0m },
                new TestResultData { ResultId = 3, ParameterName = "pH", MeasuredValue = 7.0m }
            };

            // Act
            var anomalies = _analyzer.FindAnomalies(uniformData, "pH", stdDevThreshold: 2.0);

            // Assert
            Assert.Empty(anomalies);
        }

        [Fact]
        public void FindAnomalies_WithSingleDataPoint_ReturnsEmptyList()
        {
            // Arrange (Edge Case: Cannot calculate Standard Deviation for N < 2)
            var singleData = new List<TestResultData>
            {
                new TestResultData { ResultId = 1, ParameterName = "pH", MeasuredValue = 7.0m }
            };

            // Act
            var anomalies = _analyzer.FindAnomalies(singleData, "pH", stdDevThreshold: 2.0);

            // Assert
            Assert.Empty(anomalies);
        }
    }
}
