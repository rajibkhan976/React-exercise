CREATE TABLE [dbo].[DelegationRoles] (
    [Id]                                INT             IDENTITY (1, 1) NOT NULL,
    [RoleId]                            INT             NOT NULL,
    [DelegationRoleId]                  INT             NOT NULL,
    [CreatedBy]                         NVARCHAR (20)   NULL,
    [CreatedDate]                       DATETIME        NULL,
    [ChangedBy]                         NVARCHAR (20)   NULL,
    [ChangedDate]                       DATETIME        NULL,
    CONSTRAINT [dbo.DelegationRoles]    UNIQUE          ([RoleId], [DelegationRoleId]),
    CONSTRAINT [PK_dbo.DelegationRoles] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_dbo.DelegationRoles_dbo.Roles_RoleId]              FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Roles] ([Id]),
    CONSTRAINT [FK_dbo.DelegationRoles_dbo.Roles_DelegationRoleId]    FOREIGN KEY ([DelegationRoleId]) REFERENCES [dbo].[Roles] ([Id])
);


GO