const fs = require('fs');
const path = require('path');
const root = path.resolve(__dirname, '../..');
const repo = path.resolve(root, '..');
const failures = [];
const checkedAt = new Date().toISOString();
const read = file => fs.existsSync(file) ? fs.readFileSync(file, 'utf8') : '';
const walk = directory => fs.existsSync(directory) ? fs.readdirSync(directory, { recursive: true }).map(item => path.join(directory, item)).filter(item => fs.existsSync(item) && fs.statSync(item).isFile()) : [];
const fail = (rule, file, detail) => failures.push({ rule, file: path.relative(repo, file), detail });
const scan = (files, rule, pattern, detail) => files.forEach(file => { if (pattern.test(read(file))) fail(rule, file, detail); });

const testFiles = walk(path.join(root, 'Valora.Tests')).filter(f => f.endsWith('.cs'));
const controllerFiles = walk(path.join(root, 'Valora.Api', 'Controllers')).filter(f => f.endsWith('.cs'));
const contractFiles = walk(path.join(root, 'Valora.Application', 'DTOs')).filter(f => f.endsWith('.cs'));
const browserFiles = walk(path.join(root, 'Valora.Web', 'wwwroot', 'js')).filter(f => f.endsWith('.js'));
const viewFiles = walk(path.join(root, 'Valora.Web', 'Views')).filter(f => f.endsWith('.cshtml'));
const workflowFiles = walk(path.join(repo, '.github', 'workflows')).filter(f => /\.ya?ml$/.test(f));

const removedReferences = /OperationalDtos\.cs|MigrationDtos\.cs|OfficialConsolidationDtos\.cs|banco_completo\.sql|050_reports_certificates_exports_lgpd_email\.sql|database\/postgresql\/migrations/;
scan([...testFiles, ...walk(path.join(root, 'tools')).filter(f => f !== __filename), ...workflowFiles], 'stale-reference', removedReferences, 'Reference to a retired aggregate contract or modular database asset.');
if (fs.existsSync(path.join(root, 'database', 'postgresql', 'migrations'))) fail('single-sql', path.join(root, 'database', 'postgresql', 'migrations'), 'A migrations directory is not allowed.');
if (walk(path.join(root, 'database', 'postgresql')).filter(f => f.endsWith('.sql')).length !== 1) fail('single-sql', path.join(root, 'database', 'postgresql'), 'Exactly one PostgreSQL SQL file is required.');
scan(workflowFiles, 'retired-workflow-step', new RegExp('Apply ' + 'migrations', 'i'), 'Retired database execution name.');
scan(testFiles, 'vacuous-test', /Assert\.True\(true\)|if\s*\([^)]*(?:CONNECTION|POSTGRES)[^)]*null[^)]*\)\s*return/si, 'A mandatory test may not pass without exercising its contract.');
scan(controllerFiles.filter(f => /(?:Organization|Users|UserInvitations|UserSessions)Controller\.cs$/.test(f)), 'controller-boundary', /I(?:Organization|User|Plan|Audit)Repository/, 'Organization and user controllers may depend only on application services.');
scan(contractFiles, 'typed-contract', /JsonElement|Dictionary\s*<\s*string\s*,\s*object|\bdynamic\b/, 'A public DTO must be strongly typed.');
scan(controllerFiles, 'direct-user-creation', /RandomNumberGenerator[\s\S]{0,500}users\.CreateAsync/, 'Direct user creation with a generated password is forbidden.');
scan(viewFiles, 'unfinished-ui', /Aguardando API|Opera(?:ç|c)[aã]o indispon[ií]vel/, 'Unfinished UI copy is forbidden.');
scan(browserFiles, 'browser-api-boundary', /fetch\s*\(\s*['"`]\/api\/|(?:localStorage|sessionStorage)\.(?:setItem|getItem)\s*\([^)]*(?:jwt|token)/i, 'The browser must use the BFF and must not persist tokens.');
scan([...viewFiles, ...browserFiles], 'remote-asset', /https?:\/\/[^\s'"`)]+\.(?:js|css|woff2?)/i, 'Runtime assets must be local.');
const sidebar = path.join(root, 'Valora.Web', 'Views', 'Shared', '_Sidebar.cshtml');
if (/new\s*\[|new\s+List|\{\s*"Dashboard"/.test(read(sidebar))) fail('dynamic-navigation', sidebar, 'Sidebar contains a hardcoded navigation catalog.');

const report = { phase: '2K.3', checkedAt, status: failures.length ? 'failed' : 'passed', checks: 12, failures };
const output = path.join(root, 'artifacts', 'phase2k3-validation.json');
fs.mkdirSync(path.dirname(output), { recursive: true });
fs.writeFileSync(output, JSON.stringify(report, null, 2) + '\n');
console.log(JSON.stringify(report, null, 2));
process.exitCode = failures.length ? 1 : 0;
