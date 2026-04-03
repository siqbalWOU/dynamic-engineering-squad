-- Drop child/dependent tables first
DROP TABLE IF EXISTS [UserPoints];
DROP TABLE IF EXISTS [Reports];
DROP TABLE IF EXISTS [AspNetUserRoles];
DROP TABLE IF EXISTS [AspNetUserClaims];

-- Drop parent/identity tables last
DROP TABLE IF EXISTS [AspNetUsers];
DROP TABLE IF EXISTS [AspNetUserRoles];
DROP TABLE IF EXISTS [AspNetRoleClaims];
DROP TABLE IF EXISTS [AspNetRoles];
