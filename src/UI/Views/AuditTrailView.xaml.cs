// ============================================================================
// INDEPENDENT LABORATORY DATA MANAGEMENT AND AUTOMATION SYSTEM (LDMAS)
// AuditTrailView.xaml.cs — Code-Behind for the Audit Trail Module
// ============================================================================
// Author          : Salih
// Date            : 2026-08-12
// Layer           : Presentation Layer
// Requirement Ref : FR-040 (audit logging), FR-041 (immutable trail),
//                   FR-042 (audit filtering)
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace LDMAS.UI.Views
{
    /// <summary>
    /// Code-behind for the Audit Trail module.
    /// Displays an immutable, read-only log of all data-modifying operations.
    /// Supports filtering by module, operation type, and user search.
    /// </summary>
    public partial class AuditTrailView : UserControl
    {
        /// <summary>
        /// Master list of all audit entries. Used as source for filtering.
        /// </summary>
        private List<AuditLogEntry> _allEntries = new();

        public AuditTrailView()
        {
            InitializeComponent();
            LoadSampleAuditData();
        }

        // =====================================================================
        // FILTER LOGIC
        // =====================================================================

        /// <summary>
        /// Applies combined filters whenever any filter control value changes.
        /// Handles Module ComboBox, Operation ComboBox, and User search TextBox.
        /// </summary>
        private void Filter_Changed(object sender, EventArgs e)
        {
            ApplyFilters();
        }

        /// <summary>
        /// Applies all active filters to the master audit log list.
        /// Uses LINQ to chain Where clauses for each filter dimension.
        /// </summary>
        private void ApplyFilters()
        {
            IEnumerable<AuditLogEntry> filtered = _allEntries;

            // Filter by Module
            string moduleFilter = (CmbFilterModule.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All Modules";
            if (moduleFilter != "All Modules")
            {
                filtered = filtered.Where(e => e.Module.Equals(moduleFilter, StringComparison.OrdinalIgnoreCase));
            }

            // Filter by Operation
            string operationFilter = (CmbFilterOperation.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All Operations";
            if (operationFilter != "All Operations")
            {
                filtered = filtered.Where(e => e.Operation.Equals(operationFilter, StringComparison.OrdinalIgnoreCase));
            }

            // Filter by User search text
            string userSearch = TxtSearchUser.Text.Trim();
            if (!string.IsNullOrEmpty(userSearch))
            {
                filtered = filtered.Where(e => e.User.Contains(userSearch, StringComparison.OrdinalIgnoreCase));
            }

            var resultList = filtered.ToList();
            DgAuditLog.ItemsSource = resultList;
            TxtAuditCount.Text = $"{resultList.Count} entries";
        }

        /// <summary>
        /// Resets all filter controls to their default state.
        /// </summary>
        private void BtnClearFilters_Click(object sender, RoutedEventArgs e)
        {
            CmbFilterModule.SelectedIndex = 0;
            CmbFilterOperation.SelectedIndex = 0;
            TxtSearchUser.Clear();
            ApplyFilters();
        }

        // =====================================================================
        // DATA LOADING
        // =====================================================================

        /// <summary>
        /// Loads sample audit log data for demonstration.
        /// In production, this queries the AuditLog table via the DAL.
        /// </summary>
        private void LoadSampleAuditData()
        {
            _allEntries = new List<AuditLogEntry>
            {
                new() { Timestamp = "2026-08-12 14:30:22", User = "admin",       Operation = "INSERT", Module = "Users",          RecordId = "15",  Details = "Created new user account: elif.sari@lab.edu" },
                new() { Timestamp = "2026-08-12 14:25:10", User = "dr.ayse",     Operation = "UPDATE", Module = "Experiments",     RecordId = "42",  Details = "Status changed: 'Draft' → 'In Progress'" },
                new() { Timestamp = "2026-08-12 13:58:03", User = "mehmet.t",    Operation = "INSERT", Module = "TestResults",     RecordId = "187", Details = "pH measurement: 7.12 pH units (Pass)" },
                new() { Timestamp = "2026-08-12 13:45:11", User = "admin",       Operation = "UPDATE", Module = "Equipment",       RecordId = "8",   Details = "Status changed: 'Active' → 'Under Maintenance'" },
                new() { Timestamp = "2026-08-12 12:20:45", User = "burak.y",     Operation = "INSERT", Module = "Experiments",     RecordId = "43",  Details = "Created experiment: 'Soil Nutrient Profile Analysis'" },
                new() { Timestamp = "2026-08-12 11:15:00", User = "elif.s",      Operation = "UPDATE", Module = "InventoryItems",  RecordId = "6",   Details = "Stock level updated: 15 → 12 (Ethanol 96%)" },
                new() { Timestamp = "2026-08-12 10:30:55", User = "dr.ayse",     Operation = "INSERT", Module = "TestResults",     RecordId = "186", Details = "Conductivity: 1520.0 µS/cm — ⚠️ ANOMALY flagged" },
                new() { Timestamp = "2026-08-11 16:42:18", User = "admin",       Operation = "DELETE", Module = "Users",           RecordId = "9",   Details = "Deactivated user account: intern_2025@lab.edu" },
                new() { Timestamp = "2026-08-11 15:10:33", User = "mehmet.t",    Operation = "UPDATE", Module = "Experiments",     RecordId = "40",  Details = "Status changed: 'In Progress' → 'Awaiting Review'" },
                new() { Timestamp = "2026-08-11 14:05:22", User = "admin",       Operation = "INSERT", Module = "Equipment",       RecordId = "11",  Details = "Registered: Olympus CX23 Microscope (S/N: OL-2026-003)" },
                new() { Timestamp = "2026-08-11 11:30:00", User = "elif.s",      Operation = "UPDATE", Module = "InventoryItems",  RecordId = "3",   Details = "Stock level updated: 5 → 2 (pH Buffer Solution)" },
                new() { Timestamp = "2026-08-11 09:15:44", User = "dr.ayse",     Operation = "INSERT", Module = "TestResults",     RecordId = "185", Details = "Lead (Pb): 0.082 mg/L — 🔴 FAIL (exceeds 0.01 limit)" },
                new() { Timestamp = "2026-08-10 17:00:12", User = "admin",       Operation = "UPDATE", Module = "Users",           RecordId = "3",   Details = "Role changed: 'Technician' → 'Manager' for burak.y" },
                new() { Timestamp = "2026-08-10 14:22:08", User = "burak.y",     Operation = "INSERT", Module = "Experiments",     RecordId = "41",  Details = "Created experiment: 'Volatile Organic Compounds Screening'" },
                new() { Timestamp = "2026-08-10 10:05:30", User = "admin",       Operation = "INSERT", Module = "InventoryItems",  RecordId = "10",  Details = "Added: Agar Powder (Bacteriological), 3 kg" },
            };

            DgAuditLog.ItemsSource = _allEntries;
            TxtAuditCount.Text = $"{_allEntries.Count} entries";
        }
    }

    /// <summary>
    /// Display model for the Audit Trail DataGrid.
    /// Maps to key columns of the AuditLog database table.
    /// </summary>
    public class AuditLogEntry
    {
        public string Timestamp { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        public string Operation { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
        public string RecordId { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
    }
}
