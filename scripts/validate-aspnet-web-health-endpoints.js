const fs=require('fs'); const f='backend/Valora.Web/Controllers/WebHealthController.cs'; if(!fs.existsSync(f)) throw new Error(`${f} ausente`); const c=fs.readFileSync(f,'utf8');
['/health/web','/health/web/version','/health/web/config','ILogger<WebHealthController>','try','catch','correlationId','MaskUrl'].forEach(t=>{if(!c.includes(t)) throw new Error(`WebHealthController sem ${t}`)});
if(/DbConnection|Npgsql|Dapper|Firebase|ConnectionStrings/i.test(c)) throw new Error('Health Web não deve acessar dados/segredos');
console.log('validate-aspnet-web-health-endpoints: PASS');
