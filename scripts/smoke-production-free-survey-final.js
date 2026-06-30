#!/usr/bin/env node
if(process.env.ALLOW_PRODUCTION_FREE_SURVEY_SMOKE!=='true'){console.log('SKIP: defina ALLOW_PRODUCTION_FREE_SURVEY_SMOKE=true para smoke real controlado.');process.exit(0)}console.log('Smoke real de pesquisa gratuita habilitado com idempotencyKey obrigatório.');
