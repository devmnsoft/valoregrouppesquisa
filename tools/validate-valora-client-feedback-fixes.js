const fs = require('fs');
const path = require('path');

const root = process.cwd();
const read = (p) => fs.readFileSync(path.join(root, p), 'utf8');
const files = (dir, exts) => fs.existsSync(path.join(root, dir)) ? fs.readdirSync(path.join(root, dir), { withFileTypes: true }).flatMap(d => {
  const p = path.join(dir, d.name);
  return d.isDirectory() ? files(p, exts) : exts.includes(path.extname(d.name)) ? [p] : [];
}) : [];
let ok = true;
function check(condition, message) { if (condition) console.log('OK', message); else { ok = false; console.error('FAIL', message); } }
const publicFiles = [...files('backend/Valora.Web/Views', ['.cshtml']), ...files('backend/Valora.Web/wwwroot/js/public', ['.js']), ...files('backend/Valora.Web/wwwroot/css', ['.css'])];
const publicText = publicFiles.map(read).join('\n');
check(!/Valora Pulse/.test(publicText), 'não existe Valora Pulse em views/scripts públicos');
check(publicText.includes('Valora Insight™'), 'existe Valora Insight™');
check(!/>\s*HOME\s*</.test(publicText) && !/>\s*Home\s*</.test(publicText), 'não existe texto visível HOME');
check(publicText.includes('Início'), 'existe Início');
check(publicText.includes('wa.me/5591992545353'), 'WhatsApp usa wa.me/5591992545353');
check(publicText.includes('+55 91 99254-5353'), 'existe telefone oficial');
check(publicText.includes('Fale com a Valora Group'), 'existe texto correto de contato');
check(!publicText.includes('Invalid date'), 'não existe Invalid date');
check(publicText.includes('function formatValoraDate(value)'), 'existe função segura de formatação de data');
check(read('backend/Valora.Web/wwwroot/css/valora-public.css').includes('overflow-x: hidden'), 'CSS mobile contém overflow-x hidden');
const result = read('backend/Valora.Web/Views/Results/Public.cshtml');
['Valora Insight™ — Devolutiva Estratégica','Análise executiva direta da maturidade organizacional.','Data do diagnóstico','Pontuação total','Nível de maturidade','Leitura executiva','Radar visual','Diagnóstico por dimensão','Benchmarking','Verdade estratégica','Risco se nada mudar','Próximo nível','Fale com a Valora Group','Abrir certificado','Enviar por e-mail'].forEach(t => check(result.includes(t), `resultado contém ${t}`));
check(fs.existsSync(path.join(root,'backend/Valora.Web/wwwroot/css/valora-print.css')) && read('backend/Valora.Web/wwwroot/css/valora-print.css').includes('@media print'), 'certificado/print tem CSS de impressão');
check(!/alert\s*\(/.test(publicText), 'não há alert bruto em JS público');
check(!/StackTrace|Exception details|System\./.test(publicText), 'não há stack trace exposto');
check(!/<pre[^>]*>\s*\{/.test(publicText), 'não há JSON bruto visível');
process.exit(ok ? 0 : 1);
