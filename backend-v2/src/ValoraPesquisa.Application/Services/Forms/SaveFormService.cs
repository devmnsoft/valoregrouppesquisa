using ValoraPesquisa.Application.Contracts; using ValoraPesquisa.Application.DTOs; using ValoraPesquisa.Domain.Entities;
namespace ValoraPesquisa.Application.Services.Forms;
public sealed class SaveFormService(IFormRepository repo){ public Task<FormDto> ExecuteAsync(Form form,CurrentUser user){ if(form.Questions.Count==0) throw new ArgumentException("Informe ao menos uma pergunta."); return repo.SaveAsync(form,user); } }
