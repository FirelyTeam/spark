#!/usr/bin/env bash

set -euo pipefail

readonly DEFAULT_FIXTURE_REPOSITORY="IncendiLabs/spark-database-fixtures"
readonly ARCHIVE_DIRECTORY="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
readonly -a ARCHIVES=(
    "stu3.archive.gz"
    "r4.archive.gz"
    "r4b.archive.gz"
    "r5.archive.gz"
    "r6.archive.gz"
)

fixture_repository="${FIXTURE_REPOSITORY:-${DEFAULT_FIXTURE_REPOSITORY}}"
assume_yes=false
dry_run=false

usage() {
    cat <<EOF
Usage: $(basename -- "$0") [OPTIONS]

Validate Spark's MongoDB fixtures and publish the next sequential GitHub
release. Existing vN tags are inspected and the next tag is max(N) + 1.

Options:
  --dry-run   Validate files and print the next tag without publishing
  --yes       Publish without interactive confirmation
  -h, --help  Show this help

Environment variables:
  FIXTURE_REPOSITORY  GitHub repository that receives the release
                      (default: ${DEFAULT_FIXTURE_REPOSITORY})
EOF
}

fail() {
    printf 'Error: %s\n' "$*" >&2
    exit 1
}

sha256() {
    local file="$1"

    if command -v sha256sum >/dev/null 2>&1; then
        sha256sum "$file" | awk '{ print $1 }'
    elif command -v shasum >/dev/null 2>&1; then
        shasum -a 256 "$file" | awk '{ print $1 }'
    else
        fail "sha256sum or shasum is required to create SHA256SUMS."
    fi
}

next_release_tag() {
    local highest_version=0
    local reference
    local references
    local tag
    local version

    references="$(
        gh api \
            --paginate \
            "repos/${fixture_repository}/git/matching-refs/tags/v" \
            --jq '.[].ref'
    )" || fail "Could not retrieve existing tags from ${fixture_repository}."

    while IFS= read -r reference; do
        tag="${reference##*/}"
        if [[ "$tag" =~ ^v([0-9]+)$ ]]; then
            version="${BASH_REMATCH[1]}"
            if (( 10#$version > highest_version )); then
                highest_version=$((10#$version))
            fi
        fi
    done <<< "$references"

    printf 'v%d\n' "$((highest_version + 1))"
}

while (( $# > 0 )); do
    case "$1" in
        --dry-run)
            dry_run=true
            shift
            ;;
        --yes)
            assume_yes=true
            shift
            ;;
        -h|--help)
            usage
            exit 0
            ;;
        *)
            fail "Unknown option '$1'."
            ;;
    esac
done

command -v gh >/dev/null 2>&1 || fail "GitHub CLI (gh) is required to publish fixtures."
command -v gzip >/dev/null 2>&1 || fail "gzip is required to validate fixture archives."
gh auth status >/dev/null 2>&1 || fail "GitHub CLI is not authenticated. Run 'gh auth login' first."

temporary_directory="$(mktemp -d)"
trap 'rm -rf -- "$temporary_directory"' EXIT
readonly checksum_file="${temporary_directory}/SHA256SUMS"

declare -a archive_paths=()
for archive in "${ARCHIVES[@]}"; do
    archive_path="${ARCHIVE_DIRECTORY}/${archive}"
    [[ -f "$archive_path" ]] || fail "Required fixture is missing: ${archive_path}"

    printf 'Validating %s...\n' "$archive"
    gzip --test "$archive_path" || fail "Fixture is not a valid gzip archive: ${archive_path}"
    printf '%s  %s\n' "$(sha256 "$archive_path")" "$archive" >> "$checksum_file"
    archive_paths+=("$archive_path")
done

release_tag="$(next_release_tag)"

printf '\nRepository: %s\n' "$fixture_repository"
printf 'Release:    %s\n' "$release_tag"
printf 'Assets:\n'
printf '  %s\n' "${ARCHIVES[@]}" "SHA256SUMS"

if [[ "$dry_run" == true ]]; then
    printf '\nDry run complete; no release was created.\n'
    exit 0
fi

if [[ "$assume_yes" != true ]]; then
    if [[ ! -t 0 ]]; then
        fail "Interactive confirmation is unavailable. Re-run with --yes to publish."
    fi

    read -r -p "Publish ${release_tag} as the latest fixture release? [y/N] " confirmation
    [[ "$confirmation" == "y" || "$confirmation" == "Y" ]] || {
        printf 'Release cancelled.\n'
        exit 0
    }
fi

gh release create "$release_tag" \
    "${archive_paths[@]}" \
    "$checksum_file" \
    --repo "$fixture_repository" \
    --title "Spark database fixtures ${release_tag}" \
    --notes "MongoDB fixtures for Spark's supported FHIR versions." \
    --latest

printf 'Published %s: https://github.com/%s/releases/tag/%s\n' \
    "$release_tag" "$fixture_repository" "$release_tag"
