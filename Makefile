.PHONY: build test run docker-up docker-down ef-migration ef-update clean

build:
	dotnet build TaskForge.sln

test:
	dotnet test TaskForge.sln --no-build --verbosity normal

run:
	dotnet run --project src/TaskForge.Api/TaskForge.Api.csproj

docker-up:
	docker-compose -f docker/docker-compose.yml up -d

docker-down:
	docker-compose -f docker/docker-compose.yml down

ef-migration:
	@read -p "Enter migration name: " name; \
	dotnet ef migrations add $$name --project src/TaskForge.Infrastructure --startup-project src/TaskForge.Api --output-dir Persistence/Migrations

ef-update:
	dotnet ef database update --project src/TaskForge.Infrastructure --startup-project src/TaskForge.Api

clean:
	dotnet clean TaskForge.sln
