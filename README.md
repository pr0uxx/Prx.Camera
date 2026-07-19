# Prx.Camera

A .NET 10 Native AOT application, distributed as self-contained binaries for:

- Raspberry Pi 3 (ARM64)
- Raspberry Pi 4 (ARM64)
- Linux x64

---

## For Users

### Requirements

- One of the supported platforms above, running a 64-bit Linux OS
- Root access (either logged in as root, or a user with `sudo`)
- `curl` and `file` (present on virtually every Linux distro by default)
- Internet access to `github.com` and `api.github.com`

### Installing

Download and run the installer script:

```bash
curl -fsSL https://raw.githubusercontent.com/pr0uxx/Prx.Camera/main/Prx.Camera/prx-camera-installer.sh -o prx-camera-installer.sh
chmod +x prx-camera-installer.sh
sudo ./prx-camera-installer.sh
```

The installer automatically:

1. Detects your platform (Pi 3, Pi 4, or Linux x64)
2. Downloads the matching binary from the **latest** GitHub release, along with its checksum
3. Verifies the download against the checksum before installing anything
4. Installs required runtime dependencies (`libicu-dev`, `zlib1g`, `libssl-dev`, `libpcap-dev`)
5. Installs the binary to `/usr/local/bin/Prx.Camera`
6. Installs and starts `prx-camera.service` (systemd), enabled to start on boot

Check that it's running:

```bash
sudo systemctl status prx-camera.service
```

### Updating

The installer always fetches whatever is currently tagged as the **latest** GitHub release. To update, just run the exact same command again:

```bash
sudo ./prx-camera-installer.sh
```

It will re-download the newest binary, re-verify its checksum, replace the installed binary, and restart the service. This is safe to re-run any time — there's no separate "update" script.

To see what the latest available version is before updating, check the repo's [Releases page](../../releases).

### Uninstalling

There's currently no uninstall script. To remove Prx.Camera manually:

```bash
sudo systemctl stop prx-camera.service
sudo systemctl disable prx-camera.service
sudo rm /etc/systemd/system/prx-camera.service
sudo systemctl daemon-reload
sudo rm /usr/local/bin/Prx.Camera
```

### Troubleshooting

| Symptom | Likely cause |
|---|---|
| `No release asset found for platform '<platform>'` | The latest release doesn't include a build for your platform — check the Releases page, or the release may still be running. |
| `Checksum mismatch!` | The download was corrupted or interrupted. Re-run the installer. If it persists, something's wrong with the release itself — open an issue. |
| `Not a valid ARM64 ELF` / `Not a valid x86-64 ELF` | Usually the same as above (corrupted download), or platform detection picked the wrong asset. |
| Service won't start | Check logs: `sudo journalctl -u prx-camera.service -f` |

---

## For Developers

### Repository layout

```
.
├── Prx.Camera/
│   ├── Program.cs
│   ├── Prx.Camera.csproj
│   └── prx-camera-installer.sh
├── .github/
│   └── workflows/
│       ├── release.yml           # orchestrator - human-triggered
│       ├── publish-release.yml   # reusable - aggregates artifacts, creates the GitHub release
│       └── build/
│           ├── build-rpi-3.yml
│           ├── build-rpi-4.yml
│           └── build-linux-x64.yml
```

### How a release works, end to end

1. Push a tag matching strict semver, e.g. `1.2.3` (no leading `v`).
2. From the **Actions** tab, run `release` (`workflow_dispatch`), picking that tag as the ref. There are no manual inputs — the tag itself is the only source of truth for the version.
3. `validate` job checks: the ref is actually a tag, the tag is valid semver, and no GitHub release already exists for that tag (fails fast if one does — you'll need to delete it or cut a new tag).
4. `discover` job lists every `build-*.yml` file in `.github/workflows/build/`.
5. `build` job dispatches each discovered workflow via the GitHub API (`benc-uk/workflow-dispatch@v1`), waits for it to finish, and records the run ID it produced.
6. `collect` job bundles all the run IDs into one list.
7. `publish` job (a call to `publish-release.yml`) downloads each platform's artifact by run ID, generates a `.sha256` checksum per binary, optionally generates an SBOM, and creates the GitHub release with everything attached.

Each release therefore contains, per platform: `prx-camera-<platform>-<version>` (the binary) and `prx-camera-<platform>-<version>.sha256` (its checksum). **The installer's platform-detection logic depends on this exact naming convention** — if you change it, update `prx-camera-installer.sh` too.

### Versioning rules

- Tags are the only source of truth — there's no automatic semver inference.
- Tag name = release version = the value baked into the binary via `-p:Version=`.
- Format must be strict `X.Y.Z` — no `v` prefix, no pre-release/build metadata suffixes. If you need those later, the regex to loosen is in `release.yml`'s `validate` job.

### Adding a new build platform

This is the one thing designed to require the least ceremony:

1. Copy an existing file in `.github/workflows/build/` (e.g. `build-linux-x64.yml`) as your starting point.
2. Name it `build-<platform>.yml` — the orchestrator discovers anything matching `build-*.yml` in that folder, so **no edits to `release.yml` are needed**.
3. Keep the same shape:
    - `on: workflow_dispatch:` with a required `version` string input
    - Checkout → `setup-dotnet` (10.0.x) → `dotnet restore` → `dotnet publish -r <your-RID> -p:PublishProfile=ProductionAOT -p:Version=${{ inputs.version }} --self-contained true`
    - Upload the result as an artifact named exactly `prx-camera-<platform>-${{ inputs.version }}`
4. If the target needs cross-compilation (anything other than `linux-x64` on an x64 runner), you'll likely need a toolchain install step — see the "Install ARM64 cross-compilation toolchain" step in `build-rpi-3.yml` as a template, adjusting package names for your target arch.
5. If this is a genuinely new CPU architecture (not just a new Pi model sharing `linux-arm64`), also update `detect_platform()` in `prx-camera-installer.sh` so installs on that hardware pick the right asset.

> Note: Pi 3 and Pi 4 currently both map to the same .NET RID (`linux-arm64`), so `build-rpi-3.yml` and `build-rpi-4.yml` produce identical binaries today. They're kept as separate files so there's a natural seam if Pi-4-specific flags or drivers are ever needed.

### CI/CD internals worth knowing

- **Repo permission setting matters.** For the `build` job to dispatch other workflows and for `publish` to create releases, your repo's Actions setting (Settings → Actions → General → Workflow permissions) needs to allow the default `GITHUB_TOKEN` read/write access. The workflows request only the specific `actions:write` / `contents:write` scopes they need, but those requests are capped by whatever the repo-level default allows.
- **Third-party action:** `benc-uk/workflow-dispatch@v1` is what makes "dispatch a workflow file and wait for it to finish" possible — GitHub has no first-party equivalent for dynamically-discovered (non-reusable) workflows. If your org restricts third-party actions, it'll need to be allow-listed.
- **Cross-run artifacts:** because each build runs as its own separate workflow run (not a job in the orchestrator's run), artifacts can't be pulled with a plain `actions/download-artifact` — `publish-release.yml` uses `gh run download <run-id>` instead, which is why it needs the run IDs collected in step 5 above.
- **SBOM generation is a stub.** The `Generate SBOM (optional)` step in `publish-release.yml` soft-fails if no `sbom-tool` is found on the runner — it's a placeholder. Wire in whichever SBOM generator you standardize on and confirm its exact CLI invocation.

### Testing the installer locally

The installer supports two non-production modes via the `MODE` env var, useful for testing without cutting a real release:

```bash
# Install from a local binary file instead of GitHub
MODE="LOCAL FILE" LOCAL_FILE=/path/to/binary sudo -E ./prx-camera-installer.sh

# Install from a local mock HTTP server (spins one up automatically)
MODE="LOCAL HTTP" sudo -E ./prx-camera-installer.sh
```

Note `-E` is needed with `sudo` so the `MODE`/`LOCAL_FILE` env vars survive the privilege switch. Both modes skip dependency installation and checksum verification (there's nothing to verify against), and use a dummy ELF compiled for whatever architecture you're running the test on — so `LOCAL FILE`/`LOCAL HTTP` testing is only meaningful for exercising the *installer's logic*, not for validating an actual cross-compiled ARM64/x64 binary.