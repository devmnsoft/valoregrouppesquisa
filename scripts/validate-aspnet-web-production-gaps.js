const fs=require('fs'); const f='ASPNET_WEB_API_GAPS.md'; if(!fs.existsSync(f))throw new Error(`${f} ausente`); const s=fs.readFileSync(f,'utf8');
['Endpoints existentes e consumidos pelo Valora.Web','Endpoints existentes e ainda não consumidos','Endpoints faltantes bloqueantes','Endpoints faltantes não bloqueantes','Fallbacks temporários permitidos','Fallbacks temporários proibidos'].forEach(t=>{if(!s.includes(t))throw new Error('seção ausente: '+t)});
if(/bloqueia produção:\s*sim(?![\s\S]{0,240}(endpoint criado|justificativa|fallback controlado))/i.test(s)){console.error('Gap bloqueante sem endpoint/justificativa/fallback controlado');process.exit(1)}
console.log('validate-aspnet-web-production-gaps: PASS');
