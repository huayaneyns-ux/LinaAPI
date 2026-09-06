/* Migración incremental exclusiva para Nota de Débito (SUNAT 08).
   No modifica tablas ni datos de factura, boleta o nota de crédito. */
IF OBJECT_ID(N'dbo.NotaDebitoMetadata', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.NotaDebitoMetadata
    (
        VoucherId UNIQUEIDENTIFIER NOT NULL,
        SolicitudId UNIQUEIDENTIFIER NULL,
        SolicitudHash VARBINARY(32) NULL,
        EmissionTime TIME(0) NOT NULL,
        EmissionTimeZone VARCHAR(64) NOT NULL CONSTRAINT DF_NotaDebitoMetadata_TimeZone DEFAULT 'America/Lima',
        PaymentMethod VARCHAR(20) NOT NULL CONSTRAINT DF_NotaDebitoMetadata_PaymentMethod DEFAULT 'EFECTIVO',
        BuilderVersion VARCHAR(40) NOT NULL CONSTRAINT DF_NotaDebitoMetadata_BuilderVersion DEFAULT 'ND-1',
        CorrectedVoucherId UNIQUEIDENTIFIER NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_NotaDebitoMetadata_CreatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_NotaDebitoMetadata PRIMARY KEY (VoucherId),
        CONSTRAINT FK_NotaDebitoMetadata_Voucher FOREIGN KEY (VoucherId) REFERENCES dbo.Voucher(Id),
        CONSTRAINT FK_NotaDebitoMetadata_Corrected FOREIGN KEY (CorrectedVoucherId) REFERENCES dbo.Voucher(Id),
        CONSTRAINT CK_NotaDebitoMetadata_PaymentMethod CHECK (PaymentMethod = 'EFECTIVO')
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_NotaDebitoMetadata_SolicitudId' AND object_id = OBJECT_ID(N'dbo.NotaDebitoMetadata'))
BEGIN
    CREATE UNIQUE INDEX UQ_NotaDebitoMetadata_SolicitudId
        ON dbo.NotaDebitoMetadata(SolicitudId)
        WHERE SolicitudId IS NOT NULL;
END;
GO

IF OBJECT_ID(N'dbo.NotaDebitoItemMetadata', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.NotaDebitoItemMetadata
    (
        VoucherItemId UNIQUEIDENTIFIER NOT NULL,
        Scope VARCHAR(20) NOT NULL,
        TaxAffectationCode CHAR(2) NOT NULL CONSTRAINT DF_NotaDebitoItemMetadata_Tax DEFAULT '10',
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_NotaDebitoItemMetadata_CreatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_NotaDebitoItemMetadata PRIMARY KEY (VoucherItemId),
        CONSTRAINT FK_NotaDebitoItemMetadata_Item FOREIGN KEY (VoucherItemId) REFERENCES dbo.VoucherItem(Id),
        CONSTRAINT CK_NotaDebitoItemMetadata_Scope CHECK (Scope IN ('COMPROBANTE', 'ITEM'))
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_NotaDebitoMetadata_Pending' AND object_id = OBJECT_ID(N'dbo.Voucher'))
BEGIN
    CREATE INDEX IX_NotaDebitoMetadata_Pending
        ON dbo.Voucher(SunatStatus, Series, Number)
        WHERE SunatTypeCode = '08';
END;
GO
