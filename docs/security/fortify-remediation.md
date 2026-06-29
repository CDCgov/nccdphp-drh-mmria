# Fortify Remediation Record

This file is the system of record for Fortify SAST findings and their dispositions.
It is machine-parsed for SSC writeback; do not alter heading levels or verdict strings.

---

## Finding 1 — Header Manipulation at source-code/mmria/mmria-server/util/OutboundRequestSecurityHelper.cs:45

**Scan commit:** `e5009f9b79f61b736ae6e39facf8272c56817f4c`
**SSC application version:** 10291
**SSC Issue ID:** 2221778
**Severity:** High
**Rule GUID:** `0858F67A-D592-4E5F-8C3A-514CB484E1CB`
**CWE:** CWE-113 (Improper Neutralization of CRLF Sequences in HTTP Headers)

### Verdict

**Fixed**

### Taint path

| Step | File:Line | Code |
|------|-----------|------|
| Source | `mmria-server/util/OutboundRequestSecurityHelper.cs:32` | `string bearerToken` parameter of `CreateBearerAuthenticationHeaderValue` |
| Propagation | `mmria-server/util/OutboundRequestSecurityHelper.cs:39` | `var sanitizedToken = ValidateHeaderValue(bearerToken, paramName, 4096);` |
| Propagation | `mmria-server/util/OutboundRequestSecurityHelper.cs:40–43` | `if (!BearerTokenPattern.IsMatch(sanitizedToken)) throw ...` — allow-list regex `^[A-Za-z0-9._~+/=-]{1,4096}$` |
| Sink | `mmria-server/util/OutboundRequestSecurityHelper.cs:45` | `return new AuthenticationHeaderValue("Bearer", sanitizedToken);` |

### Sanitizer evidence

The taint is neutralized before the sink via two layers of defence in `ValidateHeaderValue`
(`OutboundRequestSecurityHelper.cs:48–79`):

1. **Explicit CRLF/null guard (added in this fix, line 56–59)**  
   ```csharp
   if (value.IndexOfAny(new[] { '\r', '\n', '\0' }) >= 0)
   {
       throw new ArgumentException("Header value must not contain CR, LF, or null characters.", paramName);
   }
   ```  
   Any value containing `\r` (CR, 0x0D), `\n` (LF, 0x0A), or `\0` (null, 0x00) causes an
   immediate `ArgumentException` before the value reaches `AuthenticationHeaderValue`. This is the
   primary guard that Fortify's taint analysis recognises as neutralising CWE-113.

2. **Visible-ASCII allow-list (line 67–70, pre-existing)**  
   ```csharp
   if (trimmedValue.Any(character => !IsVisibleAsciiHeaderCharacter(character)))
   {
       throw new ArgumentException("Header value contains unsupported characters.", paramName);
   }
   ```  
   `IsVisibleAsciiHeaderCharacter` returns `true` only for characters in the range 0x20–0x7E,
   which excludes all control characters including CR (0x0D) and LF (0x0A).

3. **`CouchDbHttpClient.SanitizeHeader` comparison (line 72–76, pre-existing)**  
   The value is compared byte-for-byte with the output of `SanitizeHeader`, which strips every
   character outside `{tab, 0x20–0x7E}`. Any mismatch throws `ArgumentException`.

4. **Bearer-token allow-list regex (line 40–43, pre-existing)**  
   After `ValidateHeaderValue` returns, `BearerTokenPattern` (`^[A-Za-z0-9._~+/=-]{1,4096}$`)
   is applied. This further restricts the value to alphanumeric characters and a small set of
   safe special characters, making CRLF injection structurally impossible.

### Verdict rationale

The vulnerability is **Fixed**. The newly added explicit `IndexOfAny('\r', '\n', '\0')` check at
`ValidateHeaderValue` line 56–59 creates a recognisable early-exit guard that directly neutralises
the CWE-113 taint path. Combined with the pre-existing visible-ASCII allow-list, `SanitizeHeader`
comparison, and bearer-token regex, no value containing CR/LF characters can reach the
`AuthenticationHeaderValue` constructor at line 45. The finding was a true positive with respect
to taint-flow analysis (custom sanitizers were not recognised by Fortify), now resolved by the
explicit guard.
