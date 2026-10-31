export function attachUi(app, document, mode) {
  const view = document.getElementById('view');
  const status = document.getElementById('status');
  const refresh = document.getElementById('refresh');
  const execute = document.getElementById('execute');
  let artifactId;

  function render(result) {
    const data = result.structuredContent ?? result;
    artifactId = data.artifact?.id ?? artifactId;
    // Device/source/diagnostic strings are always text. Never use innerHTML.
    view.textContent = JSON.stringify(data, null, 2);
    status.textContent = result.isError ? 'Operation failed. See details below.' : 'Updated';
    execute.hidden = !!result.isError || mode !== 'preview' || !artifactId;
    return data;
  }

  async function invoke(name, args) {
    status.textContent = 'Waiting for host…';
    try { return render(await app.callServerTool({ name, arguments: args })); }
    catch (error) { status.textContent = `Request failed: ${error.message}`; return null; }
  }

  app.ontoolinput = (input) => { artifactId = input.arguments?.artifactId ?? artifactId; };
  app.ontoolresult = render;
  refresh.addEventListener('click', () => mode === 'dashboard'
    ? invoke('ultimate_dashboard', {})
    : artifactId ? invoke('ultimate_program_preview', { artifactId }) : undefined);
  execute.hidden = true;
  execute.addEventListener('click', async () => {
    if (!artifactId || execute.disabled) return;
    execute.disabled = true;
    try {
      const result = await invoke('ultimate_request_program_execution', { artifactId });
      if (result?.resultType === 'input_required') {
        status.textContent = 'Execution requires confirmation in the host conversation.';
        await app.sendMessage({ role: 'user', content: [{ type: 'text', text:
          `Continue ultimate_program_workflow with arguments ${JSON.stringify({ artifactId, execute: true })} and the following input-required result. Ask me for confirmation before execution: ${JSON.stringify(result)}` }] });
      }
    } catch (error) {
      status.textContent = `Could not hand off confirmation: ${error.message}. Continue in the host conversation.`;
    } finally { execute.disabled = false; }
  });
  return { render, invoke };
}
