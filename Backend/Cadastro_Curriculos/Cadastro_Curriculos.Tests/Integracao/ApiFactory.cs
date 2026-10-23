using CadastroCurriculos.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cadastro_Curriculos.Tests.Integracao;

/// <summary>
/// Sobe a API de verdade em memória (WebApplicationFactory), trocando apenas o banco:
/// SQL Server vira SQLite em memória, criado a partir do modelo do EF.
/// A conexão fica aberta durante a vida da factory, senão o banco em memória some.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _conexao = new("DataSource=:memory:");

    static ApiFactory()
    {
        // O Program.cs lê a configuração logo no início (antes do ConfigureWebHost),
        // por isso o ambiente e a chave do JWT entram por variáveis de ambiente.
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("Jwt__Chave", "chave-de-testes-de-integracao-com-mais-de-32-caracteres");
    }

    public ApiFactory()
    {
        _conexao.Open();

        // Garante a integridade referencial (cascade, SetNull) mesmo em conexão já aberta.
        using var comando = _conexao.CreateCommand();
        comando.CommandText = "PRAGMA foreign_keys = ON;";
        comando.ExecuteNonQuery();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
            services.AddDbContext<AppDbContext>(opcoes => opcoes.UseSqlite(_conexao)));
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var escopo = host.Services.CreateScope();
        escopo.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();

        return host;
    }

    /// <summary>Cliente HTTP da API, opcionalmente já autenticado com o token informado.</summary>
    public HttpClient CriarClienteApi(string? token = null)
    {
        var cliente = CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });

        if (token is not null)
            cliente.DefaultRequestHeaders.Authorization = new("Bearer", token);

        return cliente;
    }

    /// <summary>Executa uma consulta direto no banco, para conferir o que a API gravou.</summary>
    public async Task<T> ConsultarBancoAsync<T>(Func<AppDbContext, Task<T>> consulta)
    {
        using var escopo = Services.CreateScope();
        return await consulta(escopo.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _conexao.Dispose();
    }
}
