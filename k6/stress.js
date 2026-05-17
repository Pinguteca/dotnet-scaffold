// Stress profile: ramp gRPC load beyond the sustained target until
// the service breaks or saturates. Useful for surfacing breaker /
// retry behaviour under back-pressure and finding the hardware
// ceiling on the dev box.
//
// Peaks at 3000 VUs; tighten or relax based on your machine. The
// only assertion is that error rate stays under 5% on the way up;
// at the spike, anything goes.
//
// Run: mise exec -- k6 run k6/stress.js

import grpc from 'k6/net/grpc';
import { check } from 'k6';

const client = new grpc.Client();
client.load(['../proto'], 'scaffoldprojectname/v1/echo.proto');

const API_URL = __ENV.API_URL || 'localhost:7301';

export const options = {
    stages: [
        { duration: '1m', target: 500 },
        { duration: '2m', target: 1500 },
        { duration: '2m', target: 3000 },
        { duration: '1m', target: 0 },
    ],
    thresholds: {
        // Allow significant degradation at the spike but flag a
        // total collapse during the ramp.
        'checks{phase:ramp}': ['rate>0.95'],
    },
    insecureSkipTLSVerify: true,
};

// Per-VU connection state. Module scope persists across iterations
// for the same VU and is isolated between VUs.
let connected = false;

export default function () {
    if (!connected) {
        try {
            client.connect(API_URL, { plaintext: false, timeout: '30s' });
            connected = true;
        } catch (e) {
            // Ramp-up race: heavy concurrent dial can outrun the
            // server's backlog. Skip and retry on the next iteration.
            return;
        }
    }

    let response;
    try {
        response = client.invoke(
            'scaffoldprojectname.v1.EchoService/Echo',
            { message: `stress vu ${__VU}` },
        );
    } catch (e) {
        if (/no gRPC connection/i.test(e.message || '')) {
            connected = false;
            return;
        }
        throw e;
    }

    check(
        response,
        { 'status OK': (r) => r && r.status === grpc.StatusOK },
        { phase: 'ramp' },
    );
}
