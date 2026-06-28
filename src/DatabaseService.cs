using MySql.Data.MySqlClient;
using WorkforceOpsAutomation.Models;

namespace WorkforceOpsAutomation.Services;

public class DatabaseService : IDisposable
{
    private readonly MySqlConnection _connection;

    public DatabaseService(string connectionString)
    {
        _connection = new MySqlConnection(connectionString);
        _connection.Open();
    }

    // ─── Asset Operations ──────────────────────────────────────────────────────

    /// <summary>
    /// Finds the first available virtual asset. Returns null if inventory is exhausted.
    /// </summary>
    public Asset? FindAvailableAsset()
    {
        const string sql = @"
            SELECT AssetId, AssetTag, AssetType, Model, Status, LicenseKey
            FROM Assets
            WHERE Status = 'Available'
            ORDER BY AssetId ASC
            LIMIT 1;";

        using var cmd = new MySqlCommand(sql, _connection);
        using var reader = cmd.ExecuteReader();

        if (!reader.Read()) return null;

        return new Asset
        {
            AssetId      = reader.GetInt32("AssetId"),
            AssetTag     = reader.GetString("AssetTag"),
            AssetType    = reader.GetString("AssetType"),
            Model        = reader.GetString("Model"),
            Status       = reader.GetString("Status"),
            LicenseKey   = reader.GetString("LicenseKey")
        };
    }

    /// <summary>
    /// Atomically marks an asset as Assigned. Returns false if asset was already taken.
    /// </summary>
    public bool MarkAssetAssigned(int assetId)
    {
        const string sql = @"
            UPDATE Assets
            SET Status = 'Assigned', AssignedAt = NOW()
            WHERE AssetId = @AssetId AND Status = 'Available';";

        using var cmd = new MySqlCommand(sql, _connection);
        cmd.Parameters.AddWithValue("@AssetId", assetId);
        return cmd.ExecuteNonQuery() > 0;
    }

    // ─── Contractor Operations ─────────────────────────────────────────────────

    /// <summary>
    /// Inserts a provisioned contractor profile and returns the new ContractorId.
    /// </summary>
    public int InsertContractor(ProvisionedContractor c)
    {
        const string sql = @"
            INSERT INTO Contractors
                (ContractorName, CorporateEmail, Role, Department, AccessLevel,
                 SystemGroups, SoftwareLicenses, AssignedAssetId, ContractExpiry, OnboardedAt)
            VALUES
                (@ContractorName, @CorporateEmail, @Role, @Department, @AccessLevel,
                 @SystemGroups, @SoftwareLicenses, @AssignedAssetId, @ContractExpiry, @OnboardedAt);
            SELECT LAST_INSERT_ID();";

        using var cmd = new MySqlCommand(sql, _connection);
        cmd.Parameters.AddWithValue("@ContractorName",    c.ContractorName);
        cmd.Parameters.AddWithValue("@CorporateEmail",    c.CorporateEmail);
        cmd.Parameters.AddWithValue("@Role",              c.Role);
        cmd.Parameters.AddWithValue("@Department",        c.Department);
        cmd.Parameters.AddWithValue("@AccessLevel",       c.AccessLevel);
        cmd.Parameters.AddWithValue("@SystemGroups",      c.SystemGroups);
        cmd.Parameters.AddWithValue("@SoftwareLicenses",  c.SoftwareLicenses);
        cmd.Parameters.AddWithValue("@AssignedAssetId",   c.AssignedAssetId);
        cmd.Parameters.AddWithValue("@ContractExpiry",    c.ContractExpiry);
        cmd.Parameters.AddWithValue("@OnboardedAt",       c.OnboardedAt);

        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public void Dispose() => _connection.Dispose();
}
