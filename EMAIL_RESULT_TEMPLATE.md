# Template de e-mail de resultado

O template premium Valora Insight™ fica em `communication-gateway/src/templates/result-email-template.js`.

Conteúdo obrigatório:
- nome do participante;
- título da pesquisa;
- pontuação e pontuação máxima;
- nível de maturidade;
- dimensão mais forte;
- dimensão prioritária;
- botão “Ver meu resultado”;
- link do certificado quando disponível;
- rodapé Valora Group em nome da organização.

Todos os dados interpolados no HTML passam por `escapeHtml`.
