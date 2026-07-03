using Domain;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

/// <summary>
/// Uses <see cref="IdentityUserContext{TUser,TKey}"/> rather than the full <c>IdentityDbContext</c> —
/// Identity roles/claims tables are deliberately not created because <see cref="Domain.Person.Role"/>
/// is the sole authorization source of truth (Story 1.1 Critical Guardrail #6); Identity here only
/// provides login/password-hash machinery.
/// </summary>
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityUserContext<ApplicationUser, Guid>(options)
{
    public DbSet<Person> People => Set<Person>();
    public DbSet<Appointment> Appointments => Set<Appointment>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Person>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Email).IsRequired();
            entity.Property(p => p.Role).HasConversion<string>().IsRequired();
            entity.HasIndex(p => p.Email).IsUnique();
        });

        builder.Entity<Appointment>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Title).IsRequired();
            entity.Property(a => a.StartUtc).IsRequired();
            entity.Property(a => a.EndUtc).IsRequired();
            entity.HasOne<Person>().WithMany().HasForeignKey(a => a.PersonId);

            // AD-7: native appointments have Provider/ProviderEventId both NULL — the filter excludes
            // them so the partial index only enforces uniqueness among synced appointments. The filter
            // text must use the post-EFCore.NamingConventions snake_case column name, not the C#
            // property name — that convention doesn't rewrite raw SQL fragments like this one.
            entity.HasIndex(a => new { a.PersonId, a.Provider, a.ProviderEventId })
                .IsUnique()
                .HasFilter("provider_event_id IS NOT NULL");
        });
    }
}
