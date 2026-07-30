const fs = require('fs');
const path = require('path');
const root = process.cwd();
const fail = [];
function read(p){ return fs.readFileSync(path.join(root,p),'utf8'); }
function walk(dir, out=[]){ for (const e of fs.readdirSync(path.join(root,dir), {withFileTypes:true})) { const p=path.join(dir,e.name); if (e.isDirectory()) { if (!['bin','obj','node_modules'].includes(e.name)) walk(p,out); } else out.push(p); } return out; }
for (const p of walk('backend').filter(p=>p.endsWith('.csproj'))) { const s=read(p); if (!s.includes('<TargetFramework>net10.0</TargetFramework>')) fail.push(`${p} nao usa net10.0`); if (/Version="/.test(s)) fail.push(`${p} ainda fixa versao de PackageReference`); }
const web = read('backend/Valora.Web/Valora.Web.csproj');
if (/Npgsql|Dapper/i.test(web)) fail.push('Valora.Web referencia pacote de banco');
const backendFiles = walk('backend').filter(p=>/\.(cs|csproj|json)$/.test(p) && !p.includes('Valora.Tests'));
for (const p of backendFiles) { const s=read(p); if (/Firebase/i.test(s)) fail.push(`${p} referencia Firebase no backend oficial`); if (/devnull@example\.com|retorna sucesso sem persistencia|ok: true/i.test(s)) fail.push(`${p} contem marcador de dado simulado`); }
const sql = read('backend/database/postgresql/script_completo.sql');
for (const term of ['CREATE SCHEMA IF NOT EXISTS valorapesquisa','CREATE TABLE IF NOT EXISTS organizations','ON CONFLICT','free','professional','corporate','enterprise','Cultura e Propósito']) if (!sql.includes(term)) fail.push(`script_completo.sql sem ${term}`);
if (/DROP\s+(TABLE|SCHEMA)/i.test(sql)) fail.push('script_completo.sql contem DROP destrutivo');
if (fail.length) { console.error('Validacao Fase 1 falhou:\n- '+fail.join('\n- ')); process.exit(1); }
console.log('Validacao estatica Fase 1 concluida. Execute dotnet build/test/format e Testcontainers em ambiente com .NET 10 e Docker.');
