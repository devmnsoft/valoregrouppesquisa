const fs=require('fs');const s=fs.readFileSync('app.js','utf8');
for(const x of ['const LEGACY_PUBLIC_ACTIONS =','async function legacyRun','dataset?.run','dataset?.fn','dataset?.legacyAction','LEGACY_PUBLIC_ACTIONS[name]']) if(!s.includes(x)) throw new Error('legacyRun allowlist ausente: '+x);
if(/eval\s*\(|window\s*\[\s*name\s*\]/.test(s)) throw new Error('legacyRun inseguro');
console.log('legacy run allowlist: PASS');
