# Tests Invoke-UnityTests.ps1 with fake_unity.ps1 standing in for Unity: the report of each case in
# cases.txt, the arguments it gives Unity, and its defaults and errors. Prints each check that fails,
# and exits with 1 when one does.
$ErrorActionPreference = 'Stop'
$script = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..\Scripts~\Invoke-UnityTests.ps1')).Path
$project = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot 'project')).Path
$fake = Join-Path $PSScriptRoot 'fake_unity.ps1'
$tmp = (New-Item -ItemType Directory -Path (Join-Path ([IO.Path]::GetTempPath()) ([guid]::NewGuid()))).FullName
$failures = 0

# Runs a script with -File in a new Windows PowerShell started in $Folder, as a user does, and returns
# its output and then its exit code, one line each.
function Invoke-Script([string] $Path, [string[]] $Arguments, [string] $Folder = $tmp) {
    # Windows PowerShell 5.1 turns each line a native program writes to stderr into an error.
    $ErrorActionPreference = 'Continue'
    # PowerShell starts a native program in its current location.
    Push-Location -LiteralPath $Folder
    try {
        $lines = & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $Path @Arguments 2>&1 | ForEach-Object { "$_" }
    }
    finally {
        Pop-Location
    }
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

# Fails the check unless the script exits with $Code after writing $Text.
function Test-Run([string] $Name, [int] $Code, [string] $Text, [string] $Path, [string[]] $Arguments, [string] $Folder = $tmp) {
    $output = Invoke-Script $Path $Arguments $Folder
    if (-not ($output.Contains($Text) -and $output.EndsWith("exit $Code`n"))) {
        Write-Host "FAILED  $Name, expected an exit code of $Code after:`n  $Text`nWritten:`n$output"
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
    $arguments = Get-Content -LiteralPath (Join-Path $tmp 'arguments.args') |
        ForEach-Object { if ($_ -eq $project) { 'PROJECT' } else { $_.Replace("$results\", 'RESULTS/') } }
    Test-Output 'arguments' (Read-Expected 'arguments') (($arguments -join "`n") + "`n")

    $env:FAKE_ARGS = Join-Path $tmp 'given.args'
    Invoke-Script $script '-ProjectPath', $project, '-UnityPath', $fake, '-TestMode', 'EditMode', '-ResultsPath', (Join-Path $tmp 'given'), '-AssemblyNames', 'One;Two' | Out-Null
    $env:FAKE_ARGS = $null
    $given = @(Get-Content -LiteralPath (Join-Path $tmp 'given.args'))
    Test-Output 'assembly-names' "One;Two`n" ($given[[array]::IndexOf($given, '-assemblyNames') + 1] + "`n")

    # A copy of the script inside a project finds that project, and the Unity version it names. No
    # Unity has that version, so a real one never starts.
    $copy = Join-Path $tmp 'copy'
    Copy-Item -LiteralPath $project -Destination $copy -Recurse
    Set-Content -LiteralPath (Join-Path $copy 'ProjectSettings\ProjectVersion.txt') -Value 'm_EditorVersion: 0.0.0f0'
    New-Item -ItemType Directory -Path (Join-Path $copy 'Assets\Scripts'), (Join-Path $tmp 'outside') | Out-Null
    Copy-Item -LiteralPath $script -Destination (Join-Path $copy 'Assets\Scripts')
    Copy-Item -LiteralPath $script -Destination (Join-Path $tmp 'outside')
    $noUnity = "Unity not found at $env:ProgramFiles\Unity\Hub\Editor\0.0.0f0\Editor\Unity.exe. Pass -UnityPath."
    Test-Run 'defaults' 1 $noUnity (Join-Path $copy 'Assets\Scripts\Invoke-UnityTests.ps1') @()
    # A copy outside any project looks in the current folder instead. Both checks give it the fake
    # Unity, so finding the wrong project can't start a real one.
    $outside = Join-Path $tmp 'outside\Invoke-UnityTests.ps1'
    $env:FAKE_ARGS = Join-Path $tmp 'current.args'
    Invoke-Script $outside '-UnityPath', $fake, '-TestMode', 'EditMode', '-ResultsPath', (Join-Path $tmp 'current') $copy | Out-Null
    $env:FAKE_ARGS = $null
    $current = @(Get-Content -LiteralPath (Join-Path $tmp 'current.args') -ErrorAction SilentlyContinue)
    Test-Output 'current-folder' "$copy`n" ($current[[array]::IndexOf($current, '-projectPath') + 1] + "`n")
    Test-Run 'no-project' 1 'No Unity project found above this script or the current folder. Pass -ProjectPath.' $outside '-UnityPath', $fake (Join-Path $tmp 'outside')
    Test-Run 'missing-project' 1 'PathNotFound' $script '-ProjectPath', (Join-Path $tmp 'missing'), '-UnityPath', $fake
}
finally {
    Remove-Item -LiteralPath $tmp -Recurse -Force
}

if ($failures) {
    Write-Host "$failures checks failed"
    exit 1
}
Write-Host 'All checks passed'
