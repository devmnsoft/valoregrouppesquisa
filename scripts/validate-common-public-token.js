#!/usr/bin/env node
const fs=require('fs');
function read(p){return fs.readFileSync(p,'utf8');}
function assert(ok,msg){if(!ok){console.error(`FAIL: ${msg}`);process.exit(1);}}
module.exports={read,assert};
