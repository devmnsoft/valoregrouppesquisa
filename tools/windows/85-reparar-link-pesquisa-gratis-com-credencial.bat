@echo off
cd /d C:\MNSOFT\valoregrouppesquisa
set FIREBASE_PROJECT_ID=gestordepesquisa
if "%GOOGLE_APPLICATION_CREDENTIALS%"=="" (
  echo Variavel GOOGLE_APPLICATION_CREDENTIALS nao definida.
  echo Defina antes de executar:
  echo set GOOGLE_APPLICATION_CREDENTIALS=C:\FirebaseKeys\gestordepesquisa-firebase-adminsdk.json
  pause
  exit /b 1
)
echo [1/5] Validando credenciais Firebase...
call npm run firebase:admin-credentials
if errorlevel 1 goto erro
echo [2/5] Dry-run reparo link pesquisa gratis...
call npm run home:repair-free-survey-link -- --dry-run --backup --project gestordepesquisa
if errorlevel 1 goto erro
echo [3/5] Aplicando reparo link pesquisa gratis...
call npm run home:repair-free-survey-link -- --apply --backup --project gestordepesquisa
if errorlevel 1 goto erro
echo [4/5] Validando link publico real...
call npm run firebase:live-public-link
if errorlevel 1 goto erro
echo [5/5] Validando link publico estrutural...
call npm run home:validate-public-link
if errorlevel 1 goto erro
echo LINK DA PESQUISA GRATIS REPARADO E VALIDADO.
pause
exit /b 0
:erro
echo FALHA AO REPARAR LINK DA PESQUISA GRATIS.
pause
exit /b 1
