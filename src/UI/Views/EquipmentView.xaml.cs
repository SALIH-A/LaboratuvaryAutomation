// ============================================================================
// INDEPENDENT LABORATORY DATA MANAGEMENT AND AUTOMATION SYSTEM (LDMAS)
// EquipmentView.xaml.cs — Code-Behind for the Equipment Management Module
// ============================================================================
// Author          : Salih
// Date            : 2026-08-12
// Layer           : Presentation Layer
// Requirement Ref : FR-020 (equipment registry), FR-021 (calibration tracking)
// ============================================================================

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace LDMAS.UI.Views
{
    /// <summary>
    /// Code-behind for the Equipment Management module.
    /// Manages the equipment DataGrid and the Register Equipment form.
    /// In production, integrates with <c>EquipmentRepository</c> for CRUD.
    /// </summary>
    public partial class EquipmentView : UserControl
    {
        /// <summary>
        /// Observable collection backing the equipment DataGrid.
        /// New registrations from the form appear instantly.
        /// </summary>
        private readonly ObservableCollection<EquipmentRow> _equipment;

        public EquipmentView()
        {
            InitializeComponent();

            _equipment = new ObservableCollection<EquipmentRow>();
            DgEquipment.ItemsSource = _equipment;

            // Default calibration date to today
            DpEqCalibration.SelectedDate = DateTime.Now;

            // Load sample data
            LoadSampleEquipmentData();
        }

        // =====================================================================
        // EVENT HANDLERS
        // =====================================================================

        /// <summary>
        /// Validates and registers new equipment from the form fields.
        /// In production, this calls <c>EquipmentRepository.Create()</c>.
        /// </summary>
        private void BtnRegisterEquipment_Click(object sender, RoutedEventArgs e)
        {
            // ── Validation ──
            string name = TxtEqName.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                TxtEqFormStatus.Foreground = FindResource("DangerBrush") as System.Windows.Media.Brush;
                TxtEqFormStatus.Text = "⚠ Equipment name is required.";
                return;
            }

            string serial = TxtEqSerial.Text.Trim();
            if (string.IsNullOrEmpty(serial))
            {
                TxtEqFormStatus.Foreground = FindResource("DangerBrush") as System.Windows.Media.Brush;
                TxtEqFormStatus.Text = "⚠ Serial number is required.";
                return;
            }

            // ── Build the new row ──
            var newEquipment = new EquipmentRow
            {
                Name            = name,
                Model           = TxtEqModel.Text.Trim(),
                SerialNumber    = serial,
                Status          = "✅ Active",
                Location        = TxtEqLocation.Text.Trim(),
                CalibrationDate = DpEqCalibration.SelectedDate?.ToString("yyyy-MM-dd") ?? "—"
            };

            // ── Insert at top of DataGrid ──
            _equipment.Insert(0, newEquipment);
            TxtEquipmentCount.Text = $"{_equipment.Count} items";

            // ── Clear form and show success ──
            TxtEqName.Clear();
            TxtEqModel.Clear();
            TxtEqSerial.Clear();
            TxtEqLocation.Clear();
            DpEqCalibration.SelectedDate = DateTime.Now;

            TxtEqFormStatus.Foreground = FindResource("SuccessBrush") as System.Windows.Media.Brush;
            TxtEqFormStatus.Text = $"✅ \"{name}\" registered successfully.";

            System.Diagnostics.Debug.WriteLine($"[EquipmentView] New equipment registered: {name} (S/N: {serial})");
        }

        // =====================================================================
        // DATA LOADING
        // =====================================================================

        /// <summary>
        /// Loads sample equipment data for demonstration purposes.
        /// In production, this queries the Equipment table via EquipmentRepository.
        /// </summary>
        private void LoadSampleEquipmentData()
        {
            var sampleData = new List<EquipmentRow>
            {
                new() { Name = "Analytical Balance",           Model = "Mettler Toledo XPR",    SerialNumber = "MT-2026-001",  Status = "✅ Active",            Location = "Lab A",     CalibrationDate = "2026-07-15" },
                new() { Name = "UV-Vis Spectrophotometer",     Model = "Shimadzu UV-1900i",     SerialNumber = "SZ-2025-042",  Status = "✅ Active",            Location = "Lab A",     CalibrationDate = "2026-08-01" },
                new() { Name = "pH Meter",                     Model = "Hanna HI5522",          SerialNumber = "HN-2024-118",  Status = "⚠️ Calibration Due",  Location = "Lab B",     CalibrationDate = "2026-06-10" },
                new() { Name = "Centrifuge",                   Model = "Eppendorf 5425",        SerialNumber = "EP-2025-067",  Status = "✅ Active",            Location = "Lab C",     CalibrationDate = "2026-07-28" },
                new() { Name = "Autoclave",                    Model = "Tuttnauer 3870EA",      SerialNumber = "TT-2023-033",  Status = "🔧 Under Maintenance", Location = "Sterilization", CalibrationDate = "2026-05-20" },
                new() { Name = "Fume Hood",                    Model = "Labconco Protector XL",  SerialNumber = "LC-2024-089",  Status = "✅ Active",            Location = "Lab B",     CalibrationDate = "2026-08-05" },
                new() { Name = "Conductivity Meter",           Model = "YSI Pro30",             SerialNumber = "YS-2025-055",  Status = "✅ Active",            Location = "Field Kit", CalibrationDate = "2026-07-22" },
                new() { Name = "Incubator",                    Model = "Thermo Heratherm",      SerialNumber = "TH-2024-071",  Status = "✅ Active",            Location = "Lab C",     CalibrationDate = "2026-08-03" },
                new() { Name = "Turbidity Meter",              Model = "Hach 2100Q",            SerialNumber = "HC-2023-012",  Status = "🚫 Decommissioned",   Location = "Storage",   CalibrationDate = "2025-11-15" },
                new() { Name = "Microscope (Binocular)",       Model = "Olympus CX23",          SerialNumber = "OL-2026-003",  Status = "✅ Active",            Location = "Lab A",     CalibrationDate = "2026-08-08" },
            };

            foreach (var item in sampleData)
            {
                _equipment.Add(item);
            }

            TxtEquipmentCount.Text = $"{_equipment.Count} items";
        }
    }

    /// <summary>
    /// Display model for the Equipment DataGrid.
    /// Maps to key columns of the Equipment database table.
    /// </summary>
    public class EquipmentRow
    {
        public string Name { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string CalibrationDate { get; set; } = string.Empty;
    }
}
