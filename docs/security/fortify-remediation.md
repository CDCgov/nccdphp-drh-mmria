# Fortify Remediation Log

This file is the system of record for Fortify SAST findings triage and remediation in this repository.
It is machine-parsed for SSC writeback; preserve the heading and field formats exactly.

---

## Finding 1 — Header Manipulation at source-code/mmria/mmria-server/util/OutboundRequestSecurityHelper.cs:45

**SSC Issue ID:** 2221778
**Severity:** High
**Category:** Header Manipulation
**Rule GUID:** 0858F67A-D592-4E5F-8C3A-514CB484E1CB
**File:** `source-code/mmria/mmria-server/util/OutboundRequestSecurityHelper.cs`
**Line:** 45
**Scan commit:** `6e95d0f993c193ecf45458c765b0e831416f2a82`

**Verdict:** Not applicable / false positive

### Taint path

The Fortify scanner traces user-controlled input (`bearerToken` parameter) to the
`AuthenticationHeaderValue` constructor at line 45. The full path is:

1. **Source** — `bearerToken` parameter, `CreateBearerAuthenticationHeaderValue`, line 32:
   ```csharp
   public static AuthenticationHeaderValue CreateBearerAuthenticationHeaderValue(string bearerToken, string paramName = "bearerToken")
   ```
2. **Null guard** — lines 34–37: throws `ArgumentException` if `bearerToken` is null or whitespace.
3. **`ValidateHeaderValue` call** — line 39: `sanitizedToken = ValidateHeaderValue(bearerToken, paramName, 4096)`.
   Inside `ValidateHeaderValue` (lines 48–73):
   - **Visible-ASCII guard** (lines 61–64): `IsVisibleAsciiHeaderCharacter` (line 75–76) accepts only
     characters in the range `[0x20, 0x7E]`. CR (`0x0D`) and LF (`0x0A`) are both below `0x20` and
     cause an `ArgumentException` to be thrown immediately. No CRLF character can survive this guard.
   - **`SanitizeHeader` call** (line 66): provides a secondary filter; the result must exactly equal
     the input (line 67–70), so any character that `SanitizeHeader` would strip also causes rejection.
4. **Bearer-token regex** — lines 40–43: `BearerTokenPattern` = `^[A-Za-z0-9._~+/=-]{1,4096}$`.
   This allow-list permits only alphanumeric characters and the symbols `._~+/=-`; CR and LF are
   absent from the allow-list and would cause an `ArgumentException`.
5. **Sink** — line 45:
   ```csharp
   return new AuthenticationHeaderValue("Bearer", sanitizedToken);
   ```
   `sanitizedToken` reaches this line only after both guards above have been satisfied. No CRLF
   or other control character can be present at this point.

### Evidence

- `IsVisibleAsciiHeaderCharacter` (line 75–76) explicitly rejects every character outside
  `[0x20, 0x7E]`, which covers CR (`0x0D` = 13) and LF (`0x0A` = 10). Any bearer token containing
  these characters throws before reaching line 45.
- `BearerTokenPattern` (`^[A-Za-z0-9._~+/=-]{1,4096}$`) enforces a strict token allow-list.
  Neither CR nor LF is in `[A-Za-z0-9._~+/=-]`; a token containing them fails `IsMatch` and
  throws before reaching line 45.
- Both guards are applied sequentially in `CreateBearerAuthenticationHeaderValue` between the
  source (parameter input) and the sink (`AuthenticationHeaderValue` constructor). There is no
  alternate code path that bypasses them.
- The Fortify rule fires because its inter-procedural taint analysis does not model
  `ValidateHeaderValue` or the regex check as registered sanitizers for the Header Manipulation
  rule (CWE-113). The taint technically flows through the call, but the sanitizers make
  exploitation impossible: every CRLF injection attempt terminates with an `ArgumentException`
  before the header is set.

### Verdict rationale

CWE-113 (Header Injection) requires that CRLF characters reach the HTTP header value. The
full taint path shows that both the `IsVisibleAsciiHeaderCharacter` guard and the
`BearerTokenPattern` regex independently and completely prevent CRLF characters from reaching
the sink at line 45. This finding is a false positive produced by Fortify's inability to
model the domain-specific sanitizers as safe for this rule category. No code change is needed.
