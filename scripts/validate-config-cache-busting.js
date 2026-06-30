#!/usr/bin/env node
const {read,assert,finish}=require('./validate-utils-s57');
const config=read('config.js'), index=read('index.html'), firebase=read('firebase.json'), build=read('scripts/build-production.js');
const versionMatch=config.match(/APP_VERSION:\s*'([^']+)'/);
assert(!!versionMatch,'APP_VERSION declarado em config.js');
const version=versionMatch&&versionMatch[1];
['config.js','app.js','style.css'].forEach(a=>assert(index.includes(`${a}?v=${version}`),`${a} versionado no index`));
const fb=JSON.parse(firebase); const html=fb.hosting.headers.find(h=>h.source==='**/*.@(html)'); const cfg=fb.hosting.headers.find(h=>h.source==='config.js');
assert(html?.headers?.some(h=>/no-store/.test(h.value)),'HTML no-store');
assert(cfg?.headers?.some(h=>/no-store/.test(h.value)),'config.js no-store');
assert(/copyFileSync\(path\.join\(root, 'config\.js'\)/.test(build),'build copia config.js para dist');
assert(/versionMatch/.test(build)&&/config\.js\?v=\$\{appVersion\}/.test(build),'dist referencia config.js com APP_VERSION dinâmica');
finish();
