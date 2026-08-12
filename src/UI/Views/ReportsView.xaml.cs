// ============================================================================
// INDEPENDENT LABORATORY DATA MANAGEMENT AND AUTOMATION SYSTEM (LDMAS)
// ReportsView.xaml.cs — Code-Behind for the Reports & Data Export Module
// ============================================================================
// Author          : Salih
// Date            : 2026-08-12
// Layer           : Presentation Layer
// Requirement Ref : FR-033 (data export), FR-034 (anomaly reports)
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using LDMAS.Analytics;
using LDMAS.Utils;

namespace LDMAS.UI.Views
{
    /// <summary>
    /// Code-behind for the Reports &amp; Data Export module.
    /// Integrates <see cref="DataAnalyzer"/> for statistical processing and
    /// <see cref="ExportUtility"/> for CSV generation.
    /// </summary>
    public partial class ReportsView : UserControl
    {
        private readonly DataAnalyzer _analyzer;

        public ReportsView()
        {
            InitializeComponent();
            _analyzer = new DataAnalyzer();

            // Set sensible default dates
            DpStartDate.SelectedDate = DateTime.Now.AddMonths(-1);
            DpEndDate.SelectedDate = DateTime.Now;

            // Load sample preview data on initial render
            LoadSamplePreviewData();
        }

        // =====================================================================
        // EVENT HANDLERS
        // =====================================================================

        /// <summary>
        /// Handles the Generate CSV button click.
        /// Validates date range, processes data through the DataAnalyzer,
        /// and exports results via ExportUtility.
        /// </summary>
        private void BtnGenerateCsv_Click(object sender, RoutedEventArgs e)
        {
            // ── Validate date range ──
            if (DpStartDate.SelectedDate == null || DpEndDate.SelectedDate == null)
            {
                MessageBox.Show(
                    "Please select both a Start Date and an End Date.",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (DpStartDate.SelectedDate > DpEndDate.SelectedDate)
            {
                MessageBox.Show(
                    "Start Date cannot be later than End Date.",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // ── Determine report type ──
            string reportType = (CmbReportType.SelectedItem as ComboBoxItem)?.Content?.ToString()
                                ?? "Full Experiment Export";

            // ── Open Save File Dialog ──
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv",
                DefaultExt = ".csv",
                FileName = $"LDMAS_Report_{DateTime.Now:yyyyMMdd_HHmmss}"
            };

            if (dialog.ShowDialog() == true)
            {
                // In production, this would query the database and pass real data.
                // For now, we export the sample preview data.
                var sampleData = GetSampleExportData();
                bool success = ExportUtility.ExportToCsv(sampleData, dialog.FileName);

                if (success)
                {
                    MessageBox.Show(
                        $"Report exported successfully!\n\nType: {reportType}\nFile: {dialog.FileName}\nRecords: {sampleData.Count}",
                        "Export Complete",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(
                        "An error occurred while exporting the report. Check the console output for details.",
                        "Export Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        // =====================================================================
        // DATA LOADING
        // =====================================================================

        /// <summary>
        /// Loads sample data into the preview DataGrid for demonstration.
        /// In production, this would query TestResults via the DAL.
        /// </summary>
        private void LoadSamplePreviewData()
        {
            var previewData = new List<object>
            {
                new { ParameterName = "pH",             MeasuredValue = "7.12",   Unit = "pH units",  Status = "✅ Normal",  RecordedAt = "2026-08-01 09:15", Notes = "" },
                new { ParameterName = "pH",             MeasuredValue = "11.50",  Unit = "pH units",  Status = "⚠️ Anomaly", RecordedAt = "2026-08-06 14:22", Notes = "Sensor recalibrated" },
                new { ParameterName = "Conductivity",   MeasuredValue = "485.30", Unit = "µS/cm",     Status = "✅ Normal",  RecordedAt = "2026-08-02 10:00", Notes = "" },
                new { ParameterName = "Conductivity",   MeasuredValue = "1520.0", Unit = "µS/cm",     Status = "⚠️ Anomaly", RecordedAt = "2026-08-05 16:45", Notes = "Sample contamination suspected" },
                new { ParameterName = "Turbidity",      MeasuredValue = "2.10",   Unit = "NTU",       Status = "✅ Normal",  RecordedAt = "2026-08-03 08:30", Notes = "" },
                new { ParameterName = "Dissolved O₂",   MeasuredValue = "8.45",   Unit = "mg/L",      Status = "✅ Normal",  RecordedAt = "2026-08-04 11:10", Notes = "" },
                new { ParameterName = "Temperature",    MeasuredValue = "22.3",   Unit = "°C",        Status = "✅ Normal",  RecordedAt = "2026-08-07 09:00", Notes = "" },
                new { ParameterName = "Lead (Pb)",      MeasuredValue = "0.082",  Unit = "mg/L",      Status = "🔴 Fail",   RecordedAt = "2026-08-08 13:55", Notes = "Exceeds WHO limit (0.01 mg/L)" },
            };

            DgReportPreview.ItemsSource = previewData;
            TxtRecordCount.Text = $"{previewData.Count} records";
        }

        /// <summary>
        /// Returns typed sample data suitable for CSV export via ExportUtility.
        /// </summary>
        private List<ReportExportRow> GetSampleExportData()
        {
            return new List<ReportExportRow>
            {
                new() { ParameterName = "pH",           MeasuredValue = 7.12m,    Unit = "pH units", Status = "Normal",  RecordedAt = new DateTime(2026, 8, 1, 9, 15, 0),  Notes = "" },
                new() { ParameterName = "pH",           MeasuredValue = 11.50m,   Unit = "pH units", Status = "Anomaly", RecordedAt = new DateTime(2026, 8, 6, 14, 22, 0), Notes = "Sensor recalibrated" },
                new() { ParameterName = "Conductivity", MeasuredValue = 485.30m,  Unit = "µS/cm",    Status = "Normal",  RecordedAt = new DateTime(2026, 8, 2, 10, 0, 0),  Notes = "" },
                new() { ParameterName = "Conductivity", MeasuredValue = 1520.0m,  Unit = "µS/cm",    Status = "Anomaly", RecordedAt = new DateTime(2026, 8, 5, 16, 45, 0), Notes = "Sample contamination suspected" },
                new() { ParameterName = "Turbidity",    MeasuredValue = 2.10m,    Unit = "NTU",      Status = "Normal",  RecordedAt = new DateTime(2026, 8, 3, 8, 30, 0),  Notes = "" },
                new() { ParameterName = "Dissolved O₂", MeasuredValue = 8.45m,    Unit = "mg/L",     Status = "Normal",  RecordedAt = new DateTime(2026, 8, 4, 11, 10, 0), Notes = "" },
                new() { ParameterName = "Temperature",  MeasuredValue = 22.3m,    Unit = "°C",       Status = "Normal",  RecordedAt = new DateTime(2026, 8, 7, 9, 0, 0),   Notes = "" },
                new() { ParameterName = "Lead (Pb)",    MeasuredValue = 0.082m,   Unit = "mg/L",     Status = "Fail",    RecordedAt = new DateTime(2026, 8, 8, 13, 55, 0), Notes = "Exceeds WHO limit (0.01 mg/L)" },
            };
        }
    }

    /// <summary>
    /// Typed DTO for CSV export via the Reflection-based ExportUtility.
    /// </summary>
    public class ReportExportRow
    {
        public string ParameterName { get; set; } = string.Empty;
        public decimal MeasuredValue { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime RecordedAt { get; set; }
        public string Notes { get; set; } = string.Empty;
    }
}
