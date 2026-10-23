using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace Cadastro_Curriculos.Services;

public class LeitorPdf(ILogger<LeitorPdf> logger)
{
    public const long TamanhoMaximoBytes = 5 * 1024 * 1024; // 5 MB

    private static readonly byte[] AssinaturaPdf = "%PDF-"u8.ToArray();

    public async Task<string?> ValidarAsync(IFormFile? arquivo)
    {
        if (arquivo is null || arquivo.Length == 0)
            return "Nenhum arquivo foi enviado.";

        if (arquivo.Length > TamanhoMaximoBytes)
            return "O arquivo excede o tamanho máximo de 5 MB.";

        if (!Path.GetExtension(arquivo.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            return "Formato inválido. Envie um arquivo PDF.";

        var cabecalho = new byte[AssinaturaPdf.Length];
        await using var stream = arquivo.OpenReadStream();
        var lidos = await stream.ReadAtLeastAsync(cabecalho, cabecalho.Length, throwOnEndOfStream: false);

        if (lidos < cabecalho.Length || !cabecalho.SequenceEqual(AssinaturaPdf))
            return "O arquivo não é um PDF válido.";

        return null;
    }

    public async Task<string> ExtrairTextoAsync(IFormFile arquivo)
    {
        using var memoria = new MemoryStream();
        await arquivo.CopyToAsync(memoria);
        memoria.Position = 0;

        try
        {
            using var documento = PdfDocument.Open(memoria);
            var texto = new StringBuilder();

            foreach (var pagina in documento.GetPages())
                texto.AppendLine(ContentOrderTextExtractor.GetText(pagina));

            return texto.ToString();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao ler o PDF {Arquivo}", arquivo.FileName);
            throw new FalhaLeituraPdfException(
                "Não foi possível ler o conteúdo do PDF. Verifique se o arquivo não está corrompido ou protegido por senha.",
                ex);
        }
    }
}
