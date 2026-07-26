DROP SCHEMA IF EXISTS valorapesquisa CASCADE;
CREATE SCHEMA valorapesquisa;
CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE TABLE valorapesquisa.plans (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), code text UNIQUE, name text NOT NULL, monthly_price numeric(12,2) NOT NULL DEFAULT 0, annual_price numeric(12,2) NOT NULL DEFAULT 0);
CREATE TABLE valorapesquisa.plan_limits (plan_id uuid PRIMARY KEY, active_surveys int, responses_per_month int);
