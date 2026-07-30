'use strict';
const fs = require('fs');
const path = require('path');
const backend = path.resolve(__dirname, '../..');
const read = file => fs.readFileSync(file, 'utf8');
const files = directory => fs.readdirSync(directory, { withFileTypes: true }).flatMap(entry => entry.isDirectory() ? files(path.join(directory, entry.name)) : [path.join(directory, entry.name)]);
const reject = (condition, message) => { if (condition) throw new Error(`Phase 2G.1: ${message}`); };

reject(fs.existsSync(path.join(backend, 'backend')), 'diretório backend duplicado');
const maintained = new Set(['OperationalStaticContractTests.cs', 'EmailQueueTests.cs', 'DatabaseScriptCompletoTests.cs', 'AdminRepositoryMigrationTests.cs', 'OfficialBackendConsolidationTests.cs']);
const tests = files(path.join(backend, 'Valora.Tests')).filter(file => maintained.has(path.basename(file)));
for (const file of tests) {
  const source = read(file);
  reject(/AppContext\.BaseDirectory[^\n]*(\.\.\/){2,}|FindRepositoryRoot|EncontrarRaiz/.test(source), `${path.relative(backend, file)} resolve caminhos por conta própria`);
}
const sql = read(path.join(backend, 'database/postgresql/script_completo.sql'));
reject((sql.match(/CREATE TABLE IF NOT EXISTS plan_usage_counters/gi) || []).length !== 1, 'bootstrap contém mais de uma definição de contador');
reject(/\bresource_code\b|\bused_value\b|\bamount\b/.test(sql), 'bootstrap usa colunas de consumo obsoletas');
const auth = read(path.join(backend, 'Valora.Application/Services/Auth/AuthService.cs'));
reject(/CreateAsync\([\s\S]{0,300}CreateSubscriptionAsync[\s\S]{0,300}users\.CreateAsync/.test(auth), 'cadastro ainda coordena repositórios independentes');
const bff = read(path.join(backend, 'Valora.Web/Services/Bff/BffAuthenticationService.cs'));
reject(/BffSafeSession\([^)]*(AccessToken|RefreshToken)/.test(bff), 'token exposto no contrato seguro do navegador');
console.log('Phase 2G.1 registration gate: OK');
