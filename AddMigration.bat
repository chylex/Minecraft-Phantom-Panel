@echo off

if [%1]==[] (
    echo "Usage: AddMigration.bat <migration-name>"
    exit
)

dotnet ef migrations add %~1 --project Server/Phantom.Server.Database.Postgres
