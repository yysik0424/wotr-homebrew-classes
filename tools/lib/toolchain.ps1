# Shared toolchain: the one place for machine paths, shared references and the mods.json ledger reader.
# Dot-source it: . (Join-Path $PSScriptRoot 'lib\toolchain.ps1'). Every value below names where it came from


Set-StrictMode -Version 2.0

# Game Managed dir: identical in the 11 Common branches' tools\build.ps1:5 and pack-warden tools\build.ps1:3.
$WotrManaged = $null
$WotrCsc = $null
$localSettings = Join-Path $PSScriptRoot 'toolchain.local.ps1'
if (Test-Path -LiteralPath $localSettings -PathType Leaf) { . $localSettings }
$Script:GameManaged = if ($env:WOTR_MANAGED_DIR) { $env:WOTR_MANAGED_DIR } else { $WotrManaged }
# Roslyn csc: identical in the 11 Common branches' tools\build.ps1:6 / tests\run-tests.ps1:5 and pack-warden tools\build.ps1:4 / tests\run-tests.ps1:3.
$Script:Csc = if ($env:WOTR_CSC) { $env:WOTR_CSC } else { $WotrCsc }
if (-not $Script:GameManaged -or -not $Script:Csc) {
  throw 'Set WOTR_MANAGED_DIR and WOTR_CSC to absolute paths, or copy tools/lib/toolchain.local.example.ps1 to toolchain.local.ps1 and edit both paths.'
}
if (-not [IO.Path]::IsPathRooted($Script:GameManaged) -or -not [IO.Path]::IsPathRooted($Script:Csc)) {
  throw 'The game Managed folder and C# compiler must use absolute paths.'
}
# dnlib for the test exes: tests\run-tests.ps1:26 (11 branches, f92a6af) / pack-warden tests\run-tests.ps1:10.
$Script:Dnlib = Join-Path $Script:GameManaged 'UnityModManager\dnlib.dll'
# References every mod dll gets (relative to $GameManaged): spellfist tools\build.ps1:13-17 (d9465e0, 9 branches);
# phantom-blade (d155834) and pack-warden (e35c83d) add one each -> mods.json refs/devRefs. See build-inputs.md §1.
$Script:CommonRefs = @(
  'Assembly-CSharp.dll', 'UnityModManager\UnityModManager.dll', 'UnityModManager\0Harmony.dll',
  'UnityEngine.CoreModule.dll', 'UnityEngine.dll', 'UnityEngine.UI.dll',
  'Owlcat.Runtime.Core.dll', 'Owlcat.SharedTypes.dll', 'Owlcat.Runtime.Validation.dll', 'Owlcat.Runtime.UI.dll',
  'UniRx.dll', 'Newtonsoft.Json.dll', 'netstandard.dll')
# csc flags of the mod dll: all 12 branches' tools\build.ps1 (spellfist :18, pack-warden :19).
$Script:LibCscFlags = @('/nologo', '/target:library', '/langversion:latest', '/utf8output', '/debug-')
# csc flags + BCL references of the test exe: tests\run-tests.ps1:54 (11 branches) = pack-warden tests\run-tests.ps1:34.
$Script:TestCscFlags = @('/nologo', '/target:exe', '/langversion:latest', '/utf8output', '/debug-')
$Script:TestBclRefs = @('System.IO.Compression.dll', 'System.Web.Extensions.dll')
# Game-free Common sources compiled into every Common mod's test exe: tests\run-tests.ps1:47 (11 branches).
$Script:CommonTestSources = @('ReflectionLookupTypes.cs', 'GameGuids.cs', 'Checks\CaptureClassifier.cs', 'WeakKeyTable.cs')
# Info.json ManagerVersion: all 12 branches' tools\build.ps1 (spellfist :46, pack-warden :35).
$Script:ManagerVersion = '0.27.11'
# Bundle roster including Sorrow Inquisitor (2026-11-23).
$Script:ExpectedModCount = 21

# Release = without Checks folders / ledger devOnly files, no define, no devRefs.
$Script:KnownConfigs = @('Dev', 'Release')

$Script:DevDefine = 'DEV_BUILD'

$Script:ChecksFolder = 'Checks'

$Script:IlScanSource = Join-Path (Split-Path -Parent $PSScriptRoot) 'ilscan\IlScan.cs'
$Script:IlScanExe = Join-Path (Split-Path -Parent $PSScriptRoot) 'ilscan\IlScan.exe'
# Shared code folder (not a class): src\Common.
$Script:CommonFolder = 'Common'

# displayName/version/testSources/testExtraFiles exist only for a mod without src\<Id>\modinfo.json (pack-warden).
$Script:LedgerRequiredKeys = @('id', 'branch', 'usesCommon', 'refs', 'devRefs', 'requires', 'optionalAfter', 'devOnly', 'statusSource', 'releaseEntryCalls')
$Script:LedgerNoModinfoKeys = @('displayName', 'version', 'testSources', 'testExtraFiles')
$Script:LedgerOptionalKeys = @('testFile', 'linkedSources') + $Script:LedgerNoModinfoKeys
$Script:StatusSources = @('common', 'packwarden')

function Get-RepoRoot([string]$from) {
  # tools\lib\toolchain.ps1 -> repo root is two levels up from this file.
  $r = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
  if ($from) { $r = $from }
  if (-not [System.IO.Path]::IsPathRooted($r)) { throw "repo root must be absolute: '$r'" }
  foreach ($d in @('src', 'tools')) { if (-not (Test-Path -LiteralPath (Join-Path $r $d))) { throw "not a repo root (no $d\): $r" } }
  return $r
}

function Assert-StringArray($value, [string]$what) {
  if ($null -eq $value) { throw "${what}: missing (null)" }
  foreach ($v in @($value)) { if (-not ($v -is [string]) -or $v -eq '') { throw "${what}: entries must be non-empty strings (got '$v')" } }
}

# Reads and validates tools\mods.json (or -LedgerPath). Throws on any malformed or missing field and when the id set
# differs from the class folders under src\ (src\Common excluded). Returns the entries in ledger order.
function Read-ModLedger([string]$Root, [string]$LedgerPath) {
  if (-not $Root) { throw 'Read-ModLedger: -Root required' }
  if (-not $LedgerPath) { $LedgerPath = Join-Path $Root 'tools\mods.json' }
  if (-not [System.IO.Path]::IsPathRooted($LedgerPath)) { throw "ledger path must be absolute: '$LedgerPath'" }
  if (-not (Test-Path -LiteralPath $LedgerPath)) { throw "mods ledger missing: $LedgerPath" }
  $raw = [System.IO.File]::ReadAllText($LedgerPath, [System.Text.Encoding]::UTF8)
  $parsed = $raw | ConvertFrom-Json
  if (-not ($parsed -is [System.Array])) { throw "mods ledger must be a JSON array: $LedgerPath" }
  $entries = @($parsed)
  if ($entries.Count -ne $Script:ExpectedModCount) { throw "mods ledger has $($entries.Count) entries, expected $($Script:ExpectedModCount): $LedgerPath" }
  $ids = @{}
  foreach ($e in $entries) {
    $names = @($e.PSObject.Properties | ForEach-Object { $_.Name })
    foreach ($k in $Script:LedgerRequiredKeys) { if ($names -cnotcontains $k) { throw "mods ledger entry '$($e.id)': key '$k' missing" } }
    foreach ($n in $names) { if (($Script:LedgerRequiredKeys + $Script:LedgerOptionalKeys) -cnotcontains $n) { throw "mods ledger entry '$($e.id)': unknown key '$n'" } }
    if (-not ($e.id -is [string]) -or $e.id -cnotmatch '^[A-Z][A-Za-z0-9]*$') { throw "mods ledger: bad id '$($e.id)'" }
    if ($e.id -ceq $Script:CommonFolder) { throw "mods ledger: '$($Script:CommonFolder)' is shared code, not a class" }
    if ($ids.ContainsKey($e.id)) { throw "mods ledger: duplicate id '$($e.id)'" }
    $ids[$e.id] = $true
    if (-not ($e.branch -is [string]) -or $e.branch -eq '') { throw "mods ledger entry '$($e.id)': branch must be a non-empty string" }
    if (-not ($e.usesCommon -is [bool])) { throw "mods ledger entry '$($e.id)': usesCommon must be true/false" }
    foreach ($k in @('refs', 'devRefs', 'optionalAfter', 'devOnly', 'releaseEntryCalls')) {
      if (-not ($e.$k -is [System.Array])) { throw "mods ledger entry '$($e.id)': '$k' must be an array" }
      Assert-StringArray $e.$k "mods ledger entry '$($e.id)'.$k"
    }
    if ($names -ccontains 'linkedSources') {
      if (-not ($e.linkedSources -is [System.Array])) { throw "linkedSources must be an array for $($e.id)" }
      Assert-StringArray $e.linkedSources "linkedSources for $($e.id)"
      foreach ($f in $e.linkedSources) {
        if ($f -cnotmatch '^[A-Z][A-Za-z0-9]*/[A-Z][A-Za-z0-9]*[.]cs$') { throw "Invalid linked source for $($e.id): $f" }
      }
    }
    foreach ($r in @($e.refs) + @($e.devRefs)) { if ($Script:CommonRefs -contains $r) { throw "mods ledger entry '$($e.id)': '$r' is already a common reference" } }
    if (-not ($e.requires -is [System.Array])) { throw "mods ledger entry '$($e.id)': 'requires' must be an array" }
    foreach ($q in @($e.requires)) {
      $qn = @($q.PSObject.Properties | ForEach-Object { $_.Name })
      if ($qn.Count -ne 2 -or $qn -cnotcontains 'modId' -or $qn -cnotcontains 'display') { throw "mods ledger entry '$($e.id)': requires entries are {modId, display}" }
      if (-not ($q.modId -is [string]) -or $q.modId -cnotmatch '^[A-Za-z][A-Za-z0-9_.\-]*$') { throw "mods ledger entry '$($e.id)': bad requires.modId '$($q.modId)'" }
      if (-not ($q.display -is [string]) -or $q.display -eq '') { throw "mods ledger entry '$($e.id)': requires.display empty" }
    }
    if ($Script:StatusSources -cnotcontains $e.statusSource) { throw "mods ledger entry '$($e.id)': statusSource must be one of $($Script:StatusSources -join ', ')" }
    # PackWarden keeps its own Main/status fields (recon fact: no src\Common) - the two flags describe the same fact.
    if (($e.usesCommon -and $e.statusSource -ne 'common') -or (-not $e.usesCommon -and $e.statusSource -ne 'packwarden')) { throw "mods ledger entry '$($e.id)': usesCommon=$($e.usesCommon) contradicts statusSource=$($e.statusSource)" }
    if ($names -ccontains 'testFile' -and (-not ($e.testFile -is [string]) -or $e.testFile -cnotmatch '^[A-Za-z][A-Za-z0-9]*\.cs$')) { throw "mods ledger entry '$($e.id)': testFile must be a file name like '<Id>Tests.cs'" }
  }
  # Ledger ids == class folders, checked before anything that reads src\<Id> (so a wrong id is reported as such).
  $srcRoot = Join-Path $Root 'src'
  $folders = @(Get-ChildItem -LiteralPath $srcRoot -Directory | ForEach-Object { $_.Name } | Where-Object { $_ -cne $Script:CommonFolder -and $_ -cne 'Loader' })
  $missing = @($folders | Where-Object { -not $ids.ContainsKey($_) })
  $extra = @($ids.Keys | Where-Object { $folders -cnotcontains $_ })
  if ($missing.Count -gt 0 -or $extra.Count -gt 0) { throw "mods ledger ids != class folders under src\ (in src\ only: [$($missing -join ', ')]; in ledger only: [$($extra -join ', ')])" }
  foreach ($e in $entries) {
    $names = @($e.PSObject.Properties | ForEach-Object { $_.Name })
    $srcDir = Join-Path $Root "src\$($e.id)"
    $hasModinfo = Test-Path -LiteralPath (Join-Path $srcDir 'modinfo.json')
    foreach ($k in $Script:LedgerNoModinfoKeys) {
      if ($hasModinfo -and $names -ccontains $k) { throw "mods ledger entry '$($e.id)': '$k' belongs in src\$($e.id)\modinfo.json (one source), not in the ledger" }
    }
    if (-not $hasModinfo) {
      foreach ($k in @('displayName', 'version')) { if ($names -cnotcontains $k -or -not ($e.$k -is [string]) -or $e.$k -eq '') { throw "mods ledger entry '$($e.id)': no modinfo.json, so '$k' is required in the ledger" } }
    }
  }
  return $entries
}

# Picks the entries named by -Mod / -All. Exactly one of the two.
function Select-ModEntries($entries, [string]$Mod, [bool]$All) {
  if ($All -and $Mod) { throw 'give -Mod <Id> or -All, not both' }
  if (-not $All -and -not $Mod) { throw 'give -Mod <Id> or -All' }
  if ($All) { return $entries }
  $hit = @($entries | Where-Object { $_.id -ceq $Mod })
  if ($hit.Count -ne 1) { throw "unknown -Mod '$Mod' (ledger ids: $((@($entries | ForEach-Object { $_.id })) -join ', '))" }
  return $hit
}

function Assert-Config([string]$Config) {
  if (-not $Config) { throw "-Config required (one of: $($Script:KnownConfigs -join ', '))" }
  if ($Script:KnownConfigs -cnotcontains $Config) { throw "unknown -Config '$Config' (implemented: $($Script:KnownConfigs -join ', '))" }
}

# Effective reference list (file names relative to $GameManaged) of one mod for one config.
function Get-ModRefs($entry, [string]$Config) {
  Assert-Config $Config
  $r = @($Script:CommonRefs) + @($entry.refs)
  if ($Config -ceq 'Dev') { $r += @($entry.devRefs) }
  return $r
}

# Source files of one mod for one config (absolute paths). Release drops every file under a Checks folder of src\<Id> and
# src\Common, and the ledger devOnly files (paths relative to src\<Id>); each devOnly entry must name an existing file.
# Returns @{ Sources = [...]; Excluded = [...] }.
function Get-ModSources([string]$Root, $entry, [string]$Config) {
  Assert-Config $Config
  $srcDir = Join-Path $Root "src\$($entry.id)"
  $commonDir = Join-Path $Root "src\$($Script:CommonFolder)"
  $all = @(Get-ChildItem -LiteralPath $srcDir -Recurse -Filter *.cs | ForEach-Object { $_.FullName })
  if ($all.Count -eq 0) { throw "No .cs sources under $srcDir" }
  if (@($entry.PSObject.Properties.Name) -ccontains 'linkedSources') {
    foreach ($f in $entry.linkedSources) { $all += (Get-Item -LiteralPath (Join-Path $Root "src/$f")).FullName }
  }
  $roots = @($srcDir)
  if ($entry.usesCommon) {
    if (-not (Test-Path -LiteralPath $commonDir)) { throw "Common source dir missing: $commonDir" }
    $commonSources = @(Get-ChildItem -LiteralPath $commonDir -Recurse -Filter *.cs | ForEach-Object { $_.FullName })
    # Every Common mod links the shared code (ModContext, Strings, BuildHook ...); empty = broken checkout.
    if ($commonSources.Count -eq 0) { throw "No .cs sources under $commonDir" }
    $all += $commonSources
    $roots += $commonDir
  } else {
    # PackWarden has its own loader hooks but uses the same Mono weak-identity storage.
    $all += (Join-Path $commonDir 'WeakKeyTable.cs')
  }
  $devOnly = @()
  foreach ($d in @($entry.devOnly)) {
    $p = Join-Path $srcDir $d
    if (-not (Test-Path -LiteralPath $p -PathType Leaf)) { throw "mods ledger entry '$($entry.id)': devOnly file not found: $p" }
    $devOnly += (Get-Item -LiteralPath $p).FullName
  }
  if ($Config -ceq 'Dev') { return @{ Sources = $all; Excluded = @() } }
  $excluded = @()
  $keep = @()
  foreach ($f in $all) {
    $isChecks = $false
    foreach ($r in $roots) {
      $prefix = (Join-Path $r $Script:ChecksFolder) + [System.IO.Path]::DirectorySeparatorChar
      if ($f.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)) { $isChecks = $true }
    }
    if ($isChecks -or ($devOnly -contains $f)) { $excluded += $f } else { $keep += $f }
  }
  if ($keep.Count -eq 0) { throw "$($entry.id): Release keeps no sources" }
  return @{ Sources = $keep; Excluded = $excluded }
}

# Compiles tools\ilscan\IlScan.cs -> tools\ilscan\IlScan.exe (+ dnlib.dll beside it) when the exe is missing or older than
# the source. Returns the exe path. Throws on a compile failure.
function Build-IlScan {
  if (-not (Test-Path -LiteralPath $Script:IlScanSource)) { throw "IlScan source missing: $($Script:IlScanSource)" }
  if (-not (Test-Path -LiteralPath $Script:Dnlib)) { throw "dnlib not found: $($Script:Dnlib)" }
  $dir = Split-Path -Parent $Script:IlScanExe
  $dnlibCopy = Join-Path $dir 'dnlib.dll'
  $fresh = (Test-Path -LiteralPath $Script:IlScanExe) -and (Test-Path -LiteralPath $dnlibCopy) -and
    ((Get-Item -LiteralPath $Script:IlScanExe).LastWriteTimeUtc -ge (Get-Item -LiteralPath $Script:IlScanSource).LastWriteTimeUtc)
  if ($fresh) { return $Script:IlScanExe }
  if (-not (Test-Path -LiteralPath $Script:Csc)) { throw "Roslyn csc not found: $($Script:Csc)" }
  Copy-Item -LiteralPath $Script:Dnlib -Destination $dnlibCopy -Force
  $out = & $Script:Csc /nologo /target:exe /langversion:latest /utf8output /debug- ('/out:' + $Script:IlScanExe) ('/reference:' + $dnlibCopy) $Script:IlScanSource
  if ($LASTEXITCODE -ne 0) { $out | ForEach-Object { Write-Host $_ }; throw "IlScan compile failed (csc exit $LASTEXITCODE)" }
  return $Script:IlScanExe
}

# Runs IlScan and returns its stdout lines; throws on a non-zero exit (never an empty list for a failed read).
function Invoke-IlScan([string[]]$argv) {
  $exe = Build-IlScan
  $prev = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
  try { $lines = @(& $exe @argv 2>&1); $code = $LASTEXITCODE } finally { $ErrorActionPreference = $prev }
  $errs = @($lines | Where-Object { $_ -is [System.Management.Automation.ErrorRecord] } | ForEach-Object { [string]$_ })
  $outLines = @($lines | Where-Object { -not ($_ -is [System.Management.Automation.ErrorRecord]) } | ForEach-Object { [string]$_ })
  if ($code -ne 0) { throw "IlScan $($argv -join ' ') failed (exit $code): $($errs -join ' | ')" }
  return $outLines
}

function Get-DistDir([string]$Root, [string]$Config, [string]$Id) { return (Join-Path (Join-Path (Join-Path $Root 'dist') $Config) $Id) }


# "형식 이름은 소스에서 뽑아 원장 옆 생성 파일로"). Comments and string/char literals are blanked first; a type counts when it is
# declared at the namespace's own brace depth (nested types are covered by verify-release's "Outer/" prefix match).
# Throws when the file declares no type (an empty result would make the verify-release check vacuous).
function Get-SourceTopLevelTypes([string]$Path) {
  if (-not [System.IO.Path]::IsPathRooted($Path)) { throw "Get-SourceTopLevelTypes: path must be absolute: '$Path'" }
  $t = [System.IO.File]::ReadAllText($Path, [System.Text.Encoding]::UTF8)
  # Blank comments and literals (keep newlines/braces outside them): verbatim strings, regular strings, chars, // and /* */.
  $t = [regex]::Replace($t, '@"(?:[^"]|"")*"|"(?:\.|[^"\\r\n])*"|''(?:\.|[^''\\r\n])+''|//[^\r\n]*|/\*[\s\S]*?\*/', { param($m) ' ' * $m.Value.Length })
  $names = New-Object System.Collections.Generic.List[string]
  $depth = 0; $nsStack = New-Object System.Collections.Generic.List[object]   # entries: @{ Name; Depth }
  $fileNs = $null
  $tokens = [regex]::Matches($t, '\{|\}|;|\bnamespace\s+([A-Za-z_][\w.]*)\s*(\{|;)|\b(?:class|struct|interface|enum|record)\s+([A-Za-z_]\w*)|\bdelegate\s+[^;(]*?([A-Za-z_]\w*)\s*(?:<[^>]*>)?\s*\(')
  foreach ($m in $tokens) {
    $v = $m.Value
    if ($m.Groups[1].Success) {
      if ($m.Groups[2].Value -eq '{') { $depth++; $nsStack.Add(@{ Name = $m.Groups[1].Value; Depth = $depth }) } else { $fileNs = $m.Groups[1].Value }
      continue
    }
    if ($v -eq '{') { $depth++; continue }
    if ($v -eq '}') { if ($nsStack.Count -gt 0 -and $nsStack[$nsStack.Count - 1].Depth -eq $depth) { $nsStack.RemoveAt($nsStack.Count - 1) }; $depth--; continue }
    if ($v -eq ';') { continue }
    $name = if ($m.Groups[3].Success) { $m.Groups[3].Value } else { $m.Groups[4].Value }
    $nsDepth = if ($nsStack.Count -gt 0) { $nsStack[$nsStack.Count - 1].Depth } else { 0 }
    if ($depth -ne $nsDepth) { continue }
    $parts = @(); if ($fileNs) { $parts += $fileNs }; $parts += @($nsStack | ForEach-Object { $_.Name })
    $names.Add((@($parts) + @($name)) -join '.')
  }
  if ($depth -ne 0) { throw "Get-SourceTopLevelTypes: unbalanced braces after blanking literals: $Path" }
  if ($names.Count -eq 0) { throw "Get-SourceTopLevelTypes: no type declared in $Path" }
  return @($names | Sort-Object -Unique)
}


# ledger class (empty list = no devOnly file). Written by tools\gen-devonly-types.ps1; tests\ReleaseBuildTests.ps1 fails when it is stale.
function Get-DevOnlyTypeMap([string]$Root, $entries) {
  $map = [ordered]@{}
  foreach ($e in $entries) {
    $list = @()
    foreach ($d in @($e.devOnly)) {
      $p = Join-Path (Join-Path $Root "src\$($e.id)") $d
      if (-not (Test-Path -LiteralPath $p -PathType Leaf)) { throw "mods ledger entry '$($e.id)': devOnly file not found: $p" }
      $list += @(Get-SourceTopLevelTypes $p)
    }
    $map[$e.id] = @($list | Sort-Object -Unique)
  }
  return $map
}
function Get-DevOnlyTypesPath([string]$Root) { return (Join-Path $Root 'tools\devonly-types.json') }
function ConvertTo-DevOnlyTypesJson($map) {
  $lines = @('{')
  $ids = @($map.Keys)
  for ($i = 0; $i -lt $ids.Count; $i++) {
    $vals = @($map[$ids[$i]] | ForEach-Object { '"' + $_ + '"' }) -join ', '
    $lines += ('  "' + $ids[$i] + '": [' + $vals + ']' + $(if ($i -lt $ids.Count - 1) { ',' } else { '' }))
  }
  $lines += '}'
  return (($lines -join "`n") + "`n")
}
