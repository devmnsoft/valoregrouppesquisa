# Provider Híbrido

`DATA_PROVIDER=hybrid` habilita rota controlada. `HYBRID_PRIMARY_PROVIDER` define `firebase` ou `api`. Escritas seguem somente o primário; comparações usam `hybridCompare(label, primaryData, secondaryData)` e registram divergências em `state.migrationDiagnostics`.
