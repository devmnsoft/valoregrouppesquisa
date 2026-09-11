const vm = require('vm');
const { read, assert, ok } = require('./legacy-public-submit-validator-lib');
const app = read('app.js');
const start = app.indexOf('function getPublicSurveyRouteParams');
const end = app.indexOf('\nfunction hasPublicSurveyRouteParams', start);
assert(start >= 0 && end > start, 'helper semântico de parâmetros da rota pública ausente');

const sandbox = { URLSearchParams, location: { search: '?survey=reload-survey&token=reload-token&org=reload-org' } };
vm.runInNewContext(`${app.slice(start, end)};this.parseRoute=getPublicSurveyRouteParams`, sandbox);

for (const [label, source, expected] of [
  ['link direto', '?survey=direct-survey&token=direct-token&org=direct-org', { surveyId: 'direct-survey', token: 'direct-token', org: 'direct-org' }],
  ['alias surveyId', '?surveyId=alias-survey&token=alias-token&org=alias-org', { surveyId: 'alias-survey', token: 'alias-token', org: 'alias-org' }],
  ['argumentos preservados', { surveyId: 'argument-survey', token: 'argument-token', org: 'argument-org' }, { surveyId: 'argument-survey', token: 'argument-token', org: 'argument-org' }],
  ['atualização da página', null, { surveyId: 'reload-survey', token: 'reload-token', org: 'reload-org' }]
]) {
  const actual = sandbox.parseRoute(source);
  assert(JSON.stringify(actual) === JSON.stringify(expected), `${label} não preserva surveyId/token/org: ${JSON.stringify(actual)}`);
}
ok('comportamento da rota pública preserva surveyId, token e org');
