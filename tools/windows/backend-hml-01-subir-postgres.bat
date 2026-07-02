@echo off
if not exist .env copy .env.example .env
docker compose up -d postgres-hml
docker compose ps postgres-hml
