param(
  [ValidateSet('Debug','Release')] [string]$Configuration = 'Release',
  [string]$Runtime = 'win-x64',
  [switch]$Clean,
  [switch]$Binlog   # WiX CLI doesn't produce MSBuild binlogs; keeping for parity (unused now)
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
try
{
# --- Paths (relative to this script) ---
$here       = $PSScriptRoot                              # ...\installer
$root       = (Resolve-Path (Join-Path $here '..')).Path # repo root

# NOTE: Fixed csproj path (project file is inside the app folder)
$csproj     = Join-Path $root 'BBI - MDM Workflow.csproj'

# MSBUILD/WIXPROJ PATHS (not used in CLI flow; kept for easy revert)
# $msiProj    = Join-Path $here 'wix\BBI.MDM.Workflow.Msi.wixproj'
# $bundleProj = Join-Path $here 'wix\BBI.MDM.Workflow.Bundle.wixproj'

$wixDir     = Join-Path $here 'wix'
$redistExe  = Join-Path $here 'redist\WindowsAppRuntimeInstall-x64.exe'
$redistDir  = Split-Path $redistExe -Parent

# Where CLI will drop outputs
$msiOut     = Join-Path $wixDir 'BBI.MDM.Workflow.msi'
$bundleOut  = Join-Path $wixDir 'BBI.MDM.Workflow.Setup.exe'

# --- Sanity checks ---
foreach ($p in @($csproj, (Join-Path $wixDir 'Product.wxs'), (Join-Path $wixDir 'AppFiles.wxs'), (Join-Path $wixDir 'bundle\Bundle.wxs'))) {
  if (-not (Test-Path $p)) { throw "Missing file: $p" }
}
if (-not (Test-Path $redistExe)) {
  Write-Warning "WindowsAppRuntime installer not found: $redistExe (the bundle will fail to chain the runtime)."
}

# Ensure WiX CLI is available
$wixCmd = Get-Command wix -ErrorAction SilentlyContinue
if (-not $wixCmd) {
  throw "WiX CLI ('wix') not found on PATH. Install WiX v6 and make sure 'wix' resolves."
}

# Ensure we're not mixing MSBuild + CLI flows
# (These were relevant only to .wixproj builds—leaving commented for reference)
# $msiXml = if (Test-Path $msiProj) { Get-Content $msiProj -Raw } else { '' }
# if ($msiXml -match 'Compile Include=".*Bundle\.wxs"' -or $msiXml -match 'Compile Include="\*\.wxs"') {
#   throw "Bundle.wxs is included by the MSI .wixproj. For CLI builds, .wixproj files are not used."
# }

# Optional clean
if ($Clean) {
  Write-Host "Cleaning wix outputs..." -ForegroundColor Cyan
  Get-ChildItem $wixDir -Include 'BBI.MDM.Workflow.msi','BBI.MDM.Workflow.Setup.exe' -Force -ErrorAction SilentlyContinue |
    Remove-Item -Force -ErrorAction SilentlyContinue

  # MSBuild clean (not used now)
  # dotnet clean $msiProj -c $Configuration | Out-Null
  # dotnet clean $bundleProj -c $Configuration | Out-Null
}

# --- 1) Publish app so the MSI can harvest files ---
$pubArgs = @(
  $csproj, '-c', $Configuration, '-r', $Runtime,
  '-p:SelfContained=true',
  '-p:WindowsAppSDKSelfContained=false',
  '-p:PublishSingleFile=false',
  '-p:PublishTrimmed=false',
  '-p:SatelliteResourceLanguages=en-US'
)
Write-Host "`nPublishing app..." -ForegroundColor Cyan
dotnet publish @pubArgs

$publishDir = Join-Path $root "\bin\$Configuration\net8.0-windows10.0.19041.0\$Runtime\publish"
if (!(Test-Path $publishDir)) { throw "Publish output not found: $publishDir" }
if (-not (Get-ChildItem $publishDir -File -Recurse | Select-Object -First 1)) {
  throw "Publish output is empty: $publishDir"
}

# Ensure WiX CLI is available
$wixCmd = Get-Command wix -ErrorAction SilentlyContinue
if (-not $wixCmd) { throw "WiX CLI ('wix') not found on PATH. Install WiX 6.x so 'wix --version' works." }

# --- 2) Build MSI via WiX CLI ---

Write-Host "`nEnsuring WiX CLI extensions are available..." -ForegroundColor Cyan

# ✅ Correct way to add extensions (global cache under %USERPROFILE%\.wix\extensions)
wix extension add -g WixToolset.Harvesters.wixext/6.0.2 -s https://api.nuget.org/v3/index.json | Out-Null
wix extension add -g WixToolset.Bal.wixext/6.0.2         -s https://api.nuget.org/v3/index.json | Out-Null
wix extension add -g WixToolset.Util.wixext/6.0.2        -s https://api.nuget.org/v3/index.json | Out-Null

# Verify they’re present (fail fast if not)
$extList = wix extension list -g
$hasBA   = $extList | Select-String -Quiet 'WixToolset\.BootstrapperApplications\.wixext'
$hasUtil = $extList | Select-String -Quiet 'WixToolset\.Util\.wixext'

if (-not ($hasBA -and $hasUtil)) {
  throw "WiX extensions not installed. Output from 'wix extension list -g':`n$extList"
}
# helper to quote values safely
function Quote($s) { '"' + ($s -replace '"','`"') + '"' }
$bundleDir = "C:\Users\MarkYoung\source\repos\BBI - MDM Workflow\installer\wix\bundle"
$publishDirQ = Quote($publishDir)
$msiOutQ     = Quote($msiOut)
$bundleOutQ  = Quote($bundleOut)
$redistDirQ  = Quote($redistDir)
$bundleDirQ  = Quote($bundleDir)   # only if you use LicenseFile with $(var.BundleDir)


Write-Host "`nBuilding MSI (WiX CLI)..." -ForegroundColor Cyan
$msiArgs = @(
  'build',
  (Join-Path $wixDir 'Product.wxs'),
  (Join-Path $wixDir 'AppFiles.wxs'),
#  '-ext','WixToolset.Harvesters.wixext',
  '-d', "PublishDir=$publishDirQ",
  '-o', $msiOut
)
& $wixCmd.Source @msiArgs

if (!(Test-Path $msiOut)) { throw "MSI not produced: $msiOut" }

# --- 3) Build Bundle via WiX CLI ---
# Add required extensions for the bundle
#& $wixCmd.Source extension add -g WixToolset.Bal.wixext/6.0.2  -s https://api.nuget.org/v3/index.json | Out-Null
#& $wixCmd.Source extension add -g WixToolset.Util.wixext/6.0.2 -s https://api.nuget.org/v3/index.json | Out-Null

Write-Host "`nBuilding Bundle (WiX CLI)..." -ForegroundColor Cyan
$bundleArgs = @(
  'build',
  (Join-Path $wixDir 'bundle\Bundle.wxs'),
  '-ext','WixToolset.BootstrapperApplications.wixext',
  '-ext','WixToolset.Util.wixext',
  '-d',"MsiPath=$msiOutQ",
  '-d','BundleDir=$bundleDirQ'
  '-d','RedistDir=$redistDirQ',
  '-o', $bundleOut
)
& $wixCmd.Source @bundleArgs

if (!(Test-Path $bundleOut)) { throw "Bundle EXE not produced: $bundleOut" }

# --- 4) Show outputs (CLI writes directly to wix folder) ---
"`nOutputs:"
Get-Item $msiOut, $bundleOut | Select-Object Length, LastWriteTime, FullName | Format-Table -AutoSize

Write-Host "`nDone." -ForegroundColor Green
}
catch
{
# $_ or $PSItem is the **current** error record
  "Type: $($_.Exception.GetType().FullName)"
  "Message: $($_.Exception.Message)"
  "Where: $($_.InvocationInfo.PositionMessage)"
  "Stack:`n$($_.ScriptStackTrace)"
  # or: $_ | Format-List * -Force
 }