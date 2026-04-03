INSERT INTO [AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp])
VALUES 
('role-guid-admin', 'Admin', 'ADMIN', NEWID()),
('role-guid-moderator', 'Moderator', 'MODERATOR', NEWID()),
('role-guid-user', 'User', 'USER', NEWID());