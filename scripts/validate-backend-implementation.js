#!/usr/bin/env node
const fs = require('fs');
const path = require('path');
const fail = [];
const exists = p => fs.existsSync(p);
const read = p => exists(p) ? fs.readFileSync(p, 'utf8') : '';
const files = dir => exists(dir) ? fs.readdirSync(dir, { withFileTypes:true }).flatMap(d => {
  const p = path.join(dir, d.name); return d.isDirectory() ? files(p) : [p];
}) : [];
const must = (cond, msg) => { if (!cond) fail.push(msg); };

must(exists('backend/Valora.sln'), 'backend/Valora.sln não existir');
for (const p of ['Valora.Api','Valora.Application','Valora.Domain','Valora.Infrastructure','Valora.Tests']) must(exists(`backend/${p}`), `${p} não existir`);
must(exists('backend/Valora.Api/appsettings.json') && exists('backend/Valora.Api/appsettings.Development.json'), 'appsettings obrigatórios ausentes');
const apiFiles = files('backend/Valora.Api');
const infraFiles = files('backend/Valora.Infrastructure');
const appFiles = files('backend/Valora.Application');
const allBackend = [...apiFiles, ...infraFiles, ...appFiles].filter(f => /\.(cs|json)$/.test(f)).map(read).join('\n');
must(apiFiles.some(f => f.includes('Controllers') && f.endsWith('Controller.cs')), 'não houver controllers');
must(/Dapper/.test(allBackend) && /NpgsqlConnection/.test(allBackend), 'não houver repositories Dapper/Npgsql');
for (const name of ['IDbConnectionFactory','PostgresConnectionFactory','IOrganizationRepository','IUserRepository','IPlanRepository','IFormRepository','ISurveyRepository','IResponseRepository','IResultRepository','ICertificateRepository','ICommunicationRepository','IAuditRepository','IMigrationRepository']) must(allBackend.includes(name), `contrato/implementação ausente: ${name}`);
for (const endpoint of ['/health','/health/database','/health/config','/admin/architecture/status','/plans/public','/auth/register-company','/auth/login','/auth/forgot-password','/me','/public/surveys/{surveyId}/validate','/public/surveys/{surveyId}/responses','/public/results/{responseId}','/responses/{responseId}/certificate.pdf','/responses/{responseId}/certificate.png','/communications/result/{responseId}/send-email','/communications','/admin/migration/status']) {
  const escaped = endpoint.replace(/[.*+?^${}()|[\]\\]/g, '\\$&').replace(/\\\{[^}]+\\\}/g, '[^"`\']+');
  must(new RegExp(escaped).test(allBackend), `endpoint mínimo ausente: ${endpoint}`);
}
const endpointCode = apiFiles.filter(f => f.includes('Controllers')).map(read).join('\n');
must(!/StatusCode\s*\(\s*501|NotImplementedException|NotImplemented/.test(endpointCode), 'há NotImplemented/501 em endpoint obrigatório');
must(/BCrypt|password_hash|Hash\(/.test(allBackend), 'hash de senha não evidenciado');
must(/MigrationRunner/.test(allBackend), 'MigrationRunner ausente');
if (fail.length) { console.error(fail.join('\n')); process.exit(1); }
console.log('validate-backend-implementation: PASS');
