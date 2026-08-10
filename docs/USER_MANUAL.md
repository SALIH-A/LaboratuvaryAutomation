# LDMAS User Manual

**Independent Laboratory Data Management and Automation System (LDMAS)**
*Version 1.0 — 2026 Release*

---

## Welcome to LDMAS

Welcome to the Independent Laboratory Data Management and Automation System. LDMAS is designed to streamline your daily laboratory workflows, from tracking sensitive chemical experiments to managing equipment calibrations and generating research reports.

This manual will guide you through the core features of the system.

---

## 1. Getting Started

### 1.1 Logging In
Security is a top priority for laboratory data. To access the system:
1. Open the **LDMAS Application** from your desktop.
2. At the login screen, enter your assigned **Employee ID** or **Email** and your **Password**.
3. Click **Login**.
4. *Note: If you enter an incorrect password 5 times, your account will be temporarily locked. Contact your Laboratory Administrator to unlock it.*

### 1.2 Session Timeout
For security compliance, LDMAS will automatically log you out after **30 minutes of inactivity**. If you step away from your terminal, please log out manually using the power icon (⏻) in the bottom-left corner of the screen.

---

## 2. Navigating the Dashboard

Once logged in, you will see the **Main Dashboard**. The interface is divided into three main areas:

*   **Top Bar**: Displays the current page name and database connection status (a green dot indicates you are safely connected to the server).
*   **Sidebar (Left Menu)**: Your primary navigation tool. Click any module (e.g., *Experiments*, *Equipment*) to switch views. The active module is highlighted in Teal.
*   **Main Workspace**: The central area where you view data, fill out forms, and review metrics.

### 2.1 The Dashboard View
The dashboard provides a quick overview of lab health:
*   **KPI Cards**: Quick numbers showing Active Experiments, Pending Approvals, Active Equipment, and Low Stock Inventory.
*   **Recent Experiments Table**: A quick list of the latest experiments updated in the system.
*   **Quick Actions**: Large buttons to instantly start a new experiment or register new equipment without navigating through menus.

---

## 3. Managing Equipment

### 3.1 Adding New Equipment
1. Click **Equipment** (⚙️) on the left sidebar.
2. Click the **Register Equipment** button at the top right.
3. Fill out the registration form:
    *   **Name & Model**: e.g., "Centrifuge 5000", "Eppendorf"
    *   **Serial Number**: Important for maintenance tracking.
    *   **Location**: e.g., "Room 4B"
    *   **Calibration Date**: Select the date the machine was last calibrated.
4. Click **Save**. The equipment is now tracked in the system.

### 3.2 Updating Status
If a machine breaks down, locate it in the Equipment list, click **Edit**, and change its status to *Out of Order* or *Under Maintenance*. This prevents other researchers from assigning it to new experiments.

---

## 4. Tracking Experiments & Results

### 4.1 Creating a New Experiment
1. Click **Experiments** (🧪) on the left sidebar.
2. Click **New Experiment**.
3. Enter the Title, Category (e.g., Chemistry, Biology), and Start Date.
4. The status will default to **Draft**. Once you begin work, change it to **In Progress**.

### 4.2 Inputting Test Results
1. Open an existing experiment from the list.
2. Navigate to the **Samples & Results** tab.
3. Click **Add Result**.
4. Enter the Parameter (e.g., "pH", "Conductivity"), the Measured Value, and the Unit.
5. *Automated Quality Control:* The system will instantly compare your input against the reference ranges and flag the result as **Pass** or **Fail**. Extreme outliers will be highlighted in red by the Anomaly Detection system.

---

## 5. Analytics & Exporting Data

LDMAS includes powerful tools to help you analyze your data for publication or review.

### 5.1 Viewing Parameter Statistics
Navigate to the **Reports** (📋) module. You can select an experiment and view statistical summaries (Averages, Min, Max, and Standard Deviation) automatically calculated for all your parameters.

### 5.2 Exporting Data to CSV (For Excel)
If you need to perform advanced charting or share data with a colleague:
1. Go to the **Reports** module.
2. Use the date filters to select the data you want.
3. Click the **Export to CSV** button.
4. Choose where to save the file on your computer.
5. The exported `.csv` file can be opened directly in Microsoft Excel, R, or Python for further analysis.

---

## 6. Audit Trail

If you have Auditor or Administrator privileges, you will see the **Audit Trail** (🔍) option in the sidebar. This module tracks every change made in the system (who edited what, and when). This is a read-only list designed to maintain strict laboratory compliance. 
