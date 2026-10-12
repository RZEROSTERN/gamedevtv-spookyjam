#!/usr/bin/env node
// Optional QA tool: Node 22+ and a locally forwarded Chrome DevTools port.
// Reads the Web player's built-in metrics; it never sends gameplay input.
import fs from 'node:fs/promises';
import path from 'node:path';
import { parseArgs } from 'node:util';

const { values } = parseArgs({ options: {
  port: { type: 'string', default: '9222' },
  url: { type: 'string', default: 'http://127.0.0.1:8082/' },
  seconds: { type: 'string', default: '300' },
  output: { type: 'string', default: 'Builds/Verification/pixel10-metrics.jsonl' }
}});
const seconds = Number(values.seconds);
if (!Number.isFinite(seconds) || seconds < 1 || seconds > 3600) throw new Error('Seconds must be between 1 and 3600.');
const pages = await (await fetch(`http://127.0.0.1:${values.port}/json/list`)).json();
const page = pages.find(p => p.type === 'page' && p.url === values.url);
if (!page) throw new Error('Open the exact game URL in Chrome before profiling.');
const socket = new WebSocket(page.webSocketDebuggerUrl);
await new Promise((resolve, reject) => {
  const timer = setTimeout(() => reject(new Error('Connection timed out.')), 8000);
  socket.addEventListener('open', () => { clearTimeout(timer); resolve(); }, { once: true });
  socket.addEventListener('error', () => { clearTimeout(timer); reject(new Error('DevTools connection failed.')); }, { once: true });
});
let next = 0;
const pending = new Map();
socket.addEventListener('message', event => {
  const response = JSON.parse(event.data);
  const request = pending.get(response.id);
  if (!request) return;
  pending.delete(response.id);
  clearTimeout(request.timer);
  if (response.error) request.reject(new Error(JSON.stringify(response.error)));
  else request.resolve(response.result);
});
async function call(method, params) {
  return await new Promise((resolve, reject) => {
    const id = ++next;
    const timer = setTimeout(() => { pending.delete(id); reject(new Error('Metric request timed out.')); }, 8000);
    pending.set(id, { resolve, reject, timer });
    socket.send(JSON.stringify({ id, method, params }));
  });
}
await fs.mkdir(path.dirname(values.output), { recursive: true });
const file = await fs.open(values.output, 'w');
const started = performance.now();
let samples = 0;
try {
  while ((performance.now() - started) / 1000 < seconds) {
    const result = await call('Runtime.evaluate', {
      expression: '({visible:document.visibilityState,cssWidth:canvas.clientWidth,cssHeight:canvas.clientHeight,canvasWidth:canvas.width,canvasHeight:canvas.height,metrics:game.GetMetricsInfo()})',
      returnByValue: true
    });
    if (result.exceptionDetails || !result.result?.value?.metrics) throw new Error('The loaded Unity player did not expose metrics.');
    await file.write(JSON.stringify({ elapsedSeconds: (performance.now() - started) / 1000, ...result.result.value }) + '\n');
    samples++;
    await new Promise(resolve => setTimeout(resolve, 1000));
  }
  console.log(JSON.stringify({ samples, durationSeconds: (performance.now() - started) / 1000, output: values.output }));
} finally {
  await file.close();
  socket.close();
}
