using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Valora.Application.Contracts;

namespace Valora.Infrastructure.Security;
public sealed class JwtTokenService(IConfiguration cfg):IJwtTokenService{ public string CreateToken(Guid userId,Guid? organizationId,string email,string role){ var jwt=cfg.GetSection("Jwt"); var claims=new List<Claim>{new(JwtRegisteredClaimNames.Sub,userId.ToString()),new(ClaimTypes.NameIdentifier,userId.ToString()),new(ClaimTypes.Email,email),new(ClaimTypes.Role,role)}; if(organizationId.HasValue) claims.Add(new("organization_id",organizationId.Value.ToString())); var key=new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Secret"]!)); var token=new JwtSecurityToken(jwt["Issuer"],jwt["Audience"],claims,expires:DateTime.UtcNow.AddMinutes(int.Parse(jwt["ExpiresMinutes"]??"480")),signingCredentials:new SigningCredentials(key,SecurityAlgorithms.HmacSha256)); return new JwtSecurityTokenHandler().WriteToken(token); }}
