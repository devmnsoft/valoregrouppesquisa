#!/usr/bin/env node
const fs=require('fs');const repo=fs.readFileSync('firebase-repository.js','utf8');
if(/collection\('users'\)\.doc\(uid\)\.set\([\s\S]{0,600}password/.test(repo))throw new Error('Possível senha em texto puro em users/{uid}.');
console.log('audit-plain-passwords: PASS');
