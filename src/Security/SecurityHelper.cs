// ============================================================================
// INDEPENDENT LABORATORY DATA MANAGEMENT AND AUTOMATION SYSTEM (LDMAS)
// SecurityHelper.cs — Cryptographic Utilities for Password Management
// ============================================================================
// Author          : Salih
// Date            : 2026-08-13
// Layer           : Security / Utility
// Dependencies    : BCrypt.Net-Next (NuGet)
// Requirement Ref : FR-003 (password hashing), NFR-001 (bcrypt cost ≥ 12)
// ============================================================================
//
// This utility class centralizes all password hashing and verification
// logic using the BCrypt adaptive hashing algorithm. It serves as the
// single source of truth for cryptographic operations, ensuring:
//
//   • Consistent work factor (cost 12) across registration and login
//   • Automatic salt generation per hash (built into BCrypt)
//   • Timing-attack-resistant verification via BCrypt.Verify
//   • Clean separation from the AuthenticationService business logic
//
// BCrypt Cost Factor:
//   Cost 12 ≈ 250ms per hash on modern hardware, providing a strong
//   balance between security and user experience for a lab system.
//   Increase to 13-14 for higher-security deployments.
//
// ============================================================================

using System;

namespace LDMAS.Security
{
    /// <summary>
    /// Provides static utility methods for password hashing and verification
    /// using the BCrypt adaptive hashing algorithm.
    /// </summary>
    /// <remarks>
    /// All methods are stateless and thread-safe. BCrypt automatically
    /// generates a unique random salt per hash, so no external salt
    /// management is required.
    /// </remarks>
    public static class SecurityHelper
    {
        // =====================================================================
        // CONSTANTS
        // =====================================================================

        /// <summary>
        /// BCrypt work factor (cost). Each increment doubles the computation
        /// time. Cost 12 ≈ 250ms on modern CPUs.
        /// Requirement: NFR-001 specifies cost ≥ 12.
        /// </summary>
        private const int BcryptWorkFactor = 12;

        // =====================================================================
        // PUBLIC API
        // =====================================================================

        /// <summary>
        /// Hashes a plaintext password using BCrypt with automatic salt
        /// generation and the configured work factor.
        /// </summary>
        /// <param name="plainText">The user's plaintext password.</param>
        /// <returns>
        /// A BCrypt hash string in the format:
        /// <c>$2a$12$[22-char salt][31-char hash]</c>
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="plainText"/> is null or empty.
        /// </exception>
        /// <example>
        /// <code>
        /// string hash = SecurityHelper.HashPassword("MyP@ssw0rd!");
        /// // Store 'hash' in the Users.password_hash column
        /// </code>
        /// </example>
        public static string HashPassword(string plainText)
        {
            if (string.IsNullOrWhiteSpace(plainText))
            {
                throw new ArgumentException(
                    "Password cannot be null or empty.",
                    nameof(plainText));
            }

            string hash = BCrypt.Net.BCrypt.HashPassword(plainText, BcryptWorkFactor);

            System.Diagnostics.Debug.WriteLine(
                $"[SecurityHelper] Password hashed (cost {BcryptWorkFactor}, " +
                $"length {hash.Length} chars)");

            return hash;
        }

        /// <summary>
        /// Verifies a plaintext password against a stored BCrypt hash.
        /// Uses BCrypt's built-in timing-attack-resistant comparison.
        /// </summary>
        /// <param name="plainText">The plaintext password to verify.</param>
        /// <param name="hash">The stored BCrypt hash from the database.</param>
        /// <returns>
        /// <c>true</c> if the password matches the hash; <c>false</c> otherwise.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown if either parameter is null or empty.
        /// </exception>
        /// <example>
        /// <code>
        /// bool isValid = SecurityHelper.VerifyPassword("MyP@ssw0rd!", storedHash);
        /// if (isValid) { /* grant access */ }
        /// </code>
        /// </example>
        public static bool VerifyPassword(string plainText, string hash)
        {
            if (string.IsNullOrWhiteSpace(plainText))
            {
                throw new ArgumentException(
                    "Password cannot be null or empty.",
                    nameof(plainText));
            }

            if (string.IsNullOrWhiteSpace(hash))
            {
                throw new ArgumentException(
                    "Hash cannot be null or empty.",
                    nameof(hash));
            }

            try
            {
                bool result = BCrypt.Net.BCrypt.Verify(plainText, hash);

                System.Diagnostics.Debug.WriteLine(
                    $"[SecurityHelper] Password verification: {(result ? "PASS" : "FAIL")}");

                return result;
            }
            catch (BCrypt.Net.SaltParseException ex)
            {
                // Corrupted or invalid hash format in the database
                System.Diagnostics.Debug.WriteLine(
                    $"[SecurityHelper] Hash verification error — invalid hash format: {ex.Message}");
                return false;
            }
        }

        // =====================================================================
        // VALIDATION HELPERS
        // =====================================================================

        /// <summary>
        /// Validates password strength against minimum laboratory security
        /// requirements. Returns a tuple indicating pass/fail and a message.
        /// </summary>
        /// <param name="password">The plaintext password to validate.</param>
        /// <returns>
        /// A tuple: (<c>IsValid</c>, <c>Message</c>).
        /// </returns>
        /// <remarks>
        /// Enforces: minimum 8 characters, at least 1 uppercase letter,
        /// at least 1 digit, and at least 1 special character.
        /// </remarks>
        public static (bool IsValid, string Message) ValidatePasswordStrength(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return (false, "Password cannot be empty.");

            if (password.Length < 8)
                return (false, "Password must be at least 8 characters long.");

            bool hasUpper   = false;
            bool hasLower   = false;
            bool hasDigit   = false;
            bool hasSpecial = false;

            foreach (char c in password)
            {
                if (char.IsUpper(c))       hasUpper = true;
                else if (char.IsLower(c))  hasLower = true;
                else if (char.IsDigit(c))  hasDigit = true;
                else                        hasSpecial = true;
            }

            if (!hasUpper)
                return (false, "Password must contain at least one uppercase letter.");

            if (!hasLower)
                return (false, "Password must contain at least one lowercase letter.");

            if (!hasDigit)
                return (false, "Password must contain at least one digit.");

            if (!hasSpecial)
                return (false, "Password must contain at least one special character.");

            return (true, "Password meets all security requirements.");
        }
    }
}
