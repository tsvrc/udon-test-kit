#!/bin/bash
#
# Tests run_unity_tests.sh with fake_unity.sh standing in for Unity: the report
# of each case in cases.txt, the arguments it gives Unity, and its defaults and
# errors. Prints each check that fails, and exits with 1 when one does.

here="$(cd "$(dirname "$0")" && pwd)"
readonly HERE="${here}"
readonly SCRIPT="${HERE}/../../Scripts~/run_unity_tests.sh"
# Seconds each run of the script gets, so a run that never ends fails its check
# instead of stopping the test.
readonly LIMIT=60
failures=0

#######################################
# Compares what a check wrote with what it should have written.
# Arguments:
#   Check name, expected file, actual file.
#######################################
check() {
  if ! diff -u "$2" "$3"; then
    echo "FAILED  $1"
    failures=$(( failures + 1 ))
  fi
}

#######################################
# Runs a command and compares its exit code and the first line it prints with
# the expected ones.
# Arguments:
#   Check name, exit code, first line, then the command and its arguments.
#######################################
check_run() {
  local -r name="$1" code="$2" line="$3"
  shift 3
  timeout "${LIMIT}" "$@" > "${tmp}/${name}.out" 2>&1
  local -r status=$?
  local first_line
  first_line="$(head -n 1 "${tmp}/${name}.out")"
  printf 'exit %s\n%s\n' "${status}" "${first_line}" > "${tmp}/${name}.txt"
  check "${name}" <(printf 'exit %s\n%s\n' "${code}" "${line}") \
    "${tmp}/${name}.txt"
}

main() {
  tmp="$(mktemp -d)"
  trap 'rm -rf "${tmp}"' EXIT
  local project="${HERE}/project"

  local name xml code mode
  while read -r name xml code mode; do
    [[ -z "${name}" || "${name}" == \#* ]] && continue
    local fake_xml=""
    [[ "${xml}" != "-" ]] && fake_xml="${HERE}/${xml}"
    FAKE_XML="${fake_xml}" FAKE_EXIT="${code}" timeout "${LIMIT}" "${SCRIPT}" \
      -p "${project}" -u "${HERE}/fake_unity.sh" -m "${mode}" \
      -r "${tmp}/${name}" > "${tmp}/${name}.out" 2>&1
    echo "exit $?" >> "${tmp}/${name}.out"
    sed "s#${tmp}/${name}/#RESULTS/#" "${tmp}/${name}.out" \
      > "${tmp}/${name}.txt"
    check "${name}" "${HERE}/expected/${name}.txt" "${tmp}/${name}.txt"
  done < "${HERE}/cases.txt"

  FAKE_ARGS="${tmp}/arguments.args" timeout "${LIMIT}" "${SCRIPT}" \
    -p "${project}" -u "${HERE}/fake_unity.sh" -m EditMode \
    -r "${tmp}/arguments" -- -buildTarget win64 > /dev/null
  sed -e "s#^${project}\$#PROJECT#" -e "s#^${tmp}/arguments/#RESULTS/#" \
    "${tmp}/arguments.args" > "${tmp}/arguments.txt"
  check arguments "${HERE}/expected/arguments.txt" "${tmp}/arguments.txt"

  FAKE_ARGS="${tmp}/given.args" timeout "${LIMIT}" "${SCRIPT}" \
    -p "${project}" -u "${HERE}/fake_unity.sh" -m EditMode \
    -r "${tmp}/given" -a "One;Two" > /dev/null
  sed -n '/^-assemblyNames$/{n;p;}' "${tmp}/given.args" > "${tmp}/given.txt"
  check assembly-names <(echo "One;Two") "${tmp}/given.txt"

  # A copy of the script inside a project finds that project, and the Unity
  # version it names, even with Windows line endings. No Unity has that
  # version, so a real one never starts.
  cp -R "${project}" "${tmp}/copy"
  printf 'm_EditorVersion: 0.0.0f0\r\n' \
    > "${tmp}/copy/ProjectSettings/ProjectVersion.txt"
  mkdir "${tmp}/copy/Assets/Scripts" "${tmp}/outside"
  cp "${SCRIPT}" "${tmp}/copy/Assets/Scripts/"
  cp "${SCRIPT}" "${tmp}/outside/"
  local -r no_unity="run_unity_tests.sh: Unity not found at \
${tmp}/home/Unity/Hub/Editor/0.0.0f0/Editor/Unity. Pass --unity-path."
  check_run defaults 1 "${no_unity}" \
    env HOME="${tmp}/home" "${tmp}/copy/Assets/Scripts/run_unity_tests.sh"
  # A copy outside any project looks in the current folder instead. Both checks
  # give it the fake Unity, so finding the wrong project can't start a real one.
  FAKE_ARGS="${tmp}/current.args" timeout "${LIMIT}" env -C "${tmp}/copy" \
    "${tmp}/outside/run_unity_tests.sh" -u "${HERE}/fake_unity.sh" \
    -m EditMode -r "${tmp}/current" > /dev/null
  sed -n '/^-projectPath$/{n;p;}' "${tmp}/current.args" > "${tmp}/current.txt"
  check current-folder <(echo "${tmp}/copy") "${tmp}/current.txt"
  check_run no-project 1 "run_unity_tests.sh: No Unity project found above \
this script or the current folder. Pass --project-path." \
    env -C "${tmp}/outside" "${tmp}/outside/run_unity_tests.sh" \
    -u "${HERE}/fake_unity.sh"

  check_run missing-project 1 \
    "run_unity_tests.sh: No such folder: ${tmp}/missing" \
    "${SCRIPT}" -p "${tmp}/missing"
  check_run test-mode 2 "run_unity_tests.sh: test mode must be EditMode, \
PlayMode, or All, not Both" "${SCRIPT}" -m Both
  check_run usage 2 "run_unity_tests.sh: unknown option --no-such-option" \
    "${SCRIPT}" --no-such-option
  check_run missing-value 2 "run_unity_tests.sh: option -p needs a value" \
    "${SCRIPT}" -p
  check_run help 0 \
    "Usage: run_unity_tests.sh [options] [-- unity-arguments]" \
    "${SCRIPT}" --help

  if (( failures > 0 )); then
    echo "${failures} checks failed"
    exit 1
  fi
  echo "All checks passed"
}

main "$@"
