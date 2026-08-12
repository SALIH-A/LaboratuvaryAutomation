// ============================================================================
// INDEPENDENT LABORATORY DATA MANAGEMENT AND AUTOMATION SYSTEM (LDMAS)
// ExperimentsView.xaml.cs — Code-Behind for the Experiments Module
// ============================================================================
// Author          : Salih
// Date            : 2026-08-12
// Layer           : Presentation Layer
// Requirement Ref : FR-010 (experiments), FR-012 (samples), FR-015 (workflow)
// ============================================================================

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace LDMAS.UI.Views
{
    /// <summary>
    /// Code-behind for the Experiments &amp; Test Results module.
    /// Manages the experiments DataGrid and the Add New Experiment form.
    /// </summary>
    public partial class ExperimentsView : UserControl
    {
        /// <summary>
        /// Observable collection backing the experiments DataGrid.
        /// New items submitted via the form are added here in real-time.
        /// </summary>
        private readonly ObservableCollection<ExperimentRow> _experiments;

        public ExperimentsView()
        {
            InitializeComponent();

            _experiments = new ObservableCollection<ExperimentRow>();
            DgExperiments.ItemsSource = _experiments;

            // Set default start date
            DpExpStartDate.SelectedDate = DateTime.Now;

            // Load sample data
            LoadSampleExperimentData();
        }

        // =====================================================================
        // EVENT HANDLERS
        // =====================================================================

        /// <summary>
        /// Validates and submits a new experiment from the form fields.
        /// In production, this would call the BLL to insert into the database.
        /// </summary>
        private void BtnSubmitExperiment_Click(object sender, RoutedEventArgs e)
        {
            // ── Validation ──
            string title = TxtExpTitle.Text.Trim();
            if (string.IsNullOrEmpty(title))
            {
                TxtFormStatus.Foreground = FindResource("DangerBrush") as System.Windows.Media.Brush;
                TxtFormStatus.Text = "⚠ Experiment title is required.";
                return;
            }

            if (DpExpStartDate.SelectedDate == null)
            {
                TxtFormStatus.Foreground = FindResource("DangerBrush") as System.Windows.Media.Brush;
                TxtFormStatus.Text = "⚠ Start date is required.";
                return;
            }

            // ── Build the new row ──
            string category = (CmbExpCategory.SelectedItem as ComboBoxItem)?.Content?.ToString()
                              ?? "Chemistry";

            var newExperiment = new ExperimentRow
            {
                Title      = title,
                Category   = category,
                Status     = "📝 Draft",
                Technician = "Current User",   // In production: read from session
                StartDate  = DpExpStartDate.SelectedDate.Value.ToString("yyyy-MM-dd")
            };

            // ── Insert at top of DataGrid ──
            _experiments.Insert(0, newExperiment);
            TxtExperimentCount.Text = $"{_experiments.Count} records";

            // ── Clear form and show success ──
            TxtExpTitle.Clear();
            TxtExpDescription.Clear();
            DpExpStartDate.SelectedDate = DateTime.Now;
            CmbExpCategory.SelectedIndex = 0;

            TxtFormStatus.Foreground = FindResource("SuccessBrush") as System.Windows.Media.Brush;
            TxtFormStatus.Text = $"✅ \"{title}\" submitted successfully.";

            System.Diagnostics.Debug.WriteLine($"[ExperimentsView] New experiment created: {title}");
        }

        // =====================================================================
        // DATA LOADING
        // =====================================================================

        /// <summary>
        /// Loads sample experiment data for demonstration purposes.
        /// In production, this queries the Experiments table via the DAL.
        /// </summary>
        private void LoadSampleExperimentData()
        {
            var sampleData = new List<ExperimentRow>
            {
                new() { Title = "pH Analysis — Batch 2026-Q3",             Category = "Chemistry",       Status = "🔬 In Progress",     Technician = "Dr. Ayşe K.",  StartDate = "2026-08-09" },
                new() { Title = "Microbial Culture Sensitivity Test",       Category = "Microbiology",    Status = "⏳ Awaiting Review",  Technician = "Mehmet T.",    StartDate = "2026-08-08" },
                new() { Title = "Water Conductivity Assessment — Site B",   Category = "Environmental",   Status = "✅ Approved",         Technician = "Elif S.",      StartDate = "2026-08-07" },
                new() { Title = "Heavy Metal Concentration — Sample A",     Category = "Chemistry",       Status = "🔬 In Progress",     Technician = "Dr. Ayşe K.",  StartDate = "2026-08-06" },
                new() { Title = "Soil Nutrient Profile Analysis",           Category = "Environmental",   Status = "📝 Draft",           Technician = "Burak Y.",     StartDate = "2026-08-05" },
                new() { Title = "Protein Electrophoresis Run #47",          Category = "Biochemistry",    Status = "✅ Approved",         Technician = "Elif S.",      StartDate = "2026-08-04" },
                new() { Title = "Bacterial Colony Morphology Study",        Category = "Microbiology",    Status = "📦 Archived",        Technician = "Mehmet T.",    StartDate = "2026-08-02" },
                new() { Title = "Volatile Organic Compounds Screening",     Category = "Toxicology",      Status = "❌ Rejected",         Technician = "Burak Y.",     StartDate = "2026-07-30" },
            };

            foreach (var item in sampleData)
            {
                _experiments.Add(item);
            }

            TxtExperimentCount.Text = $"{_experiments.Count} records";
        }
    }

    /// <summary>
    /// Display model for the Experiments DataGrid.
    /// Maps to key columns of the Experiments database table.
    /// </summary>
    public class ExperimentRow
    {
        public string Title { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Technician { get; set; } = string.Empty;
        public string StartDate { get; set; } = string.Empty;
    }
}
