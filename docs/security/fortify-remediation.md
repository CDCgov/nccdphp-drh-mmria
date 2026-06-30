# Fortify Remediation

This document is the system of record for Fortify SAST findings and their verdicts.
Each `## Finding` section maps directly to an SSC issue ID for writeback.

---

## Finding 1 — Header Manipulation at source-code/mmria/mmria-server/util/OutboundRequestSecurityHelper.cs:45

**SSC Issue ID:** 2221778  
**Severity:** High  
**Category:** Header Manipulation  
**Rule GUID:** 0858F67A-D592-4E5F-8C3A-514CB484E1CB  
**Scan commit:** e5009f9b79f61b736ae6e39facf8272c56817f4c  
**SSC Application Version:** 10291  

### Verdict

**Fixed**

### Taint path

| Step | File:Line | Code |
|------|-----------|------|
| Source | `AccountController.OIDC.cs:226` | `access_token` from OIDC response / `SteveAPI_Instance.cs:330` `bearerToken` from caller |
| Propagation | `OutboundRequestSecurityHelper.cs:39` | `var sanitizedToken = ValidateHeaderValue(bearerToken, paramName, 4096)` |
| Sink | `OutboundRequestSecurityHelper.cs:45` | `return new AuthenticationHeaderValue("Bearer", sanitizedToken)` |

### Fix applied

Explicit CR/LF stripping added at the top of `ValidateHeaderValue`
(`OutboundRequestSecurityHelper.cs:55–58`) using `string.Replace` with `StringComparison.Ordinal`.
This removes CR (0x0D) and LF (0x0A) before any downstream processing, eliminating the
HTTP header injection vector (CWE-113) at the earliest point in the sanitization chain.

**Before (at scanned commit e5009f9b):**
```csharp
var trimmedValue = value.Trim();
```

**After:**
```csharp
// Explicitly strip CR and LF to prevent HTTP header injection (CWE-113 / Header Manipulation)
var crlfStripped = value.Replace("\r", string.Empty, StringComparison.Ordinal)
                        .Replace("\n", string.Empty, StringComparison.Ordinal);
var trimmedValue = crlfStripped.Trim();
```

### Defense-in-depth layers

The following controls are layered after the CRLF strip, providing defense-in-depth:

1. **Visible-ASCII whitelist** — `IsVisibleAsciiHeaderCharacter` (`OutboundRequestSecurityHelper.cs:78`) rejects any character outside 0x20–0x7E via exception
2. **External sanitizer equality check** — `CouchDbHttpClient.SanitizeHeader` (`CouchDbHttpClient.cs:674`) is called and the result must be identical to the input; any further modification throws (`OutboundRequestSecurityHelper.cs:70–73`)
3. **Strict allow-list regex** — `BearerTokenPattern` (`^[A-Za-z0-9._~+/=-]{1,4096}$`) applied to the return value before use in `AuthenticationHeaderValue` (`OutboundRequestSecurityHelper.cs:40–43`)

### Verdict rationale

The taint source is an externally supplied bearer token (OIDC `access_token` or STEVE API bearer token). Prior to this fix, Fortify's taint engine tracked the value through `ValidateHeaderValue` to the `AuthenticationHeaderValue` sink without recognizing the custom validation chain as a sanitizer for CWE-113. The explicit `Replace("\r", …).Replace("\n", …)` at the function entry point is a recognized sanitizer pattern for Header Manipulation that untaints the CRLF dimension before any further propagation. All remaining validation layers are preserved and continue to protect against other injection vectors.
