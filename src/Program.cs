using Newtonsoft.Json;
using WorkforceOpsAutomation.Models;
using WorkforceOpsAutomation.Services;

namespace WorkforceOpsAutomation;

class Program
{
    // ─── CONFIGURE YOUR LOCAL MySQL CONNECTION HERE ────────────────────────────
    private const string ConnectionString =
        "Server=localhost;Port=3306;Database=workforce_ops;Uid=root;Pwd=YOUR_PASSWORD_HERE;";

    static int Main(string[] args)
    {
        Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║     Workforce Ops Automation — Contractor Onboarding     ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        // ── Resolve input file ─────────────────────────────────────────────────
        string inputFile = args.Length > 0 ? args[0] : "new_contractor.json";

        if (!File.Exists(inputFile))
        {
            Console.WriteLine($"[ERROR] Input file not found: {inputFile}");
            Console.WriteLine($"        Usage: WorkforceOpsAutomation <path_to_contractor.json>");
            return 1;
        }

        Console.WriteLine($"[INFO]  Reading payload: {inputFile}");

        // ── Deserialise payload ────────────────────────────────────────────────
        ContractorPayload payload;
        try
        {
            string json = File.ReadAllText(inputFile);
            payload = JsonConvert.DeserializeObject<ContractorPayload>(json)
                      ?? throw new InvalidDataException("JSON parsed to null.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Failed to parse input file: {ex.Message}");
            return 1;
        }

        Console.WriteLine($"[INFO]  Contractor: {payload.ContractorName} | Role: {payload.Role} | Level: {payload.AccessLevel}");
        Console.WriteLine();
        Console.WriteLine("── Running Onboarding Engine ──────────────────────────────────");

        // ── Connect to DB and run engine ───────────────────────────────────────
        ComplianceLog result;
        try
        {
            using var db = new DatabaseService(ConnectionString);
            var engine = new OnboardingEngine(db, payload);
            result = engine.Run(payload);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n[FATAL] Could not connect to database: {ex.Message}");
            Console.WriteLine("        Check your connection string in Program.cs");
            return 1;
        }

        Console.WriteLine();
        Console.WriteLine("── Result ─────────────────────────────────────────────────────");
        Console.WriteLine($"  Status : {result.Status}");
        Console.WriteLine($"  Detail : {result.StatusDetail}");

        // ── Write compliance log ───────────────────────────────────────────────
        string outputDir = "output";
        Directory.CreateDirectory(outputDir);

        string logFileName = $"onboarding_compliance_log_{result.RunId[..8]}.json";
        string logPath = Path.Combine(outputDir, logFileName);

        string logJson = JsonConvert.SerializeObject(result, Formatting.Indented);
        File.WriteAllText(logPath, logJson);

        Console.WriteLine();
        Console.WriteLine($"[AUDIT] Compliance log written → {logPath}");
        Console.WriteLine();

        if (result.Status == "SUCCESS")
        {
            Console.WriteLine("✅  ONBOARDING COMPLETE — SLA satisfied. All provisioning confirmed.");
            return 0;
        }
        else
        {
            Console.WriteLine("❌  ONBOARDING FAILED — See compliance log for details.");
            return 2;
        }
    }
}
