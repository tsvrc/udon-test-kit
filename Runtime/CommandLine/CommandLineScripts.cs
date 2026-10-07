using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace UdonTestKit
{
    /// <summary>
    /// Creates the scripts that run the project's tests from the command line,
    /// Invoke-UnityTests.ps1 for Windows and invoke_unity_tests.sh for Linux, in a folder of the
    /// user's choice. Each finds the project from its own location, so they keep working wherever
    /// the folder and the project are cloned together. Creating them again replaces them.
    /// </summary>
    public static class CommandLineScripts
    {
        public const string MenuPath = "Tools/Udon Test Kit/Create Command-Line Scripts";
        public const string WindowsScriptName = "Invoke-UnityTests.ps1";
        public const string LinuxScriptName = "invoke_unity_tests.sh";

        // CommandLineTestRunner refuses a script that passes another version. Raise it whenever
        // the arguments the scripts pass to Unity change.
        internal const int ScriptVersion = 1;

        [MenuItem(MenuPath + "...")]
        private static void CreateFromMenu()
        {
            string folder = EditorUtility.OpenFolderPanel("Create Command-Line Scripts", ProjectPath(), "");
            if (!string.IsNullOrEmpty(folder))
                Create(folder);
        }

        /// <summary>Writes both scripts into <paramref name="folder"/>, for this project.</summary>
        public static void Create(string folder)
        {
            Create(folder, ProjectPath());
            Debug.Log($"[UdonTestKit] Created {WindowsScriptName} and {LinuxScriptName} in {folder}.");
        }

        internal static void Create(string folder, string projectPath)
        {
            string relativePath = Path.GetRelativePath(folder, projectPath);

            // GetRelativePath returns projectPath itself when the two have different roots, such
            // as different drives, and an absolute path would break on every other machine.
            if (Path.IsPathRooted(relativePath))
                throw new ArgumentException($"The scripts must be on the same drive as the project, {projectPath}.");
            relativePath = relativePath.Replace('\\', '/');

            // Windows PowerShell 5.1 reads a script without a byte order mark as ANSI, which garbles
            // a path with non-ASCII characters. A byte order mark breaks sh.
            Write(Path.Combine(folder, WindowsScriptName), PowerShellScript,
                "'" + relativePath.Replace("'", "''") + "'", new UTF8Encoding(true));
            Write(Path.Combine(folder, LinuxScriptName), ShellScript,
                "'" + relativePath.Replace("'", "'\\''") + "'", new UTF8Encoding(false));
        }

        private static string ProjectPath()
        {
            return Path.GetDirectoryName(Application.dataPath);
        }

        private static void Write(string path, string template, string quotedProjectPath, Encoding encoding)
        {
            // This file's own line endings depend on how it was checked out, and sh fails on CRLF.
            string script = template
                .Replace("\r\n", "\n")
                .Replace("__PROJECT_PATH__", quotedProjectPath)
                .Replace("__SCRIPT_VERSION__", ScriptVersion.ToString());
            File.WriteAllText(path, script, encoding);
        }

        private const string PowerShellScript =
@"<#
.SYNOPSIS
    Runs a Unity project's tests in batch mode and prints a short report.

.DESCRIPTION
    Starts Unity once per test mode with UdonTestKit.CommandLineTestRunner, which picks the
    tests, runs them and writes the report. Close the Unity Editor first: batch mode can't
    open a project another Editor has open.

.PARAMETER ProjectPath
    Unity project to test. Defaults to the project this script was created for.

.PARAMETER UnityPath
    Path to Unity.exe. Defaults to the version in the project's ProjectVersion.txt, installed
    by Unity Hub in its default folder.

.PARAMETER TestMode
    EditMode, PlayMode, or All. Defaults to All.

.PARAMETER AssemblyNames
    Test assemblies to run, separated by "";"". Defaults to the project's own: every
    assembly with source files under Assets.

.PARAMETER ResultsPath
    Folder for each mode's report and Unity log. Defaults to a new folder under the temp folder.

.NOTES
    Created by Udon Test Kit with Tools > Udon Test Kit > Create Command-Line Scripts. Create
    it again there instead of editing it, such as when it reports another version of the kit.
#>
[CmdletBinding(PositionalBinding = $false)]
param(
    [string] $ProjectPath,
    [string] $UnityPath,
    [ValidateSet('EditMode', 'PlayMode', 'All')]
    [string] $TestMode = 'All',
    [string] $AssemblyNames,
    [string] $ResultsPath = (Join-Path ([IO.Path]::GetTempPath()) ""UnityTestResults\$(Get-Date -Format yyyyMMdd-HHmmss)"")
)

$ErrorActionPreference = 'Stop'

# Set here, not as the parameter's default: Windows PowerShell 5.1 leaves $PSScriptRoot empty in
# parameter defaults when a script with CmdletBinding runs with -File.
if (-not $ProjectPath) { $ProjectPath = Join-Path $PSScriptRoot __PROJECT_PATH__ }
$ProjectPath = (Resolve-Path $ProjectPath).Path
if (-not $UnityPath) {
    $version = (Get-Content (Join-Path $ProjectPath 'ProjectSettings\ProjectVersion.txt') -TotalCount 1) -replace '^m_EditorVersion:\s*'
    $UnityPath = Join-Path $env:ProgramFiles ""Unity\Hub\Editor\$version\Editor\Unity.exe""
}
if (-not (Test-Path $UnityPath)) {
    throw ""Unity not found at $UnityPath. Pass -UnityPath.""
}
New-Item -ItemType Directory -Path $ResultsPath -Force | Out-Null

$modes = if ($TestMode -eq 'All') { 'EditMode', 'PlayMode' } else { $TestMode }
$exitCode = 0
foreach ($mode in $modes) {
    $report = Join-Path $ResultsPath ""$mode.txt""
    $log = Join-Path $ResultsPath ""$mode.log""
    $unityArgs = @(
        '-batchmode', '-nographics', '-projectPath', $ProjectPath, '-logFile', $log
        '-executeMethod', 'UdonTestKit.CommandLineTestRunner.Run', '-udonTestKitScriptVersion', '__SCRIPT_VERSION__'
        '-testPlatform', $mode, '-testReport', $report
    )
    if ($AssemblyNames) { $unityArgs += '-assemblyNames', $AssemblyNames }

    Remove-Item $report -ErrorAction Ignore

    Write-Host ""$mode tests...""
    # Unity.exe is a Windows GUI program: PowerShell waits for one, and sets $LASTEXITCODE, only
    # when its output is piped.
    & $UnityPath $unityArgs | Out-Null
    if (Test-Path $report) { Get-Content $report } else { Write-Host ""No report was written. See $log"" }
    Write-Host ''
    if ($LASTEXITCODE -ne 0) { $exitCode = 1 }
}
exit $exitCode
";

        private const string ShellScript =
@"#!/bin/sh
# Runs a Unity project's tests in batch mode and prints a short report. Starts Unity once per
# test mode with UdonTestKit.CommandLineTestRunner, which picks the tests, runs them and writes
# the report. Close the Unity Editor first: batch mode can't open a project another Editor has open.
#
# Created by Udon Test Kit with Tools > Udon Test Kit > Create Command-Line Scripts. Create it
# again there instead of editing it, such as when it reports another version of the kit.
set -eu

usage() {
    cat <<EOF
Usage: $0 [-m mode] [-a assemblies] [-p project] [-u unity] [-r results]
  -m  EditMode, PlayMode or All; defaults to All
  -a  test assemblies to run, separated by "";""; defaults to every assembly with source files under Assets
  -p  Unity project to test; defaults to the project this script was created for
  -u  Unity executable; defaults to the project's version, installed by Unity Hub in its default folder
  -r  folder for each mode's report and Unity log; defaults to a new temp folder
EOF
}

mode=All
assemblies=
project=$(dirname ""$0"")/__PROJECT_PATH__
unity=
results=
while getopts 'm:a:p:u:r:h' option; do
    case $option in
        m) mode=$OPTARG ;;
        a) assemblies=$OPTARG ;;
        p) project=$OPTARG ;;
        u) unity=$OPTARG ;;
        r) results=$OPTARG ;;
        h) usage; exit 0 ;;
        *) usage >&2; exit 2 ;;
    esac
done
shift $((OPTIND - 1))
if [ $# -gt 0 ]; then
    usage >&2
    exit 2
fi

case $mode in
    All) modes=""EditMode PlayMode"" ;;
    EditMode | PlayMode) modes=$mode ;;
    *) echo ""$0: invalid mode: $mode"" >&2; usage >&2; exit 2 ;;
esac

project=$(cd ""$project"" && pwd)
if [ -z ""$unity"" ]; then
    # ProjectVersion.txt keeps Windows line endings when the project was saved on Windows.
    version=$(sed -n 's/^m_EditorVersion: *//p' ""$project/ProjectSettings/ProjectVersion.txt"" | tr -d '\r')
    unity=$HOME/Unity/Hub/Editor/$version/Editor/Unity
fi
if [ ! -x ""$unity"" ]; then
    echo ""$0: Unity not found at $unity. Pass -u."" >&2
    exit 1
fi
results=${results:-$(mktemp -d)}
mkdir -p ""$results""

status=0
for mode in $modes; do
    report=$results/$mode.txt
    log=$results/$mode.log
    rm -f ""$report""

    echo ""$mode tests...""
    ""$unity"" -batchmode -nographics -projectPath ""$project"" -logFile ""$log"" \
        -executeMethod UdonTestKit.CommandLineTestRunner.Run -udonTestKitScriptVersion __SCRIPT_VERSION__ \
        -testPlatform ""$mode"" -testReport ""$report"" \
        ${assemblies:+-assemblyNames ""$assemblies""} || status=1
    if [ -f ""$report"" ]; then cat ""$report""; else echo ""No report was written. See $log""; fi
    echo
done
exit $status
";
    }
}
