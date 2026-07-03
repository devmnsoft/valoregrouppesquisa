const fs=require('fs');const s=fs.readFileSync('app.js','utf8');function fail(m){console.error(m);process.exit(1)}
const m=s.match(/async function tryEnhancePublicResult[\s\S]*?\n}\nasync function reloadPublicResult/);if(!m)fail('tryEnhancePublicResult não existe');
if(!/try\s*{[\s\S]*renderResult/.test(m[0])||!/catch\s*\(err\)/.test(m[0]))fail('tryEnhancePublicResult não tem try/catch em renderResult');
console.log('OK result enhance safe');
