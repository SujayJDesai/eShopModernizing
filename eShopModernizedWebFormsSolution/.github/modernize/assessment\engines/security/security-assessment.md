# Security Assessment Report

**Generated:** 2026-08-17T09:15:42.7032720Z

## Summary

| Metric | Count |
|--------|-------|
| Total Findings | 7 |
| CVE Vulnerabilities | 0 |
| CWE Vulnerabilities | 7 |
| Total Rules Assessed | 59 |
| Rules Passed | 52 |

### By Severity

| Severity | Count |
|----------|-------|
| mandatory | 1 |
| optional | 3 |
| potential | 3 |

## CVE Findings (Dependency Vulnerabilities)

No CVE findings met the minimum severity threshold.


## CWE Findings (Code-Level Vulnerabilities)

### CWE-775: Missing Release of File Descriptor or Handle after Effective Lifetime
- **Category:** Code Quality
- **Severity:** potential
- **Story Points:** 3
- **Files:** src/eShopModernizedWebForms/Services/ImageAzureStorage.cs:109

ImageAzureStorage.UpLoadImageFromFile opens a FileStream with File.OpenRead at line 109 and uploads it to blob storage, but the stream is not enclosed in a using statement or otherwise disposed after use.

### CWE-567: Unsynchronized Access to Shared Data in a Multithreaded Context
- **Category:** Concurrency & Synchronization
- **Severity:** potential
- **Story Points:** 5
- **Files:** src/eShopModernizedWebForms/Global.asax.cs:22, src/eShopModernizedWebForms/Global.asax.cs:79

Global defines a mutable static IContainerProvider field at line 22 and assigns it during Application_Start at line 79 without synchronization or volatile publication. ASP.NET requests can read ContainerProvider concurrently through the static field.

### CWE-259: Use of Hard-coded Password
- **Category:** Credentials & Secrets
- **Severity:** optional
- **Story Points:** 5
- **Files:** docker-compose.override.yml:21, src/eShopModernizedWebForms/Web.config:20

The Docker Compose override and Web.config include database connection settings with a hard-coded SQL administrator password for outbound communication to the SQL Server container/database.

### CWE-732: Incorrect Permission Assignment for Critical Resource
- **Category:** Credentials & Secrets
- **Severity:** optional
- **Story Points:** 5
- **Files:** src/eShopModernizedWebForms/Services/ImageAzureStorage.cs:47

ImageAzureStorage.InitializeCatalogImages sets the Azure blob container permissions to BlobContainerPublicAccessType.Blob at line 47, making uploaded catalog image blobs publicly readable.

### CWE-798: Use of Hard-coded Credentials
- **Category:** Credentials & Secrets
- **Severity:** optional
- **Story Points:** 5
- **Files:** docker-compose.override.yml:21, src/eShopModernizedWebForms/Web.config:20

The application configuration contains hard-coded SQL credentials in the Docker Compose environment and default Web.config connection string.

### CWE-434: Unrestricted Upload of File with Dangerous Type
- **Category:** File & Path Security
- **Severity:** mandatory
- **Story Points:** 8
- **Files:** src/eShopModernizedWebForms/Catalog/PicUploader.asmx.cs:40, src/eShopModernizedWebForms/Services/ImageAzureStorage.cs:93

PicUploader.UploadImage reads an uploaded HttpPostedFile from Request.Files at line 40 and ImageAzureStorage.UploadTempImage stores it using the client-controlled file.FileName and ContentType at lines 93-97. The code validates image decoding but does not enforce a safe file extension or content-type allowlist before writing the public blob.

### CWE-99: Improper Control of Resource Identifiers ('Resource Injection')
- **Category:** Injection Attacks
- **Severity:** potential
- **Story Points:** 3
- **Files:** src/eShopModernizedWebForms/Catalog/PicUploader.asmx.cs:40, src/eShopModernizedWebForms/Services/ImageAzureStorage.cs:93

PicUploader.UploadImage accepts an uploaded file from Request.Files at line 40. ImageAzureStorage.UploadTempImage then uses the external file.FileName value directly in the Azure blob name at line 93, allowing caller-controlled input to select the blob resource identifier under the temp/catalog prefix.
