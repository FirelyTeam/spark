#!/bin/bash

set -e

JSON_PATH=$1
ANNOTATIONS_FILE=$2

function usage() {
  me=`basename "$0"`
  if [ ! -z "$1" ]; then
    echo $1
  fi
  echo "Usage: ./${me} path/to/input/json_results path/to/output/annotations.json"
  exit 1
}

[ $# -eq 2 ] || usage

[ ! -z "${JSON_PATH}" ] || usage

[ -d "${JSON_PATH}" ] || usage "${JSON_PATH} must be a directory"

touch $ANNOTATIONS_FILE || "$ANNOTATIONS_FILE is not writable"

DIR=$(pwd)

cd ${JSON_PATH}

# Have to use warning annotation level, notice isn't working anymore (but could be in future).

SUMMARY=$(ls _summary*.json | xargs jq -c '[ .
  | { "file": "summary",  "line": 1, "message": ("PASS: \(.pass // 0)\nFAIL: \(.fail // 0)\nERROR: \(.error // 0)\nSKIP: \(.skip // 0)"), "annotation_level": "warning" }
]')

FAILURES=$(ls -I '_summary*.json' | xargs jq -c '[ .[]
    | select(((.status == "skip") and (.message | contains("TODO") | not))
        or .status == "fail"
        or .status == "error")
    | .id as $id
    | .status as $status
    | .message as $message
    | .description as $description
    | .test_method as $method
    | input_filename as $file
    | { "file": $id,  "line": 1, "message": $message, "annotation_level": "failure" }
]')

cd $DIR

jq 'reduce inputs as $i (.; . += $i)' <(echo "${SUMMARY}") <(echo "${FAILURES}") > ${ANNOTATIONS_FILE}
