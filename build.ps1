[CmdletBinding()]
param(
    [switch]$Test,
    [switch]$Package
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$outputDirectory = Join-Path $projectRoot 'dist'
$manifest = Join-Path $projectRoot 'TouchPlayer.manifest'
$source = Join-Path $projectRoot 'TouchPlayer.cs'
$testSource = Join-Path $projectRoot 'playlist_formats_test.cs'

if (-not (Test-Path -LiteralPath $compiler)) {
    throw 'Der 64-Bit-C#-Compiler des .NET Frameworks wurde nicht gefunden. Installiere das .NET Framework 4.8 Developer Pack.'
}

New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
$outputExe = Join-Path $outputDirectory 'Surface Touch Mediaplayer.exe'

& $compiler /nologo /target:winexe /platform:x64 "/out:$outputExe" "/win32manifest:$manifest" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Xml.dll $source
if ($LASTEXITCODE -ne 0) { throw 'Der Release-Build ist fehlgeschlagen.' }

if ($Test) {
    $testExe = Join-Path $outputDirectory 'playlist_formats_test.exe'
    & $compiler /nologo /target:exe /platform:x64 "/out:$testExe" /main:PlaylistFormatsTest /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Xml.dll $source $testSource
    if ($LASTEXITCODE -ne 0) { throw 'Der Test-Build ist fehlgeschlagen.' }
    & $testExe
    if ($LASTEXITCODE -ne 0) { throw 'Die Funktionstests sind fehlgeschlagen.' }
    Remove-Item -LiteralPath $testExe -Force
    $testLog = Join-Path $outputDirectory 'TouchPlayer.log'
    if (Test-Path -LiteralPath $testLog) { Remove-Item -LiteralPath $testLog -Force }
}

if ($Package) {
    $archive = Join-Path $outputDirectory 'Surface-Touch-Mediaplayer-1.0.0-win-x64.zip'
    $files = @($outputExe, (Join-Path $projectRoot 'README.md'))
    if (Test-Path -LiteralPath (Join-Path $projectRoot 'LICENSE')) { $files += Join-Path $projectRoot 'LICENSE' }
    Compress-Archive -LiteralPath $files -DestinationPath $archive -Force
    Write-Output "Release-Archiv: $archive"
}

Get-FileHash -Algorithm SHA256 -LiteralPath $outputExe
