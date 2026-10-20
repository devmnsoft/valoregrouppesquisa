# CHECKLIST DE LIMITAÇÕES CONHECIDAS E ROADMAP
## Valora Insight™ — Release 1.0 Candidate

---

## 1. Escopo Deliberado da Release 1.0

A Release 1.0 do Valora Insight™ prioriza **estabilidade matemática, segurança de dados, isolamento multi-tenant absoluto e prontidão comercial**. Funcionalidades secundárias foram conscientemente adiadas para evitar débito técnico ou complexidade não homologada no primeiro piloto.

---

## 2. Limitações Conhecidas na 1.0 e Justificativas Técnicas

### 2.1 Integrações com Provedores Externos de SSO (SAML 2.0 / Azure AD / Okta)
- **Status na 1.0:** Não incluído nativamente; autenticação suportada via JWT próprio com PBKDF2/Argon2 e tokens assinados em ASP.NET Core 10.
- **Justificativa Técnica:** As empresas piloto iniciais operam com convites por e-mail e autenticação corporativa direta. O protocolo de federação OIDC/SAML será introduzido na versão 1.1 para manter o vetor de ataque da 1.0 reduzido e testado.
- **Previsão:** Release 1.1 (Q1 2027).

### 2.2 Notificações Push Mobile Nativas
- **Status na 1.0:** Disparo de convites e lembretes via e-mail e links diretos responsivos (PWA / Web Mobile).
- **Justificativa Técnica:** A interface mobile é totalmente compatível com navegadores modernos de smartphones (iOS Safari e Android Chrome), dispensando a necessidade de app nativo em lojas no lançamento piloto.
- **Previsão:** Release 1.2.

### 2.3 Importação Automatizada de Folha de Pagamento via APIs de RH (Senior / Totvs / ADP)
- **Status na 1.0:** Importação via upload de planilha padronizada CSV/Excel ou API REST documentada.
- **Justificativa Técnica:** A variabilidade de conectores de ERPs e sistemas legados de RH exigiria módulos customizados por cliente. O formato universal CSV atende 100% dos cenários de onboarding da 1.0.
- **Previsão:** Conectores ERP dedicados na Release 1.3 (Plano Enterprise).

### 2.4 Treinamento de Modelos Preditivos de IA Locais
- **Status na 1.0:** O motor de maturidade utiliza inferência estatística ponderada canônica e geração de planos guiada por heurísticas comprovadas, sem dependência opaca de LLMs de terceiros que possam vazar dados confidenciais de colaboradores.
- **Justificativa Técnica:** Conformidade estrita com a LGPD e soberania dos dados da empresa cliente. Zero risco de vazamento de informações corporativas para modelos de inteligência artificial de nuvens públicas.
- **Previsão:** Módulo de IA On-Premises / Private Cloud isolado na Release 1.2.

---

## 3. Garantias Mantidas na 1.0
- Integridade relacional em PostgreSQL 16.
- Zero mock mascarado como resultado real.
- Anonimato garantido de respondentes com piso de 5 respostas por agrupamento.
- Backup com RPO de 1h e RTO de 30 minutos.
