const { read, ok, done } = require('./validate-helper');
const css = read('style.css');
function blocks(selector){
  const escaped = selector.replace(/[.*+?^${}()|[\]\\]/g, '\\$&').replace(/\\ /g, '\\s+');
  return [...css.matchAll(new RegExp(escaped + '\\s*\\{([\\s\\S]*?)\\}', 'g'))].map(m => m[1]);
}
const sensitive = ['.free-diagnostic-preview-card', '.free-diagnostic-preview-card h3', '.free-diagnostic-hero__copy h2', '.free-diagnostic-title', '.free-diagnostic-start-card h3', '.free-diagnostic-card h3'];
for (const selector of sensitive) {
  for (const block of blocks(selector)) {
    ok(!/word-break\s*:\s*break-all/i.test(block), `${selector} não pode usar word-break: break-all.`);
    ok(!/overflow-wrap\s*:\s*anywhere/i.test(block), `${selector} não pode usar overflow-wrap: anywhere.`);
    ok(!/hyphens\s*:\s*auto/i.test(block), `${selector} não pode usar hyphens: auto.`);
  }
}
ok(/\.free-diagnostic-hero__copy h2,\s*\n\.free-diagnostic-preview-card h3\s*\{[^}]*word-break:\s*normal[^}]*overflow-wrap:\s*normal[^}]*hyphens:\s*none/s.test(css), 'Regra defensiva anti-quebra agressiva precisa existir.');
done('validate-legacy-free-diagnostic-no-word-break');
