param([Parameter(Mandatory)][string]$Path)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2
$rulesFile = Join-Path $PSScriptRoot 'forbidden-terms.txt'
$utf8 = New-Object Text.UTF8Encoding($false, $true)
$rules = @([IO.File]::ReadAllLines($rulesFile, $utf8) | Where-Object { $_.Trim() -and -not $_.TrimStart().StartsWith('#') })
if ($rules.Count -eq 0) { throw 'The public content policy is empty.' }
$pattern = [regex]::new((($rules | ForEach-Object { '(?:' + $_ + ')' }) -join '|'))
# Samples are assembled so that the scanner can inspect its own source using the same rules.
foreach ($badSample in @('person' + '@' + 'example.org'; 'C:' + '/Users/' + 'example'; 'App' + 'Data/Roaming/')) {
    if (-not $pattern.IsMatch($badSample)) { throw 'Public scan failed its positive control.' }
}
foreach ($goodSample in @('DarkCodex', 'standard action', 'IL_00B0', 'yysik0424')) {
    if ($pattern.IsMatch($goodSample)) { throw "Public scan rejected an allowed example: $goodSample" }
}
$target = Get-Item -LiteralPath $Path
$script:issues = 0
function Check-Line([string]$location, [string]$line) {
    if ($pattern.IsMatch($line)) {
        $script:issues++
        if ($script:issues -le 40) { Write-Host "Public content match: $location" }
    }
}
function Scan-File([string]$name, [string]$physical) {
    $extension = [IO.Path]::GetExtension($name).ToLowerInvariant()
    if ($extension -eq '.dll') {
        . (Join-Path $PSScriptRoot 'lib\toolchain.ps1')
        foreach ($s in @(Invoke-IlScan @('strings', $physical))) { Check-Line $name $s }
    } elseif ($extension -eq '.zip') {
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        $archive = [IO.Compression.ZipFile]::OpenRead($physical)
        try {
            foreach ($entry in $archive.Entries) {
                $child = $entry.FullName
                Check-Line "$name/$child" $child
                if ($child.EndsWith('/')) { continue }
                if ([IO.Path]::GetExtension($child) -ieq '.zip') { throw "Unexpected nested zip in $name" }
                $stream = $entry.Open()
                try {
                    if ([IO.Path]::GetExtension($child) -ieq '.dll') {
                        $scratch = [IO.Path]::Combine([IO.Path]::GetTempPath(), ([guid]::NewGuid().ToString('N') + '.dll'))
                        try {
                            $dst = [IO.File]::Open($scratch, [IO.FileMode]::CreateNew)
                            try { $stream.CopyTo($dst) } finally { $dst.Dispose() }
                            Scan-File "$name/$child" $scratch
                        } finally { if ([IO.File]::Exists($scratch)) { [IO.File]::Delete($scratch) } }
                    } else {
                        $reader = New-Object IO.StreamReader($stream,$utf8)
                        try { $n=0; while (($line=$reader.ReadLine()) -ne $null) { $n++; Check-Line "$name/$child ($n)" $line } } finally { $reader.Dispose() }
                    }
                } finally { $stream.Dispose() }
            }
        } finally { $archive.Dispose() }
    } else {
        $n = 0
        foreach ($s in [IO.File]::ReadLines($physical, $utf8)) { $n++; Check-Line "$name ($n)" $s }
    }
}
if ($target.PSIsContainer) {
    $base = $target.FullName.TrimEnd('\') + '\'
    $policy = Join-Path $base 'tools\forbidden-terms.txt'
    foreach ($item in Get-ChildItem -LiteralPath $target.FullName -Recurse -Force) {
        if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw "Unexpected link: $($item.Name)" }
        $relative = $item.FullName.Substring($base.Length)
        Check-Line $relative $relative
        if ($item.PSIsContainer) { continue }
        if ($item.FullName -ieq $policy -and (Get-FileHash -LiteralPath $policy).Hash -ceq (Get-FileHash -LiteralPath $rulesFile).Hash) { continue }
        Scan-File $relative $item.FullName
    }
} else {
    Check-Line $target.Name $target.Name
    if ($target.FullName -ine $rulesFile) { Scan-File $target.Name $target.FullName }
}
if ($script:issues) { throw "Public content scan found $script:issues matches." }
Write-Host 'SCAN OK'
