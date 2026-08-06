'use strict';
const assert = require('node:assert/strict');
const test = require('node:test');
const v4 = require('../operational-v4');

test('onboarding acompanha exatamente oito etapas e vira checklist opcional', () => {
  const pending = v4.onboarding({ company: true, plan: true });
  assert.equal(pending.steps.length, 8);
  assert.equal(pending.percent, 25);
  assert.equal(pending.next.id, 'unit');
  const complete = v4.onboarding(Object.fromEntries(v4.ONBOARDING_STEPS.map(step => [step[2], true])));
  assert.equal(complete.complete, true);
  assert.equal(complete.mode, 'optional-checklist');
});

test('entitlement indica plano que libera o recurso sem expor erro técnico', () => {
  const result = v4.entitlement('free', 'unitComparison');
  assert.equal(result.allowed, false);
  assert.equal(result.recommended.id, 'corporate');
  assert.match(result.message, /não está disponível no seu plano atual/i);
});

test('perfis respeitam escopo de administração e leitura', () => {
  assert.equal(v4.can('empresa_admin', 'member.manage'), true);
  assert.equal(v4.can('analyst', 'member.manage'), false);
  assert.equal(v4.can('sector_manager', 'sector.view'), true);
  assert.equal(v4.can('respondent', 'survey.publish'), false);
});

test('recomendações reais podem ser convertidas em ação rastreável', () => {
  const rows = v4.recommendation({ dimensions: [{ id: 'culture', name: 'Cultura', score: 1.8 }], adherence: 32, unitGap: 1.4, scoreDrop: 4 });
  assert.equal(rows.length, 4);
  assert.ok(rows.every(row => row.title && row.priority && row.effort && row.impact && row.owner && row.status === 'sugerida'));
  const action = v4.acceptRecommendation(rows[0], { unitId: 'u1', sectorId: 's1' });
  assert.equal(action.status, 'em andamento');
  assert.equal(action.unitId, 'u1');
  assert.equal(action.history[0].event, 'Recomendação aceita');
});

test('limites distinguem aviso de bloqueio e recursos ilimitados', () => {
  const free = v4.usage('free', { surveys: 1, responses: 1 });
  assert.equal(free.find(item => item.key === 'surveys').blocked, true);
  const enterprise = v4.usage('enterprise', { surveys: 999 });
  assert.equal(enterprise.find(item => item.key === 'surveys').blocked, false);
});

test('mensagens públicas permanecem amigáveis', () => {
  assert.doesNotMatch(Object.values(v4.FRIENDLY_MESSAGES).join(' '), /entitlement|exception|stack|firebase/i);
  assert.match(v4.FRIENDLY_MESSAGES.invalidLink, /expirado|encerrado|removido/i);
});
