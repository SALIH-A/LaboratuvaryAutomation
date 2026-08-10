# Testing & Optimization Report

## Week 7 Deliverable — LDMAS

| Field             | Detail                                                                    |
|-------------------|---------------------------------------------------------------------------|
| **Author**        | Salih                                                                     |
| **Date**          | 2026-08-10                                                                |
| **Version**       | 1.0                                                                       |
| **Milestone**     | Week 7 — Unit Testing, Integration Testing, Database Optimization         |
| **Tech Stack**    | xUnit · MySQL B-Tree Indexes · Query Execution Plans                      |
| **Prerequisites** | [AnalyticsModule.md](AnalyticsModule.md) · [SecurityArchitecture.md](SecurityArchitecture.md) |

---

## Table of Contents

1. [Testing Strategy](#1-testing-strategy)
2. [Unit Testing with xUnit](#2-unit-testing-with-xunit)
3. [Test Setup Commands](#3-test-setup-commands)
4. [Database Performance Optimization](#4-database-performance-optimization)
5. [Next Steps — Week 8 Finalization](#5-next-steps--week-8-finalization)

---

## 1. Testing Strategy

To ensure the reliability of the Independent Laboratory Data Management and Automation System (LDMAS), a **Shift-Left** testing strategy was adopted. This focuses on writing tests for the core business logic (BLL) before connecting the UI layer.

### 1.1 Testing Layers
1. **Unit Testing (xUnit)**: Targets isolated, pure-logic components like `DataAnalyzer`. Asserts that algorithms produce correct mathematical results.
2. **Integration Testing**: Verifies that the Data Access Layer (DAL) components (e.g., `EquipmentRepository`) correctly execute parameterized queries against a real local MySQL database.
3. **Security Testing**: Validates that RBAC logic (`AuthenticationService.IsAuthorized`) correctly rejects unauthorized roles, and that the lockout policy blocks brute-force attempts after 5 failures.

---

## 2. Unit Testing with xUnit

We utilized the **xUnit** framework to test the `DataAnalyzer` service built in Week 6.

### 2.1 Why xUnit?
- **Isolation**: xUnit creates a new instance of the test class for *every* test method, preventing state contamination between tests.
- **Data-Driven**: Easily supports parameterized tests (`[Theory]` and `[InlineData]`) for edge cases.

### 2.2 Test Coverage Highlights (`DataAnalyzerTests.cs`)

| Method Tested | Scenario | Assertion Outcome |
|---|---|---|
| `CalculateAverageForDateRange` | Valid data in range | Calculates precise mathematical mean |
| `CalculateAverageForDateRange` | No data in date range | Gracefully returns `null` |
| `CalculateAverageForDateRange` | Empty dataset | Gracefully returns `null` |
| `FindAnomalies` | Dataset with known outliers | Identifies exact IDs of anomalous records |
| `FindAnomalies` | High StdDev Threshold (e.g., 5.0) | Returns empty list (no false positives) |
| `FindAnomalies` | Identical dataset values ($\sigma = 0$) | Returns empty list (prevents divide-by-zero) |
| `FindAnomalies` | $N < 2$ data points | Returns empty list (requires $N \ge 2$ for StdDev) |

---

## 3. Test Setup Commands

To execute the unit tests, the test project must be properly configured and linked to the main application project.

### 3.1 PowerShell Setup Commands

Run these exact commands from your workspace root (`c:\Users\salih\Desktop\LaboratuvarAotumation`):

```powershell
# 1. Create a new xUnit test project in the tests folder
dotnet new xunit -n LDMAS.UnitTests -o tests/UnitTests

# 2. Add the test project to your main Solution (if you have a .sln file)
# dotnet sln add tests/UnitTests/LDMAS.UnitTests.csproj

# 3. Add a project reference so the test project can "see" the main code
dotnet add tests/UnitTests/LDMAS.UnitTests.csproj reference src/LaboratuvarAotumation.csproj

# 4. Run the tests to verify output
dotnet test tests/UnitTests/LDMAS.UnitTests.csproj
```

---

## 4. Database Performance Optimization

As a laboratory system scales over time, the `TestResults` and `AuditLog` tables will grow exponentially. To prevent query timeouts, structural optimizations were applied via `optimizations.sql`.

### 4.1 Indexing Strategy (B-Tree Indexes)

Indexes were added to columns frequently utilized in `WHERE`, `ORDER BY`, and `GROUP BY` clauses.

**Key Optimizations Implemented:**

1. **Dashboard KPI Queries**
   - Added `idx_equipment_status` on `Equipment(status)`.
   - Added `idx_experiments_status` on `Experiments(status)`.
   - *Impact*: Changes $O(N)$ full-table scans into $O(\log N)$ index lookups for the 4 dashboard metric cards.

2. **Analytics Range Queries**
   - Added a **Composite Index**: `idx_testresults_param_date` on `TestResults(parameter_name, recorded_at)`.
   - *Impact*: Dramatically speeds up the `DataAnalyzer.CalculateAverageForDateRange()` method, as MySQL can instantly locate the start date for a specific parameter without scanning unrelated tests.

3. **Security & Audit Efficiency**
   - Added `idx_users_is_active` on `Users(is_active)`. Every login attempt checks this boolean flag.
   - Added `idx_auditlog_table` on `AuditLog(table_name)`. Allows instant filtering of the audit trail for specific module histories.

### 4.2 Query Execution Plans

To verify the optimization, you can use the `EXPLAIN` keyword in MySQL:

```sql
-- Before Index: Type = 'ALL' (Full Table Scan)
-- After Index:  Type = 'ref' (B-Tree Lookup)
EXPLAIN SELECT AVG(measured_value) FROM TestResults 
WHERE parameter_name = 'pH' AND recorded_at BETWEEN '2026-08-01' AND '2026-08-31';
```

---

## 5. Next Steps — Week 8 Finalization

Week 8 marks the final phase of the internship:
1. **Deployment & Configuration**: Containerizing the database (Docker) or setting up production IIS configurations.
2. **User Manual Generation**: Documenting how lab researchers will operate the application.
3. **Final Presentation**: Presenting the architectural decisions, security features, and analytics capabilities to stakeholders.

---

> **Document Classification:** Internal — Internship Project Documentation  
> **Repository:** [LaboratuvaryAutomation](https://github.com/SALIH-A/LaboratuvaryAutomation)
