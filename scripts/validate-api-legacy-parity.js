const fs=require('fs'), path=require('path'); const dir='backend/Valora.Api/Controllers';
const all=fs.readdirSync(dir).filter(f=>f.endsWith('.cs')).map(f=>fs.readFileSync(path.join(dir,f),'utf8')).join('\n');
const names=['Auth','Organizations','Plans','Surveys','Responses','Certificates','Communications','Operations'];
for (const n of names) if(!all.includes(`class ${n}`)&&!all.includes(`${n}Controller`)) throw new Error(`Controller obrigatório não localizado: ${n}`);
const repo=fs.readFileSync('backend/Valora.Infrastructure/Repositories/SurveyRepository.cs','utf8');
for (const term of ['IsFreeOfficialSurvey','FreeOfficialSql','sl.revoked_at IS NULL','o.status=\'active\'']) if(!repo.includes(term)) throw new Error(`Regra API/free survey ausente: ${term}`);
console.log('Paridade API x legado validada estaticamente.');
