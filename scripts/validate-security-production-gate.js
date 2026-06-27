const { assert, exists, readIf, allFiles, pass, scanForbiddenFrontendSecrets } = require('./production-gate-utils');

const csFiles = allFiles(['backend'], ['.cs']);
const cs = csFiles.map(readIf).join('\n');
const program = readIf('backend/Valora.Api/Program.cs');
const jwt = readIf('backend/Valora.Api/Configuration/JwtConfiguration.cs');
const errors = readIf('backend/Valora.Api/Middleware/ErrorHandlingMiddleware.cs');
const firebase = readIf('firebase.json');
const rules = readIf('firestore.rules');
const config = readIf('config.js');
const securityDocs = [
  'SECURITY_PRODUCTION_READINESS.md',
  'LGPD_PRODUCTION_READINESS.md',
  'AUDIT_LOG_POLICY.md',
  'SENSITIVE_LOG_DATA_POLICY.md'
];

securityDocs.forEach((file) => assert(exists(file), `${file} missing`));

assert(/ValidateIssuer\s*=\s*true/.test(jwt) && /ValidIssuer\s*=\s*jwt\["Issuer"\]/.test(jwt), 'JWT issuer validation missing');
assert(/ValidateAudience\s*=\s*true/.test(jwt) && /ValidAudience\s*=\s*jwt\["Audience"\]/.test(jwt), 'JWT audience validation missing');
assert(/IssuerSigningKey/.test(jwt) && !/Secret"\s*:\s*"(?!\$\{|CHANGE_ME|__|trocar)[^"]{16,}"/.test(readIf('backend/Valora.Api/appsettings.json')), 'JWT signing key or production secret policy invalid');

assert(/UseMiddleware<CorrelationIdMiddleware>\(\)/.test(program), 'CorrelationId middleware missing');
assert(/UseMiddleware<ErrorHandlingMiddleware>\(\)/.test(program), 'JSON error middleware missing');
assert(/UseAuthentication\(\)/.test(program) && /UseAuthorization\(\)/.test(program), 'authentication/authorization pipeline missing');
assert(/StatusCodes\.Status500InternalServerError/.test(errors) && /application\/json/.test(errors) && !/StackTrace/.test(errors), 'production JSON error handling leaks stack or is incomplete');

assert(/X-Frame-Options/.test(firebase) && /Content-Security-Policy/.test(firebase) && /X-Content-Type-Options/.test(firebase), 'Firebase hosting security headers missing');
assert(/allow read, write: if false|signedIn\(\)|request\.auth/.test(rules), 'Firestore rules do not preserve authenticated/deny controls');
assert(!/service_account|private_key|client_email/.test(config), 'frontend contains Firebase service account markers');
scanForbiddenFrontendSecrets(['config.js', 'index.html', 'app.js', 'api-client.js', 'api-repository.js', 'repository.js', 'gateway-client.js', 'runtime-capabilities.js']);

const adminControllers = ['backend/Valora.Api/Controllers/CommunicationsController.cs', 'backend/Valora.Api/Controllers/AdminDatabaseController.cs', 'backend/Valora.Api/Controllers/MigrationController.cs'];
adminControllers.forEach((file) => {
  const text = readIf(file);
  if (text) assert(/\[Authorize\]/.test(text), `${file} admin endpoint lacks [Authorize]`);
});

['backend/Valora.Api/Controllers/PublicController.cs', 'backend/Valora.Api/Controllers/AdminController.cs'].forEach((file) => {
  const text = readIf(file);
  assert(/LegacyEnabled/.test(text) && /legacy_route_disabled/.test(text), `${file} legacy routes are not guarded`);
});

assert(/ALLOW_API_PRODUCTION_CUTOVER:\s*false/.test(config), 'ALLOW_API_PRODUCTION_CUTOVER=false missing');
assert(/DATA_PROVIDER:\s*['"]firebase['"]/.test(config), 'DATA_PROVIDER=firebase production default missing');

const logCalls = cs.split('\n').filter((line) => /Log(Information|Warning|Error|Debug|Trace)|Console\.(WriteLine|Error|Log)/.test(line)).join('\n');
const forbiddenLogMarkers = /(password_hash|smtp password|private_key|connection string completa|token puro|CPF completo|telefone completo|e-mail completo sem máscara)/i;
assert(!forbiddenLogMarkers.test(logCalls), 'sensitive logging marker found in backend log calls');

const secDoc = securityDocs.map(readIf).join('\n');
['CORS', 'rate limit', 'payload', 'stack trace', 'legacy', 'Firebase'].forEach((term) => {
  assert(new RegExp(term, 'i').test(secDoc + cs + firebase), `security control missing from code/docs: ${term}`);
});

pass('validate-security-production-gate');
