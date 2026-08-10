// ============================================================================
// INDEPENDENT LABORATORY DATA MANAGEMENT AND AUTOMATION SYSTEM (LDMAS)
// AuthenticationService.cs — User Registration, Login & RBAC Enforcement
// ============================================================================
// Author          : Salih
// Date            : 2026-08-10
// Layer           : Security / Business Logic Layer (BLL)
// Dependencies    : BCrypt.Net-Next (NuGet), MySql.Data (NuGet)
// Requirement Ref : FR-001 (user CRUD), FR-002 (authentication),
//                   FR-003 (password hashing), FR-004 (RBAC),
//                   FR-005 (session timeout), FR-006 (auth logging),
//                   NFR-001 (bcrypt cost ≥ 12), NFR-002 (parameterized queries)
// ============================================================================

using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using LDMAS.DataAccess;

namespace LDMAS.Security
{
    // =========================================================================
    // MODELS
    // =========================================================================

    /// <summary>
    /// Represents a user entity mapped to the <c>Users</c> table.
    /// Used as the return type for authentication and user management operations.
    /// </summary>
    public class User
    {
        /// <summary>Auto-incremented primary key (<c>user_id</c>).</summary>
        public int UserId { get; set; }

        /// <summary>Unique login username.</summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>Unique email address.</summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>User's first name.</summary>
        public string FirstName { get; set; } = string.Empty;

        /// <summary>User's last name.</summary>
        public string LastName { get; set; } = string.Empty;

        /// <summary>Contact phone number (optional).</summary>
        public string? Phone { get; set; }

        /// <summary>Department or lab section (optional).</summary>
        public string? Department { get; set; }

        /// <summary>Account active flag (soft-delete mechanism).</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>Timestamp of last successful login.</summary>
        public DateTime? LastLoginAt { get; set; }

        /// <summary>Record creation timestamp.</summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>Last modification timestamp.</summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// List of role names assigned to this user (populated after authentication).
        /// Example: ["Admin", "Manager"]
        /// </summary>
        public List<string> Roles { get; set; } = new List<string>();

        /// <summary>Computed full name for display purposes.</summary>
        public string FullName => $"{FirstName} {LastName}";

        /// <summary>Checks whether the user holds a specific role.</summary>
        /// <param name="roleName">The role name to check (case-insensitive).</param>
        public bool HasRole(string roleName)
        {
            return Roles.Exists(r => r.Equals(roleName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Checks whether the user holds the Admin role.</summary>
        public bool IsAdmin => HasRole("Admin");

        /// <summary>Checks whether the user holds the Manager role.</summary>
        public bool IsManager => HasRole("Manager");

        /// <summary>Checks whether the user holds the Technician role.</summary>
        public bool IsTechnician => HasRole("Technician");

        /// <summary>Checks whether the user holds the Auditor role.</summary>
        public bool IsAuditor => HasRole("Auditor");
    }

    /// <summary>
    /// Encapsulates the data required to register a new user.
    /// Keeps raw password separate from the <see cref="User"/> model
    /// which never carries plaintext credentials.
    /// </summary>
    public class UserRegistrationRequest
    {
        /// <summary>Desired username (must be unique).</summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>Email address (must be unique).</summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>Plaintext password — will be hashed before storage.</summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>User's first name.</summary>
        public string FirstName { get; set; } = string.Empty;

        /// <summary>User's last name.</summary>
        public string LastName { get; set; } = string.Empty;

        /// <summary>Contact phone number (optional).</summary>
        public string? Phone { get; set; }

        /// <summary>Department or lab section (optional).</summary>
        public string? Department { get; set; }

        /// <summary>
        /// Role name to assign upon registration.
        /// Defaults to "Technician" — the least-privileged operational role.
        /// </summary>
        public string RoleName { get; set; } = "Technician";
    }

    /// <summary>
    /// Encapsulates the result of an authentication attempt, providing
    /// structured success/failure information alongside the authenticated user.
    /// </summary>
    public class AuthenticationResult
    {
        /// <summary>Whether the authentication was successful.</summary>
        public bool IsSuccess { get; set; }

        /// <summary>Human-readable message describing the result.</summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// The authenticated user (populated only on success).
        /// Includes role assignments for RBAC enforcement.
        /// </summary>
        public User? AuthenticatedUser { get; set; }
    }

    // =========================================================================
    // AUTHENTICATION SERVICE
    // =========================================================================

    /// <summary>
    /// Core security service responsible for user registration, authentication,
    /// password management, and role-based access control (RBAC) enforcement.
    /// 
    /// <para><b>Password Security (FR-003, NFR-001):</b></para>
    /// <list type="bullet">
    ///   <item>All passwords are hashed using <b>BCrypt</b> with a configurable
    ///         work factor (default: 12, per NFR-001).</item>
    ///   <item>Plaintext passwords are <b>never stored, logged, or returned</b>
    ///         by any method in this class.</item>
    ///   <item>Password verification uses BCrypt's constant-time comparison
    ///         to prevent timing attacks.</item>
    /// </list>
    /// 
    /// <para><b>SQL Injection Prevention (NFR-002):</b></para>
    /// All database queries use <see cref="MySqlParameter"/> parameterization.
    /// 
    /// <para><b>Audit Logging (FR-006):</b></para>
    /// Every authentication attempt (success or failure) is logged to the
    /// <c>AuthenticationLog</c> table with timestamp and metadata.
    /// </summary>
    public class AuthenticationService
    {
        // =====================================================================
        // CONSTANTS
        // =====================================================================

        /// <summary>
        /// BCrypt work factor (cost). A value of 12 means 2^12 = 4096 iterations.
        /// NFR-001 mandates a minimum of 12.
        /// Higher values increase computation time exponentially, improving
        /// resistance to brute-force attacks at the cost of login latency.
        /// </summary>
        private const int BCRYPT_WORK_FACTOR = 12;

        /// <summary>
        /// Minimum password length enforced during registration.
        /// Aligns with OWASP recommendations for password policy.
        /// </summary>
        private const int MIN_PASSWORD_LENGTH = 8;

        /// <summary>
        /// Maximum consecutive failed login attempts before account lockout
        /// consideration. Currently logged only — lockout enforcement is
        /// planned for Week 5.
        /// </summary>
        private const int MAX_FAILED_ATTEMPTS = 5;

        // =====================================================================
        // SQL COMMAND CONSTANTS
        // =====================================================================

        private const string SQL_INSERT_USER = @"
            INSERT INTO Users 
                (username, email, password_hash, first_name, last_name, phone, department, is_active)
            VALUES 
                (@Username, @Email, @PasswordHash, @FirstName, @LastName, @Phone, @Department, TRUE);
            SELECT LAST_INSERT_ID();";

        private const string SQL_SELECT_BY_USERNAME = @"
            SELECT 
                user_id, username, email, password_hash, first_name, last_name,
                phone, department, is_active, last_login_at, created_at, updated_at
            FROM Users 
            WHERE username = @Username;";

        private const string SQL_SELECT_BY_EMAIL = @"
            SELECT 
                user_id, username, email, password_hash, first_name, last_name,
                phone, department, is_active, last_login_at, created_at, updated_at
            FROM Users 
            WHERE email = @Email;";

        private const string SQL_SELECT_BY_ID = @"
            SELECT 
                user_id, username, email, password_hash, first_name, last_name,
                phone, department, is_active, last_login_at, created_at, updated_at
            FROM Users 
            WHERE user_id = @UserId;";

        private const string SQL_CHECK_USERNAME_EXISTS = @"
            SELECT COUNT(1) FROM Users WHERE username = @Username;";

        private const string SQL_CHECK_EMAIL_EXISTS = @"
            SELECT COUNT(1) FROM Users WHERE email = @Email;";

        private const string SQL_GET_USER_ROLES = @"
            SELECT r.role_name 
            FROM UserRoles ur
            INNER JOIN Roles r ON ur.role_id = r.role_id
            WHERE ur.user_id = @UserId AND r.is_active = TRUE;";

        private const string SQL_ASSIGN_ROLE = @"
            INSERT INTO UserRoles (user_id, role_id, assigned_by)
            SELECT @UserId, role_id, @AssignedBy
            FROM Roles 
            WHERE role_name = @RoleName AND is_active = TRUE;";

        private const string SQL_UPDATE_LAST_LOGIN = @"
            UPDATE Users SET last_login_at = NOW() WHERE user_id = @UserId;";

        private const string SQL_LOG_AUTH_ATTEMPT = @"
            INSERT INTO AuthenticationLog 
                (user_id, username, attempt_result, ip_address, user_agent)
            VALUES 
                (@UserId, @Username, @AttemptResult, @IpAddress, @UserAgent);";

        private const string SQL_UPDATE_PASSWORD = @"
            UPDATE Users SET password_hash = @PasswordHash WHERE user_id = @UserId;";

        private const string SQL_DEACTIVATE_USER = @"
            UPDATE Users SET is_active = FALSE WHERE user_id = @UserId;";

        private const string SQL_ACTIVATE_USER = @"
            UPDATE Users SET is_active = TRUE WHERE user_id = @UserId;";

        private const string SQL_GET_ALL_USERS = @"
            SELECT 
                user_id, username, email, first_name, last_name,
                phone, department, is_active, last_login_at, created_at, updated_at
            FROM Users
            ORDER BY created_at DESC;";

        private const string SQL_COUNT_FAILED_ATTEMPTS = @"
            SELECT COUNT(*) FROM AuthenticationLog
            WHERE username = @Username 
              AND attempt_result = 'Failure'
              AND attempted_at > DATE_SUB(NOW(), INTERVAL 30 MINUTE);";

        // =====================================================================
        // USER REGISTRATION
        // =====================================================================

        /// <summary>
        /// Registers a new user in the system with a securely hashed password.
        /// 
        /// <para><b>Process:</b></para>
        /// <list type="number">
        ///   <item>Validates all required fields and password strength.</item>
        ///   <item>Checks for duplicate username and email.</item>
        ///   <item>Hashes the plaintext password using BCrypt (cost factor = 12).</item>
        ///   <item>Inserts the user record into the <c>Users</c> table.</item>
        ///   <item>Assigns the specified role via the <c>UserRoles</c> junction table.</item>
        /// </list>
        /// 
        /// <para><b>Security:</b> The plaintext password exists only in memory
        /// during this method call and is never persisted or logged.</para>
        /// </summary>
        /// <param name="request">The registration data including plaintext password.</param>
        /// <param name="assignedByUserId">
        /// The <c>user_id</c> of the admin performing the registration.
        /// Pass <c>null</c> for self-registration (if enabled).
        /// </param>
        /// <returns>
        /// The <c>user_id</c> of the newly created user, or <c>-1</c> on failure.
        /// </returns>
        public int RegisterUser(UserRegistrationRequest request, int? assignedByUserId = null)
        {
            // -----------------------------------------------------------------
            // Step 1: Input Validation
            // -----------------------------------------------------------------
            var validationError = ValidateRegistrationRequest(request);
            if (validationError != null)
            {
                Console.Error.WriteLine($"[AuthService] Registration failed — {validationError}");
                return -1;
            }

            try
            {
                using (var connection = DatabaseConnection.Instance.CreateConnection())
                {
                    connection.Open();

                    // ---------------------------------------------------------
                    // Step 2: Check for Duplicate Username
                    // ---------------------------------------------------------
                    if (UsernameExists(connection, request.Username))
                    {
                        Console.Error.WriteLine($"[AuthService] Registration failed — Username '{request.Username}' already exists.");
                        return -1;
                    }

                    // ---------------------------------------------------------
                    // Step 3: Check for Duplicate Email
                    // ---------------------------------------------------------
                    if (EmailExists(connection, request.Email))
                    {
                        Console.Error.WriteLine($"[AuthService] Registration failed — Email '{request.Email}' already exists.");
                        return -1;
                    }

                    // ---------------------------------------------------------
                    // Step 4: Hash Password with BCrypt
                    // ---------------------------------------------------------
                    // BCrypt.Net.BCrypt.EnhancedHashPassword generates a random
                    // salt and embeds it in the hash string automatically.
                    // Format: $2a$12$<22-char-salt><31-char-hash>
                    // ---------------------------------------------------------
                    string passwordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(
                        request.Password,
                        BCRYPT_WORK_FACTOR
                    );

                    // ---------------------------------------------------------
                    // Step 5: Insert User Record (within transaction for atomicity)
                    // ---------------------------------------------------------
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            int newUserId;

                            // Insert the user
                            using (var cmd = new MySqlCommand(SQL_INSERT_USER, connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@Username",     request.Username);
                                cmd.Parameters.AddWithValue("@Email",        request.Email);
                                cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
                                cmd.Parameters.AddWithValue("@FirstName",    request.FirstName);
                                cmd.Parameters.AddWithValue("@LastName",     request.LastName);
                                cmd.Parameters.AddWithValue("@Phone",        (object?)request.Phone      ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@Department",   (object?)request.Department ?? DBNull.Value);

                                newUserId = Convert.ToInt32(cmd.ExecuteScalar());
                            }

                            // -------------------------------------------------
                            // Step 6: Assign Role
                            // -------------------------------------------------
                            using (var roleCmd = new MySqlCommand(SQL_ASSIGN_ROLE, connection, transaction))
                            {
                                roleCmd.Parameters.AddWithValue("@UserId",     newUserId);
                                roleCmd.Parameters.AddWithValue("@RoleName",   request.RoleName);
                                roleCmd.Parameters.AddWithValue("@AssignedBy", (object?)assignedByUserId ?? DBNull.Value);

                                int rolesAssigned = roleCmd.ExecuteNonQuery();
                                if (rolesAssigned == 0)
                                {
                                    Console.Error.WriteLine($"[AuthService] Warning — Role '{request.RoleName}' not found. User created without role assignment.");
                                }
                            }

                            transaction.Commit();

                            Console.WriteLine($"[AuthService] User registered successfully — ID={newUserId}, Username='{request.Username}', Role='{request.RoleName}'");
                            return newUserId;
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw; // Re-throw to outer catch
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                Console.Error.WriteLine($"[AuthService] Registration error — {ex.Message}");
                return -1;
            }
        }

        // =====================================================================
        // USER AUTHENTICATION (LOGIN)
        // =====================================================================

        /// <summary>
        /// Authenticates a user by verifying their username and password against
        /// the database. On success, loads the user's role assignments for RBAC.
        /// 
        /// <para><b>Process:</b></para>
        /// <list type="number">
        ///   <item>Looks up the user record by username.</item>
        ///   <item>Checks account active status.</item>
        ///   <item>Verifies the plaintext password against the stored BCrypt hash.</item>
        ///   <item>Loads assigned roles from <c>UserRoles</c> + <c>Roles</c> tables.</item>
        ///   <item>Updates <c>last_login_at</c> timestamp.</item>
        ///   <item>Logs the authentication attempt (success or failure) to <c>AuthenticationLog</c>.</item>
        /// </list>
        /// 
        /// <para><b>Timing Safety:</b> BCrypt's <c>Verify</c> method uses
        /// constant-time comparison internally, preventing timing-based
        /// side-channel attacks on password verification.</para>
        /// </summary>
        /// <param name="username">The username to authenticate.</param>
        /// <param name="password">The plaintext password to verify.</param>
        /// <param name="ipAddress">Client IP address for audit logging (optional).</param>
        /// <param name="userAgent">Client user-agent string for audit logging (optional).</param>
        /// <returns>
        /// An <see cref="AuthenticationResult"/> containing the outcome and,
        /// on success, the fully populated <see cref="User"/> with role data.
        /// </returns>
        public AuthenticationResult AuthenticateUser(
            string username,
            string password,
            string? ipAddress = null,
            string? userAgent = null)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return new AuthenticationResult
                {
                    IsSuccess = false,
                    Message = "Username and password are required."
                };
            }

            try
            {
                using (var connection = DatabaseConnection.Instance.CreateConnection())
                {
                    connection.Open();

                    // ---------------------------------------------------------
                    // Step 1: Check Recent Failed Attempts (Brute-Force Mitigation)
                    // ---------------------------------------------------------
                    int recentFailures = GetRecentFailedAttempts(connection, username);
                    if (recentFailures >= MAX_FAILED_ATTEMPTS)
                    {
                        LogAuthenticationAttempt(connection, null, username, "Failure", ipAddress, userAgent);

                        return new AuthenticationResult
                        {
                            IsSuccess = false,
                            Message = $"Account temporarily locked due to {MAX_FAILED_ATTEMPTS} consecutive failed attempts. Please try again in 30 minutes."
                        };
                    }

                    // ---------------------------------------------------------
                    // Step 2: Look Up User by Username
                    // ---------------------------------------------------------
                    string? storedHash = null;
                    User? user = null;

                    using (var cmd = new MySqlCommand(SQL_SELECT_BY_USERNAME, connection))
                    {
                        cmd.Parameters.AddWithValue("@Username", username);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                storedHash = reader.GetString("password_hash");
                                user = MapReaderToUser(reader);
                            }
                        }
                    }

                    // Username not found
                    if (user == null || storedHash == null)
                    {
                        // Still perform a dummy BCrypt hash to prevent timing attacks
                        // that could reveal whether a username exists
                        BCrypt.Net.BCrypt.EnhancedHashPassword("dummy_timing_equalization", BCRYPT_WORK_FACTOR);

                        LogAuthenticationAttempt(connection, null, username, "Failure", ipAddress, userAgent);

                        return new AuthenticationResult
                        {
                            IsSuccess = false,
                            Message = "Invalid username or password."
                        };
                    }

                    // ---------------------------------------------------------
                    // Step 3: Check Account Active Status
                    // ---------------------------------------------------------
                    if (!user.IsActive)
                    {
                        LogAuthenticationAttempt(connection, user.UserId, username, "Failure", ipAddress, userAgent);

                        return new AuthenticationResult
                        {
                            IsSuccess = false,
                            Message = "This account has been deactivated. Please contact an administrator."
                        };
                    }

                    // ---------------------------------------------------------
                    // Step 4: Verify Password Against BCrypt Hash
                    // ---------------------------------------------------------
                    bool isPasswordValid = BCrypt.Net.BCrypt.EnhancedVerify(password, storedHash);

                    if (!isPasswordValid)
                    {
                        LogAuthenticationAttempt(connection, user.UserId, username, "Failure", ipAddress, userAgent);

                        return new AuthenticationResult
                        {
                            IsSuccess = false,
                            Message = "Invalid username or password."
                        };
                    }

                    // ---------------------------------------------------------
                    // Step 5: Load User Roles (RBAC — FR-004)
                    // ---------------------------------------------------------
                    user.Roles = GetUserRoles(connection, user.UserId);

                    // ---------------------------------------------------------
                    // Step 6: Update Last Login Timestamp
                    // ---------------------------------------------------------
                    UpdateLastLogin(connection, user.UserId);
                    user.LastLoginAt = DateTime.Now;

                    // ---------------------------------------------------------
                    // Step 7: Log Successful Authentication (FR-006)
                    // ---------------------------------------------------------
                    LogAuthenticationAttempt(connection, user.UserId, username, "Success", ipAddress, userAgent);

                    Console.WriteLine($"[AuthService] Authentication successful — User='{username}', Roles=[{string.Join(", ", user.Roles)}]");

                    return new AuthenticationResult
                    {
                        IsSuccess = true,
                        Message = "Authentication successful.",
                        AuthenticatedUser = user
                    };
                }
            }
            catch (MySqlException ex)
            {
                Console.Error.WriteLine($"[AuthService] Authentication error — {ex.Message}");

                return new AuthenticationResult
                {
                    IsSuccess = false,
                    Message = "An internal error occurred during authentication. Please try again."
                };
            }
        }

        // =====================================================================
        // PASSWORD MANAGEMENT
        // =====================================================================

        /// <summary>
        /// Changes a user's password after verifying their current password.
        /// The new password is hashed with BCrypt before storage.
        /// </summary>
        /// <param name="userId">The <c>user_id</c> of the user changing their password.</param>
        /// <param name="currentPassword">The user's current plaintext password for verification.</param>
        /// <param name="newPassword">The desired new plaintext password.</param>
        /// <returns><c>true</c> if the password was changed successfully.</returns>
        public bool ChangePassword(int userId, string currentPassword, string newPassword)
        {
            // Validate new password strength
            var strengthError = ValidatePasswordStrength(newPassword);
            if (strengthError != null)
            {
                Console.Error.WriteLine($"[AuthService] Password change failed — {strengthError}");
                return false;
            }

            try
            {
                using (var connection = DatabaseConnection.Instance.CreateConnection())
                {
                    connection.Open();

                    // Retrieve current hash
                    string? currentHash = null;
                    using (var cmd = new MySqlCommand(SQL_SELECT_BY_ID, connection))
                    {
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                currentHash = reader.GetString("password_hash");
                            }
                        }
                    }

                    if (currentHash == null)
                    {
                        Console.Error.WriteLine($"[AuthService] Password change failed — User ID={userId} not found.");
                        return false;
                    }

                    // Verify current password
                    if (!BCrypt.Net.BCrypt.EnhancedVerify(currentPassword, currentHash))
                    {
                        Console.Error.WriteLine("[AuthService] Password change failed — Current password is incorrect.");
                        return false;
                    }

                    // Hash and store new password
                    string newHash = BCrypt.Net.BCrypt.EnhancedHashPassword(newPassword, BCRYPT_WORK_FACTOR);

                    using (var cmd = new MySqlCommand(SQL_UPDATE_PASSWORD, connection))
                    {
                        cmd.Parameters.AddWithValue("@PasswordHash", newHash);
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        cmd.ExecuteNonQuery();
                    }

                    Console.WriteLine($"[AuthService] Password changed successfully for User ID={userId}.");
                    return true;
                }
            }
            catch (MySqlException ex)
            {
                Console.Error.WriteLine($"[AuthService] Password change error — {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Resets a user's password to a new value without requiring the
        /// current password. Intended for administrator-initiated resets only.
        /// </summary>
        /// <param name="userId">The <c>user_id</c> whose password will be reset.</param>
        /// <param name="newPassword">The new plaintext password.</param>
        /// <returns><c>true</c> if the reset was successful.</returns>
        public bool ResetPassword(int userId, string newPassword)
        {
            var strengthError = ValidatePasswordStrength(newPassword);
            if (strengthError != null)
            {
                Console.Error.WriteLine($"[AuthService] Password reset failed — {strengthError}");
                return false;
            }

            try
            {
                using (var connection = DatabaseConnection.Instance.CreateConnection())
                {
                    connection.Open();

                    string newHash = BCrypt.Net.BCrypt.EnhancedHashPassword(newPassword, BCRYPT_WORK_FACTOR);

                    using (var cmd = new MySqlCommand(SQL_UPDATE_PASSWORD, connection))
                    {
                        cmd.Parameters.AddWithValue("@PasswordHash", newHash);
                        cmd.Parameters.AddWithValue("@UserId", userId);

                        int rows = cmd.ExecuteNonQuery();
                        if (rows == 1)
                        {
                            Console.WriteLine($"[AuthService] Password reset completed for User ID={userId}.");
                            return true;
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                Console.Error.WriteLine($"[AuthService] Password reset error — {ex.Message}");
            }

            return false;
        }

        // =====================================================================
        // USER MANAGEMENT
        // =====================================================================

        /// <summary>
        /// Retrieves a user by their <c>user_id</c> with role assignments.
        /// </summary>
        public User? GetUserById(int userId)
        {
            try
            {
                using (var connection = DatabaseConnection.Instance.CreateConnection())
                {
                    connection.Open();

                    User? user = null;
                    using (var cmd = new MySqlCommand(SQL_SELECT_BY_ID, connection))
                    {
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                user = MapReaderToUser(reader);
                            }
                        }
                    }

                    if (user != null)
                    {
                        user.Roles = GetUserRoles(connection, user.UserId);
                    }

                    return user;
                }
            }
            catch (MySqlException ex)
            {
                Console.Error.WriteLine($"[AuthService] GetUserById({userId}) failed — {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Retrieves all users in the system (without password hashes).
        /// Intended for the Admin user management interface.
        /// </summary>
        public List<User> GetAllUsers()
        {
            var users = new List<User>();

            try
            {
                using (var connection = DatabaseConnection.Instance.CreateConnection())
                {
                    connection.Open();

                    using (var cmd = new MySqlCommand(SQL_GET_ALL_USERS, connection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            users.Add(MapReaderToUserWithoutHash(reader));
                        }
                    }

                    // Load roles for each user
                    foreach (var user in users)
                    {
                        user.Roles = GetUserRoles(connection, user.UserId);
                    }
                }
            }
            catch (MySqlException ex)
            {
                Console.Error.WriteLine($"[AuthService] GetAllUsers failed — {ex.Message}");
            }

            return users;
        }

        /// <summary>
        /// Deactivates a user account (soft delete). The account remains in the
        /// database but cannot authenticate. Satisfies FR-001 (deactivate accounts).
        /// </summary>
        /// <param name="userId">The <c>user_id</c> to deactivate.</param>
        /// <returns><c>true</c> if the account was deactivated.</returns>
        public bool DeactivateUser(int userId)
        {
            try
            {
                using (var connection = DatabaseConnection.Instance.CreateConnection())
                {
                    connection.Open();

                    using (var cmd = new MySqlCommand(SQL_DEACTIVATE_USER, connection))
                    {
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        int rows = cmd.ExecuteNonQuery();

                        if (rows == 1)
                        {
                            Console.WriteLine($"[AuthService] User ID={userId} deactivated.");
                            return true;
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                Console.Error.WriteLine($"[AuthService] DeactivateUser({userId}) failed — {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Reactivates a previously deactivated user account.
        /// </summary>
        /// <param name="userId">The <c>user_id</c> to reactivate.</param>
        /// <returns><c>true</c> if the account was reactivated.</returns>
        public bool ActivateUser(int userId)
        {
            try
            {
                using (var connection = DatabaseConnection.Instance.CreateConnection())
                {
                    connection.Open();

                    using (var cmd = new MySqlCommand(SQL_ACTIVATE_USER, connection))
                    {
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        int rows = cmd.ExecuteNonQuery();

                        if (rows == 1)
                        {
                            Console.WriteLine($"[AuthService] User ID={userId} reactivated.");
                            return true;
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                Console.Error.WriteLine($"[AuthService] ActivateUser({userId}) failed — {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Assigns an additional role to an existing user.
        /// </summary>
        /// <param name="userId">The <c>user_id</c> to assign the role to.</param>
        /// <param name="roleName">The role name (e.g., "Manager", "Auditor").</param>
        /// <param name="assignedByUserId">The admin performing the assignment.</param>
        /// <returns><c>true</c> if the role was assigned successfully.</returns>
        public bool AssignRole(int userId, string roleName, int? assignedByUserId = null)
        {
            try
            {
                using (var connection = DatabaseConnection.Instance.CreateConnection())
                {
                    connection.Open();

                    using (var cmd = new MySqlCommand(SQL_ASSIGN_ROLE, connection))
                    {
                        cmd.Parameters.AddWithValue("@UserId",     userId);
                        cmd.Parameters.AddWithValue("@RoleName",   roleName);
                        cmd.Parameters.AddWithValue("@AssignedBy", (object?)assignedByUserId ?? DBNull.Value);

                        int rows = cmd.ExecuteNonQuery();
                        if (rows == 1)
                        {
                            Console.WriteLine($"[AuthService] Role '{roleName}' assigned to User ID={userId}.");
                            return true;
                        }
                        else
                        {
                            Console.Error.WriteLine($"[AuthService] Role assignment failed — Role '{roleName}' not found or already assigned.");
                            return false;
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                // MySQL error 1062 = duplicate entry (role already assigned)
                if (ex.Number == 1062)
                {
                    Console.Error.WriteLine($"[AuthService] Role '{roleName}' is already assigned to User ID={userId}.");
                    return false;
                }

                Console.Error.WriteLine($"[AuthService] AssignRole failed — {ex.Message}");
                return false;
            }
        }

        // =====================================================================
        // AUTHORIZATION HELPERS
        // =====================================================================

        /// <summary>
        /// Checks whether a user is authorized to perform an action based on
        /// their assigned roles. Used for RBAC enforcement at the BLL layer.
        /// </summary>
        /// <param name="user">The authenticated user.</param>
        /// <param name="requiredRoles">One or more role names that grant access.</param>
        /// <returns><c>true</c> if the user holds at least one of the required roles.</returns>
        public static bool IsAuthorized(User user, params string[] requiredRoles)
        {
            if (user == null || user.Roles == null || user.Roles.Count == 0)
                return false;

            // Admin role always has full access
            if (user.IsAdmin)
                return true;

            foreach (var requiredRole in requiredRoles)
            {
                if (user.HasRole(requiredRole))
                    return true;
            }

            return false;
        }

        // =====================================================================
        // PRIVATE HELPERS
        // =====================================================================

        /// <summary>
        /// Validates a <see cref="UserRegistrationRequest"/> for required fields
        /// and password strength before database operations.
        /// </summary>
        /// <returns>An error message string, or <c>null</c> if validation passes.</returns>
        private string? ValidateRegistrationRequest(UserRegistrationRequest request)
        {
            if (request == null)
                return "Registration request cannot be null.";

            if (string.IsNullOrWhiteSpace(request.Username))
                return "Username is required.";

            if (request.Username.Length < 3 || request.Username.Length > 100)
                return "Username must be between 3 and 100 characters.";

            if (string.IsNullOrWhiteSpace(request.Email))
                return "Email is required.";

            if (!request.Email.Contains("@") || !request.Email.Contains("."))
                return "Email format is invalid.";

            if (string.IsNullOrWhiteSpace(request.FirstName))
                return "First name is required.";

            if (string.IsNullOrWhiteSpace(request.LastName))
                return "Last name is required.";

            return ValidatePasswordStrength(request.Password);
        }

        /// <summary>
        /// Validates password strength against LDMAS security requirements.
        /// </summary>
        /// <returns>An error message string, or <c>null</c> if the password is acceptable.</returns>
        private string? ValidatePasswordStrength(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return "Password is required.";

            if (password.Length < MIN_PASSWORD_LENGTH)
                return $"Password must be at least {MIN_PASSWORD_LENGTH} characters long.";

            bool hasUpper   = false;
            bool hasLower   = false;
            bool hasDigit   = false;
            bool hasSpecial = false;

            foreach (char c in password)
            {
                if (char.IsUpper(c))       hasUpper   = true;
                else if (char.IsLower(c))  hasLower   = true;
                else if (char.IsDigit(c))  hasDigit   = true;
                else                       hasSpecial = true;
            }

            if (!hasUpper)   return "Password must contain at least one uppercase letter.";
            if (!hasLower)   return "Password must contain at least one lowercase letter.";
            if (!hasDigit)   return "Password must contain at least one digit.";
            if (!hasSpecial) return "Password must contain at least one special character.";

            return null; // All checks passed
        }

        /// <summary>
        /// Checks whether a username already exists in the database.
        /// </summary>
        private bool UsernameExists(MySqlConnection connection, string username)
        {
            using (var cmd = new MySqlCommand(SQL_CHECK_USERNAME_EXISTS, connection))
            {
                cmd.Parameters.AddWithValue("@Username", username);
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        /// <summary>
        /// Checks whether an email already exists in the database.
        /// </summary>
        private bool EmailExists(MySqlConnection connection, string email)
        {
            using (var cmd = new MySqlCommand(SQL_CHECK_EMAIL_EXISTS, connection))
            {
                cmd.Parameters.AddWithValue("@Email", email);
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        /// <summary>
        /// Loads all active role names assigned to a user.
        /// </summary>
        private List<string> GetUserRoles(MySqlConnection connection, int userId)
        {
            var roles = new List<string>();

            using (var cmd = new MySqlCommand(SQL_GET_USER_ROLES, connection))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        roles.Add(reader.GetString("role_name"));
                    }
                }
            }

            return roles;
        }

        /// <summary>
        /// Updates the <c>last_login_at</c> timestamp for a user.
        /// </summary>
        private void UpdateLastLogin(MySqlConnection connection, int userId)
        {
            using (var cmd = new MySqlCommand(SQL_UPDATE_LAST_LOGIN, connection))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Logs an authentication attempt (success or failure) to the
        /// <c>AuthenticationLog</c> table for security auditing (FR-006).
        /// </summary>
        private void LogAuthenticationAttempt(
            MySqlConnection connection,
            int? userId,
            string username,
            string attemptResult,
            string? ipAddress,
            string? userAgent)
        {
            try
            {
                using (var cmd = new MySqlCommand(SQL_LOG_AUTH_ATTEMPT, connection))
                {
                    cmd.Parameters.AddWithValue("@UserId",        (object?)userId    ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Username",      username);
                    cmd.Parameters.AddWithValue("@AttemptResult", attemptResult);
                    cmd.Parameters.AddWithValue("@IpAddress",     (object?)ipAddress ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@UserAgent",     (object?)userAgent ?? DBNull.Value);

                    cmd.ExecuteNonQuery();
                }
            }
            catch (MySqlException ex)
            {
                // Auth logging failure should never prevent the login flow
                Console.Error.WriteLine($"[AuthService] Warning — Failed to log auth attempt: {ex.Message}");
            }
        }

        /// <summary>
        /// Counts failed login attempts for a username within the last 30 minutes.
        /// Used for brute-force detection.
        /// </summary>
        private int GetRecentFailedAttempts(MySqlConnection connection, string username)
        {
            using (var cmd = new MySqlCommand(SQL_COUNT_FAILED_ATTEMPTS, connection))
            {
                cmd.Parameters.AddWithValue("@Username", username);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        /// <summary>
        /// Maps a <see cref="MySqlDataReader"/> row to a <see cref="User"/> object.
        /// Includes the <c>password_hash</c> column for internal use only.
        /// </summary>
        private static User MapReaderToUser(MySqlDataReader reader)
        {
            return new User
            {
                UserId      = reader.GetInt32("user_id"),
                Username    = reader.GetString("username"),
                Email       = reader.GetString("email"),
                FirstName   = reader.GetString("first_name"),
                LastName    = reader.GetString("last_name"),
                Phone       = reader.IsDBNull(reader.GetOrdinal("phone"))
                                  ? null : reader.GetString("phone"),
                Department  = reader.IsDBNull(reader.GetOrdinal("department"))
                                  ? null : reader.GetString("department"),
                IsActive    = reader.GetBoolean("is_active"),
                LastLoginAt = reader.IsDBNull(reader.GetOrdinal("last_login_at"))
                                  ? null : reader.GetDateTime("last_login_at"),
                CreatedAt   = reader.GetDateTime("created_at"),
                UpdatedAt   = reader.GetDateTime("updated_at")
            };
        }

        /// <summary>
        /// Maps a <see cref="MySqlDataReader"/> row to a <see cref="User"/> object
        /// from the <c>SQL_GET_ALL_USERS</c> query which excludes <c>password_hash</c>.
        /// </summary>
        private static User MapReaderToUserWithoutHash(MySqlDataReader reader)
        {
            return new User
            {
                UserId      = reader.GetInt32("user_id"),
                Username    = reader.GetString("username"),
                Email       = reader.GetString("email"),
                FirstName   = reader.GetString("first_name"),
                LastName    = reader.GetString("last_name"),
                Phone       = reader.IsDBNull(reader.GetOrdinal("phone"))
                                  ? null : reader.GetString("phone"),
                Department  = reader.IsDBNull(reader.GetOrdinal("department"))
                                  ? null : reader.GetString("department"),
                IsActive    = reader.GetBoolean("is_active"),
                LastLoginAt = reader.IsDBNull(reader.GetOrdinal("last_login_at"))
                                  ? null : reader.GetDateTime("last_login_at"),
                CreatedAt   = reader.GetDateTime("created_at"),
                UpdatedAt   = reader.GetDateTime("updated_at")
            };
        }
    }
}
