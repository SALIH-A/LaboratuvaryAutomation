// ============================================================================
// INDEPENDENT LABORATORY DATA MANAGEMENT AND AUTOMATION SYSTEM (LDMAS)
// UserManagementView.xaml.cs — Code-Behind for User Management Module
// ============================================================================
// Author          : Salih
// Date            : 2026-08-12
// Layer           : Presentation Layer
// Requirement Ref : FR-001 (user creation), FR-004 (RBAC role assignment)
// ============================================================================

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace LDMAS.UI.Views
{
    /// <summary>
    /// Code-behind for the User Management module.
    /// Admin-only interface for creating user accounts and assigning RBAC roles.
    /// In production, integrates with <c>AuthenticationService.RegisterUser()</c>.
    /// </summary>
    public partial class UserManagementView : UserControl
    {
        /// <summary>
        /// Observable collection backing the users DataGrid.
        /// </summary>
        private readonly ObservableCollection<UserRow> _users;

        public UserManagementView()
        {
            InitializeComponent();

            _users = new ObservableCollection<UserRow>();
            DgUsers.ItemsSource = _users;

            LoadSampleUserData();
        }

        // =====================================================================
        // EVENT HANDLERS
        // =====================================================================

        /// <summary>
        /// Validates and creates a new user account.
        /// In production, this calls <c>AuthenticationService.RegisterUser()</c>
        /// which hashes the temporary password with BCrypt.
        /// </summary>
        private void BtnCreateUser_Click(object sender, RoutedEventArgs e)
        {
            // ── Validation ──
            string firstName = TxtFirstName.Text.Trim();
            string lastName = TxtLastName.Text.Trim();
            string username = TxtUsername.Text.Trim();
            string email = TxtEmail.Text.Trim();

            if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName))
            {
                ShowFormError("⚠ First name and last name are required.");
                return;
            }

            if (string.IsNullOrEmpty(username))
            {
                ShowFormError("⚠ Username is required.");
                return;
            }

            // Check for duplicate username
            if (_users.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
            {
                ShowFormError($"⚠ Username \"{username}\" already exists.");
                return;
            }

            if (string.IsNullOrEmpty(email) || !email.Contains('@'))
            {
                ShowFormError("⚠ A valid email address is required.");
                return;
            }

            // ── Build the new row ──
            string role = (CmbRole.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Technician";
            string department = (CmbDepartment.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Chemistry";

            var newUser = new UserRow
            {
                Username   = username,
                FullName   = $"{firstName} {lastName}",
                Email      = email,
                Role       = $"🛡️ {role}",
                Department = department,
                Status     = "✅ Active"
            };

            // ── Insert at top ──
            _users.Insert(0, newUser);
            UpdateSummaryCards();

            // ── Clear form ──
            TxtFirstName.Clear();
            TxtLastName.Clear();
            TxtUsername.Clear();
            TxtEmail.Clear();
            CmbRole.SelectedIndex = 2;       // Default: Technician
            CmbDepartment.SelectedIndex = 0;

            TxtUserFormStatus.Foreground = FindResource("SuccessBrush") as System.Windows.Media.Brush;
            TxtUserFormStatus.Text = $"✅ User \"{username}\" created. Temporary password generated.";

            System.Diagnostics.Debug.WriteLine($"[UserManagement] Created user: {username} ({role})");
        }

        // =====================================================================
        // HELPERS
        // =====================================================================

        private void ShowFormError(string message)
        {
            TxtUserFormStatus.Foreground = FindResource("DangerBrush") as System.Windows.Media.Brush;
            TxtUserFormStatus.Text = message;
        }

        private void UpdateSummaryCards()
        {
            int total = _users.Count;
            int active = _users.Count(u => u.Status.Contains("Active"));
            int inactive = total - active;
            int roles = _users.Select(u => u.Role).Distinct().Count();

            TxtTotalUsers.Text = total.ToString();
            TxtActiveUsers.Text = active.ToString();
            TxtInactiveUsers.Text = inactive.ToString();
            TxtRoleCount.Text = roles.ToString();
        }

        // =====================================================================
        // DATA LOADING
        // =====================================================================

        /// <summary>
        /// Loads sample user data for demonstration.
        /// In production, this queries Users + UserRoles tables via the DAL.
        /// </summary>
        private void LoadSampleUserData()
        {
            var sampleUsers = new List<UserRow>
            {
                new() { Username = "admin",      FullName = "System Administrator", Email = "admin@ldmas.lab.edu",      Role = "🛡️ Admin",      Department = "Administration",   Status = "✅ Active" },
                new() { Username = "dr.ayse",    FullName = "Dr. Ayşe Kara",        Email = "ayse.kara@lab.edu",        Role = "🛡️ Manager",    Department = "Chemistry",        Status = "✅ Active" },
                new() { Username = "mehmet.t",   FullName = "Mehmet Tunç",           Email = "mehmet.tunc@lab.edu",      Role = "🛡️ Technician", Department = "Microbiology",     Status = "✅ Active" },
                new() { Username = "elif.s",     FullName = "Elif Sarı",             Email = "elif.sari@lab.edu",        Role = "🛡️ Technician", Department = "Environmental",    Status = "✅ Active" },
                new() { Username = "burak.y",    FullName = "Burak Yılmaz",          Email = "burak.yilmaz@lab.edu",     Role = "🛡️ Manager",    Department = "Environmental",    Status = "✅ Active" },
                new() { Username = "zeynep.d",   FullName = "Zeynep Demir",          Email = "zeynep.demir@lab.edu",     Role = "🛡️ Auditor",    Department = "Quality Control",  Status = "✅ Active" },
                new() { Username = "can.oz",     FullName = "Can Özkan",             Email = "can.ozkan@lab.edu",        Role = "🛡️ Technician", Department = "Chemistry",        Status = "⏸ Inactive" },
                new() { Username = "intern_2025",FullName = "Former Intern",         Email = "intern2025@lab.edu",       Role = "🛡️ Technician", Department = "Chemistry",        Status = "⏸ Inactive" },
            };

            foreach (var user in sampleUsers)
            {
                _users.Add(user);
            }

            UpdateSummaryCards();
        }
    }

    /// <summary>
    /// Display model for the Users DataGrid.
    /// Maps to key columns of the Users + UserRoles database tables.
    /// </summary>
    public class UserRow
    {
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
