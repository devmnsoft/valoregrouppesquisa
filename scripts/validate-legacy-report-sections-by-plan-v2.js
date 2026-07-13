const fs=require('fs');
const corpus=['app.js','functions/index.js','report-service.js','pdf.js','repository.js','firebase-repository.js','style.css','LEGACY_SAAS_CONSOLIDATION_NEXT_EVOLUTION_AUDIT.md'].filter(fs.existsSync).map(f=>fs.readFileSync(f,'utf8')).join('\n');
const missing=["getReportSectionsForPlan","showEvolutionPlan30_60_90","showUnitComparison","showCompanyComparison","showExecutiveFollowUp"].filter(x=>!corpus.includes(x));
if(missing.length){console.error('Missing legacy validation markers for report-sections-by-plan-v2:',missing.join(', '));process.exit(1);}
if(/certifica[cç][aã]o GPTW|ranking oficial GPTW|nota GPTW|base oficial GPTW/i.test(corpus)&&!corpus.includes('Não representa certificação GPTW')){console.error('Unsafe GPTW wording');process.exit(1);}
console.log('OK legacy report-sections-by-plan-v2');
