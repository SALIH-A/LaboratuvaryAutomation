// ============================================================================
// INDEPENDENT LABORATORY DATA MANAGEMENT AND AUTOMATION SYSTEM (LDMAS)
// DataAnalyzer.cs — Experimental Data Processing and Mining
// ============================================================================
// Author          : Salih
// Date            : 2026-08-10
// Layer           : Analytics / Business Logic Layer (BLL)
// Pattern         : LINQ-based Data Processing Service
// Requirement Ref : FR-033 (data analysis), FR-034 (anomaly detection)
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace LDMAS.Analytics
{
    /// <summary>
    /// Represents a simplified test result for data analysis purposes.
    /// Maps to the TestResults table schema.
    /// </summary>
    public class TestResultData
    {
        public int ResultId { get; set; }
        public int SampleId { get; set; }
        public string ParameterName { get; set; } = string.Empty;
        public decimal MeasuredValue { get; set; }
        public string Unit { get; set; } = string.Empty;
        public DateTime RecordedAt { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Represents statistical summary data for a specific parameter.
    /// </summary>
    public class ParameterStatistics
    {
        public string ParameterName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public int DataPointCount { get; set; }
        public decimal Average { get; set; }
        public decimal Min { get; set; }
        public decimal Max { get; set; }
        public double StandardDeviation { get; set; }
    }

    /// <summary>
    /// Service for processing and analyzing experimental test results.
    /// Utilizes LINQ for efficient in-memory data querying and statistical calculations.
    /// </summary>
    public class DataAnalyzer
    {
        /// <summary>
        /// Calculates the average (mean) value for a specific parameter within a given date range.
        /// </summary>
        /// <param name="data">The dataset to analyze.</param>
        /// <param name="parameterName">The name of the parameter (e.g., "pH", "Conductivity").</param>
        /// <param name="startDate">The start of the date range (inclusive).</param>
        /// <param name="endDate">The end of the date range (inclusive).</param>
        /// <returns>The average value, or null if no data points fall within the criteria.</returns>
        public decimal? CalculateAverageForDateRange(
            IEnumerable<TestResultData> data, 
            string parameterName, 
            DateTime startDate, 
            DateTime endDate)
        {
            if (data == null || !data.Any()) return null;

            var filteredData = data
                .Where(r => r.ParameterName.Equals(parameterName, StringComparison.OrdinalIgnoreCase))
                .Where(r => r.RecordedAt.Date >= startDate.Date && r.RecordedAt.Date <= endDate.Date)
                .Select(r => r.MeasuredValue)
                .ToList();

            if (!filteredData.Any()) return null;

            return filteredData.Average();
        }

        /// <summary>
        /// Generates comprehensive statistical summaries for all parameters in the provided dataset.
        /// </summary>
        /// <param name="data">The dataset to analyze.</param>
        /// <returns>A list of statistics grouped by parameter.</returns>
        public List<ParameterStatistics> GenerateParameterStatistics(IEnumerable<TestResultData> data)
        {
            if (data == null || !data.Any()) return new List<ParameterStatistics>();

            var statisticsList = new List<ParameterStatistics>();

            // Group by parameter name and unit to calculate stats per group
            var groupedData = data.GroupBy(r => new { r.ParameterName, r.Unit });

            foreach (var group in groupedData)
            {
                var values = group.Select(r => (double)r.MeasuredValue).ToList();
                int count = values.Count;

                if (count == 0) continue;

                double avg = values.Average();
                double sumOfSquaresOfDifferences = values.Select(val => (val - avg) * (val - avg)).Sum();
                
                // Calculate sample standard deviation
                double sd = count > 1 ? Math.Sqrt(sumOfSquaresOfDifferences / (count - 1)) : 0;

                statisticsList.Add(new ParameterStatistics
                {
                    ParameterName = group.Key.ParameterName,
                    Unit = group.Key.Unit,
                    DataPointCount = count,
                    Average = (decimal)avg,
                    Min = (decimal)values.Min(),
                    Max = (decimal)values.Max(),
                    StandardDeviation = sd
                });
            }

            return statisticsList.OrderBy(s => s.ParameterName).ToList();
        }

        /// <summary>
        /// Identifies anomalous results in a dataset based on a standard deviation threshold.
        /// A result is considered an anomaly if its value is further from the mean than the 
        /// allowed number of standard deviations.
        /// </summary>
        /// <param name="data">The dataset to analyze.</param>
        /// <param name="parameterName">The specific parameter to check.</param>
        /// <param name="stdDevThreshold">The number of standard deviations to use as the threshold (e.g., 2.0 or 3.0).</param>
        /// <returns>A list of test results classified as anomalies.</returns>
        public List<TestResultData> FindAnomalies(
            IEnumerable<TestResultData> data, 
            string parameterName, 
            double stdDevThreshold = 2.0)
        {
            if (data == null || !data.Any()) return new List<TestResultData>();

            var parameterData = data
                .Where(r => r.ParameterName.Equals(parameterName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (parameterData.Count < 2) return new List<TestResultData>(); // Cannot calculate SD with < 2 points

            var values = parameterData.Select(r => (double)r.MeasuredValue).ToList();
            double avg = values.Average();
            
            double sumOfSquares = values.Select(val => Math.Pow(val - avg, 2)).Sum();
            double stdDev = Math.Sqrt(sumOfSquares / (values.Count - 1));

            // If standard deviation is 0, all values are identical, thus no anomalies
            if (stdDev == 0) return new List<TestResultData>();

            double lowerBound = avg - (stdDevThreshold * stdDev);
            double upperBound = avg + (stdDevThreshold * stdDev);

            // Use LINQ to filter out the anomalies
            var anomalies = parameterData
                .Where(r => (double)r.MeasuredValue < lowerBound || (double)r.MeasuredValue > upperBound)
                .OrderByDescending(r => Math.Abs((double)r.MeasuredValue - avg)) // Order by severity
                .ToList();

            return anomalies;
        }

        /// <summary>
        /// Identifies trends by calculating the moving average over a specified window size.
        /// </summary>
        /// <param name="data">The dataset (must be time-series for a single parameter).</param>
        /// <param name="parameterName">The parameter to calculate the trend for.</param>
        /// <param name="windowSize">The number of data points to include in the moving average.</param>
        /// <returns>A dictionary mapping the recording date to its calculated moving average.</returns>
        public Dictionary<DateTime, decimal> CalculateMovingAverage(
            IEnumerable<TestResultData> data, 
            string parameterName, 
            int windowSize = 3)
        {
            var result = new Dictionary<DateTime, decimal>();

            if (data == null || windowSize <= 0) return result;

            var orderedData = data
                .Where(r => r.ParameterName.Equals(parameterName, StringComparison.OrdinalIgnoreCase))
                .OrderBy(r => r.RecordedAt)
                .ToList();

            if (orderedData.Count < windowSize) return result;

            for (int i = windowSize - 1; i < orderedData.Count; i++)
            {
                var windowValues = orderedData
                    .Skip(i - windowSize + 1)
                    .Take(windowSize)
                    .Select(r => r.MeasuredValue);

                result.Add(orderedData[i].RecordedAt, windowValues.Average());
            }

            return result;
        }
    }
}
