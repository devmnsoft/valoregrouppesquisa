const fs=require('fs');const css=fs.readFileSync('style.css','utf8');
function fail(m){console.error(`validate-legacy-free-diagnostic-mobile-only: FAIL - ${m}`);process.exit(1)}
const media=css.match(/@media \(max-width:760px\)\{[\s\S]*?@media \(max-width:420px\)/);
if(!media)fail('media query max-width 760px ausente');
const m=media[0];
for(const needle of ['.free-diagnostic-copy{text-align:center','grid-template-columns:1fr','max-width:300px','width:min(100%,320px)','max-width:240px']) if(!m.includes(needle))fail(`regra mobile ausente: ${needle}`);
console.log('validate-legacy-free-diagnostic-mobile-only: PASS');
