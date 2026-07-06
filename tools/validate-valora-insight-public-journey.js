#!/usr/bin/env node
const fs=require('fs');const cp=require('child_process');function read(p){return fs.existsSync(p)?fs.readFileSync(p,'utf8'):''}function ok(c,m){if(!c){console.error('FAIL:',m);process.exit(1)}console.log('OK:',m)}
const files=['backend/Valora.Web/Views/Home/Index.cshtml','backend/Valora.Web/Views/PublicPages/FreeDiagnostic.cshtml','backend/Valora.Web/Views/PublicSurvey/Take.cshtml','backend/Valora.Web/Views/Results/Public.cshtml','backend/Valora.Web/Views/Certificates/Details.cshtml','backend/Valora.Web/Views/Lgpd/Index.cshtml','database/postgresql/013_seed_valora_insight_questions.sql','backend/Valora.Application/Results/ValoraInsightCalculator.cs','backend/Valora.Application/Results/ValoraInsightDevolutivaService.cs'];
files.forEach(f=>ok(fs.existsSync(f),`${f} existe`));const sql=read('database/postgresql/013_seed_valora_insight_questions.sql');
['Cultura e Propósito','Gestão e Governança','Liderança','Pessoas e Talentos','Resultados e Crescimento'].forEach(d=>ok(sql.includes(d),`dimensão ${d}`));
ok((sql.match(/','/g)||[]).length>=25 && (sql.match(/Discordo totalmente|Concordo totalmente/g)||[]).length>=2,'25 perguntas e escala 1 a 5 existem no seed');
ok(!/pergunta\s+[1-5]/i.test(sql),'seed oficial não contém perguntas genéricas');
const result=read('backend/Valora.Web/Views/Results/Public.cshtml')+read('backend/Valora.Application/Results/ValoraInsightCalculator.cs')+read('backend/Valora.Application/Results/ValoraInsightDevolutivaService.cs');
['Leitura executiva','Diagnóstico por dimensão','Radar','Benchmarking','Verdade estratégica','Risco','Próximo nível','Transição','CTA'].forEach(s=>ok(result.toLowerCase().includes(s.toLowerCase()),`resultado/devolutiva contém ${s}`));
ok(/WhatsApp|551|wa\.me/i.test(read('backend/Valora.Web/Views/Shared/Public/_PublicFloatingActions.cshtml')+result),'CTA WhatsApp existe');
ok(/LGPD|consent/i.test(read('backend/Valora.Web/Views/Lgpd/Index.cshtml')+read('backend/Valora.Web/Views/PublicSurvey/Take.cshtml')),'LGPD/consentimento existe');
ok(/certificado|certificate/i.test(read('backend/Valora.Web/Views/Certificates/Details.cshtml')+result),'certificado existe');
ok(/e-mail|email/i.test(result+read('backend/Valora.Web/Views/PublicSurvey/Take.cshtml')),'e-mail existe');
const webFiles=cp.execSync('find backend/Valora.Web -type f',{encoding:'utf8'}).trim().split(/\n/).filter(Boolean);ok(!webFiles.some(f=>/firebase/i.test(read(f))),'Web oficial não usa Firebase');
