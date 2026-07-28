#!/usr/bin/env node
'use strict';
const fs = require('fs');
const path = require('path');
const backend = path.resolve(__dirname, '../..');
const read = relative => fs.readFileSync(path.join(backend, relative), 'utf8');
const failures = [];
const reject = (condition, message) => { if (condition) failures.push(message); };
const identityRepositories = [
  'Valora.Infrastructure/Repositories/UserRepository.cs',
  'Valora.Infrastructure/Repositories/OrganizationRepository.cs',
  'Valora.Infrastructure/Repositories/PlanRepository.cs',
  'Valora.Infrastructure/Repositories/Modules/ModuleRepository.cs',
  'Valora.Infrastructure/Repositories/Subscriptions/SubscriptionRepository.cs',
  'Valora.Infrastructure/Repositories/Usage/UsageRepository.cs',
  'Valora.Infrastructure/Repositories/Dashboard/DashboardMetricsRepository.cs'
];
for (const file of identityRepositories) {
  const source = read(file);
  reject(/\bdynamic\b/.test(source), `${file}: dynamic proibido na identidade`);
  reject(/SELECT\s+\*/i.test(source), `${file}: SELECT * proibido`);
}
const all = fs.readdirSync(path.join(backend, 'Valora.Api/Controllers'))
  .filter(x => x.endsWith('.cs')).map(x => read(`Valora.Api/Controllers/${x}`)).join('\n');
const identityWeb = ['core/auth-session.js', 'api/ajax-client.js', 'pages/login-page.js',
  'pages/register-company-page.js', 'pages/forgot-password-page.js',
  'pages/reset-password-page.js', 'pages/organization-page.js', 'pages/users-page.js'];
const web = identityWeb.map(x => read(`Valora.Web/wwwroot/js/${x}`)).join('\n');
const auth = read('Valora.Application/Services/Auth/AuthService.cs');
const jwt = read('Valora.Api/Configuration/JwtConfiguration.cs');
reject(/480/.test(jwt), 'access token nao pode ter duracao fixa de 480 minutos');
reject(/tokenPreview/.test(auth), 'tokenPreview proibido');
const applicationServices = fs.readdirSync(path.join(backend, 'Valora.Application/Services'), { recursive: true }).filter(x => x.endsWith('.cs')).map(x => read(`Valora.Application/Services/${x}`)).join('\n');
reject(/FEATURE_NOT_IMPLEMENTED/.test(applicationServices), 'e-mail operacional ainda nao implementado');
reject(fs.existsSync(path.join(backend, 'Valora.Application/Services/OperationalServices.cs')), 'OperationalServices.cs deve ser separado');
reject(fs.existsSync(path.join(backend, 'Valora.Infrastructure/Repositories/SaasRepositories.cs')), 'SaasRepositories.cs deve ser separado');
reject(/localStorage|sessionStorage/.test(web), 'tokens/sessao nao podem usar armazenamento do navegador');
reject(/Toast\.success[\s\S]{0,160}return\s+payload\s*\|\|\s*\{\s*ok:\s*true/.test(web), 'sucesso simulado detectado');
reject(/\[Http(?:Get|Post|Delete|Put)\("\/auth\//.test(all), 'rota de identidade nao versionada sem depreciacao');
if (failures.length) { console.error(failures.map(x => `- ${x}`).join('\n')); process.exit(1); }
console.log('Phase 2D identity vertical contract: OK');
