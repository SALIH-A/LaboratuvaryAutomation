<div align="center">
  <img src="https://img.icons8.com/color/96/000000/microscope.png" alt="Microscope Logo"/>
  <h1>Laboratory Data Management & Automation System (LDMAS)</h1>
  <p><strong>Project Year: 2026</strong></p>

  <!-- Badges -->
  <p>
    <img src="https://img.shields.io/badge/.NET-9.0-512BD4?style=for-the-badge&logo=dotnet" alt=".NET 9"/>
    <img src="https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white" alt="C#"/>
    <img src="https://img.shields.io/badge/WPF-0078D7?style=for-the-badge&logo=windows&logoColor=white" alt="WPF"/>
    <img src="https://img.shields.io/badge/MySQL-4479A1?style=for-the-badge&logo=mysql&logoColor=white" alt="MySQL"/>
    <img src="https://img.shields.io/badge/Security-BCrypt-red?style=for-the-badge" alt="BCrypt"/>
    <img src="https://img.shields.io/badge/AI-Gemini%202.5%20Flash-8E75B2?style=for-the-badge" alt="Gemini AI"/>
  </p>
</div>

---

## 🔬 Project Overview

**LDMAS** is a comprehensive, desktop-based laboratory automation system developed to replace manual tracking of chemical experiments, equipment calibrations, and inventory management. Designed for a high-compliance academic or clinical environment, it ensures data integrity through strict Role-Based Access Control, robust database normalizations, and immutable audit trails.

This project was built over an 8-week software engineering internship cycle, focusing on building a highly scalable three-tier architecture from scratch.

### ✨ Key Features

- **📊 Advanced WPF Dashboard**: Clinical dark-themed UI with real-time KPI metrics and modular navigation.
- **🔐 Enterprise Security**: BCrypt password hashing, session timeouts, and granular Role-Based Access Control (Admin, Manager, Technician, Auditor).
- **📈 Data Mining & Analytics**: In-memory LINQ processing to calculate statistical averages and identify anomalous test results using Standard Deviation algorithms.
- **📂 Reflection-Based Export**: Automated conversion of any data model into RFC 4180 compliant CSV formats for external research reporting.
- **🛡️ Robust Data Layer**: Fully normalized MySQL 8.0 schema (11 tables) utilizing Parameterized Queries to prevent SQL injection, and optimized via B-Tree indexes.

---

## 🛠️ Technology Stack

| Domain | Technology |
|---|---|
| **Frontend UI** | Windows Presentation Foundation (WPF), XAML |
| **Backend Logic** | C# 10+, .NET 9.0 |
| **Database** | MySQL 8.0, ADO.NET (`MySql.Data`) |
| **Security** | `BCrypt.Net-Next` |
| **Testing** | xUnit, Fluent Assertions |
| **AI Assistant** | Gemini 2.5 Flash |

---

## 🚀 Getting Started

### Prerequisites
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download)
- MySQL Server running locally on port 3306

### Installation & Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/SALIH-A/LaboratuvaryAutomation.git
   cd LaboratuvaryAutomation
   ```

2. **Initialize the Database**
   Import the schema and optimization scripts into your local MySQL instance:
   - `database/ldmas_schema.sql`
   - `database/optimizations.sql`

3. **Configure Database Connection**
   Open `src/DataAccess/DatabaseConnection.cs` and update the placeholder password in the connection string to match your local MySQL root password:
   ```csharp
   "Server=localhost;Database=ldmas_db;Uid=root;Pwd=[YOUR_PASSWORD];"
   ```

4. **Run the Application**
   ```bash
   dotnet restore
   dotnet run
   ```

5. **Run Unit Tests**
   ```bash
   dotnet test tests/UnitTests/LDMAS.UnitTests.csproj
   ```

---

## 📚 Documentation

Detailed documentation generated throughout the internship lifecycle:

- [Technical Handover](docs/TECHNICAL_HANDOVER.md) — Architecture, Schema, and Dev Setup.
- [User Manual](docs/USER_MANUAL.md) — Guide for laboratory staff.
- [System Architecture](docs/SystemArchitecture.md) — 3-Tier design decisions.
- [Security Architecture](docs/SecurityArchitecture.md) — Encryption and RBAC logic.
- [Analytics Module](docs/AnalyticsModule.md) — Math models for anomaly detection.
- [UI Design](docs/UIDesign.md) — WPF styling and component maps.

---
*Developed by Salih — Software Engineering Internship (2026).*
