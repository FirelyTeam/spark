# Building Docker Images Locally

The CI workflow (`.github/workflows/docker_image_linux_r4.yml`) builds multi-architecture Docker
images (`linux/amd64` and `linux/arm64`) using Docker Buildx and pushes them to DockerHub on
every release. This document explains how to replicate that build on your local machine.

## Prerequisites

- [Docker](https://docs.docker.com/get-docker/) v20+ (tested with v28)
- Docker Buildx v0.10+ (bundled with Docker Desktop and recent Docker Engine installs)

Verify both are available:

```bash
docker --version
docker buildx version
```

## Download the database fixtures

The Mongo images include example databases that are published separately as release assets in
[`IncendiLabs/spark-database-fixtures`](https://github.com/IncendiLabs/spark-database-fixtures).
The archives are not stored in this Git repository, so download them before building a Mongo image.

Run the download script from the repository root and pass the FHIR version you intend to build:

```bash
./.docker/linux/download-database-fixtures.sh r4
```

Supported versions are `stu3`, `r4`, `r4b`, `r5`, and `r6`. To download every fixture, omit the
version:

```bash
./.docker/linux/download-database-fixtures.sh
```

By default, the script downloads from the latest fixture release. To reproduce a build using a
specific fixture release, set `FIXTURE_RELEASE` to its tag:

```bash
FIXTURE_RELEASE=v1 ./.docker/linux/download-database-fixtures.sh r4
```

The script downloads `SHA256SUMS` from the selected release and verifies every archive before
installing it in `.docker/linux/`. Existing archives with the expected checksum are reused, while
invalid files are replaced using a temporary download. A failed download or checksum mismatch
leaves no partial archive at the destination.

The source repository and destination directory can also be overridden when needed:

```bash
FIXTURE_REPOSITORY=IncendiLabs/spark-database-fixtures \
FIXTURE_DESTINATION="$PWD/database-fixtures" \
  ./.docker/linux/download-database-fixtures.sh r4
```

## Publish database fixtures

Project maintainers can publish updated archives with the upload script. Before running it, install
the [GitHub CLI](https://cli.github.com/) and authenticate with an account that can create releases
in `IncendiLabs/spark-database-fixtures`:

```bash
gh auth login
gh auth status
```

Place the updated `stu3.archive.gz`, `r4.archive.gz`, `r4b.archive.gz`, `r5.archive.gz`, and
`r6.archive.gz` files in `.docker/linux/`. Preview the publication without creating a release:

```bash
./.docker/linux/upload-database-fixtures.sh --dry-run
```

The script verifies that every required file exists and is a valid gzip archive, generates a
`SHA256SUMS` asset, and examines the repository's existing sequential tags. If `v1` through `v3`
exist, for example, the next release will be `v4`. The preview lists the selected repository, tag,
and assets but does not create the tag or release.

After reviewing the preview, publish interactively:

```bash
./.docker/linux/upload-database-fixtures.sh
```

The script shows the same release summary and asks for confirmation before creating the release.
The new release is marked as the latest fixture release, so subsequent downloads that do not pin
`FIXTURE_RELEASE` will select it automatically.

For a trusted non-interactive environment, use `--yes` to skip the confirmation prompt:

```bash
./.docker/linux/upload-database-fixtures.sh --yes
```

To publish to a different repository, override `FIXTURE_REPOSITORY`:

```bash
FIXTURE_REPOSITORY=example/spark-database-fixtures \
  ./.docker/linux/upload-database-fixtures.sh --dry-run
```

Published releases are immutable inputs. If any fixture changes, run the script again to create the
next release instead of replacing assets in an existing release.

## Step 1 — Enable ARM64 emulation via QEMU

QEMU binfmt handlers allow your AMD64 machine to build ARM64 images. This is a one-time setup
per machine (or after a reboot on some systems):

```bash
docker run --privileged --rm tonistiigi/binfmt --install all
```

Confirm `linux/arm64` is listed under `supported`:

```bash
docker run --privileged --rm tonistiigi/binfmt
```

## Step 2 — Create a multi-platform Buildx builder

The default `docker` driver does not support multi-platform builds. Create a builder using the
`docker-container` driver instead:

```bash
docker buildx create --name multiarch --driver docker-container --use
docker buildx inspect --bootstrap
```

The output should list both `linux/amd64` and `linux/arm64` under `Platforms`.

## Step 3 — Build the images

Run the builds from the repository root. Omitting `--push` keeps the result in the build cache
only — this is sufficient to verify the build succeeds.

**Spark image:**

```bash
docker buildx build \
  --platform linux/amd64,linux/arm64 \
  --file .docker/linux/Spark.R4.Dockerfile \
  .
```

**Mongo image:**

Download the matching fixture first if you have not already done so:

```bash
./.docker/linux/download-database-fixtures.sh r4
```

```bash
docker buildx build \
  --platform linux/amd64,linux/arm64 \
  --file .docker/linux/Mongo.R4.Dockerfile \
  .
```

> **Note:** You will see the following warning at the end of each build — this is expected when
> not using `--push` or `--load`:
> ```
> WARNING: No output specified with docker-container driver. Build result will only remain in the build cache.
> ```

### Saving the image locally (optional)

Multi-platform builds cannot be loaded into the local Docker daemon directly (a `docker` daemon
limitation). To save the image as an OCI archive instead:

```bash
docker buildx build \
  --platform linux/amd64,linux/arm64 \
  --file .docker/linux/Spark.R4.Dockerfile \
  --output type=oci,dest=/tmp/spark-image.tar \
  .
```

To load a **single** platform into the local daemon (e.g. for running locally):

```bash
docker buildx build \
  --platform linux/amd64 \
  --file .docker/linux/Spark.R4.Dockerfile \
  --load \
  --tag spark:local \
  .
```

## Step 4 — Clean up

Remove the builder when you are done:

```bash
docker buildx rm multiarch
```

## Differences from CI

| Aspect | GitHub Actions CI | Local build |
|---|---|---|
| Cache | `type=gha` (GitHub Actions cache) | None (or `type=local`) |
| Secrets | Stored in repository settings | Not needed for build-only verification |
| Push | Pushes to DockerHub on release | Omit `--push` for local verification |
| Trigger | `on: release: published` | Run manually |

### Using a local cache

To speed up repeated local builds, replace the GHA cache with a local directory cache:

```bash
docker buildx build \
  --platform linux/amd64,linux/arm64 \
  --file .docker/linux/Spark.R4.Dockerfile \
  --cache-from type=local,src=/tmp/buildx-cache \
  --cache-to   type=local,dest=/tmp/buildx-cache,mode=max \
  .
```
