# Tests Invoke-UnityTests.ps1 with fake_unity.ps1 standing in for Unity: the report of each case in
# cases.txt, the arguments it gives Unity, and its defaults. Prints each check that fails, and exits
# with 1 when one does.
$ErrorActionPreference = 'Stop'
$script = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..\Scripts~\Invoke-UnityTests.ps1')).Path
$project = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot 'project')).Path
$fake = Join-Path $PSScriptRoot 'fake_unity.ps1'
$tmp = (New-Item -ItemType Directory -Path (Join-Path ([IO.Path]::GetTempPath()) ([guid]::NewGuid()))).FullName
$failures = 0

# Runs a script with -File in a new Windows PowerShell, as a user does, and returns its output and
# then its exit code, one line each.
function Invoke-Script([string] $Path, [string[]] $Arguments) {
    # Windows PowerShell 5.1 turns each line a native program writes to stderr into an error.
    $ErrorActionPreference = 'Continue'
    $lines =& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $Path @Arguments 2>&1 | ForEach-Object { "$_" }
    ((@($lines) + "exit $LASTEXITCODE") -join "`n") + "`n"
}

function Test-Output([string] $Name, [string] $Expected, [string] $Actual) {
    if ($Expected -cne $Actual) {
        Write-Host "FAILED  $Name, lines expected (<=) and written (=>):"
        Compare-Object ($Expected -split "`n") ($Actual -split "`n") -CaseSensitive |
            ForEach-Object { Write-Host "  $($_.SideIndicator) $($_.InputObject)" }
        $script:failures++
    }
}

function Read-Expected([string] $Name) {
    [IO.File]::ReadAllText((Join-Path $PSScriptRoot "expected\$Name.txt"))
}

try {
    foreach ($line in Get-Content -LiteralPath (Join-Path $PSScriptRoot 'cases.txt')) {
        if (-not $line -or $line.StartsWith('#')) { continue }
        $name, $xml, $code, $mode = $line -split ' '
        $results = (New-Item -ItemType Directory -Path (Join-Path $tmp $name)).FullName
        $env:FAKE_XML = if ($xml -ne '-') { Join-Path $PSScriptRoot $xml }
        $env:FAKE_EXIT = $code
        $output = Invoke-Script $script '-ProjectPath', $project, '-UnityPath', $fake, '-TestMode', $mode, '-ResultsPath', $results
        Test-Output $name (Read-Expected $name) $output.Replace("$results\", 'RESULTS/')
    }

    $results = (New-Item -ItemType Directory -Path (Join-Path $tmp 'arguments')).FullName
    $env:FAKE_XML = $null
    $env:FAKE_EXIT = '0'
    $env:FAKE_ARGS = Join-Path $tmp 'arguments.args'
    Invoke-Script $script '-ProjectPath', $project, '-UnityPath', $fake, '-TestMode', 'EditMode', '-ResultsPath', $results, '-buildTarget', 'win64' | Out-Null
    $env:FAKE_ARGS = $null
    $arguments = Get-Content -LiteralPath (Join-Path $tmp 'arguments.args') |
        ForEach-Object { if ($_ -eq $project) { 'PROJECT' } else { $_.Replace("$results\", 'RESULTS/') } }
    Test-Output 'arguments' (Read-Expected 'arguments') (($arguments -join "`n") + "`n")

    # A copy of the script inside a project finds that project, and the Unity version it names. No
    # Unity has that version, so a real one never starts.
    $copy = Join-Path $tmp 'copy'
    Copy-Item -LiteralPath $project -Destination $copy -Recurse
    Set-Content -LiteralPath (Join-Path $copy 'ProjectSettings\ProjectVersion.txt') -Value 'm_EditorVersion: 0.0.0f0'
    New-Item -ItemType Directory -Path (Join-Path $copy 'Assets\Scripts') | Out-Null
    Copy-Item -LiteralPath $script -Destination (Join-Path $copy 'Assets\Scripts')
    $output = Invoke-Script (Join-Path $copy 'Assets\Scripts\Invoke-UnityTests.ps1') @()
    $missing = "Unity not found at $env:ProgramFiles\Unity\Hub\Editor\0.0.0f0\Editor\Unity.exe. Pass -UnityPath."
    if (-not ($output.Contains($missing) -and $output.EndsWith("exit 1`n"))) {
        Write-Host "FAILED  defaults`nExpected an exit code of 1 after:`n$missing`nActual:`n$output"
        $failures++
    }
}
finally {
    Remove-Item -LiteralPath $tmp -Recurse -Force
}

if ($failures) {
    Write-Host "$failures checks failed"
    exit 1
}
Write-Host 'All checks passed'
# Run in the caller's session, as the Scripts workflow does, the test would otherwise leave the
# exit code of the last runner it started in $LASTEXITCODE.
exit 0
