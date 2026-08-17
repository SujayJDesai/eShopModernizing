# Security Assessment Report

**Generated:** 2026-08-17T09:12:32.6490000Z

## Summary

| Metric | Count |
|--------|-------|
| Total Findings | 12 |
| CVE Vulnerabilities | 4 |
| CWE Vulnerabilities | 8 |
| Total Rules Assessed | 59 |
| Rules Passed | 51 |

### By Severity

| Severity | Count |
|----------|-------|
| mandatory | 6 |
| optional | 2 |
| potential | 4 |

## CVE Findings (Dependency Vulnerabilities)

### CVE-2024-21907: Improper Handling of Exceptional Conditions in Newtonsoft.Json
- **Severity:** mandatory
- **Story Points:** 1
- **Files:** eShopLegacyMVCSolution/src/eShopLegacyMVC/packages.config:29

[CVE-2024-21907](https://github.com/advisories/GHSA-5crp-9r3c-p9vr): Improper Handling of Exceptional Conditions in Newtonsoft.Json

Severity: HIGH

Affected dependencies:
  - Newtonsoft.Json:12.0.1 (declared at eShopLegacyMVCSolution/src/eShopLegacyMVC/packages.config:29)

Recommended fix:
  - Upgrade Newtonsoft.Json to 13.0.1 or later

### CVE-2025-55315: Microsoft Security Advisory CVE-2025-55315: .NET Security Feature Bypass Vulnerability
- **Severity:** mandatory
- **Story Points:** 1
- **Files:** eShopLegacyMVCSolution/eShopPorted/eShopPorted.csproj:7

[CVE-2025-55315](https://github.com/advisories/GHSA-5rrx-jjjq-q2r5): Microsoft Security Advisory CVE-2025-55315: .NET Security Feature Bypass Vulnerability

Severity: CRITICAL

Affected dependencies:
  - Microsoft.AspNetCore.Server.Kestrel.Core:2.2.0 (transitive, pulled by Microsoft.AspNetCore at eShopLegacyMVCSolution/eShopPorted/eShopPorted.csproj:7)

Recommended fix:
  - Upgrade to a version of ASP.NET Core runtime with a patched Kestrel.Core (>= 2.3.6, or move to a supported .NET runtime)

### CVE-2024-0056: Microsoft.Data.SqlClient and System.Data.SqlClient vulnerable to SQL Data Provider Security Feature Bypass
- **Severity:** mandatory
- **Story Points:** 1
- **Files:** eShopLegacyMVCSolution/eShopPorted/eShopPorted.csproj:22

[CVE-2024-0056](https://github.com/advisories/GHSA-98g6-xh36-x2p7): Microsoft.Data.SqlClient and System.Data.SqlClient vulnerable to SQL Data Provider Security Feature Bypass

Severity: HIGH

Affected dependencies:
  - System.Data.SqlClient:4.6.1 (transitive, pulled by Microsoft.EntityFrameworkCore.SqlServer at eShopLegacyMVCSolution/eShopPorted/eShopPorted.csproj:22)

Recommended fix:
  - Upgrade System.Data.SqlClient to 4.8.6 or later

### CVE-2021-26701: .NET Core Remote Code Execution Vulnerability
- **Severity:** mandatory
- **Story Points:** 1
- **Files:** eShopLegacyMVCSolution/eShopPorted/eShopPorted.csproj:7

[CVE-2021-26701](https://github.com/advisories/GHSA-ghhp-997w-qr28): .NET Core Remote Code Execution Vulnerability

Severity: CRITICAL

Affected dependencies:
  - System.Text.Encodings.Web:4.5.0 (transitive, pulled by Microsoft.AspNetCore at eShopLegacyMVCSolution/eShopPorted/eShopPorted.csproj:7)

Recommended fix:
  - Upgrade System.Text.Encodings.Web to 4.5.1 or later

## CWE Findings (Code-Level Vulnerabilities)

### CWE-681: Incorrect Conversion between Numeric Types
- **Category:** Code Quality
- **Severity:** potential
- **Story Points:** 3
- **Files:** eShopLegacyMVCSolution/src/eShopLegacyMVC/Models/CatalogItemHiLoGenerator.cs, eShopLegacyMVCSolution/src/eShopLegacyMVC/Models/Infrastructure/CatalogDBInitializer.cs

In CatalogItemHiLoGenerator.GetNextSequenceValue(CatalogDBContext db) at line 23, the result of db.Database.SqlQuery<Int64>(...).Single() (an Int64) is narrowed with an unchecked (int) cast: sequenceId = (int)rawQuery.Single(). The same pattern occurs in CatalogDBInitializer.GetSequenceIdFromSelectedDBSequence at line 329: var sequenceId = (int)rawQuery.Single(). If the underlying database sequence value exceeds Int32.MaxValue, the narrowing conversion silently truncates/overflows the value, producing an incorrect sequenceId used as an entity's HiLo key generator seed.

### CWE-543: Use of Singleton Pattern Without Synchronization in a Multithreaded Context
- **Category:** Concurrency & Synchronization
- **Severity:** potential
- **Story Points:** 5
- **Files:** eShopLegacyMVCSolution/src/eShopLegacyMVC/Services/CatalogServiceMock.cs, eShopLegacyMVCSolution/src/eShopLegacyMVC/Modules/ApplicationModule.cs

CatalogServiceMock is registered with Autofac as a SingleInstance() service (see ApplicationModule.cs:22-23, applied when useMockData is true), so a single instance is shared across all concurrent ASP.NET MVC request threads. It holds a mutable private field `List<CatalogItem> catalogItems` (CatalogServiceMock.cs:12) that is read and mutated by CreateCatalogItem (lines 48-52), UpdateCatalogItem (lines 55-61), and RemoveCatalogItem (lines 64-67) with no lock, Monitor, or other synchronization primitive protecting access to the list. Concurrent requests calling these methods can race and corrupt the shared list (e.g. lost updates, InvalidOperationException from concurrent enumeration/mutation, or duplicate Id values from the unsynchronized `catalogItems.Max(i => i.Id)` + increment in CreateCatalogItem).

### CWE-567: Unsynchronized Access to Shared Data in a Multithreaded Context
- **Category:** Concurrency & Synchronization
- **Severity:** potential
- **Story Points:** 5
- **Files:** eShopLegacyMVCSolution/src/eShopLegacyMVC/Services/CatalogServiceMock.cs, eShopLegacyMVCSolution/src/eShopLegacyMVC/Modules/ApplicationModule.cs

CatalogServiceMock is registered with Autofac as a SingleInstance() service (see ApplicationModule.cs:22-23, applied when useMockData is true), so a single instance is shared across all concurrent ASP.NET MVC request threads. It holds a mutable private field `List<CatalogItem> catalogItems` (CatalogServiceMock.cs:12) that is read and mutated by CreateCatalogItem (lines 48-52), UpdateCatalogItem (lines 55-61), and RemoveCatalogItem (lines 64-67) with no lock, Monitor, or other synchronization primitive protecting access to the list. Concurrent requests calling these methods can race and corrupt the shared list (e.g. lost updates, InvalidOperationException from concurrent enumeration/mutation, or duplicate Id values from the unsynchronized `catalogItems.Max(i => i.Id)` + increment in CreateCatalogItem).

### CWE-820: Missing Synchronization
- **Category:** Concurrency & Synchronization
- **Severity:** potential
- **Story Points:** 8
- **Files:** eShopLegacyMVCSolution/src/eShopLegacyMVC/Services/CatalogServiceMock.cs, eShopLegacyMVCSolution/src/eShopLegacyMVC/Modules/ApplicationModule.cs

CatalogServiceMock is registered with Autofac as a SingleInstance() service (see ApplicationModule.cs:22-23, applied when useMockData is true), so a single instance is shared across all concurrent ASP.NET MVC request threads. It holds a mutable private field `List<CatalogItem> catalogItems` (CatalogServiceMock.cs:12) that is read and mutated by CreateCatalogItem (lines 48-52), UpdateCatalogItem (lines 55-61), and RemoveCatalogItem (lines 64-67) with no lock, Monitor, or other synchronization primitive protecting access to the list. Concurrent requests calling these methods can race and corrupt the shared list (e.g. lost updates, InvalidOperationException from concurrent enumeration/mutation, or duplicate Id values from the unsynchronized `catalogItems.Max(i => i.Id)` + increment in CreateCatalogItem).

### CWE-22: Improper Limitation of a Pathname to a Restricted Directory ('Path Traversal')
- **Category:** File & Path Security
- **Severity:** optional
- **Story Points:** 8
- **Files:** eShopLegacyMVCSolution/src/eShopLegacyMVC/Controllers/PicController.cs, eShopLegacyMVCSolution/eShopPorted/Controllers/PicController.cs

In PicController.Index(int catalogItemId) the picture file name (item.PictureFileName, sourced from the CatalogItem entity) is concatenated into a filesystem path with Path.Combine(webRoot, item.PictureFileName) at src/eShopLegacyMVC/Controllers/PicController.cs:39 and then read directly with System.IO.File.ReadAllBytes(path) at line 44 (same pattern at eShopPorted/Controllers/PicController.cs:40,45). The PictureFileName value is never validated or sanitized to strip path separators or '..' sequences before being used to build the file path, so a value containing traversal sequences could cause the file read to escape the intended '~/Pics' directory.

### CWE-23: Relative Path Traversal
- **Category:** File & Path Security
- **Severity:** optional
- **Story Points:** 5
- **Files:** eShopLegacyMVCSolution/src/eShopLegacyMVC/Controllers/PicController.cs, eShopLegacyMVCSolution/eShopPorted/Controllers/PicController.cs

Same code path as CWE-22: item.PictureFileName is combined with the '~/Pics' web root via Path.Combine at src/eShopLegacyMVC/Controllers/PicController.cs:39 (and eShopPorted/Controllers/PicController.cs:40) without stripping relative traversal sequences like '..', allowing the resolved path to escape the restricted Pics directory.

### CWE-89: Improper Neutralization of Special Elements used in an SQL Command ('SQL Injection')
- **Category:** Injection Attacks
- **Severity:** mandatory
- **Story Points:** 13
- **Files:** eShopLegacyMVCSolution/src/eShopLegacyMVC/Models/Infrastructure/CatalogDBInitializer.cs

In CatalogDBInitializer.GetSequenceIdFromSelectedDBSequence(CatalogDBContext context, string dBSequenceName) at line 328, a raw SQL statement is built via string interpolation: context.Database.SqlQuery<Int64>($"SELECT NEXT VALUE FOR {dBSequenceName}") instead of using a parameterized query. The dBSequenceName value is concatenated directly into the SQL text, so any caller passing externally-influenced data through this method would allow SQL injection.

### CWE-502: Deserialization of Untrusted Data
- **Category:** Injection Attacks
- **Severity:** mandatory
- **Story Points:** 13
- **Files:** eShopLegacyMVCSolution/eShopLegacy.Utilities/Serializing.cs

Serializing.DeserializeBinary(Stream stream) at lines 17-21 deserializes an arbitrary input Stream using System.Runtime.Serialization.Formatters.Binary.BinaryFormatter, which is well known to allow arbitrary code execution when deserializing untrusted/attacker-controlled data. The method accepts any Stream with no validation of its origin or content prior to deserialization.
