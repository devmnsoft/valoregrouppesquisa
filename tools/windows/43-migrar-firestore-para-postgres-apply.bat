@echo off
cd /d C:\DBBACK\valoregrouppesquisa
node migration\import-postgres.js --apply --batch-size 100
