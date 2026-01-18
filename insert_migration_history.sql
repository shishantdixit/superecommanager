-- Insert migration history record for Shared/ApplicationDbContext
INSERT INTO shared."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260118150354_InitialCreate', '9.0.12')
ON CONFLICT ("MigrationId") DO NOTHING;

-- Insert migration history record for Tenant/TenantDbContext (in public schema)
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260118150318_InitialCreate', '9.0.12')
ON CONFLICT ("MigrationId") DO NOTHING;
