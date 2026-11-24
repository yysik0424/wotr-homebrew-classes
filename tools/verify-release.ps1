
# Last line: 'RELEASE OK <Id>' or 'RELEASE FAIL <Id>: <item>; <item>...' (exit 1). Reads the dll with tools\ilscan\IlScan.exe only.
# Items:
#   1 no type whose namespace ends with '.Checks' (or contains '.Checks.')
#   2 no type defined by a ledger devOnly file (names from tools\devonly-types.json, generated from source - no Dev dll needed)
#   3 no IlScan string matching tools\forbidden-terms.txt or the fixed trigger-file names below
#   4 no call to UnityEngine.Application::Quit
#   5 Harmony patches on the save path: only the allow list below, and no bool-returning Prefix on it
#   6 <Id>.Main::Load references every ledger releaseEntryCalls entry
#   7 (-DevDll) user types: release == dev - .Checks - devOnly; generated ('G:') types: release subset of dev
param([string]$Dll, [string]$Mod, [string]$DevDll, [string]$LedgerPath)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'lib\toolchain.ps1')
$root = Get-RepoRoot
if (-not $Dll -or -not $Mod) { throw 'usage: verify-release.ps1 -Dll <path> -Mod <Id> [-DevDll <path>]' }
foreach ($p in @($Dll, $DevDll)) { if ($p -and -not [System.IO.Path]::IsPathRooted($p)) { throw "dll path must be absolute: '$p'" } }
if (-not (Test-Path -LiteralPath $Dll)) { throw "release dll not found: $Dll" }
if ($DevDll -and -not (Test-Path -LiteralPath $DevDll)) { throw "dev dll not found: $DevDll" }
$ledger = Read-ModLedger -Root $root -LedgerPath $LedgerPath
$isLoader = $Mod -ceq 'HomebrewClasses'
if ($isLoader) {
  if ($DevDll) { throw 'The bundle entry assembly has one build configuration' }
} else {
  $entry = @(Select-ModEntries $ledger $Mod $false)[0]
}


# Common Checks\CheckRunner.cs TriggerFileName / run-all, PhantomBlade LeakCleanupRules.TriggerFileName.
$FixedForbidden = @('RUN_CHECKS', 'RUN_ALL_CHECKS', 'CLEAN_LEAKED_ITEMS')

$QuitMethod = 'UnityEngine.Application::Quit'

# Common Checks\CheckPatches.cs:39). Allowed: Golemancer FusionPreSavePatch, a void Prefix that detaches fusions before saving
# (src\Golemancer\Fusion.cs FusionPreSavePatch; recon fact "golemancer Fusion.cs SaveRoutine Prefix 는 void ... 남긴다").
$SaveTargets = @(
  @{ Type = 'Kingmaker.EntitySystem.Persistence.SaveManager'; Method = '*' },
  @{ Type = 'Kingmaker.Game'; Method = 'SaveGame' })
$SaveAllow = @(@{ Mod = 'Golemancer'; Patch = 'Golemancer.FusionPreSavePatch'; Prefix = 'System.Void' })
$ChecksNsPattern = '\.Checks$|\.Checks\.'
$GenPrefix = 'G:'

$fails = New-Object System.Collections.Generic.List[string]
$identity = [System.Reflection.AssemblyName]::GetAssemblyName($Dll).Name
if ($identity -cne $Mod) { $fails.Add("assembly identity '$identity' differs from '$Mod'") }
function Top([string]$full) { $n = $full; if ($n.StartsWith($GenPrefix)) { $n = $n.Substring($GenPrefix.Length) }; $k = $n.IndexOf('/'); if ($k -ge 0) { $n = $n.Substring(0, $k) }; return $n }
function Ns([string]$full) { $t = Top $full; $k = $t.LastIndexOf('.'); if ($k -lt 0) { return '' }; return $t.Substring(0, $k) }

$types = @(Invoke-IlScan @('types', $Dll))
if ($types.Count -eq 0) { throw "IlScan types returned no type for $Dll" }

# 1
$checksTypes = @($types | Where-Object { (Ns $_) -match $ChecksNsPattern })
if ($checksTypes.Count -gt 0) { $fails.Add("item 1 .Checks types ($($checksTypes.Count)): $((@($checksTypes | Select-Object -First 5)) -join ', ')") }

# 2
$devOnlyPath = Get-DevOnlyTypesPath $root
if (-not (Test-Path -LiteralPath $devOnlyPath)) { throw "generated devOnly type list missing: $devOnlyPath (run tools\gen-devonly-types.ps1)" }
$devOnlyMap = [System.IO.File]::ReadAllText($devOnlyPath, [System.Text.Encoding]::UTF8) | ConvertFrom-Json
$devOnlyTypes = @()
if ($isLoader) {
  $devOnlyTypes = @($devOnlyMap.PSObject.Properties | ForEach-Object { $_.Value } | Sort-Object -Unique)
} else {
  if (@($devOnlyMap.PSObject.Properties.Name) -cnotcontains $Mod) { throw "$devOnlyPath has no entry for $Mod (regenerate its type inventory)" }
  $devOnlyTypes = @($devOnlyMap.$Mod)
  if (@($entry.devOnly).Count -gt 0 -and $devOnlyTypes.Count -eq 0) { throw "$devOnlyPath lists no type for $Mod but the ledger has devOnly files" }
}
$hitDevOnly = @($types | Where-Object { $devOnlyTypes -ccontains (Top $_) })
if ($hitDevOnly.Count -gt 0) { $fails.Add("item 2 devOnly types: $((@($hitDevOnly | Select-Object -First 5)) -join ', ')") }

# 3
$termsPath = Join-Path $root 'tools\forbidden-terms.txt'
if (-not (Test-Path -LiteralPath $termsPath)) { throw "forbidden terms missing: $termsPath" }
$terms = @([System.IO.File]::ReadAllLines($termsPath, [System.Text.Encoding]::UTF8) | Where-Object { $_.Trim() -ne '' -and -not $_.TrimStart().StartsWith('#') })
if ($terms.Count -eq 0) { throw "no forbidden term in $termsPath" }
$patterns = @($terms | ForEach-Object { [regex]::new($_) }) + @($FixedForbidden | ForEach-Object { [regex]::new([regex]::Escape($_)) })
$strings = @(Invoke-IlScan @('strings', $Dll))
$strHits = New-Object System.Collections.Generic.List[string]
foreach ($s in $strings) { foreach ($rx in $patterns) { if ($rx.IsMatch($s)) { $strHits.Add("[$rx] $s"); break } } }
if ($strHits.Count -gt 0) { $fails.Add("item 3 forbidden strings ($($strHits.Count)): $((@($strHits | Select-Object -First 5)) -join ' || ')") }

# 4
$quit = @(Invoke-IlScan @('calls', $Dll, $QuitMethod))
if ($quit.Count -gt 0) { $fails.Add("item 4 $QuitMethod callers: $($quit -join ', ')") }

# 5
foreach ($line in @(Invoke-IlScan @('harmony', $Dll))) {
  $c = $line.Split("`t")
  if ($c.Count -ne 5) { throw "IlScan harmony: unexpected line '$line'" }
  $patch = $c[0]; $tt = $c[1]; $tm = $c[2]; $prefix = $c[3].Substring('prefix='.Length)
  $onSave = @($SaveTargets | Where-Object { $_.Type -ceq $tt -and ($_.Method -ceq '*' -or $_.Method -ceq $tm) }).Count -gt 0
  if (-not $onSave) { continue }
  if ($prefix -ceq 'System.Boolean') { $fails.Add("item 5 bool Prefix on save path: $patch -> ${tt}::${tm}"); continue }
  $ok = @($SaveAllow | Where-Object { $_.Mod -ceq $Mod -and $_.Patch -ceq $patch -and $_.Prefix -ceq $prefix }).Count -gt 0
  if (-not $ok) { $fails.Add("item 5 save-path patch not in allow list: $patch -> ${tt}::${tm} (prefix=$prefix)") }
}

# 6
if ($isLoader) {
  $loadSpec = 'HomebrewClasses.Loader.Main::Load'
  $want = @('HomebrewClasses.Loader.AssemblyNameResolver::Install', 'HomebrewClasses.Loader.LoaderPlan::Decide',
            'HomebrewClasses.Loader.ClassHost::Load', 'HomebrewClasses.Loader.BundlePatches::Install',
            'HomebrewClasses.Loader.BundleRoutes::Activate')
} else {
  $loadSpec = "$Mod.Main::Load"
  $want = @($entry.releaseEntryCalls)
}
if ($want.Count -eq 0) { $fails.Add("item 6 ledger releaseEntryCalls is empty for $Mod (nothing locks the entry point)") }
$callees = @(Invoke-IlScan @('callees', $Dll, $loadSpec))
$missingCalls = @($want | Where-Object { $callees -cnotcontains $_ })
if ($missingCalls.Count -gt 0) { $fails.Add("item 6 $loadSpec misses: $($missingCalls -join ', ')") }

# 7
if ($DevDll) {
  $devTypes = @(Invoke-IlScan @('types', $DevDll))
  $relUser = @($types | Where-Object { -not $_.StartsWith($GenPrefix) })
  $devUser = @($devTypes | Where-Object { -not $_.StartsWith($GenPrefix) -and (Ns $_) -notmatch $ChecksNsPattern -and $devOnlyTypes -cnotcontains (Top $_) })
  $extra = @($relUser | Where-Object { $devUser -cnotcontains $_ })
  $missing = @($devUser | Where-Object { $relUser -cnotcontains $_ })
  # Generated names carry compiler ordinals (<>c__DisplayClass29_0, <Foo>d__30, <>c__0 ...) that shift when a member is left
  # out of the release build (#if DEV_BUILD). Compare with every digit run replaced by '#', as multisets: each release
  # generated type needs its own dev counterpart (release subset of dev). Names that differ beyond digits still fail.
  $relGen = @($types | Where-Object { $_.StartsWith($GenPrefix) } | ForEach-Object { $_ -replace '\d+', '#' })
  $devGenBag = @{}
  foreach ($g in @($devTypes | Where-Object { $_.StartsWith($GenPrefix) } | ForEach-Object { $_ -replace '\d+', '#' })) { if ($devGenBag.ContainsKey($g)) { $devGenBag[$g]++ } else { $devGenBag[$g] = 1 } }
  $extraGen = New-Object System.Collections.Generic.List[string]
  foreach ($g in $relGen) { if ($devGenBag.ContainsKey($g) -and $devGenBag[$g] -gt 0) { $devGenBag[$g]-- } else { $extraGen.Add($g) } }
  if ($extra.Count -gt 0) { $fails.Add("item 7 release-only types: $($extra -join ', ')") }
  if ($missing.Count -gt 0) { $fails.Add("item 7 types missing from release: $($missing -join ', ')") }
  if ($extraGen.Count -gt 0) { $fails.Add("item 7 generated types not in dev: $($extraGen -join ', ')") }
}

if ($fails.Count -eq 0) { Write-Host "RELEASE OK $Mod"; exit 0 }
Write-Host "RELEASE FAIL ${Mod}: $($fails -join '; ')"
exit 1
