# .NET 10 Upgrade Plan — JobFlow-API

## Table of Contents
1. [Executive Summary](#1-executive-summary)
2. [Migration Strategy](#2-migration-strategy)
3. [Detailed Dependency Analysis](#3-detailed-dependency-analysis)
4. [Project-by-Project Plans](#4-project-by-project-plans)
5. [Package Update Reference](#5-package-update-reference)
6. [Breaking Changes Catalog](#6-breaking-changes-catalog)
7. [Testing & Validation Strategy](#7-testing--validation-strategy)
8. [Risk Management](#8-risk-management)
9. [Complexity & Effort Assessment](#9-complexity--effort-assessment)
10. [Source Control](#10-source-control)
11. [Success Criteria](#11-success-criteria)

---

## 1. Executive Summary
Target: upgrade the solution from `net8.0` to `net10.0`.

Assessment highlights:
- 5 projects, 52 total items flagged (10 mandatory).
- Most issues are concentrated in the top-level ASP.NET Core app `JobFlow.API` and the library `JobFlow.Infrastructure`.
- Primary workstreams:
  - Update `TargetFramework` to `net10.0` across all projects.
  - Update/replace incompatible or deprecated NuGet packages.
  - Validate API/source compatibility and behavioral changes (per assessment rules: `Api.0001`, `Api.0002`, `Api.0003`).
  - Remove or replace deprecated packages.

---

## 2. Migration Strategy
Use an **incremental, dependency-ordered upgrade**:
1. Upgrade foundational libraries first (Domain → Business / Persistence → Infrastructure).
2. Upgrade the ASP.NET Core host last (`JobFlow.API`) because it has the majority of package and API surface issues.

At each step:
- Update target framework.
- Restore/build.
- Fix compile errors.
- Re-run tests / smoke checks.

---

## 3. Detailed Dependency Analysis
Upgrade order (from assessment dependency graph):
1. `JobFlow.Domain` (Level 0)
2. `JobFlow.Business` and `JobFlow.Infrastructure.Persistence` (Level 1)
3. `JobFlow.Infrastructure` (Level 2)
4. `JobFlow.API` (Level 3)

This ordering keeps downstream projects compiling as soon as possible and limits error fan-out.

---

## 4. Project-by-Project Plans

### 4.1 `JobFlow.Domain` (`net8.0` → `net10.0`)
- Change `TargetFramework` to `net10.0`.
- Restore/build.

### 4.2 `JobFlow.Business` (`net8.0` → `net10.0`)
- Change `TargetFramework` to `net10.0`.
- Apply recommended NuGet upgrades flagged by `NuGet.0002`.
- Restore/build.

### 4.3 `JobFlow.Infrastructure.Persistence` (`net8.0` → `net10.0`)
- Change `TargetFramework` to `net10.0`.
- Apply recommended NuGet upgrades flagged by `NuGet.0002`.
- Restore/build.

### 4.4 `JobFlow.Infrastructure` (`net8.0` → `net10.0`)
- Change `TargetFramework` to `net10.0`.
- Address assessment flags:
  - `Api.0002` (source incompatibilities)
  - `Api.0003` (behavioral changes)
  - `NuGet.0003` (functionality now included with framework reference)
- Restore/build and fix compile issues.

### 4.5 `JobFlow.API` (`net8.0` → `net10.0`)
- Change `TargetFramework` to `net10.0`.
- Address assessment flags:
  - `Api.0001` (binary incompatibilities)
  - `Api.0002` (source incompatibilities)
  - `Api.0003` (behavioral changes)
  - `NuGet.0001` (incompatible packages)
  - `NuGet.0002` (recommended package upgrades)
  - `NuGet.0005` (deprecated packages)
- Restore/build and fix compile issues.

---

## 5. Package Update Reference
From assessment (`list_packages`):

### 5.1 Packages to upgrade (recommended)
- `Microsoft.AspNetCore.Authentication.JwtBearer`: `8.0.14` → `10.0.3`
- `Microsoft.AspNetCore.Identity.EntityFrameworkCore`: `8.0.14` → `10.0.3`
- `Microsoft.EntityFrameworkCore` (+ Design / SqlServer / Tools): `9.0.2` → `10.0.3`
- `System.Text.Json`: `9.0.2` → `10.0.3`

### 5.2 Packages to replace
- `Microsoft.AspNetCore.SignalR` (1.2.0)
  - Replace with: `Microsoft.AspNetCore.SignalR.Client` `10.0.3`

### 5.3 Packages to remove / incompatible
- `Microsoft.AspNet.WebApi.Core` (5.3.0) is flagged incompatible.
  - Action: remove usage or migrate to ASP.NET Core equivalents.

### 5.4 Deprecated packages (verify and update strategy)
- `Azure.Identity` (1.13.2) flagged deprecated in assessment.
- `FluentValidation.AspNetCore` (11.3.0) flagged deprecated in assessment.

> Note: For deprecated packages, prefer newer supported packages/approaches per vendor guidance, and confirm whether the deprecated status is about the package itself or the integration style.

---

## 6. Breaking Changes Catalog
Use the assessment rule categories to drive investigation:
- `Api.0001` — binary incompatibilities
  - Usually caused by outdated framework packages or packages compiled against older assemblies.
- `Api.0002` — source incompatibilities
  - Fix compile errors by adjusting APIs / namespaces.
- `Api.0003` — behavioral changes
  - Validate runtime behavior in auth, JSON serialization, EF Core, hosting, and background jobs.

Concrete known hotspots in this solution based on packages:
- ASP.NET Core Authentication / Identity
- EF Core major version alignment
- JSON serialization (`System.Text.Json`)
- SignalR client package change
- Legacy ASP.NET WebApi package (`Microsoft.AspNet.WebApi.Core`) removal/migration

---

## 7. Testing & Validation Strategy
Minimum checkpoints:
1. `dotnet restore` after each project/framework change.
2. `dotnet build` for the full solution after completing each dependency level.
3. Run unit/integration tests (if present) after upgrading EF Core / auth packages.
4. Smoke test `JobFlow.API`:
   - App starts.
   - Health endpoint responds (if available).
   - Auth endpoints validate token creation/validation flows.
   - Database migrations (if used) apply successfully to a test DB.

---

## 8. Risk Management
Top risks and mitigations:
- **Legacy ASP.NET WebApi dependency** (`Microsoft.AspNet.WebApi.Core`) blocks `net10.0`.
  - Mitigation: remove and migrate code paths to ASP.NET Core MVC (`Microsoft.AspNetCore.Mvc`).
- **EF Core major version mismatch**.
  - Mitigation: ensure all `Microsoft.EntityFrameworkCore*` packages align to the same `10.0.x` version.
- **Runtime behavioral changes**.
  - Mitigation: add/execute targeted tests around serialization, auth, and persistence.

---

## 9. Complexity & Effort Assessment
- Low: framework target updates in library projects.
- Medium: package alignment (ASP.NET Core packages + EF Core).
- High: removal/migration away from `Microsoft.AspNet.WebApi.Core` if actively used.

---

## 10. Source Control
- Source branch: `feature/schedule`
- Working branch: `upgrade-to-NET10`
- Commit strategy:
  - Commit per logical unit: (1) TFM updates, (2) package upgrades, (3) compatibility fixes.

---

## 11. Success Criteria
Upgrade is considered complete when:
- All projects target `net10.0`.
- Solution builds cleanly.
- Tests pass (or are updated with documented rationale).
- API host runs and core user flows validate (auth + persistence + background processing).
