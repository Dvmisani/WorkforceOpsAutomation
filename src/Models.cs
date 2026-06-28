namespace WorkforceOpsAutomation.Models;

public class ContractorPayload
{
    public string ContractorName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string AccessLevel { get; set; } = string.Empty;
    public string ContractDurationMonths { get; set; } = "6";
}

public class Asset
{
    public int AssetId { get; set; }
    public string AssetTag { get; set; } = string.Empty;
    public string AssetType { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string LicenseKey { get; set; } = string.Empty;
}

public class ProvisionedContractor
{
    public int ContractorId { get; set; }
    public string ContractorName { get; set; } = string.Empty;
    public string CorporateEmail { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string AccessLevel { get; set; } = string.Empty;
    public string SystemGroups { get; set; } = string.Empty;
    public string SoftwareLicenses { get; set; } = string.Empty;
    public int AssignedAssetId { get; set; }
    public string ContractExpiry { get; set; } = string.Empty;
    public string OnboardedAt { get; set; } = string.Empty;
}

public class ComplianceLog
{
    public string RunId { get; set; } = Guid.NewGuid().ToString();
    public string Timestamp { get; set; } = DateTime.UtcNow.ToString("o");
    public string Status { get; set; } = string.Empty;
    public string StatusDetail { get; set; } = string.Empty;
    public ContractorPayload? InputPayload { get; set; }
    public ProvisionedContractor? ProvisionedProfile { get; set; }
    public Asset? AssignedAsset { get; set; }
    public List<string> AuditSteps { get; set; } = new();
    public string? FailureReason { get; set; }
}
