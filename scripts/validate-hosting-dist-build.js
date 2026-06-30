const fs = require('fs');
const path = require('path');
function fail(message) { console.error(message); process.exit(1); }
const required = ['dist', 'dist/index.html', 'dist/config.js', 'dist/assets'];
for (const item of required) if (!fs.existsSync(item)) fail(`${item} ausente. Execute npm run build:prod antes do deploy.`);
const html = fs.readFileSync('dist/index.html', 'utf8');
function assertReferencedAssets(regex, label) {
  const matches = [...html.matchAll(regex)].map(match => match[1]).filter(src => !/^https?:\/\//.test(src));
  if (!matches.length) fail(`dist/index.html não referencia ${label}`);
  for (const src of matches) {
    const clean = src.split('?')[0].replace(/^\/+/, '');
    if (!fs.existsSync(path.join('dist', clean))) fail(`Asset referenciado não encontrado: ${src}`);
  }
}
assertReferencedAssets(/<script\b[^>]*\bsrc=["']([^"']+\.js(?:\?[^"']*)?)["']/gi, 'JS válido');
assertReferencedAssets(/<link\b[^>]*\bhref=["']([^"']+\.css(?:\?[^"']*)?)["']/gi, 'CSS válido');
const bridgeInHtml = /legacy-admin-mobile-menu-bridge\.js/.test(html);
const bridgeFile = fs.existsSync('dist/legacy-admin-mobile-menu-bridge.js') || fs.existsSync('dist/assets/legacy-admin-mobile-menu-bridge.js');
const bundleContainsBridge = fs.readdirSync('dist/assets').filter(name => name.endsWith('.js')).some(name => /legacy-admin-mobile-menu-bridge\.js|adminMobileMenuParts|bindAdminMobileMenuEvents/.test(fs.readFileSync(path.join('dist/assets', name), 'utf8')));
if (!bridgeInHtml && !bridgeFile && !bundleContainsBridge) fail('legacy-admin-mobile-menu-bridge.js não está referenciado nem presente no bundle/dist');
console.log('hosting:dist-build OK');
