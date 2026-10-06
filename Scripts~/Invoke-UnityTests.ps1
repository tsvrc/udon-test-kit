<#
.SYNOPSIS
    Runs a Unity project's EditMode/PlayMode tests and prints a short pass/fail report.

.DESCRIPTION
    Wraps Unity's batchmode test runner. Finds the Unity Editor and the project on its own
    when it can, asks for whatever it can't find, and lets every guess be overridden with a
    parameter. Not tied to any particular project: point -ProjectPath at whichever Unity
    project you want tested.

    Only failures, inconclusive and skipped tests print by default, each with its message;
    passes are just counted.
    Pass -Verbose to see every test.

.PARAMETER ProjectPath
    Unity project to test. Defaults to the nearest ancestor folder that has a ProjectSettings
    folder, searching from this script's own location first, then the current directory.

.PARAMETER UnityPath
    Path to Unity.exe. Defaults to the version pinned in the project's ProjectVersion.txt,
    located under Unity Hub's default install folder, or the registry as a fallback.

.PARAMETER TestMode
    EditMode, PlayMode, or All. Defaults to All.

.PARAMETER AssemblyNames
    Passed straight to Unity's -assemblyNames, to run only those test assemblies (separate
    several with ";"). Defaults to the project's own tests: every assembly definition under
    Assets that references Unity Test Framework or NUnit, plus Assembly-CSharp-Editor. Tests
    that come with packages, such as the VRChat SDK's, don't run unless named here.

.PARAMETER ResultsPath
    Folder to write the NUnit XML results into. Defaults to a timestamped folder under $env:TEMP.

.EXAMPLE
    .\Invoke-UnityTests.ps1 -ProjectPath C:\path\to\a\unity\project -TestMode EditMode -AssemblyNames "Some.Tests.EditMode"
#>
[CmdletBinding()]
param(
    [string] $ProjectPath,
    [string] $UnityPath,
    [ValidateSet('EditMode', 'PlayMode', 'All')]
    [string] $TestMode = 'All',
    [string] $AssemblyNames,
    [string] $ResultsPath
)

$ErrorActionPreference = 'Stop'

function Find-ProjectPath {
    param([string] $StartDir)
    $dir = $StartDir
    while ($dir) {
        if (Test-Path (Join-Path $dir 'ProjectSettings')) { return $dir }
        $parent = Split-Path $dir -Parent
        if (-not $parent -or $parent -eq $dir) { break }
        $dir = $parent
    }
    return $null
}

function Get-RequiredUnityVersion([string] $projectPath) {
    $versionFile = Join-Path $projectPath 'ProjectSettings\ProjectVersion.txt'
    if (-not (Test-Path $versionFile)) { return $null }
    $match = Select-String -Path $versionFile -Pattern '^m_EditorVersion:\s*(\S+)'
    if (-not $match) { return $null }
    return $match.Matches[0].Groups[1].Value
}

function Find-UnityEditor([string] $version) {
    $hubRoot = Join-Path $env:ProgramFiles 'Unity\Hub\Editor'
    if ($version) {
        $exact = Join-Path $hubRoot "$version\Editor\Unity.exe"
        if (Test-Path $exact) { return $exact }
    }
    if (Test-Path $hubRoot) {
        $newest = Get-ChildItem $hubRoot -Directory | Sort-Object Name -Descending | Select-Object -First 1
        if ($newest) {
            $candidate = Join-Path $newest.FullName 'Editor\Unity.exe'
            if (Test-Path $candidate) { return $candidate }
        }
    }
    $installerKey = 'HKCU:\Software\Unity Technologies\Installer\Unity'
    if (Test-Path $installerKey) {
        $installDir = (Get-ItemProperty $installerKey -ErrorAction SilentlyContinue).'Location x64'
        if ($installDir) {
            $candidate = Join-Path $installDir 'Unity.exe'
            if (Test-Path $candidate) { return $candidate }
        }
    }
    return $null
}

# The project's own test assemblies: assembly definitions under Assets that reference Unity Test
# Framework or NUnit, by name or by the Test Runner assemblies' GUIDs, plus Assembly-CSharp-Editor
# for tests kept in Editor folders. Packages' tests, such as the VRChat SDK's, are left out.
function Find-ProjectTestAssemblies([string] $projectPath) {
    $testReferences = @(
        'UnityEngine.TestRunner', 'UnityEditor.TestRunner', 'nunit.framework.dll'
        'GUID:27619889b8ba8c24980f49ee34dbb44a', 'GUID:0acc523941302664db1f4e527237feb3'
    )
    $names = @('Assembly-CSharp-Editor')
    foreach ($file in Get-ChildItem (Join-Path $projectPath 'Assets') -Recurse -Filter '*.asmdef') {
        $definition = Get-Content $file.FullName -Raw | ConvertFrom-Json
        $references = @($definition.references) + @($definition.precompiledReferences)
        if ($references | Where-Object { $testReferences -contains $_ }) { $names += $definition.name }
    }
    return $names
}

if (-not $ProjectPath) {
    # Prefer walking up from the script's own location: when this package is embedded in a host
    # project, that's the project you almost always mean, regardless of the caller's cwd.
    $ProjectPath = Find-ProjectPath $PSScriptRoot
    if (-not $ProjectPath) { $ProjectPath = Find-ProjectPath (Get-Location).Path }
    if (-not $ProjectPath) {
        throw 'No Unity project found above the script or the current directory (no ProjectSettings folder). Pass -ProjectPath.'
    }
}
$ProjectPath = (Resolve-Path $ProjectPath).Path

if (-not $UnityPath) {
    $requiredVersion = Get-RequiredUnityVersion $ProjectPath
    $UnityPath = Find-UnityEditor $requiredVersion
}
if (-not $UnityPath -or -not (Test-Path $UnityPath)) {
    if ([Environment]::UserInteractive) {
        $UnityPath = Read-Host 'Could not find Unity.exe. Enter its full path'
    }
    if (-not $UnityPath -or -not (Test-Path $UnityPath)) {
        throw 'Unity Editor not found. Pass -UnityPath explicitly.'
    }
}

if (-not $AssemblyNames) {
    $AssemblyNames = (Find-ProjectTestAssemblies $ProjectPath) -join ';'
}

if (-not $ResultsPath) {
    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $ResultsPath = Join-Path $env:TEMP "UnityTestResults\$stamp"
}
New-Item -ItemType Directory -Path $ResultsPath -Force | Out-Null

$modes = @('EditMode', 'PlayMode')
if ($TestMode -ne 'All') { $modes = @($TestMode) }

$totalPassed = 0
$totalFailed = 0
$totalNotRun = 0

foreach ($mode in $modes) {
    $resultsFile = Join-Path $ResultsPath "$mode.xml"
    $logFile = Join-Path $ResultsPath "$mode.log"

    $unityArgs = @(
        '-batchmode', '-nographics'
        '-projectPath', $ProjectPath
        '-runTests', '-testPlatform', $mode
        '-testResults', $resultsFile
        '-logFile', $logFile
    )
    if ($AssemblyNames) { $unityArgs += @('-assemblyNames', $AssemblyNames) }

    Write-Host "$mode" -ForegroundColor Cyan
    # Start-Process joins arguments with spaces without quoting them, so a path with a space
    # would reach Unity cut in two.
    $quotedArgs = $unityArgs | ForEach-Object { if ($_ -match '\s') { '"' + $_ + '"' } else { $_ } }
    $process = Start-Process -FilePath $UnityPath -ArgumentList $quotedArgs -NoNewWindow -PassThru
    $spinner = '|', '/', '-', '\'
    $frame = 0
    while (-not $process.HasExited) {
        Write-Host -NoNewline "`r  $($spinner[$frame % 4]) running..."
        Start-Sleep -Milliseconds 200
        $frame++
    }
    Write-Host "`r                    `r" -NoNewline

    if (-not (Test-Path $resultsFile)) {
        Write-Host "  No results were written. See the log: $logFile" -ForegroundColor Red
        $totalFailed++
        continue
    }

    $results = [xml] (Get-Content $resultsFile -Raw)
    $passed = 0
    $failed = 0
    $notRun = 0
    foreach ($testCase in $results.SelectNodes('//test-case')) {
        if ($testCase.result -eq 'Passed') {
            $passed++
            Write-Verbose "  PASS  $($testCase.fullname)"
        }
        elseif ($testCase.result -eq 'Failed') {
            $failed++
            Write-Host "  FAIL  $($testCase.fullname)" -ForegroundColor Red
            $messageNode = $testCase.SelectSingleNode('failure/message')
            if ($messageNode) {
                Write-Host "      $($messageNode.InnerText.Trim())" -ForegroundColor DarkGray
            }
        }
        else {
            # Inconclusive (an Assume didn't hold) or Skipped (ignored): the test proved nothing.
            $notRun++
            Write-Host "  $($testCase.result.ToUpper())  $($testCase.fullname)" -ForegroundColor Yellow
            $messageNode = $testCase.SelectSingleNode('reason/message')
            if ($messageNode) {
                Write-Host "      $($messageNode.InnerText.Trim())" -ForegroundColor DarkGray
            }
        }
    }

    $summaryColor = 'Green'
    if ($notRun -gt 0) { $summaryColor = 'Yellow' }
    if ($failed -gt 0) { $summaryColor = 'Red' }
    Write-Host "  $passed passed, $failed failed, $notRun inconclusive or skipped" -ForegroundColor $summaryColor
    if ($passed + $failed + $notRun -eq 0) {
        Write-Host "  No $mode tests found. Create test assemblies with Assets > Create > Udon Test Kit > Test Assemblies." -ForegroundColor Yellow
    }
    Write-Host "  Results: $resultsFile" -ForegroundColor DarkGray
    Write-Host ''

    $totalPassed += $passed
    $totalFailed += $failed
    $totalNotRun += $notRun
}

$totalColor = 'Green'
if ($totalNotRun -gt 0) { $totalColor = 'Yellow' }
if ($totalFailed -gt 0) { $totalColor = 'Red' }
Write-Host "Total: $totalPassed passed, $totalFailed failed, $totalNotRun inconclusive or skipped" -ForegroundColor $totalColor

if ($totalFailed -gt 0) { exit 1 }
exit 0
