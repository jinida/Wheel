CREATE TABLE [dbo].[Class]
(
    [id] INT IDENTITY(1,1) NOT NULL,
    [projectId] INT NOT NULL,
    [classIdx] INT NOT NULL,
    [name] NVARCHAR(30) NOT NULL,
    [color] NVARCHAR(7) NOT NULL,
    [rowVersion] ROWVERSION NOT NULL,

    CONSTRAINT [PK_Class] PRIMARY KEY CLUSTERED ([id] ASC),
    CONSTRAINT [FK_Class_Project] FOREIGN KEY ([projectId]) REFERENCES [dbo].[Project]([Id]) ON DELETE CASCADE,
    CONSTRAINT [UQ_Class_ProjectId_ClassIdx] UNIQUE ([projectId], [classIdx]),
    CONSTRAINT [CK_Class_ClassIdx] CHECK ([classIdx] >= 0),
    CONSTRAINT [CK_Class_Color] CHECK ([color] LIKE '#[0-9A-F][0-9A-F][0-9A-F][0-9A-F][0-9A-F][0-9A-F]')
)
GO

CREATE NONCLUSTERED INDEX [IX_Class_ProjectId] ON [dbo].[Class] ([projectId])
GO
