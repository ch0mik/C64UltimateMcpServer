import { build } from 'esbuild';
import { mkdir, writeFile } from 'node:fs/promises';
const built = await build({ entryPoints: ['entry.mjs'], bundle: true, minify: true, format: 'iife', write: false, target: 'es2022' });
const script = built.outputFiles[0].text.replaceAll('</script', '<\\/script');
await mkdir('dist', { recursive: true });
for (const [file, mode, title] of [['dashboard.html', 'dashboard', 'C64 Ultimate dashboard'], ['program-preview.html', 'preview', 'PRG artifact preview']]) {
  await writeFile(`dist/${file}`, `<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>${title}</title>
<style>body{font:15px system-ui,sans-serif;color:var(--color-text-primary,#20242a);background:var(--color-background-primary,#f4f6fa);margin:20px}main{max-width:760px}h1{font-size:22px}button{padding:8px 14px;margin:8px 8px 8px 0;cursor:pointer}pre{white-space:pre-wrap;overflow-wrap:anywhere;padding:16px;border:1px solid #8886;border-radius:8px}#status{min-height:1.5em}</style></head>
<body data-mode="${mode}"><main><h1>${title}</h1><p id="status">Waiting for tool result from host…</p><button id="refresh">Refresh</button><button id="execute" hidden>Request execution…</button><pre id="view"></pre></main><script>${script}</script></body></html>`);
}
