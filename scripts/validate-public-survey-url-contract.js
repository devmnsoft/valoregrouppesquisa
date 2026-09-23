const fs = require('fs');

const app = fs.readFileSync('app.js', 'utf8');
const failures = [];

function functionBody(name) {
  const start = app.search(new RegExp(`(?:async\\s+)?function\\s+${name}\\s*\\(`));
  if (start < 0) return '';
  // Function parameters may contain object defaults (`params={}`), so the first
  // brace after the name is not necessarily the body.
  const signatureEnd = app.indexOf('){', start);
  const open = signatureEnd < 0 ? app.indexOf('{', start) : signatureEnd + 1;
  let depth = 0;
  let quote = null;
  let escaped = false;
  for (let i = open; i < app.length; i += 1) {
    const char = app[i];
    if (quote) {
      if (escaped) escaped = false;
      else if (char === '\\') escaped = true;
      else if (char === quote) quote = null;
      continue;
    }
    if (char === "'" || char === '"' || char === '`') { quote = char; continue; }
    if (char === '{') depth += 1;
    if (char === '}' && --depth === 0) return app.slice(open + 1, i);
  }
  return '';
}

function requireText(condition, message) { if (!condition) failures.push(message); }
function before(body, first, second) {
  const left = body.indexOf(first); const right = body.indexOf(second);
  return left >= 0 && right >= 0 && left < right;
}

const route = functionBody('routeFromLocation');
const render = functionBody('renderTakeSurvey');
requireText(functionBody('publicSurveyRouteContract'), 'contrato de URL pública ausente.');
requireText(app.includes('missing_token') && app.includes('Link incompleto'), 'tela amigável para token ausente ausente.');
requireText(app.includes('isTokenHashLike') && app.includes('token_hash_not_allowed'), 'tokenHash não bloqueado no front.');
requireText(before(route, 'publicSurveyRouteContract(surveyRoute)', 'resolveProductionPublicSurveyLink'), 'routeFromLocation não valida contrato antes de resolver o link.');
requireText(before(render, 'publicSurveyRouteContract(route)', 'resolvePublicSurveyContext(route'), 'renderTakeSurvey não bloqueia link inválido antes de buscar o contexto.');
requireText(before(render, 'if(!initialContract.ok)', 'renderShell()'), 'renderTakeSurvey pode renderizar formulário sem contrato válido.');
requireText(app.includes('Esta pesquisa não está mais disponível') && app.includes('survey_not_found') && app.includes('invalid_public_token') && app.includes('survey_unavailable'), 'pesquisa inexistente, encerrada ou token inválido sem tratamento amigável.');
requireText(app.includes("history.replaceState({},'',APP_CONFIG.APP_PUBLIC_URL||location.origin)"), 'query antiga não é removida em link indisponível.');
requireText(app.includes('redirectToFeaturedHomeSurvey') && app.includes('getFeaturedHomeSurveyUrl'), 'ação para gerar/abrir diagnóstico atual ausente.');

if (failures.length) { console.error(failures.join('\n')); process.exit(1); }
console.log('validate-public-survey-url-contract: PASS (ordem estrutural das funções validada)');
