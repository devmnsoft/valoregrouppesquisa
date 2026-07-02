#!/usr/bin/env node
'use strict';
const args=new Set(process.argv.slice(2));
const apply=args.has('--apply');
const dry=args.has('--dry-run')||!apply;
if(apply&&!args.has('--confirm')){console.error('Use --apply --confirm gestordepesquisa para aplicar.');process.exit(1)}
if(apply&&!process.argv.includes('gestordepesquisa')){console.error('Confirmação inválida. Use --confirm gestordepesquisa.');process.exit(1)}
console.log(`dedupeNotifications ${dry?'dry-run':'apply'}: execute a callable dedupeNotifications como admin_valora no projeto gestordepesquisa.`);
console.log('Sem credencial local/Admin SDK, abra o painel admin e use o botão “Limpar notificações duplicadas” ou chame firebase.functions().httpsCallable("dedupeNotifications")({apply:false}).');
