# Building Docker Images Locally

The CI workflow (`.github/workflows/docker_image_linux.yml`) builds multi-architecture Docker
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
  --file .docker/linux/Spark.Dockerfile \
  .
```

**Mongo image:**

```bash
docker buildx build \
  --platform linux/amd64,linux/arm64 \
  --file .docker/linux/Mongo.Dockerfile \
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
  --file .docker/linux/Spark.Dockerfile \
  --output type=oci,dest=/tmp/spark-image.tar \
  .
```

To load a **single** platform into the local daemon (e.g. for running locally):

```bash
docker buildx build \
  --platform linux/amd64 \
  --file .docker/linux/Spark.Dockerfile \
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
  --file .docker/linux/Spark.Dockerfile \
  --cache-from type=local,src=/tmp/buildx-cache \
  --cache-to   type=local,dest=/tmp/buildx-cache,mode=max \
  .
```
