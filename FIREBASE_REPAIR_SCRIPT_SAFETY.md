# Segurança de Scripts de Reparo Firebase

Scripts que alteram Firestore devem rodar em dry-run por padrão. Para aplicar, exigem `--apply`, `--backup`, projeto explícito, credencial válida e `ALLOW_FIREBASE_REPAIR_APPLY=true`.
