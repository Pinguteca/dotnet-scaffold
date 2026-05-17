# Load tests

Starter k6 scripts for the scaffold's services. Run them against a
running ApiService (and FakeIdp if you have it wired locally; see
`dev/`).

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

## Run

```
mise exec -- k6 run k6/smoke.js
mise exec -- k6 run k6/echo.js
mise exec -- k6 run k6/stress.js
```

## Environment

| Variable  | Default               | Notes                              |
|-----------|-----------------------|------------------------------------|
| `API_URL` | `localhost:7301`      | gRPC host:port for echo.js/stress.js |
| `API_URL` | `https://localhost:7301` | smoke.js prepends `/health/live`   |

If your ApiService requires auth, extend the scripts with an
opening token-fetch against the configured IdP and stash the
Bearer in `client.connect`'s metadata.
