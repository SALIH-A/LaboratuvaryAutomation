// ============================================================================
// INDEPENDENT LABORATORY DATA MANAGEMENT AND AUTOMATION SYSTEM (LDMAS)
// ExportUtility.cs — Data Export Services for Research Reporting
// ============================================================================
// Author          : Salih
// Date            : 2026-08-10
// Layer           : Utilities
// Pattern         : Reflection-based CSV Generation
// Requirement Ref : FR-033 (data export), FR-035 (reporting)
// ============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace LDMAS.Utils
{
    /// <summary>
    /// Utility class providing automated export features for research reporting.
    /// Allows converting application datasets into common formats like CSV for 
    /// analysis in external tools (e.g., Excel, R, Python).
    /// </summary>
    public static class ExportUtility
    {
        // =====================================================================
        // CONSTANTS
        // =====================================================================
        
        private const string CSV_SEPARATOR = ",";
        private const string QUOTE_CHAR = "\"";

        // =====================================================================
        // PUBLIC METHODS
        // =====================================================================

        /// <summary>
        /// Exports any generic List to a CSV file.
        /// Uses Reflection to dynamically determine headers and row values 
        /// based on the object's properties.
        /// </summary>
        /// <typeparam name="T">The type of the objects in the list.</typeparam>
        /// <param name="data">The list of objects to export.</param>
        /// <param name="filePath">The absolute or relative path where the CSV should be saved.</param>
        /// <param name="includeHeaders">Whether to include property names as the first row.</param>
        /// <returns>True if the export was successful; False otherwise.</returns>
        public static bool ExportToCsv<T>(IEnumerable<T> data, string filePath, bool includeHeaders = true)
        {
            if (data == null)
            {
                Console.Error.WriteLine("[ExportUtility] Data source is null. Cannot export to CSV.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(filePath))
            {
                Console.Error.WriteLine("[ExportUtility] File path is invalid.");
                return false;
            }

            try
            {
                // Ensure directory exists
                string? directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Get public properties of type T
                PropertyInfo[] properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

                if (properties.Length == 0)
                {
                    Console.Error.WriteLine($"[ExportUtility] Type {typeof(T).Name} has no public properties to export.");
                    return false;
                }

                using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
                {
                    // Write Headers
                    if (includeHeaders)
                    {
                        var headers = properties.Select(p => EscapeForCsv(p.Name));
                        writer.WriteLine(string.Join(CSV_SEPARATOR, headers));
                    }

                    // Write Data Rows
                    foreach (var item in data)
                    {
                        var rowValues = properties.Select(p => 
                        {
                            var value = p.GetValue(item, null);
                            return EscapeForCsv(value?.ToString());
                        });

                        writer.WriteLine(string.Join(CSV_SEPARATOR, rowValues));
                    }
                }

                Console.WriteLine($"[ExportUtility] Successfully exported {data.Count()} records to {filePath}");
                return true;
            }
            catch (IOException ex)
            {
                Console.Error.WriteLine($"[ExportUtility] IO Error writing to file {filePath}: {ex.Message}");
                return false;
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.Error.WriteLine($"[ExportUtility] Access denied writing to file {filePath}: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ExportUtility] Unexpected error during CSV export: {ex.Message}");
                return false;
            }
        }

        // =====================================================================
        // PRIVATE HELPERS
        // =====================================================================

        /// <summary>
        /// Escapes a string value for safe insertion into a CSV format.
        /// Handles commas, double quotes, and newlines by wrapping the string 
        /// in quotes and escaping internal quotes.
        /// </summary>
        private static string EscapeForCsv(string? field)
        {
            if (string.IsNullOrEmpty(field))
            {
                return string.Empty;
            }

            // If the field contains a comma, quote, or newline, it must be escaped
            if (field.Contains(CSV_SEPARATOR) || field.Contains(QUOTE_CHAR) || field.Contains("\n") || field.Contains("\r"))
            {
                // Escape internal quotes by doubling them
                string escapedField = field.Replace(QUOTE_CHAR, QUOTE_CHAR + QUOTE_CHAR);
                
                // Wrap the whole field in quotes
                return $"{QUOTE_CHAR}{escapedField}{QUOTE_CHAR}";
            }

            return field;
        }
    }
}
