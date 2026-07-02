const fs=require('fs'),path=require('path');function fail(m){console.error(m);process.exit(1)}
const dir='dist/assets';if(!fs.existsSync(dir)){console.warn('dist/assets não existe; rode build:prod para validar bundle.');process.exit(0)}
const files=fs.readdirSync(dir).filter(f=>/^app.*\.js$/.test(f));if(!files.length){console.warn('Nenhum app*.js em dist/assets; rode build:prod.');process.exit(0)}
for(const f of files){const s=fs.readFileSync(path.join(dir,f),'utf8');if(s.includes('recommendationFor(')&&!s.includes('function recommendationFor'))fail(`${f}: chama recommendationFor mas bundle não declara function recommendationFor.`);for(const h of ['normalizeResultRenderError','renderFallbackResultAfterSubmit','result_render_error']) if(!s.includes(h)) fail(`${f}: ausente ${h}`)}
console.log('OK dist result helpers definidos');
