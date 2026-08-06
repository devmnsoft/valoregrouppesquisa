(function (root, factory) {
  const api = factory();
  if (typeof module === 'object' && module.exports) module.exports = api;
  if (root) root.ValoraOperationalV4 = api;
})(typeof globalThis !== 'undefined' ? globalThis : this, function () {
  'use strict';

  const FRIENDLY_MESSAGES = Object.freeze({
    permission: 'Seu perfil não possui permissão para acessar esta área. Caso precise desse acesso, solicite ao administrador da empresa.',
    entitlement: 'Este recurso não está disponível no seu plano atual. Para liberar esta funcionalidade, fale com a Valora.',
    temporary: 'Não foi possível concluir esta operação agora. Tente novamente em alguns instantes.',
    empty: 'Ainda não há dados suficientes para exibir esta análise. Publique uma pesquisa ou aguarde novas respostas.',
    invalidLink: 'Este link não está disponível. Ele pode ter expirado, sido encerrado ou removido pelo administrador.'
  });

  const PLAN_ORDER = ['free', 'professional', 'corporate', 'enterprise'];
  const PLANS = Object.freeze({
    free: { name: 'Gratuito', limits: { surveys: 1, responses: 1, units: 1, sectors: 1, members: 1, certificates: 0, reports: 0 }, features: ['simpleResult'], value: 'Experimente o diagnóstico essencial antes de ampliar a operação.' },
    professional: { name: 'Profissional', limits: { surveys: 12, responses: 1000, units: 1, sectors: 20, members: 8, certificates: 1000, reports: 24 }, features: ['simpleResult', 'sectorView', 'basicReports', 'certificates', 'companyDashboard'], value: 'Conecte diagnóstico, setores, certificados e gestão da empresa.' },
    corporate: { name: 'Corporativo', limits: { surveys: -1, responses: 10000, units: -1, sectors: -1, members: 50, certificates: -1, reports: -1 }, features: ['simpleResult', 'sectorView', 'unitComparison', 'sectorComparison', 'executiveReports', 'certificates', 'companyDashboard', 'actionPlan'], value: 'Compare unidades e setores para decidir com mais precisão.' },
    enterprise: { name: 'Enterprise', limits: { surveys: -1, responses: -1, units: -1, sectors: -1, members: -1, certificates: -1, reports: -1 }, features: ['simpleResult', 'sectorView', 'unitComparison', 'sectorComparison', 'executiveReports', 'certificates', 'companyDashboard', 'actionPlan', 'multipleCompanies', 'consolidatedAnalysis', 'advancedGovernance'], value: 'Governe grupos, redes, franquias e holdings em uma visão consolidada.' }
  });

  const PERMISSIONS = Object.freeze({
    admin_valora: ['*'],
    empresa_admin: ['survey.create', 'survey.edit', 'survey.publish', 'survey.close', 'result.view', 'certificate.generate', 'report.download', 'member.manage', 'unit.manage', 'sector.manage', 'dashboard.executive', 'unit.view', 'sector.view', 'action.manage'],
    unit_manager: ['survey.create', 'survey.edit', 'survey.publish', 'survey.close', 'result.view', 'certificate.generate', 'report.download', 'unit.view', 'sector.view', 'action.manage'],
    sector_manager: ['result.view', 'certificate.generate', 'report.download', 'sector.view', 'action.manage'],
    analyst: ['result.view', 'report.download', 'dashboard.executive', 'unit.view', 'sector.view'],
    respondent: ['survey.respond', 'certificate.view']
  });

  const ONBOARDING_STEPS = Object.freeze([
    ['company', 'Dados da empresa', 'company'], ['plan', 'Escolha do plano', 'plan'], ['unit', 'Unidade principal', 'unit'], ['sectors', 'Setores', 'sectors'],
    ['members', 'Convite de membros', 'members'], ['template', 'Template da primeira pesquisa', 'template'], ['publication', 'Publicação da primeira pesquisa', 'publication'], ['publicLink', 'Visualização do link público', 'publicLink']
  ]);

  function normalizePlan(code) {
    const value = String(code || 'free').toLowerCase();
    if (value.includes('enterprise')) return 'enterprise';
    if (value.includes('corpor')) return 'corporate';
    if (value.includes('prof') || value === 'growth' || value === 'essential') return 'professional';
    return 'free';
  }
  function getPlan(code) { const id = normalizePlan(code); return { id, ...PLANS[id] }; }
  function can(role, permission) { const list = PERMISSIONS[role] || []; return list.includes('*') || list.includes(permission); }
  function entitlement(planCode, feature) {
    const current = getPlan(planCode);
    if (current.features.includes(feature)) return { allowed: true, current, recommended: current };
    const recommendedId = PLAN_ORDER.slice(PLAN_ORDER.indexOf(current.id) + 1).find(id => PLANS[id].features.includes(feature)) || 'enterprise';
    return { allowed: false, current, recommended: getPlan(recommendedId), feature, message: FRIENDLY_MESSAGES.entitlement };
  }
  function onboarding(snapshot) {
    const values = snapshot || {};
    const steps = ONBOARDING_STEPS.map(([id, label, key], index) => ({ id, label, order: index + 1, done: Boolean(values[key]), optional: ['members', 'sectors'].includes(id) }));
    const completed = steps.filter(step => step.done).length;
    return { steps, completed, percent: Math.round(completed / steps.length * 100), complete: completed === steps.length, next: steps.find(step => !step.done) || null, mode: completed === steps.length ? 'optional-checklist' : 'required' };
  }
  function priority(score) { return score < 2 ? 'crítica' : score < 3 ? 'alta' : 'média'; }
  function recommendation(input) {
    const data = input || {}, dimensions = [...(data.dimensions || [])].sort((a, b) => Number(a.score) - Number(b.score));
    const rows = [];
    if (dimensions[0]) rows.push({ key: `dimension:${dimensions[0].id || dimensions[0].name}`, title: `Fortalecer ${dimensions[0].name}`, description: 'Fortalecer comunicação interna entre liderança e operação e acompanhar a dimensão mensalmente.', priority: priority(Number(dimensions[0].score)), effort: 'médio', impact: 'alto', suggestedDays: 30, owner: 'Liderança da área', status: 'sugerida' });
    if (Number(data.adherence) < 50) rows.push({ key: 'low-adherence', title: 'Elevar a adesão da próxima rodada', description: 'Reforce o propósito da pesquisa, mobilize gestores e programe lembretes aos participantes.', priority: 'alta', effort: 'baixo', impact: 'alto', suggestedDays: 10, owner: 'Gestor da pesquisa', status: 'sugerida' });
    if (Number(data.unitGap) >= 1) rows.push({ key: 'unit-gap', title: 'Apoiar unidades com menor maturidade', description: 'Priorizar treinamento dos gestores das unidades com menor maturidade.', priority: 'alta', effort: 'alto', impact: 'alto', suggestedDays: 60, owner: 'Administrador da empresa', status: 'sugerida' });
    if (Number(data.scoreDrop) > 0) rows.push({ key: 'score-drop', title: 'Reverter a queda do score', description: 'Revise causas, responsáveis e indicadores antes de executar uma nova rodada de pesquisa.', priority: 'crítica', effort: 'médio', impact: 'alto', suggestedDays: 30, owner: 'Liderança executiva', status: 'sugerida' });
    return rows;
  }
  function acceptRecommendation(item, context) {
    const now = new Date(), due = new Date(now); due.setUTCDate(due.getUTCDate() + Number(item.suggestedDays || 30));
    return { id: `action_${Date.now()}`, recommendationKey: item.key, title: item.title, description: item.description, priority: item.priority, owner: item.owner, unitId: context?.unitId || '', sectorId: context?.sectorId || '', dueDate: due.toISOString().slice(0, 10), status: 'em andamento', notes: '', history: [{ event: 'Recomendação aceita', at: now.toISOString() }] };
  }
  function usage(planCode, current) {
    const plan = getPlan(planCode); return Object.keys(plan.limits).map(key => { const used = Number(current?.[key] || 0), limit = plan.limits[key]; return { key, used, limit, percent: limit < 0 ? 0 : Math.min(100, Math.round(used / Math.max(1, limit) * 100)), blocked: limit >= 0 && used >= limit }; });
  }
  function audit(type, entityId, metadata) { return { id: `audit_${Date.now()}`, type, entityId: entityId || '', metadata: { ...(metadata || {}) }, createdAt: new Date().toISOString() }; }
  return { FRIENDLY_MESSAGES, PLANS, PERMISSIONS, ONBOARDING_STEPS, normalizePlan, getPlan, can, entitlement, onboarding, recommendation, acceptRecommendation, usage, audit };
});
