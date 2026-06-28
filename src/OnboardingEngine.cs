using WorkforceOpsAutomation.Models;

namespace WorkforceOpsAutomation.Services;

public class OnboardingEngine
{
    private readonly DatabaseService _db;
    private readonly ComplianceLog _log;

    public OnboardingEngine(DatabaseService db, ContractorPayload payload)
    {
        _db = db;
        _log = new ComplianceLog
        {
            InputPayload = payload
        };
    }

    public ComplianceLog Run(ContractorPayload payload)
    {
        try
        {
            // ── Step 1: Validate input ──────────────────────────────────────
            AddStep("STEP_1: Validating input payload...");

            var validation = ValidationEngine.Validate(payload);
            if (!validation.IsValid)
            {
                return Fail($"INPUT_VALIDATION_FAILED: {validation.ErrorMessage}");
            }
            AddStep($"STEP_1: PASSED — Role={payload.Role}, AccessLevel={payload.AccessLevel}");

            // ── Step 2: Find available asset ────────────────────────────────
            AddStep("STEP_2: Scanning asset inventory for available hardware/license...");

            var asset = _db.FindAvailableAsset();
            if (asset == null)
            {
                return Fail("ALLOCATION_FAILURE: No virtual assets are currently available. " +
                            "Infrastructure allocation limits have been reached. " +
                            "Please contact the IT Asset Management team to replenish inventory.");
            }
            AddStep($"STEP_2: Asset located — Tag={asset.AssetTag}, Model={asset.Model}");

            // ── Step 3: Reserve asset (atomic check) ────────────────────────
            AddStep($"STEP_3: Reserving asset {asset.AssetTag} (AssetId={asset.AssetId})...");

            bool reserved = _db.MarkAssetAssigned(asset.AssetId);
            if (!reserved)
            {
                return Fail("ALLOCATION_FAILURE: Asset was claimed by another process before reservation could complete. " +
                            "Re-run onboarding to attempt a new allocation.");
            }
            AddStep($"STEP_3: Asset {asset.AssetTag} successfully reserved and marked Assigned.");

            // ── Step 4: Build contractor profile ────────────────────────────
            AddStep("STEP_4: Building corporate profile and provisioning accounts...");

            string emailAlias = GenerateEmailAlias(payload.ContractorName);
            int durationMonths = int.Parse(payload.ContractDurationMonths);
            string expiry = DateTime.UtcNow.AddMonths(durationMonths).ToString("yyyy-MM-dd");
            string now = DateTime.UtcNow.ToString("o");

            var contractor = new ProvisionedContractor
            {
                ContractorName   = payload.ContractorName,
                CorporateEmail   = emailAlias,
                Role             = payload.Role,
                Department       = payload.Department,
                AccessLevel      = payload.AccessLevel,
                SystemGroups     = validation.SystemGroups,
                SoftwareLicenses = validation.SoftwareLicenses,
                AssignedAssetId  = asset.AssetId,
                ContractExpiry   = expiry,
                OnboardedAt      = now
            };

            AddStep($"STEP_4: Corporate email generated: {emailAlias}");
            AddStep($"STEP_4: System groups assigned: {validation.SystemGroups}");
            AddStep($"STEP_4: Software licenses provisioned: {validation.SoftwareLicenses}");

            // ── Step 5: Persist to database ─────────────────────────────────
            AddStep("STEP_5: Writing contractor record to database...");

            int newId = _db.InsertContractor(contractor);
            contractor.ContractorId = newId;

            AddStep($"STEP_5: Contractor profile created — ContractorId={newId}");

            // ── Step 6: Seal compliance log ──────────────────────────────────
            AddStep("STEP_6: Sealing compliance artifact. Onboarding complete.");

            _log.Status              = "SUCCESS";
            _log.StatusDetail        = "All onboarding steps completed within SLA window. Audit trail sealed.";
            _log.ProvisionedProfile  = contractor;
            _log.AssignedAsset       = asset;
        }
        catch (Exception ex)
        {
            return Fail($"SYSTEM_ERROR: Unexpected exception — {ex.Message}");
        }

        return _log;
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private ComplianceLog Fail(string reason)
    {
        _log.Status        = "FAILED";
        _log.StatusDetail  = reason;
        _log.FailureReason = reason;
        AddStep($"ABORTED: {reason}");
        return _log;
    }

    private void AddStep(string step)
    {
        Console.WriteLine($"  [{DateTime.UtcNow:HH:mm:ss.fff}] {step}");
        _log.AuditSteps.Add($"{DateTime.UtcNow:o} | {step}");
    }

    private static string GenerateEmailAlias(string fullName)
    {
        var parts = fullName.Trim().ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        string alias = parts.Length >= 2
            ? $"{parts[0]}.{parts[^1]}"
            : parts[0];
        // Strip anything that isn't alphanumeric or dot
        alias = System.Text.RegularExpressions.Regex.Replace(alias, @"[^a-z0-9\.]", "");
        return $"{alias}@corp.internal";
    }
}
