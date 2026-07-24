# Route Authorization Tests
Quando o projeto HabitFlow MVC estiver no repo, criar testes com `WebApplicationFactory`: públicas retornam 200 anônimo; protegidas redirecionam para login; Admin exige role Admin; SuperAdmin exige role SuperAdmin. Os testes devem executar pipeline MVC, não apenas procurar strings.
