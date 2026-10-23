using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Cadastro_Curriculos.Infrastructure;

public static class ErrosDeBanco
{
    /// <summary>
    /// Indica se a falha ao salvar foi uma violação de índice único, ou seja, o próprio banco
    /// recusou um valor repetido (e-mail). É o banco quem decide, não uma consulta prévia.
    /// </summary>
    public static bool ViolacaoDeUnicidade(DbUpdateException ex) => ex.InnerException switch
    {
        // SQL Server / Azure SQL: 2601 = índice único, 2627 = constraint única.
        SqlException { Number: 2601 or 2627 } => true,

        // SQLite, usado só pelos testes de integração: "UNIQUE constraint failed: ...".
        { } interna when interna.GetType().Name == "SqliteException"
            && interna.Message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase) => true,

        _ => false
    };
}
