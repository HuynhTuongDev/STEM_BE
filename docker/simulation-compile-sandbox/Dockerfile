# Sandbox image for compiling student-submitted Arduino/ESP32 sketches.
#
# NOT YET VERIFIED TO BUILD — Docker Desktop was unavailable in the environment
# this was authored in (see VIRTUAL_LAB_PLAN.md, item 0.B1/3.B). Build and smoke-test
# with `docker build -t stem-arduino-cli-sandbox:latest -f docker/simulation-compile-sandbox/Dockerfile .`
# before relying on it.
#
# Design: all board cores/toolchains are baked in at image-build time (when network
# access is available). At compile time the container runs with --network none, so
# nothing at runtime can reach the internet, and arduino-cli never needs to touch
# its data directory (read-only at runtime) — only the tmpfs build/output scratch dirs.

FROM debian:bookworm-slim

ARG ARDUINO_CLI_VERSION=1.1.1
ENV ARDUINO_DIRECTORIES_DATA=/opt/arduino-data \
    ARDUINO_DIRECTORIES_DOWNLOADS=/opt/arduino-data/staging \
    ARDUINO_DIRECTORIES_USER=/opt/arduino-user \
    DEBIAN_FRONTEND=noninteractive

RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates curl python3 \
    && rm -rf /var/lib/apt/lists/*

RUN curl -fsSL https://raw.githubusercontent.com/arduino/arduino-cli/master/install.sh \
        | BINDIR=/usr/local/bin sh -s -- v${ARDUINO_CLI_VERSION}

RUN mkdir -p /opt/arduino-data /opt/arduino-user \
    && arduino-cli config init \
        --additional-urls https://raw.githubusercontent.com/espressif/arduino-esp32/gh-pages/package_esp32_index.json \
    && arduino-cli core update-index

# Split into separate layers (and retried individually below) so a transient
# download failure on one core doesn't discard a successful download of the
# other — the esp32 core alone is ~300+ MiB and upstream tool mirrors have
# been observed to intermittently 500.
RUN arduino-cli core install arduino:avr
RUN arduino-cli core install esp32:esp32

# Dedicated non-root user the container always runs as (uid/gid pinned so the
# API's `docker run --user` flag can match without a runtime lookup).
RUN groupadd -g 10001 builder \
    && useradd -m -u 10001 -g builder -s /usr/sbin/nologin builder \
    && chown -R builder:builder /opt/arduino-data /opt/arduino-user

# Everything needed to compile is now baked into image layers (read-only at
# runtime via `docker run --read-only`); only /workspace/build and /tmp need to
# be writable, and those are supplied as tmpfs mounts by the caller, not baked here.
RUN mkdir -p /workspace && chown builder:builder /workspace

USER builder
WORKDIR /workspace
ENTRYPOINT ["arduino-cli"]
