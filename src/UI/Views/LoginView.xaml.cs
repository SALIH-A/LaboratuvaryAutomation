// ============================================================================
// INDEPENDENT LABORATORY DATA MANAGEMENT AND AUTOMATION SYSTEM (LDMAS)
// LoginView.xaml.cs — Code-Behind for the Login Screen
// ============================================================================
// Author          : Salih
// Date            : 2026-08-13
// Layer           : Presentation Layer
// Requirement Ref : FR-002 (authentication), FR-005 (session management)
// ============================================================================
//
// Authentication Flow:
//   1. User enters username + password.
//   2. LoginView calls AuthenticationService.AuthenticateUser().
//   3. AuthenticationService queries MySQL, verifies BCrypt hash, loads roles.
//   4. On success, SessionManager.Login(user) stores the session.
//   5. LoginView raises the OnLoginSuccess event so the host window can
//      swap this view for the MainWindow dashboard.
//
// Security:
//   • PasswordBox.Password is never stored — read once and passed directly.
//   • Error messages are generic ("Invalid username or password") to prevent
//     username enumeration.
//   • AuthenticationService performs dummy hashing on unknown usernames to
//     prevent timing-based username discovery.
//
// ============================================================================

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LDMAS.Security;

namespace LDMAS.UI.Views
{
    /// <summary>
    /// Code-behind for the Login view.
    /// Authenticates users via <see cref="AuthenticationService"/> and
    /// establishes sessions via <see cref="SessionManager"/>.
    /// </summary>
    public partial class LoginView : UserControl
    {
        // =====================================================================
        // EVENTS
        // =====================================================================

        /// <summary>
        /// Raised when authentication succeeds and SessionManager is populated.
        /// The host window should subscribe to this event to navigate to the
        /// main dashboard.
        /// </summary>
        public event EventHandler? OnLoginSuccess;

        /// <summary>
        /// Raised when the user clicks "Create Account" to switch to the
        /// registration view.
        /// </summary>
        public event EventHandler? OnNavigateToRegister;

        // =====================================================================
        // FIELDS
        // =====================================================================

        private readonly AuthenticationService _authService;

        // =====================================================================
        // CONSTRUCTOR
        // =====================================================================

        public LoginView()
        {
            InitializeComponent();
            _authService = new AuthenticationService();

            // Focus the username field on load
            Loaded += (s, e) => TxtUsername.Focus();
        }

        // =====================================================================
        // EVENT HANDLERS
        // =====================================================================

        /// <summary>
        /// Handles the Sign In button click.
        /// Validates inputs, calls AuthenticationService, and on success
        /// sets the global session via SessionManager.
        /// </summary>
        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            PerformLogin();
        }

        /// <summary>
        /// Handles Enter key press on both input fields to submit the form.
        /// </summary>
        private void Input_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                PerformLogin();
            }
        }

        /// <summary>
        /// Navigates to the registration view.
        /// </summary>
        private void LnkCreateAccount_Click(object sender, MouseButtonEventArgs e)
        {
            OnNavigateToRegister?.Invoke(this, EventArgs.Empty);
        }

        // =====================================================================
        // CORE AUTHENTICATION LOGIC
        // =====================================================================

        /// <summary>
        /// Executes the full authentication flow:
        /// input validation → BLL authentication → session creation.
        /// </summary>
        private void PerformLogin()
        {
            // ── Clear previous status ──
            TxtStatus.Text = "";
            TxtStatus.Foreground = FindResource("DangerRed") as System.Windows.Media.Brush;

            // ── Input validation ──
            string username = TxtUsername.Text.Trim();
            string password = PwdPassword.Password;

            if (string.IsNullOrEmpty(username))
            {
                TxtStatus.Text = "Please enter your username.";
                TxtUsername.Focus();
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                TxtStatus.Text = "Please enter your password.";
                PwdPassword.Focus();
                return;
            }

            // ── Disable button to prevent double-click ──
            BtnLogin.IsEnabled = false;

            try
            {
                // ── Call AuthenticationService ──
                AuthenticationResult result = _authService.AuthenticateUser(
                    username,
                    password,
                    ipAddress: "127.0.0.1",     // Local WPF application
                    userAgent: "LDMAS-WPF/1.0"
                );

                if (result.IsSuccess && result.AuthenticatedUser != null)
                {
                    // ── Establish session ──
                    SessionManager.Login(result.AuthenticatedUser);

                    System.Diagnostics.Debug.WriteLine(
                        $"[LoginView] Login successful: {result.AuthenticatedUser.Username} " +
                        $"(Roles: {string.Join(", ", result.AuthenticatedUser.Roles)})");

                    // ── Raise success event for host window ──
                    OnLoginSuccess?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    // ── Show error (generic to prevent enumeration) ──
                    TxtStatus.Text = result.Message;
                    PwdPassword.Password = "";
                    PwdPassword.Focus();
                }
            }
            catch (Exception ex)
            {
                // ── Database connection or unexpected error ──
                TxtStatus.Text = "Unable to connect to the database. Please check your connection.";
                System.Diagnostics.Debug.WriteLine(
                    $"[LoginView] Authentication error: {ex.Message}");
            }
            finally
            {
                BtnLogin.IsEnabled = true;
            }
        }
    }
}
