# Technical Handover Document

**Independent Laboratory Data Management and Automation System (LDMAS)**
*Project Year: 2026*

| Field             | Detail                                                                    |
|-------------------|---------------------------------------------------------------------------|
| **Developer**     | Salih                                                                     |
| **Role**          | Senior Software Engineer (Internship Final Deliverable)                   |
| **Date**          | 2026-08-10                                                                |
| **Tech Stack**    | C# (.NET 9.0), WPF, MySQL 8.0+, xUnit                                     |

---

## 1. Executive Summary

This document serves as the final technical handover for the LDMAS project. It details the system architecture, database schema, security protocols, and setup instructions required for future maintainers to deploy, modify, or extend the application.

LDMAS is a desktop-based, three-tier application built to replace manual laboratory workflows with a secure, automated digital system featuring Role-Based Access Control (RBAC), statistical anomaly detection, and comprehensive audit trails.

---

## 2. System Architecture

The application strictly adheres to an N-Tier (Three-Tier) architecture to ensure separation of concerns, testability, and maintainability.

### 2.1 Presentation Layer (WPF)
- **Framework**: Windows Presentation Foundation (WPF) utilizing XAML.
- **Location**: `src/UI/`
- **Design Pattern**: Code-Behind with modular grid-based navigation.
- **Key Features**: Borderless custom window chrome, clinical dark theme (`#0F1B2D` / `#00B4D8`), dynamic dashboard KPIs, and inactivity session timeouts (FR-005).

### 2.2 Business Logic Layer (BLL)
- **Framework**: C# 10+
- **Location**: `src/Security/`, `src/Analytics/`, `src/Utils/`
- **Analytics (`DataAnalyzer.cs`)**: Utilizes LINQ for in-memory data processing. Implements standard deviation algorithms ($\sigma$) to detect test result anomalies and calculates moving averages for time-series data.
- **Utilities (`ExportUtility.cs`)**: Implements C# Reflection (`System.Reflection`) to dynamically map generic object properties (`IEnumerable<T>`) to RFC 4180 compliant CSV files for research data export.

### 2.3 Data Access Layer (DAL)
- **Framework**: ADO.NET (`MySql.Data`)
- **Location**: `src/DataAccess/`
- **Key Features**: Implements the Repository Pattern (`EquipmentRepository.cs`). Strictly enforces **Parameterized Queries** across all methods to prevent SQL Injection vulnerabilities. Database connections are managed via a thread-safe Singleton pattern.

---

## 3. Database Schema (MySQL)

The database (`ldmas_db`) consists of 11 fully normalized relational tables.

- **Core Tables**: `Users`, `Roles`, `Experiments`, `Samples`, `TestResults`, `Equipment`, `Inventory`.
- **Relational Integrity**: Enforced via Foreign Keys with `ON UPDATE CASCADE` and `ON DELETE RESTRICT` (to prevent accidental deletion of historical data).
- **Optimization**: B-Tree indexes (`optimizations.sql`) applied to high-frequency query columns (`status`, `is_active`, `parameter_name`) and composite indexes implemented specifically to optimize the BLL LINQ date-range queries.

---

## 4. Security Protocols

Security was integrated using a Shift-Left approach, prioritizing data integrity and access control.

1. **Authentication (Identity)**
   - Handled by `AuthenticationService.cs`.
   - Passwords are **never** stored in plaintext. They are hashed using **BCrypt** (`BCrypt.Net-Next`) with a work factor (cost) of 12.
   - Includes brute-force protection (account lockout after 5 failed attempts).

2. **Authorization (RBAC)**
   - Users are assigned roles: `Admin`, `Manager`, `Technician`, or `Auditor`.
   - The UI layer reads these roles to toggle the visibility of sensitive modules (e.g., the User Management screen is hidden from Technicians).

3. **Audit Trail**
   - An immutable `AuditLog` table tracks all INSERT, UPDATE, and DELETE operations.
   - Captures User ID, Timestamp, Table Name, and the exact Action performed.

---

## 5. Development Environment Setup

Future developers must follow these steps to compile and run LDMAS locally.

### 5.1 Prerequisites
- **.NET 9.0 SDK** or higher.
- **MySQL Server 8.0+** running locally on port 3306.
- **Visual Studio 2022** (Recommended) or VS Code with C# Dev Kit.

### 5.2 Database Initialization
1. Open MySQL Workbench or your preferred database client.
2. Execute the schema script: `database/ldmas_schema.sql`
3. Execute the optimization script: `database/optimizations.sql`
4. *(Optional)* Insert dummy data into the `Users` table to create an initial Admin account. Remember to hash the password via BCrypt first.

### 5.3 Application Configuration
1. Open `src/DataAccess/DatabaseConnection.cs`.
2. Locate the connection string placeholder:
   ```csharp
   "Server=localhost;Database=ldmas_db;Uid=root;Pwd=[YOUR_PASSWORD];"
   ```
3. Update `Pwd=` with your local MySQL root password. *(Note: In production, this should be moved to a secure configuration file or environment variable).*

### 5.4 Build and Run
Execute the following commands in the project root:

```powershell
# Restore NuGet packages (BCrypt.Net-Next, MySql.Data)
dotnet restore

# Build the WPF Application
dotnet build

# Run the Application
dotnet run
```

### 5.5 Running Unit Tests
The analytical components are verified via xUnit.

```powershell
dotnet test tests/UnitTests/LDMAS.UnitTests.csproj
```

---
*End of Handover Document.*
