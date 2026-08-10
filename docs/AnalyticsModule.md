# Analytics & Reporting Module

## Week 6 Deliverable — LDMAS

| Field             | Detail                                                                    |
|-------------------|---------------------------------------------------------------------------|
| **Author**        | Salih                                                                     |
| **Date**          | 2026-08-10                                                                |
| **Version**       | 1.0                                                                       |
| **Milestone**     | Week 6 — Data Analysis & Export Features                                  |
| **Tech Stack**    | C# 10+ · LINQ · System.Reflection · File I/O                              |
| **Prerequisites** | [Requirements.md](../Requirements.md) · [SystemArchitecture.md](SystemArchitecture.md) |

---

## Table of Contents

1. [Overview](#1-overview)
2. [Data Analysis Module (`DataAnalyzer.cs`)](#2-data-analysis-module-dataanalyzercs)
3. [Data Mining & Anomaly Detection](#3-data-mining--anomaly-detection)
4. [Export Utility (`ExportUtility.cs`)](#4-export-utility-exportutilitycs)
5. [Integration Workflow](#5-integration-workflow)
6. [Next Steps — Week 7 Preview](#6-next-steps--week-7-preview)

---

## 1. Overview

Week 6 focuses on transforming raw laboratory data into actionable insights and research reports. The system now features **Data Analysis** and **Automated Export** capabilities. 

This enables researchers to:
- Identify outlier test results (Data Mining).
- Generate statistical summaries of experiment parameters.
- Export structured datasets to CSV format for external processing in Excel, Python, or R.

These features map directly to **FR-033 (Data Analysis & Export)** and **FR-034 (Anomaly Detection)**.

---

## 2. Data Analysis Module (`DataAnalyzer.cs`)

The `DataAnalyzer` service sits in the Business Logic Layer (BLL) and utilizes **Language Integrated Query (LINQ)** to process datasets efficiently in-memory.

### 2.1 Core Capabilities

1. **Statistical Summaries**: Calculates Average, Min, Max, and Sample Standard Deviation for parameters across a dataset.
2. **Date Range Filtering**: Dynamically filters experimental data between specified dates to track changes over time.
3. **Moving Averages**: Calculates rolling trends to smooth out short-term fluctuations in time-series data.

### 2.2 Why LINQ?

LINQ was chosen for this layer instead of raw SQL aggregations to allow for:
- **In-Memory Processing**: We can pull a raw dataset once and run multiple complex analytical algorithms on it without repeatedly hitting the database.
- **Maintainability**: LINQ provides a highly readable and type-safe syntax compared to complex nested SQL queries.
- **Flexibility**: The `DataAnalyzer` operates on generic `IEnumerable<T>` collections, making it easy to unit test with mock data.

---

## 3. Data Mining & Anomaly Detection

A critical feature for laboratory quality control is detecting test results that deviate significantly from the norm. 

### 3.1 Anomaly Detection Algorithm

The `FindAnomalies` method implements a statistical outlier detection algorithm using the **Standard Deviation Method**.

**Algorithm Steps:**
1. Filter the dataset for a specific parameter (e.g., "pH").
2. Calculate the **Mean ($\mu$)** of all measured values.
3. Calculate the **Sample Standard Deviation ($\sigma$)**:
   $$\sigma = \sqrt{\frac{\sum(x_i - \mu)^2}{N - 1}}$$
4. Define the acceptable bounds using a configurable threshold (default is $2\sigma$):
   - Lower Bound = $\mu - (Threshold \times \sigma)$
   - Upper Bound = $\mu + (Threshold \times \sigma)$
5. Identify any values falling outside these bounds.

### 3.2 Threshold Tuning
- **2 Standard Deviations ($2\sigma$)**: Captures ~95% of normal data. Identifies moderate outliers.
- **3 Standard Deviations ($3\sigma$)**: Captures ~99.7% of normal data. Identifies severe, definitive anomalies.

This automated flagging helps researchers immediately spot potential instrument calibration issues or sample contamination.

---

## 4. Export Utility (`ExportUtility.cs`)

To facilitate advanced external analysis, the `ExportUtility` provides a robust, generic mechanism to export application data.

### 4.1 Reflection-Based Generic Export

The `ExportToCsv<T>` method uses **C# Reflection** (`System.Reflection`). This is a powerful design choice that makes the utility universally applicable.

Instead of writing separate export functions for Users, Equipment, and TestResults, the generic method dynamically inspects the properties of *any* given class at runtime:

```csharp
// Dynamically discovers all public properties of the object type
PropertyInfo[] properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
```

### 4.2 CSV Formatting & Escaping

Generating valid CSV files requires strict string escaping. The utility automatically handles:
- Commas within data (e.g., "Sample A, Batch 2")
- Newline characters within text fields (e.g., multi-line notes)
- Double quotes within data

If a field contains any of these characters, it is safely wrapped in double quotes `""`, and internal quotes are escaped per RFC 4180 standards.

---

## 5. Integration Workflow

How these new modules integrate into the researcher's workflow:

1. **Query**: The researcher queries the `TestResults` table for a specific experiment via the UI.
2. **Analyze**: The UI calls `DataAnalyzer.GenerateParameterStatistics()` to display summary tables.
3. **Flag**: The UI calls `DataAnalyzer.FindAnomalies()` to highlight outlier rows in red on the dashboard.
4. **Export**: The researcher clicks "Export Data". The UI passes the list of results to `ExportUtility.ExportToCsv()`.
5. **Review**: The researcher opens the generated `.csv` file in Excel for publication formatting.

---

## 6. Next Steps — Week 7 Preview

### System Integration & Unit Testing

Week 7 will focus on ensuring the reliability and robustness of the LDMAS backend.

1. **Unit Testing Framework**: Setting up xUnit or NUnit to test BLL components.
2. **Testing Analytics**: Writing test cases for `DataAnalyzer` to verify the mathematical accuracy of the anomaly detection and standard deviation algorithms.
3. **Testing Data Access**: Mocking the database to test repository logic.
4. **End-to-End Workflows**: Wiring the UI directly to the Database via the BLL to complete the three-tier architecture integration.

---

> **Document Classification:** Internal — Internship Project Documentation  
> **Repository:** [LaboratuvaryAutomation](https://github.com/SALIH-A/LaboratuvaryAutomation)
