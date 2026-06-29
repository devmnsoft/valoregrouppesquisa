using Valora.Application.Communication;
using Xunit;
public class EmailConfigurationTests { [Fact] public void Invalid_config_blocks_send(){ var s=EmailConfigurationValidator.Validate(new EmailOptions{Enabled=true}); Assert.False(s.CanSend); } [Fact] public void Valid_config_allows_send(){ var o=new EmailOptions{Enabled=true,FromEmail="valoragroup@mnsoft.com.br",FromName="Valora",ReplyTo="valoragroup@mnsoft.com.br"}; o.Smtp.Host="smtp.example.com"; o.Smtp.Username="user"; o.Smtp.Password="secret"; var s=EmailConfigurationValidator.Validate(o); Assert.True(s.CanSend); } }
