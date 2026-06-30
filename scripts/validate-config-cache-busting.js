#!/usr/bin/env node
const {read,assert,finish,fs}=require('./validate-utils-s57');
const config=read('config.js'), index=read('index.html'), firebase=read('firebase.json'), build=read('scripts/build-production.js');
assert(/APP_VERSION:\s*'8\.7\.2'/.test(config),'APP_VERSION 8.7.2');
['config.js','app.js','style.css'].forEach(a=>assert(index.includes(`${a}?v=8.7.2`),`${a} versionado no index`));
const fb=JSON.parse(firebase); const html=fb.hosting.headers.find(h=>h.source==='**/*.@(html)'); const cfg=fb.hosting.headers.find(h=>h.source==='config.js');
assert(html?.headers?.some(h=>/no-store/.test(h.value)),'HTML no-store');
assert(cfg?.headers?.some(h=>/no-store/.test(h.value)),'config.js no-store');
assert(/copyFileSync\(path\.join\(root, 'config\.js'\)/.test(build),'build copia config.js para dist');
assert(/config\.js\?v=8\.7\.2/.test(build),'dist referencia config.js versionado');
finish();
