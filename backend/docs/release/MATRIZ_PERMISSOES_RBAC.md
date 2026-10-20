# MATRIZ DE PERMISSÕES E PERFIS (RBAC)
## Valora Insight™ — Release 1.0 Candidate

---

## 1. Perfis Oficiais do Sistema

| Código do Papel | Nome Amigável | Escopo de Atuação |
| :--- | :--- | :--- |
| `admin_valora` | Administrador Valora | Global / Plataforma SaaS (Superadministrador MNSOFT) |
| `empresa_admin` | Administrador da Empresa | Organização / Tenant completo |
| `gestor_pesquisa` | Gestor de Pesquisa | Ciclos de Diagnóstico, Formulários e Campanhas da Empresa |
| `analista_resultados` | Analista de Resultados | Acesso a Dashboards, Heatmaps, Exportações e Relatórios |
| `gestor_unidade` | Gestor de Unidade | Resultados e Ações restritos à sua Unidade Organizacional |
| `participante` | Participante | Responder questionários atribuídos e visualizar seus próprios convites |

---

## 2. Matriz de Autorização por Funcionalidade

| Funcionalidade / Módulo | `admin_valora` | `empresa_admin` | `gestor_pesquisa` | `analista_resultados` | `gestor_unidade` | `participante` |
| :--- | :---: | :---: | :---: | :---: | :---: | :---: |
| **Gerenciar Tenants e Planos** | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| **Acessar Métricas Globais da Plataforma** | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| **Configurar Unidades e Departamentos** | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| **Convidar / Desativar Usuários** | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| **Criar / Editar Diagnósticos e Formulários** | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ |
| **Publicar e Encerrar Coletas de Pesquisa** | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ |
| **Visualizar Dashboard Geral da Empresa** | ✅ | ✅ | ✅ | ✅ | ⚠️ *(Apenas sua Unidade)* | ❌ |
| **Acessar Heatmaps e Correlações** | ✅ | ✅ | ✅ | ✅ | ⚠️ *(Apenas sua Unidade)* | ❌ |
| **Criar e Atribuir Planos de Ação** | ✅ | ✅ | ✅ | ✅ | ✅ *(Dentro de sua Unidade)* | ❌ |
| **Atualizar Status de Item de Ação** | ✅ | ✅ | ✅ | ✅ | ✅ *(Itens de sua Unidade)* | ❌ |
| **Exportar Relatórios PDF e Planilhas Excel** | ✅ | ✅ | ✅ | ✅ | ⚠️ *(Dados de sua Unidade)* | ❌ |
| **Responder Questionário de Diagnóstico** | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ |
| **Acessar Histórico de Auditoria (Logs)** | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |

**Legenda:**
- ✅ **Permitido:** Acesso total à funcionalidade.
- ❌ **Bloqueado:** Requisição rejeitada com HTTP 403 Forbidden.
- ⚠️ **Restrito:** Permitido com filtragem mandatória no banco de dados vinculada à Unidade/Escopo do usuário.

---

## 3. Regras de Isolamento e Segurança (Negative Security Gates)
1. **Anonimato Estatístico:** Nenhuma visualização de resultados exibirá segmentações com menos de 5 participantes respondentes para evitar quebra de sigilo individual.
2. **Isolamento de Tenant Inviolável:** Cláusula `WHERE organization_id = @orgId` é mandatória em 100% das consultas SQL do backend, validada por testes negativos de chave estrangeira e queries cruzadas.
3. **Imutabilidade de Coletas Encerradas:** Uma vez fechada a coleta (`status = 'closed'`), nenhuma nova submissão é aceita, garantindo a integridade dos cálculos do motor de maturidade.
