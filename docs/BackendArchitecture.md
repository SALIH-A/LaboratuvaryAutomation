# Backend Architecture — Phase I

## Week 3 Deliverable — LDMAS

| Field             | Detail                                                                    |
|-------------------|---------------------------------------------------------------------------|
| **Author**        | Salih                                                                     |
| **Date**          | 2026-08-10                                                                |
| **Version**       | 1.0                                                                       |
| **Milestone**     | Week 3 — Backend Development Phase I (DAL Foundation)                     |
| **Tech Stack**    | C# 10+ · .NET 6+ · MySql.Data (ADO.NET) · MySQL 8.0                      |
| **Prerequisites** | [Requirements.md](../Requirements.md) · [SystemArchitecture.md](SystemArchitecture.md) |

---

## Table of Contents

1. [Overview](#1-overview)
2. [Development Environment Setup](#2-development-environment-setup)
3. [Database Connection Architecture](#3-database-connection-architecture)
4. [Repository Pattern Implementation](#4-repository-pattern-implementation)
5. [CRUD Operations — Equipment Module](#5-crud-operations--equipment-module)
6. [Security Measures](#6-security-measures)
7. [Error Handling Strategy](#7-error-handling-strategy)
8. [File Structure](#8-file-structure)
9. [How to Run](#9-how-to-run)
10. [Next Steps — Week 4 Preview](#10-next-steps--week-4-preview)

---

## 1. Overview

### 1.1 Scope of This Document

This document details **Phase I** of the LDMAS backend development, focusing on the foundational **Data Access Layer (DAL)**. Week 3 establishes the core infrastructure that all subsequent feature modules (Weeks 4–6) will build upon:

| Component                  | File                           | Purpose                                           |
|----------------------------|--------------------------------|----------------------------------------------------|
| **Connection Factory**     | `DatabaseConnection.cs`        | Singleton MySQL connection management              |
| **Equipment Repository**   | `EquipmentRepository.cs`       | Full CRUD operations for the Equipment table       |

### 1.2 Architectural Context

These components occupy the **Data Access Layer** within the three-tier architecture defined in [SystemArchitecture.md](SystemArchitecture.md):

```
┌──────────────────────────────────────────────────────────────────┐
│  PRESENTATION LAYER          (Week 5+)                          │
├──────────────────────────────────────────────────────────────────┤
│  BUSINESS LOGIC LAYER        (Week 4)                           │
├──────────────────────────────────────────────────────────────────┤
│  DATA ACCESS LAYER           ◄── WEEK 3 (This Deliverable)     │
│                                                                  │
│  ┌─────────────────────┐    ┌──────────────────────────┐        │
│  │ DatabaseConnection  │───▶│  EquipmentRepository     │        │
│  │ (Singleton Factory)  │    │  (CRUD Operations)       │        │
│  └─────────┬───────────┘    └──────────────────────────┘        │
│            │                                                     │
│            ▼                                                     │
│  ┌─────────────────────┐                                        │
│  │    MySql.Data        │  ← NuGet Package (ADO.NET driver)     │
│  │    (MySqlConnection) │                                        │
│  └─────────┬───────────┘                                        │
├────────────┼─────────────────────────────────────────────────────┤
│            ▼                                                     │
│  ┌─────────────────────┐                                        │
│  │     MySQL 8.0       │                                        │
│  │     ldmas_db        │                                        │
│  └─────────────────────┘                                        │
│  DATABASE LAYER                                                  │
└──────────────────────────────────────────────────────────────────┘
```

### 1.3 Design Decisions — ADO.NET vs. ORM

For this phase, **raw ADO.NET** via `MySql.Data.MySqlClient` was selected over Entity Framework Core or Dapper:

| Factor                | ADO.NET (MySql.Data)           | Entity Framework Core         |
|-----------------------|--------------------------------|-------------------------------|
| Learning Value        | Deep SQL/DB understanding      | Abstracted away               |
| Query Control         | Full control over SQL          | LINQ-to-SQL translation       |
| Performance           | Minimal overhead               | Slight ORM overhead           |
| SQL Injection Safety  | Manual (parameterized queries) | Automatic (LINQ)              |
| Schema Coupling       | Manual mapping                 | Auto-generated models         |
| Project Complexity    | Low dependency footprint       | Requires migration tooling    |

**Rationale:** As an internship learning project, working with raw ADO.NET ensures deep understanding of database interactions, parameterized query construction, and connection lifecycle management — foundational skills that underpin all higher-level abstractions.

---

## 2. Development Environment Setup

### 2.1 Prerequisites

| Component            | Version Required   | Verification Command                    |
|----------------------|--------------------|-----------------------------------------|
| .NET SDK             | 6.0+ (LTS)        | `dotnet --version`                      |
| MySQL Server         | 8.0+               | `mysql --version`                       |
| Visual Studio / Code | Latest             | —                                       |
| Git                  | Latest             | `git --version`                         |

### 2.2 NuGet Package Installation

The `MySql.Data` package provides the ADO.NET driver for MySQL connectivity:

```powershell
# Install MySql.Data NuGet package into the project
dotnet add package MySql.Data

# Verify installation
dotnet list package
```

> **Note:** If using Visual Studio Package Manager Console:
> ```powershell
> Install-Package MySql.Data
> ```

### 2.3 App.config Configuration

Create an `App.config` file in the project root with the following structure:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<configuration>
    <!-- Option A: Full connection string (recommended) -->
    <connectionStrings>
        <add name="LdmasDb"
             connectionString="Server=localhost;Port=3306;Database=ldmas_db;Uid=root;Pwd=YOUR_PASSWORD_HERE;SslMode=Preferred;CharacterSet=utf8mb4;"
             providerName="MySql.Data.MySqlClient" />
    </connectionStrings>

    <!-- Option B: Individual keys (fallback) -->
    <appSettings>
        <add key="DbServer"   value="localhost" />
        <add key="DbPort"     value="3306" />
        <add key="DbName"     value="ldmas_db" />
        <add key="DbUser"     value="root" />
        <add key="DbPassword" value="YOUR_PASSWORD_HERE" />
    </appSettings>
</configuration>
```

### 2.4 Secure Password Configuration

**Never commit actual passwords to Git.** Use one of these methods:

```powershell
# Method 1: Environment variable (recommended for development)
$env:LDMAS_DB_PASSWORD = "YourActualPassword"

# Method 2: .NET User Secrets (for .NET 6+ projects)
dotnet user-secrets init
dotnet user-secrets set "DbPassword" "YourActualPassword"
```

The `DatabaseConnection` class automatically checks for the `LDMAS_DB_PASSWORD` environment variable and prioritizes it over any App.config value.

---

## 3. Database Connection Architecture

### 3.1 Singleton Pattern

The `DatabaseConnection` class implements the **Singleton pattern** with **double-checked locking** to ensure:

- **Single configuration point** — The connection string is built once and reused.
- **Thread safety** — Safe for multi-threaded access in UI applications.
- **Resource efficiency** — No redundant connection string parsing.

```
┌─────────────────────────────────────────────────────────────────────┐
│                     DatabaseConnection (Singleton)                  │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ┌─────────────────────────────────────┐                            │
│  │  Configuration Precedence           │                            │
│  │                                     │                            │
│  │  1. Environment Variable            │ ← Highest Priority        │
│  │     $LDMAS_DB_PASSWORD              │                            │
│  │                                     │                            │
│  │  2. App.config <connectionStrings>  │                            │
│  │     name="LdmasDb"                  │                            │
│  │                                     │                            │
│  │  3. App.config <appSettings>        │                            │
│  │     Individual keys (DbServer, ...) │                            │
│  │                                     │                            │
│  │  4. Hardcoded Defaults              │ ← Lowest Priority         │
│  │     localhost:3306/ldmas_db/root     │                            │
│  └─────────────────────────────────────┘                            │
│                                                                     │
│  Public Methods:                                                    │
│    • CreateConnection() → MySqlConnection                           │
│    • TestConnection()   → bool                                      │
│    • GetMaskedConnectionString() → string                           │
│                                                                     │
├─────────────────────────────────────────────────────────────────────┤
│  Connection Pool Settings:                                          │
│    MinPoolSize=2  MaxPoolSize=20  ConnectionLifeTime=300s           │
└─────────────────────────────────────────────────────────────────────┘
```

### 3.2 Connection Lifecycle

All repository methods follow this lifecycle pattern:

```csharp
// 1. Acquire connection from singleton factory
using (var connection = DatabaseConnection.Instance.CreateConnection())
{
    // 2. Open the connection (drawn from pool)
    connection.Open();

    // 3. Execute parameterized query
    using (var command = new MySqlCommand(sql, connection))
    {
        command.Parameters.AddWithValue("@param", value);
        // ... execute ...
    }

    // 4. Connection automatically returned to pool via Dispose()
}
```

**Key principles:**
- Every connection is wrapped in `using` for deterministic disposal (NFR-020).
- Connections are **never stored** as class fields — they are always short-lived.
- The underlying `MySqlConnection` pooling handles connection reuse transparently.

---

## 4. Repository Pattern Implementation

### 4.1 Pattern Overview

The **Repository Pattern** abstracts data access logic behind a clean interface, providing these benefits:

| Benefit                  | Description                                                              |
|--------------------------|--------------------------------------------------------------------------|
| **Separation of concerns** | SQL logic is isolated from business rules and UI code.                |
| **Testability**          | Repositories can be mocked for unit testing the BLL (Week 7).           |
| **Maintainability**      | SQL changes are localized to a single file per entity.                   |
| **Consistency**          | All data access follows the same structural patterns.                    |

### 4.2 Repository Structure

Each repository follows this consistent internal structure:

```
Repository Class
│
├── SQL Constants          ← All SQL statements as named constants
│   ├── SQL_INSERT
│   ├── SQL_SELECT_BY_ID
│   ├── SQL_SELECT_ALL
│   ├── SQL_UPDATE
│   └── SQL_DELETE
│
├── CREATE Operations      ← Insert methods
├── READ Operations        ← GetById, GetAll, GetByFilter, Search
├── UPDATE Operations      ← Full update, partial update (status only)
├── DELETE Operations      ← Hard delete
├── UTILITY Methods        ← Count, Exists
│
└── PRIVATE Helpers        ← MapReaderToModel, AddParameters
```

---

## 5. CRUD Operations — Equipment Module

### 5.1 Operations Summary

The `EquipmentRepository` class implements the following operations, all traceable to functional requirements:

| Method              | SQL Operation  | Description                                              | Requirement |
|---------------------|----------------|----------------------------------------------------------|-------------|
| `Create()`          | `INSERT`       | Registers new equipment; returns auto-generated ID       | FR-020      |
| `GetById()`         | `SELECT`       | Retrieves single equipment record by PK                  | FR-025      |
| `GetAll()`          | `SELECT`       | Lists all equipment (newest first)                       | FR-025      |
| `GetByStatus()`     | `SELECT`       | Filters by status ENUM value                             | FR-021      |
| `GetByLocation()`   | `SELECT`       | Filters by laboratory location                           | FR-020      |
| `Search()`          | `SELECT LIKE`  | Partial text search across name/model/manufacturer/serial| FR-025      |
| `Update()`          | `UPDATE`       | Updates all mutable fields                               | FR-025      |
| `UpdateStatus()`    | `UPDATE`       | Quick status-only transition                             | FR-021      |
| `Delete()`          | `DELETE`       | Hard deletes record (cascades to calibrations)           | FR-025      |
| `GetTotalCount()`   | `SELECT COUNT` | Returns total equipment count                            | FR-035      |
| `GetCountByStatus()`| `SELECT COUNT` | Returns count per status (for dashboard)                 | FR-035      |
| `Exists()`          | `SELECT COUNT` | Checks record existence by PK                            | —           |

### 5.2 Parameterized Query Example

Every query uses `MySqlParameter` objects to prevent SQL injection (NFR-002):

```csharp
// ❌ VULNERABLE — String concatenation (NEVER do this)
string sql = $"SELECT * FROM Equipment WHERE name = '{userInput}'";

// ✅ SECURE — Parameterized query (all LDMAS queries use this pattern)
string sql = "SELECT * FROM Equipment WHERE name = @Name";
command.Parameters.AddWithValue("@Name", userInput);
```

### 5.3 Nullable Column Handling

The Equipment table contains several nullable columns (`model`, `manufacturer`, `serial_number`, etc.). The repository handles these bidirectionally:

**Writing nulls to MySQL:**
```csharp
command.Parameters.AddWithValue("@Model", (object?)equipment.Model ?? DBNull.Value);
```

**Reading nulls from MySQL:**
```csharp
Model = reader.IsDBNull(reader.GetOrdinal("model")) ? null : reader.GetString("model");
```

---

## 6. Security Measures

### 6.1 Implemented Security Controls

| Control                          | Implementation                                       | Requirement |
|----------------------------------|------------------------------------------------------|-------------|
| SQL Injection Prevention         | All queries use `MySqlParameter` parameterization    | NFR-002     |
| No Hardcoded Credentials         | Password from `$LDMAS_DB_PASSWORD` env var           | NFR-004     |
| Connection String Masking        | `GetMaskedConnectionString()` for safe logging       | NFR-004     |
| Connection Pooling Limits        | `MaxPoolSize=20` prevents resource exhaustion        | NFR-010     |
| Command Timeout                  | `DefaultCommandTimeout=60s` prevents hanging queries | NFR-010     |
| Deterministic Resource Cleanup   | All connections wrapped in `using` statements        | NFR-020     |

### 6.2 .gitignore Additions

The following patterns must be in `.gitignore` to prevent credential leaks:

```gitignore
# Sensitive configuration files
**/App.config
**/appsettings.json
**/appsettings.*.json

# .NET User Secrets
**/secrets.json
```

---

## 7. Error Handling Strategy

### 7.1 Current Approach (Phase I)

For Phase I, the repository layer uses **catch-and-log** error handling:

```csharp
catch (MySqlException ex)
{
    Console.Error.WriteLine($"[EquipmentRepository] Operation failed — {ex.Message}");
    return defaultValue; // -1 for Create, false for Update/Delete, null for GetById
}
```

### 7.2 Phase II Enhancement (Week 4+)

In Week 4, this will be upgraded to a proper exception hierarchy:

```
LdmasException (base)
├── LdmasDataAccessException      ← Wraps MySqlException
│   ├── ConnectionFailedException
│   ├── DuplicateEntryException   ← MySQL error 1062
│   └── ForeignKeyViolationException ← MySQL error 1451
└── LdmasBusinessException        ← Business rule violations
```

---

## 8. File Structure

### 8.1 Current Project Layout (After Week 3)

```
LaboratuvarAutomation/
├── .git/
├── Requirements.md                         ← Week 1
├── database/
│   └── ldmas_schema.sql                    ← Week 2
├── docs/
│   ├── SystemArchitecture.md               ← Week 2
│   └── BackendArchitecture.md              ← Week 3 (this document)
└── src/
    └── DataAccess/                         ← Week 3 (NEW)
        ├── DatabaseConnection.cs           ← Singleton connection factory
        └── EquipmentRepository.cs          ← Equipment CRUD repository
```

### 8.2 Planned Structure (Weeks 4–6)

```
src/
├── DataAccess/
│   ├── DatabaseConnection.cs
│   ├── EquipmentRepository.cs              ← Week 3 ✓
│   ├── ExperimentRepository.cs             ← Week 4
│   ├── SampleRepository.cs                 ← Week 4
│   ├── UserRepository.cs                   ← Week 4
│   ├── InventoryRepository.cs              ← Week 5
│   └── AuditRepository.cs                  ← Week 6
├── BusinessLogic/
│   ├── Services/
│   │   ├── EquipmentService.cs             ← Week 4
│   │   ├── ExperimentService.cs            ← Week 4
│   │   └── AuthenticationService.cs        ← Week 5
│   └── Validation/
│       └── EquipmentValidator.cs           ← Week 5
├── Models/
│   ├── Equipment.cs                        ← Week 4 (extracted from repository)
│   ├── Experiment.cs                       ← Week 4
│   └── User.cs                             ← Week 4
└── Presentation/                           ← Week 5+
```

---

## 9. How to Run

### 9.1 Quick Start

```powershell
# 1. Ensure MySQL is running and the schema is loaded
mysql -u root -p < database/ldmas_schema.sql

# 2. Set the database password as an environment variable
$env:LDMAS_DB_PASSWORD = "YourMySQLPassword"

# 3. Install the MySql.Data NuGet package
dotnet add package MySql.Data

# 4. Build the project
dotnet build

# 5. Run (once a Program.cs entry point is created in Week 4)
dotnet run
```

### 9.2 Connection Test

You can verify database connectivity by calling:

```csharp
bool isConnected = DatabaseConnection.Instance.TestConnection();
// Output: [DatabaseConnection] Connection test PASSED — Server: 8.0.xx
```

---

## 10. Next Steps — Week 4 Preview

### Backend Core — Business Logic Layer

Week 4 will build the **Business Logic Layer (BLL)** on top of the DAL foundation created this week:

1. **Additional Repositories** — Implement `UserRepository`, `ExperimentRepository`, and `SampleRepository` following the same patterns established in `EquipmentRepository`.

2. **Model Extraction** — Extract entity classes (`Equipment`, `User`, `Experiment`, `Sample`) into a dedicated `Models/` namespace, decoupling them from the repository files.

3. **Service Layer** — Create service classes (`EquipmentService`, `ExperimentService`) that encapsulate business rules, validation logic, and workflow orchestration above the repository layer.

4. **Dependency Injection** — Introduce interface abstractions (`IEquipmentRepository`, `IExperimentService`) and configure DI container registration for testability (NFR-034).

5. **Custom Exceptions** — Replace the current catch-and-log approach with a structured exception hierarchy for graceful error propagation from DAL → BLL → Presentation.

---

> **Document Classification:** Internal — Internship Project Documentation  
> **Repository:** [LaboratuvaryAutomation](https://github.com/SALIH-A/LaboratuvaryAutomation)  
> **Parent Documents:** [Requirements.md](../Requirements.md) · [SystemArchitecture.md](SystemArchitecture.md)
