# Sandbox image for compiling student-submitted Arduino/ESP32 sketches.
#
# Build with:
# `docker build -t stem-arduino-cli-sandbox:latest -f docker/simulation-compile-sandbox/Dockerfile .`
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
    && apt-get install -y --no-install-recommends ca-certificates curl python3 python3-pip \
    && rm -rf /var/lib/apt/lists/*

# esp32:esp32@2.0.17's esptool_py@4.5.1 merge_bin step shells out to a raw
# esptool.py script (not the self-contained PyInstaller binary the 3.x core's
# tool version uses) — needs pyserial on the system Python to even import,
# despite never touching a real serial port for merge_bin. Confirmed via
# direct repro: compile fails with "ModuleNotFoundError: No module named
# 'serial'" without this (see VIRTUAL_LAB_PLAN.md 8.13).
RUN pip3 install --break-system-packages --no-cache-dir pyserial

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
# Pinned to 2.0.17 (NOT latest) — the IDF5-based 3.x line (verified: 3.3.10)
# crashes non-deterministically under qemu-system-xtensa with
# "Guru Meditation Error: Core panic'ed (Cache error)" (repro'd even on plain
# Blink with zero custom instrumentation, sometimes before setup() even runs).
# 2.0.17 (IDF4.4-based) verified crash-free across 3 independent 12s runs
# under the exact same QEMU image/flags — see VIRTUAL_LAB_PLAN.md 8.13.
RUN arduino-cli core install esp32:esp32@2.0.17

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
