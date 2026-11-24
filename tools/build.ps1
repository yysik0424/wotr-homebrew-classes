# Builds class dlls: tools\build.ps1 (-Mod <Id> | -All) -Config Dev [-LedgerPath <mods.json>]
# Output: dist\<Config>\<Id>\<Id>.dll + Info.json. Ledger = tools\mods.json (read through tools\lib\toolchain.ps1).

# (+ src\Common\**\*.cs when usesCommon), references = common list + ledger refs (+ devRefs for Dev).
# Dev = all sources + /define:DEV_BUILD + devRefs. Release = without src\<Id>\Checks, src\Common\Checks and the ledger devOnly

# -Config is checked here, not with [Parameter(Mandatory)], so a missing value fails instead of prompting.
# testFile is not needed here: the public build has no tests (plan C#10).
param([string]$Mod, [switch]$All, [string]$Config, [string]$LedgerPath, [switch]$Loader)
$ErrorActionPreference = 'Stop'
if ($Loader) {
  if ($Mod -or $All -or ($Config -and $Config -cne 'Release')) { throw 'Use -Loader alone or with -Config Release.' }
  & (Join-Path $PSScriptRoot 'build-loader.ps1') -LedgerPath $LedgerPath
  return
}
. (Join-Path $PSScriptRoot 'lib\toolchain.ps1')
$root = Get-RepoRoot
Assert-Config $Config
$ledger = Read-ModLedger -Root $root -LedgerPath $LedgerPath
if ($Config -ceq 'Dev' -and -not (Test-Path -LiteralPath (Join-Path $root 'src\Common\Checks') -PathType Container)) {
  throw 'Development builds require the Checks sources. Choose Release for the public source distribution.'
}
if (-not (Test-Path -LiteralPath $Script:Csc)) { throw "Roslyn csc not found: $($Script:Csc)" }
if (-not (Test-Path -LiteralPath $Script:GameManaged)) { throw "Game Managed dir not found: $($Script:GameManaged)" }
$targets = @(Select-ModEntries $ledger $Mod $All.IsPresent)

function Build-One($entry) {
  $id = $entry.id
  $out = Get-DistDir $root $Config $id
  $srcDir = Join-Path $root "src\$id"
  $cscArgs = @($Script:LibCscFlags) + @('/out:' + (Join-Path $out "$id.dll"))
  foreach ($ref in (Get-ModRefs $entry $Config)) {
    $p = Join-Path $Script:GameManaged $ref
    if (-not (Test-Path -LiteralPath $p)) { throw "Missing reference: $p" }
    $cscArgs += '/reference:' + $p
  }
  # Display name / version: src\<Id>\modinfo.json (11 classes) or the ledger (pack-warden has no modinfo.json; its values
  # come from pack-warden tools\build.ps1:34-35, build-inputs.md §3). Read-ModLedger already forbids both / requires one.
  $modinfoPath = Join-Path $srcDir 'modinfo.json'
  $modinfo = $null
  if (Test-Path -LiteralPath $modinfoPath) {
    $modinfo = [System.IO.File]::ReadAllText($modinfoPath, [System.Text.Encoding]::UTF8) | ConvertFrom-Json
    foreach ($k in @('displayName', 'version')) { if (-not $modinfo.$k) { throw "modinfo.json: required field '$k' missing or empty ($modinfoPath)" } }
    $displayName = [string]$modinfo.displayName; $version = [string]$modinfo.version
  } else {
    $displayName = [string]$entry.displayName; $version = [string]$entry.version
  }
  # Sources per config (toolchain Get-ModSources): Dev = all, Release = minus Checks folders and ledger devOnly files.
  $sel = Get-ModSources $root $entry $Config
  $sources = @($sel.Sources)
  if ($Config -ceq 'Dev') { $cscArgs += '/define:' + $Script:DevDefine }
  $cscArgs += $sources

  # Requirements: modinfo 'Requirements' (UMM ModInfo.Requirements string[]) as in the 9-branch tools\build.ps1:47-58 (d9465e0).
  
  $req = @()
  if ($null -ne $modinfo) {
    if ($modinfo.PSObject.Properties.Name -ccontains 'Requirements') {
      $req = @($modinfo.Requirements)
      if ($req.Count -eq 0) { throw "modinfo.json: 'Requirements' is present but empty (omit the field instead): $modinfoPath" }
      foreach ($r in $req) { if (-not ($r -is [string]) -or $r -notmatch '^[A-Za-z][A-Za-z0-9_.\-]*$') { throw "modinfo.json: bad Requirements entry '$r' ($modinfoPath)" } }
    } elseif ($modinfo.PSObject.Properties.Name -contains 'Requirements') {
      throw "modinfo.json: field must be spelled 'Requirements' (case-sensitive): $modinfoPath"
    }
  }
  $ledgerReq = @($entry.requires | ForEach-Object { $_.modId })
  if ((($req | Sort-Object) -join '|') -cne (($ledgerReq | Sort-Object) -join '|')) { throw "${id}: modinfo Requirements [$($req -join ', ')] != mods.json requires [$($ledgerReq -join ', ')]" }

  # Output dir only after validation, so a failed validation leaves no empty dist\<Config>\<Id>.
  [System.IO.Directory]::CreateDirectory($out) | Out-Null
  & $Script:Csc @cscArgs
  if ($LASTEXITCODE -ne 0) { throw "compile failed for $id (csc exit $LASTEXITCODE)" }
  $info = [ordered]@{ Id = $id; DisplayName = $displayName; Version = $version; ManagerVersion = $Script:ManagerVersion; AssemblyName = "$id.dll"; EntryMethod = "$id.Main.Load" }
  if ($req.Count -gt 0) { $info['Requirements'] = [string[]]$req }
  [System.IO.File]::WriteAllText((Join-Path $out 'Info.json'), ($info | ConvertTo-Json -Depth 3), [System.Text.UTF8Encoding]::new($false))
  Write-Host ("{0} {1}: compiled {2} source file(s), excluded {3}" -f $Config, $id, $sources.Count, @($sel.Excluded).Count)
  Get-Item (Join-Path $out "$id.dll") | Select-Object Name, Length, LastWriteTime
}

[void](Build-IlScan)
foreach ($e in $targets) { Build-One $e }
