@echo off
cd /d %~dp0\..\..
psql "%POSTGRES_CONNECTION%" -f database\postgresql\scriptbd_completo.sql
