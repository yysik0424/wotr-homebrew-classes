function Read-BundleMetadata([string]$Root, [string]$Version) {
  $metadata = [IO.File]::ReadAllText((Join-Path $Root 'src\Loader\loader.json')) | ConvertFrom-Json
  if ($Version -cnotmatch '^\d+\.\d+\.\d+(?:-[A-Za-z0-9.-]+)?$' -or $metadata.Version -cne $Version) {
    throw "Requested version '$Version' differs from the release version '$($metadata.Version)' or is invalid."
  }
  if ($metadata.Id -cne 'HomebrewClasses') { throw "Unexpected bundle ID '$($metadata.Id)'." }
  return $metadata
}

function Assert-EmptyBundleDestination([string]$Path) {
  if ((Test-Path -LiteralPath $Path) -and
      (-not [IO.Directory]::Exists($Path) -or @(Get-ChildItem -LiteralPath $Path -Force).Count -gt 0)) {
    throw "Package output must be a new or empty directory: $Path"
  }
}

function Write-ClassBundle {
  param([string]$Root, $Metadata, [string]$OutDir, [string]$ReleaseDir, [string]$DevDir)
  $ErrorActionPreference = 'Stop'
  $id = $Metadata.Id
  $ledger = @(Read-ModLedger -Root $Root)
  $loaderDir = Join-Path $ReleaseDir $id
  $infoPath = Join-Path $loaderDir 'Info.json'
  $verifiedHashes = @{}
  $verifiedHashes["$id/Info.json"] = (Get-FileHash -LiteralPath $infoPath).Hash
  $infoText = [IO.File]::ReadAllText($infoPath)
  $info = $infoText | ConvertFrom-Json
  $fields = @('Id','Version','EntryMethod','AssemblyName','DisplayName','ManagerVersion','Author','HomePage','Repository')
  $allowedFields = $fields + 'LoadAfter'
  foreach ($field in $info.PSObject.Properties.Name) {
    if ($allowedFields -cnotcontains $field) { throw "Unsupported Info.json field: $field" }
  }
  $patterns = [IO.File]::ReadAllLines((Join-Path $Root 'tools\forbidden-terms.txt')) | Where-Object { $_.Trim() -and -not $_.TrimStart().StartsWith('#') }
  foreach ($pattern in $patterns) {
    if ($infoText -cmatch $pattern -or @($info.PSObject.Properties.Value -cmatch $pattern).Count) { throw 'Forbidden content in Info.json.' }
  }
  foreach ($field in $fields) {
    if (-not $Metadata.$field -or $info.$field -isnot [string] -or $info.$field -cne $Metadata.$field) { throw "Info.json mismatch: $field" }
  }
  $expectedAfter = @($(foreach ($m in $ledger) {
    foreach ($r in $m.requires) { $r.modId }
    foreach ($after in $m.optionalAfter) { $after }
  }) | Sort-Object -Unique)
  if ($info.LoadAfter -isnot [array] -or $info.LoadAfter.Count -ne $expectedAfter.Count -or
      @(Compare-Object -ReferenceObject $expectedAfter -DifferenceObject $info.LoadAfter -CaseSensitive).Count) {
    throw 'Info.json LoadAfter differs from the class ledger.'
  }
  $paths = [ordered]@{}
  $paths["$id/Info.json"] = $infoPath
  $paths["$id/$id.dll"] = Join-Path $loaderDir "$id.dll"
  $verifiedHashes["$id/$id.dll"] = (Get-FileHash -LiteralPath $paths["$id/$id.dll"]).Hash
  $verify = Join-Path $Root 'tools\verify-release.ps1'
  & $verify -Dll $paths["$id/$id.dll"] -Mod $id
  if ($LASTEXITCODE -ne 0) { throw "Release verification failed: $id" }
  foreach ($m in $ledger) {
    $name = $m.id
    $dll = Join-Path $ReleaseDir "$name\$name.dll"
    $params = @{ Dll = $dll; Mod = $name }
    if ($DevDir) { $params.DevDll = Join-Path $DevDir "$name\$name.dll" }
    $verifiedHashes["$id/classes/$name.dll"] = (Get-FileHash -LiteralPath $dll).Hash
    & $verify @params
    if ($LASTEXITCODE -ne 0) { throw "Release verification failed: $name" }
    $paths["$id/classes/$name.dll"] = $dll
  }
  foreach ($relative in $paths.Keys) {
    foreach ($pattern in $patterns) { if ($relative -cmatch $pattern) { throw "Forbidden package path: $relative" } }
  }

  Assert-EmptyBundleDestination $OutDir
  [void][IO.Directory]::CreateDirectory($OutDir)
  $lockPath = Join-Path $OutDir '.package.lock'
  $lock = [IO.File]::Open($lockPath, 'CreateNew', 'Write', 'None')
  $work = Join-Path $OutDir ('.stage-' + [Guid]::NewGuid().ToString('N'))
  $archiveName = "$id-$($Metadata.Version).zip"
  $hashes = [ordered]@{}
  $published = New-Object System.Collections.Generic.List[string]
  try {
    if (@(Get-ChildItem -Force -LiteralPath $OutDir | Where-Object { $_.Name -ne '.package.lock' }).Count) { throw "Output directory changed: $OutDir" }
    [void][IO.Directory]::CreateDirectory((Join-Path $work "$id\classes"))
    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archivePath = Join-Path $work $archiveName
    $zip = [IO.Compression.ZipFile]::Open($archivePath, 'Create')
    try {
      foreach ($relative in $paths.Keys) {
        $copy = Join-Path $work $relative
        [IO.File]::Copy($paths[$relative], $copy)
        $hashes[$relative] = (Get-FileHash -LiteralPath $copy -Algorithm SHA256).Hash
        if ($hashes[$relative] -cne $verifiedHashes[$relative]) { throw "Build output changed during verification: $relative" }
        $entry = $zip.CreateEntry($relative, [IO.Compression.CompressionLevel]::Optimal)
        # Unity Mod Manager derives its assembly cache key from the installed DLL's modification time.
        $entry.LastWriteTime = [DateTimeOffset]::new([IO.File]::GetLastWriteTime($copy))
        $sourceStream = [IO.File]::OpenRead($copy)
        try {
          $targetStream = $entry.Open()
          try { $sourceStream.CopyTo($targetStream) } finally { $targetStream.Dispose() }
        } finally { $sourceStream.Dispose() }
      }
    } finally { $zip.Dispose() }
    $zip = [IO.Compression.ZipFile]::OpenRead($archivePath)
    try {
      if ($zip.Entries.Count -ne $paths.Count) { throw 'Archive entry count mismatch.' }
      foreach ($relative in $paths.Keys) {
        $entry = $zip.GetEntry($relative)
        if ($null -eq $entry) { throw "Missing archive entry: $relative" }
        $stream = $entry.Open()
        try { $actual = (Get-FileHash -InputStream $stream -Algorithm SHA256).Hash } finally { $stream.Dispose() }
        if ($hashes[$relative] -cne $actual) { throw "Archive hash mismatch: $relative" }
      }
    } finally { $zip.Dispose() }
    $hashes[$archiveName] = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash
    $lines = foreach ($relative in $hashes.Keys) { "$($hashes[$relative])  $relative" }
    [IO.File]::WriteAllLines((Join-Path $work 'SHA256SUMS.txt'), [string[]]$lines, [Text.UTF8Encoding]::new($false))
    [IO.Directory]::Move((Join-Path $work $id), (Join-Path $OutDir $id))
    $published.Add($id)
    [IO.File]::Move((Join-Path $work 'SHA256SUMS.txt'), (Join-Path $OutDir 'SHA256SUMS.txt'))
    $published.Add('SHA256SUMS.txt')
    [IO.File]::Move($archivePath, (Join-Path $OutDir $archiveName))
    Write-Host "PACKAGE OK $archiveName SHA256 $($hashes[$archiveName])"
  } catch {
    foreach ($part in $published) {
      if ($part -ceq $id) { [IO.Directory]::Move((Join-Path $OutDir $part), (Join-Path $work $part)) }
      else { [IO.File]::Move((Join-Path $OutDir $part), (Join-Path $work $part)) }
    }
    throw
  } finally {
    $staging = [IO.Path]::GetFullPath($work)
    if ([IO.Path]::GetDirectoryName($staging) -ceq [IO.Path]::GetFullPath($OutDir).TrimEnd('\') -and
        [IO.Path]::GetFileName($staging) -cmatch '^\.stage-[a-f0-9]{32}$' -and [IO.Directory]::Exists($staging)) {
      Remove-Item -LiteralPath $staging -Force -Recurse
    }
    $lock.Dispose()
    [IO.File]::Delete($lockPath)
  }
}
