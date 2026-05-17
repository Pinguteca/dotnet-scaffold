// One-shot smoke against the scaffold's HTTP liveness endpoint.
// Useful as a CI gate before kicking off a real load run.
//
// Run: mise exec -- k6 run k6/smoke.js

import http from 'k6/http';
import { check } from 'k6';

const API_URL = __ENV.API_URL || 'https://localhost:7301';

export const options = {
    vus: 1,
    iterations: 1,
    insecureSkipTLSVerify: true,
};

export default function () {
    const response = http.get(`${API_URL}/health/live`);
    check(response, {
        'liveness 200': (r) => r.status === 200,
    });
}
