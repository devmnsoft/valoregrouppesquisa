const fs=require('fs');
const files=['backend/Valora.Web/Controllers/WebConfigController.cs','backend/Valora.Web/Views/Shared/_Layout.cshtml','backend/Valora.Web/appsettings.json','backend/Valora.Web/appsettings.Development.json'];
for(const f of files) if(!fs.existsSync(f)) throw new Error(`${f} ausente`);
const c=fs.readFileSync(files[0],'utf8'), l=fs.readFileSync(files[1],'utf8'), app=fs.readFileSync(files[2],'utf8');
['/web-config.js','IOptions<ApiOptions>','IOptions<WebAppOptions>','IWebHostEnvironment','CacheControl','Object.freeze','JsonSerializer'].forEach(t=>{if(!c.includes(t)) throw new Error(`WebConfigController sem ${t}`)});
if(!l.includes('<script src="/web-config.js"></script>') || l.indexOf('/web-config.js')>l.indexOf('/js/config.js')) throw new Error('Layout não carrega /web-config.js antes do fallback config.js');
if(/ConnectionStrings|Password=|private_key|smtp password|token_hash|result_token_hash/i.test(app)) throw new Error('appsettings do Web expõe segredo');
console.log('validate-aspnet-web-dynamic-config: PASS');
