CREATE TABLE [dbo].[Training]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [ProjectId] INT NOT NULL,
    [Name] NVARCHAR(200) NOT NULL,
    [Status] INT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [EndedAt] DATETIME2 NULL,
    [RowVersion] ROWVERSION NOT NULL,

    CONSTRAINT [PK_Training] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Training_Project] FOREIGN KEY ([ProjectId]) REFERENCES [dbo].[Project]([Id]) ON DELETE CASCADE,
    CONSTRAINT [CK_Training_Status] CHECK ([Status] IN (0, 1, 2, 3)),
)
GO

CREATE NONCLUSTERED INDEX [IX_Training_ProjectId] ON [dbo].[Training] ([ProjectId])
GO