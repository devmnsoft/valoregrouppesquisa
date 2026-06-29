const fs = require('fs');
const path = require('path');

function fail(message) {
  console.error(message);
  process.exit(1);
}
function must(condition, message) { if (!condition) fail(message); }
function read(file) { return fs.readFileSync(file, 'utf8'); }
function walk(dir) {
  if (!fs.existsSync(dir)) return [];
  return fs.readdirSync(dir, { withFileTypes: true }).flatMap((entry) => {
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) return walk(full);
    return [full];
  });
}

const pkg = JSON.parse(read('package.json'));
const csprojPath = 'backend/Valora.Web/Valora.Web.csproj';
const slnPath = 'backend/Valora.sln';

must(fs.existsSync(csprojPath), 'backend/Valora.Web/Valora.Web.csproj ausente');
must(fs.existsSync(slnPath), 'backend/Valora.sln ausente');

const csproj = read(csprojPath);
const sln = read(slnPath);
must(csproj.includes('Microsoft.NET.Sdk.Web'), 'Valora.Web deve usar Microsoft.NET.Sdk.Web');
must(/<TargetFramework>\s*net8\.0\s*<\/TargetFramework>/.test(csproj), 'Valora.Web deve usar net8.0');
must(sln.includes('Valora.Web\\Valora.Web.csproj') || sln.includes('Valora.Web/Valora.Web.csproj'), 'Valora.Web deve estar dentro de backend/Valora.sln');
must((pkg.scripts['web:run'] || '').includes('dotnet run') && (pkg.scripts['web:run'] || '').includes('backend/Valora.Web/Valora.Web.csproj'), 'web:run deve usar dotnet run no Valora.Web');
must((pkg.scripts['web:build'] || '').includes('dotnet build') && (pkg.scripts['web:build'] || '').includes('backend/Valora.Web/Valora.Web.csproj'), 'web:build deve usar dotnet build no Valora.Web');
must(!fs.existsSync('frontend-web/server.js'), 'frontend-web/server.js não pode existir como front oficial paralelo');

const officialWebFiles = walk('backend/Valora.Web').filter((file) => /\.(cshtml|js|json|csproj|cs)$/.test(file));
const forbiddenFrameworkMarkers = [
  'react', 'react-dom', '@angular/core', '@vue/', 'createApp(', 'new Vue(', 'ng serve', 'vite --host', 'next dev'
];
const hits = [];
for (const file of officialWebFiles) {
  const lower = read(file).toLowerCase();
  for (const marker of forbiddenFrameworkMarkers) {
    if (lower.includes(marker.toLowerCase())) hits.push(`${file}: ${marker}`);
  }
}
if (hits.length) fail(`Framework SPA indevido no Valora.Web oficial:\n${hits.join('\n')}`);

console.log('validate-valora-web-is-aspnet: PASS');
