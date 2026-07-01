#!/usr/bin/env node
const fs = require('fs');
const path = require('path');
const crypto = require('crypto');

const root = path.resolve(__dirname, '..');
const dist = path.join(root, 'dist');
const distAssets = path.join(dist, 'assets');
const indexPath = path.join(root, 'index.html');
const cssPath = path.join(root, 'style.css');
const assetFiles = ['logo-full.jpeg', 'logo-symbol.jpeg'];

function read(file) { return fs.readFileSync(path.join(root, file), 'utf8'); }
function hash(content) { return crypto.createHash('sha256').update(content).digest('hex').slice(0, 12); }
function cleanJs(source) {
  return source
    .replace(/\/\/#[#@] sourceMappingURL=.*$/gm, '')
    .replace(/\/\*[\s\S]*?sourceMappingURL=[\s\S]*?\*\//g, '')
    .trim();
}

function assertNoLegacyDemoProductionArtifact(label, content) {
  const forbidden = /survey_demo|empresa-exemplo|demo-token|tokenHash=/i;
  if (forbidden.test(content)) throw new Error(`${label} contém demo/legado proibido em produção.`);
}
function minifyCss(source) {
  return source.replace(/\/\*[\s\S]*?\*\//g, '').replace(/\s+/g, ' ').replace(/\s*([{}:;,>+~])\s*/g, '$1').trim();
}

fs.rmSync(dist, { recursive: true, force: true });
fs.mkdirSync(distAssets, { recursive: true });
fs.mkdirSync(path.join(dist, 'vendor', 'bootstrap'), { recursive: true });

const index = fs.readFileSync(indexPath, 'utf8');
const productionConfigPath = path.join(root, 'config', 'config.production.js');
const configFileForDist = fs.existsSync(productionConfigPath) ? 'config/config.production.js' : 'config.js';
const configSource = read(configFileForDist);
const versionMatch = configSource.match(/APP_VERSION:\s*'([^']+)'/);
const appVersion = versionMatch ? versionMatch[1] : '0.0.0';
// Bundles legacy-admin-mobile-menu-bridge.js when referenced by index.html.
const localScripts = [...index.matchAll(/<script\s+src="([^"]+\.js)(?:\?v=[^"]*)?"\s+defer><\/script>/g)]
  .map((match) => match[1])
  .filter((src) => !src.startsWith('http') && src !== 'config.js');

let jsBundle = localScripts.map((script) => `;\n/* ${script} */\n${cleanJs(read(script))}`).join('\n');
const preliminaryHash = hash(jsBundle);
const buildInfo = { version: appVersion, hash: preliminaryHash, builtAt: new Date().toISOString() };
jsBundle = `;window.ValoraBuildInfo=${JSON.stringify(buildInfo)};\nif('serviceWorker' in navigator){navigator.serviceWorker.getRegistrations().then(rs=>rs.forEach(r=>r.update())).catch(()=>{});}\nif(window.caches){caches.keys().then(keys=>Promise.all(keys.filter(k=>/valora|firebase|app/i.test(k)).map(k=>caches.delete(k)))).catch(()=>{});}\n` + jsBundle;
const finalHash = hash(jsBundle);
jsBundle = jsBundle.replace(`\"hash\":\"${preliminaryHash}\"`, `\"hash\":\"${finalHash}\"`);
const jsFile = `app.${finalHash}.js`;
fs.writeFileSync(path.join(distAssets, jsFile), jsBundle, 'utf8');
fs.copyFileSync(path.join(root, configFileForDist), path.join(dist, 'config.js'));
fs.copyFileSync(indexPath, path.join(dist, 'index.source.html')); // evidência do index.html de origem usado no build

const cssBundle = minifyCss(fs.readFileSync(cssPath, 'utf8'));
const cssFile = `style.${hash(cssBundle)}.css`;
fs.writeFileSync(path.join(distAssets, cssFile), cssBundle, 'utf8');

for (const file of ['bootstrap.min.css', 'bootstrap.bundle.min.js']) {
  const src = path.join(root, 'vendor', 'bootstrap', file);
  if (!fs.existsSync(src)) throw new Error(`Bootstrap local não encontrado: vendor/bootstrap/${file}`);
  fs.copyFileSync(src, path.join(dist, 'vendor', 'bootstrap', file));
}

for (const file of assetFiles) {
  const src = path.join(root, 'assets', file);
  if (!fs.existsSync(src)) throw new Error(`Asset original não encontrado: assets/${file}`);
  fs.copyFileSync(src, path.join(distAssets, file));
}

let html = index
  .replace(/<link rel="stylesheet" href="style\.css(?:\?v=[^"]*)?">/, `<link rel="stylesheet" href="assets/${cssFile}">`)
  .replace(/<script\s+src="(?!https?:)[^"]+\.js(?:\?v=[^"]*)?"\s+defer><\/script>\n?/g, '')
  .replace('</body>', `  <script src="config.js?v=${appVersion}" defer></script>\n  <script src="assets/${jsFile}" defer></script>\n</body>`);
assertNoLegacyDemoProductionArtifact('dist/index.html', html);
fs.writeFileSync(path.join(dist, 'index.html'), html, 'utf8');
const webConfig=path.join(root,'templates','iis','web.config');
if(fs.existsSync(webConfig))fs.copyFileSync(webConfig,path.join(dist,'web.config'));
else console.warn('templates/iis/web.config não encontrado; dist/web.config não foi gerado.');

require('./postbuild-security-check');
fs.writeFileSync(path.join(dist,'build-info.json'), JSON.stringify({version:appVersion,hash:finalHash,builtAt:buildInfo.builtAt,jsFile,cssFile}, null, 2));
console.log(`Build de produção gerado em dist/ com ${jsFile} e ${cssFile}.`);
