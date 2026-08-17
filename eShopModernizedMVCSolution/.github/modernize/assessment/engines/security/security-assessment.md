# Security Assessment Report

**Generated:** 2026-08-17T09:09:05.0000000Z

## Summary

| Metric | Count |
|--------|-------|
| Total Findings | 4 |
| CVE Vulnerabilities | 0 |
| CWE Vulnerabilities | 4 |
| Total Rules Assessed | 59 |
| Rules Passed | 55 |

### By Severity

| Severity | Count |
|----------|-------|
| mandatory | 1 |
| optional | 1 |
| potential | 2 |

## CVE Findings (Dependency Vulnerabilities)

_No CVE findings met the minimum severity threshold (`high`)._

## CWE Findings (Code-Level Vulnerabilities)

### CWE-775: Missing Release of File Descriptor or Handle after Effective Lifetime
- **Category:** Code Quality
- **Severity:** potential
- **Story Points:** 3
- **Files:** src/eShopModernizedMVC/Services/ImageAzureStorage.cs

`UpLoadImageFromFile` (Services/ImageAzureStorage.cs, line 111) opens a file handle with `File.OpenRead(filePath)` but never disposes or closes the returned `FileStream` (no `using` statement or explicit `Dispose()`/`Close()` call), leaking the file handle after the upload completes.

### CWE-567: Unsynchronized Access to Shared Data in a Multithreaded Context
- **Category:** Concurrency & Synchronization
- **Severity:** potential
- **Story Points:** 5
- **Files:** src/eShopModernizedMVC/Services/CatalogServiceMock.cs, src/eShopModernizedMVC/Modules/ApplicationModule.cs

`CatalogServiceMock` is registered with Autofac as `SingleInstance()` (Modules/ApplicationModule.cs), meaning a single shared instance handles concurrent web requests. Its `catalogItems` list field is mutated by `CreateCatalogItem` and `UpdateCatalogItem` (Services/CatalogServiceMock.cs) with no locking or thread-safe collection, so concurrent requests can race on `Add`/indexer writes to the shared list.

### CWE-23: Relative Path Traversal
- **Category:** File & Path Security
- **Severity:** optional
- **Story Points:** 5
- **Files:** src/eShopModernizedMVC/Services/ImageAzureStorage.cs

In `ImageAzureStorage.UploadTempImage` (Services/ImageAzureStorage.cs), the user-supplied `file.FileName` from the uploaded `HttpPostedFile` is concatenated directly into the Azure Blob storage path (`path + file.FileName.ToLower()`) without stripping path separators or relative path sequences (e.g. `../`). A crafted file name could cause the resulting blob path to resolve outside the intended `temp/`/`{catalogItemId}/` prefix within the container.

### CWE-89: Improper Neutralization of Special Elements used in an SQL Command ('SQL Injection')
- **Category:** Injection Attacks
- **Severity:** mandatory
- **Story Points:** 13
- **Files:** src/eShopModernizedMVC/Models/Infrastructure/CatalogDBInitializer.cs

`GetSequenceIdFromSelectedDBSequence` (Models/Infrastructure/CatalogDBInitializer.cs, line 326) builds a raw SQL statement via string interpolation — `context.Database.SqlQuery<Int64>($"SELECT NEXT VALUE FOR {dBSequenceName}")` — and executes it directly instead of using a parameterized query, which is an unsafe SQL construction pattern even though the value currently originates from internal callers.

