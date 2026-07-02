const fs=require('fs');const s=fs.readFileSync('app.js','utf8');function fail(m){console.error(m);process.exit(1)}
const h=s.slice(s.indexOf('function handlePublicSubmitSuccess'),s.indexOf('\n\nfunction assertPublicSubmitPayloadReady'));
if(h.includes('publicApiFriendlyError')||h.includes('renderFallbackResultAfterSubmit'))fail('result_render_error pode virar tela final após submit');
if(!s.includes('lastResultRenderError')||!s.includes('originalMessage'))fail('diagnostics não salva originalMessage');
console.log('OK result_render_error not final');
