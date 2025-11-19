CREATE TABLE [dbo].[Project]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [Name] NVARCHAR(50) NOT NULL,
    [Type] INT NOT NULL,
    [Description] NVARCHAR(255) NULL,
    [DatasetId] INT NOT NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [CreatedBy] NVARCHAR(100) NULL,
    [ModifiedAt] DATETIME2 NULL,
    [ModifiedBy] NVARCHAR(100) NULL,
    [RowVersion] ROWVERSION NOT NULL,

    CONSTRAINT [PK_Project] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Project_Dataset] FOREIGN KEY ([DatasetId]) REFERENCES [dbo].[Dataset]([Id]) ON DELETE NO ACTION,
    CONSTRAINT [CK_Project_Type] CHECK ([Type] IN (0, 1, 2, 3))
)
GO

CREATE NONCLUSTERED INDEX [IX_Project_DatasetId] ON [dbo].[Project] ([DatasetId])
GO
