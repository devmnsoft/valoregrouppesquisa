const fs=require('fs'); const req=['backend/Valora.Web/web.config','backend/Valora.Web/Views','backend/Valora.Web/wwwroot','backend/Valora.Web/Controllers/WebConfigController.cs','tools/windows/web/05-publicar-web-iis-dry-run.bat']; for(const r of req) if(!fs.existsSync(r)) throw new Error(`${r} ausente`);
const app=fs.readFileSync('backend/Valora.Web/appsettings.json','utf8'); if(/ConnectionStrings|Password=|private_key|smtp password/i.test(app)) throw new Error('appsettings expõe segredo');
console.log('validate-aspnet-web-iis-publish-dry-run: PASS');
