#!/bin/bash
#
# Runs the tests of a Unity project in batch mode and prints a short report,
# the Linux counterpart of Invoke-UnityTests.ps1. Run it with --help for the
# options.

# ** matches every folder level, except hidden folders, which Unity skips too.
shopt -s globstar nullglob

readonly SCRIPT_NAME="${0##*/}"
readonly ASSEMBLY_CSHARP_NAMES="Assembly-CSharp;Assembly-CSharp-firstpass;\
Assembly-CSharp-Editor;Assembly-CSharp-Editor-firstpass"

usage() {
  cat <<EOF
Usage: ${SCRIPT_NAME} [options] [-- unity-arguments]

Starts Unity once per test mode with the Unity Test Framework -runTests
option, then reads the results file Unity writes. Prints each failed,
inconclusive and skipped test with its message, and counts the rest. Exits
with 1 when a test failed or was inconclusive, or a run didn't finish, and
with 2 when the options are wrong.

Works in any Unity project that has Unity Test Framework, with nothing else
installed. Copy it anywhere inside the project. Close the Unity Editor first:
batch mode can't open a project another Editor has open.

Options:
  -p, --project-path PATH
      Unity project to test. Defaults to the nearest folder with a
      ProjectSettings/ProjectVersion.txt, looking up from this script's
      folder, then from the current folder.
  -u, --unity-path PATH
      Path to the Unity executable. Defaults to the version in the project's
      ProjectVersion.txt, installed by Unity Hub in its default folder,
      ~/Unity/Hub/Editor.
  -m, --test-mode MODE
      EditMode, PlayMode, or All. Defaults to All.
  -a, --assembly-names NAMES
      Test assemblies to run, separated by ";". Defaults to the project's
      own: every assembly definition Unity imports under Assets, and the
      assemblies Unity compiles scripts without one into, such as tests in
      Editor folders.
  -r, --results-path PATH
      Folder for each mode's results file and Unity log. Defaults to a new
      folder under the temp folder.
  -h, --help
      Prints this help.
  -- UNITY-ARGUMENTS
      More arguments for Unity, such as -- -buildTarget win64. Everything
      after -- goes to Unity, after the script's own.
EOF
}

err() {
  echo "${SCRIPT_NAME}: $*" >&2
}

usage_error() {
  err "$*"
  echo "Try '${SCRIPT_NAME} --help'." >&2
  exit 2
}

# Exits with a usage error when the option in $1 has no value after it.
require_value() {
  if (( $# < 2 )); then
    usage_error "option $1 needs a value"
  fi
}

#######################################
# Finds the nearest folder with a ProjectSettings/ProjectVersion.txt at or
# above a folder.
# Arguments:
#   Folder to start from.
# Outputs:
#   Writes the project folder to stdout.
# Returns:
#   1 when there's no project above the folder.
#######################################
find_project() {
  local folder="$1"
  while [[ ! -f "${folder}/ProjectSettings/ProjectVersion.txt" ]]; do
    if [[ "${folder}" == "/" ]]; then
      return 1
    fi
    folder="$(dirname "${folder}")"
  done
  echo "${folder}"
}

#######################################
# Lists the project's own assemblies: the name of every assembly definition
# Unity imports under Assets, then the Assembly-CSharp ones.
# Arguments:
#   Project folder.
# Outputs:
#   Writes the names to stdout, separated by ";".
#######################################
default_assembly_names() {
  # Unity writes the assembly name as the first "name" key of the file.
  local -r name_regex='"name"[[:space:]]*:[[:space:]]*"([^"]*)"'
  local names=""
  local asmdef content
  for asmdef in "$1"/Assets/**/*.asmdef; do
    # Unity also skips a folder whose name ends with ~.
    if [[ "${asmdef#"$1"/Assets/}" == *"~/"* ]]; then
      continue
    fi
    content="$(<"${asmdef}")"
    if [[ "${content}" =~ ${name_regex} ]]; then
      names+="${BASH_REMATCH[1]};"
    fi
  done
  echo "${names}${ASSEMBLY_CSHARP_NAMES}"
}

#######################################
# Prints each test case in an NUnit 3 results file that didn't pass, with its
# message, then counts the results.
# Arguments:
#   Results file.
# Outputs:
#   Writes the report to stdout.
#######################################
report() {
  awk '
    function unescape(s) {
      gsub(/&lt;/, "<", s); gsub(/&gt;/, ">", s); gsub(/&quot;/, "\"", s)
      gsub(/&apos;/, "\047", s); gsub(/&#xA;/, "\n", s)
      gsub(/&#xD;/, "\r", s); gsub(/&#x9;/, "\t", s); gsub(/&amp;/, "\\&", s)
      return s
    }
    function attr(tag, name,    rest, start) {
      start = index(tag, " " name "=\"")
      if (start == 0) return ""
      rest = substr(tag, start + length(name) + 3)
      return unescape(substr(rest, 1, index(rest, "\"") - 1))
    }
    # The length of the tag at the start of s, up to its closing ">", or 0
    # when the tag goes on past this line.
    function tag_length(s,    i, c, quoted) {
      for (i = 1; i <= length(s); i++) {
        c = substr(s, i, 1)
        if (c == "\"") quoted = !quoted
        else if (c == ">" && !quoted) return i
      }
      return 0
    }
    function trim(s) {
      sub(/^[ \t\r\n]+/, "", s); sub(/[ \t\r\n]+$/, "", s)
      return s
    }
    function finish_case() {
      total++
      if (result == "Passed") { passed++; return }
      if (result == "Failed") failed++
      print toupper(result) "  " fullname
      message = trim(message)
      if (message != "") {
        gsub(/\n/, "\n    ", message)
        print "    " message
      }
    }
    function handle(tag) {
      if (tag ~ /^<test-case[ >\/]/) {
        in_case = 1; want = 0; got = 0; message = ""
        result = attr(tag, "result"); fullname = attr(tag, "fullname")
        if (tag ~ /\/>$/) { in_case = 0; finish_case() }
      }
      else if (tag == "</test-case>") { in_case = 0; finish_case() }
      else if (in_case && tag ~ /^<(failure|reason)[ >]/) want = 1
      else if (tag ~ /^<\/(failure|reason)>/) want = 0
      else if (in_case && want && !got && tag == "<message>") in_message = 1
      else if (tag == "</message>" && in_message) { in_message = 0; got = 1 }
    }
    {
      line = $0
      if (pending != "") { line = pending "\n" line; pending = "" }
      while (line != "") {
        if (in_cdata) {
          end = index(line, "]]>")
          if (end == 0) {
            if (in_message) message = message line "\n"
            line = ""
          }
          else {
            if (in_message) message = message substr(line, 1, end - 1)
            in_cdata = 0
            line = substr(line, end + 3)
          }
          continue
        }
        start = index(line, "<")
        text = start ? substr(line, 1, start - 1) : line
        if (in_message) message = message unescape(text) (start ? "" : "\n")
        if (start == 0) break
        line = substr(line, start)
        if (substr(line, 1, 9) == "<![CDATA[") {
          in_cdata = 1
          line = substr(line, 10)
          continue
        }
        tag_end = tag_length(line)
        if (tag_end == 0) { pending = line; break }
        handle(substr(line, 1, tag_end))
        line = substr(line, tag_end + 1)
      }
    }
    END {
      printf "%d passed, %d failed, %d inconclusive or skipped\n",
        passed, failed, total - passed - failed
    }
  ' "$1"
}

main() {
  local project_path="" unity_path="" test_mode="All" assembly_names=""
  local unity_arguments=()
  local results_path
  results_path="${TMPDIR:-/tmp}/UnityTestResults/$(date +%Y%m%d-%H%M%S)"

  while (( $# > 0 )); do
    case "$1" in
      -p|--project-path)
        require_value "$@"
        project_path="$2"
        shift 2
        ;;
      -u|--unity-path)
        require_value "$@"
        unity_path="$2"
        shift 2
        ;;
      -m|--test-mode)
        require_value "$@"
        test_mode="$2"
        shift 2
        ;;
      -a|--assembly-names)
        require_value "$@"
        assembly_names="$2"
        shift 2
        ;;
      -r|--results-path)
        require_value "$@"
        results_path="$2"
        shift 2
        ;;
      -h|--help)
        usage
        exit 0
        ;;
      --)
        shift
        unity_arguments=("$@")
        break
        ;;
      *)
        usage_error "unknown option $1"
        ;;
    esac
  done
  case "${test_mode}" in
    EditMode|PlayMode|All) ;;
    *)
      usage_error "test mode must be EditMode, PlayMode, or All," \
        "not ${test_mode}"
      ;;
  esac

  if [[ -z "${project_path}" ]]; then
    local script_folder
    script_folder="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
    if ! project_path="$(find_project "${script_folder}")" \
      && ! project_path="$(find_project "${PWD}")"; then
      err "No Unity project found above this script or the current folder." \
        "Pass --project-path."
      exit 1
    fi
  fi
  local absolute_project_path
  if ! absolute_project_path="$(cd "${project_path}" 2>/dev/null && pwd)"; then
    err "No such folder: ${project_path}"
    exit 1
  fi
  project_path="${absolute_project_path}"
  if [[ -z "${unity_path}" ]]; then
    local version
    version="$(head -n 1 "${project_path}/ProjectSettings/ProjectVersion.txt")"
    version="${version#m_EditorVersion:}"
    # A ProjectVersion.txt saved on Windows ends its lines with a carriage
    # return.
    version="${version//[[:space:]]/}"
    unity_path="${HOME}/Unity/Hub/Editor/${version}/Editor/Unity"
  fi
  if [[ ! -x "${unity_path}" ]]; then
    err "Unity not found at ${unity_path}. Pass --unity-path."
    exit 1
  fi

  # Without assembly names Unity runs every test it finds, including the
  # ones that come with packages.
  if [[ -z "${assembly_names}" ]]; then
    assembly_names="$(default_assembly_names "${project_path}")"
  fi

  # Absolute, because Unity resolves a relative path from the project folder.
  local absolute_results_path
  if ! mkdir -p -- "${results_path}" \
    || ! absolute_results_path="$(cd "${results_path}" && pwd)"; then
    err "Can't create the results folder ${results_path}"
    exit 1
  fi
  results_path="${absolute_results_path}"

  local modes=("${test_mode}")
  if [[ "${test_mode}" == "All" ]]; then
    modes=(EditMode PlayMode)
  fi
  local exit_code=0
  local mode results log
  for mode in "${modes[@]}"; do
    results="${results_path}/${mode}.xml"
    log="${results_path}/${mode}.log"
    rm -f -- "${results}"

    echo "${mode} tests..."
    if ! "${unity_path}" -batchmode -nographics \
      -projectPath "${project_path}" -logFile "${log}" \
      -runTests -testPlatform "${mode}" -testResults "${results}" \
      -assemblyNames "${assembly_names}" "${unity_arguments[@]}" \
      > /dev/null; then
      exit_code=1
    fi

    if [[ ! -f "${results}" ]]; then
      echo "No results were written. See ${log}"
      exit_code=1
    else
      report "${results}"
    fi
    echo
  done
  exit "${exit_code}"
}

main "$@"
