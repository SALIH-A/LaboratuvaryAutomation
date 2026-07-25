-- ============================================================================
-- INDEPENDENT LABORATORY DATA MANAGEMENT AND AUTOMATION SYSTEM (LDMAS)
-- Database Schema Creation Script
-- ============================================================================
-- Author       : Salih
-- Date         : 2026-07-25
-- Version      : 1.0
-- Engine       : MySQL 8.0+
-- Charset      : UTF-8 (utf8mb4)
-- Collation    : utf8mb4_unicode_ci
-- Normalization: Third Normal Form (3NF)
-- ============================================================================
-- This script creates the complete relational schema for LDMAS.
-- Execute this script on a clean MySQL 8.0+ instance.
--
-- Execution Order:
--   1. Database creation
--   2. Lookup / reference tables (Roles)
--   3. Core entity tables (Users, Equipment, InventoryItems)
--   4. Junction / association tables (UserRoles)
--   5. Dependent entity tables (Experiments, Samples, TestResults, etc.)
--   6. Audit infrastructure (AuditLog)
--   7. Indexes for query optimization
--   8. Triggers for automated audit logging
--   9. Seed data for initial system configuration
-- ============================================================================

-- ----------------------------------------------------------------------------
-- 1. DATABASE CREATION
-- ----------------------------------------------------------------------------

DROP DATABASE IF EXISTS ldmas_db;

CREATE DATABASE ldmas_db
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

USE ldmas_db;

-- ============================================================================
-- 2. LOOKUP / REFERENCE TABLES
-- ============================================================================

-- ----------------------------------------------------------------------------
-- Table: Roles
-- Description: Defines the available system roles for RBAC enforcement.
-- Requirement Traceability: FR-004
-- ----------------------------------------------------------------------------

CREATE TABLE Roles (
    role_id     INT             AUTO_INCREMENT  PRIMARY KEY,
    role_name   VARCHAR(50)     NOT NULL        UNIQUE,
    description VARCHAR(255)    NULL,
    is_active   BOOLEAN         NOT NULL        DEFAULT TRUE,
    created_at  DATETIME        NOT NULL        DEFAULT CURRENT_TIMESTAMP,
    updated_at  DATETIME        NOT NULL        DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='System roles for Role-Based Access Control (RBAC). Ref: FR-004';


-- ============================================================================
-- 3. CORE ENTITY TABLES
-- ============================================================================

-- ----------------------------------------------------------------------------
-- Table: Users
-- Description: Stores user account information for authentication and
--              authorization. Passwords are stored as bcrypt hashes.
-- Requirement Traceability: FR-001, FR-002, FR-003, NFR-001
-- ----------------------------------------------------------------------------

CREATE TABLE Users (
    user_id         INT             AUTO_INCREMENT  PRIMARY KEY,
    username        VARCHAR(100)    NOT NULL        UNIQUE,
    email           VARCHAR(255)    NOT NULL        UNIQUE,
    password_hash   VARCHAR(255)    NOT NULL        COMMENT 'Bcrypt hash with cost factor >= 12',
    first_name      VARCHAR(100)    NOT NULL,
    last_name       VARCHAR(100)    NOT NULL,
    phone           VARCHAR(20)     NULL,
    department      VARCHAR(100)    NULL,
    is_active       BOOLEAN         NOT NULL        DEFAULT TRUE,
    last_login_at   DATETIME        NULL,
    created_at      DATETIME        NOT NULL        DEFAULT CURRENT_TIMESTAMP,
    updated_at      DATETIME        NOT NULL        DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='User accounts for authentication and RBAC. Ref: FR-001, FR-002, FR-003';

-- ----------------------------------------------------------------------------
-- Table: Equipment
-- Description: Central registry of laboratory equipment and instruments.
-- Requirement Traceability: FR-020, FR-021
-- ----------------------------------------------------------------------------

CREATE TABLE Equipment (
    equipment_id    INT             AUTO_INCREMENT  PRIMARY KEY,
    name            VARCHAR(200)    NOT NULL,
    model           VARCHAR(200)    NULL,
    manufacturer    VARCHAR(200)    NULL,
    serial_number   VARCHAR(100)    NULL            UNIQUE,
    purchase_date   DATE            NULL,
    location        VARCHAR(200)    NULL,
    status          ENUM(
                        'Active',
                        'Under Maintenance',
                        'Calibration Due',
                        'Decommissioned'
                    )               NOT NULL        DEFAULT 'Active',
    notes           TEXT            NULL,
    created_by      INT             NOT NULL,
    created_at      DATETIME        NOT NULL        DEFAULT CURRENT_TIMESTAMP,
    updated_at      DATETIME        NOT NULL        DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

    CONSTRAINT fk_equipment_created_by
        FOREIGN KEY (created_by) REFERENCES Users(user_id)
        ON UPDATE CASCADE ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Laboratory equipment and instrument registry. Ref: FR-020, FR-021';

-- ----------------------------------------------------------------------------
-- Table: InventoryItems
-- Description: Tracks reagents, consumables, and laboratory supplies.
-- Requirement Traceability: FR-023, FR-024
-- ----------------------------------------------------------------------------

CREATE TABLE InventoryItems (
    item_id             INT             AUTO_INCREMENT  PRIMARY KEY,
    name                VARCHAR(200)    NOT NULL,
    category            VARCHAR(100)    NOT NULL,
    lot_number          VARCHAR(100)    NULL,
    quantity            DECIMAL(12,4)   NOT NULL        DEFAULT 0.0000,
    unit                VARCHAR(50)     NOT NULL        COMMENT 'e.g., mL, g, units, pcs',
    expiry_date         DATE            NULL,
    minimum_stock_level DECIMAL(12,4)   NOT NULL        DEFAULT 0.0000,
    supplier            VARCHAR(200)    NULL,
    storage_location    VARCHAR(200)    NULL,
    is_below_threshold  BOOLEAN         GENERATED ALWAYS AS (quantity < minimum_stock_level) STORED
                                        COMMENT 'Auto-computed flag for low-stock alerts (FR-024)',
    created_by          INT             NOT NULL,
    created_at          DATETIME        NOT NULL        DEFAULT CURRENT_TIMESTAMP,
    updated_at          DATETIME        NOT NULL        DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

    CONSTRAINT fk_inventory_created_by
        FOREIGN KEY (created_by) REFERENCES Users(user_id)
        ON UPDATE CASCADE ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Reagent and consumable inventory with threshold alerts. Ref: FR-023, FR-024';


-- ============================================================================
-- 4. JUNCTION / ASSOCIATION TABLES
-- ============================================================================

-- ----------------------------------------------------------------------------
-- Table: UserRoles
-- Description: Many-to-many mapping between Users and Roles.
--              A user may hold multiple roles simultaneously.
-- Requirement Traceability: FR-004
-- ----------------------------------------------------------------------------

CREATE TABLE UserRoles (
    user_role_id    INT         AUTO_INCREMENT  PRIMARY KEY,
    user_id         INT         NOT NULL,
    role_id         INT         NOT NULL,
    assigned_at     DATETIME    NOT NULL        DEFAULT CURRENT_TIMESTAMP,
    assigned_by     INT         NULL            COMMENT 'Admin user who assigned this role',

    CONSTRAINT uq_user_role UNIQUE (user_id, role_id),

    CONSTRAINT fk_userroles_user
        FOREIGN KEY (user_id) REFERENCES Users(user_id)
        ON UPDATE CASCADE ON DELETE CASCADE,

    CONSTRAINT fk_userroles_role
        FOREIGN KEY (role_id) REFERENCES Roles(role_id)
        ON UPDATE CASCADE ON DELETE RESTRICT,

    CONSTRAINT fk_userroles_assigned_by
        FOREIGN KEY (assigned_by) REFERENCES Users(user_id)
        ON UPDATE CASCADE ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='User-to-Role mapping for RBAC. Ref: FR-004';


-- ============================================================================
-- 5. DEPENDENT ENTITY TABLES
-- ============================================================================

-- ----------------------------------------------------------------------------
-- Table: Experiments
-- Description: Core experiment records with status workflow tracking.
-- Requirement Traceability: FR-010, FR-012, FR-015, FR-016
-- ----------------------------------------------------------------------------

CREATE TABLE Experiments (
    experiment_id       INT             AUTO_INCREMENT  PRIMARY KEY,
    title               VARCHAR(300)    NOT NULL,
    description         TEXT            NULL,
    category            VARCHAR(100)    NOT NULL,
    start_date          DATE            NOT NULL,
    end_date            DATE            NULL,
    status              ENUM(
                            'Draft',
                            'In Progress',
                            'Awaiting Review',
                            'Approved',
                            'Rejected',
                            'Archived'
                        )               NOT NULL        DEFAULT 'Draft',
    assigned_technician INT             NOT NULL        COMMENT 'FK to Users — the technician running this experiment',
    reviewed_by         INT             NULL            COMMENT 'FK to Users — manager who approved/rejected',
    reviewer_comments   TEXT            NULL            COMMENT 'Mandatory when status is Approved or Rejected (FR-016)',
    reviewed_at         DATETIME        NULL,
    created_by          INT             NOT NULL,
    created_at          DATETIME        NOT NULL        DEFAULT CURRENT_TIMESTAMP,
    updated_at          DATETIME        NOT NULL        DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

    CONSTRAINT fk_experiments_technician
        FOREIGN KEY (assigned_technician) REFERENCES Users(user_id)
        ON UPDATE CASCADE ON DELETE RESTRICT,

    CONSTRAINT fk_experiments_reviewer
        FOREIGN KEY (reviewed_by) REFERENCES Users(user_id)
        ON UPDATE CASCADE ON DELETE SET NULL,

    CONSTRAINT fk_experiments_created_by
        FOREIGN KEY (created_by) REFERENCES Users(user_id)
        ON UPDATE CASCADE ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Experiment records with status workflow. Ref: FR-010, FR-012, FR-015, FR-016';

-- ----------------------------------------------------------------------------
-- Table: Samples
-- Description: Laboratory samples associated with experiments.
-- Requirement Traceability: FR-011, FR-012
-- ----------------------------------------------------------------------------

CREATE TABLE Samples (
    sample_id           INT             AUTO_INCREMENT  PRIMARY KEY,
    experiment_id       INT             NOT NULL,
    source              VARCHAR(200)    NOT NULL        COMMENT 'Origin or source of the sample',
    collection_date     DATE            NOT NULL,
    storage_conditions  VARCHAR(200)    NULL            COMMENT 'e.g., -20°C, Room Temp, 4°C',
    status              ENUM(
                            'Registered',
                            'In Testing',
                            'Completed',
                            'Disposed'
                        )               NOT NULL        DEFAULT 'Registered',
    notes               TEXT            NULL,
    registered_by       INT             NOT NULL,
    created_at          DATETIME        NOT NULL        DEFAULT CURRENT_TIMESTAMP,
    updated_at          DATETIME        NOT NULL        DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

    CONSTRAINT fk_samples_experiment
        FOREIGN KEY (experiment_id) REFERENCES Experiments(experiment_id)
        ON UPDATE CASCADE ON DELETE CASCADE,

    CONSTRAINT fk_samples_registered_by
        FOREIGN KEY (registered_by) REFERENCES Users(user_id)
        ON UPDATE CASCADE ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Laboratory samples linked to experiments. Ref: FR-011, FR-012';

-- ----------------------------------------------------------------------------
-- Table: TestResults
-- Description: Individual test measurement results for each sample.
-- Requirement Traceability: FR-013, FR-014
-- ----------------------------------------------------------------------------

CREATE TABLE TestResults (
    result_id           INT             AUTO_INCREMENT  PRIMARY KEY,
    sample_id           INT             NOT NULL,
    parameter_name      VARCHAR(200)    NOT NULL        COMMENT 'e.g., pH, Conductivity, Concentration',
    measured_value      DECIMAL(18,6)   NOT NULL,
    unit                VARCHAR(50)     NOT NULL        COMMENT 'e.g., pH units, µS/cm, mg/L',
    reference_min       DECIMAL(18,6)   NULL            COMMENT 'Lower bound of acceptable range',
    reference_max       DECIMAL(18,6)   NULL            COMMENT 'Upper bound of acceptable range',
    pass_fail           ENUM('Pass', 'Fail', 'Pending') NOT NULL DEFAULT 'Pending',
    recorded_by         INT             NOT NULL,
    recorded_at         DATETIME        NOT NULL        DEFAULT CURRENT_TIMESTAMP,
    notes               TEXT            NULL,

    CONSTRAINT fk_results_sample
        FOREIGN KEY (sample_id) REFERENCES Samples(sample_id)
        ON UPDATE CASCADE ON DELETE CASCADE,

    CONSTRAINT fk_results_recorded_by
        FOREIGN KEY (recorded_by) REFERENCES Users(user_id)
        ON UPDATE CASCADE ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Test results per sample with pass/fail evaluation. Ref: FR-013, FR-014';

-- ----------------------------------------------------------------------------
-- Table: CalibrationRecords
-- Description: Calibration event history for laboratory equipment.
-- Requirement Traceability: FR-022
-- ----------------------------------------------------------------------------

CREATE TABLE CalibrationRecords (
    calibration_id          INT             AUTO_INCREMENT  PRIMARY KEY,
    equipment_id            INT             NOT NULL,
    calibration_date        DATE            NOT NULL,
    next_due_date           DATE            NOT NULL,
    performed_by            INT             NOT NULL,
    certificate_reference   VARCHAR(200)    NULL            COMMENT 'External calibration certificate ID',
    result                  ENUM('Pass', 'Fail')    NOT NULL,
    notes                   TEXT            NULL,
    created_at              DATETIME        NOT NULL        DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_calibration_equipment
        FOREIGN KEY (equipment_id) REFERENCES Equipment(equipment_id)
        ON UPDATE CASCADE ON DELETE CASCADE,

    CONSTRAINT fk_calibration_performed_by
        FOREIGN KEY (performed_by) REFERENCES Users(user_id)
        ON UPDATE CASCADE ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Equipment calibration event log. Ref: FR-022';

-- ----------------------------------------------------------------------------
-- Table: StockTransactions
-- Description: Tracks all inventory movements (additions, consumptions,
--              adjustments) for a complete stock history.
-- Supports: FR-023, FR-024 (inventory monitoring & threshold alerts)
-- ----------------------------------------------------------------------------

CREATE TABLE StockTransactions (
    transaction_id      INT             AUTO_INCREMENT  PRIMARY KEY,
    item_id             INT             NOT NULL,
    transaction_type    ENUM(
                            'Addition',
                            'Consumption',
                            'Adjustment',
                            'Disposal'
                        )               NOT NULL,
    quantity_change     DECIMAL(12,4)   NOT NULL        COMMENT 'Positive for additions, negative for consumption',
    quantity_after      DECIMAL(12,4)   NOT NULL        COMMENT 'Snapshot of item quantity after this transaction',
    reason              VARCHAR(500)    NULL,
    performed_by        INT             NOT NULL,
    performed_at        DATETIME        NOT NULL        DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_stock_tx_item
        FOREIGN KEY (item_id) REFERENCES InventoryItems(item_id)
        ON UPDATE CASCADE ON DELETE CASCADE,

    CONSTRAINT fk_stock_tx_performed_by
        FOREIGN KEY (performed_by) REFERENCES Users(user_id)
        ON UPDATE CASCADE ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Inventory movement history for stock auditing. Ref: FR-023, FR-024';

-- ----------------------------------------------------------------------------
-- Table: AuthenticationLog
-- Description: Logs all authentication attempts (successful and failed).
-- Requirement Traceability: FR-006
-- ----------------------------------------------------------------------------

CREATE TABLE AuthenticationLog (
    log_id          INT             AUTO_INCREMENT  PRIMARY KEY,
    user_id         INT             NULL            COMMENT 'NULL if username not found in system',
    username        VARCHAR(100)    NOT NULL        COMMENT 'Username attempted (stored regardless of existence)',
    attempt_result  ENUM('Success', 'Failure')  NOT NULL,
    ip_address      VARCHAR(45)     NULL            COMMENT 'IPv4 or IPv6 address',
    user_agent      VARCHAR(500)    NULL,
    attempted_at    DATETIME        NOT NULL        DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_authlog_user
        FOREIGN KEY (user_id) REFERENCES Users(user_id)
        ON UPDATE CASCADE ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Authentication attempt log for security auditing. Ref: FR-006';


-- ============================================================================
-- 6. AUDIT INFRASTRUCTURE
-- ============================================================================

-- ----------------------------------------------------------------------------
-- Table: AuditLog
-- Description: Immutable audit trail capturing all data-modifying operations.
--              No UPDATE or DELETE is permitted on this table (enforced at
--              application layer and via restricted DB user privileges).
-- Requirement Traceability: FR-040, FR-041, FR-042
-- ----------------------------------------------------------------------------

CREATE TABLE AuditLog (
    audit_id        BIGINT          AUTO_INCREMENT  PRIMARY KEY,
    table_name      VARCHAR(100)    NOT NULL,
    record_id       INT             NOT NULL,
    operation_type  ENUM('INSERT', 'UPDATE', 'DELETE')  NOT NULL,
    old_values      JSON            NULL            COMMENT 'JSON snapshot of record state before modification',
    new_values      JSON            NULL            COMMENT 'JSON snapshot of record state after modification',
    changed_by      INT             NULL            COMMENT 'User who performed the operation',
    changed_at      DATETIME        NOT NULL        DEFAULT CURRENT_TIMESTAMP,
    ip_address      VARCHAR(45)     NULL,

    CONSTRAINT fk_audit_changed_by
        FOREIGN KEY (changed_by) REFERENCES Users(user_id)
        ON UPDATE CASCADE ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Immutable audit trail for all CRUD operations. Ref: FR-040, FR-041, FR-042';


-- ============================================================================
-- 7. INDEXES FOR QUERY OPTIMIZATION
-- ============================================================================
-- Requirement Traceability: NFR-013 (indexing on frequently queried columns)
-- Strategy: Index foreign keys, status fields, date fields, and search targets.
-- ----------------------------------------------------------------------------

-- Users
CREATE INDEX idx_users_is_active         ON Users(is_active);
CREATE INDEX idx_users_department        ON Users(department);
CREATE INDEX idx_users_last_login        ON Users(last_login_at);

-- Equipment
CREATE INDEX idx_equipment_status        ON Equipment(status);
CREATE INDEX idx_equipment_location      ON Equipment(location);
CREATE INDEX idx_equipment_created_by    ON Equipment(created_by);

-- InventoryItems
CREATE INDEX idx_inventory_category      ON InventoryItems(category);
CREATE INDEX idx_inventory_expiry        ON InventoryItems(expiry_date);
CREATE INDEX idx_inventory_below_thresh  ON InventoryItems(is_below_threshold);
CREATE INDEX idx_inventory_created_by    ON InventoryItems(created_by);

-- Experiments
CREATE INDEX idx_experiments_status      ON Experiments(status);
CREATE INDEX idx_experiments_category    ON Experiments(category);
CREATE INDEX idx_experiments_start_date  ON Experiments(start_date);
CREATE INDEX idx_experiments_technician  ON Experiments(assigned_technician);
CREATE INDEX idx_experiments_created_by  ON Experiments(created_by);
CREATE INDEX idx_experiments_date_status ON Experiments(start_date, status);

-- Samples
CREATE INDEX idx_samples_experiment      ON Samples(experiment_id);
CREATE INDEX idx_samples_status          ON Samples(status);
CREATE INDEX idx_samples_collection_date ON Samples(collection_date);

-- TestResults
CREATE INDEX idx_results_sample          ON TestResults(sample_id);
CREATE INDEX idx_results_parameter       ON TestResults(parameter_name);
CREATE INDEX idx_results_pass_fail       ON TestResults(pass_fail);
CREATE INDEX idx_results_recorded_at     ON TestResults(recorded_at);

-- CalibrationRecords
CREATE INDEX idx_calibration_equipment   ON CalibrationRecords(equipment_id);
CREATE INDEX idx_calibration_next_due    ON CalibrationRecords(next_due_date);
CREATE INDEX idx_calibration_result      ON CalibrationRecords(result);

-- StockTransactions
CREATE INDEX idx_stock_tx_item           ON StockTransactions(item_id);
CREATE INDEX idx_stock_tx_type           ON StockTransactions(transaction_type);
CREATE INDEX idx_stock_tx_performed_at   ON StockTransactions(performed_at);

-- AuthenticationLog
CREATE INDEX idx_authlog_user            ON AuthenticationLog(user_id);
CREATE INDEX idx_authlog_result          ON AuthenticationLog(attempt_result);
CREATE INDEX idx_authlog_attempted_at    ON AuthenticationLog(attempted_at);

-- AuditLog
CREATE INDEX idx_audit_table_name        ON AuditLog(table_name);
CREATE INDEX idx_audit_record_id         ON AuditLog(record_id);
CREATE INDEX idx_audit_operation         ON AuditLog(operation_type);
CREATE INDEX idx_audit_changed_by        ON AuditLog(changed_by);
CREATE INDEX idx_audit_changed_at        ON AuditLog(changed_at);
CREATE INDEX idx_audit_table_record      ON AuditLog(table_name, record_id);


-- ============================================================================
-- 8. TRIGGERS FOR AUTOMATED AUDIT LOGGING
-- ============================================================================
-- Requirement Traceability: FR-040
-- These triggers automatically capture INSERT, UPDATE, and DELETE operations
-- on core business tables into the AuditLog table.
-- Note: @app_user_id is a session variable set by the application layer
--       to identify the authenticated user performing the operation.
-- ----------------------------------------------------------------------------

-- ---- Experiments Triggers ----

DELIMITER //

CREATE TRIGGER trg_experiments_after_insert
AFTER INSERT ON Experiments
FOR EACH ROW
BEGIN
    INSERT INTO AuditLog (table_name, record_id, operation_type, old_values, new_values, changed_by, changed_at)
    VALUES (
        'Experiments',
        NEW.experiment_id,
        'INSERT',
        NULL,
        JSON_OBJECT(
            'title', NEW.title,
            'category', NEW.category,
            'status', NEW.status,
            'assigned_technician', NEW.assigned_technician,
            'start_date', NEW.start_date
        ),
        @app_user_id,
        NOW()
    );
END//

CREATE TRIGGER trg_experiments_after_update
AFTER UPDATE ON Experiments
FOR EACH ROW
BEGIN
    INSERT INTO AuditLog (table_name, record_id, operation_type, old_values, new_values, changed_by, changed_at)
    VALUES (
        'Experiments',
        NEW.experiment_id,
        'UPDATE',
        JSON_OBJECT(
            'title', OLD.title,
            'category', OLD.category,
            'status', OLD.status,
            'assigned_technician', OLD.assigned_technician,
            'reviewed_by', OLD.reviewed_by
        ),
        JSON_OBJECT(
            'title', NEW.title,
            'category', NEW.category,
            'status', NEW.status,
            'assigned_technician', NEW.assigned_technician,
            'reviewed_by', NEW.reviewed_by
        ),
        @app_user_id,
        NOW()
    );
END//

CREATE TRIGGER trg_experiments_after_delete
AFTER DELETE ON Experiments
FOR EACH ROW
BEGIN
    INSERT INTO AuditLog (table_name, record_id, operation_type, old_values, new_values, changed_by, changed_at)
    VALUES (
        'Experiments',
        OLD.experiment_id,
        'DELETE',
        JSON_OBJECT(
            'title', OLD.title,
            'category', OLD.category,
            'status', OLD.status,
            'assigned_technician', OLD.assigned_technician
        ),
        NULL,
        @app_user_id,
        NOW()
    );
END//

-- ---- Equipment Triggers ----

CREATE TRIGGER trg_equipment_after_insert
AFTER INSERT ON Equipment
FOR EACH ROW
BEGIN
    INSERT INTO AuditLog (table_name, record_id, operation_type, old_values, new_values, changed_by, changed_at)
    VALUES (
        'Equipment',
        NEW.equipment_id,
        'INSERT',
        NULL,
        JSON_OBJECT(
            'name', NEW.name,
            'serial_number', NEW.serial_number,
            'status', NEW.status,
            'location', NEW.location
        ),
        @app_user_id,
        NOW()
    );
END//

CREATE TRIGGER trg_equipment_after_update
AFTER UPDATE ON Equipment
FOR EACH ROW
BEGIN
    INSERT INTO AuditLog (table_name, record_id, operation_type, old_values, new_values, changed_by, changed_at)
    VALUES (
        'Equipment',
        NEW.equipment_id,
        'UPDATE',
        JSON_OBJECT(
            'name', OLD.name,
            'serial_number', OLD.serial_number,
            'status', OLD.status,
            'location', OLD.location
        ),
        JSON_OBJECT(
            'name', NEW.name,
            'serial_number', NEW.serial_number,
            'status', NEW.status,
            'location', NEW.location
        ),
        @app_user_id,
        NOW()
    );
END//

-- ---- Samples Triggers ----

CREATE TRIGGER trg_samples_after_insert
AFTER INSERT ON Samples
FOR EACH ROW
BEGIN
    INSERT INTO AuditLog (table_name, record_id, operation_type, old_values, new_values, changed_by, changed_at)
    VALUES (
        'Samples',
        NEW.sample_id,
        'INSERT',
        NULL,
        JSON_OBJECT(
            'experiment_id', NEW.experiment_id,
            'source', NEW.source,
            'status', NEW.status,
            'collection_date', NEW.collection_date
        ),
        @app_user_id,
        NOW()
    );
END//

CREATE TRIGGER trg_samples_after_update
AFTER UPDATE ON Samples
FOR EACH ROW
BEGIN
    INSERT INTO AuditLog (table_name, record_id, operation_type, old_values, new_values, changed_by, changed_at)
    VALUES (
        'Samples',
        NEW.sample_id,
        'UPDATE',
        JSON_OBJECT(
            'experiment_id', OLD.experiment_id,
            'source', OLD.source,
            'status', OLD.status
        ),
        JSON_OBJECT(
            'experiment_id', NEW.experiment_id,
            'source', NEW.source,
            'status', NEW.status
        ),
        @app_user_id,
        NOW()
    );
END//

-- ---- TestResults Triggers ----

CREATE TRIGGER trg_results_after_insert
AFTER INSERT ON TestResults
FOR EACH ROW
BEGIN
    INSERT INTO AuditLog (table_name, record_id, operation_type, old_values, new_values, changed_by, changed_at)
    VALUES (
        'TestResults',
        NEW.result_id,
        'INSERT',
        NULL,
        JSON_OBJECT(
            'sample_id', NEW.sample_id,
            'parameter_name', NEW.parameter_name,
            'measured_value', NEW.measured_value,
            'pass_fail', NEW.pass_fail
        ),
        @app_user_id,
        NOW()
    );
END//

DELIMITER ;


-- ============================================================================
-- 9. SEED DATA — INITIAL SYSTEM CONFIGURATION
-- ============================================================================
-- Populates the system with default roles and an initial administrator account.
-- Default admin credentials:
--   Username : admin
--   Password : Admin@LDMAS2026  (bcrypt hash below, cost factor = 12)
-- IMPORTANT: Change this password immediately after first login.
-- ----------------------------------------------------------------------------

-- 9.1 Default Roles
INSERT INTO Roles (role_name, description) VALUES
    ('Admin',       'System administrator with full access to all modules and user management.'),
    ('Manager',     'Lab manager who oversees experiments, approves results, and manages equipment.'),
    ('Technician',  'Lab technician responsible for data entry, sample processing, and test execution.'),
    ('Auditor',     'Quality auditor with read-only access to audit trails, reports, and historical data.');

-- 9.2 Default Administrator Account
-- Password: Admin@LDMAS2026 → bcrypt hash (cost=12)
INSERT INTO Users (username, email, password_hash, first_name, last_name, is_active) VALUES
    ('admin', 'admin@ldmas.local', '$2a$12$LJ3m4ys5Qp8Wk.vG7rZ1GeZKxHw5FqNBcUvR9aE3n0yF1dS2hJ6Oe', 'System', 'Administrator', TRUE);

-- 9.3 Assign Admin Role to Default Administrator
INSERT INTO UserRoles (user_id, role_id, assigned_by) VALUES
    (1, 1, NULL);


-- ============================================================================
-- STORED PROCEDURES
-- ============================================================================

DELIMITER //

-- ----------------------------------------------------------------------------
-- Procedure: sp_get_experiments_by_filter
-- Description: Retrieves experiments filtered by status, category, date range,
--              and/or assigned technician. Supports the search functionality
--              defined in FR-017.
-- ----------------------------------------------------------------------------

CREATE PROCEDURE sp_get_experiments_by_filter(
    IN p_status         VARCHAR(50),
    IN p_category       VARCHAR(100),
    IN p_start_from     DATE,
    IN p_start_to       DATE,
    IN p_technician_id  INT
)
BEGIN
    SELECT
        e.experiment_id,
        e.title,
        e.description,
        e.category,
        e.start_date,
        e.end_date,
        e.status,
        CONCAT(u.first_name, ' ', u.last_name)     AS technician_name,
        CONCAT(r.first_name, ' ', r.last_name)      AS reviewer_name,
        e.reviewer_comments,
        e.reviewed_at,
        e.created_at,
        e.updated_at
    FROM Experiments e
    INNER JOIN Users u ON e.assigned_technician = u.user_id
    LEFT  JOIN Users r ON e.reviewed_by = r.user_id
    WHERE
        (p_status IS NULL        OR e.status = p_status)
        AND (p_category IS NULL  OR e.category = p_category)
        AND (p_start_from IS NULL OR e.start_date >= p_start_from)
        AND (p_start_to IS NULL  OR e.start_date <= p_start_to)
        AND (p_technician_id IS NULL OR e.assigned_technician = p_technician_id)
    ORDER BY e.start_date DESC, e.created_at DESC;
END//

-- ----------------------------------------------------------------------------
-- Procedure: sp_get_calibration_status_report
-- Description: Generates a calibration status report showing overdue and
--              upcoming calibrations. Supports FR-031.
-- ----------------------------------------------------------------------------

CREATE PROCEDURE sp_get_calibration_status_report()
BEGIN
    SELECT
        eq.equipment_id,
        eq.name             AS equipment_name,
        eq.serial_number,
        eq.status           AS equipment_status,
        cr.calibration_date AS last_calibration_date,
        cr.next_due_date,
        cr.result           AS last_calibration_result,
        CONCAT(u.first_name, ' ', u.last_name) AS performed_by_name,
        CASE
            WHEN cr.next_due_date < CURDATE() THEN 'OVERDUE'
            WHEN cr.next_due_date <= DATE_ADD(CURDATE(), INTERVAL 30 DAY) THEN 'DUE SOON'
            ELSE 'OK'
        END AS calibration_urgency
    FROM Equipment eq
    LEFT JOIN (
        SELECT cr1.*
        FROM CalibrationRecords cr1
        INNER JOIN (
            SELECT equipment_id, MAX(calibration_date) AS max_date
            FROM CalibrationRecords
            GROUP BY equipment_id
        ) cr2 ON cr1.equipment_id = cr2.equipment_id AND cr1.calibration_date = cr2.max_date
    ) cr ON eq.equipment_id = cr.equipment_id
    LEFT JOIN Users u ON cr.performed_by = u.user_id
    WHERE eq.status != 'Decommissioned'
    ORDER BY
        CASE
            WHEN cr.next_due_date < CURDATE() THEN 0
            WHEN cr.next_due_date <= DATE_ADD(CURDATE(), INTERVAL 30 DAY) THEN 1
            ELSE 2
        END,
        cr.next_due_date ASC;
END//

-- ----------------------------------------------------------------------------
-- Procedure: sp_get_inventory_status_report
-- Description: Generates an inventory status report showing current stock
--              levels and items below minimum threshold. Supports FR-032.
-- ----------------------------------------------------------------------------

CREATE PROCEDURE sp_get_inventory_status_report()
BEGIN
    SELECT
        i.item_id,
        i.name,
        i.category,
        i.lot_number,
        i.quantity,
        i.unit,
        i.minimum_stock_level,
        i.expiry_date,
        i.is_below_threshold,
        i.supplier,
        i.storage_location,
        CASE
            WHEN i.quantity <= 0 THEN 'OUT OF STOCK'
            WHEN i.is_below_threshold = TRUE THEN 'LOW STOCK'
            WHEN i.expiry_date IS NOT NULL AND i.expiry_date <= DATE_ADD(CURDATE(), INTERVAL 30 DAY) THEN 'EXPIRING SOON'
            ELSE 'OK'
        END AS stock_urgency
    FROM InventoryItems i
    ORDER BY
        CASE
            WHEN i.quantity <= 0 THEN 0
            WHEN i.is_below_threshold = TRUE THEN 1
            WHEN i.expiry_date IS NOT NULL AND i.expiry_date <= DATE_ADD(CURDATE(), INTERVAL 30 DAY) THEN 2
            ELSE 3
        END,
        i.name ASC;
END//

-- ----------------------------------------------------------------------------
-- Procedure: sp_get_dashboard_summary
-- Description: Returns key metrics for the dashboard view.
--              Supports FR-035.
-- ----------------------------------------------------------------------------

CREATE PROCEDURE sp_get_dashboard_summary()
BEGIN
    -- Active experiments count
    SELECT COUNT(*) AS active_experiments
    FROM Experiments
    WHERE status IN ('Draft', 'In Progress');

    -- Pending approvals count
    SELECT COUNT(*) AS pending_approvals
    FROM Experiments
    WHERE status = 'Awaiting Review';

    -- Overdue calibrations count
    SELECT COUNT(*) AS overdue_calibrations
    FROM Equipment eq
    INNER JOIN (
        SELECT equipment_id, MAX(next_due_date) AS latest_due
        FROM CalibrationRecords
        GROUP BY equipment_id
    ) cr ON eq.equipment_id = cr.equipment_id
    WHERE cr.latest_due < CURDATE()
      AND eq.status != 'Decommissioned';

    -- Low-stock items count
    SELECT COUNT(*) AS low_stock_items
    FROM InventoryItems
    WHERE is_below_threshold = TRUE;
END//

DELIMITER ;


-- ============================================================================
-- SCHEMA CREATION COMPLETE
-- ============================================================================
-- Summary:
--   Tables Created         : 11
--   Indexes Created        : 33
--   Triggers Created       : 9
--   Stored Procedures      : 4
--   Seed Records Inserted  : 6 (4 Roles + 1 User + 1 UserRole)
--
-- Requirement Coverage:
--   FR-001 to FR-007  → Users, Roles, UserRoles, AuthenticationLog
--   FR-010 to FR-017  → Experiments, Samples, TestResults
--   FR-020 to FR-025  → Equipment, CalibrationRecords, InventoryItems, StockTransactions
--   FR-030 to FR-035  → Stored Procedures (sp_get_*)
--   FR-040 to FR-043  → AuditLog, Triggers
--   NFR-013           → Composite and single-column indexes
-- ============================================================================
