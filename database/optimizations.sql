-- ============================================================================
-- INDEPENDENT LABORATORY DATA MANAGEMENT AND AUTOMATION SYSTEM (LDMAS)
-- Database Optimizations — Indexing Strategy
-- ============================================================================
-- Author          : Salih
-- Date            : 2026-08-10
-- Target Database : ldmas_db
-- Objective       : Improve query performance on frequently accessed columns
--                   using B-Tree indexes. (Week 7 Deliverable)
-- ============================================================================

USE ldmas_db;

-- ----------------------------------------------------------------------------
-- 1. Users Table Optimizations
-- ----------------------------------------------------------------------------
-- The 'is_active' column is checked during EVERY authentication attempt and 
-- user listing query. Indexing it speeds up active-user filtering.
-- 'department' is frequently used in aggregate reporting (e.g., users per lab).
CREATE INDEX idx_users_is_active ON Users (is_active);
CREATE INDEX idx_users_department ON Users (department);

-- ----------------------------------------------------------------------------
-- 2. Equipment Table Optimizations
-- ----------------------------------------------------------------------------
-- Dashboard queries heavily rely on filtering equipment by 'status' (e.g., finding
-- "Calibration Due" equipment). 
-- 'location' is frequently queried by technicians to find physical assets.
CREATE INDEX idx_equipment_status ON Equipment (status);
CREATE INDEX idx_equipment_location ON Equipment (location);

-- ----------------------------------------------------------------------------
-- 3. Experiments Table Optimizations
-- ----------------------------------------------------------------------------
-- 'status' dictates workflow state and is the primary filter on the dashboard.
-- 'category' is frequently used in reports.
-- 'assigned_technician' is used to filter "My Experiments" views.
CREATE INDEX idx_experiments_status ON Experiments (status);
CREATE INDEX idx_experiments_category ON Experiments (category);
CREATE INDEX idx_experiments_assigned_technician ON Experiments (assigned_technician);

-- Composite Index for Date-based reporting per Status
-- Optimizes queries like: "Show me all 'Approved' experiments from last month"
CREATE INDEX idx_experiments_status_startdate ON Experiments (status, start_date);

-- ----------------------------------------------------------------------------
-- 4. TestResults Table Optimizations (Analytics Module)
-- ----------------------------------------------------------------------------
-- The Analytics module (DataAnalyzer.cs) heavily queries by 'parameter_name' 
-- and 'recorded_at' for trend analysis and anomaly detection.
CREATE INDEX idx_testresults_parameter ON TestResults (parameter_name);

-- Composite Index specifically for CalculateAverageForDateRange() LINQ queries
-- Covers: WHERE parameter_name = 'X' AND recorded_at BETWEEN 'Y' AND 'Z'
CREATE INDEX idx_testresults_param_date ON TestResults (parameter_name, recorded_at);

-- ----------------------------------------------------------------------------
-- 5. AuditLog Optimizations
-- ----------------------------------------------------------------------------
-- Audit trails grow massive. Searching by table_name or operation_type is slow 
-- without indexes.
CREATE INDEX idx_auditlog_table ON AuditLog (table_name);
CREATE INDEX idx_auditlog_operation ON AuditLog (operation_type);

-- ============================================================================
-- End of Optimizations Script
-- ============================================================================
