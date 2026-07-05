Copy-Item .env.example .env -ErrorAction SilentlyContinue
docker compose up -d
dotnet restore
dotnet build
