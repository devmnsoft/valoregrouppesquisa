#!/usr/bin/env node
const fs=require('fs');
function read(f){return fs.readFileSync(f,'utf8')}
function assert(c,m){if(!c){console.error('FAIL:',m);process.exitCode=1}}
const app=read('app.js'), css=read('style.css');
assert(app.includes('aria-controls="adminSidebar"'),'toggle sem aria-controls');
assert(app.includes('ensureAdminMobileOverlay();bindAdminMobileMenuEvents();renderPortalTab'),'bind apos render admin ausente');
assert(app.includes("event.key==='Escape'")||app.includes('event.key === \'Escape\''),'ESC não fecha');
assert(css.includes('100dvh'),'height 100dvh ausente');
assert(css.includes('z-index:1200'),'z-index sidebar insuficiente');
if(process.exitCode)process.exit(1);
console.log('OK validate-admin-mobile-menu-runtime-contract.js');
