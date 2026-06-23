# Fortify remediation

## Finding 1 — Header Manipulation at source-code/mmria/mmria-server/util/OutboundRequestSecurityHelper.cs:45
**SSC Issue ID:** 2221778  
**Scan:** mmria s2i @ 99433885  
**Verdict:** Fixed

### Taint path
- **Source:** `bearerToken` enters `CreateBearerAuthenticationHeaderValue(...)` in `source-code/mmria/mmria-server/util/OutboundRequestSecurityHelper.cs:32`.
- **Propagation:** the value passes through `ValidateHeaderValue(...)` and the token-character regex gate in `source-code/mmria/mmria-server/util/OutboundRequestSecurityHelper.cs:39-43`.
- **Sink:** authorization header creation in `source-code/mmria/mmria-server/util/OutboundRequestSecurityHelper.cs:45`.

### Remediation
The sink now uses `AuthenticationHeaderValue.TryParse("Bearer " + sanitizedToken, out var headerValue)` and verifies scheme/parameter round-trip equality before returning the header value. If parsing or round-trip checks fail, the method throws and no header is emitted.
