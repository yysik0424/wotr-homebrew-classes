param(
  [Parameter(Mandatory)][string]$Version,
  [Parameter(Mandatory)][string]$Out,
  [switch]$NoDevCompare
)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'lib\toolchain.ps1')
. (Join-Path $PSScriptRoot 'lib\package-content.ps1')
$root = Get-RepoRoot
$outDir = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Out)
$metadata = Read-BundleMetadata $root $Version
Assert-EmptyBundleDestination $outDir
$build = Join-Path $PSScriptRoot 'build.ps1'
if (-not $NoDevCompare) { & $build -All -Config Dev }
& $build -All -Config Release
& (Join-Path $PSScriptRoot 'build-loader.ps1')
$devDir = if (-not $NoDevCompare) { Join-Path $root 'dist\Dev' }
Write-ClassBundle -Root $root -Metadata $metadata -OutDir $outDir -ReleaseDir (Join-Path $root 'dist\Release') -DevDir $devDir
