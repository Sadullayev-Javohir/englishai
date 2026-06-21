import http from "k6/http";
import { check } from "k6";

export const options = { vus: Number(__ENV.VUS || 100), iterations: Number(__ENV.ITERATIONS || 500) };
const base = __ENV.BASE_URL || "http://localhost:5000";
const token = __ENV.TEST_JWT;
const sessionId = __ENV.SESSION_ID;
const mode = __ENV.MODE || "repeated";

export default function () {
  const unique = `${__VU}-${__ITER}`;
  const question = mode === "repeated" ? "Present Perfect qachon ishlatiladi?" : `grammar valid savol ${unique}`;
  const response = http.post(`${base}/api/assistant/sessions/${sessionId}/messages/stream`, JSON.stringify({
    question,
    focusText: "",
    clientRequestId: crypto.randomUUID(),
  }), { headers: { Authorization: `Bearer ${token}`, "Content-Type": "application/json", Accept: "text/event-stream" } });
  check(response, { "200": (r) => r.status === 200, "non-empty done": (r) => r.body.includes("event: done") && r.body.includes("message") });
}
