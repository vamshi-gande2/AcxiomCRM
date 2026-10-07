# AcxiomCRM - Enterprise Customer Relationship Management System

AcxiomCRM is a production-grade, role-based CRM application engineered for the **Acxiom Technical Assessment**. It manages the end-to-end sales lifecycle—from lead capture and automated conversion to opportunity pipeline management, follow-up scheduling, activity tracking, executive analytics, and append-only audit logging.

Built with **ASP.NET Core (.NET 10 / 8 compatible)**, **Entity Framework Core**, **ASP.NET Core Identity**, **Bootstrap 5**, and **Chart.js**.

---

## 🏗️ Architecture & Solution Structure

The project follows a Clean Layered Architecture with strict separation of concerns:

```
AcxiomCRM/
│
├── AcxiomCRM.Core/             # Domain & Application Layer (Class Library)
│   ├── Data/                   # EF Core DbContext, Model Configuration & Auto-Seeding
│   ├── Models/                 # Core Entities (Customer, Lead, Opportunity, FollowUp, Activity, AuditLog, ApplicationUser)
│   ├── Services/               # Business Logic, Scoped Queries, Validations & Audit Services
│   ├── ViewModels/             # Strongly-typed models for MVC Razor Views & Dashboards
│   └── DTOs/                   # Data Transfer Objects & API Envelopes for REST APIs
│
├── AcxiomCRM.Web/              # Presentation & API Layer (ASP.NET Core MVC)
│   ├── Controllers/            # MVC Controllers (Role-based access, CSRF-protected)
│   │   └── Api/                # RESTful API Controllers (/api/auth, /api/customers, /api/leads, etc.)
│   ├── Views/                  # Responsive Bootstrap 5 Razor Views
│   ├── wwwroot/                # Static assets, CSS, JavaScript, and Chart.js integration
│   └── Program.cs              # DI configuration, Identity security policies, and DB migration
│
└── AcxiomCRM.Tests/            # Automated Test Suite (xUnit)
    ├── OpportunityValidationTests.cs
    ├── FollowUpValidationTests.cs
    ├── CustomerServiceTests.cs
    ├── LeadConversionTests.cs
    ├── RoleScopingTests.cs
    └── AuditLogTests.cs
```

---

## 👥 Default User Accounts & Roles

The system automatically initializes and seeds the SQLite database (`acxiomcrm.db`) on application startup with the following test credentials:

| Role | Email | Password | Access Scope |
| :--- | :--- | :--- | :--- |
| **Admin** | `admin@acxiom.com` | `Admin@12345` | Full system access, User & Role administration, Audit Logs, all CRM records, reports. |
| **Manager** | `manager@acxiom.com` | `Manager@12345` | Department pipeline monitoring, team leads/opportunities, management reports, audit logs. |
| **SalesExecutive** | `sales@acxiom.com` | `Sales@12345` | Restricted strictly to assigned customers, leads, opportunities, follow-ups, and own sales KPIs. |

> **Tip:** The login screen includes a **1-Click Demo Fill** widget for rapid testing of each role during evaluation.

---

## 🚀 Quick Start Guide

### Prerequisites
- [.NET SDK 8.0, 9.0, or 10.0](https://dotnet.microsoft.com/download)
- Git

### 1. Clone & Navigate
```bash
git clone https://github.com/vamshi-gande2/AcxiomCRM.git
cd AcxiomCRM
```

### 2. Run the Application
```bash
dotnet run --project AcxiomCRM.Web
```
The database (`acxiomcrm.db`) will be automatically created, migrated, and seeded with sample accounts and data on first run. Open your browser and navigate to:
```
http://localhost:5022
```

### 3. Run Automated Tests
```bash
dotnet test
```
All **20 test cases** across business validations, role scoping, lead conversion, and audit logging will execute and pass.

---

## 📋 Acceptance Criteria Compliance Matrix

| # | Acceptance Scenario (Section 17.19) | Implementation & Verification | Status |
|---|---|---|:---:|
| 1 | **Anonymous Access Protection** | Protected CRM pages require authentication; unauthorized requests redirect to `/Account/Login`. |  Passed |
| 2 | **Authentication & Redirect** | Valid login redirects users directly to their role-scoped Dashboard. |  Passed |
| 3 | **Client-Side Validation** | Required fields, email format, and 10-digit phone regex validated before form submission. |  Passed |
| 4 | **Server-Side Validation** | Duplicate customer email and phone prevention enforced on server. |  Passed |
| 5 | **Opportunity Amount > 0** | Rejects `Amount <= 0` with *"Opportunity Amount must be greater than 0."* |  Passed |
| 6 | **Probability 0–100** | Rejects `Probability < 0` or `> 100` with *"Probability must be between 0 and 100."* |  Passed |
| 7 | **Opportunity Close Date** | Rejects past close dates for active deals with *"Expected Close Date cannot be in the past."* |  Passed |
| 8 | **Follow-Up Date Rule** | Rejects dates earlier than today with *"Follow-up date cannot be earlier than today."* |  Passed |
| 9 | **Sales Executive Scope** | Sales reps only see and manage records assigned to them (`AssignedTo` / `CreatedBy`). |  Passed |
| 10 | **Manager Scope** | Managers have visibility into team pipeline, conversion rates, and executive reports. |  Passed |
| 11 | **Admin Administration** | Admins manage user accounts, assign roles, unlock locked users, and view full audit logs. |  Passed |
| 12 | **Audit Trail Logging** | Append-only audit table logs Login, Failed Login, Create, Update, Delete, and Role changes. |  Passed |
| 13 | **REST API Support** | Standard REST endpoints (`/api/customers`, `/api/leads`, etc.) return structured JSON with DTOs. |  Passed |
| 14 | **Analytics & Charts** | Real-time KPI summary cards and interactive Chart.js visualizations (Lead Status, Pipeline Stages, Monthly Revenue). |  Passed |

---

## 🔒 Security Implementations

- **ASP.NET Core Identity**: Framework-managed PBKDF2 password hashing (no plain-text passwords stored).
- **Password Complexity Policy**: Minimum 8 characters, uppercase, lowercase, digit, and special symbol.
- **Account Lockout**: Automatically locks user accounts for 15 minutes after 5 consecutive failed login attempts; audit logs security events.
- **Anti-Forgery Protection**: State-changing POST forms protected with `[ValidateAntiForgeryToken]`.
- **SQL Injection Prevention**: Entity Framework Core parameterized query APIs used throughout.
- **Role-Based Authorization**: Controller and action-level authorization attributes (`[Authorize(Roles = "...")]`).

---

## 📡 REST API Reference

| Method | Endpoint | Description | Auth Required |
|---|---|---|:---:|
| `POST` | `/api/auth/login` | Authenticate user & retrieve session info | No (Public) |
| `POST` | `/api/auth/logout` | Terminate session & log audit event | Yes |
| `GET` | `/api/customers` | List all scoped customers (supports `?search=&status=`) | Yes |
| `GET` | `/api/customers/{id}` | Get customer by ID | Yes |
| `POST` | `/api/customers` | Create a new customer | Yes |
| `PUT` | `/api/customers/{id}` | Update existing customer | Yes |
| `DELETE`| `/api/customers/{id}` | Delete customer (Admin / Manager only) | Yes |
| `GET` | `/api/leads` | List all scoped prospective leads | Yes |
| `POST` | `/api/leads` | Create a new lead | Yes |
| `GET` | `/api/opportunities` | List sales opportunities | Yes |
| `POST` | `/api/opportunities` | Create sales deal (validates amount, probability, close date) | Yes |
| `GET` | `/api/followups` | List scheduled follow-ups | Yes |
| `POST` | `/api/followups` | Schedule new follow-up (validates date $\ge$ today) | Yes |
| `GET` | `/api/reports/pipeline` | Retrieve pipeline metrics, stage aggregates & weighted revenue | Yes |

---

## 📊 Core CRM Workflow: Lead-to-Customer

1. **Lead Capture**: Create prospective lead with source, contact info, and expected value.
2. **Contact & Qualify**: Update lead status from `New` $\rightarrow$ `Contacted` $\rightarrow$ `Qualified`.
3. **One-Click Conversion**: On the Lead Details screen, click **"Convert to Customer & Deal"**.
4. **Automated Pipeline**: The system automatically creates a `Customer` account, generates an `Opportunity` deal, sets the lead status to `Converted`, and writes a tamper-proof audit record.
