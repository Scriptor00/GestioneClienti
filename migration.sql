IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [Clienti] (
    [IdCliente] int NOT NULL IDENTITY,
    [Nome] nvarchar(50) NOT NULL,
    [Cognome] nvarchar(50) NOT NULL,
    [Email] nvarchar(max) NOT NULL,
    [DataIscrizione] datetime2 NOT NULL,
    [Attivo] bit NOT NULL,
    CONSTRAINT [PK_Clienti] PRIMARY KEY ([IdCliente])
);

CREATE TABLE [Prodotti] (
    [IdProdotto] int NOT NULL IDENTITY,
    [NomeProdotto] nvarchar(100) NOT NULL,
    [Categoria] nvarchar(50) NOT NULL,
    [Prezzo] DECIMAL(10,2) NOT NULL,
    [Giacenza] int NOT NULL,
    [DataInserimento] datetime2 NOT NULL,
    CONSTRAINT [PK_Prodotti] PRIMARY KEY ([IdProdotto])
);

CREATE TABLE [Ordini] (
    [IdOrdine] int NOT NULL IDENTITY,
    [IdCliente] int NOT NULL,
    [DataOrdine] datetime2 NOT NULL,
    [Stato] nvarchar(20) NOT NULL,
    [TotaleOrdine] DECIMAL(10,2) NULL,
    CONSTRAINT [PK_Ordini] PRIMARY KEY ([IdOrdine]),
    CONSTRAINT [FK_Ordini_Clienti_IdCliente] FOREIGN KEY ([IdCliente]) REFERENCES [Clienti] ([IdCliente]) ON DELETE CASCADE
);

CREATE TABLE [DettagliOrdini] (
    [IdDettaglio] int NOT NULL IDENTITY,
    [IdOrdine] int NOT NULL,
    [IdProdotto] int NOT NULL,
    [Quantita] int NOT NULL,
    [PrezzoUnitario] DECIMAL(10,2) NOT NULL,
    CONSTRAINT [PK_DettagliOrdini] PRIMARY KEY ([IdDettaglio]),
    CONSTRAINT [FK_DettagliOrdini_Ordini_IdOrdine] FOREIGN KEY ([IdOrdine]) REFERENCES [Ordini] ([IdOrdine]) ON DELETE CASCADE,
    CONSTRAINT [FK_DettagliOrdini_Prodotti_IdProdotto] FOREIGN KEY ([IdProdotto]) REFERENCES [Prodotti] ([IdProdotto]) ON DELETE CASCADE
);

CREATE INDEX [IX_DettagliOrdini_IdOrdine] ON [DettagliOrdini] ([IdOrdine]);

CREATE INDEX [IX_DettagliOrdini_IdProdotto] ON [DettagliOrdini] ([IdProdotto]);

CREATE INDEX [IX_Ordini_IdCliente] ON [Ordini] ([IdCliente]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250225113014_InitialCreate', N'9.0.3');

DECLARE @var sysname;
SELECT @var = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Ordini]') AND [c].[name] = N'TotaleOrdine');
IF @var IS NOT NULL EXEC(N'ALTER TABLE [Ordini] DROP CONSTRAINT [' + @var + '];');
UPDATE [Ordini] SET [TotaleOrdine] = 0.0 WHERE [TotaleOrdine] IS NULL;
ALTER TABLE [Ordini] ALTER COLUMN [TotaleOrdine] DECIMAL(10,2) NOT NULL;
ALTER TABLE [Ordini] ADD DEFAULT 0.0 FOR [TotaleOrdine];

DECLARE @var1 sysname;
SELECT @var1 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Ordini]') AND [c].[name] = N'Stato');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Ordini] DROP CONSTRAINT [' + @var1 + '];');
ALTER TABLE [Ordini] ALTER COLUMN [Stato] int NOT NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250227105101_AddStatoOrdineEnumToOrdine', N'9.0.3');

ALTER TABLE [Ordini] DROP CONSTRAINT [FK_Ordini_Clienti_IdCliente];

ALTER TABLE [Ordini] ADD CONSTRAINT [FK_Ordini_Clienti_IdCliente] FOREIGN KEY ([IdCliente]) REFERENCES [Clienti] ([IdCliente]) ON DELETE NO ACTION;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250318103803_UltimaMigr', N'9.0.3');

CREATE UNIQUE INDEX [IX_Utenti_Username] ON [Utenti] ([Username]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250321122935_AddUtenteTable', N'9.0.3');

ALTER TABLE [Prodotti] ADD [TrailerUrl] nvarchar(max) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250324125550_AggiuntoTrailerUrl', N'9.0.3');

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250324130852_SyncWithExistingUtentiTable', N'9.0.3');

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250327091516_AddCarrelloTable', N'9.0.3');

CREATE TABLE [Carrello] (
    [Id] int NOT NULL IDENTITY,
    [UserId] nvarchar(max) NOT NULL,
    [ProdottoId] int NOT NULL,
    [Quantita] int NOT NULL,
    [DataAggiunta] datetime2 NOT NULL,
    CONSTRAINT [PK_Carrello] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Carrello_Prodotti_ProdottoId] FOREIGN KEY ([ProdottoId]) REFERENCES [Prodotti] ([IdProdotto]) ON DELETE CASCADE
);

CREATE INDEX [IX_Carrello_ProdottoId] ON [Carrello] ([ProdottoId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250327093322_CarrelloItem', N'9.0.3');

CREATE TABLE [Notifiche] (
    [Id] int NOT NULL IDENTITY,
    [Titolo] nvarchar(max) NOT NULL,
    [Messaggio] nvarchar(max) NOT NULL,
    [DataInvio] datetime2 NOT NULL,
    [Letta] bit NOT NULL,
    [Tipo] nvarchar(max) NOT NULL,
    [LinkAzione] nvarchar(max) NOT NULL,
    [IdRiferimento] int NULL,
    CONSTRAINT [PK_Notifiche] PRIMARY KEY ([Id])
);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250331094355_Notifiche', N'9.0.3');

ALTER TABLE [Utenti] ADD [Email] nvarchar(max) NOT NULL DEFAULT N'';

ALTER TABLE [Utenti] ADD [PasswordResetToken] nvarchar(max) NOT NULL DEFAULT N'';

ALTER TABLE [Utenti] ADD [PasswordResetTokenExpires] datetime2 NULL;

ALTER TABLE [Notifiche] ADD [NomeDestinatario] nvarchar(max) NOT NULL DEFAULT N'';

ALTER TABLE [Notifiche] ADD [UtenteId] int NOT NULL DEFAULT 0;

CREATE INDEX [IX_Notifiche_UtenteId] ON [Notifiche] ([UtenteId]);

ALTER TABLE [Notifiche] ADD CONSTRAINT [FK_Notifiche_Utenti_UtenteId] FOREIGN KEY ([UtenteId]) REFERENCES [Utenti] ([Id]) ON DELETE CASCADE;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250403105934_EmailToUtente', N'9.0.3');

COMMIT;
GO

