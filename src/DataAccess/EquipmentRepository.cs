// ============================================================================
// INDEPENDENT LABORATORY DATA MANAGEMENT AND AUTOMATION SYSTEM (LDMAS)
// EquipmentRepository.cs — Full CRUD Operations for Equipment Table
// ============================================================================
// Author          : Salih
// Date            : 2026-08-10
// Layer           : Data Access Layer (DAL)
// Pattern         : Repository Pattern
// Requirement Ref : FR-020, FR-021, FR-025, NFR-002 (parameterized queries)
// ============================================================================

using System;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;

namespace LDMAS.DataAccess
{
    // =========================================================================
    // EQUIPMENT MODEL
    // =========================================================================

    /// <summary>
    /// Represents a single equipment record from the <c>Equipment</c> table.
    /// All properties map directly to the MySQL schema columns defined in
    /// <c>ldmas_schema.sql</c> (Week 2 deliverable).
    /// </summary>
    public class Equipment
    {
        /// <summary>Auto-incremented primary key.</summary>
        public int EquipmentId { get; set; }

        /// <summary>Equipment name (required).</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Equipment model designation.</summary>
        public string? Model { get; set; }

        /// <summary>Manufacturer name.</summary>
        public string? Manufacturer { get; set; }

        /// <summary>Unique manufacturer serial number.</summary>
        public string? SerialNumber { get; set; }

        /// <summary>Date of acquisition.</summary>
        public DateTime? PurchaseDate { get; set; }

        /// <summary>Physical location within the laboratory.</summary>
        public string? Location { get; set; }

        /// <summary>
        /// Current operational status.
        /// Valid values: Active, Under Maintenance, Calibration Due, Decommissioned.
        /// </summary>
        public string Status { get; set; } = "Active";

        /// <summary>Free-text notes.</summary>
        public string? Notes { get; set; }

        /// <summary>Foreign key to <c>Users.user_id</c> — the user who registered this equipment.</summary>
        public int CreatedBy { get; set; }

        /// <summary>Record creation timestamp (set by MySQL DEFAULT).</summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>Last modification timestamp (set by MySQL ON UPDATE).</summary>
        public DateTime UpdatedAt { get; set; }
    }

    // =========================================================================
    // EQUIPMENT REPOSITORY
    // =========================================================================

    /// <summary>
    /// Provides full CRUD (Create, Read, Update, Delete) operations for the
    /// <c>Equipment</c> table in the LDMAS database.
    /// 
    /// <para><b>Security (NFR-002):</b> All queries use <see cref="MySqlParameter"/>
    /// for parameterized execution, preventing SQL injection attacks.</para>
    /// 
    /// <para><b>Pattern:</b> Repository Pattern — encapsulates all Equipment-related
    /// data access logic, keeping the Business Logic Layer free of SQL concerns.</para>
    /// 
    /// <para><b>Requirement Traceability:</b></para>
    /// <list type="bullet">
    ///   <item>FR-020: Equipment registry with specified fields.</item>
    ///   <item>FR-021: Equipment status tracking (ENUM).</item>
    ///   <item>FR-025: CRUD operations on equipment records.</item>
    ///   <item>NFR-002: Parameterized queries for SQL injection prevention.</item>
    ///   <item>NFR-021: Transactional data modifications for atomicity.</item>
    /// </list>
    /// </summary>
    public class EquipmentRepository
    {
        // =====================================================================
        // SQL COMMAND CONSTANTS
        // =====================================================================
        // Centralizing SQL statements as constants improves maintainability
        // and reduces the risk of typos across multiple methods.
        // =====================================================================

        private const string SQL_INSERT = @"
            INSERT INTO Equipment 
                (name, model, manufacturer, serial_number, purchase_date, 
                 location, status, notes, created_by)
            VALUES 
                (@Name, @Model, @Manufacturer, @SerialNumber, @PurchaseDate, 
                 @Location, @Status, @Notes, @CreatedBy);
            SELECT LAST_INSERT_ID();";

        private const string SQL_SELECT_BY_ID = @"
            SELECT 
                equipment_id, name, model, manufacturer, serial_number,
                purchase_date, location, status, notes, created_by,
                created_at, updated_at
            FROM Equipment 
            WHERE equipment_id = @EquipmentId;";

        private const string SQL_SELECT_ALL = @"
            SELECT 
                equipment_id, name, model, manufacturer, serial_number,
                purchase_date, location, status, notes, created_by,
                created_at, updated_at
            FROM Equipment 
            ORDER BY created_at DESC;";

        private const string SQL_SELECT_BY_STATUS = @"
            SELECT 
                equipment_id, name, model, manufacturer, serial_number,
                purchase_date, location, status, notes, created_by,
                created_at, updated_at
            FROM Equipment 
            WHERE status = @Status
            ORDER BY name ASC;";

        private const string SQL_SELECT_BY_LOCATION = @"
            SELECT 
                equipment_id, name, model, manufacturer, serial_number,
                purchase_date, location, status, notes, created_by,
                created_at, updated_at
            FROM Equipment 
            WHERE location = @Location
            ORDER BY name ASC;";

        private const string SQL_SEARCH = @"
            SELECT 
                equipment_id, name, model, manufacturer, serial_number,
                purchase_date, location, status, notes, created_by,
                created_at, updated_at
            FROM Equipment 
            WHERE 
                name LIKE @SearchTerm 
                OR model LIKE @SearchTerm 
                OR manufacturer LIKE @SearchTerm 
                OR serial_number LIKE @SearchTerm
            ORDER BY name ASC;";

        private const string SQL_UPDATE = @"
            UPDATE Equipment 
            SET 
                name            = @Name,
                model           = @Model,
                manufacturer    = @Manufacturer,
                serial_number   = @SerialNumber,
                purchase_date   = @PurchaseDate,
                location        = @Location,
                status          = @Status,
                notes           = @Notes
            WHERE equipment_id  = @EquipmentId;";

        private const string SQL_UPDATE_STATUS = @"
            UPDATE Equipment 
            SET status = @Status
            WHERE equipment_id = @EquipmentId;";

        private const string SQL_DELETE = @"
            DELETE FROM Equipment 
            WHERE equipment_id = @EquipmentId;";

        private const string SQL_COUNT = @"
            SELECT COUNT(*) FROM Equipment;";

        private const string SQL_COUNT_BY_STATUS = @"
            SELECT COUNT(*) FROM Equipment WHERE status = @Status;";

        private const string SQL_EXISTS = @"
            SELECT COUNT(1) FROM Equipment WHERE equipment_id = @EquipmentId;";

        // =====================================================================
        // CREATE OPERATIONS
        // =====================================================================

        /// <summary>
        /// Inserts a new equipment record into the database.
        /// </summary>
        /// <param name="equipment">
        /// The <see cref="Equipment"/> object to insert. The <c>EquipmentId</c>,
        /// <c>CreatedAt</c>, and <c>UpdatedAt</c> properties are ignored — they
        /// are auto-generated by MySQL.
        /// </param>
        /// <returns>
        /// The auto-generated <c>equipment_id</c> of the newly inserted record.
        /// Returns <c>-1</c> if the insert operation fails.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="equipment"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown if <c>equipment.Name</c> is null or empty (database NOT NULL constraint).
        /// </exception>
        public int Create(Equipment equipment)
        {
            if (equipment == null)
                throw new ArgumentNullException(nameof(equipment));

            if (string.IsNullOrWhiteSpace(equipment.Name))
                throw new ArgumentException("Equipment name is required.", nameof(equipment));

            try
            {
                using (var connection = DatabaseConnection.Instance.CreateConnection())
                {
                    connection.Open();

                    using (var command = new MySqlCommand(SQL_INSERT, connection))
                    {
                        AddEquipmentParameters(command, equipment);
                        command.Parameters.AddWithValue("@CreatedBy", equipment.CreatedBy);

                        // LAST_INSERT_ID() returns the auto-incremented PK
                        var result = command.ExecuteScalar();
                        int newId = Convert.ToInt32(result);

                        Console.WriteLine($"[EquipmentRepository] Created equipment ID={newId}: '{equipment.Name}'");
                        return newId;
                    }
                }
            }
            catch (MySqlException ex)
            {
                Console.Error.WriteLine($"[EquipmentRepository] Create failed — {ex.Message}");
                return -1;
            }
        }

        // =====================================================================
        // READ OPERATIONS
        // =====================================================================

        /// <summary>
        /// Retrieves a single equipment record by its primary key.
        /// </summary>
        /// <param name="equipmentId">The <c>equipment_id</c> to look up.</param>
        /// <returns>
        /// The <see cref="Equipment"/> object if found; <c>null</c> otherwise.
        /// </returns>
        public Equipment? GetById(int equipmentId)
        {
            try
            {
                using (var connection = DatabaseConnection.Instance.CreateConnection())
                {
                    connection.Open();

                    using (var command = new MySqlCommand(SQL_SELECT_BY_ID, connection))
                    {
                        command.Parameters.AddWithValue("@EquipmentId", equipmentId);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return MapReaderToEquipment(reader);
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                Console.Error.WriteLine($"[EquipmentRepository] GetById({equipmentId}) failed — {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Retrieves all equipment records from the database,
        /// ordered by creation date (newest first).
        /// </summary>
        /// <returns>A list of all <see cref="Equipment"/> records.</returns>
        public List<Equipment> GetAll()
        {
            var equipmentList = new List<Equipment>();

            try
            {
                using (var connection = DatabaseConnection.Instance.CreateConnection())
                {
                    connection.Open();

                    using (var command = new MySqlCommand(SQL_SELECT_ALL, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            equipmentList.Add(MapReaderToEquipment(reader));
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                Console.Error.WriteLine($"[EquipmentRepository] GetAll failed — {ex.Message}");
            }

            return equipmentList;
        }

        /// <summary>
        /// Retrieves all equipment records matching a specific status.
        /// Supports FR-021 equipment status tracking.
        /// </summary>
        /// <param name="status">
        /// The status to filter by. Valid values:
        /// <c>Active</c>, <c>Under Maintenance</c>, <c>Calibration Due</c>, <c>Decommissioned</c>.
        /// </param>
        /// <returns>A list of matching <see cref="Equipment"/> records.</returns>
        public List<Equipment> GetByStatus(string status)
        {
            var equipmentList = new List<Equipment>();

            try
            {
                using (var connection = DatabaseConnection.Instance.CreateConnection())
                {
                    connection.Open();

                    using (var command = new MySqlCommand(SQL_SELECT_BY_STATUS, connection))
                    {
                        command.Parameters.AddWithValue("@Status", status);

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                equipmentList.Add(MapReaderToEquipment(reader));
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                Console.Error.WriteLine($"[EquipmentRepository] GetByStatus('{status}') failed — {ex.Message}");
            }

            return equipmentList;
        }

        /// <summary>
        /// Retrieves all equipment records at a specific location.
        /// </summary>
        /// <param name="location">The laboratory location to filter by.</param>
        /// <returns>A list of matching <see cref="Equipment"/> records.</returns>
        public List<Equipment> GetByLocation(string location)
        {
            var equipmentList = new List<Equipment>();

            try
            {
                using (var connection = DatabaseConnection.Instance.CreateConnection())
                {
                    connection.Open();

                    using (var command = new MySqlCommand(SQL_SELECT_BY_LOCATION, connection))
                    {
                        command.Parameters.AddWithValue("@Location", location);

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                equipmentList.Add(MapReaderToEquipment(reader));
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                Console.Error.WriteLine($"[EquipmentRepository] GetByLocation('{location}') failed — {ex.Message}");
            }

            return equipmentList;
        }

        /// <summary>
        /// Searches equipment records by name, model, manufacturer, or serial number.
        /// Uses SQL <c>LIKE</c> with parameterized wildcards for safe partial matching.
        /// </summary>
        /// <param name="searchTerm">The search keyword (partial match supported).</param>
        /// <returns>A list of matching <see cref="Equipment"/> records.</returns>
        public List<Equipment> Search(string searchTerm)
        {
            var equipmentList = new List<Equipment>();

            if (string.IsNullOrWhiteSpace(searchTerm))
                return equipmentList;

            try
            {
                using (var connection = DatabaseConnection.Instance.CreateConnection())
                {
                    connection.Open();

                    using (var command = new MySqlCommand(SQL_SEARCH, connection))
                    {
                        // Parameterized LIKE — prevents SQL injection even with wildcards
                        command.Parameters.AddWithValue("@SearchTerm", $"%{searchTerm}%");

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                equipmentList.Add(MapReaderToEquipment(reader));
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                Console.Error.WriteLine($"[EquipmentRepository] Search('{searchTerm}') failed — {ex.Message}");
            }

            return equipmentList;
        }

        // =====================================================================
        // UPDATE OPERATIONS
        // =====================================================================

        /// <summary>
        /// Updates all mutable fields of an existing equipment record.
        /// The <c>created_by</c>, <c>created_at</c> fields are immutable
        /// and not included in the UPDATE statement.
        /// </summary>
        /// <param name="equipment">
        /// The <see cref="Equipment"/> object with updated values.
        /// <c>EquipmentId</c> must match an existing record.
        /// </param>
        /// <returns>
        /// <c>true</c> if exactly one row was updated; <c>false</c> otherwise.
        /// </returns>
        public bool Update(Equipment equipment)
        {
            if (equipment == null)
                throw new ArgumentNullException(nameof(equipment));

            if (string.IsNullOrWhiteSpace(equipment.Name))
                throw new ArgumentException("Equipment name is required.", nameof(equipment));

            try
            {
                using (var connection = DatabaseConnection.Instance.CreateConnection())
                {
                    connection.Open();

                    using (var command = new MySqlCommand(SQL_UPDATE, connection))
                    {
                        AddEquipmentParameters(command, equipment);
                        command.Parameters.AddWithValue("@EquipmentId", equipment.EquipmentId);

                        int rowsAffected = command.ExecuteNonQuery();
                        bool success = rowsAffected == 1;

                        if (success)
                            Console.WriteLine($"[EquipmentRepository] Updated equipment ID={equipment.EquipmentId}");
                        else
                            Console.Error.WriteLine($"[EquipmentRepository] Update failed — ID={equipment.EquipmentId} not found");

                        return success;
                    }
                }
            }
            catch (MySqlException ex)
            {
                Console.Error.WriteLine($"[EquipmentRepository] Update({equipment.EquipmentId}) failed — {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Updates only the status field of an equipment record.
        /// Useful for quick status transitions (e.g., Active → Under Maintenance)
        /// without modifying other fields.
        /// </summary>
        /// <param name="equipmentId">The <c>equipment_id</c> to update.</param>
        /// <param name="newStatus">
        /// The new status value. Must be one of:
        /// <c>Active</c>, <c>Under Maintenance</c>, <c>Calibration Due</c>, <c>Decommissioned</c>.
        /// </param>
        /// <returns><c>true</c> if the update succeeded; <c>false</c> otherwise.</returns>
        public bool UpdateStatus(int equipmentId, string newStatus)
        {
            try
            {
                using (var connection = DatabaseConnection.Instance.CreateConnection())
                {
                    connection.Open();

                    using (var command = new MySqlCommand(SQL_UPDATE_STATUS, connection))
                    {
                        command.Parameters.AddWithValue("@EquipmentId", equipmentId);
                        command.Parameters.AddWithValue("@Status", newStatus);

                        int rowsAffected = command.ExecuteNonQuery();
                        return rowsAffected == 1;
                    }
                }
            }
            catch (MySqlException ex)
            {
                Console.Error.WriteLine($"[EquipmentRepository] UpdateStatus({equipmentId}, '{newStatus}') failed — {ex.Message}");
                return false;
            }
        }

        // =====================================================================
        // DELETE OPERATIONS
        // =====================================================================

        /// <summary>
        /// Permanently deletes an equipment record from the database.
        /// 
        /// <para><b>Warning:</b> This operation cascades to <c>CalibrationRecords</c>
        /// due to the <c>ON DELETE CASCADE</c> foreign key constraint defined in
        /// the schema. All calibration history for this equipment will be lost.</para>
        /// 
        /// <para>Consider using <see cref="UpdateStatus"/> to set status to
        /// <c>Decommissioned</c> instead for a soft-delete approach.</para>
        /// </summary>
        /// <param name="equipmentId">The <c>equipment_id</c> to delete.</param>
        /// <returns><c>true</c> if exactly one row was deleted; <c>false</c> otherwise.</returns>
        public bool Delete(int equipmentId)
        {
            try
            {
                using (var connection = DatabaseConnection.Instance.CreateConnection())
                {
                    connection.Open();

                    using (var command = new MySqlCommand(SQL_DELETE, connection))
                    {
                        command.Parameters.AddWithValue("@EquipmentId", equipmentId);

                        int rowsAffected = command.ExecuteNonQuery();
                        bool success = rowsAffected == 1;

                        if (success)
                            Console.WriteLine($"[EquipmentRepository] Deleted equipment ID={equipmentId}");
                        else
                            Console.Error.WriteLine($"[EquipmentRepository] Delete failed — ID={equipmentId} not found");

                        return success;
                    }
                }
            }
            catch (MySqlException ex)
            {
                Console.Error.WriteLine($"[EquipmentRepository] Delete({equipmentId}) failed — {ex.Message}");
                return false;
            }
        }

        // =====================================================================
        // UTILITY METHODS
        // =====================================================================

        /// <summary>
        /// Returns the total count of equipment records in the database.
        /// </summary>
        public int GetTotalCount()
        {
            try
            {
                using (var connection = DatabaseConnection.Instance.CreateConnection())
                {
                    connection.Open();

                    using (var command = new MySqlCommand(SQL_COUNT, connection))
                    {
                        return Convert.ToInt32(command.ExecuteScalar());
                    }
                }
            }
            catch (MySqlException ex)
            {
                Console.Error.WriteLine($"[EquipmentRepository] GetTotalCount failed — {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Returns the count of equipment records with a specific status.
        /// Useful for dashboard metrics (FR-035).
        /// </summary>
        /// <param name="status">The status to count.</param>
        public int GetCountByStatus(string status)
        {
            try
            {
                using (var connection = DatabaseConnection.Instance.CreateConnection())
                {
                    connection.Open();

                    using (var command = new MySqlCommand(SQL_COUNT_BY_STATUS, connection))
                    {
                        command.Parameters.AddWithValue("@Status", status);
                        return Convert.ToInt32(command.ExecuteScalar());
                    }
                }
            }
            catch (MySqlException ex)
            {
                Console.Error.WriteLine($"[EquipmentRepository] GetCountByStatus('{status}') failed — {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Checks whether an equipment record with the given ID exists.
        /// </summary>
        /// <param name="equipmentId">The <c>equipment_id</c> to check.</param>
        /// <returns><c>true</c> if the record exists; <c>false</c> otherwise.</returns>
        public bool Exists(int equipmentId)
        {
            try
            {
                using (var connection = DatabaseConnection.Instance.CreateConnection())
                {
                    connection.Open();

                    using (var command = new MySqlCommand(SQL_EXISTS, connection))
                    {
                        command.Parameters.AddWithValue("@EquipmentId", equipmentId);
                        return Convert.ToInt32(command.ExecuteScalar()) > 0;
                    }
                }
            }
            catch (MySqlException ex)
            {
                Console.Error.WriteLine($"[EquipmentRepository] Exists({equipmentId}) failed — {ex.Message}");
                return false;
            }
        }

        // =====================================================================
        // PRIVATE HELPERS
        // =====================================================================

        /// <summary>
        /// Maps a <see cref="MySqlDataReader"/> row to an <see cref="Equipment"/> object.
        /// Handles <c>DBNull</c> values for all nullable columns.
        /// </summary>
        private static Equipment MapReaderToEquipment(MySqlDataReader reader)
        {
            return new Equipment
            {
                EquipmentId  = reader.GetInt32("equipment_id"),
                Name         = reader.GetString("name"),
                Model        = reader.IsDBNull(reader.GetOrdinal("model"))
                                   ? null : reader.GetString("model"),
                Manufacturer = reader.IsDBNull(reader.GetOrdinal("manufacturer"))
                                   ? null : reader.GetString("manufacturer"),
                SerialNumber = reader.IsDBNull(reader.GetOrdinal("serial_number"))
                                   ? null : reader.GetString("serial_number"),
                PurchaseDate = reader.IsDBNull(reader.GetOrdinal("purchase_date"))
                                   ? null : reader.GetDateTime("purchase_date"),
                Location     = reader.IsDBNull(reader.GetOrdinal("location"))
                                   ? null : reader.GetString("location"),
                Status       = reader.GetString("status"),
                Notes        = reader.IsDBNull(reader.GetOrdinal("notes"))
                                   ? null : reader.GetString("notes"),
                CreatedBy    = reader.GetInt32("created_by"),
                CreatedAt    = reader.GetDateTime("created_at"),
                UpdatedAt    = reader.GetDateTime("updated_at")
            };
        }

        /// <summary>
        /// Adds the common equipment parameters to a <see cref="MySqlCommand"/>.
        /// Shared between <see cref="Create"/> and <see cref="Update"/> to eliminate
        /// duplication. Properly handles nullable values via <c>DBNull.Value</c>.
        /// </summary>
        private static void AddEquipmentParameters(MySqlCommand command, Equipment equipment)
        {
            command.Parameters.AddWithValue("@Name",         equipment.Name);
            command.Parameters.AddWithValue("@Model",        (object?)equipment.Model        ?? DBNull.Value);
            command.Parameters.AddWithValue("@Manufacturer", (object?)equipment.Manufacturer ?? DBNull.Value);
            command.Parameters.AddWithValue("@SerialNumber", (object?)equipment.SerialNumber ?? DBNull.Value);
            command.Parameters.AddWithValue("@PurchaseDate", (object?)equipment.PurchaseDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@Location",     (object?)equipment.Location     ?? DBNull.Value);
            command.Parameters.AddWithValue("@Status",       equipment.Status);
            command.Parameters.AddWithValue("@Notes",        (object?)equipment.Notes        ?? DBNull.Value);
        }
    }
}
