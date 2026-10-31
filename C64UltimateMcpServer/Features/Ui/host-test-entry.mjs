import { AppBridge, PostMessageTransport } from '@modelcontextprotocol/ext-apps/app-bridge';
window.attachReferenceHost = async function (iframe, mode, artifactId) {
  const bridge = new AppBridge(null, { name: 'C64 reference host', version: '2.0.4' }, { serverTools: {}, message: {} });
  window.calls = [];
  window.hostMessages = [];
  bridge.oncalltool = async params => {
    window.calls.push(params);
    return await window.callMcpFromHost(params);
  };
  bridge.onmessage = async params => { window.hostMessages.push(params); return {}; };
  bridge.oninitialized = async () => {
    await bridge.sendToolInput({ arguments: mode === 'preview' ? { artifactId } : {} });
    await bridge.sendToolResult(await window.callMcpFromHost({ name: mode === 'preview' ? 'ultimate_program_preview' : 'ultimate_dashboard', arguments: mode === 'preview' ? { artifactId } : {} }));
    window.bridgeReady = true;
  };
  window.referenceBridge = bridge;
  await bridge.connect(new PostMessageTransport(iframe.contentWindow, iframe.contentWindow));
};
