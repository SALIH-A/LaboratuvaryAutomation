// ============================================================================
// INDEPENDENT LABORATORY DATA MANAGEMENT AND AUTOMATION SYSTEM (LDMAS)
// InventoryView.xaml.cs — Code-Behind for the Inventory Management Module
// ============================================================================
// Author          : Salih
// Date            : 2026-08-12
// Layer           : Presentation Layer
// Requirement Ref : FR-020 (inventory tracking), FR-022 (low-stock alerts)
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;

namespace LDMAS.UI.Views
{
    /// <summary>
    /// Code-behind for the Inventory Management module.
    /// Displays reagent and consumable stock levels, highlights items
    /// below minimum threshold, and calculates summary statistics.
    /// </summary>
    public partial class InventoryView : UserControl
    {
        public InventoryView()
        {
            InitializeComponent();

            // Load sample data for demonstration
            LoadSampleInventoryData();
        }

        // =====================================================================
        // DATA LOADING
        // =====================================================================

        /// <summary>
        /// Loads sample inventory data into the DataGrid and updates the
        /// summary KPI cards. In production, this queries the Inventory table.
        /// </summary>
        private void LoadSampleInventoryData()
        {
            var inventoryItems = new List<InventoryItem>
            {
                new() { ItemName = "Hydrochloric Acid (HCl) 1M",     Category = "Reagent",     StockLevel = 12,    MinThreshold = 5,   Unit = "L",    LastRestocked = "2026-07-28" },
                new() { ItemName = "Sodium Hydroxide (NaOH) 0.5M",   Category = "Reagent",     StockLevel = 8,     MinThreshold = 5,   Unit = "L",    LastRestocked = "2026-08-01" },
                new() { ItemName = "pH Buffer Solution (pH 7.0)",     Category = "Calibration", StockLevel = 2,     MinThreshold = 3,   Unit = "L",    LastRestocked = "2026-07-15" },
                new() { ItemName = "Disposable Cuvettes (10mm)",      Category = "Consumable",  StockLevel = 450,   MinThreshold = 100, Unit = "pcs",  LastRestocked = "2026-08-05" },
                new() { ItemName = "Nitrile Gloves (Medium)",         Category = "PPE",         StockLevel = 28,    MinThreshold = 50,  Unit = "pcs",  LastRestocked = "2026-07-20" },
                new() { ItemName = "Ethanol (96%, Analytical)",       Category = "Solvent",     StockLevel = 15,    MinThreshold = 5,   Unit = "L",    LastRestocked = "2026-08-02" },
                new() { ItemName = "Petri Dishes (90mm, Sterile)",    Category = "Consumable",  StockLevel = 180,   MinThreshold = 50,  Unit = "pcs",  LastRestocked = "2026-08-03" },
                new() { ItemName = "Filter Paper (Whatman No. 1)",    Category = "Consumable",  StockLevel = 95,    MinThreshold = 40,  Unit = "pcs",  LastRestocked = "2026-07-25" },
                new() { ItemName = "Lead Standard Solution (1000ppm)",Category = "Standard",    StockLevel = 1,     MinThreshold = 2,   Unit = "L",    LastRestocked = "2026-06-10" },
                new() { ItemName = "Agar Powder (Bacteriological)",   Category = "Media",       StockLevel = 3,     MinThreshold = 2,   Unit = "kg",   LastRestocked = "2026-07-18" },
            };

            // Calculate status for each item
            foreach (var item in inventoryItems)
            {
                if (item.StockLevel <= 0)
                    item.Status = "🔴 Out of Stock";
                else if (item.StockLevel < item.MinThreshold)
                    item.Status = "⚠️ Low Stock";
                else
                    item.Status = "✅ In Stock";
            }

            DgInventory.ItemsSource = inventoryItems;

            // Update summary cards
            int total = inventoryItems.Count;
            int lowStock = inventoryItems.Count(i => i.StockLevel < i.MinThreshold);
            int inStock = total - lowStock;

            TxtTotalItems.Text = total.ToString();
            TxtInStock.Text = inStock.ToString();
            TxtLowStock.Text = lowStock.ToString();
        }
    }

    /// <summary>
    /// Data model representing an inventory item for the DataGrid display.
    /// In production, this would map to the Inventory database table.
    /// </summary>
    public class InventoryItem
    {
        public string ItemName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int StockLevel { get; set; }
        public int MinThreshold { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string LastRestocked { get; set; } = string.Empty;
    }
}
