using System.Text;
using Cadastro_Curriculos.Infrastructure;
using Cadastro_Curriculos.Services;
using CadastroCurriculos.Api.Data;
using CadastroCurriculos.Api.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Nos testes de integração (ambiente "Testing") o banco é trocado por SQLite em memória,
// então a connection string do SQL Server só é exigida fora dele.
if (!builder.Environment.IsEnvironment("Testing"))
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException(
            "Connection string 'DefaultConnection' não encontrada. Configure-a via User Secrets ou appsettings.");

    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(connectionString));
}

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddSingleton<CurriculoExtrator>();
builder.Services.AddSingleton<LeitorPdf>();

// --- Autenticação (JWT) ---
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();

if (jwt.Chave.Length < JwtOptions.TamanhoMinimoDaChave)
    throw new InvalidOperationException(
        $"Configuração 'Jwt:Chave' ausente ou curta (mínimo {JwtOptions.TamanhoMinimoDaChave} caracteres). " +
        "Configure-a via User Secrets (desenvolvimento) ou na variável de ambiente Jwt__Chave (produção).");

builder.Services.AddSingleton(jwt);
builder.Services.AddSingleton<GeradorDeToken>();
builder.Services.AddSingleton<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Mantém os nomes originais dos claims ("sub" continua "sub"), que é o que o controller lê.
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Emissor,
            ValidateAudience = true,
            ValidAudience = jwt.Audiencia,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Chave)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization();

const string PoliticaFrontend = "Frontend";

builder.Services.AddCors(options =>
    options.AddPolicy(PoliticaFrontend, policy =>
        policy.WithOrigins(builder.Configuration.GetSection("Cors:Origens").Get<string[]>()
                           ?? ["http://localhost:4200"])
              .AllowAnyHeader()
              .AllowAnyMethod()));

var app = builder.Build();

app.UseExceptionHandler();
// Respostas sem corpo (401, 403, 404 de rota) também saem como ProblemDetails.
app.UseStatusCodePages();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors(PoliticaFrontend);

if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

// Autenticação antes da autorização: primeiro descobre quem é, depois decide o que pode.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Torna a classe Program visível para o WebApplicationFactory dos testes de integração.
public partial class Program;
