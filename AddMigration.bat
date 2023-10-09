@echo off

if [%1]==[] (
    echo "Usage: AddMigration.bat <migration-name>"
    exit
)

dotnet ef migrations add %~1 --project Controller/Phantom.Controller.Database.Postgres --msbuildprojectextensionspath .artifacts/obj/Phantom.Controller.Database.Postgres
