// ============================================================================
// INDEPENDENT LABORATORY DATA MANAGEMENT AND AUTOMATION SYSTEM (LDMAS)
// DatabaseConnection.cs — Secure MySQL Connection Factory
// ============================================================================
// Author          : Salih
// Date            : 2026-08-10
// Layer           : Data Access Layer (DAL)
// Pattern         : Singleton Factory with Thread Safety
// Requirement Ref : NFR-002 (parameterized queries), NFR-004 (no hardcoded credentials)
// ============================================================================

using System;
using System.Configuration;
using MySql.Data.MySqlClient;

namespace LDMAS.DataAccess
{
    /// <summary>
    /// Provides a centralized, thread-safe factory for creating and managing
    /// MySQL database connections to the LDMAS database.
    /// 
    /// <para>
    /// This class implements the <b>Singleton pattern</b> with lazy initialization
    /// and double-checked locking to ensure a single instance manages all
    /// connection string configuration throughout the application lifecycle.
    /// </para>
    /// 
    /// <para><b>Security Notes (NFR-004):</b></para>
    /// <list type="bullet">
    ///   <item>Connection strings are read from App.config / appsettings — never hardcoded.</item>
    ///   <item>The password placeholder must be replaced via environment variable or user secrets.</item>
    ///   <item>All connections returned by this factory support parameterized queries (NFR-002).</item>
    /// </list>
    /// 
    /// <para><b>Usage:</b></para>
    /// <code>
    /// using (var connection = DatabaseConnection.Instance.CreateConnection())
    /// {
    ///     connection.Open();
    ///     // Execute parameterized queries here
    /// }
    /// </code>
    /// </summary>
    public sealed class DatabaseConnection
    {
        // =====================================================================
        // SINGLETON INFRASTRUCTURE
        // =====================================================================

        /// <summary>
        /// The single instance of <see cref="DatabaseConnection"/>.
        /// Initialized lazily upon first access.
        /// </summary>
        private static volatile DatabaseConnection? _instance;

        /// <summary>
        /// Lock object for thread-safe singleton initialization.
        /// </summary>
        private static readonly object _lock = new object();

        /// <summary>
        /// The fully constructed MySQL connection string.
        /// Built once during singleton initialization and reused for all connections.
        /// </summary>
        private readonly string _connectionString;

        // =====================================================================
        // CONNECTION STRING CONFIGURATION
        // =====================================================================
        // These defaults are used when no App.config entry is found.
        // In production, always configure via App.config or environment variables.
        // =====================================================================

        private const string DEFAULT_SERVER   = "localhost";
        private const string DEFAULT_PORT     = "3306";
        private const string DEFAULT_DATABASE = "ldmas_db";
        private const string DEFAULT_USER     = "root";

        // =====================================================================
        // CONSTRUCTOR (Private — Singleton enforcement)
        // =====================================================================

        /// <summary>
        /// Private constructor. Builds the connection string from configuration
        /// sources with the following precedence:
        /// <list type="number">
        ///   <item>Environment variable <c>LDMAS_DB_PASSWORD</c></item>
        ///   <item>App.config <c>connectionStrings</c> section</item>
        ///   <item>App.config <c>appSettings</c> individual keys</item>
        ///   <item>Hardcoded defaults (server, port, database, user only)</item>
        /// </list>
        /// </summary>
        private DatabaseConnection()
        {
            _connectionString = BuildConnectionString();
        }

        // =====================================================================
        // PUBLIC SINGLETON ACCESSOR
        // =====================================================================

        /// <summary>
        /// Gets the singleton instance of <see cref="DatabaseConnection"/>.
        /// Thread-safe via double-checked locking.
        /// </summary>
        public static DatabaseConnection Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new DatabaseConnection();
                        }
                    }
                }
                return _instance;
            }
        }

        // =====================================================================
        // PUBLIC METHODS
        // =====================================================================

        /// <summary>
        /// Creates a new <see cref="MySqlConnection"/> instance using the
        /// preconfigured connection string.
        /// 
        /// <para>
        /// The caller is responsible for opening, using, and disposing this
        /// connection. Always wrap in a <c>using</c> statement to ensure
        /// proper resource cleanup.
        /// </para>
        /// </summary>
        /// <returns>
        /// A new, unopened <see cref="MySqlConnection"/> ready for use.
        /// </returns>
        /// <example>
        /// <code>
        /// using (var conn = DatabaseConnection.Instance.CreateConnection())
        /// {
        ///     conn.Open();
        ///     using (var cmd = new MySqlCommand("SELECT * FROM Equipment WHERE status = @status", conn))
        ///     {
        ///         cmd.Parameters.AddWithValue("@status", "Active");
        ///         using (var reader = cmd.ExecuteReader())
        ///         {
        ///             while (reader.Read()) { /* process rows */ }
        ///         }
        ///     }
        /// }
        /// </code>
        /// </example>
        public MySqlConnection CreateConnection()
        {
            return new MySqlConnection(_connectionString);
        }

        /// <summary>
        /// Tests the database connectivity by attempting to open and immediately
        /// close a connection. Useful for startup health checks.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the connection was successfully established;
        /// <c>false</c> otherwise.
        /// </returns>
        public bool TestConnection()
        {
            try
            {
                using (var connection = CreateConnection())
                {
                    connection.Open();
                    Console.WriteLine("[DatabaseConnection] Connection test PASSED — " +
                                      $"Server: {connection.ServerVersion}");
                    return true;
                }
            }
            catch (MySqlException ex)
            {
                Console.Error.WriteLine($"[DatabaseConnection] Connection test FAILED — {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Returns the configured connection string with the password masked.
        /// For diagnostic/logging purposes only — never log the actual password.
        /// </summary>
        /// <returns>A masked version of the connection string.</returns>
        public string GetMaskedConnectionString()
        {
            var builder = new MySqlConnectionStringBuilder(_connectionString)
            {
                Password = "********"
            };
            return builder.ConnectionString;
        }

        // =====================================================================
        // PRIVATE HELPERS
        // =====================================================================

        /// <summary>
        /// Builds the MySQL connection string using a layered configuration strategy.
        /// Environment variables take highest precedence for sensitive values.
        /// </summary>
        private static string BuildConnectionString()
        {
            // --- Step 1: Try reading a full connection string from App.config ---
            string? configConnStr = null;
            try
            {
                configConnStr = ConfigurationManager.ConnectionStrings["LdmasDb"]?.ConnectionString;
            }
            catch (ConfigurationErrorsException)
            {
                // App.config not available or malformed — fall through to manual build
            }

            if (!string.IsNullOrWhiteSpace(configConnStr))
            {
                // Override password with environment variable if set
                var envPassword = Environment.GetEnvironmentVariable("LDMAS_DB_PASSWORD");
                if (!string.IsNullOrEmpty(envPassword))
                {
                    var builder = new MySqlConnectionStringBuilder(configConnStr)
                    {
                        Password = envPassword
                    };
                    return builder.ConnectionString;
                }
                return configConnStr;
            }

            // --- Step 2: Build manually from individual App.config keys / defaults ---
            string server   = GetConfigValue("DbServer",   DEFAULT_SERVER);
            string port     = GetConfigValue("DbPort",     DEFAULT_PORT);
            string database = GetConfigValue("DbName",     DEFAULT_DATABASE);
            string userId   = GetConfigValue("DbUser",     DEFAULT_USER);

            // Password: Environment variable > App.config > Placeholder
            string password = Environment.GetEnvironmentVariable("LDMAS_DB_PASSWORD")
                              ?? GetConfigValue("DbPassword", "YOUR_PASSWORD_HERE");

            var connBuilder = new MySqlConnectionStringBuilder
            {
                Server              = server,
                Port                = uint.Parse(port),
                Database            = database,
                UserID              = userId,
                Password            = password,
                SslMode             = MySqlSslMode.Preferred,
                CharacterSet        = "utf8mb4",
                ConnectionTimeout   = 30,
                DefaultCommandTimeout = 60,
                Pooling             = true,
                MinimumPoolSize     = 2,
                MaximumPoolSize     = 20,
                ConnectionLifeTime  = 300
            };

            return connBuilder.ConnectionString;
        }

        /// <summary>
        /// Reads a value from the <c>appSettings</c> section of App.config.
        /// Returns <paramref name="defaultValue"/> if the key is not found.
        /// </summary>
        private static string GetConfigValue(string key, string defaultValue)
        {
            try
            {
                string? value = ConfigurationManager.AppSettings[key];
                return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
            }
            catch
            {
                return defaultValue;
            }
        }
    }
}
