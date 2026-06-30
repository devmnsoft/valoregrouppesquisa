const fs=require('fs'); const dirs=['Home','Account','Dashboard','Organization','Plans','Users','Forms','Surveys','PublicSurvey','Results','Certificates','Responses','Communications','Operations','EnvironmentStatus','Audit','Settings'];
for (const d of dirs) if(!fs.existsSync(`backend/Valora.Web/Views/${d}`)) throw new Error(`Página/pasta Web ausente: ${d}`);
const layout=fs.readFileSync('backend/Valora.Web/Views/Shared/_Layout.cshtml','utf8'); if(layout.includes('React')||layout.includes('Vue')||layout.includes('Angular')) throw new Error('Framework SPA proibido encontrado.');
console.log('Paridade Web x legado validada estaticamente.');
