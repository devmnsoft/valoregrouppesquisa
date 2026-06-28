@echo off
cd /d C:\DBBACK\valoregrouppesquisa
node scripts\restore-local-postgres.js %1
