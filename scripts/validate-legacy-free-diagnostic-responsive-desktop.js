const fs=require('fs');
const css=fs.readFileSync('style.css','utf8');
const app=fs.readFileSync('app.js','utf8');
function fail(m){console.error(`validate-legacy-free-diagnostic-responsive-desktop: FAIL - ${m}`);process.exit(1)}
function ok(c,m){if(!c)fail(m)}
ok(!/free-diagnostic-mobile-card/.test(app),'app.js ainda usa free-diagnostic-mobile-card como container');
ok(/class="[^"]*free-diagnostic-section/.test(app)&&/class="[^"]*free-diagnostic-layout/.test(app),'HTML não contém wrappers desktop free-diagnostic-section/layout');
ok(/\.free-diagnostic-layout\{[^}]*grid-template-columns:minmax\(0,1\.05fr\) minmax\(360px,\.95fr\)/.test(css),'desktop não define grid de duas colunas em .free-diagnostic-layout');
ok(/\.free-diagnostic-copy\{[^}]*text-align:left/.test(css),'desktop não mantém copy alinhado à esquerda');
ok(/\.free-diagnostic-start-card,\.free-diagnostic-preview-card\{[^}]*max-width:460px/.test(css),'card desktop não usa max-width premium de 460px');
console.log('validate-legacy-free-diagnostic-responsive-desktop: PASS');
