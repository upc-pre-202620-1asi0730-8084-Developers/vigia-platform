# Changelog

## [1.0.0] - 2026-11-14

- Publish the .NET API with Scalar OpenAPI documentation and authenticated role-based access.
- Persist projects, inventory, ordering, fleet, transit, receptions, discrepancies and activity history in MySQL.
- Apply versioned EF Core migrations through a separate deployment command, with safeguards for existing schemas.
- Deploy the API, MySQL and Caddy HTTPS on an Azure Ubuntu VM using Docker Compose and persistent volumes.
- Provision the initial production administrator without local demonstration records.
