const fs = require('fs');
const app = fs.readFileSync('app.js', 'utf8');

const requiredHelpers = ['async function withLoading', 'function setButtonLoading', 'const pendingActions', 'function actionKey'];
const requiredActions = [
  'deleteForm',
  'deleteSurvey',
  'submitSurvey',
  'featureSurvey',
  'preparePublicSurveyLink',
  'shareSurvey',
  'saveSettings',
  'markNotificationRead',
  'dismissNotification',
  'actNotification'
];

for (const token of requiredHelpers) {
  if (!app.includes(token)) throw new Error(`${token} ausente`);
}

for (const action of requiredActions) {
  const positions = [];
  let idx = app.indexOf(action);
  while (idx >= 0) {
    positions.push(idx);
    idx = app.indexOf(action, idx + action.length);
  }
  if (!positions.length) throw new Error(`${action} ausente`);
  const covered = positions.some(pos => {
    const window = app.slice(Math.max(0, pos - 450), Math.min(app.length, pos + 1400));
    return /withLoading\s*\(/.test(window) || /setButtonLoading\s*\(/.test(window);
  });
  if (!covered) throw new Error(`${action} sem loading local/global próximo`);
}

const withLoadingCalls = (app.match(/withLoading\s*\(/g) || []).length;
if (withLoadingCalls < 20) throw new Error(`Cobertura de loading insuficiente: ${withLoadingCalls} chamadas`);

console.log('validate-async-actions-have-loading: PASS');
