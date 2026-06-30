const fs = require('fs');
function read(p){ return fs.readFileSync(p, 'utf8'); }
function fail(m){ console.error('FAIL:', m); process.exitCode = 1; }
function ok(m){ console.log('OK:', m); }
function versionAtLeast(v, min='8.7.2'){ const a=v.split('.').map(Number), b=min.split('.').map(Number); for(let i=0;i<3;i++){ if((a[i]||0)>(b[i]||0)) return true; if((a[i]||0)<(b[i]||0)) return false; } return true; }
if(!fs.existsSync('legacy-admin-mobile-menu-bridge.js')) fail('bridge inexistente'); else ok('bridge existe');
const index = read('index.html'), bridge = read('legacy-admin-mobile-menu-bridge.js'), css = read('style.css'), config = read('config.js');
const appPos = index.indexOf('app.js?v='); const bridgePos = index.indexOf('legacy-admin-mobile-menu-bridge.js?v=');
if(appPos < 0 || bridgePos < 0 || bridgePos < appPos) fail('bridge deve carregar depois de app.js'); else ok('ordem app.js -> bridge');
['window.ValoraAdminMobileMenuBridge','bind','open','close','toggle','debug'].forEach(x => bridge.includes(x) ? ok(`bridge contém ${x}`) : fail(`bridge sem ${x}`));
if(!/document\.addEventListener\(\s*['"]click['"][\s\S]*?,\s*true\s*\)/.test(bridge)) fail('click não está em capture=true'); else ok('click em capture=true');
['data-action="toggleAdminMobileMenu"','#adminSidebar','.admin-sidebar','.admin-mobile-overlay'].forEach(x => bridge.includes(x) ? ok(`bridge procura/cria ${x}`) : fail(`bridge sem ${x}`));
['.admin-sidebar.open','.admin-mobile-overlay.active','.admin-mobile-toggle'].forEach(x => css.includes(x) ? ok(`CSS contém ${x}`) : fail(`CSS sem ${x}`));
const vm = config.match(/APP_VERSION:\s*['"]([^'"]+)['"]/); if(!vm || !versionAtLeast(vm[1])) fail('APP_VERSION < 8.7.2'); else ok(`APP_VERSION ${vm[1]}`);
['style.css','config.js','app.js','legacy-admin-mobile-menu-bridge.js'].forEach(asset => { const re = new RegExp(asset.replace('.', '\\.') + '\\?v=([0-9.]+)'); const m=index.match(re); if(!m || !versionAtLeast(m[1])) fail(`${asset} sem v>=8.7.2`); else ok(`${asset} v=${m[1]}`); });
if(process.exitCode) process.exit(process.exitCode);
