using WorkforceOpsAutomation.Models;

namespace WorkforceOpsAutomation.Services;

public class ValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public string SystemGroups { get; set; } = string.Empty;
    public string SoftwareLicenses { get; set; } = string.Empty;
}

public static class ValidationEngine
{
    // RBAC mapping: AccessLevel → system groups and software bundles
    private static readonly Dictionary<string, (string Groups, string Licenses)> RbacMap = new()
    {
        ["Tier-1"] = (
            Groups: "GRP_ReadOnly_Monitoring,GRP_ServiceDesk_Basic,GRP_VPN_Standard",
            Licenses: "Microsoft365_Basic,Monitoring_Dashboard_ReadOnly,Slack_Standard"
        ),
        ["Tier-2"] = (
            Groups: "GRP_CloudOps_Contributor,GRP_Incident_Response,GRP_VPN_Standard",
            Licenses: "Microsoft365_Business,Azure_Portal_Contributor,Jira_Standard,Slack_Standard"
        ),
        ["Tier-3"] = (
            Groups: "GRP_CloudOps_Admin,GRP_Security_Response,GRP_VPN_PrivilegedAccess",
            Licenses: "Microsoft365_E3,Azure_Portal_Admin,Jira_Premium,PagerDuty,Slack_Standard"
        )
    };

    private static readonly HashSet<string> ValidRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "Junior Cloud Support",
        "Senior Cloud Support",
        "Cloud Architect",
        "DevOps Engineer",
        "IT Support Analyst",
        "Security Analyst",
        "Database Administrator",
        "Network Engineer"
    };

    private static readonly HashSet<string> ValidDepartments = new(StringComparer.OrdinalIgnoreCase)
    {
        "Global Delivery",
        "Cybersecurity",
        "Infrastructure",
        "Application Management",
        "Contingent Workforce",
        "IT Operations"
    };

    public static ValidationResult Validate(ContractorPayload payload)
    {
        // --- Name validation ---
        if (string.IsNullOrWhiteSpace(payload.ContractorName))
            return Fail("ContractorName cannot be empty.");

        if (payload.ContractorName.Length > 100)
            return Fail("ContractorName exceeds maximum length of 100 characters.");

        // Reject injection characters
        if (ContainsSqlInjectionPatterns(payload.ContractorName))
            return Fail("DATA_INTEGRITY_VIOLATION: ContractorName contains disallowed characters.");

        // --- Role validation ---
        if (string.IsNullOrWhiteSpace(payload.Role))
            return Fail("Role cannot be empty.");

        if (!ValidRoles.Contains(payload.Role))
            return Fail($"DATA_INTEGRITY_VIOLATION: Role '{payload.Role}' is not a recognised system role. " +
                        $"Valid roles: {string.Join(", ", ValidRoles)}");

        // --- Department validation ---
        if (string.IsNullOrWhiteSpace(payload.Department))
            return Fail("Department cannot be empty.");

        if (!ValidDepartments.Contains(payload.Department))
            return Fail($"DATA_INTEGRITY_VIOLATION: Department '{payload.Department}' is not a recognised department. " +
                        $"Valid departments: {string.Join(", ", ValidDepartments)}");

        // --- AccessLevel (RBAC) validation ---
        if (string.IsNullOrWhiteSpace(payload.AccessLevel))
            return Fail("AccessLevel cannot be empty.");

        if (!RbacMap.TryGetValue(payload.AccessLevel, out var rbac))
            return Fail($"DATA_INTEGRITY_VIOLATION: AccessLevel '{payload.AccessLevel}' is not a recognised tier. " +
                        $"Valid levels: {string.Join(", ", RbacMap.Keys)}");

        // --- Contract duration validation ---
        if (!int.TryParse(payload.ContractDurationMonths, out int duration) || duration < 1 || duration > 24)
            return Fail("ContractDurationMonths must be a number between 1 and 24.");

        return new ValidationResult
        {
            IsValid = true,
            SystemGroups = rbac.Groups,
            SoftwareLicenses = rbac.Licenses
        };
    }

    private static bool ContainsSqlInjectionPatterns(string input)
    {
        string[] patterns = { "--", ";", "/*", "*/", "xp_", "DROP", "SELECT", "INSERT", "DELETE", "UPDATE", "<script" };
        return patterns.Any(p => input.Contains(p, StringComparison.OrdinalIgnoreCase));
    }

    private static ValidationResult Fail(string message) =>
        new() { IsValid = false, ErrorMessage = message };
}
