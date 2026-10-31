import { App } from '@modelcontextprotocol/ext-apps';
import { attachUi } from './ui.mjs';
const mode = document.body.dataset.mode;
const app = new App({ name: `C64 ${mode}`, version: '1.0.0' }, {});
attachUi(app, document, mode);
app.connect().catch(error => { document.getElementById('status').textContent = `Host connection failed: ${error.message}`; });
