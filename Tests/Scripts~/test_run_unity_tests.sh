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
# Runs a command and compares its exit code and everything it prints with the
# expected ones.
# Arguments:
#   Check name, exit code, output, then the command and its arguments.
#######################################
check_run() {
  local -r name="$1" code="$2" output="$3"
  shift 3
  timeout "${LIMIT}" "$@" > "${tmp}/${name}.out" 2>&1
  local -r status=$?
  { echo "exit ${status}"; cat "${tmp}/${name}.out"; } > "${tmp}/${name}.txt"
  check "${name}" <(printf 'exit %s\n%s\n' "${code}" "${output}") \
    "${tmp}/${name}.txt"
}

#######################################
# Runs a command and compares its exit code and the first line it prints with
# the expected ones.
# Arguments:
#   Check name, exit code, first line, then the command and its arguments.
#######################################
check_first_line() {
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
    local fake_xml="" mode_option=(-m "${mode}")
    [[ "${xml}" != "-" ]] && fake_xml="${HERE}/${xml}"
    [[ "${mode}" == "-" ]] && mode_option=()
    FAKE_XML="${fake_xml}" FAKE_EXIT="${code}" timeout "${LIMIT}" "${SCRIPT}" \
      -p "${project}" -u "${HERE}/fake_unity.sh" "${mode_option[@]}" \
      -r "${tmp}/${name}" > "${tmp}/${name}.out" 2>&1
    echo "exit $?" >> "${tmp}/${name}.out"
    sed "s#${tmp}/${name}/#RESULTS/#" "${tmp}/${name}.out" \
      > "${tmp}/${name}.txt"
    check "${name}" "${HERE}/expected/${name}.txt" "${tmp}/${name}.txt"
  done < "${HERE}/cases.txt"

  FAKE_ARGS="${tmp}/arguments.args" timeout "${LIMIT}" "${SCRIPT}" \
    -p "${project}" -u "${HERE}/fake_unity.sh" -m EditMode \
    -r "${tmp}/arguments" -- -buildTarget win64 extra > /dev/null
  sed -e "s#^${project}\$#PROJECT#" -e "s#^${tmp}/arguments/#RESULTS/#" \
    "${tmp}/arguments.args" > "${tmp}/arguments.txt"
  check arguments "${HERE}/expected/arguments.txt" "${tmp}/arguments.txt"

  FAKE_ARGS="${tmp}/given.args" timeout "${LIMIT}" "${SCRIPT}" \
    -p "${project}" -u "${HERE}/fake_unity.sh" -m EditMode \
    -r "${tmp}/given" -a "One;Two" > /dev/null
  sed -n '/^-assemblyNames$/{n;p;}' "${tmp}/given.args" > "${tmp}/given.txt"
  check assembly-names <(echo "One;Two") "${tmp}/given.txt"

  FAKE_ARGS="${tmp}/long.args" timeout "${LIMIT}" "${SCRIPT}" \
    --project-path "${project}" --unity-path "${HERE}/fake_unity.sh" \
    --test-mode EditMode --results-path "${tmp}/long" \
    --assembly-names "One;Two" > /dev/null
  sed -n '/^-assemblyNames$/{n;p;}' "${tmp}/long.args" > "${tmp}/long.txt"
  check long-options <(echo "One;Two") "${tmp}/long.txt"

  # Unity gets the project path as an absolute one.
  FAKE_ARGS="${tmp}/relative.args" timeout "${LIMIT}" env -C "${HERE}" \
    "${SCRIPT}" -p project -u "${HERE}/fake_unity.sh" -m EditMode \
    -r "${tmp}/relative" > /dev/null
  sed -n '/^-projectPath$/{n;p;}' "${tmp}/relative.args" \
    > "${tmp}/relative.txt"
  check relative-project <(echo "${project}") "${tmp}/relative.txt"
  # And the results path as an absolute one.
  FAKE_ARGS="${tmp}/relative-results.args" timeout "${LIMIT}" env -C "${tmp}" \
    "${SCRIPT}" -p "${project}" -u "${HERE}/fake_unity.sh" -m EditMode \
    -r relative-results > /dev/null
  sed -n '/^-testResults$/{n;p;}' "${tmp}/relative-results.args" \
    > "${tmp}/relative-results.txt"
  check relative-results <(echo "${tmp}/relative-results/EditMode.xml") \
    "${tmp}/relative-results.txt"

  # A project without assembly definitions runs the Assembly-CSharp ones, and
  # looking for them writes no error.
  mkdir -p "${tmp}/bare/Assets" "${tmp}/bare/ProjectSettings"
  FAKE_ARGS="${tmp}/bare.args" timeout "${LIMIT}" "${SCRIPT}" \
    -p "${tmp}/bare" -u "${HERE}/fake_unity.sh" -m EditMode \
    -r "${tmp}/bare-results" > "${tmp}/bare.out" 2>&1
  sed -n '/^-assemblyNames$/{n;p;}' "${tmp}/bare.args" >> "${tmp}/bare.out"
  check no-assembly-definitions <(printf '%s\n' "EditMode tests..." \
    "No results were written. See ${tmp}/bare-results/EditMode.log" "" \
    "Assembly-CSharp;Assembly-CSharp-firstpass;Assembly-CSharp-Editor;\
Assembly-CSharp-Editor-firstpass") "${tmp}/bare.out"

  # Without -r, the results go to a new folder in the temp folder.
  mkdir "${tmp}/temp"
  FAKE_ARGS="${tmp}/default.args" TMPDIR="${tmp}/temp" timeout "${LIMIT}" \
    "${SCRIPT}" -p "${project}" -u "${HERE}/fake_unity.sh" -m EditMode \
    > /dev/null
  sed -n '/^-testResults$/{n;p;}' "${tmp}/default.args" > "${tmp}/default.out"
  sed -E 's#/[0-9]{8}-[0-9]{6}/#/DATE/#' "${tmp}/default.out" \
    > "${tmp}/default.txt"
  check results-default \
    <(echo "${tmp}/temp/UnityTestResults/DATE/EditMode.xml") \
    "${tmp}/default.txt"

  # A results file an earlier run left is removed first, so a run that writes
  # none doesn't report the old one.
  mkdir "${tmp}/stale"
  cp "${HERE}/results-passed.xml" "${tmp}/stale/EditMode.xml"
  FAKE_XML="" timeout "${LIMIT}" "${SCRIPT}" -p "${project}" \
    -u "${HERE}/fake_unity.sh" -m EditMode -r "${tmp}/stale" \
    > "${tmp}/stale.out" 2>&1
  echo "exit $?" >> "${tmp}/stale.out"
  sed "s#${tmp}/stale/#RESULTS/#" "${tmp}/stale.out" > "${tmp}/stale.txt"
  check stale-results "${HERE}/expected/no-results.txt" "${tmp}/stale.txt"

  touch "${tmp}/file"
  timeout "${LIMIT}" "${SCRIPT}" -p "${project}" -u "${HERE}/fake_unity.sh" \
    -r "${tmp}/file/results" > "${tmp}/results-folder.out" 2>&1
  echo "exit $?" >> "${tmp}/results-folder.out"
  tail -n 2 "${tmp}/results-folder.out" > "${tmp}/results-folder.txt"
  check results-folder <(printf '%s\nexit 1\n' "run_unity_tests.sh: Can't \
create the results folder ${tmp}/file/results") "${tmp}/results-folder.txt"

  # The PowerShell runner leaves character references to the .NET XML parser;
  # the awk one turns &#xD; into a carriage return too, which the shared
  # expected outputs can't hold.
  printf '<test-case fullname="A&#xD;B" result="Failed" />\n' > "${tmp}/cr.xml"
  FAKE_XML="${tmp}/cr.xml" FAKE_EXIT=2 timeout "${LIMIT}" "${SCRIPT}" \
    -p "${project}" -u "${HERE}/fake_unity.sh" -m EditMode -r "${tmp}/cr" \
    > "${tmp}/cr.out"
  sed -n 2p "${tmp}/cr.out" > "${tmp}/cr.txt"
  check carriage-return <(printf 'FAILED  A\rB\n') "${tmp}/cr.txt"

  # A copy of the script inside a project finds that project, past a
  # ProjectSettings folder without a version file, and the Unity version on the
  # first line of its version file, even with Windows line endings. No Unity has
  # that version, so a real one never starts.
  cp -R "${project}" "${tmp}/copy"
  printf '%s\r\n' 'm_EditorVersion: 0.0.0f0' \
    'm_EditorVersionWithRevision: 0.0.0f0' \
    > "${tmp}/copy/ProjectSettings/ProjectVersion.txt"
  mkdir -p "${tmp}/copy/Assets/Scripts/ProjectSettings" "${tmp}/outside"
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
  # The project around the script comes before the current folder's.
  FAKE_ARGS="${tmp}/first.args" timeout "${LIMIT}" env -C "${project}" \
    "${tmp}/copy/Assets/Scripts/run_unity_tests.sh" \
    -u "${HERE}/fake_unity.sh" -m EditMode -r "${tmp}/first" > /dev/null
  sed -n '/^-projectPath$/{n;p;}' "${tmp}/first.args" > "${tmp}/first.txt"
  check script-folder-first <(echo "${tmp}/copy") "${tmp}/first.txt"
  check_run no-project 1 "run_unity_tests.sh: No Unity project found above \
this script or the current folder. Pass --project-path." \
    env -C "${tmp}/outside" "${tmp}/outside/run_unity_tests.sh" \
    -u "${HERE}/fake_unity.sh"

  check_run missing-project 1 \
    "run_unity_tests.sh: No such folder: ${tmp}/missing" \
    "${SCRIPT}" -p "${tmp}/missing"
  local -r hint="Try 'run_unity_tests.sh --help'."
  check_run test-mode 2 "run_unity_tests.sh: test mode must be EditMode, \
PlayMode, or All, not Both
${hint}" "${SCRIPT}" -m Both
  touch "${tmp}/not-unity"
  check_run unity-not-executable 1 "run_unity_tests.sh: Unity not found at \
${tmp}/not-unity. Pass --unity-path." \
    "${SCRIPT}" -p "${project}" -u "${tmp}/not-unity"
  check_run usage 2 "run_unity_tests.sh: unknown option --no-such-option
${hint}" "${SCRIPT}" --no-such-option
  local option
  for option in -p -u -m -a -r; do
    check_run "missing-value${option}" 2 \
      "run_unity_tests.sh: option ${option} needs a value
${hint}" "${SCRIPT}" "${option}"
  done
  check_first_line help 0 \
    "Usage: run_unity_tests.sh [options] [-- unity-arguments]" \
    "${SCRIPT}" --help
  check_first_line help-short 0 \
    "Usage: run_unity_tests.sh [options] [-- unity-arguments]" "${SCRIPT}" -h

  if (( failures > 0 )); then
    echo "${failures} checks failed"
    exit 1
  fi
  echo "All checks passed"
}

main "$@"
