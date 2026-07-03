const fs=require('fs');const s=fs.readFileSync('app.js','utf8');function fail(m){console.error(m);process.exit(1)}
const h=s.slice(s.indexOf('function handlePublicSubmitSuccess'),s.indexOf('\n\nfunction assertPublicSubmitPayloadReady'));
if(/catch\s*\([^)]*\)\s*{[^}]*renderFallbackResultAfterSubmit/s.test(h))fail('submit success ainda troca sucesso por fallback de erro');
if(!/tryEnhancePublicResult[\s\S]*catch\s*\(err\)[\s\S]*lastResultEnhanceError/.test(s))fail('enhance não preserva tela com diagnostics');
console.log('OK result render does not replace success');
