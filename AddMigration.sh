if [ -z "$1" ]; then
    echo "Usage: AddMigration.sh <migration-name>"
    exit 1
fi

dotnet ef migrations add "$1" --project Server/Phantom.Server.Database.Postgres
