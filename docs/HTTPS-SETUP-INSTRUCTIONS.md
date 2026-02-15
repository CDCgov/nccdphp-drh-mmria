# HTTPS Setup for Local Multi-Tenant Development

This guide enables HTTPS for local development to support Service Workers with custom tenant domains.

## Prerequisites

- Windows 10/11
- PowerShell (Administrator access required)
- ASP.NET Core application

## Step 1: Generate Self-Signed Certificate

1. **Open PowerShell as Administrator**
   - Right-click on PowerShell icon
   - Select "Run as Administrator"

2. **Run the certificate generation script:**

```powershell
cd C:\repos\nccdphp-drh-mmria

$certPassword = "mmria-dev-2026"
$certPath = "C:\repos\nccdphp-drh-mmria\mmria-dev-cert.pfx"
$dnsNames = @("tenant1-mmria.local","tenant2-mmria.local","tenant3-mmria.local","tenant4-mmria.local","tenant5-mmria.local","localhost")

# Create the certificate
$cert = New-SelfSignedCertificate -Subject "CN=MMRIA Local Development" -DnsName $dnsNames -KeyAlgorithm RSA -KeyLength 2048 -NotBefore (Get-Date) -NotAfter (Get-Date).AddYears(5) -CertStoreLocation "Cert:\CurrentUser\My" -FriendlyName "MMRIA Local Development Certificate" -HashAlgorithm SHA256 -KeyUsage DigitalSignature,KeyEncipherment,DataEncipherment -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.1")

# Export to PFX file
$certPasswordSecure = ConvertTo-SecureString -String $certPassword -Force -AsPlainText
Export-PfxCertificate -Cert $cert -FilePath $certPath -Password $certPasswordSecure

# Trust the certificate (add to LocalMachine Trusted Root)
$store = New-Object System.Security.Cryptography.X509Certificates.X509Store "Root","LocalMachine"
$store.Open("ReadWrite")
$store.Add($cert)
$store.Close()

Write-Host "Certificate created at: $certPath" -ForegroundColor Green
Write-Host "Password: $certPassword" -ForegroundColor Yellow
```

3. **Verify the certificate was created:**
   - Check that `C:\repos\nccdphp-drh-mmria\mmria-dev-cert.pfx` exists
   - Certificate is valid for 5 years

## Step 2: Update appsettings.local.json

Add the Kestrel HTTPS configuration to your `appsettings.local.json`:

```json
{
  "mmria_settings": {
    "web_site_url": "https://*:12345",
    // ... other settings ...
  },
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://*:12344"
      },
      "Https": {
        "Url": "https://*:12345",
        "Certificate": {
          "Path": "C:\\repos\\nccdphp-drh-mmria\\mmria-dev-cert.pfx",
          "Password": "mmria-dev-2026"
        }
      }
    }
  }
}
```

**Important Notes:**
- Use double backslashes (`\\`) in the path for JSON
- HTTP port (12344) is optional but useful for fallback
- HTTPS port (12345) is the main port for service workers

## Step 3: Configure Hosts File

1. **Open Notepad as Administrator**
   - Right-click Notepad
   - Select "Run as Administrator"

2. **Open the hosts file:**
   - File → Open
   - Navigate to: `C:\Windows\System32\drivers\etc`
   - Change file filter to "All Files (*.*)"
   - Open `hosts`

3. **Add tenant domain entries:**

```
127.0.0.1 tenant1-mmria.local
127.0.0.1 tenant2-mmria.local
127.0.0.1 tenant3-mmria.local
127.0.0.1 tenant4-mmria.local
127.0.0.1 tenant5-mmria.local
```

4. **Save and close**

## Step 4: Restart and Test

1. **Restart your browser** (important for certificate trust to take effect)

2. **Start the MMRIA application**

3. **Access your tenants via HTTPS:**
   - `https://tenant1-mmria.local:12345`
   - `https://tenant2-mmria.local:12345`
   - `https://tenant3-mmria.local:12345`
   - `https://tenant4-mmria.local:12345`
   - `https://tenant5-mmria.local:12345`
   - `https://localhost:12345`

4. **Verify no security warnings:**
   - Browser should show secure padlock icon
   - No "Not secure" warning
   - Service Workers should be available

## Troubleshooting

### "Not secure" warning still appears

1. Verify certificate is in Trusted Root:
   ```powershell
   Get-ChildItem -Path Cert:\LocalMachine\Root | Where-Object { $_.Subject -like "*MMRIA*" }
   ```

2. If missing, re-run Step 1 in Administrator PowerShell

3. **Clear browser cache and restart browser**

### File not found error on startup

- Verify certificate path in `appsettings.local.json` is correct
- Use absolute path: `C:\\repos\\nccdphp-drh-mmria\\mmria-dev-cert.pfx`
- Verify file exists at that location

### Service Workers not available

- Ensure accessing via HTTPS (not HTTP)
- Check browser console: `'serviceWorker' in navigator` should be `true`
- Verify domain is in certificate DNS names

### Port already in use

- Change port numbers in `appsettings.local.json`
- Update any hardcoded references to the old port
- Common alternative ports: 5001, 44331, 12346

## Certificate Information

- **Valid for:** 5 years from creation date
- **Algorithm:** RSA 2048-bit
- **Hash:** SHA256
- **Domains:** All tenant subdomains + localhost
- **Location:** `C:\repos\nccdphp-drh-mmria\mmria-dev-cert.pfx`
- **Password:** `mmria-dev-2026` (change if deploying beyond local dev)

## Renewing the Certificate

When the certificate expires (5 years), simply re-run Step 1 to generate a new certificate.

## Security Notes

⚠️ **For Development Only**
- This is a self-signed certificate for local development
- Never use this certificate in production
- Password is in plain text for convenience - acceptable for local dev only
- Certificate is added to LocalMachine Trusted Root - your machine will trust it

## Adding More Tenants

To add additional tenant domains:

1. Update the `$dnsNames` array in Step 1:
   ```powershell
   $dnsNames = @("tenant1-mmria.local","tenant2-mmria.local","tenant6-mmria.local","localhost")
   ```

2. Re-run Step 1 to regenerate the certificate

3. Add new entries to hosts file in Step 3

4. Restart browser

## Related Documentation

- [Service Worker Requirements](https://developer.mozilla.org/en-US/docs/Web/API/Service_Worker_API#security)
- [ASP.NET Core Kestrel HTTPS](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/endpoints)
- [Self-Signed Certificates](https://learn.microsoft.com/en-us/dotnet/core/additional-tools/self-signed-certificates-guide)
