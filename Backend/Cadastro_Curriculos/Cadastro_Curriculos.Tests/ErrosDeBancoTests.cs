using Cadastro_Curriculos.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cadastro_Curriculos.Tests;

public class ErrosDeBancoTests
{
    [Fact]
    public void ViolacaoDeUnicidade_SemExcecaoInterna_RetornaFalso()
    {
        var ex = new DbUpdateException("falha ao salvar");

        Assert.False(ErrosDeBanco.ViolacaoDeUnicidade(ex));
    }

    [Fact]
    public void ViolacaoDeUnicidade_ExcecaoInternaQualquer_RetornaFalso()
    {
        var ex = new DbUpdateException("falha ao salvar", new InvalidOperationException("outro problema"));

        Assert.False(ErrosDeBanco.ViolacaoDeUnicidade(ex));
    }

    [Fact]
    public void ViolacaoDeUnicidade_ErroDeUnicidadeDoSqlite_RetornaVerdadeiro()
    {
        var interna = new SqliteException("SQLite Error 19: 'UNIQUE constraint failed: Candidatos.Email'.", 19);
        var ex = new DbUpdateException("falha ao salvar", interna);

        Assert.True(ErrosDeBanco.ViolacaoDeUnicidade(ex));
    }

    [Fact]
    public void ViolacaoDeUnicidade_OutroErroDoSqlite_RetornaFalso()
    {
        var interna = new SqliteException("SQLite Error 19: 'NOT NULL constraint failed: Candidatos.Email'.", 19);
        var ex = new DbUpdateException("falha ao salvar", interna);

        Assert.False(ErrosDeBanco.ViolacaoDeUnicidade(ex));
    }
}
