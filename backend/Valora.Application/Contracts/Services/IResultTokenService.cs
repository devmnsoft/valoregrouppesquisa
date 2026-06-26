namespace Valora.Application.Contracts;
public interface IResultTokenService { string CreateToken(); string HashToken(string token); bool Verify(string token,string hash); }
