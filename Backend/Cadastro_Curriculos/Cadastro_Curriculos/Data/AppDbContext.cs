using CadastroCurriculos.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CadastroCurriculos.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Candidato> Candidatos => Set<Candidato>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<ExperienciaProfissional> ExperienciasProfissionais => Set<ExperienciaProfissional>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.DataCadastro).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        modelBuilder.Entity<Candidato>(e =>
        {
            e.HasIndex(c => c.Email).IsUnique();
            e.Property(c => c.DataCadastro).HasDefaultValueSql("SYSUTCDATETIME()");

            // 1:0..1 — um usuário tem no máximo um currículo; o índice único garante isso no banco.
            e.HasOne(c => c.Usuario)
                .WithOne(u => u.Candidato)
                .HasForeignKey<Candidato>(c => c.UsuarioId)
                .OnDelete(DeleteBehavior.SetNull);

            // 1:N — ao excluir o candidato, as experiências dele são excluídas junto.
            e.HasMany(c => c.Experiencias)
                .WithOne(x => x.Candidato)
                .HasForeignKey(x => x.CandidatoId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
