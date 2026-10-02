param(
    [Parameter(Mandatory = $true)][string]$Destination,
    [ValidateSet('x64', 'arm64')][string]$Architecture = 'x64'
)
$ErrorActionPreference = 'Stop'
$destinationDirectory = [IO.Path]::GetFullPath($Destination)
$sniDirectory = Join-Path $destinationDirectory 'bin/SniSpoofing'
$xrayDirectory = Join-Path $destinationDirectory 'bin/Serverless'
New-Item -ItemType Directory -Path $sniDirectory, $xrayDirectory -Force | Out-Null
$sniName = if ($Architecture -eq 'x64') { 'sni-spoofing-windows-amd64.exe' } else { 'sni-spoofing-windows-arm64.exe' }
$sniHash = if ($Architecture -eq 'x64') { 'fc878e85aaab88362f41f66833ab85b0a9df9b399ecca1f34a75625c9dcfc8c4' } else { '80a03bb6c53d13ed0320827a1aab6fbd6c40f77c824bcefd366274aa0797cd7a' }
$xrayName = if ($Architecture -eq 'x64') { 'Xray-windows-64.zip' } else { 'Xray-windows-arm64-v8a.zip' }
$xrayHash = if ($Architecture -eq 'x64') { 'b17a619343c11b89d8faf36278298856749b15c52277d875d32051b33dcda617' } else { '5cf7ad4fd9aac5aa248ae7e598f3f0a0d0f79a6195089d611ddef9653830f1b0' }
$downloadDirectory = Join-Path ([IO.Path]::GetTempPath()) ('v2rayN-dpi-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $downloadDirectory | Out-Null
function Download-Verified([string]$Url, [string]$Output, [string]$Hash) {
    Invoke-WebRequest -Uri $Url -OutFile $Output
    if ((Get-FileHash -LiteralPath $Output -Algorithm SHA256).Hash.ToLowerInvariant() -ne $Hash) {
        throw "SHA-256 mismatch for $Url"
    }
}
$sniDownload = Join-Path $downloadDirectory $sniName
Download-Verified "https://github.com/aleskxyz/SNI-Spoofing-Go/releases/download/v0.7.2/$sniName" $sniDownload $sniHash
Copy-Item -LiteralPath $sniDownload -Destination (Join-Path $sniDirectory $sniName) -Force
Invoke-WebRequest 'https://raw.githubusercontent.com/aleskxyz/SNI-Spoofing-Go/v0.7.2/LICENSE' -OutFile (Join-Path $sniDirectory 'LICENSE')
$xrayDownload = Join-Path $downloadDirectory $xrayName
Download-Verified "https://github.com/XTLS/Xray-core/releases/download/v26.9.30/$xrayName" $xrayDownload $xrayHash
Expand-Archive -LiteralPath $xrayDownload -DestinationPath $xrayDirectory -Force
Write-Host "Installed verified SNI and Serverless/Cloudflare runtimes in $destinationDirectory"
