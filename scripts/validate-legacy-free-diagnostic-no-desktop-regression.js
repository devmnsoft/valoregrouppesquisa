const fs=require('fs');const css=fs.readFileSync('style.css','utf8');const app=fs.readFileSync('app.js','utf8');
function fail(m){console.error(`validate-legacy-free-diagnostic-no-desktop-regression: FAIL - ${m}`);process.exit(1)}
function stripMedia(s){return s.replace(/@media[^{}]*\{(?:[^{}]*\{[^{}]*\})*[^{}]*\}/g,'')}
const base=stripMedia(css).replace(/\s+/g,'');
if(/free-diagnostic-mobile-card/.test(app))fail('classe mobile usada no app.js');
if(/\.free-diagnostic-copy\{[^}]*text-align:center/.test(base))fail('free-diagnostic-copy centralizado fora de media query');
if(/\.free-diagnostic-layout\{[^}]*grid-template-columns:1fr/.test(base))fail('layout 1fr fora de media query');
if(/\.free-diagnostic-start-card[^}]*width:min\(100%,300px\)/.test(base))fail('start-card 300px fora de media query');
if(/\.free-diagnostic-benefits\{[^}]*grid-template-columns:1fr/.test(base))fail('benefits 1fr fora de media query');
console.log('validate-legacy-free-diagnostic-no-desktop-regression: PASS');
