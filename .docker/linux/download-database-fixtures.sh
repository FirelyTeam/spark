#!/usr/bin/env bash

set -euo pipefail

readonly DEFAULT_FIXTURE_REPOSITORY="IncendiLabs/spark-database-fixtures"
readonly DEFAULT_FIXTURE_RELEASE="latest"
readonly SCRIPT_DIRECTORY="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"

fixture_repository="${FIXTURE_REPOSITORY:-${DEFAULT_FIXTURE_REPOSITORY}}"
fixture_release="${FIXTURE_RELEASE:-${DEFAULT_FIXTURE_RELEASE}}"
destination_directory="${FIXTURE_DESTINATION:-${SCRIPT_DIRECTORY}}"
readonly -a all_archives=(
    "stu3.archive.gz"
    "r4.archive.gz"
    "r4b.archive.gz"
    "r5.archive.gz"
    "r6.archive.gz"
)

usage() {
    cat <<EOF
Usage: $(basename -- "$0") [FHIR_VERSION ...]

Download and verify the MongoDB database fixtures used by the Linux Docker
images. With no arguments, all fixtures are downloaded. Versions may be
specified as stu3, r4, r4b, r5, or r6.

Environment variables:
  FIXTURE_REPOSITORY   GitHub repository containing the release assets
                       (default: ${DEFAULT_FIXTURE_REPOSITORY})
  FIXTURE_RELEASE      Release tag to download, or latest (default: ${DEFAULT_FIXTURE_RELEASE})
  FIXTURE_DESTINATION  Destination directory (default: ${SCRIPT_DIRECTORY})
EOF
}

fail() {
    printf 'Error: %s\n' "$*" >&2
    exit 1
}

download() {
    local url="$1"
    local output="$2"

    curl \
        --fail \
        --location \
        --retry 3 \
        --retry-all-errors \
        --silent \
        --show-error \
        --output "$output" \
        "$url"
}

resolve_latest_release() {
    local latest_url="https://github.com/${fixture_repository}/releases/latest"
    local resolved_url

    resolved_url="$(curl \
        --fail \
        --location \
        --retry 3 \
        --retry-all-errors \
        --silent \
        --show-error \
        --output /dev/null \
        --write-out '%{url_effective}' \
        "$latest_url")" ||
        fail "Could not determine the latest release for ${fixture_repository}."

    resolved_url="${resolved_url%/}"
    [[ "$resolved_url" == "https://github.com/${fixture_repository}/releases/tag/"* ]] ||
        fail "GitHub returned an unexpected latest-release URL: ${resolved_url}"

    printf '%s\n' "${resolved_url##*/}"
}

sha256() {
    local file="$1"

    if command -v sha256sum >/dev/null 2>&1; then
        sha256sum "$file" | awk '{ print $1 }'
    elif command -v shasum >/dev/null 2>&1; then
        shasum -a 256 "$file" | awk '{ print $1 }'
    else
        fail "sha256sum or shasum is required to verify fixture downloads."
    fi
}

checksum_for() {
    local archive="$1"
    local checksum_file="$2"

    awk -v archive="$archive" '
        $1 ~ /^[[:xdigit:]]{64}$/ {
            name = $2
            sub(/^\*/, "", name)
            count = split(name, parts, "/")
            if (parts[count] == archive) {
                print tolower($1)
                exit
            }
        }
    ' "$checksum_file"
}

archive_for_version() {
    case "$1" in
        stu3|r4|r4b|r5|r6)
            printf '%s.archive.gz\n' "$1"
            ;;
        *)
            fail "Unknown FHIR version '$1'. Expected stu3, r4, r4b, r5, or r6."
            ;;
    esac
}

if [[ "${1:-}" == "--help" || "${1:-}" == "-h" ]]; then
    usage
    exit 0
fi

command -v curl >/dev/null 2>&1 || fail "curl is required to download fixture assets."
mkdir -p -- "$destination_directory"

temporary_directory="$(mktemp -d)"
trap 'rm -rf -- "$temporary_directory"' EXIT

if [[ "$fixture_release" == "latest" ]]; then
    fixture_release="$(resolve_latest_release)"
    printf 'Latest fixture release is %s.\n' "$fixture_release"
fi

readonly release_url="https://github.com/${fixture_repository}/releases/download/${fixture_release}"
readonly checksum_file="${temporary_directory}/SHA256SUMS"

printf 'Downloading checksums from %s/%s...\n' "$fixture_repository" "$fixture_release"
download "${release_url}/SHA256SUMS" "$checksum_file" ||
    fail "Could not download SHA256SUMS from ${fixture_repository} release ${fixture_release}."

declare -a requested_archives
if (( $# == 0 )); then
    requested_archives=("${all_archives[@]}")
else
    for version in "$@"; do
        requested_archives+=("$(archive_for_version "$version")")
    done
fi

for archive in "${requested_archives[@]}"; do
    expected_checksum="$(checksum_for "$archive" "$checksum_file")"
    [[ -n "$expected_checksum" ]] ||
        fail "SHA256SUMS does not contain a valid checksum for ${archive}."

    destination="${destination_directory}/${archive}"
    if [[ -f "$destination" ]] && [[ "$(sha256 "$destination")" == "$expected_checksum" ]]; then
        printf '%s is already downloaded and verified.\n' "$archive"
        continue
    fi

    temporary_archive="${temporary_directory}/${archive}"
    printf 'Downloading %s...\n' "$archive"
    download "${release_url}/${archive}" "$temporary_archive" ||
        fail "Could not download ${archive} from ${fixture_repository} release ${fixture_release}."

    actual_checksum="$(sha256 "$temporary_archive")"
    [[ "$actual_checksum" == "$expected_checksum" ]] ||
        fail "Checksum verification failed for ${archive}."

    mv -- "$temporary_archive" "$destination"
    printf 'Installed and verified %s.\n' "$destination"
done
