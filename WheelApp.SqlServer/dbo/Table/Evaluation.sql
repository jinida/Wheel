CREATE TABLE [dbo].[Evaluation]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [TrainingId] INT NOT NULL,
    [Path] NVARCHAR(512) NOT NULL,
    [MetricsJson] NVARCHAR(4000) NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [RowVersion] ROWVERSION NOT NULL,

    CONSTRAINT [PK_Evaluation] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Evaluation_Training] FOREIGN KEY ([TrainingId]) REFERENCES [dbo].[Training]([Id]) ON DELETE CASCADE
)
GO

CREATE NONCLUSTERED INDEX [IX_Evaluation_TrainingId] ON [dbo].[Evaluation] ([TrainingId])
GO
