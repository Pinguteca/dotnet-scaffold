# Load tests

Starter k6 scripts for the scaffold's services. Run them
standalone via mise or let the Aspire AppHost orchestrate them as
a containerised resource alongside the API.

## Profiles

| Script    | Purpose                                | Peak VUs | Duration |
|-----------|----------------------------------------|----------|----------|
| smoke.js  | One-shot sanity check on /health/live  | 1        | seconds  |
| echo.js   | Sustained gRPC load on EchoService.Echo | 1000    | ~5 min   |
| stress.js | Ramp until the service saturates        | 3000    | ~6 min   |

Targets assume a modern dev box (8+ cores, 16+ GB RAM) and a
loopback gRPC connection; the API should clear `echo.js` at p95
under 50 ms. Tune the stage targets and thresholds to whatever
hardware you actually deploy on.

## Run standalone via mise

```
mise run loadtest:smoke    # alias: lts
mise run loadtest:load     # alias: ltl
mise run loadtest:stress   # alias: ltr
```

Override the target with `API_URL` (defaults to `localhost:7301`).

## Run inside the AppHost

Opt-in: set `EnableK6=true` in `appsettings.json`, environment, or
user secrets. With the flag on, the AppHost adds a `k6` resource
that bind-mounts this directory and the workspace `/proto` tree
into the official `grafana/k6` container. The default entrypoint
is `echo.js`; swap the `WithScript("/scripts/echo.js")` call in
`src/AppHost/AppHost.cs` to switch profiles. Metrics flow into the
dashboard's OTel pipeline alongside the rest of the resources.

The bind mount requires your container runtime to have host file
access for the repo path:

- Docker Desktop: Settings -> Resources -> File Sharing -> add the
  repo's drive (e.g. `G:\`).
- Podman rootless on Windows: re-init the machine with
  `podman machine init --volume <repoRoot>:<repoRoot>`.

Off by default so a fresh consumer who has not configured their
container runtime can still run the AppHost. The standalone mise
tasks above work regardless.

## Environment

| Variable  | Default               | Notes                              |
|-----------|-----------------------|------------------------------------|
| `API_URL` | `localhost:7301`      | gRPC host:port for echo.js/stress.js |
| `API_URL` | `https://localhost:7301` | smoke.js prepends `/health/live`   |

When the script runs under Aspire, `API_URL` is injected
automatically from the apiservice endpoint reference.

If your ApiService requires auth, extend the scripts with an
opening token-fetch against the configured IdP and stash the
Bearer in `client.connect`'s metadata.
