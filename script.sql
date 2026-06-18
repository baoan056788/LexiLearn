BEGIN TRANSACTION;
CREATE TABLE [Feedbacks] (
    [FeedbackId] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [Subject] nvarchar(255) NOT NULL,
    [Content] nvarchar(max) NOT NULL,
    [Status] nvarchar(50) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Feedbacks] PRIMARY KEY ([FeedbackId]),
    CONSTRAINT [FK_Feedbacks_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE CASCADE
);

CREATE TABLE [Notifications] (
    [NotificationId] int NOT NULL IDENTITY,
    [Title] nvarchar(255) NOT NULL,
    [Content] nvarchar(max) NOT NULL,
    [Type] nvarchar(50) NOT NULL,
    [Status] nvarchar(50) NOT NULL,
    [CreatedById] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [SentAt] datetime2 NULL,
    CONSTRAINT [PK_Notifications] PRIMARY KEY ([NotificationId]),
    CONSTRAINT [FK_Notifications_Users_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION
);

CREATE TABLE [SystemSettings] (
    [SettingKey] nvarchar(100) NOT NULL,
    [SettingValue] nvarchar(max) NOT NULL,
    [Description] nvarchar(255) NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_SystemSettings] PRIMARY KEY ([SettingKey])
);

CREATE TABLE [WordDefinitions] (
    [WordId] int NOT NULL IDENTITY,
    [Word] nvarchar(100) NOT NULL,
    [Phonetic] nvarchar(100) NULL,
    [AudioUrl] nvarchar(500) NULL,
    CONSTRAINT [PK_WordDefinitions] PRIMARY KEY ([WordId])
);

CREATE TABLE [NotificationRecipients] (
    [Id] int NOT NULL IDENTITY,
    [NotificationId] int NOT NULL,
    [UserId] int NOT NULL,
    [IsRead] bit NOT NULL,
    [ReadAt] datetime2 NULL,
    CONSTRAINT [PK_NotificationRecipients] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_NotificationRecipients_Notifications_NotificationId] FOREIGN KEY ([NotificationId]) REFERENCES [Notifications] ([NotificationId]) ON DELETE CASCADE,
    CONSTRAINT [FK_NotificationRecipients_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId])
);

CREATE TABLE [RelatedWords] (
    [Id] int NOT NULL IDENTITY,
    [WordId] int NOT NULL,
    [RelatedWordText] nvarchar(100) NOT NULL,
    CONSTRAINT [PK_RelatedWords] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RelatedWords_WordDefinitions_WordId] FOREIGN KEY ([WordId]) REFERENCES [WordDefinitions] ([WordId]) ON DELETE CASCADE
);

CREATE TABLE [Synonyms] (
    [Id] int NOT NULL IDENTITY,
    [WordId] int NOT NULL,
    [SynonymWord] nvarchar(100) NOT NULL,
    CONSTRAINT [PK_Synonyms] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Synonyms_WordDefinitions_WordId] FOREIGN KEY ([WordId]) REFERENCES [WordDefinitions] ([WordId]) ON DELETE CASCADE
);

CREATE TABLE [WordTypes] (
    [Id] int NOT NULL IDENTITY,
    [WordId] int NOT NULL,
    [PartOfSpeech] nvarchar(50) NOT NULL,
    [Meaning] nvarchar(max) NOT NULL,
    [Example] nvarchar(max) NULL,
    CONSTRAINT [PK_WordTypes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_WordTypes_WordDefinitions_WordId] FOREIGN KEY ([WordId]) REFERENCES [WordDefinitions] ([WordId]) ON DELETE CASCADE
);

CREATE INDEX [IX_Feedbacks_UserId] ON [Feedbacks] ([UserId]);

CREATE INDEX [IX_NotificationRecipients_NotificationId] ON [NotificationRecipients] ([NotificationId]);

CREATE INDEX [IX_NotificationRecipients_UserId] ON [NotificationRecipients] ([UserId]);

CREATE INDEX [IX_Notifications_CreatedById] ON [Notifications] ([CreatedById]);

CREATE INDEX [IX_RelatedWords_WordId] ON [RelatedWords] ([WordId]);

CREATE INDEX [IX_Synonyms_WordId] ON [Synonyms] ([WordId]);

CREATE INDEX [IX_WordTypes_WordId] ON [WordTypes] ([WordId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260615091934_AddAdminTables', N'9.0.15');

COMMIT;
GO

