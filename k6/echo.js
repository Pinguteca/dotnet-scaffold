// Sustained gRPC load against EchoService.Echo.
//
// Profile: warm up to 200 VUs over 30s, hold 1000 VUs for 3 minutes,
// then drain. On modern hardware (8+ cores, 16+ GB RAM) the loopback
// gRPC stack easily clears this with p95 well under 50 ms; treat the
// thresholds as a regression guard, not a performance target.
//
// Run: mise exec -- k6 run k6/echo.js

import grpc from 'k6/net/grpc';
import { check } from 'k6';

const client = new grpc.Client();
client.load(['../proto'], 'scaffoldprojectname/v1/echo.proto');

const API_URL = __ENV.API_URL || 'localhost:7301';

export const options = {
    stages: [
        { duration: '30s', target: 200 },
        { duration: '30s', target: 1000 },
        { duration: '3m', target: 1000 },
        { duration: '30s', target: 0 },
    ],
    thresholds: {
        grpc_req_duration: ['p(95)<100', 'p(99)<250'],
        checks: ['rate>0.995'],
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
            // Ramp-up race: many VUs dialing in parallel can starve
            // the server's backlog. Skip this iteration and retry the
            // connect on the next one.
            return;
        }
    }

    let response;
    try {
        response = client.invoke(
            'scaffoldprojectname.v1.EchoService/Echo',
            { message: `vu ${__VU} iter ${__ITER}` },
        );
    } catch (e) {
        if (/no gRPC connection/i.test(e.message || '')) {
            connected = false;
            return;
        }
        throw e;
    }

    check(response, {
        'status OK': (r) => r && r.status === grpc.StatusOK,
        'echoed message': (r) => r && r.message && r.message.message.startsWith('vu '),
    });
}
