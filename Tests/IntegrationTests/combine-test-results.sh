#!/bin/bash

set -e

JSON_PATH=$1
ANNOTATIONS_FILE=$2
WORKFLOW_FILE=${3:-.github/workflows/integration_tests_r4.yml}

function usage() {
  me=`basename "$0"`
  if [ ! -z "$1" ]; then
    echo $1
  fi
  echo "Usage: ./${me} path/to/input/json_results path/to/output/annotations.json [workflow_file]"
  exit 1
}

[ $# -ge 2 ] || usage

[ ! -z "${JSON_PATH}" ] || usage

[ -d "${JSON_PATH}" ] || usage "${JSON_PATH} must be a directory"

touch $ANNOTATIONS_FILE || "$ANNOTATIONS_FILE is not writable"

DIR=$(pwd)

cd ${JSON_PATH}

# Output summary to stdout

ls _summary*.json | xargs jq -r '. | "PASS: \(.pass // 0)
FAIL: \(.fail // 0)
ERROR: \(.error // 0)
SKIP: \(.skip // 0)"
'

# Have to use warning annotation level, notice isn't working anymore (but could be in future).

SUMMARY=$(ls _summary*.json | xargs jq --arg wf "$WORKFLOW_FILE" '[ .
  | { "file": $wf,  "line": 1, "message": ("PASS: \(.pass // 0)\nFAIL: \(.fail // 0)\nERROR: \(.error // 0)\nSKIP: \(.skip // 0)"), "annotation_level": "warning" }
]')

FAILURES=$(ls -I '_summary*.json' | xargs -I '{}' jq --arg wf "$WORKFLOW_FILE" '[ .[]
    | select(((.status == "skip") and (.message | contains("TODO") | not))
        or .status == "fail"
        or .status == "error")
    | .id as $id
    | .status as $status
    | .message as $message
    | .description as $description
    | .test_method as $method
    | input_filename as $file
    | { "file": $wf,  "line": 1, "message": ($id + ": " + $message), "annotation_level": "failure" }
]' '{}')

cd $DIR

jq 'reduce inputs as $i (.; . += $i)' <(echo "${SUMMARY}") <(echo "${FAILURES}") > ${ANNOTATIONS_FILE}
