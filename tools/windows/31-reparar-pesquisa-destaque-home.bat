@echo off
cd /d C:\DBBACK\valoregrouppesquisa
set GOOGLE_APPLICATION_CREDENTIALS=C:\FirebaseKeys\gestordepesquisa-firebase-adminsdk-fbsvc-fe4f2380fd.json
echo [1/3] Dry-run reparo pesquisa destaque...
node scripts\repair-featured-home-survey.js --project gestordepesquisa --backup --dry-run
if errorlevel 1 goto erro
echo [2/3] Aplicando reparo...
node scripts\repair-featured-home-survey.js --project gestordepesquisa --backup --apply
if errorlevel 1 goto erro
echo [3/3] Validando destaque...
node scripts\validate-featured-home-survey.js --project gestordepesquisa
if errorlevel 1 goto erro
echo PESQUISA DESTAQUE REPARADA.
pause
exit /b 0
:erro
echo FALHA AO REPARAR PESQUISA DESTAQUE.
pause
exit /b 1
