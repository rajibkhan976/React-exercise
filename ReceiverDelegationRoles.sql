CREATE TABLE [dbo].[ReceiverDelegationRoles] (
    [Id]                                        INT             IDENTITY (1, 1) NOT NULL,
    [ReceiverRoleId]                            INT             NOT NULL,
    [ReceiverDelegationRoleId]                  INT             NOT NULL,
    [CreatedBy]                                 NVARCHAR (20)   NULL,
    [CreatedDate]                               DATETIME        NULL,
    [ChangedBy]                                 NVARCHAR (20)   NULL,
    [ChangedDate]                               DATETIME        NULL,
    CONSTRAINT [dbo.ReceiverDelegationRoles]    UNIQUE          ([ReceiverRoleId], [ReceiverDelegationRoleId]),
    CONSTRAINT [PK_dbo.ReceiverDelegationRoles] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_dbo.ReceiverDelegationRoles_dbo.Roles_ReceiverRoleId]              FOREIGN KEY ([ReceiverRoleId]) REFERENCES [dbo].[Roles] ([Id]),
    CONSTRAINT [FK_dbo.ReceiverDelegationRoles_dbo.Roles_ReceiverDelegationRoleId]    FOREIGN KEY ([ReceiverDelegationRoleId]) REFERENCES [dbo].[Roles] ([Id])
);


GO