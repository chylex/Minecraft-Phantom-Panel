if [ -z "$1" ]; then
    echo "Usage: AddMigration.sh <migration-name>"
    exit 1
fi

dotnet ef migrations add "$1" --project Controller/Phantom.Controller.Database.Postgres --msbuildprojectextensionspath .artifacts/obj/Phantom.Controller.Database.Postgres
