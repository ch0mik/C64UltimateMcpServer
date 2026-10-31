import test from 'node:test';
import assert from 'node:assert/strict';
import { attachUi } from './ui.mjs';

function fixture(mode) {
  const elements = Object.fromEntries(['view', 'status', 'refresh', 'execute'].map(id => [id, { textContent: '', hidden: false, disabled: false, addEventListener(type, callback) { this.click = callback; } }]));
  const calls = [];
  const app = { async callServerTool(request) { calls.push(request); return { structuredContent: { ok: true } }; } };
  return { elements, app, calls, ui: attachUi(app, { getElementById: id => elements[id] }, mode) };
}

test('dashboard refresh uses host bridge and renders errors as text', async () => {
  const f = fixture('dashboard');
  await f.elements.refresh.click();
  assert.equal(f.calls[0].name, 'ultimate_dashboard');
  f.ui.render({ isError: true, structuredContent: { error: '<script>alert(1)</script>' } });
  assert.match(f.elements.view.textContent, /<script>/);
  assert.match(f.elements.status.textContent, /failed/);
  assert.equal(f.elements.execute.hidden, true);
});

test('preview never runs on render and execution uses confirmed workflow', async () => {
  const f = fixture('preview');
  f.app.ontoolinput({ arguments: { artifactId: 'abc' } });
  f.ui.render({ structuredContent: { artifact: { id: 'abc', sha256: '123' }, hex: '0108' } });
  assert.equal(f.calls.length, 0);
  await f.elements.execute.click();
  assert.deepEqual(f.calls[0], { name: 'ultimate_request_program_execution', arguments: { artifactId: 'abc' } });
  assert.equal(f.elements.execute.disabled, false);
});

test('expired artifact and failed bridge are visible without interpreting markup', async () => {
  const f = fixture('preview');
  f.ui.render({ isError: true, structuredContent: { errorType: 'ArtifactUnavailable', error: '<img onerror=evil()>' } });
  assert.equal(f.elements.execute.hidden, true);
  f.app.callServerTool = async () => { throw new Error('host disconnected'); };
  await f.ui.invoke('ultimate_program_preview', { artifactId: 'expired' });
  assert.match(f.elements.status.textContent, /host disconnected/);
});

test('execution request hands continuation to conversation and handles a refusing host', async () => {
  const f = fixture('preview');
  f.ui.render({ structuredContent: { artifact: { id: 'abc' } } });
  f.app.callServerTool = async () => ({ structuredContent: { resultType: 'input_required', requestState: 'once', inputRequests: { confirm: {} } } });
  const messages = [];
  f.app.sendMessage = async message => { messages.push(message); };
  await f.elements.execute.click();
  assert.match(messages[0].content[0].text, /Ask me for confirmation/);
  assert.match(messages[0].content[0].text, /ultimate_program_workflow/);
  f.app.sendMessage = async () => { throw new Error('host refused'); };
  await f.elements.execute.click();
  assert.match(f.elements.status.textContent, /Continue in the host conversation/);
});
