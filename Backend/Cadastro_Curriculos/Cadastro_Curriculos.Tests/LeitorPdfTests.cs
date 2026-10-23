using System.Text;
using Cadastro_Curriculos.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cadastro_Curriculos.Tests;

public class LeitorPdfTests
{
    private readonly LeitorPdf _leitor = new(NullLogger<LeitorPdf>.Instance);

    private static readonly byte[] CabecalhoPdf = Encoding.ASCII.GetBytes("%PDF-1.7\n");

    private static IFormFile CriarArquivo(string nome, byte[] conteudo) =>
        new FormFile(new MemoryStream(conteudo), 0, conteudo.Length, "arquivo", nome);

    [Fact]
    public async Task ValidarAsync_ArquivoNulo_RetornaErro()
    {
        var erro = await _leitor.ValidarAsync(null);

        Assert.Equal("Nenhum arquivo foi enviado.", erro);
    }

    [Fact]
    public async Task ValidarAsync_ArquivoVazio_RetornaErro()
    {
        var erro = await _leitor.ValidarAsync(CriarArquivo("curriculo.pdf", []));

        Assert.Equal("Nenhum arquivo foi enviado.", erro);
    }

    [Fact]
    public async Task ValidarAsync_ArquivoAcimaDe5MB_RetornaErro()
    {
        var conteudo = new byte[LeitorPdf.TamanhoMaximoBytes + 1];
        CabecalhoPdf.CopyTo(conteudo, 0);

        var erro = await _leitor.ValidarAsync(CriarArquivo("curriculo.pdf", conteudo));

        Assert.Equal("O arquivo excede o tamanho máximo de 5 MB.", erro);
    }

    [Fact]
    public async Task ValidarAsync_ExtensaoDiferenteDePdf_RetornaErro()
    {
        var erro = await _leitor.ValidarAsync(CriarArquivo("curriculo.docx", CabecalhoPdf));

        Assert.Equal("Formato inválido. Envie um arquivo PDF.", erro);
    }

    [Fact]
    public async Task ValidarAsync_ArquivoRenomeadoParaPdf_RetornaErro()
    {
        var conteudo = Encoding.UTF8.GetBytes("Isto é um arquivo de texto qualquer.");

        var erro = await _leitor.ValidarAsync(CriarArquivo("falso.pdf", conteudo));

        Assert.Equal("O arquivo não é um PDF válido.", erro);
    }

    [Theory]
    [InlineData("curriculo.pdf")]
    [InlineData("CURRICULO.PDF")]
    public async Task ValidarAsync_PdfValido_NaoRetornaErro(string nome)
    {
        var erro = await _leitor.ValidarAsync(CriarArquivo(nome, CabecalhoPdf));

        Assert.Null(erro);
    }

    [Fact]
    public async Task ExtrairTextoAsync_PdfCorrompido_LancaFalhaLeituraPdfException()
    {
        var conteudo = Encoding.ASCII.GetBytes("%PDF-1.7\nconteudo corrompido sem estrutura de PDF");

        await Assert.ThrowsAsync<FalhaLeituraPdfException>(
            () => _leitor.ExtrairTextoAsync(CriarArquivo("corrompido.pdf", conteudo)));
    }
}