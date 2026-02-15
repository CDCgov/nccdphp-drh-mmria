# Generate self-signed certificate for local multi-tenant MMRIA development
# Run this script as Administrator in PowerShell

# Certificate parameters
$certPassword = "mmria-dev-2026"  # Change this password as needed
$certPath = "$PSScriptRoot\mmria-dev-cert.pfx"
$certName = "MMRIA Local Development Certificate"

# All tenant domains that need to be covered
$dnsNames = @(
    "tenant1-mmria.local",
    "tenant2-mmria.local",
    "tenant3-mmria.local",
    "tenant4-mmria.local",
    "tenant5-mmria.local",
    "localhost"  # Include localhost as well
)

Write-Host "=== MMRIA Development Certificate Generator ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Creating certificate for:" -ForegroundColor Yellow
$dnsNames | ForEach-Object { Write-Host "  - https://$_" }
Write-Host ""

# Create the certificate with all DNS names
try {
    $cert = New-SelfSignedCertificate `
        -Subject "CN=MMRIA Local Development" `
        -DnsName $dnsNames `
        -KeyAlgorithm RSA `
        -KeyLength 2048 `
        -NotBefore (Get-Date) `
        -NotAfter (Get-Date).AddYears(5) `
        -CertStoreLocation "Cert:\CurrentUser\My" `
        -FriendlyName $certName `
        -HashAlgorithm SHA256 `
        -KeyUsage DigitalSignature, KeyEncipherment, DataEncipherment `
        -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.1") # Server Authentication

    Write-Host "✓ Certificate created successfully" -ForegroundColor Green
    Write-Host "  Thumbprint: $($cert.Thumbprint)" -ForegroundColor Gray
    Write-Host ""

    # Export certificate to PFX file
    $certPasswordSecure = ConvertTo-SecureString -String $certPassword -Force -AsPlainText
    Export-PfxCertificate -Cert $cert -FilePath $certPath -Password $certPasswordSecure | Out-Null
    
    Write-Host "✓ Certificate exported to: $certPath" -ForegroundColor Green
    Write-Host "  Password: $certPassword" -ForegroundColor Yellow
    Write-Host ""

    # Trust the certificate (add to Trusted Root)
    $store = New-Object System.Security.Cryptography.X509Certificates.X509Store "Root", "CurrentUser"
    $store.Open("ReadWrite")
    $store.Add($cert)
    $store.Close()

    Write-Host "✓ Certificate added to Trusted Root Certificate Authorities" -ForegroundColor Green
    Write-Host ""

    # Display configuration instructions
    Write-Host "=== Next Steps ===" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "1. Add to appsettings.local.json (or appsettings.Development.json):" -ForegroundColor Yellow
    Write-Host ""
    Write-Host @"
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://*:5000"
      },
      "Https": {
        "Url": "https://*:5001",
        "Certificate": {
          "Path": "$certPath",
          "Password": "$certPassword"
        }
      }
    }
  }
}
"@ -ForegroundColor White
    Write-Host ""
    
    Write-Host "2. Update your hosts file (C:\Windows\System32\drivers\etc\hosts):" -ForegroundColor Yellow
    Write-Host ""
    Write-Host @"
127.0.0.1 tenant1-mmria.local
127.0.0.1 tenant2-mmria.local
127.0.0.1 tenant3-mmria.local
127.0.0.1 tenant4-mmria.local
127.0.0.1 tenant5-mmria.local
"@ -ForegroundColor White
    Write-Host ""

    Write-Host "3. Access your sites using HTTPS:" -ForegroundColor Yellow
    $dnsNames | Where-Object { $_ -ne "localhost" } | ForEach-Object { 
        Write-Host "   https://$_`:5001" -ForegroundColor White
    }
    Write-Host ""

    Write-Host "=== Certificate Details ===" -ForegroundColor Cyan
    Write-Host "  Subject: $($cert.Subject)" -ForegroundColor Gray
    Write-Host "  Issuer: $($cert.Issuer)" -ForegroundColor Gray
    Write-Host "  Valid From: $($cert.NotBefore)" -ForegroundColor Gray
    Write-Host "  Valid To: $($cert.NotAfter)" -ForegroundColor Gray
    Write-Host "  Thumbprint: $($cert.Thumbprint)" -ForegroundColor Gray
    Write-Host ""

    Write-Host "✓ Setup complete! Service workers will now work on all tenant domains." -ForegroundColor Green
    
} catch {
    Write-Host "✗ Error creating certificate: $_" -ForegroundColor Red
    exit 1
}
