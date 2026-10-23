namespace Cadastro_Curriculos.Services;

/// <summary>Configuração do token JWT (seção "Jwt" do appsettings, User Secrets ou variáveis de ambiente).</summary>
public class JwtOptions
{
    public const int TamanhoMinimoDaChave = 32;

    // A chave nunca vai para o repositório: User Secrets em desenvolvimento e
    // a configuração "Jwt__Chave" do App Service em produção.
    public string Chave { get; set; } = string.Empty;

    public string Emissor { get; set; } = "cadastro-curriculos-api";

    public string Audiencia { get; set; } = "cadastro-curriculos-front";

    public int ExpiraEmMinutos { get; set; } = 120;
}
