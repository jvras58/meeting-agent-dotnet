COMPOSE_DEV=docker compose -f compose.dev.yml
OLLAMA_MODEL ?= qwen3:8b
OLLAMA_BASE_URL ?= http://ollama:11434

up:
	$(COMPOSE_DEV) up -d

down:
	$(COMPOSE_DEV) down

restart:
	$(COMPOSE_DEV) down
	$(COMPOSE_DEV) up -d

ps:
	$(COMPOSE_DEV) ps

logs:
	$(COMPOSE_DEV) logs -f

logs-api:
	@echo "A API normalmente roda via dotnet run/dotnet watch dentro do container; veja o terminal onde ela foi iniciada."

logs-worker:
	@echo "O Worker normalmente roda via dotnet run/dotnet watch dentro do container; veja o terminal onde ele foi iniciado."

logs-ollama:
	$(COMPOSE_DEV) logs -f ollama

logs-rabbitmq:
	$(COMPOSE_DEV) logs -f rabbitmq

logs-postgres:
	$(COMPOSE_DEV) logs -f postgres

shell:
	$(COMPOSE_DEV) exec dotnet_dev zsh

restore:
	@if [ -f /.dockerenv ]; then \
		dotnet restore; \
	else \
		$(COMPOSE_DEV) exec dotnet_dev dotnet restore; \
	fi

build:
	@if [ -f /.dockerenv ]; then \
		dotnet build; \
	else \
		$(COMPOSE_DEV) exec dotnet_dev dotnet build; \
	fi

test:
	@if [ -f /.dockerenv ]; then \
		dotnet test; \
	else \
		$(COMPOSE_DEV) exec dotnet_dev dotnet test; \
	fi

api:
	@if [ -f /.dockerenv ]; then \
		dotnet run --project src/MeetingAgent.Api; \
	else \
		$(COMPOSE_DEV) exec dotnet_dev dotnet run --project src/MeetingAgent.Api; \
	fi

api-watch:
	@if [ -f /.dockerenv ]; then \
		dotnet watch --project src/MeetingAgent.Api run; \
	else \
		$(COMPOSE_DEV) exec dotnet_dev dotnet watch --project src/MeetingAgent.Api run; \
	fi

worker:
	@if [ -f /.dockerenv ]; then \
		dotnet run --project src/MeetingAgent.Worker; \
	else \
		$(COMPOSE_DEV) exec dotnet_dev dotnet run --project src/MeetingAgent.Worker; \
	fi

worker-watch:
	@if [ -f /.dockerenv ]; then \
		dotnet watch --project src/MeetingAgent.Worker run; \
	else \
		$(COMPOSE_DEV) exec dotnet_dev dotnet watch --project src/MeetingAgent.Worker run; \
	fi

ollama-model:
	@if [ -f /.dockerenv ]; then \
		curl -fsS $(OLLAMA_BASE_URL)/api/pull \
			-H "Content-Type: application/json" \
			-d '{"name":"$(OLLAMA_MODEL)","stream":false}'; \
	else \
		$(COMPOSE_DEV) exec ollama ollama pull $(OLLAMA_MODEL); \
	fi

ollama-list:
	@if [ -f /.dockerenv ]; then \
		curl -fsS $(OLLAMA_BASE_URL)/api/tags; \
	else \
		$(COMPOSE_DEV) exec ollama ollama list; \
	fi
