#!/usr/bin/env node
const fs = require('fs');
const path = require('path');
const vm = require('vm');
const root = path.resolve(__dirname, '..');
const app = fs.readFileSync(path.join(root, 'app.js'), 'utf8');
const start = app.indexOf('const VALORA_INSIGHT_DIMENSIONS=');
const end = app.indexOf('async function renderResult', start);
if (start < 0 || end < 0) throw new Error('Bloco Valora Insight não encontrado em app.js');
const sandbox = {
  console,
  nowIso: () => '2026-06-22T00:00:00.000Z',
  deepClone: v => JSON.parse(JSON.stringify(v)),
  clamp: (n, min, max) => Math.max(min, Math.min(max, Number.isFinite(Number(n)) ? Number(n) : min)),
  formDimensionName: (form, id) => (form.dimensions || []).find(d => d.id === id)?.name || '',
};
vm.createContext(sandbox);
vm.runInContext(app.slice(start, end) + '\nObject.assign(globalThis,{VALORA_INSIGHT_DIMENSIONS,renderRadarBar,calculateValoraInsightResult,generateValoraInsightDevolutiva,normalizeLegacyValoraInsightResponse});', sandbox);
const dims = sandbox.VALORA_INSIGHT_DIMENSIONS;
const banned = [/Parabéns pelo excelente resultado/i,/Sua empresa está indo muito bem/i,/Continue assim/i,/Você está no caminho certo/i,/Resultado muito positivo/i];
const official = ['Cultura e Propósito','Gestão e Governança','Liderança','Pessoas e Talentos','Resultados e Crescimento'];
const errors = [];
const assert = (ok, msg) => { if (!ok) errors.push(msg); };
assert(JSON.stringify(dims) === JSON.stringify(official), 'Dimensões oficiais divergentes');
const form = { name: 'Valora Insight™ — Diagnóstico Estratégico', dimensions: official.map((name,i)=>({id:['cultura','governanca','lideranca','talentos','resultados'][i], name})), questions: [] };
for (const d of form.dimensions) for (let i = 1; i <= 5; i++) form.questions.push({ id: `${d.id}_${i}`, type: 'scale', dimensionId: d.id, dimensionName: d.name, maxScore: 5, weight: 1 });
assert(form.questions.length === 25, 'Valora Insight deve ter 25 perguntas');
assert(form.questions.reduce((s,q)=>s+q.maxScore,0) === 125, 'Pontuação máxima deve ser 125');
assert(!form.dimensions.some(d => /ESG|Sustentabilidade/i.test(d.name)), 'ESG não pode estar nas dimensões oficiais');
function responseFor(scores) { const byDimension = {}; official.forEach((name,i)=>byDimension[name]={rawScore:scores[i],maxScore:25,count:5}); return { byDimension, maxScore: 125, rawScore: scores.reduce((a,b)=>a+b,0) }; }
const scenarios = [
  { scores: [9,8,9,9,10], total: 45, level: 'Crítico' },
  { scores: [18,12,16,14,12], total: 72, level: 'Em estruturação' },
  { scores: [20,18,19,19,20], total: 96, level: 'Estruturada' },
  { scores: [23,23,23,23,24], total: 116, level: 'Alta maturidade' },
];
for (const sc of scenarios) {
  const result = sandbox.calculateValoraInsightResult(responseFor(sc.scores), form);
  assert(result.totalScore === sc.total, `Total esperado ${sc.total}, recebido ${result.totalScore}`);
  assert(result.maxScore === 125, 'maxScore deve ser sempre 125');
  assert(result.maturityLevel.label === sc.level, `Nível esperado ${sc.level}, recebido ${result.maturityLevel.label}`);
  assert(Number.isFinite(result.percentage), 'Percentual não pode ser NaN/undefined');
  assert(result.totalScore <= 125, 'Total não pode passar de 125');
  assert(result.dimensions.length === 5, 'Resultado deve conter 5 dimensões');
  assert(result.radar.every(line => /: [█░]{10} \d+\/25$/.test(line)), 'Radar textual inválido');
}
assert(sandbox.renderRadarBar(undefined).length === 10, 'Radar deve tolerar valor vazio e ter 10 blocos');
assert(sandbox.renderRadarBar(40) === '██████████', 'Radar deve respeitar teto');
const dev = sandbox.generateValoraInsightDevolutiva(responseFor([18,12,16,14,12]), form, {});
const required = ['title','subtitle','totalScoreText','maturityLevel','executiveSummary','directReading','dimensionDiagnosis','radarText','radarExplanation','benchmarking','strategicTruth','riskIfNothingChanges','nextLevelGuidance','transitionToSolution','finalPrinciple'];
for (const key of required) assert(dev[key] !== undefined && dev[key] !== null && dev[key] !== '', `Campo obrigatório ausente: ${key}`);
const devText = JSON.stringify(dev);
assert(!/NaN|undefined|\[object Object\]/.test(devText), 'Devolutiva contém NaN, undefined ou [object Object]');
assert(!/ESG|Sustentabilidade/i.test(dev.radarText), 'ESG não pode aparecer no diagnóstico principal');
assert(!banned.some(rx => rx.test(devText)), 'Devolutiva contém frase genérica proibida');
assert(dev.maturityLevel.label === 'Em estruturação', 'Resposta exemplo 72/125 deve ser Em estruturação');
assert(dev.finalPrinciple === 'Empresas não evoluem quando entendem o diagnóstico.\nElas evoluem quando aceitam a verdade que ele revela.', 'Princípio final obrigatório divergente');
const persistedOnly = sandbox.calculateValoraInsightResult({ valoraInsight: { dimensionScores: official.map((name,i)=>({ name, score: [18,12,16,14,12][i] })) } }, form);
assert(persistedOnly.totalScore === 72, 'Resposta persistida só com valoraInsight.dimensionScores deve regenerar 72/125');
const legacy = sandbox.normalizeLegacyValoraInsightResponse({ rawScore: 130, maxScore: 135, byDimension: { ESG: { rawScore: 10, maxScore: 10 } } }, form);
assert(legacy.legacyScore && legacy.legacyScore.maxScore === 135, 'Legacy score 135 deve ser preservado');
if (errors.length) { console.error('Valora Insight QA: FAIL'); for (const e of errors) console.error(`- ${e}`); process.exit(1); }
console.log('Valora Insight QA: PASS');
console.log('Exemplo 72/125: Nível Em estruturação validado.');
