using System.Net.Http.Json;
namespace ValoraPesquisa.Tests;
public class PostgresIntegrationTests{
 [Fact(Skip="Requer PostgreSQL local/container e API .NET em execução. Execute via backend-v2/docker-compose.yml ou scripts em backend-v2/tools.")]
 public async Task Minimum_Vertical_Flow_Against_PostgreSql(){
  using var http=new HttpClient{BaseAddress=new Uri(Environment.GetEnvironmentVariable("VALORA_API_URL")??"http://localhost:5000")};
  var org=await http.PostAsJsonAsync("/organizations",new{name="Org integração",publicName="Org integração",slug="org-integracao",email="integracao@example.com",planCode="free"});
  Assert.True(org.IsSuccessStatusCode);
  // Intended sequence: create user, login, save form, save survey, publish, create link, validate public survey, submit response, get result and list audit.
 }
}
