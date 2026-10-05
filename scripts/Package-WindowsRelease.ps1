param(
    [Parameter(Mandatory = $true)][string]$PublishDirectory,
    [Parameter(Mandatory = $true)][string]$CoreDirectory,
    [Parameter(Mandatory = $true)][string]$OutputDirectory
)
$ErrorActionPreference = 'Stop'
$output = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $output) { throw 'OutputDirectory must be a new directory.' }
New-Item -ItemType Directory -Path $output | Out-Null
# Only published executables/native libraries and known core assets are distributed.
# Never copy application settings, databases, generated configs, caches or logs.
Get-ChildItem -LiteralPath $PublishDirectory -File | Where-Object {
    $_.Extension -in @('.exe', '.dll')
} | Copy-Item -Destination $output
foreach ($name in @('Xray', 'sing_box', 'Psiphon', 'SniSpoofing', 'Serverless', 'srss')) {
    $source = Join-Path $CoreDirectory $name
    $target = Join-Path $output "bin/$name"
    New-Item -ItemType Directory -Path $target -Force | Out-Null
    Get-ChildItem -LiteralPath $source -File | Where-Object {
        $_.Extension -in @('.exe', '.dll', '.dat', '.srs') -or
        $_.Name -like 'LICENSE*' -or $_.Name -like 'README*'
    } | Copy-Item -Destination $target
}
Get-ChildItem -LiteralPath $CoreDirectory -File -Filter '*.dat' | Copy-Item -Destination (Join-Path $output 'bin')
foreach ($required in @('v2rayN.exe', 'bin/Xray/xray.exe', 'bin/sing_box/sing-box.exe',
    'bin/Psiphon/psiphon-tunnel-core.exe', 'bin/SniSpoofing/sni-spoofing-windows-amd64.exe',
    'bin/Serverless/xray.exe')) {
    if (-not (Test-Path -LiteralPath (Join-Path $output $required))) { throw "Missing runtime: $required" }
}
$repo = Split-Path $PSScriptRoot -Parent
Copy-Item -LiteralPath (Join-Path $repo 'LICENSE') -Destination (Join-Path $output 'LICENSE-v2rayN.txt')
Copy-Item -LiteralPath (Join-Path $repo 'README.md') -Destination (Join-Path $output 'README.md')
Copy-Item -LiteralPath (Join-Path $repo 'docs/DPI-BYPASS.md') -Destination (Join-Path $output 'DPI-BYPASS.md')
Write-Host "Clean Windows x64 release staged in $output"
