const vm = require('vm');
const { read, must } = require('./legacy-public-final-validator-common');
const app = read('app.js');
const start = app.indexOf('function isPublicSurveyContextReady');
const end = app.indexOf('\nfunction clearPublicSurveyDomArtifacts', start);
must('readiness guard exists', start >= 0 && end > start);
const sandbox = { getPublicSurveyState: () => ({}) };
vm.runInNewContext(`${app.slice(start, end)};this.isReady=isPublicSurveyContextReady`, sandbox);

const valid = { status: 'ready', context: { surveyId: 's1', token: 't1', org: 'o1', survey: { id: 's1', formId: 'f1' }, form: { id: 'f1', questions: [{ id: 'q1' }] } } };
const invalidCases = [
  ['token inválido/ausente', { ...valid, context: { ...valid.context, token: '' } }],
  ['contexto ainda carregando', { ...valid, status: 'loading' }],
  ['formulário incompatível', { ...valid, context: { ...valid.context, form: { id: 'outro', questions: [{ id: 'q1' }] } } }],
  ['formulário sem perguntas', { ...valid, context: { ...valid.context, form: { id: 'f1', questions: [] } } }]
];
must('valid context renders questions', sandbox.isReady(valid) === true);
for (const [label, state] of invalidCases) must(`${label} blocks form and submit`, sandbox.isReady(state) === false);
must('renderTakeSurvey applies readiness guard before form HTML', /if \(!isPublicSurveyContextReady\(stateNow\)\) \{[\s\S]*?renderPublicSurveyUnavailable/.test(app));
