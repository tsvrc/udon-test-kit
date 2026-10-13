<#
.SYNOPSIS
    Runs a Unity project's tests in batch mode and prints a short report.

.DESCRIPTION
    Starts Unity once per test mode with the Unity Test Framework -runTests option, then reads the
    results file Unity writes. Prints each failed, inconclusive and skipped test with its
    message, and counts the rest. Exits with 1 when a test failed or was inconclusive, or a run
    didn't finish.

    Works in any Unity project that has Unity Test Framework, with nothing else installed. Copy
    it anywhere inside the project. Close the Unity Editor first: batch mode can't open a
    project another Editor has open.

.PARAMETER ProjectPath
    Unity project to test. Defaults to the nearest folder with a ProjectSettings\ProjectVersion.txt,
    looking up from this script's folder, then from the current folder.

.PARAMETER UnityPath
    Path to Unity.exe. Defaults to the version in the project's ProjectVersion.txt, installed
    by Unity Hub in its default folder.

.PARAMETER TestMode
    EditMode, PlayMode, or All. Defaults to All.

.PARAMETER AssemblyNames
    Test assemblies to run, separated by ";". Defaults to the project's own: every assembly
    definition under Assets, and the assemblies Unity compiles scripts without one into, such
    as tests in Editor folders.

.PARAMETER ResultsPath
    Folder for each mode's results file and Unity log. Defaults to a new folder under the temp folder.

.PARAMETER UnityArguments
    More arguments for Unity, such as -buildTarget win64. Every argument the script doesn't
    recognize goes to Unity, after its own.
#>
[CmdletBinding(PositionalBinding = $false)]
param(
    [string] $ProjectPath,
    [string] $UnityPath,
    [ValidateSet('EditMode', 'PlayMode', 'All')]
    [string] $TestMode = 'All',
    [string] $AssemblyNames,
    [string] $ResultsPath = (Join-Path ([IO.Path]::GetTempPath()) "UnityTestResults\$(Get-Date -Format yyyyMMdd-HHmmss)"),
    [Parameter(ValueFromRemainingArguments)]
    [string[]] $UnityArguments
)

$ErrorActionPreference = 'Stop'

# Set here, not as the parameter's default: Windows PowerShell 5.1 leaves $PSScriptRoot empty in
# parameter defaults when a script with CmdletBinding runs with -File.
if (-not $ProjectPath) {
    foreach ($folder in $PSScriptRoot, (Get-Location).Path) {
        while ($folder -and -not (Test-Path -LiteralPath (Join-Path $folder 'ProjectSettings\ProjectVersion.txt'))) {
            $folder = Split-Path $folder -Parent
        }
        if ($folder) { $ProjectPath = $folder; break }
    }
    if (-not $ProjectPath) { throw 'No Unity project found above this script or the current folder. Pass -ProjectPath.' }
}
$ProjectPath = (Resolve-Path -LiteralPath $ProjectPath).Path
if (-not $UnityPath) {
    $version = (Get-Content -LiteralPath (Join-Path $ProjectPath 'ProjectSettings\ProjectVersion.txt') -TotalCount 1) -replace '^m_EditorVersion:\s*'
    $UnityPath = Join-Path $env:ProgramFiles "Unity\Hub\Editor\$version\Editor\Unity.exe"
}
if (-not (Test-Path -LiteralPath $UnityPath)) {
    throw "Unity not found at $UnityPath. Pass -UnityPath."
}

# Without assembly names Unity runs every test it finds, including the ones that come with packages.
if (-not $AssemblyNames) {
    $names = @(Get-ChildItem -LiteralPath (Join-Path $ProjectPath 'Assets') -Filter *.asmdef -Recurse |
        ForEach-Object { (Get-Content -LiteralPath $_.FullName -Raw | ConvertFrom-Json).name })
    $names += 'Assembly-CSharp', 'Assembly-CSharp-firstpass', 'Assembly-CSharp-Editor', 'Assembly-CSharp-Editor-firstpass'
    $AssemblyNames = $names -join ';'
}

# Absolute, because Unity resolves a relative path from the project folder and .NET from the
# process's folder, not from PowerShell's current folder.
$ResultsPath = (New-Item -ItemType Directory -Path $ResultsPath -Force).FullName

$modes = if ($TestMode -eq 'All') { 'EditMode', 'PlayMode' } else { $TestMode }
$exitCode = 0
foreach ($mode in $modes) {
    $results = Join-Path $ResultsPath "$mode.xml"
    $log = Join-Path $ResultsPath "$mode.log"
    Remove-Item -LiteralPath $results -ErrorAction Ignore

    Write-Host "$mode tests..."
    # Unity.exe is a Windows GUI program: PowerShell waits for one, and sets $LASTEXITCODE, only
    # when its output is piped.
    & $UnityPath -batchmode -nographics -projectPath $ProjectPath -logFile $log `
        -runTests -testPlatform $mode -testResults $results -assemblyNames $AssemblyNames @UnityArguments | Out-Null
    if ($LASTEXITCODE -ne 0) { $exitCode = 1 }

    if (-not (Test-Path -LiteralPath $results)) {
        Write-Host "No results were written. See $log"
        $exitCode = 1
    }
    else {
        $testCases = @(([xml][IO.File]::ReadAllText($results)).SelectNodes('//test-case'))
        foreach ($testCase in $testCases | Where-Object result -ne 'Passed') {
            Write-Host "$($testCase.result.ToUpperInvariant())  $($testCase.fullname)"
            $message = $testCase.SelectSingleNode('failure/message|reason/message')
            if ($message -and $message.InnerText.Trim()) {
                Write-Host ('    ' + ($message.InnerText.Trim() -replace "`n", "`n    "))
            }
        }
        $passed = @($testCases | Where-Object result -eq 'Passed').Count
        $failed = @($testCases | Where-Object result -eq 'Failed').Count
        Write-Host "$passed passed, $failed failed, $($testCases.Count - $passed - $failed) inconclusive or skipped"
    }
    Write-Host ''
}
exit $exitCode
