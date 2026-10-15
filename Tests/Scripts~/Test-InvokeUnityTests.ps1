# Tests Invoke-UnityTests.ps1 with fake_unity.ps1 standing in for Unity: the report of each case in
# cases.txt, the arguments it gives Unity, and its defaults and errors. Prints each check that fails,
# and exits with 1 when one does.
$ErrorActionPreference = 'Stop'
$script = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..\Scripts~\Invoke-UnityTests.ps1')).Path
$project = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot 'project')).Path
$fake = Join-Path $PSScriptRoot 'fake_unity.ps1'
$tmp = (New-Item -ItemType Directory -Path (Join-Path ([IO.Path]::GetTempPath()) ([guid]::NewGuid()))).FullName
$assemblyCSharp = 'Assembly-CSharp;Assembly-CSharp-firstpass;Assembly-CSharp-Editor;Assembly-CSharp-Editor-firstpass'
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

# Fails the check unless the script exits with $Code after writing $Text, without starting Unity.
function Test-Run([string] $Name, [int] $Code, [string] $Text, [string] $Path, [string[]] $Arguments, [string] $Folder = $tmp) {
    # The fake Unity writes this file if the script starts it.
    $started = Join-Path $tmp "$Name.started"
    $env:FAKE_ARGS = $started
    try {
        $output = Invoke-Script $Path $Arguments $Folder
    }
    finally {
        $env:FAKE_ARGS = $null
    }
    if (-not ($output.Contains($Text) -and $output.EndsWith("exit $Code`n")) -or (Test-Path -LiteralPath $started)) {
        Write-Host "FAILED  $Name, expected an exit code of $Code after:`n  $Text`nwithout starting Unity. Written:`n$output"
        $script:failures++
    }
}

# Fails the check unless the fake Unity gets $Expected after its $Argument argument.
function Test-UnityArgument([string] $Name, [string] $Argument, [string] $Expected, [string] $Path, [string[]] $Arguments, [string] $Folder = $tmp) {
    $env:FAKE_ARGS = Join-Path $tmp "$Name.args"
    try {
        Invoke-Script $Path $Arguments $Folder | Out-Null
    }
    finally {
        $env:FAKE_ARGS = $null
    }
    $recorded = @(Get-Content -LiteralPath (Join-Path $tmp "$Name.args") -ErrorAction SilentlyContinue)
    Test-Output $Name "$Expected`n" ($recorded[[array]::IndexOf($recorded, $Argument) + 1] + "`n")
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
        $arguments = '-ProjectPath', $project, '-UnityPath', $fake, '-ResultsPath', $results
        if ($mode -ne '-') { $arguments += '-TestMode', $mode }
        $output = Invoke-Script $script $arguments
        Test-Output $name (Read-Expected $name) $output.Replace("$results\", 'RESULTS/')
    }

    $results = (New-Item -ItemType Directory -Path (Join-Path $tmp 'arguments')).FullName
    $env:FAKE_XML = $null
    $env:FAKE_EXIT = '0'
    $env:FAKE_ARGS = Join-Path $tmp 'arguments.args'
    Invoke-Script $script '-ProjectPath', $project, '-UnityPath', $fake, '-TestMode', 'EditMode', '-ResultsPath', $results, '-buildTarget', 'win64', 'extra' | Out-Null
    $env:FAKE_ARGS = $null
    $arguments = Get-Content -LiteralPath (Join-Path $tmp 'arguments.args') |
        ForEach-Object { if ($_ -eq $project) { 'PROJECT' } else { $_.Replace("$results\", 'RESULTS/') } }
    Test-Output 'arguments' (Read-Expected 'arguments') (($arguments -join "`n") + "`n")

    Test-UnityArgument 'assembly-names' '-assemblyNames' 'One;Two' $script '-ProjectPath', $project, '-UnityPath', $fake, '-TestMode', 'EditMode', '-ResultsPath', (Join-Path $tmp 'given'), '-AssemblyNames', 'One;Two'
    # Unity gets the project and results paths as absolute ones.
    Test-UnityArgument 'relative-project' '-projectPath' $project $script '-ProjectPath', 'project', '-UnityPath', $fake, '-TestMode', 'EditMode', '-ResultsPath', (Join-Path $tmp 'relative') $PSScriptRoot
    Test-UnityArgument 'relative-results' '-testResults' "$tmp\relative-results\EditMode.xml" $script '-ProjectPath', $project, '-UnityPath', $fake, '-TestMode', 'EditMode', '-ResultsPath', 'relative-results'

    # A project without assembly definitions runs the Assembly-CSharp ones.
    $bare = Join-Path $tmp 'bare'
    New-Item -ItemType Directory -Path (Join-Path $bare 'Assets') | Out-Null
    Test-UnityArgument 'no-assembly-definitions' '-assemblyNames' $assemblyCSharp $script '-ProjectPath', $bare, '-UnityPath', $fake, '-TestMode', 'EditMode', '-ResultsPath', (Join-Path $tmp 'bare-results')

    # Unity skips a folder with the Hidden attribute, but imports a file with it, in Assets too.
    $hidden = Join-Path $tmp 'hidden'
    Copy-Item -LiteralPath $project -Destination $hidden -Recurse
    Set-Content -LiteralPath (Join-Path $hidden 'Assets\Root.asmdef') -Value '{ "name": "Root" }'
    (Get-Item -LiteralPath (Join-Path $hidden 'Assets\Root.asmdef')).Attributes = 'Hidden'
    (Get-Item -LiteralPath (Join-Path $hidden 'Assets\A')).Attributes = 'Directory, Hidden'
    (Get-Item -LiteralPath (Join-Path $hidden 'Assets\With Space\Spaced.asmdef')).Attributes = 'Hidden'
    Test-UnityArgument 'hidden-attribute' '-assemblyNames' "Root;Beta;Spaced.Bom;$assemblyCSharp" $script '-ProjectPath', $hidden, '-UnityPath', $fake, '-TestMode', 'EditMode', '-ResultsPath', (Join-Path $tmp 'hidden-results')

    # Without -ResultsPath, the results go to a new folder in the temp folder, which a new process
    # reads from TMP.
    $temp = (New-Item -ItemType Directory -Path (Join-Path $tmp 'temp')).FullName
    $previousTemp = $env:TMP
    $env:TMP = $temp
    $env:FAKE_ARGS = Join-Path $tmp 'default.args'
    try {
        Invoke-Script $script '-ProjectPath', $project, '-UnityPath', $fake, '-TestMode', 'EditMode' | Out-Null
    }
    finally {
        $env:TMP = $previousTemp
        $env:FAKE_ARGS = $null
    }
    $default = @(Get-Content -LiteralPath (Join-Path $tmp 'default.args') -ErrorAction SilentlyContinue)
    $recorded = $default[[array]::IndexOf($default, '-testResults') + 1] -replace '\\\d{8}-\d{6}\\', '\DATE\'
    Test-Output 'results-default' "$temp\UnityTestResults\DATE\EditMode.xml`n" ($recorded + "`n")

    # A results file an earlier run left is removed first, so a run that writes none doesn't report
    # the old one.
    $stale = (New-Item -ItemType Directory -Path (Join-Path $tmp 'stale')).FullName
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'results-passed.xml') -Destination (Join-Path $stale 'EditMode.xml')
    $env:FAKE_XML = $null
    $output = Invoke-Script $script '-ProjectPath', $project, '-UnityPath', $fake, '-TestMode', 'EditMode', '-ResultsPath', $stale
    Test-Output 'stale-results' (Read-Expected 'no-results') $output.Replace("$stale\", 'RESULTS/')

    Set-Content -LiteralPath (Join-Path $tmp 'file') -Value ''
    foreach ($path in (Join-Path $tmp 'file'), (Join-Path $tmp 'file\results')) {
        Test-Run 'results-folder' 1 "Can't create the results folder $path" $script '-ProjectPath', $project, '-UnityPath', $fake, '-ResultsPath', $path
    }

    # A copy of the script inside a project finds that project, past a ProjectSettings folder
    # without a version file, and the Unity version on the first line of its version file. No Unity
    # has that version, so a real one never starts.
    $copy = Join-Path $tmp 'copy'
    Copy-Item -LiteralPath $project -Destination $copy -Recurse
    Set-Content -LiteralPath (Join-Path $copy 'ProjectSettings\ProjectVersion.txt') -Value 'm_EditorVersion: 0.0.0f0', 'm_EditorVersionWithRevision: 0.0.0f0'
    New-Item -ItemType Directory -Path (Join-Path $copy 'Assets\Scripts\ProjectSettings'), (Join-Path $tmp 'outside') | Out-Null
    Copy-Item -LiteralPath $script -Destination (Join-Path $copy 'Assets\Scripts')
    Copy-Item -LiteralPath $script -Destination (Join-Path $tmp 'outside')
    $inside = Join-Path $copy 'Assets\Scripts\Invoke-UnityTests.ps1'
    $noUnity = "Unity not found at $env:ProgramFiles\Unity\Hub\Editor\0.0.0f0\Editor\Unity.exe. Pass -UnityPath."
    Test-Run 'defaults' 1 $noUnity $inside @()
    # The project around the script comes before the current folder's.
    Test-UnityArgument 'script-folder-first' '-projectPath' $copy $inside '-UnityPath', $fake, '-TestMode', 'EditMode', '-ResultsPath', (Join-Path $tmp 'first') $project
    # A copy outside any project looks in the current folder instead. These checks give it the fake
    # Unity, so finding the wrong project can't start a real one.
    $outside = Join-Path $tmp 'outside\Invoke-UnityTests.ps1'
    Test-UnityArgument 'current-folder' '-projectPath' $copy $outside '-UnityPath', $fake, '-TestMode', 'EditMode', '-ResultsPath', (Join-Path $tmp 'current') $copy
    Test-Run 'no-project' 1 'No Unity project found above this script or the current folder. Pass -ProjectPath.' $outside '-UnityPath', $fake (Join-Path $tmp 'outside')
    Test-Run 'missing-project' 1 'PathNotFound' $script '-ProjectPath', (Join-Path $tmp 'missing'), '-UnityPath', $fake
    Test-Run 'test-mode' 1 'ParameterArgumentValidationError' $script '-ProjectPath', $project, '-UnityPath', $fake, '-TestMode', 'Both'
}
finally {
    Remove-Item -LiteralPath $tmp -Recurse -Force
}

if ($failures) {
    Write-Host "$failures checks failed"
    exit 1
}
Write-Host 'All checks passed'
