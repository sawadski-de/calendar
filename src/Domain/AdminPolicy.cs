namespace Domain;

/// <summary>
/// Bus-factor-1 guardrail (Story 1.1, AC 11): at least one active Admin must always remain.
/// </summary>
public static class AdminPolicy
{
    public static bool CanDemoteLastAdmin(int currentActiveAdminCount) => currentActiveAdminCount > 1;
}
