# Escopo Funcional — Valora Group™ 8.3

## Incluído nesta entrega

- site público responsivo e clean;
- identidade Valora Group;
- pesquisa de destaque configurável;
- planos editáveis;
- FAQ, contato, LGPD e WhatsApp editáveis;
- ValoraBot contextual;
- login local por perfil;
- portais separados por responsabilidade;
- clientes PF/PJ e consulta de CNPJ/CEP;
- CRUD de usuários e perfis;
- formulários e provas dinâmicas;
- seis tipos de pergunta;
- pesos, notas, alternativas, resposta correta e faixas;
- revisão estrutural e revisão ortográfica do navegador;
- criação instantânea de pesquisa;
- token, prazo e compartilhamento;
- identificação e consentimento do participante;
- resultado, relatório e certificado;
- dashboards e KPIs;
- financeiro e planos;
- módulos parametrizáveis;
- relatórios PDF, Word, Excel, JSON e CSV;
- e-mail de teste e SMTP pelo servidor local;
- logs e backup exclusivos do administrador geral;
- importação/exportação JSON;
- arquivos de preparação para Firebase.

## Requisitos antes de produção comercial

- Firebase Authentication ou provedor equivalente;
- armazenamento Firestore por tenant;
- validação de tokens e limites em Cloud Functions;
- senha com hash e política de sessão;
- MFA para administradores;
- rate limit e proteção contra abuso;
- serviço transacional de e-mail e WhatsApp Business;
- consentimento/versionamento jurídico validado;
- retenção, anonimização e descarte automatizados;
- trilha de auditoria imutável;
- pagamentos e conciliação;
- observabilidade, backup e recuperação;
- testes automatizados em pipeline.

## Fora do escopo do MVP local

- envio real sem configuração SMTP;
- cobrança automática;
- assinatura eletrônica qualificada;
- certificação formal de modelos de gestão;
- benchmark de mercado sem base de referência contratada;
- garantia jurídica do texto LGPD sem revisão profissional;
- multiusuário simultâneo usando apenas `localStorage`.
