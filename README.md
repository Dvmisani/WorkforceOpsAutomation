# Workforce Ops Automation
### Automated IT Asset & Contractor Onboarding Engine

> **Target Division:** Application Management & Contingent Workforce  
> **Stack:** C# / .NET 8 · MySQL · JSON  
> **Author:** Dvmisani

---

## Overview

This tool automates the full contractor onboarding lifecycle — from reading an HR ticket through to provisioning accounts, assigning virtual assets, enforcing RBAC policies, and generating a tamper-proof compliance audit log.

A process that would normally take an IT team 2–4 hours of manual effort is reduced to **milliseconds**.

---

## Architecture

```
┌─────────────────────┐     JSON      ┌──────────────────────┐
│  HR Input Payload   │ ──────────→  │   Validation Engine   │
│  new_contractor.json│              │  (RBAC rules, sanitise)│
└─────────────────────┘              └──────────┬───────────┘
                                                │  PASS
                                     ┌──────────▼───────────┐
                                     │   Onboarding Engine   │
                                     │   (orchestrates all   │
                                     │    business logic)    │
                                     └──────────┬───────────┘
                            ┌──────────────────┼─────────────────┐
                            │                  │                  │
                   ┌────────▼───────┐ ┌────────▼──────┐ ┌────────▼───────┐
                   │  Database Svc  │ │  Profile Build │ │  Compliance Log│
                   │  (MySQL CRUD)  │ │  (email, groups│ │  (JSON output) │
                   └────────────────┘ │   licenses)    │ └────────────────┘
                                      └────────────────┘
```

---

## Database Schema

### `Assets` Table
| Column | Type | Description |
|--------|------|-------------|
| AssetId | INT PK | Auto-increment identifier |
| AssetTag | VARCHAR(50) | Unique asset reference (e.g. `ASSET-V001`) |
| AssetType | VARCHAR(50) | `Virtual Laptop`, `Cloud License`, `Security Token` |
| Model | VARCHAR(100) | Hardware/software model name |
| Status | ENUM | `Available` · `Assigned` · `Retired` |
| LicenseKey | VARCHAR(100) | License key (if applicable) |
| AssignedAt | DATETIME | Timestamp of assignment |

### `Contractors` Table
| Column | Type | Description |
|--------|------|-------------|
| ContractorId | INT PK | Auto-increment identifier |
| ContractorName | VARCHAR(100) | Full legal name |
| CorporateEmail | VARCHAR(150) | Auto-generated internal email |
| Role | VARCHAR(100) | Validated job role |
| Department | VARCHAR(100) | Validated department |
| AccessLevel | VARCHAR(20) | `Tier-1` · `Tier-2` · `Tier-3` |
| SystemGroups | TEXT | Comma-separated RBAC groups |
| SoftwareLicenses | TEXT | Comma-separated provisioned licenses |
| AssignedAssetId | INT FK | References `Assets.AssetId` |
| ContractExpiry | DATE | Auto-calculated from contract duration |
| OnboardedAt | DATETIME | UTC timestamp of onboarding |

---

## RBAC Policy Map

| Access Level | System Groups | Software Licenses |
|---|---|---|
| **Tier-1** | ReadOnly_Monitoring, ServiceDesk_Basic, VPN_Standard | Microsoft365_Basic, Monitoring_Dashboard_ReadOnly, Slack |
| **Tier-2** | CloudOps_Contributor, Incident_Response, VPN_Standard | Microsoft365_Business, Azure_Portal_Contributor, Jira, Slack |
| **Tier-3** | CloudOps_Admin, Security_Response, VPN_PrivilegedAccess | Microsoft365_E3, Azure_Portal_Admin, Jira_Premium, PagerDuty, Slack |

---

## Setup

### Prerequisites
- .NET 8 SDK: https://dotnet.microsoft.com/download
- MySQL Workbench with a local MySQL server running

### 1. Initialise the Database

Open MySQL Workbench, connect to `localhost`, and run:

```sql
-- database/schema.sql
```

This creates the `workforce_ops` database, both tables, and seeds 10 virtual assets.

### 2. Configure the Connection String

Open `src/Program.cs` and update line 11:

```csharp
private const string ConnectionString =
    "Server=localhost;Port=3306;Database=workforce_ops;Uid=root;Pwd=YOUR_PASSWORD_HERE;";
```

Replace `YOUR_PASSWORD_HERE` with your MySQL root password.

### 3. Restore NuGet Packages

```bash
cd src
dotnet restore
```

### 4. Build

```bash
dotnet build
```

---

## Running the Three Tests

### ✅ Test 1 — Success Path

```bash
dotnet run -- ../samples/new_contractor.json
```

**Expected:** Database updates, `output/onboarding_compliance_log_*.json` created with `"Status": "SUCCESS"`.

---

### ⚠️ Test 2 — Zero Inventory

In MySQL Workbench, run:
```sql
UPDATE Assets SET Status = 'Retired' WHERE Status = 'Available';
```

Then run:
```bash
dotnet run -- ../samples/new_contractor.json
```

**Expected:** Graceful halt with `"Status": "FAILED"` and `ALLOCATION_FAILURE` message in the log. No database insert occurs. No crash.

To restore inventory:
```sql
UPDATE Assets SET Status = 'Available', AssignedAt = NULL WHERE Status = 'Retired';
```

---

### 🛡️ Test 3 — Malicious Input

```bash
dotnet run -- ../samples/malicious_contractor.json
```

**Expected:** Validation engine rejects the payload immediately with `DATA_INTEGRITY_VIOLATION`. The database is never touched.

---

## Example Output Log

See `samples/example_compliance_log_SUCCESS.json` for a full annotated example of a successful run's audit artifact.

---

## Project Structure

```
WorkforceOpsAutomation/
├── src/
│   ├── WorkforceOpsAutomation.csproj   # Project & NuGet dependencies
│   ├── Program.cs                       # CLI entry point
│   ├── Models.cs                        # Data models
│   ├── ValidationEngine.cs             # RBAC rules & input sanitisation
│   ├── DatabaseService.cs              # MySQL CRUD operations
│   └── OnboardingEngine.cs             # Core orchestration logic
├── database/
│   └── schema.sql                      # Full DB setup script
├── samples/
│   ├── new_contractor.json             # Success path payload
│   ├── malicious_contractor.json       # Malicious input payload
│   └── example_compliance_log_SUCCESS.json
└── README.md
```

---

## Key Engineering Decisions

- **Parameterised queries throughout** — eliminates SQL injection at the database layer even if validation is bypassed.
- **Atomic asset reservation** — the `UPDATE ... WHERE Status = 'Available'` pattern prevents race conditions in concurrent runs.
- **Immutable audit log** — each run generates a unique `RunId` (UUID). Logs are append-only files, never overwritten.
- **Exit codes** — `0` = success, `1` = startup error, `2` = onboarding failure. Enables CI/CD pipeline integration.
