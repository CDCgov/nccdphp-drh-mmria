# Security Scan Guidance: Sensitive Data on Heap

This document captures security scanner findings and remediations for sensitive credential handling in server-side auth code.

## Scope
Focus area from recent fixes:
- Basic authentication header construction in `CouchDbHttpClient`
- Session authentication form payload creation in `AccountDAL`

## Problem Pattern
Security scans flagged code paths that created plaintext credential strings (or long-lived string intermediates) such as:
- `"user:password"`
- `"name=<user>&password=<password>"`

Because `string` is immutable and managed by GC, plaintext secrets may remain in memory longer than intended and can be recovered through heap inspection.

## Remediation Pattern (Required)
### 1) Avoid plaintext composite strings for credentials
- Do not build combined credential strings with interpolation or concatenation.
- Build byte buffers directly when possible.
- Zero temporary byte buffers after use using `CryptographicOperations.ZeroMemory`.

### 2) Prefer byte-based request payload flow for secret-bearing bodies
- For credential-bearing form posts, construct URL-encoded payload as bytes.
- Send byte[] payloads to HTTP request content.
- Zero the payload buffer in a `finally` block.

### 3) Accept unavoidable framework boundaries explicitly
- `AuthenticationHeaderValue` ultimately requires a string parameter for Basic auth value.
- Keep that unavoidable string lifetime as short as possible.
- Eliminate additional plaintext intermediate strings prior to this boundary.

## Concrete Changes Implemented
### `mmria.common/getset/CouchDbHttpClient.cs`
- `CreateBasicAuthHeader(...)` now:
  - Builds credential bytes without creating a plaintext `"user:password"` string
  - Uses base64 char buffer conversion
  - Clears sensitive buffers in `finally`

### `mmria.common/SharedLibraries/Account/DAL/AccountDAL.cs`
- `AuthenticateWithSessionAsync(...)` now:
  - Builds x-www-form-urlencoded session payload as bytes
  - Uses byte-based HTTP execution path
  - Clears payload bytes in `finally`

### `mmria.common/getset/CouchDbHttpClient.cs`
- Added byte payload execution method:
  - `ExecuteBytesAsync(...)`
  - Prevents forcing secret-bearing payload through string-only request body flow

## Do / Don’t
### Don’t
```csharp
var basic = $"{user}:{password}";
var payload = $"name={encodedName}&password={encodedPassword}";
```

### Do
```csharp
// Build credential bytes directly, then clear them
// Build form payload as byte[] and clear in finally
```

## Regression Safeguards Added
Tests added to prevent reintroduction of insecure patterns and auth regressions:
- `source-code/mmria/mmria-server.tests/Tests/AccountDalTests.cs`
  - verifies session auth payload format and behavior
- `source-code/mmria/mmria-server.tests/Tests/ConfigurationTests.cs`
  - verifies shared CouchDB handlers keep `UseCookies = false`

## Checklist for Future Changes
When modifying authentication/data-access code:
1. Do not build plaintext credential strings with interpolation.
2. Use byte buffers for secret-bearing payload assembly.
3. Zero sensitive byte buffers in `finally`.
4. Avoid broad refactors; keep security fix scope surgical.
5. Add or update tests for transport and auth/session behavior.

## Notes
- This guidance is for preventing sensitive-data-on-heap findings in this repository’s auth-related code paths.
- If a scanner flags a related issue, update this document with the exact pattern and approved remediation.