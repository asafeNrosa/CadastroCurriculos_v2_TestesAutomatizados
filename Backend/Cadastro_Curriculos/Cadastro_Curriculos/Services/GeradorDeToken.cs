using System.Security.Claims;
using System.Text;
using CadastroCurriculos.Api.Models;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Cadastro_Curriculos.Services;

public class GeradorDeToken(JwtOptions opcoes)
{
    public (string Token, DateTime ExpiraEm) Gerar(Usuario usuario, DateTime? agora = null)
    {
        var expiraEm = (agora ?? DateTime.UtcNow).AddMinutes(opcoes.ExpiraEmMinutos);

        var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(opcoes.Chave));

        var descritor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, usuario.Email)
            }),
            Issuer = opcoes.Emissor,
            Audience = opcoes.Audiencia,
            Expires = expiraEm,
            SigningCredentials = new SigningCredentials(chave, SecurityAlgorithms.HmacSha256)
        };

        var token = new JsonWebTokenHandler().CreateToken(descritor);
        return (token, expiraEm);
    }
}
