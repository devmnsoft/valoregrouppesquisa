'use strict';

const fs = require('fs');
const path = require('path');

const root = path.resolve(__dirname, '..');
const viewsRoot = path.join(root, 'backend', 'Valora.Web', 'Views');
const priorityModules = new Set([
  'Account', 'Dashboard', 'Forms', 'Diagnostics', 'Surveys', 'Results', 'Reports',
  'Certificates', 'ActionCenter', 'Evolution', 'Journey', 'Indicators', 'Benchmarks',
  'Methodology', 'Governance', 'Administration', 'AdminHub', 'Plans', 'Organization'
]);

function walk(directory) {
  return fs.readdirSync(directory, { withFileTypes: true }).flatMap(entry => {
    const target = path.join(directory, entry.name);
    return entry.isDirectory() ? walk(target) : [target];
  });
}

const files = walk(viewsRoot)
  .filter(file => file.endsWith('.cshtml'))
  .filter(file => !file.includes(`${path.sep}Shared${path.sep}`))
  .filter(file => !path.basename(file).startsWith('_'));

const tests = {
  title: text => /ViewData\["Title"\]|<h1\b/i.test(text),
  action: text => /PageHeaderAction|\b(btn|valora-button)\b|<button\b/i.test(text),
  form: text => /<form\b/i.test(text),
  filters: text => /type="search"|\b(filter|filtro|buscar|busca)\b/i.test(text),
  emptyState: text => /empty-state|_EmptyState|Nenhum(?:a)?\s/i.test(text),
  feedback: text => /valora-alert|_Toast|data-error|validation-summary|field-validation/i.test(text),
  responsive: text => /table-responsive|metric-grid|executive-grid|\b(row|col-)\b|valora-page/i.test(text),
  manual: text => /_PageHelpPanel|page-help|Como usar|Orientações/i.test(text)
};

const rows = files.map(file => {
  const text = fs.readFileSync(file, 'utf8');
  const relative = path.relative(root, file).replaceAll(path.sep, '/');
  const module = path.relative(viewsRoot, file).split(path.sep)[0];
  const flags = Object.fromEntries(Object.entries(tests).map(([name, test]) => [name, test(text)]));
  const concerns = [];
  if (!flags.title) concerns.push('sem título local');
  if (!flags.action) concerns.push('sem ação local');
  if (!flags.feedback) concerns.push('feedback apenas global');
  if (!flags.emptyState && !flags.form) concerns.push('sem empty state local');
  const classification = concerns.length === 0
    ? 'funcional e bem desenhada'
    : concerns.length <= 2 ? 'funcional, cobertura global complementa a tela' : 'requer revisão específica';
  return { relative, module, flags, concerns, classification };
});

const priority = rows.filter(row => priorityModules.has(row.module));
const invalidRazorCss = files.filter(file => /(?<!@)@(media|keyframes|supports|container)\b/.test(fs.readFileSync(file, 'utf8')));
const dynamicModels = files.filter(file => /^\s*@model\s+dynamic\b/m.test(fs.readFileSync(file, 'utf8')));
const manualIds = files.filter(file => /<input[^>]+name="(?:organizationId|tenantId|userId|formId|diagnosticId|resultId)"[^>]*(?!type="hidden")/i.test(fs.readFileSync(file, 'utf8')));

const report = [
  '# Auditoria automatizada das views internas', '',
  `- Views de página verificadas: **${rows.length}**.`,
  `- Views dos módulos prioritários: **${priority.length}**.`,
  `- Razor CSS com diretiva não escapada: **${invalidRazorCss.length}**.`,
  `- Views com \`@model dynamic\`: **${dynamicModels.length}**.`,
  `- Possíveis identificadores técnicos em inputs: **${manualIds.length}**.`, '',
  '> O layout interno injeta orientação contextual, contexto de organização, mensagens, loading e confirmação. A classificação abaixo registra também os recursos locais de cada view.', '',
  '| View | Classificação | Cobertura local | Pontos de atenção |',
  '|---|---|---|---|',
  ...rows.map(row => {
    const coverage = Object.entries(row.flags).filter(([, value]) => value).map(([key]) => key).join(', ') || 'layout global';
    return `| \`${row.relative}\` | ${row.classification} | ${coverage} | ${row.concerns.join('; ') || '—'} |`;
  })
];

fs.writeFileSync(path.join(root, 'SPRINT_DEEP_UI_AUDIT.md'), `${report.join('\n')}\n`);

if (invalidRazorCss.length || dynamicModels.length || manualIds.length) {
  console.error('Auditoria encontrou padrões técnicos proibidos. Consulte SPRINT_DEEP_UI_AUDIT.md.');
  process.exitCode = 1;
} else {
  console.log(`Auditoria concluída: ${rows.length} views, ${priority.length} prioritárias e nenhum padrão técnico proibido.`);
}
