# Migrations

The initial migration is committed in this directory. The server applies pending migrations automatically at startup after verifying database connectivity. For an explicit maintenance-window update, run:

```powershell
dotnet ef database update --project src/TotalMonitor.Infrastructure --startup-project src/TotalMonitor.Server

This operation is incremental and does not delete existing users or passwords.
```
