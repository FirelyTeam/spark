#!/bin/bash

set -e

JSON_PATH=$1
ANNOTATIONS_FILE=$2

function usage() {
  me=`basename "$0"`
  echo "Usage: ./${me} path/to/input/json_results path/to/output/annotations.json"
  exit 1
}

[ $# -eq 2 ] || usage

[ ! -z "${JSON_PATH}" ] || usage

[ -d "${JSON_PATH}" ] || usage

touch $ANNOTATIONS_FILE || usage

ls ${JSON_PATH}/*.json | xargs jq -c '[ .[]
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
]' | jq 'reduce inputs as $i (.; . += $i)' > ${ANNOTATIONS_FILE}

