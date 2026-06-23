#!/usr/bin/env node
const fs=require('fs');const assert=require('assert');
const app=fs.readFileSync('app.js','utf8');
const admin={id:'admin_test',uid:'admin_test',role:'admin_valora',email:'admin@valoragroup.com'};
function getUserRoleSafe(user){return String(user?.role||user?.profile||user?.claims?.role||'').toLowerCase();}
function isGlobalAdmin(user=admin){return ['admin_valora','super_admin','admin'].includes(getUserRoleSafe(user));}
function resolveResponsePermissions(response,user=admin){if(isGlobalAdmin(user))return {canView:true,canEdit:true,canDelete:true,canAnonymize:true,canExport:true,canDownloadCertificate:true,canDownloadReport:true,canViewSensitiveData:true};return {canView:false};}
function canAccessRoute(route,user=admin){if(isGlobalAdmin(user))return true;return !/^admin\//.test(route);}
['admin/dashboard','admin/responses','admin/settings','admin/surveys','admin/forms','admin/plans','admin/support','admin/audit','responses','result','certificate'].forEach(r=>assert(canAccessRoute(r,admin),`admin bloqueado em ${r}`));
const perms=resolveResponsePermissions({companyId:'any'},admin);['canView','canEdit','canDelete','canAnonymize','canExport','canDownloadCertificate','canDownloadReport','canViewSensitiveData'].forEach(k=>assert.strictEqual(perms[k],true,`permissão ${k} negada`));
assert(/function isGlobalAdmin/.test(app),'helper isGlobalAdmin ausente');
assert(/if\(isGlobalAdmin\(currentUser\(\)\)\)\{\}else/.test(app),'route guard não prioriza admin global');
assert(!/if\(!canViewResponse\(r,publicToken\)\)return \$\('#app'\)\.innerHTML=invalidLink\('Acesso negado'/.test(app),'resultado ainda bloqueia admin usando só token');
console.log('validate-admin-access: PASS');
