using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Valora.Application.Contracts;

namespace Valora.Infrastructure.Security;
public sealed class BCryptPasswordHasher:IPasswordHasher{ public string Hash(string password)=>BCrypt.Net.BCrypt.HashPassword(password); public bool Verify(string password,string hash)=>BCrypt.Net.BCrypt.Verify(password,hash); }
