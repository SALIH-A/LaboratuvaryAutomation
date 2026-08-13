// ============================================================================
// INDEPENDENT LABORATORY DATA MANAGEMENT AND AUTOMATION SYSTEM (LDMAS)
// RegisterView.xaml.cs — Code-Behind for the Registration Screen
// ============================================================================
// Author          : Salih
// Date            : 2026-08-13
// Layer           : Presentation Layer
// Requirement Ref : FR-001 (user creation), FR-003 (password hashing)
// ============================================================================
//
// CRITICAL SECURITY RULE:
//   Self-registered users are ALWAYS assigned the "Technician" role.
//   There is no role selector in this UI — by design.
//   Only administrators can elevate roles via the User Management module.
//
// Registration Flow:
//   1. User fills in personal details + password.
//   2. RegisterView validates all fields + password strength.
//   3. Calls AuthenticationService.RegisterUser() which:
//      a. Hashes the password with BCrypt (cost 12).
//      b. Inserts into Users table.
//      c. Assigns "Technician" role via UserRoles table.
//   4. On success, raises OnRegistrationSuccess so the host window
//      can navigate back to the login screen.
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
    /// Code-behind for the Registration view.
    /// Creates new user accounts via <see cref="AuthenticationService.RegisterUser"/>
    /// with hardcoded "Technician" role assignment.
    /// </summary>
    public partial class RegisterView : UserControl
    {
        // =====================================================================
        // CONSTANTS
        // =====================================================================

        /// <summary>
        /// The role assigned to ALL self-registered users.
        /// This is a security invariant — never expose a role selector to
        /// non-admin users. Only admins can elevate roles via UserManagementView.
        /// </summary>
        private const string DEFAULT_SELF_REGISTRATION_ROLE = "Technician";

        // =====================================================================
        // EVENTS
        // =====================================================================

        /// <summary>
        /// Raised when registration succeeds. The host window should
        /// navigate back to the login screen.
        /// </summary>
        public event EventHandler? OnRegistrationSuccess;

        /// <summary>
        /// Raised when the user clicks "Sign In" to switch back to login.
        /// </summary>
        public event EventHandler? OnNavigateToLogin;

        // =====================================================================
        // FIELDS
        // =====================================================================

        private readonly AuthenticationService _authService;

        // =====================================================================
        // CONSTRUCTOR
        // =====================================================================

        public RegisterView()
        {
            InitializeComponent();
            _authService = new AuthenticationService();

            // Focus the first name field on load
            Loaded += (s, e) => TxtFirstName.Focus();
        }

        // =====================================================================
        // EVENT HANDLERS
        // =====================================================================

        /// <summary>
        /// Handles the Create Account button click.
        /// </summary>
        private void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            PerformRegistration();
        }

        /// <summary>
        /// Navigates back to the login view.
        /// </summary>
        private void LnkBackToLogin_Click(object sender, MouseButtonEventArgs e)
        {
            OnNavigateToLogin?.Invoke(this, EventArgs.Empty);
        }

        // =====================================================================
        // CORE REGISTRATION LOGIC
        // =====================================================================

        /// <summary>
        /// Executes the full registration flow:
        /// input validation → password strength check → BLL registration.
        /// </summary>
        private void PerformRegistration()
        {
            // ── Clear previous status ──
            TxtStatus.Text = "";
            TxtStatus.Foreground = FindResource("DangerRed") as System.Windows.Media.Brush;

            // ── Gather inputs ──
            string firstName = TxtFirstName.Text.Trim();
            string lastName  = TxtLastName.Text.Trim();
            string email     = TxtEmail.Text.Trim();
            string username  = TxtUsername.Text.Trim();
            string password  = PwdPassword.Password;

            // ── Field-by-field validation ──
            if (string.IsNullOrEmpty(firstName))
            {
                TxtStatus.Text = "First name is required.";
                TxtFirstName.Focus();
                return;
            }

            if (string.IsNullOrEmpty(lastName))
            {
                TxtStatus.Text = "Last name is required.";
                TxtLastName.Focus();
                return;
            }

            if (string.IsNullOrEmpty(email) || !email.Contains('@') || !email.Contains('.'))
            {
                TxtStatus.Text = "A valid email address is required.";
                TxtEmail.Focus();
                return;
            }

            if (string.IsNullOrEmpty(username))
            {
                TxtStatus.Text = "Username is required.";
                TxtUsername.Focus();
                return;
            }

            if (username.Length < 3)
            {
                TxtStatus.Text = "Username must be at least 3 characters.";
                TxtUsername.Focus();
                return;
            }

            // ── Password strength validation ──
            var (isValid, strengthMessage) = SecurityHelper.ValidatePasswordStrength(password);
            if (!isValid)
            {
                TxtStatus.Text = strengthMessage;
                PwdPassword.Focus();
                return;
            }

            // ── Disable button to prevent double-click ──
            BtnRegister.IsEnabled = false;

            try
            {
                // ── Build registration request ──
                // SECURITY: Role is hardcoded — never read from a UI control.
                var request = new UserRegistrationRequest
                {
                    FirstName  = firstName,
                    LastName   = lastName,
                    Email      = email,
                    Username   = username,
                    Password   = password,
                    RoleName   = DEFAULT_SELF_REGISTRATION_ROLE  // Always "Technician"
                };

                // ── Call AuthenticationService ──
                int newUserId = _authService.RegisterUser(request, assignedByUserId: null);

                if (newUserId > 0)
                {
                    // ── Success ──
                    TxtStatus.Foreground = FindResource("SuccessGreen") as System.Windows.Media.Brush;
                    TxtStatus.Text = $"✅ Account created successfully! You can now sign in as \"{username}\".";

                    System.Diagnostics.Debug.WriteLine(
                        $"[RegisterView] User registered: ID={newUserId}, " +
                        $"Username='{username}', Role='{DEFAULT_SELF_REGISTRATION_ROLE}'");

                    // Clear sensitive field
                    PwdPassword.Password = "";

                    // Raise success event after brief delay for user to read the message
                    var timer = new System.Windows.Threading.DispatcherTimer
                    {
                        Interval = TimeSpan.FromSeconds(2)
                    };
                    timer.Tick += (s, e) =>
                    {
                        timer.Stop();
                        OnRegistrationSuccess?.Invoke(this, EventArgs.Empty);
                    };
                    timer.Start();
                }
                else
                {
                    // ── Registration failed (duplicate username/email or validation error) ──
                    TxtStatus.Text = "Registration failed. Username or email may already exist.";
                }
            }
            catch (Exception ex)
            {
                TxtStatus.Text = "Unable to connect to the database. Please check your connection.";
                System.Diagnostics.Debug.WriteLine(
                    $"[RegisterView] Registration error: {ex.Message}");
            }
            finally
            {
                BtnRegister.IsEnabled = true;
            }
        }
    }
}
