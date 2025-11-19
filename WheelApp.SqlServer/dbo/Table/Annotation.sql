CREATE TABLE [dbo].[Annotation]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [ImageId] INT NOT NULL,
    [ProjectId] INT NOT NULL,
    [ClassId] INT NOT NULL,
    [Information] NVARCHAR(4000) NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [RowVersion] ROWVERSION NOT NULL,

    CONSTRAINT [PK_Annotation] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Annotation_Image] FOREIGN KEY ([ImageId]) REFERENCES [dbo].[Image]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Annotation_Project] FOREIGN KEY ([ProjectId]) REFERENCES [dbo].[Project]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Annotation_Class] FOREIGN KEY ([ClassId]) REFERENCES [dbo].[Class]([id]) ON DELETE NO ACTION
)
GO

CREATE NONCLUSTERED INDEX [IX_Annotation_ImageId] ON [dbo].[Annotation] ([ImageId])
GO

CREATE NONCLUSTERED INDEX [IX_Annotation_ProjectId] ON [dbo].[Annotation] ([ProjectId])
GO