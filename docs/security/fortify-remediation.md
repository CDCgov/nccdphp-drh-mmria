# Fortify Remediation Log

This file is the system of record for Fortify SAST findings and their disposition.
It is parsed by a batch job for SSC writeback. Shape must remain exact.

---

## Finding 1 — Header Manipulation at source-code/mmria/mmria-server/util/OutboundRequestSecurityHelper.cs:45

**Scan commit:** `3141fa8685885293b49e6719df4924d8378f7dfd`
**SSC Issue ID:** 2221778
**Severity:** High
**Verdict:** Fixed

### Taint path

| Step | File:Line | Code |
|---|---|---|
| Source | `source-code/mmria/mmria-server/Controllers/AccountController.OIDC.cs:226` | `access_token` received from OIDC token endpoint response |
| Propagation | `source-code/mmria/mmria-server/util/OutboundRequestSecurityHelper.cs:39` | `var sanitizedToken = ValidateHeaderValue(bearerToken, paramName, 4096);` |
| Sink | `source-code/mmria/mmria-server/util/OutboundRequestSecurityHelper.cs:49` | `return new AuthenticationHeaderValue("Bearer", safeToken);` |

### Fix applied

File: `source-code/mmria/mmria-server/util/OutboundRequestSecurityHelper.cs`, lines 45–49.

**Before (line 45):**
```csharp
return new AuthenticationHeaderValue("Bearer", sanitizedToken);
```

**After (lines 45–49):**
```csharp
// Explicitly strip CR and LF to prevent HTTP header injection (defense in depth).
var safeToken = sanitizedToken
    .Replace("\r", string.Empty, StringComparison.Ordinal)
    .Replace("\n", string.Empty, StringComparison.Ordinal);
return new AuthenticationHeaderValue("Bearer", safeToken);
```

The existing `ValidateHeaderValue` call (line 39) already rejects input containing control characters, and `BearerTokenPattern` (`^[A-Za-z0-9._~+/=-]{1,4096}$`) enforces an allow-list that excludes CR/LF. The explicit `.Replace` calls above are a defense-in-depth measure that makes CRLF stripping visible directly at the sink, so Fortify's dataflow analysis can recognize the sanitization.

### Evidence

1. **Source:** `access_token` originates from an OIDC token exchange response and is passed directly to `CreateBearerAuthenticationHeaderValue` (`AccountController.OIDC.cs:226`).
2. **Sanitization chain at `ValidateHeaderValue` (`OutboundRequestSecurityHelper.cs:52–77`):**
   - Line 65: `trimmedValue.Any(character => !IsVisibleAsciiHeaderCharacter(character))` — rejects any char outside `0x20–0x7E`, which includes CR (`0x0D`) and LF (`0x0A`).
   - Line 70: `CouchDbHttpClient.SanitizeHeader(trimmedValue)` — strips control characters and compares result to input; throws if they differ.
3. **Regex allow-list at line 40:** `BearerTokenPattern` = `^[A-Za-z0-9._~+/=-]{1,4096}$` — only permits alphanumeric and a small set of URL-safe characters; CR/LF cannot match.
4. **Explicit CRLF strip at sink (lines 46–48):** `sanitizedToken.Replace("\r", …).Replace("\n", …)` — defense-in-depth that makes the sanitization visible to Fortify at the exact point of header construction.
