-- Minimal production seed: structural communication rules only; create SuperAdmin with scripts/admin/create-superadmin.ps1.
\set ON_ERROR_STOP on
\i database/migrations/029_operational_completeness_v61.sql
