// ============================================================================
// INDEPENDENT LABORATORY DATA MANAGEMENT AND AUTOMATION SYSTEM (LDMAS)
// SessionManager.cs — Global Session State for Authenticated Users
// ============================================================================
// Author          : Salih
// Date            : 2026-08-13
// Layer           : Security / Presentation Support
// Requirement Ref : FR-002 (authentication), FR-004 (RBAC),
//                   FR-005 (session timeout)
// ============================================================================
//
// This static class provides application-wide access to the currently
// authenticated user. It acts as the bridge between the AuthenticationService
// (BLL) and the WPF Presentation Layer, enabling:
//
//   • Role-based UI gating  (e.g., hide Admin-only menus)
//   • Session timeout tracking
//   • Audit trail attribution (who performed this action?)
//
// Usage:
//   SessionManager.Login(authenticatedUser);        // After successful auth
//   var name = SessionManager.CurrentUser.FullName; // Read from anywhere
//   SessionManager.Logout();                        // Clear on logout/timeout
//
// Thread Safety:
//   WPF is single-threaded (Dispatcher), so this static holder is safe.
//   If extended to multi-threaded contexts, add locking.
//
// ============================================================================

using System;

namespace LDMAS.Security
{
    /// <summary>
    /// Holds the current authenticated user session state.
    /// Provides application-wide access via <see cref="CurrentUser"/>
    /// and manages session lifecycle through <see cref="Login"/> and
    /// <see cref="Logout"/> methods.
    /// </summary>
    public static class SessionManager
    {
        // =====================================================================
        // SESSION STATE
        // =====================================================================

        /// <summary>
        /// The currently authenticated user, or <c>null</c> if no user is
        /// logged in. All UI components read this property to determine
        /// access rights and display the user's identity.
        /// </summary>
        public static User? CurrentUser { get; private set; }

        /// <summary>
        /// Timestamp of when the current session was established.
        /// Used by the MainWindow session timeout timer (FR-005).
        /// </summary>
        public static DateTime? SessionStartedAt { get; private set; }

        /// <summary>
        /// Timestamp of the most recent user interaction.
        /// Updated by MainWindow on mouse/keyboard activity.
        /// </summary>
        public static DateTime? LastActivityAt { get; private set; }

        // =====================================================================
        // COMPUTED PROPERTIES
        // =====================================================================

        /// <summary>
        /// Returns <c>true</c> if a user is currently authenticated.
        /// </summary>
        public static bool IsAuthenticated => CurrentUser != null;

        /// <summary>
        /// Returns the primary role name of the current user, or "Guest".
        /// </summary>
        public static string CurrentRole =>
            CurrentUser?.Roles.Count > 0
                ? CurrentUser.Roles[0]
                : "Guest";

        /// <summary>
        /// Returns <c>true</c> if the current user holds the Admin role.
        /// </summary>
        public static bool IsAdmin => CurrentUser?.IsAdmin ?? false;

        /// <summary>
        /// Returns the display name, or "Not Logged In".
        /// Safe to bind directly in XAML via code-behind.
        /// </summary>
        public static string DisplayName =>
            CurrentUser?.FullName ?? "Not Logged In";

        // =====================================================================
        // SESSION LIFECYCLE
        // =====================================================================

        /// <summary>
        /// Establishes a new session for the authenticated user.
        /// Call this after <see cref="AuthenticationService.AuthenticateUser"/>
        /// returns a valid <see cref="User"/> object.
        /// </summary>
        /// <param name="user">The authenticated user from the BLL.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="user"/> is null.
        /// </exception>
        public static void Login(User user)
        {
            CurrentUser     = user ?? throw new ArgumentNullException(nameof(user));
            SessionStartedAt = DateTime.Now;
            LastActivityAt   = DateTime.Now;

            System.Diagnostics.Debug.WriteLine(
                $"[SessionManager] Session started for '{user.Username}' " +
                $"(Roles: {string.Join(", ", user.Roles)}) at {SessionStartedAt:HH:mm:ss}");
        }

        /// <summary>
        /// Terminates the current session and clears all user state.
        /// Call this on explicit logout or session timeout (FR-005).
        /// </summary>
        public static void Logout()
        {
            string previousUser = CurrentUser?.Username ?? "unknown";

            CurrentUser      = null;
            SessionStartedAt = null;
            LastActivityAt   = null;

            System.Diagnostics.Debug.WriteLine(
                $"[SessionManager] Session terminated for '{previousUser}' at {DateTime.Now:HH:mm:ss}");
        }

        /// <summary>
        /// Records a user interaction timestamp. Called by MainWindow on
        /// mouse/keyboard events to reset the session timeout clock.
        /// </summary>
        public static void RecordActivity()
        {
            LastActivityAt = DateTime.Now;
        }

        /// <summary>
        /// Checks whether the current session has been idle longer than
        /// the specified timeout. Used by the MainWindow DispatcherTimer.
        /// </summary>
        /// <param name="timeoutMinutes">Maximum idle time in minutes (default: 30).</param>
        /// <returns><c>true</c> if the session has expired.</returns>
        public static bool IsSessionExpired(int timeoutMinutes = 30)
        {
            if (LastActivityAt == null) return true;
            return (DateTime.Now - LastActivityAt.Value).TotalMinutes >= timeoutMinutes;
        }

        // =====================================================================
        // AUTHORIZATION HELPERS
        // =====================================================================

        /// <summary>
        /// Checks if the current user has a specific role.
        /// Returns <c>false</c> if no user is logged in.
        /// </summary>
        /// <param name="roleName">Role name (case-insensitive).</param>
        public static bool HasRole(string roleName)
        {
            return CurrentUser?.HasRole(roleName) ?? false;
        }

        /// <summary>
        /// Checks if the current user has at least one of the specified roles.
        /// Useful for gating UI modules that accept multiple role levels.
        /// </summary>
        /// <param name="roles">One or more role names to check.</param>
        /// <returns><c>true</c> if the user has any of the listed roles.</returns>
        public static bool HasAnyRole(params string[] roles)
        {
            if (CurrentUser == null) return false;
            foreach (var role in roles)
            {
                if (CurrentUser.HasRole(role)) return true;
            }
            return false;
        }
    }
}
