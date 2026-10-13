#!/bin/bash
#
# Tests run_unity_tests.sh with fake_unity.sh standing in for Unity: the report
# of each case in cases.txt, the arguments it gives Unity, and its defaults and
# errors. Prints each check that fails, and exits with 1 when one does.

here="$(cd "$(dirname "$0")" && pwd)"
readonly HERE="${here}"
readonly SCRIPT="${HERE}/../../Scripts~/run_unity_tests.sh"
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

main() {
  tmp="$(mktemp -d)"
  trap 'rm -rf "${tmp}"' EXIT
  local project="${HERE}/project"

  local name xml code mode
  while read -r name xml code mode; do
    [[ -z "${name}" || "${name}" == \#* ]] && continue
    local fake_xml=""
    [[ "${xml}" != "-" ]] && fake_xml="${HERE}/${xml}"
    FAKE_XML="${fake_xml}" FAKE_EXIT="${code}" "${SCRIPT}" -p "${project}" \
      -u "${HERE}/fake_unity.sh" -m "${mode}" -r "${tmp}/${name}" \
      > "${tmp}/${name}.out" 2>&1
    echo "exit $?" >> "${tmp}/${name}.out"
    sed "s#${tmp}/${name}/#RESULTS/#" "${tmp}/${name}.out" > "${tmp}/${name}.txt"
    check "${name}" "${HERE}/expected/${name}.txt" "${tmp}/${name}.txt"
  done < "${HERE}/cases.txt"

  FAKE_ARGS="${tmp}/arguments.args" "${SCRIPT}" -p "${project}" \
    -u "${HERE}/fake_unity.sh" -m EditMode -r "${tmp}/arguments" \
    -- -buildTarget win64 > /dev/null
  sed -e "s#^${project}\$#PROJECT#" -e "s#^${tmp}/arguments/#RESULTS/#" \
    "${tmp}/arguments.args" > "${tmp}/arguments.txt"
  check arguments "${HERE}/expected/arguments.txt" "${tmp}/arguments.txt"

  # A copy of the script inside a project finds that project, and the Unity
  # version it names, even with Windows line endings. No Unity has that
  # version, so a real one never starts.
  cp -R "${project}" "${tmp}/copy"
  printf 'm_EditorVersion: 0.0.0f0\r\n' \
    > "${tmp}/copy/ProjectSettings/ProjectVersion.txt"
  mkdir "${tmp}/copy/Assets/Scripts"
  cp "${SCRIPT}" "${tmp}/copy/Assets/Scripts/"
  HOME="${tmp}/home" "${tmp}/copy/Assets/Scripts/run_unity_tests.sh" \
    2> "${tmp}/defaults.err"
  echo "exit $?" >> "${tmp}/defaults.err"
  printf 'run_unity_tests.sh: Unity not found at %s. Pass --unity-path.\nexit 1\n' \
    "${tmp}/home/Unity/Hub/Editor/0.0.0f0/Editor/Unity" \
    > "${tmp}/defaults.expected"
  check defaults "${tmp}/defaults.expected" "${tmp}/defaults.err"

  "${SCRIPT}" --no-such-option > /dev/null 2>&1
  echo "exit $?" > "${tmp}/usage.txt"
  check usage <(echo "exit 2") "${tmp}/usage.txt"

  if (( failures > 0 )); then
    echo "${failures} checks failed"
    exit 1
  fi
  echo "All checks passed"
}

main "$@"
