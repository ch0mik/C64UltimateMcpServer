import { build } from 'esbuild';
import { chromium } from 'playwright-core';
import { createServer } from 'node:http';
import { access } from 'node:fs/promises';
import assert from 'node:assert/strict';

const endpoint = process.env.MCP_TEST_URL ?? 'http://127.0.0.1:18085';
let deviceActions = 0;
const device = createServer((request, response) => {
  response.setHeader('Content-Type', 'application/json');
  if (request.method !== 'GET') deviceActions++;
  response.end(request.url === '/v1/version' ? '{"version":"test-0.1","errors":[]}' : '{"drives":[],"errors":[]}');
});
await new Promise(resolve => device.listen(Number(process.env.C64_TEST_DEVICE_PORT ?? '18086'), '127.0.0.1', resolve));
let requestId = 0;
async function rpc(method, params = {}) {
  params._meta = {
    'io.modelcontextprotocol/protocolVersion': '2026-07-28',
    'io.modelcontextprotocol/clientInfo': { name: 'c64-reference-host', version: '2.0.4' },
    'io.modelcontextprotocol/clientCapabilities': { elicitation: { form: {} }, extensions: { 'io.modelcontextprotocol/ui': { mimeTypes: ['text/html;profile=mcp-app'] } } }
  };
  const headers = { 'Content-Type': 'application/json', Accept: 'application/json, text/event-stream', 'MCP-Protocol-Version': '2026-07-28', 'Mcp-Method': method };
  if (params.name ?? params.uri) headers['Mcp-Name'] = params.name ?? params.uri;
  const response = await fetch(endpoint, { method: 'POST', headers, body: JSON.stringify({ jsonrpc: '2.0', id: ++requestId, method, params }), signal: AbortSignal.timeout(10000) });
  const text = await response.text();
  assert(response.ok, text);
  const json = text.startsWith('{') ? JSON.parse(text) : text.split('\n').filter(s => s.startsWith('data:')).map(s => JSON.parse(s.slice(5))).find(s => s.id);
  assert(!json.error, JSON.stringify(json.error));
  return json.result;
}

const artifact = (await rpc('tools/call', { name: 'ultimate_program_workflow', arguments: { language: 'basic', source: '10 PRINT "<img onerror=evil()>"' } })).structuredContent.artifact;
const bundle = (await build({ entryPoints: ['host-test-entry.mjs'], bundle: true, format: 'iife', write: false })).outputFiles[0].text;
const server = createServer((request, response) => {
  if (request.url === '/host.js') { response.setHeader('Content-Type', 'text/javascript'); response.end(bundle); }
  else response.end('<!doctype html><script src="/host.js"></script><iframe id="app" sandbox="allow-scripts allow-same-origin"></iframe>');
});
await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
const hostUrl = `http://127.0.0.1:${server.address().port}`;
let executablePath = process.env.C64_TEST_BROWSER;
if (!executablePath) {
  for (const path of ['C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe', 'C:/Program Files/Google/Chrome/Application/chrome.exe']) {
    try { await access(path); executablePath = path; break; } catch {}
  }
}
let browser;
try {
  browser = await chromium.launch({ headless: true, ...(executablePath ? { executablePath } : {}) });
  console.log(`Reference host: @modelcontextprotocol/ext-apps 2.0.4; browser ${browser.version()}`);
  for (const mode of ['dashboard', 'preview']) {
    const page = await browser.newPage();
    const external = [];
    page.on('request', request => { if (!request.url().startsWith(hostUrl) && !request.url().startsWith('about:')) external.push(request.url()); });
    await page.exposeFunction('callMcpFromHost', params => rpc('tools/call', params));
    await page.goto(hostUrl);
    const uri = mode === 'preview' ? 'ui://c64/program-preview.html' : 'ui://c64/dashboard.html';
    const html = (await rpc('resources/read', { uri })).contents[0].text;
    // Connect host transport before executing the iframe's initialization script.
    await page.evaluate(async ({ mode, artifactId }) => window.attachReferenceHost(document.getElementById('app'), mode, artifactId), { mode, artifactId: artifact.id });
    await page.evaluate(html => { document.getElementById('app').srcdoc = html; }, html);
    await page.waitForFunction(() => window.bridgeReady === true, { timeout: 10000 });
    const frame = page.frameLocator('#app');
    await frame.locator('#refresh').click();
    await page.waitForFunction(() => window.calls.length === 1);
    if (mode === 'preview') {
      const data = await frame.locator('#view').textContent();
      assert(data.includes(artifact.sha256));
      await frame.locator('#execute').click();
      await page.waitForFunction(() => window.hostMessages.length === 1);
      assert((await frame.locator('#status').textContent()).includes('requires confirmation'));
      const calls = await page.evaluate(() => window.calls);
      assert.equal(calls[1].name, 'ultimate_request_program_execution');
    }
    assert.deepEqual(external, [], 'UI must not contact device or external origins');
    console.log(`PASS official AppBridge ${mode}: initialization, render, refresh${mode === 'preview' ? ', execution confirmation handoff' : ''}`);
    await page.close();
  }
  assert.equal(deviceActions, 0, 'Preview and confirmation request must never execute on the device');
} finally { await browser?.close(); await new Promise(resolve => server.close(resolve)); await new Promise(resolve => device.close(resolve)); }
