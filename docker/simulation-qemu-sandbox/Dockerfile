# Sandbox image for running compiled ESP32 firmware (from SimulationCompileService's
# Docker sandbox) inside QEMU — separate from the compile sandbox image on purpose
# (different tool, different attack surface: this one executes arbitrary machine code
# under emulation rather than compiling source text).
#
# Design mirrors ../simulation-compile-sandbox/Dockerfile: everything needed is baked
# in at image-build time (network available then); at run time the container gets
# --network none and only ever sees the firmware bind-mounted read-only — no data
# directory, no state directory, nothing else to touch.
#
# QEMU binary is Espressif's official prebuilt release (not built from source, not
# apt/Docker-Hub — verified boot-tested against real compiled Arduino-ESP32 firmware
# during feasibility research: real ROM bootloader log, delay() timed at exactly
# 1.000s real wall-clock per tick). Pinned by release tag for reproducibility.

FROM debian:bookworm-slim

ARG QEMU_RELEASE_TAG=esp-develop-9.2.2-20260417
ARG QEMU_ASSET=qemu-xtensa-softmmu-esp_develop_9.2.2_20260417-x86_64-linux-gnu.tar.xz
ENV DEBIAN_FRONTEND=noninteractive

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        ca-certificates curl xz-utils \
        libpixman-1-0 libsdl2-2.0-0 libslirp0 libglib2.0-0 \
    && rm -rf /var/lib/apt/lists/*

RUN mkdir -p /opt/qemu-download \
    && curl -fsSL -o /opt/qemu-download/qemu.tar.xz \
        "https://github.com/espressif/qemu/releases/download/${QEMU_RELEASE_TAG}/${QEMU_ASSET}" \
    && tar -xf /opt/qemu-download/qemu.tar.xz -C /opt \
    && rm -rf /opt/qemu-download

# Dedicated non-root user the container always runs as (same uid/gid scheme as the
# compile sandbox so `docker run --user` doesn't need a runtime lookup either).
RUN groupadd -g 10001 runner \
    && useradd -m -u 10001 -g runner -s /usr/sbin/nologin runner \
    && mkdir -p /workspace && chown -R runner:runner /workspace

ENV LD_LIBRARY_PATH=/opt/qemu/lib/x86_64-linux-gnu:/opt/qemu/lib
ENV PATH=/opt/qemu/bin:$PATH

USER runner
WORKDIR /workspace
ENTRYPOINT ["qemu-system-xtensa"]
