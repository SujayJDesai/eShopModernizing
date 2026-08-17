# Security Assessment Report

**Generated:** 2026-08-17T09:10:53.0000000Z

## Summary

| Metric | Count |
|--------|-------|
| Total Findings | 9 |
| CVE Vulnerabilities | 5 |
| CWE Vulnerabilities | 4 |
| Total Rules Assessed | 59 |
| Rules Passed | 55 |

### By Severity

| Severity | Count |
|----------|-------|
| mandatory | 5 |
| optional | 2 |
| potential | 2 |

## CVE Findings (Dependency Vulnerabilities)

### CVE-2018-8292: .NET Core Information Disclosure
- **Severity:** mandatory
- **Story Points:** 1
- **Files:** eShopModernizedNTier/src/eShopWinForms/eShopWinForms.csproj

[CVE-2018-8292](https://github.com/advisories/GHSA-7jgj-8wvc-jh57): .NET Core Information Disclosure

Severity: HIGH

Affected dependencies:
  - System.Net.Http:4.3.0 (declared at eShopModernizedNTier/src/eShopWinForms/eShopWinForms.csproj)

Recommended fix:
  - Upgrade System.Net.Http to 4.3.4 or later

### CVE-2019-0820: Regular Expression Denial of Service in System.Text.RegularExpressions
- **Severity:** mandatory
- **Story Points:** 1
- **Files:** eShopModernizedNTier/src/eShopWinForms/eShopWinForms.csproj

[CVE-2019-0820](https://github.com/advisories/GHSA-cmhx-cq75-c4mj): Regular Expression Denial of Service in System.Text.RegularExpressions

Severity: HIGH

Affected dependencies:
  - System.Text.RegularExpressions:4.3.0 (declared at eShopModernizedNTier/src/eShopWinForms/eShopWinForms.csproj)

Recommended fix:
  - Upgrade System.Text.RegularExpressions to 4.3.1 or later

### CVE-2021-24112: .NET Core Remote Code Execution Vulnerability
- **Severity:** mandatory
- **Story Points:** 1
- **Files:** eShopModernizedNTier/src/eShopWinForms/eShopWinForms.csproj

[CVE-2021-24112](https://github.com/advisories/GHSA-rxg9-xrhp-64gj): .NET Core Remote Code Execution Vulnerability

Severity: CRITICAL

Affected dependencies:
  - System.Drawing.Common:4.7.0 (declared at eShopModernizedNTier/src/eShopWinForms/eShopWinForms.csproj)

Recommended fix:
  - Upgrade System.Drawing.Common to 4.7.2 or later

### CVE-2024-0056: Microsoft.Data.SqlClient and System.Data.SqlClient vulnerable to SQL Data Provider Security Feature Bypass 
- **Severity:** mandatory
- **Story Points:** 1
- **Files:** eShopModernizedNTier/src/eShopWinForms/eShopWinForms.csproj

[CVE-2024-0056](https://github.com/advisories/GHSA-98g6-xh36-x2p7): Microsoft.Data.SqlClient and System.Data.SqlClient vulnerable to SQL Data Provider Security Feature Bypass 

Severity: HIGH

Affected dependencies:
  - System.Data.SqlClient:4.8.1 (declared at eShopModernizedNTier/src/eShopWinForms/eShopWinForms.csproj)

Recommended fix:
  - Upgrade System.Data.SqlClient to 4.8.6 or later

### CVE-2024-21907: Improper Handling of Exceptional Conditions in Newtonsoft.Json
- **Severity:** mandatory
- **Story Points:** 1
- **Files:** eShopModernizedNTier/src/eShopWinForms/eShopWinForms.csproj, eShopModernizedNTier/src/eShopWinForms/eShopWinForms.fx.csproj

[CVE-2024-21907](https://github.com/advisories/GHSA-5crp-9r3c-p9vr): Improper Handling of Exceptional Conditions in Newtonsoft.Json

Severity: HIGH

Affected dependencies:
  - Newtonsoft.Json:10.0.1 (declared at eShopModernizedNTier/src/eShopWinForms/eShopWinForms.csproj)
  - Newtonsoft.Json:6.0.4 (declared at eShopModernizedNTier/src/eShopWinForms/eShopWinForms.fx.csproj)

Recommended fix:
  - Upgrade Newtonsoft.Json to 13.0.1 or later

## CWE Findings (Code-Level Vulnerabilities)

### CWE-681: Incorrect Conversion between Numeric Types
- **Category:** Code Quality
- **Severity:** potential
- **Story Points:** 3
- **Files:** eShopModernizedNTier/src/eShopWCFService/Models/CatalogItemHiLoGenerator.cs

In CatalogItemHiLoGenerator.GetNextSequenceValue (line 21-22), the result of the SQL Server 'NEXT VALUE FOR catalog_hilo' sequence is fetched as an Int64 and then narrowed to int via an unchecked cast '(int)rawQuery.Single()'. If the underlying sequence value exceeds Int32.MaxValue, this cast silently truncates/wraps the value instead of throwing, producing an incorrect sequenceId used as an entity key generator.

### CWE-778: Insufficient Logging
- **Category:** Credentials & Secrets
- **Severity:** potential
- **Story Points:** 3
- **Files:** eShopModernizedNTier/src/eShopWCFService/CatalogService.svc.cs, eShopModernizedNTier/src/eShopWinForms/Controllers/CatalogController.cs

No logging framework (e.g., ILogger, Trace, or any log calls) is present anywhere in the eShopWCFService or eShopWinForms projects, and no <system.diagnostics>/tracing configuration exists in Web.config. Security-relevant operations such as catalog service calls and stock/discount updates in CatalogService.svc.cs and CatalogController.cs execute without any audit trail, so failures or malicious inputs would go unrecorded.

### CWE-22: Improper Limitation of a Pathname to a Restricted Directory ('Path Traversal')
- **Category:** File & Path Security
- **Severity:** optional
- **Story Points:** 8
- **Files:** eShopModernizedNTier/src/eShopWinForms/Views/CatalogView.cs

In CatalogView.SetCatalogItems (line 57-58), the file path used to load a catalog item image is built by string-concatenating Environment.CurrentDirectory with a hard-coded relative segment and catalogItem.Picturefilename, a value retrieved from the WCF catalog service/database. This value is passed directly to Image.FromFile without any validation or sanitization, so a crafted Picturefilename value could cause the application to read a file outside of the intended Assets/Images/Catalog directory.

### CWE-23: Relative Path Traversal
- **Category:** File & Path Security
- **Severity:** optional
- **Story Points:** 5
- **Files:** eShopModernizedNTier/src/eShopWinForms/Views/CatalogView.cs

In CatalogView.SetCatalogItems (line 57-58), catalogItem.Picturefilename (external service data) is concatenated onto a base image directory path with no check for '..' sequences before being passed to Image.FromFile(imagename), allowing relative path traversal outside the Assets/Images/Catalog directory.
