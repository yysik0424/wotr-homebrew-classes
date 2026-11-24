param([string]$LedgerPath)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'lib\toolchain.ps1')
$root = Get-RepoRoot
$ledger = @(Read-ModLedger -Root $root -LedgerPath $LedgerPath)
$loaderDir = Join-Path $root 'src\Loader'
$info = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $loaderDir 'loader.json') | ConvertFrom-Json
if ($info.PSObject.Properties.Name -contains 'Requirements' -or $info.PSObject.Properties.Name -contains 'LoadAfter') {
  throw 'Bundle dependencies are determined by the class manifest.'
}
if ($info.ManagerVersion -cne $Script:ManagerVersion) { throw 'Inconsistent manager version.' }
$out = Get-DistDir $root 'Release' $info.Id
[void][IO.Directory]::CreateDirectory($out)
$generatedDir = Join-Path $root 'dist\generated\Loader'
[void][IO.Directory]::CreateDirectory($generatedDir)
function Quote-Cs([string]$value) {
  if ([string]::IsNullOrWhiteSpace($value) -or $value -match '[\x00-\x1f]') { throw 'Invalid class manifest text.' }
  return '"' + $value.Replace('\', '\\').Replace('"','\"') + '"'
}
$lines = New-Object 'System.Collections.Generic.List[string]'
$ordering = New-Object 'System.Collections.Generic.List[string]'
foreach ($entry in $ledger) {
  $meta = $entry
  if ($entry.usesCommon) {
    $meta = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $root "src\$($entry.id)\modinfo.json") | ConvertFrom-Json
  }
  $deps = @()
  foreach ($dep in $entry.requires) {
    $deps += ('new Dependency(' + (Quote-Cs $dep.modId) + ', ' + (Quote-Cs $dep.display) + ')')
    if (-not $ordering.Contains($dep.modId)) { $ordering.Add($dep.modId) }
  }
  foreach ($id in $entry.optionalAfter) { if (-not $ordering.Contains($id)) { $ordering.Add($id) } }
  $lines.Add(('new ModuleInfo({0}, {1}, {2}, {3}, new Dependency[] {{ {4} }}),' -f (Quote-Cs $entry.id), (Quote-Cs $meta.displayName), (Quote-Cs $meta.version), (Quote-Cs $entry.statusSource), ($deps -join ', ')))
}
$g = Join-Path $generatedDir 'ModuleLedger.g.cs'
$body = @('namespace HomebrewClasses.Loader { internal static class ModuleLedger { internal static readonly ModuleInfo[] Modules = {') + $lines.ToArray() + @('}; } }')
[IO.File]::WriteAllLines($g, [string[]]$body, [Text.Encoding]::UTF8)
$info | Add-Member -NotePropertyName LoadAfter -NotePropertyValue ([string[]]$ordering.ToArray())
$refs = @('Assembly-CSharp.dll','UnityModManager\UnityModManager.dll','UnityModManager\0Harmony.dll','UnityEngine.dll','UnityEngine.CoreModule.dll','UnityEngine.IMGUIModule.dll','netstandard.dll') | ForEach-Object { '/reference:'+(Join-Path $Script:GameManaged $_) }
$src = @(Get-ChildItem -LiteralPath $loaderDir -Filter '*.cs' -File | ForEach-Object {$_.FullName})
& $Script:Csc @Script:LibCscFlags @refs '/reference:System.Xml.dll' '/reference:System.Core.dll' ('/out:'+(Join-Path $out $info.AssemblyName)) @src $g
if ($LASTEXITCODE -ne 0) { throw "Loader compilation failed (exit $LASTEXITCODE)." }
[IO.File]::WriteAllText((Join-Path $out 'Info.json'), ($info | ConvertTo-Json -Depth 4), [Text.UTF8Encoding]::new($false))
Write-Output "Loader built: $out"
