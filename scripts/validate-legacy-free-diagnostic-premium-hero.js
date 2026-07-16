const fs = require('fs');
const { read, ok, done } = require('./validate-helper');
const app = read('app.js');
const css = read('style.css');
const distApp = fs.existsSync('dist/app.js') ? read('dist/app.js') : '';
const distCss = fs.existsSync('dist/style.css') ? read('dist/style.css') : '';
ok(/function\s+renderFreeDiagnosticHero\s*\(/.test(app), 'renderFreeDiagnosticHero precisa existir em app.js.');
ok(app.includes('class="free-diagnostic-hero"') && app.includes('id="diagnostico-gratuito"'), 'HTML precisa renderizar .free-diagnostic-hero com id diagnostico-gratuito.');
ok(app.includes('free-diagnostic-hero__inner'), 'HTML precisa conter .free-diagnostic-hero__inner.');
ok(app.includes('<h3>Diagnóstico Valora Insight™</h3>'), 'Título do card deve ser texto único em h3.');
ok(!/<h3[^>]*>\s*Diagnóstico\s*<br/i.test(app), 'Título do card não pode usar <br>.');
ok(!/<h3[^>]*>[\s\S]{0,220}<span[^>]*>Diagnóstico<\/span>/i.test(app), 'Título do card não pode ser separado em spans.');
ok(css.includes('.free-diagnostic-hero') && css.includes('.free-diagnostic-preview-card'), 'CSS premium da seção precisa existir.');
ok(/\.free-diagnostic-hero__inner\s*\{[\s\S]*?display:\s*grid[\s\S]*?grid-template-columns:\s*minmax\(0,\s*1\.08fr\)\s*minmax\(430px,\s*\.92fr\)/.test(css), 'Desktop precisa ter grid de duas colunas com coluna direita mínima de 430px.');
if (distApp || distCss) {
  ok((distApp + distCss).includes('free-diagnostic-hero__inner'), 'Build final precisa conter as classes novas da seção.');
}
done('validate-legacy-free-diagnostic-premium-hero');
