'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const { classifyFile } = require('./validate-project-boundaries');

test('accepts the official solution and Valora namespaces', () => {
  assert.deepEqual(classifyFile('backend/Valora.sln', 'Project("Valora.Api")'), []);
  assert.deepEqual(classifyFile('backend/Valora.Api/Program.cs', 'namespace Valora.Api;'), []);
});

test('rejects every configured external product in paths and source', () => {
  for (const product of ['HabitFlow', 'InovaGed', 'IntegraRP', 'SIGOV', 'PlantaoPro', 'OrcaFacil']) {
    assert.ok(classifyFile(`docs/${product}.md`, '').includes('caminho de produto externo'));
    assert.ok(classifyFile('backend/Example.cs', `namespace ${product}.Api;`).includes('conteúdo de produto externo'));
  }
});

test('rejects parallel solutions, projects and copied migrations', () => {
  assert.ok(classifyFile('backend/Other.sln').includes('solution não oficial'));
  assert.ok(classifyFile('src/Other/Other.csproj').includes('projeto .NET fora do backend oficial'));
  assert.ok(classifyFile('migration/copied.sql').includes('migration fora do backend oficial'));
});
