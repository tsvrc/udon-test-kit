#!/bin/bash
#
# Stands in for Unity in test_run_unity_tests.sh: writes each argument on a
# line of $FAKE_ARGS, copies $FAKE_XML to the -testResults path, and exits with
# $FAKE_EXIT.

printf '%s\n' "$@" > "${FAKE_ARGS:-/dev/null}"
while (( $# > 0 )); do
  if [[ "$1" == "-testResults" && -n "${FAKE_XML:-}" ]]; then
    cp "${FAKE_XML}" "$2"
  fi
  shift
done
exit "${FAKE_EXIT:-0}"
