#!/usr/bin/env node
const fs = require('fs');
const path = require('path');
const root = path.resolve(__dirname, '..');
const stamp = () => new Date().toISOString().replace(/[-:T]/g, '').slice(0, 12);
function parse(argv){const a={latest:false};for(let i=0;i<argv.length;i++){const x=argv[i];if(!x.startsWith('--'))continue;const k=x.slice(2).replace(/-([a-z])/g,(_,c)=>c.toUpperCase());if(['latest','list'].includes(k))a[k]=true;else a[k]=argv[++i];}return a;}
function loadConfig(){try{return JSON.parse(fs.readFileSync(path.join(root,'publish.config.json'),'utf8'));}catch(_){return {};}}
const cfg=loadConfig(), args=parse(process.argv.slice(2));
const iisPath=args.iisPath||cfg.iisPath;
function cfgPath(v,fallback){return path.resolve(root,String(v||fallback).replace(/\\/g,path.sep));}
const backupRoot=cfgPath(args.backupPath||cfg.backupPath,'backups/iis');
function fail(m){console.error(`ERRO: ${m}`);process.exit(1);} 
function copyDir(src,dst){fs.mkdirSync(dst,{recursive:true});for(const e of fs.readdirSync(src,{withFileTypes:true})){const s=path.join(src,e.name),d=path.join(dst,e.name);if(e.isDirectory())copyDir(s,d);else fs.copyFileSync(s,d);}}
function empty(dir){fs.mkdirSync(dir,{recursive:true});for(const e of fs.readdirSync(dir))fs.rmSync(path.join(dir,e),{recursive:true,force:true});}
function backups(){if(!fs.existsSync(backupRoot))return [];return fs.readdirSync(backupRoot,{withFileTypes:true}).filter(d=>d.isDirectory()).map(d=>path.join(backupRoot,d.name)).sort();}
if(!iisPath)fail('Informe --iis-path ou configure publish.config.json.');
const list=backups();
if(args.list){console.log(list.map(p=>path.relative(root,p)).join('\n')||'Nenhum backup encontrado.');process.exit(0);}
const chosen=args.backup?path.resolve(root,args.backup):(args.latest?list[list.length-1]:null);
if(!chosen)fail('Informe --latest ou --backup <pasta>.');
if(!fs.existsSync(chosen))fail(`Backup não encontrado: ${chosen}`);
const safety=path.join(backupRoot,`pre-restore-${stamp()}`);
if(fs.existsSync(iisPath)&&fs.readdirSync(iisPath).length){copyDir(iisPath,safety);console.log(`Backup da versão atual antes do restore: ${path.relative(root,safety)}`);}else fs.mkdirSync(safety,{recursive:true});
empty(iisPath); copyDir(chosen,iisPath);
for(const f of ['index.html','web.config']) if(!fs.existsSync(path.join(iisPath,f))) fail(`Restore executado, mas ${f} não foi encontrado no IIS.`);
console.log(`Restore concluído a partir de ${path.relative(root,chosen)} para ${iisPath}`);
