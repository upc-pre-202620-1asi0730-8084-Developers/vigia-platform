# vigia-platform

## Database migrations

Requires .NET 10 and MySQL. Store `ConnectionStrings:DefaultConnection` in user-secrets for `VigiaPlatform.API`; do not commit database credentials.

For the first migration, use an empty database. Existing databases created with `EnsureCreated()` must be preserved or backed up and transitioned separately; do not apply `InitialCreate` over their existing tables.

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet ef database update --project VigiaPlatform.API
```

Development startup also applies pending migrations and seeds reception sample data. Production startup does not apply migrations or seed sample data. Apply migrations separately during deployment, before starting the new API version. Generate a SQL script for review with:

```powershell
dotnet ef migrations script --project VigiaPlatform.API --output migration.sql
```

Commit each migration with its designer file and `AppDbContextModelSnapshot`. Coordinate schema changes through one migration branch at a time, based on the current `develop` model. Do not use `EnsureCreated()` with migrations.
