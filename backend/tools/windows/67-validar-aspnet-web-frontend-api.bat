@echo off
cd /d C:\DBBACK\valoregrouppesquisa
call npm run web:build || goto erro
call npm run web:validate || goto erro
call npm run web:jquery || goto erro
call npm run web:api-contract || goto erro
call npm run web:screens || goto erro
call npm run web:errors || goto erro
call npm run web:mobile || goto erro
call npm run web:cors || goto erro
call npm run web:docker || goto erro
call npm run web:no-binaries || goto erro
echo VALORA.WEB ASP.NET FRONTEND API-FIRST VALIDADO COM SUCESSO.
pause
exit /b 0
:erro
echo FALHA NA VALIDACAO DO VALORA.WEB ASP.NET.
pause
exit /b 1
