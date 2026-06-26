@echo off
cd /d C:\DBBACK\valoregrouppesquisa
node migration\compare-firebase-postgres.js
if errorlevel 1 goto erro
echo COMPARACAO FIREBASE X POSTGRES CONCLUIDA.
pause
exit /b 0
:erro
echo FALHA NA COMPARACAO FIREBASE X POSTGRES.
pause
exit /b 1
