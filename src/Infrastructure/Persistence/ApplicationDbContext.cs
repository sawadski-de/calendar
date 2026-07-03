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
    public DbSet<Attendee> Attendees => Set<Attendee>();

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
            entity.Property(a => a.IsAllDay).IsRequired();
            // Explicit default so a future ADD COLUMN backfills any pre-existing row with a valid
            // enum member (the string conversion has no "" member) instead of EF's CLR-default blank
            // string, which would throw on the very next read of that row.
            entity.Property(a => a.Status).HasConversion<string>().IsRequired().HasDefaultValue(AvailabilityStatus.Unterbrechbar);
            entity.HasOne<Person>().WithMany().HasForeignKey(a => a.PersonId);

            // Backing field (_attendees) is picked up by EF Core's default convention — no extra
            // access-mode configuration needed, same as this codebase's other read-only properties.
            entity.HasMany(a => a.Attendees).WithOne().HasForeignKey(at => at.AppointmentId).OnDelete(DeleteBehavior.Cascade);

            // AD-7: native appointments have Provider/ProviderEventId both NULL — the filter excludes
            // them so the partial index only enforces uniqueness among synced appointments. The filter
            // text must use the post-EFCore.NamingConventions snake_case column name, not the C#
            // property name — that convention doesn't rewrite raw SQL fragments like this one.
            entity.HasIndex(a => new { a.PersonId, a.Provider, a.ProviderEventId })
                .IsUnique()
                .HasFilter("provider_event_id IS NOT NULL");
        });

        builder.Entity<Attendee>(entity =>
        {
            entity.HasKey(at => at.Id);
            entity.HasOne<Person>().WithMany().HasForeignKey(at => at.PersonId).OnDelete(DeleteBehavior.Restrict);

            // Defensive guardrail: the same person cannot be added twice as an attendee of the same
            // appointment.
            entity.HasIndex(at => new { at.AppointmentId, at.PersonId }).IsUnique();
        });
    }
}
