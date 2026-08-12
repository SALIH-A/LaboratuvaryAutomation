// ============================================================================
// INDEPENDENT LABORATORY DATA MANAGEMENT AND AUTOMATION SYSTEM (LDMAS)
// MainWindow.xaml.cs — Code-Behind for the Main Dashboard Window
// ============================================================================
// Author          : Salih
// Date            : 2026-08-10
// Layer           : Presentation Layer
// Pattern         : WPF Code-Behind with Page-Based Navigation
// Requirement Ref : FR-035 (dashboard), FR-005 (session timeout)
// ============================================================================

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace LDMAS.UI
{
    /// <summary>
    /// Code-behind for the LDMAS main dashboard window.
    /// 
    /// <para><b>Responsibilities:</b></para>
    /// <list type="bullet">
    ///   <item>Sidebar navigation — switching between module pages.</item>
    ///   <item>Window chrome controls — minimize, maximize/restore, close.</item>
    ///   <item>Session timeout monitoring — auto-logout after inactivity (FR-005).</item>
    ///   <item>User profile display — name, role, initials in the sidebar footer.</item>
    ///   <item>Dashboard metric loading — populating KPI cards on startup (FR-035).</item>
    /// </list>
    /// </summary>
    public partial class MainWindow : Window
    {
        // =====================================================================
        // CONSTANTS
        // =====================================================================

        /// <summary>
        /// Session inactivity timeout in minutes (FR-005, default: 30).
        /// After this period of no user interaction, the session expires
        /// and the user is returned to the login screen.
        /// </summary>
        private const int SESSION_TIMEOUT_MINUTES = 30;

        // =====================================================================
        // FIELDS
        // =====================================================================

        /// <summary>
        /// Dictionary mapping page names to their Grid containers in the XAML.
        /// Enables efficient O(1) page lookups during navigation.
        /// </summary>
        private readonly Dictionary<string, Grid> _pages;

        /// <summary>
        /// Dictionary mapping page names to their sidebar navigation buttons.
        /// Used to toggle active/inactive visual states on navigation.
        /// </summary>
        private readonly Dictionary<string, Button> _navButtons;

        /// <summary>
        /// Maps page names to their display titles and subtitles for the top bar.
        /// </summary>
        private readonly Dictionary<string, (string Title, string Subtitle)> _pageTitles;

        /// <summary>
        /// The currently active page name. Used to prevent redundant navigation.
        /// </summary>
        private string _currentPage = "Dashboard";

        /// <summary>
        /// Timer for session inactivity timeout (FR-005).
        /// Resets on every user interaction event.
        /// </summary>
        private DispatcherTimer? _sessionTimer;

        /// <summary>
        /// Tracks the last user activity timestamp for session timeout calculation.
        /// </summary>
        private DateTime _lastActivityTime;

        // =====================================================================
        // CONSTRUCTOR
        // =====================================================================

        /// <summary>
        /// Initializes the MainWindow, builds navigation maps, loads sample
        /// dashboard data, and starts the session timeout monitor.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            // Build page name → Grid element mapping
            _pages = new Dictionary<string, Grid>
            {
                { "Dashboard",   PageDashboard },
                { "Experiments", PageExperiments },
                { "Equipment",   PageEquipment },
                { "Inventory",   PageInventory },
                { "Reports",     PageReports },
                { "AuditTrail",  PageAuditTrail },
                { "Users",       PageUsers }
            };

            // Build page name → navigation button mapping
            _navButtons = new Dictionary<string, Button>
            {
                { "Dashboard",   BtnNavDashboard },
                { "Experiments", BtnNavExperiments },
                { "Equipment",   BtnNavEquipment },
                { "Inventory",   BtnNavInventory },
                { "Reports",     BtnNavReports },
                { "AuditTrail",  BtnNavAuditTrail },
                { "Users",       BtnNavUsers }
            };

            // Build page name → title/subtitle mapping
            _pageTitles = new Dictionary<string, (string, string)>
            {
                { "Dashboard",   ("Dashboard",         "  —  System Overview") },
                { "Experiments", ("Experiments",        "  —  Manage Laboratory Experiments") },
                { "Equipment",   ("Equipment",          "  —  Asset Registry & Calibration") },
                { "Inventory",   ("Inventory",          "  —  Reagents & Consumables") },
                { "Reports",     ("Reports",            "  —  Data Export & Analytics") },
                { "AuditTrail",  ("Audit Trail",        "  —  Operation History (Read-Only)") },
                { "Users",       ("User Management",    "  —  Accounts & Roles (Admin Only)") }
            };

            // Load initial data
            LoadDashboardSampleData();

            // Initialize session timeout monitor
            InitializeSessionTimer();

            // Record initial activity
            _lastActivityTime = DateTime.Now;
        }

        // =====================================================================
        // NAVIGATION LOGIC
        // =====================================================================

        /// <summary>
        /// Handles all sidebar navigation button clicks. Reads the target page
        /// name from the button's <c>Tag</c> property and switches the visible
        /// content panel accordingly.
        /// </summary>
        private void NavButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string pageName)
            {
                NavigateToPage(pageName);
            }
        }

        /// <summary>
        /// Switches the main content area to display the specified page.
        /// Updates sidebar button states, page title, and resets the session timer.
        /// </summary>
        /// <param name="pageName">
        /// The name of the page to navigate to. Must match a key in <see cref="_pages"/>.
        /// </param>
        private void NavigateToPage(string pageName)
        {
            // Guard: don't re-navigate to the current page
            if (pageName == _currentPage)
                return;

            // Guard: ensure the page exists
            if (!_pages.ContainsKey(pageName))
            {
                MessageBox.Show(
                    $"Page '{pageName}' is not yet implemented.",
                    "Navigation",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            // ── Step 1: Hide all pages ──
            foreach (var page in _pages.Values)
            {
                page.Visibility = Visibility.Collapsed;
            }

            // ── Step 2: Show the target page ──
            _pages[pageName].Visibility = Visibility.Visible;

            // ── Step 3: Update sidebar button styles ──
            UpdateNavButtonStates(pageName);

            // ── Step 4: Update top bar title ──
            if (_pageTitles.TryGetValue(pageName, out var titles))
            {
                TxtPageTitle.Text = titles.Title;
                TxtPageSubtitle.Text = titles.Subtitle;
            }

            // ── Step 5: Track current page ──
            _currentPage = pageName;

            // ── Step 6: Reset session timer ──
            ResetSessionTimer();

            System.Diagnostics.Debug.WriteLine($"[MainWindow] Navigated to: {pageName}");
        }

        /// <summary>
        /// Updates sidebar navigation button visual states — sets the active button
        /// to <c>NavButtonActiveStyle</c> and all others to <c>NavButtonStyle</c>.
        /// </summary>
        private void UpdateNavButtonStates(string activePageName)
        {
            var activeStyle = (Style)FindResource("NavButtonActiveStyle");
            var normalStyle = (Style)FindResource("NavButtonStyle");

            foreach (var kvp in _navButtons)
            {
                kvp.Value.Style = kvp.Key == activePageName ? activeStyle : normalStyle;
            }
        }

        // =====================================================================
        // WINDOW CHROME CONTROLS (Borderless Window)
        // =====================================================================

        /// <summary>
        /// Enables window dragging from the title bar and sidebar header areas.
        /// </summary>
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                // Double-click to toggle maximize
                ToggleMaximize();
            }
            else
            {
                // Single click to drag
                if (WindowState == WindowState.Maximized)
                {
                    // Restore before dragging from maximized state
                    var point = PointToScreen(e.GetPosition(this));
                    WindowState = WindowState.Normal;
                    Left = point.X - (ActualWidth / 2);
                    Top = point.Y - 20;
                }
                DragMove();
            }

            ResetSessionTimer();
        }

        /// <summary>Minimizes the window.</summary>
        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        /// <summary>Toggles between maximized and normal window states.</summary>
        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            ToggleMaximize();
        }

        /// <summary>Closes the application with confirmation.</summary>
        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to exit LDMAS?",
                "Confirm Exit",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                Application.Current.Shutdown();
            }
        }

        /// <summary>
        /// Toggles window state and updates the maximize/restore button icon.
        /// </summary>
        private void ToggleMaximize()
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
                BtnMaxRestore.Content = "☐";
            }
            else
            {
                WindowState = WindowState.Maximized;
                BtnMaxRestore.Content = "❐";
            }
        }

        // =====================================================================
        // USER PROFILE & LOGOUT
        // =====================================================================

        /// <summary>
        /// Sets the user profile information displayed in the sidebar footer.
        /// Called after successful authentication to populate name, role, and initials.
        /// </summary>
        /// <param name="fullName">The user's full display name.</param>
        /// <param name="roleName">The user's primary role name.</param>
        public void SetUserProfile(string fullName, string roleName)
        {
            TxtUserFullName.Text = fullName;
            TxtUserRole.Text = roleName;

            // Generate initials from first and last name
            var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                TxtUserInitial.Text = $"{parts[0][0]}{parts[parts.Length - 1][0]}".ToUpper();
            }
            else if (parts.Length == 1 && parts[0].Length >= 2)
            {
                TxtUserInitial.Text = parts[0].Substring(0, 2).ToUpper();
            }

            TxtWelcome.Text = $"Welcome back, {parts[0]}";
        }

        /// <summary>
        /// Handles the logout button click. Confirms, then exits to the login screen.
        /// In a full implementation, this would clear the session and show LoginWindow.
        /// </summary>
        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to log out?",
                "Confirm Logout",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                StopSessionTimer();
                System.Diagnostics.Debug.WriteLine("[MainWindow] User logged out.");

                // TODO: Show LoginWindow and close MainWindow
                // var loginWindow = new LoginWindow();
                // loginWindow.Show();
                // this.Close();

                MessageBox.Show(
                    "You have been logged out successfully.\n\nIn the full implementation, the Login window would appear here.",
                    "Logged Out",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        // =====================================================================
        // SESSION TIMEOUT (FR-005)
        // =====================================================================

        /// <summary>
        /// Initializes the session inactivity timer. The timer fires every minute
        /// to check whether the inactivity threshold has been exceeded.
        /// </summary>
        private void InitializeSessionTimer()
        {
            _sessionTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(1)
            };
            _sessionTimer.Tick += SessionTimer_Tick;
            _sessionTimer.Start();

            _lastActivityTime = DateTime.Now;

            // Also track user input events for activity detection
            this.PreviewMouseMove += OnUserActivity;
            this.PreviewKeyDown += OnUserActivity;
            this.PreviewMouseDown += OnUserActivity;
        }

        /// <summary>
        /// Checks session validity on each timer tick. If the inactivity period
        /// exceeds <see cref="SESSION_TIMEOUT_MINUTES"/>, the session is terminated.
        /// </summary>
        private void SessionTimer_Tick(object? sender, EventArgs e)
        {
            var inactiveMinutes = (DateTime.Now - _lastActivityTime).TotalMinutes;

            if (inactiveMinutes >= SESSION_TIMEOUT_MINUTES)
            {
                StopSessionTimer();

                MessageBox.Show(
                    $"Your session has expired after {SESSION_TIMEOUT_MINUTES} minutes of inactivity.\nPlease log in again.",
                    "Session Expired",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                System.Diagnostics.Debug.WriteLine("[MainWindow] Session expired due to inactivity.");

                // TODO: Navigate to LoginWindow
                // Application.Current.Shutdown();
            }
        }

        /// <summary>
        /// Resets the last activity timestamp whenever user input is detected.
        /// </summary>
        private void OnUserActivity(object sender, EventArgs e)
        {
            _lastActivityTime = DateTime.Now;
        }

        /// <summary>Resets the session timeout counter.</summary>
        private void ResetSessionTimer()
        {
            _lastActivityTime = DateTime.Now;
        }

        /// <summary>Stops and cleans up the session timer.</summary>
        private void StopSessionTimer()
        {
            if (_sessionTimer != null)
            {
                _sessionTimer.Stop();
                _sessionTimer.Tick -= SessionTimer_Tick;
            }
        }

        // =====================================================================
        // QUICK ACTION HANDLERS
        // =====================================================================

        /// <summary>
        /// Quick action: Navigate to the Experiments page to create a new experiment.
        /// </summary>
        private void BtnQuickNewExperiment_Click(object sender, RoutedEventArgs e)
        {
            NavigateToPage("Experiments");
        }

        /// <summary>
        /// Quick action: Navigate to the Equipment page to register new equipment.
        /// </summary>
        private void BtnQuickAddEquipment_Click(object sender, RoutedEventArgs e)
        {
            NavigateToPage("Equipment");
        }

        /// <summary>
        /// Quick action: Navigate to the Reports page to generate a CSV export.
        /// </summary>
        private void BtnQuickGenerateReport_Click(object sender, RoutedEventArgs e)
        {
            NavigateToPage("Reports");
        }

        /// <summary>
        /// Quick action: Navigate to the Inventory page to check stock levels.
        /// </summary>
        private void BtnQuickCheckInventory_Click(object sender, RoutedEventArgs e)
        {
            NavigateToPage("Inventory");
        }

        // =====================================================================
        // DASHBOARD DATA LOADING
        // =====================================================================

        /// <summary>
        /// Loads sample data into the dashboard for demonstration purposes.
        /// In production, this will query the database via the BLL services
        /// using the <c>sp_get_dashboard_summary</c> stored procedure (FR-035).
        /// </summary>
        private void LoadDashboardSampleData()
        {
            // ── Sample data for the Recent Experiments DataGrid ──
            var sampleExperiments = new List<dynamic>
            {
                new { Title = "pH Analysis — Batch 2026-Q3",          Category = "Chemistry",       Status = "In Progress",     Date = "2026-08-09" },
                new { Title = "Microbial Culture Sensitivity",         Category = "Microbiology",    Status = "Awaiting Review", Date = "2026-08-08" },
                new { Title = "Water Conductivity Assessment",         Category = "Environmental",   Status = "Approved",        Date = "2026-08-07" },
                new { Title = "Heavy Metal Concentration — Sample A",  Category = "Chemistry",       Status = "In Progress",     Date = "2026-08-06" },
                new { Title = "Soil Nutrient Profile Analysis",        Category = "Environmental",   Status = "Draft",           Date = "2026-08-05" },
                new { Title = "Protein Electrophoresis Run #47",       Category = "Biochemistry",    Status = "Approved",        Date = "2026-08-04" },
                new { Title = "Bacterial Colony Morphology Study",      Category = "Microbiology",    Status = "Archived",        Date = "2026-08-02" }
            };

            DgRecentExperiments.ItemsSource = sampleExperiments;

            // ── Update KPI cards with sample metrics ──
            // In production: call sp_get_dashboard_summary and bind results.
            TxtMetricExperiments.Text = "12";
            TxtMetricApprovals.Text   = "5";
            TxtMetricEquipment.Text   = "28";
            TxtMetricLowStock.Text    = "3";

            TxtLastLogin.Text = $"Last login: {DateTime.Now:dd MMM yyyy, HH:mm}";
        }

        // =====================================================================
        // PUBLIC METHODS — For Integration with BLL Services
        // =====================================================================

        /// <summary>
        /// Refreshes the dashboard metrics by querying the database.
        /// Called when navigating back to the dashboard or after data modifications.
        /// </summary>
        public void RefreshDashboardMetrics(int activeExperiments, int pendingApprovals,
                                              int activeEquipment, int lowStockItems)
        {
            TxtMetricExperiments.Text = activeExperiments.ToString();
            TxtMetricApprovals.Text   = pendingApprovals.ToString();
            TxtMetricEquipment.Text   = activeEquipment.ToString();
            TxtMetricLowStock.Text    = lowStockItems.ToString();
        }

        /// <summary>
        /// Updates the database connection status indicator in the top bar.
        /// </summary>
        /// <param name="isConnected">Whether the database is reachable.</param>
        public void SetConnectionStatus(bool isConnected)
        {
            TxtConnectionStatus.Text = isConnected ? "Connected" : "Disconnected";
            // In a full implementation, also toggle the green/red dot
        }
    }
}
