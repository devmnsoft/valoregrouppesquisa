const {has}=require('./validate-demo-production-common');
has('functions/index.js',/exports\.purgeProductionDemoData/,'purgeProductionDemoData ausente');
has('functions/index.js',/dryRun[\s\S]*archivedSurveys[\s\S]*demoUrlsFound/,'relatório/dry-run do purge incompleto');
has('scripts/purge-production-demo-data.js',/--dry-run[\s\S]*--apply/,'script purge sem modos dry-run/apply');
