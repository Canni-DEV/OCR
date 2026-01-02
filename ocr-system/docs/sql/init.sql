CREATE TABLE OcrAudit (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    RequestId NVARCHAR(64) NOT NULL,
    FileName NVARCHAR(255) NOT NULL,
    Length BIGINT NOT NULL,
    Source NVARCHAR(50) NOT NULL,
    CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    Endpoints NVARCHAR(MAX) NULL
);

CREATE UNIQUE INDEX IX_OcrAudit_RequestId ON OcrAudit (RequestId);

CREATE TABLE AzureUsage (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    PeriodStartUtc DATETIME2 NOT NULL,
    PeriodEndUtc DATETIME2 NOT NULL,
    UsedCount INT NOT NULL
);

CREATE UNIQUE INDEX IX_AzureUsage_Period ON AzureUsage (PeriodStartUtc, PeriodEndUtc);

CREATE TABLE RemitoExamples (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CuitCliente NVARCHAR(32) NOT NULL,
    ExampleText NVARCHAR(256) NOT NULL
);

CREATE INDEX IX_RemitoExamples_CuitCliente ON RemitoExamples (CuitCliente);
