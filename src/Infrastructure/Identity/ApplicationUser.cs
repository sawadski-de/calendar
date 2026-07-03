using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity;

/// <summary>
/// ASP.NET Core Identity's auth-concern user record (password hash, security stamp) — kept separate
/// from the Domain <see cref="Domain.Person"/> entity so Domain never references the Identity framework
/// (Story 1.1 Critical Guardrail #3). Shares its Id with the corresponding Person.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>;
