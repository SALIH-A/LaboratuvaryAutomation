# Independent Laboratory Data Management and Automation System

## Project Proposal and Requirement Specifications

| Field             | Detail                                                        |
|-------------------|---------------------------------------------------------------|
| **Author**        | Salih                                                         |
| **Date**          | 2026-07-25                                                    |
| **Version**       | 1.0                                                           |
| **Duration**      | 8 Weeks (Internship Project)                                  |
| **Tech Stack**    | C# (.NET) · MySQL · GitHub                                    |
| **Document Type** | Initial Project Proposal & Requirement Specifications (Week 1)|

---

## Table of Contents

1. [Project Overview](#1-project-overview)
2. [Initial Research & Workflow Analysis](#2-initial-research--workflow-analysis)
3. [Project Scope](#3-project-scope)
4. [Functional Requirements](#4-functional-requirements)
5. [Non-Functional Requirements](#5-non-functional-requirements)
6. [System Constraints & Assumptions](#6-system-constraints--assumptions)
7. [Risk Assessment](#7-risk-assessment)
8. [Weekly Development Timeline](#8-weekly-development-timeline)
9. [Next Steps — Week 2 Preview](#9-next-steps--week-2-preview)
10. [References](#10-references)
11. [Revision History](#11-revision-history)

---

## 1. Project Overview

### 1.1 Purpose

This document defines the project proposal and requirement specifications for the **Independent Laboratory Data Management and Automation System (LDMAS)**. The system is designed to replace manual, paper-based workflows in independent laboratory environments with a digitized, secure, and auditable data management platform.

### 1.2 Problem Statement

Independent and small-to-medium-scale laboratories frequently rely on manual record-keeping processes for tracking experiment results, managing equipment inventories, scheduling instrument calibrations, and generating analytical reports. These manual workflows introduce significant risks:

- **Data integrity issues** — Handwritten logs are prone to transcription errors, illegibility, and inconsistency.
- **Traceability gaps** — Paper-based records lack comprehensive audit trails, making regulatory compliance difficult.
- **Operational inefficiency** — Manual cross-referencing of experiment results, sample status, and equipment availability is time-consuming and error-prone.
- **Scalability limitations** — As sample throughput increases, paper-based systems cannot scale without proportional increases in administrative overhead.

### 1.3 Proposed Solution

LDMAS will provide a centralized, role-based data management platform built on a **C# backend** with a **MySQL relational database**. The system will offer:

- Structured data entry and retrieval for laboratory experiments, samples, and results.
- Equipment and inventory tracking with calibration scheduling.
- Role-Based Access Control (RBAC) to enforce data confidentiality and operational segregation.
- Exportable reports for internal review and external auditing purposes.
- Comprehensive audit logging of all data-modifying operations.

### 1.4 Target Users

| User Role              | Description                                                              |
|------------------------|--------------------------------------------------------------------------|
| **Lab Administrator**  | Manages users, system configuration, and global settings.                |
| **Lab Manager**        | Oversees experiments, approves results, and manages equipment records.   |
| **Lab Technician**     | Performs data entry for experiments, samples, and test results.           |
| **Quality Auditor**    | Read-only access to audit trails, reports, and historical data.          |

---

## 2. Initial Research & Workflow Analysis

### 2.1 Research Methodology

An analysis of typical independent laboratory workflows was conducted to identify digitization opportunities. The research focused on three core operational domains: **Experiment & Result Management**, **Equipment & Inventory Tracking**, and **Reporting & Compliance**.

### 2.2 Current Manual Workflows (As-Is Analysis)

#### 2.2.1 Experiment & Result Tracking — Manual Workflow

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                   MANUAL EXPERIMENT TRACKING WORKFLOW                       │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                            │
│  1. Technician receives sample → Logs in paper notebook                    │
│  2. Assigns experiment ID manually → Cross-references existing logs        │
│  3. Performs tests → Records raw data on pre-printed forms                 │
│  4. Calculates results manually → Transfers to results logbook            │
│  5. Manager reviews → Signs physical logbook for approval                 │
│  6. Results filed → Paper copies stored in filing cabinet                 │
│                                                                            │
│  Pain Points:                                                              │
│  ✗ No centralized search capability                                       │
│  ✗ Duplicate entries across multiple notebooks                            │
│  ✗ No automated validation of data ranges                                 │
│  ✗ Results retrieval takes 15–30 minutes per query                        │
│  ✗ No audit trail for modifications                                       │
│                                                                            │
└──────────────────────────────────────────────────────────────────────────────┘
```

#### 2.2.2 Equipment & Inventory Management — Manual Workflow

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                MANUAL EQUIPMENT & INVENTORY WORKFLOW                        │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                            │
│  1. Equipment received → Logged in asset register (spreadsheet)            │
│  2. Calibration schedule created → Tracked via wall calendar               │
│  3. Reagent stock levels → Monitored by visual inspection                  │
│  4. Reorder decisions → Based on technician's subjective assessment        │
│  5. Maintenance events → Recorded in equipment-specific logbooks           │
│  6. Disposal/decommission → Paper form submitted to manager                │
│                                                                            │
│  Pain Points:                                                              │
│  ✗ Calibration deadlines frequently missed                                │
│  ✗ No real-time visibility into stock levels                              │
│  ✗ Equipment history scattered across multiple logbooks                   │
│  ✗ No automated low-stock alerts                                          │
│  ✗ Disposal records difficult to locate during audits                     │
│                                                                            │
└──────────────────────────────────────────────────────────────────────────────┘
```

#### 2.2.3 Reporting & Compliance — Manual Workflow

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                  MANUAL REPORTING & COMPLIANCE WORKFLOW                     │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                            │
│  1. Manager requests monthly summary → Technician compiles from logs       │
│  2. Data aggregation → Manual counting and calculation in spreadsheet      │
│  3. Report formatting → Typed into Word document template                  │
│  4. Review cycle → Printed, reviewed, annotated by hand, re-typed         │
│  5. Audit preparation → Manual collation of logbooks, forms, records       │
│                                                                            │
│  Pain Points:                                                              │
│  ✗ Report generation takes 2–4 hours per report                           │
│  ✗ Aggregation errors in manual calculations                              │
│  ✗ No standardized format across report types                             │
│  ✗ Historical trend analysis practically impossible                       │
│  ✗ Audit readiness requires days of preparation                           │
│                                                                            │
└──────────────────────────────────────────────────────────────────────────────┘
```

### 2.3 Proposed Digital Workflows (To-Be State)

| Operation                  | Manual (As-Is)                          | Digital (To-Be)                                        |
|----------------------------|-----------------------------------------|--------------------------------------------------------|
| Sample Registration        | Paper notebook entry                   | Structured form with auto-generated Sample ID          |
| Experiment Logging         | Handwritten forms                      | Digital data entry with field-level validation         |
| Result Calculation         | Manual computation                     | System-computed derived values with formula audit      |
| Manager Approval           | Physical signature on logbook          | Digital approval workflow with timestamp               |
| Equipment Tracking         | Spreadsheets + wall calendar           | Centralized asset registry with status tracking        |
| Calibration Scheduling     | Manual calendar reminders              | Automated scheduling with due-date alerts              |
| Inventory Monitoring       | Visual inspection                      | Real-time stock level tracking with threshold alerts   |
| Report Generation          | Manual spreadsheet compilation         | One-click report generation with export (CSV/PDF)      |
| Audit Trail                | Non-existent or ad-hoc                 | Automatic, immutable logging of all CRUD operations    |
| Data Retrieval             | 15–30 minutes manual search            | Sub-second query with filters and full-text search     |

---

## 3. Project Scope

### 3.1 In-Scope

The following features and deliverables are within scope for this 8-week independent project:

#### Core Modules

- **FR-MOD-01: User Management Module** — Registration, authentication, role assignment, and access control.
- **FR-MOD-02: Experiment Management Module** — CRUD operations for experiments, associated samples, and test results.
- **FR-MOD-03: Equipment & Inventory Module** — Asset registration, calibration tracking, and reagent/consumable stock management.
- **FR-MOD-04: Reporting Module** — Generation and export of structured reports from stored data.
- **FR-MOD-05: Audit Trail Module** — Immutable logging of all data-modifying operations with user and timestamp metadata.

#### Technical Deliverables

- Relational database schema design (ER diagrams, normalization to 3NF).
- C# backend application with layered architecture (Presentation, Business Logic, Data Access).
- MySQL database implementation with stored procedures and indexing.
- Unit and integration test suite.
- API documentation and user manual.
- Version-controlled source code on GitHub with structured commit history.

### 3.2 Out of Scope

The following are explicitly **excluded** from this 8-week project:

- Real-time instrument interfacing (e.g., LIMS hardware integration).
- Mobile application development.
- Cloud deployment and CI/CD pipeline configuration.
- Multi-tenancy and multi-laboratory support.
- Barcode/QR code scanning integration.
- Regulatory-specific compliance certifications (ISO 17025, GLP).
- Advanced analytics, machine learning, or predictive modeling.
- Internationalization (i18n) and localization (l10n).

### 3.3 Deliverables Summary

| Week | Deliverable                                              | Type          |
|------|----------------------------------------------------------|---------------|
| 1    | Project Proposal & Requirement Specifications            | Document      |
| 2    | System Architecture & ER Diagrams                        | Document/Code |
| 3    | Database Implementation (DDL, seed data, stored procs)   | Code          |
| 4    | Backend Core — Data Access Layer & Business Logic        | Code          |
| 5    | Backend Feature Modules — CRUD & Validation              | Code          |
| 6    | Reporting, Audit Trail & Data Export                      | Code          |
| 7    | Testing, Bug Fixes & Performance Optimization            | Code/Test     |
| 8    | Documentation, Final Review & Project Presentation       | Document      |

---

## 4. Functional Requirements

### 4.1 User Management & Authentication

| ID        | Requirement                                                                                                 | Priority |
|-----------|-------------------------------------------------------------------------------------------------------------|----------|
| FR-001    | The system SHALL allow an administrator to create, read, update, and deactivate user accounts.              | High     |
| FR-002    | The system SHALL enforce authentication via username and password before granting access.                    | High     |
| FR-003    | The system SHALL store passwords using a cryptographic hashing algorithm (e.g., bcrypt or PBKDF2).          | High     |
| FR-004    | The system SHALL support role-based access control with at minimum four roles: Admin, Manager, Technician, Auditor. | High |
| FR-005    | The system SHALL enforce session timeout after a configurable period of inactivity (default: 30 minutes).   | Medium   |
| FR-006    | The system SHALL log all authentication attempts (successful and failed) with timestamp and IP metadata.    | Medium   |
| FR-007    | The system SHALL prevent concurrent active sessions for the same user account.                               | Low      |

### 4.2 Experiment & Sample Management

| ID        | Requirement                                                                                                 | Priority |
|-----------|-------------------------------------------------------------------------------------------------------------|----------|
| FR-010    | The system SHALL allow authorized users to create new experiment records with the following fields: Experiment ID (auto-generated), Title, Description, Category, Start Date, Status, Assigned Technician. | High |
| FR-011    | The system SHALL allow authorized users to register samples linked to an experiment with fields: Sample ID (auto-generated), Source, Collection Date, Storage Conditions, Status. | High |
| FR-012    | The system SHALL support CRUD operations on experiment and sample records with appropriate role restrictions. | High |
| FR-013    | The system SHALL allow technicians to record test results for each sample, including: Parameter Name, Measured Value, Unit, Reference Range, Pass/Fail Status. | High |
| FR-014    | The system SHALL validate data entry against predefined field constraints (e.g., numeric ranges, required fields, date formats). | High |
| FR-015    | The system SHALL support experiment status transitions: `Draft → In Progress → Awaiting Review → Approved → Archived`. | Medium |
| FR-016    | The system SHALL allow managers to approve or reject experiment results with mandatory reviewer comments.    | Medium   |
| FR-017    | The system SHALL support searching and filtering experiments by date range, status, category, and assigned technician. | Medium |

### 4.3 Equipment & Inventory Management

| ID        | Requirement                                                                                                 | Priority |
|-----------|-------------------------------------------------------------------------------------------------------------|----------|
| FR-020    | The system SHALL maintain an equipment registry with fields: Equipment ID, Name, Model, Manufacturer, Serial Number, Purchase Date, Location, Status. | High |
| FR-021    | The system SHALL track equipment status: `Active`, `Under Maintenance`, `Calibration Due`, `Decommissioned`. | High |
| FR-022    | The system SHALL record calibration events with fields: Calibration Date, Next Due Date, Performed By, Certificate Reference, Result (Pass/Fail). | Medium |
| FR-023    | The system SHALL maintain inventory records for reagents and consumables with fields: Item ID, Name, Category, Lot Number, Quantity, Unit, Expiry Date, Minimum Stock Level. | Medium |
| FR-024    | The system SHALL flag items when stock quantity falls below the configured minimum threshold.                 | Medium   |
| FR-025    | The system SHALL support CRUD operations on all equipment and inventory records with role-based permissions.  | High     |

### 4.4 Reporting & Data Extraction

| ID        | Requirement                                                                                                 | Priority |
|-----------|-------------------------------------------------------------------------------------------------------------|----------|
| FR-030    | The system SHALL generate summary reports for experiments within a user-specified date range.                 | High     |
| FR-031    | The system SHALL generate equipment calibration status reports listing overdue and upcoming calibrations.     | Medium   |
| FR-032    | The system SHALL generate inventory status reports with current stock levels and items below threshold.       | Medium   |
| FR-033    | The system SHALL support exporting report data in CSV format.                                                | High     |
| FR-034    | The system SHALL support exporting report data in PDF format for formal distribution.                        | Low      |
| FR-035    | The system SHALL provide a dashboard view summarizing key metrics: active experiments, pending approvals, overdue calibrations, low-stock items. | Medium |

### 4.5 Audit Trail & Data Integrity

| ID        | Requirement                                                                                                 | Priority |
|-----------|-------------------------------------------------------------------------------------------------------------|----------|
| FR-040    | The system SHALL automatically log all INSERT, UPDATE, and DELETE operations with: Timestamp, User ID, Table Name, Record ID, Operation Type, Old Value, New Value. | High |
| FR-041    | The system SHALL make audit trail records immutable (no update or delete permitted on audit entries).         | High     |
| FR-042    | The system SHALL allow authorized users (Auditor, Admin) to search and filter audit logs by date range, user, table, and operation type. | Medium |
| FR-043    | The system SHALL support exporting audit trail data for external review.                                      | Low      |

---

## 5. Non-Functional Requirements

### 5.1 Security

| ID        | Requirement                                                                                                 | Priority |
|-----------|-------------------------------------------------------------------------------------------------------------|----------|
| NFR-001   | All passwords SHALL be hashed using bcrypt with a minimum cost factor of 12 before storage.                  | High     |
| NFR-002   | The system SHALL implement parameterized queries or ORM-based data access to prevent SQL injection attacks.  | High     |
| NFR-003   | The system SHALL sanitize all user inputs to prevent Cross-Site Scripting (XSS) and injection vulnerabilities. | High   |
| NFR-004   | Database credentials SHALL NOT be hardcoded in source code; environment variables or encrypted configuration files SHALL be used. | High |
| NFR-005   | The system SHALL enforce the principle of least privilege for database user accounts (separate read/write roles). | Medium |
| NFR-006   | Sensitive data fields (e.g., user email, contact information) SHOULD be encrypted at rest where feasible.    | Low      |

### 5.2 Performance

| ID        | Requirement                                                                                                 | Priority |
|-----------|-------------------------------------------------------------------------------------------------------------|----------|
| NFR-010   | Standard CRUD operations SHALL complete within 2 seconds under normal load conditions.                       | High     |
| NFR-011   | Search and filter queries SHALL return results within 3 seconds for datasets up to 100,000 records.          | Medium   |
| NFR-012   | Report generation SHALL complete within 10 seconds for datasets spanning up to 12 months of data.            | Medium   |
| NFR-013   | The database schema SHALL be optimized with appropriate indexing on frequently queried columns (foreign keys, status fields, date fields). | High |

### 5.3 Reliability & Availability

| ID        | Requirement                                                                                                 | Priority |
|-----------|-------------------------------------------------------------------------------------------------------------|----------|
| NFR-020   | The system SHALL handle database connection failures gracefully with user-facing error messages.              | High     |
| NFR-021   | All data-modifying operations SHALL be wrapped in database transactions to ensure atomicity.                  | High     |
| NFR-022   | The system SHALL implement input validation on both client-side and server-side (defense in depth).           | Medium   |

### 5.4 Maintainability & Code Quality

| ID        | Requirement                                                                                                 | Priority |
|-----------|-------------------------------------------------------------------------------------------------------------|----------|
| NFR-030   | The application SHALL follow a layered architecture pattern: Presentation Layer, Business Logic Layer (BLL), Data Access Layer (DAL). | High |
| NFR-031   | The codebase SHALL adhere to C# naming conventions and coding standards as defined by Microsoft's Framework Design Guidelines. | Medium |
| NFR-032   | All public classes and methods SHALL include XML documentation comments.                                     | Medium   |
| NFR-033   | The project SHALL maintain a minimum of 70% unit test coverage for the Business Logic Layer.                 | Medium   |
| NFR-034   | The system SHALL use dependency injection for service and repository class instantiation.                     | Medium   |

### 5.5 Technology Stack Constraints

| Component           | Technology                         | Version (Minimum)      |
|---------------------|------------------------------------|------------------------|
| Programming Language | C#                                | 10.0+                  |
| Runtime Framework    | .NET                              | 6.0+ (LTS recommended)|
| Database Engine      | MySQL                             | 8.0+                   |
| ORM (Optional)       | Entity Framework Core / Dapper    | EF Core 6.0+ / Latest |
| Version Control      | Git + GitHub                      | Latest                 |
| IDE                  | Visual Studio / VS Code           | Latest                 |
| Testing Framework    | xUnit / NUnit                     | Latest                 |

---

## 6. System Constraints & Assumptions

### 6.1 Constraints

- The project is limited to an **8-week development timeline** with a single developer.
- The system will operate as a **desktop or local web application**; no cloud hosting is required.
- No proprietary or licensed third-party components shall be used; all dependencies must be open-source or freely available.
- The database will run on a **single MySQL server instance** (no replication or clustering).

### 6.2 Assumptions

- The developer has foundational knowledge of C#, SQL, and object-oriented design principles.
- A local development environment with MySQL Server and .NET SDK is available.
- Laboratory workflows described in Section 2 are representative of typical independent laboratory operations.
- End-user testing will be simulated by the developer using test accounts with different role assignments.
- Internet connectivity is available for package management (NuGet) and version control (GitHub).

---

## 7. Risk Assessment

| Risk ID | Risk Description                                           | Likelihood | Impact | Mitigation Strategy                                                 |
|---------|-------------------------------------------------------------|------------|--------|----------------------------------------------------------------------|
| R-001   | Scope creep due to feature expansion                       | High       | High   | Strict adherence to in-scope definitions; weekly milestone reviews.  |
| R-002   | Database design errors requiring late-stage schema changes  | Medium     | High   | Thorough ER modeling in Week 2; early prototyping of key queries.    |
| R-003   | Insufficient time for testing and documentation             | Medium     | Medium | Dedicated weeks (7 & 8) for testing and documentation.              |
| R-004   | Security vulnerabilities in authentication implementation   | Medium     | High   | Use proven libraries (e.g., BCrypt.Net); follow OWASP guidelines.   |
| R-005   | Performance degradation with large datasets                | Low        | Medium | Implement indexing strategy; test with synthetic large datasets.     |
| R-006   | Data loss due to lack of backup strategy                   | Low        | High   | Implement database export scripts; regular Git commits.              |

---

## 8. Weekly Development Timeline

```
Week 1  ██████████ Orientation, Requirement Analysis, Repo Setup
Week 2  ██████████ System Architecture & Database Design (ER Models)
Week 3  ██████████ Database Implementation (DDL, Stored Procedures, Seed Data)
Week 4  ██████████ Backend Core — DAL, BLL, Dependency Injection
Week 5  ██████████ Feature Modules — CRUD Operations, Validation, Workflows
Week 6  ██████████ Reporting, Audit Trail, Data Export
Week 7  ██████████ Testing, Bug Fixes, Performance Optimization
Week 8  ██████████ Documentation, Final Review, Project Presentation
```

---

## 9. Next Steps — Week 2 Preview

### System Architecture & Database Design

Week 2 will focus on translating the requirements defined in this document into a concrete technical architecture and relational data model. Key deliverables include:

1. **System Architecture Diagram** — A layered architecture diagram illustrating the Presentation, Business Logic, and Data Access layers, along with their interactions and dependencies.

2. **Entity-Relationship (ER) Model** — A comprehensive ER diagram covering all identified entities:
   - `Users`, `Roles`, `UserRoles`
   - `Experiments`, `Samples`, `TestResults`
   - `Equipment`, `CalibrationRecords`
   - `InventoryItems`, `StockTransactions`
   - `AuditLog`

3. **Database Normalization** — Ensuring all tables conform to Third Normal Form (3NF) to eliminate data redundancy and ensure referential integrity.

4. **Data Dictionary** — A complete data dictionary documenting each table, column, data type, constraints, and relationships.

5. **Initial DDL Scripts** — Draft `CREATE TABLE` statements for peer review before Week 3 implementation.

---

## 10. References

- Microsoft. (2024). *C# Programming Guide*. https://learn.microsoft.com/en-us/dotnet/csharp/
- Microsoft. (2024). *Entity Framework Core Documentation*. https://learn.microsoft.com/en-us/ef/core/
- Oracle. (2024). *MySQL 8.0 Reference Manual*. https://dev.mysql.com/doc/refman/8.0/en/
- OWASP Foundation. (2024). *OWASP Top Ten*. https://owasp.org/www-project-top-ten/
- Sommerville, I. (2015). *Software Engineering* (10th ed.). Pearson.
- Pressman, R. S. (2014). *Software Engineering: A Practitioner's Approach* (8th ed.). McGraw-Hill.

---

## 11. Revision History

| Version | Date       | Author | Description                                    |
|---------|------------|--------|------------------------------------------------|
| 1.0     | 2026-07-25 | Salih  | Initial project proposal and requirements draft|

---

> **Document Classification:** Internal — Internship Project Documentation  
> **Repository:** [LaboratuvarAotumation](https://github.com/)  
> **License:** This project is developed as part of an academic internship program.
