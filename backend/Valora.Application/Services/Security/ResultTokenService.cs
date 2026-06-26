using System.Security.Cryptography;
using System.Text;
using Valora.Application.Contracts;
namespace Valora.Application.Services;
public sealed class ResultTokenService:IResultTokenService{ public string CreateToken()=>"rt_"+Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).Replace('+','-').Replace('/','_').TrimEnd('='); public string HashToken(string token){var bytes=SHA256.HashData(Encoding.UTF8.GetBytes(token)); return Convert.ToHexString(bytes).ToLowerInvariant();} public bool Verify(string token,string hash)=>CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(HashToken(token)),Encoding.UTF8.GetBytes(hash));}
