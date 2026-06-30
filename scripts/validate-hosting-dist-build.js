#!/usr/bin/env node
'use strict';

const fs = require('fs');
const path = require('path');

const root = path.resolve(__dirname, '..');
const dist = path.join(root, 'dist');

function walk(dir) {
  if (!fs.existsSync(dir)) return [];
  const out = [];
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) out.push(...walk(full));
    else if (entry.isFile()) out.push(full);
  }
  return out;
}

function rel(file) {
  return path.relative(dist, file).replace(/\\/g, '/');
}

if (!fs.existsSync(dist)) {
  throw new Error('dist não existe. Rode npm run build:prod antes do deploy.');
}

const files = walk(dist).map(rel);
const indexPath = path.join(dist, 'index.html');
const configPath = path.join(dist, 'config.js');
const assetsPath = path.join(dist, 'assets');

if (!fs.existsSync(indexPath)) throw new Error(`dist sem index.html. Arquivos encontrados:\n${files.join('\n')}`);
if (!fs.existsSync(configPath)) throw new Error(`dist sem config.js. Arquivos encontrados:\n${files.join('\n')}`);
if (!fs.existsSync(assetsPath) || !fs.statSync(assetsPath).isDirectory()) throw new Error(`dist sem pasta assets. Arquivos encontrados:\n${files.join('\n')}`);

const jsFiles = files.filter(f => /(^|\/)(app|main|bundle)\.[a-f0-9]{6,}\.js$/.test(f) || /(^|\/)app\.js$/.test(f));
const cssFiles = files.filter(f => /(^|\/)(style|app|main|bundle)\.[a-f0-9]{6,}\.css$/.test(f) || /(^|\/)style\.css$/.test(f));

if (!jsFiles.length) throw new Error(`dist sem app javascript versionado. Arquivos encontrados:\n${files.join('\n')}`);
if (!cssFiles.length) throw new Error(`dist sem css versionado. Arquivos encontrados:\n${files.join('\n')}`);

const html = fs.readFileSync(indexPath, 'utf8');
const jsReferenced = jsFiles.some(f => html.includes(f));
const cssReferenced = cssFiles.some(f => html.includes(f));

if (!jsReferenced) throw new Error(`dist/index.html não referencia o JS versionado. JS encontrados: ${jsFiles.join(', ')}. Arquivos encontrados:\n${files.join('\n')}`);
if (!cssReferenced) throw new Error(`dist/index.html não referencia o CSS versionado. CSS encontrados: ${cssFiles.join(', ')}. Arquivos encontrados:\n${files.join('\n')}`);
if (/survey_demo|empresa-exemplo|tokenHash=/.test(html)) throw new Error('dist contém link demo proibido');

console.log(`validate-hosting-dist-build: PASS. JS=${jsFiles.join(', ')} CSS=${cssFiles.join(', ')}`);
