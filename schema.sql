-- ============================================================
--  Workforce Ops Automation — Database Schema
--  Run this once in MySQL Workbench to initialise your DB.
-- ============================================================

CREATE DATABASE IF NOT EXISTS workforce_ops
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

USE workforce_ops;

-- ──────────────────────────────────────────────────────────
--  TABLE: Assets
--  Stores virtual IT assets and software licenses.
--  Status: 'Available' | 'Assigned' | 'Retired'
-- ──────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS Assets (
    AssetId     INT AUTO_INCREMENT PRIMARY KEY,
    AssetTag    VARCHAR(50)  NOT NULL UNIQUE,
    AssetType   VARCHAR(50)  NOT NULL,          -- e.g. 'Virtual Laptop', 'Cloud License'
    Model       VARCHAR(100) NOT NULL,
    Status      ENUM('Available','Assigned','Retired') NOT NULL DEFAULT 'Available',
    LicenseKey  VARCHAR(100) NOT NULL DEFAULT 'N/A',
    AssignedAt  DATETIME     NULL,
    CreatedAt   DATETIME     NOT NULL DEFAULT NOW()
);

-- ──────────────────────────────────────────────────────────
--  TABLE: Contractors
--  Stores provisioned contractor profiles.
-- ──────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS Contractors (
    ContractorId     INT AUTO_INCREMENT PRIMARY KEY,
    ContractorName   VARCHAR(100) NOT NULL,
    CorporateEmail   VARCHAR(150) NOT NULL UNIQUE,
    Role             VARCHAR(100) NOT NULL,
    Department       VARCHAR(100) NOT NULL,
    AccessLevel      VARCHAR(20)  NOT NULL,
    SystemGroups     TEXT         NOT NULL,
    SoftwareLicenses TEXT         NOT NULL,
    AssignedAssetId  INT          NOT NULL,
    ContractExpiry   DATE         NOT NULL,
    OnboardedAt      DATETIME     NOT NULL,

    FOREIGN KEY (AssignedAssetId) REFERENCES Assets(AssetId)
);

-- ──────────────────────────────────────────────────────────
--  SEED DATA — 10 virtual assets
--  Run again after your Zero Inventory test to restore.
-- ──────────────────────────────────────────────────────────
INSERT INTO Assets (AssetTag, AssetType, Model, Status, LicenseKey) VALUES
  ('ASSET-V001', 'Virtual Laptop',   'Dell Latitude 5540 (VDI)',       'Available', 'N/A'),
  ('ASSET-V002', 'Virtual Laptop',   'Lenovo ThinkPad E14 (VDI)',      'Available', 'N/A'),
  ('ASSET-V003', 'Cloud License',    'Microsoft 365 Business Standard', 'Available', 'M365-BST-0003'),
  ('ASSET-V004', 'Cloud License',    'Microsoft 365 Business Standard', 'Available', 'M365-BST-0004'),
  ('ASSET-V005', 'Virtual Laptop',   'HP EliteBook 840 G10 (VDI)',     'Available', 'N/A'),
  ('ASSET-V006', 'Cloud License',    'Azure Contributor Seat',          'Available', 'AZ-CONT-0006'),
  ('ASSET-V007', 'Virtual Laptop',   'Dell Latitude 5540 (VDI)',       'Available', 'N/A'),
  ('ASSET-V008', 'Security Token',   'YubiKey 5 NFC (Virtual)',        'Available', 'YK-V-0008'),
  ('ASSET-V009', 'Cloud License',    'Microsoft 365 E3',               'Available', 'M365-E3-0009'),
  ('ASSET-V010', 'Virtual Laptop',   'Lenovo ThinkPad E14 (VDI)',      'Available', 'N/A');

-- ──────────────────────────────────────────────────────────
--  HELPER: Use this to simulate the Zero Inventory test
-- ──────────────────────────────────────────────────────────
-- UPDATE Assets SET Status = 'Retired' WHERE Status = 'Available';

-- ──────────────────────────────────────────────────────────
--  HELPER: Restore inventory after testing
-- ──────────────────────────────────────────────────────────
-- UPDATE Assets SET Status = 'Available', AssignedAt = NULL WHERE Status = 'Retired';

SELECT 'Schema initialised successfully.' AS Result;
