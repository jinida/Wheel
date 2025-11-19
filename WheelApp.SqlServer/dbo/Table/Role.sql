CREATE TABLE [dbo].[Role]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [ImageId] INT NOT NULL,
    [ProjectId] INT NOT NULL,
    [RoleType] INT NOT NULL,
    [RowVersion] ROWVERSION NOT NULL,

    CONSTRAINT [PK_Role] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Role_Image] FOREIGN KEY ([ImageId]) REFERENCES [dbo].[Image]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Role_Project] FOREIGN KEY ([ProjectId]) REFERENCES [dbo].[Project]([Id]) ON DELETE CASCADE,
    CONSTRAINT [UQ_Role_ImageId_ProjectId] UNIQUE ([ImageId], [ProjectId]),
    CONSTRAINT [CK_Role_RoleType] CHECK ([RoleType] IN (0, 1, 2, 3))
)
GO

CREATE NONCLUSTERED INDEX [IX_Role_ProjectId] ON [dbo].[Role] ([ProjectId])
GO
