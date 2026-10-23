using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Cadastro_Curriculos.Dtos;

namespace Cadastro_Curriculos.Services;

public partial class CurriculoExtrator
{
    private const int LinhasAnalisadasParaNome = 10;
    private const int LimiteAreaInteresse = 100;
    private const int LimiteResumo = 2000;

    private static readonly HashSet<string> TitulosIgnorados =
        ["curriculo", "curriculum", "curriculum vitae", "resume", "cv"];

    private static readonly HashSet<string> TitulosArea =
        ["objetivo", "objetivo profissional", "cargo pretendido", "cargo de interesse",
         "area de interesse", "area de atuacao", "objective"];

    private static readonly HashSet<string> TitulosResumo =
        ["resumo", "resumo profissional", "perfil", "perfil profissional", "sobre mim",
         "apresentacao", "summary", "professional summary", "about me"];

    private static readonly HashSet<string> OutrosTitulos =
        ["experiencia", "experiencias", "experiencia profissional", "experiencias profissionais",
         "historico profissional", "formacao", "formacao academica", "educacao", "escolaridade",
         "habilidades", "competencias", "conhecimentos", "conhecimentos tecnicos", "cursos",
         "certificacoes", "certificados", "idiomas", "projetos", "contato", "dados pessoais",
         "informacoes pessoais", "referencias", "atividades complementares",
         "experience", "work experience", "education", "skills", "languages", "projects",
         "certifications", "contact"];

    private static readonly HashSet<string> TodosOsTitulos =
        [.. TitulosArea, .. TitulosResumo, .. OutrosTitulos];

    public DadosExtraidosResponse Extrair(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return new DadosExtraidosResponse(null, null, null, null, null);

        var linhas = DividirLinhas(texto);

        return new DadosExtraidosResponse(
            ExtrairNome(linhas),
            ExtrairEmail(texto),
            ExtrairTelefone(texto),
            ExtrairAreaInteresse(linhas),
            ExtrairResumo(linhas));
    }

    private static List<string> DividirLinhas(string texto) =>
        texto.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    private static string? ExtrairEmail(string texto)
    {
        var match = EmailRegex().Match(texto);
        return match.Success ? match.Value.ToLowerInvariant() : null;
    }

    private static string? ExtrairTelefone(string texto)
    {
        var match = TelefoneRegex().Match(texto);
        return match.Success ? match.Value.Trim() : null;
    }

    private static string? ExtrairNome(List<string> linhas)
    {
        foreach (var linha in linhas.Take(LinhasAnalisadasParaNome))
        {
            if (linha.Length > 150) continue;
            if (TitulosIgnorados.Contains(Normalizar(linha))) continue;
            if (EhTitulo(linha, TodosOsTitulos, out _)) continue;
            if (!NomeRegex().IsMatch(linha)) continue;
            if (linha.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length < 2) continue;

            return linha;
        }

        return null;
    }

    private static string? ExtrairAreaInteresse(List<string> linhas)
    {
        for (var i = 0; i < linhas.Count; i++)
        {
            if (!EhTitulo(linhas[i], TitulosArea, out var conteudoNaLinha)) continue;

            var valor = conteudoNaLinha ?? LinhaSeguinte(linhas, i);
            return valor is null ? null : Limitar(valor, LimiteAreaInteresse);
        }

        return null;
    }

    private static string? ExtrairResumo(List<string> linhas)
    {
        for (var i = 0; i < linhas.Count; i++)
        {
            if (!EhTitulo(linhas[i], TitulosResumo, out var conteudoNaLinha)) continue;

            var partes = new List<string>();
            if (conteudoNaLinha is not null) partes.Add(conteudoNaLinha);

            for (var j = i + 1; j < linhas.Count && !EhTitulo(linhas[j], TodosOsTitulos, out _); j++)
                partes.Add(linhas[j]);

            return partes.Count == 0 ? null : Limitar(string.Join(' ', partes), LimiteResumo);
        }

        return null;
    }

    private static bool EhTitulo(string linha, HashSet<string> titulos, out string? conteudoNaLinha)
    {
        conteudoNaLinha = null;

        var separador = linha.IndexOf(':');
        var cabecalho = separador >= 0 ? linha[..separador] : linha;

        if (!titulos.Contains(Normalizar(cabecalho)))
            return false;

        if (separador >= 0)
        {
            var resto = linha[(separador + 1)..].Trim();
            conteudoNaLinha = resto.Length > 0 ? resto : null;
        }

        return true;
    }

    private static string? LinhaSeguinte(List<string> linhas, int indice)
    {
        if (indice + 1 >= linhas.Count) return null;

        var proxima = linhas[indice + 1];
        return EhTitulo(proxima, TodosOsTitulos, out _) ? null : proxima;
    }

    private static string Limitar(string texto, int maximo)
    {
        if (texto.Length <= maximo) return texto;

        var corte = texto.LastIndexOf(' ', maximo);
        return (corte > 0 ? texto[..corte] : texto[..maximo]).TrimEnd();
    }

    private static string Normalizar(string texto)
    {
        var decomposto = texto.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var semAcentos = new string(decomposto
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .ToArray());

        return EspacosRegex().Replace(semAcentos, " ");
    }

    [GeneratedRegex(@"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}")]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"(?<!\d)(?:\+?55[\s-]?)?\(?\d{2}\)?[\s-]?9?\d{4}[\s.-]?\d{4}(?!\d)")]
    private static partial Regex TelefoneRegex();

    [GeneratedRegex(@"^\p{L}[\p{L}' .-]*$")]
    private static partial Regex NomeRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex EspacosRegex();
}