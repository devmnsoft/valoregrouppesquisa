const fs=require('fs');const app=fs.readFileSync('app.js','utf8');function fail(m){console.error(m);process.exit(1)}
for(const s of ['function normalizeResultRenderError','result_render_error','Seu resultado foi registrado, mas não conseguimos montar a devolutiva completa agora']) if(!app.includes(s)) fail(`Tratamento público ausente: ${s}`);
if(!/isReference\s*=\s*error\?\.name\s*===\s*['"]ReferenceError['"]\s*\|\|\s*\/is not defined\/i/.test(app)) fail('normalizeResultRenderError não normaliza ReferenceError/is not defined.');
if(/Código:\s*\$\{esc\(code\)\}/.test(app)&&!/result_render_error/.test(app)) fail('Código público não normalizado para result_render_error.');
console.log('OK result render no raw ReferenceError');
