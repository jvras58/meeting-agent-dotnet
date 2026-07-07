COMPOSE_DEV=docker compose -f compose.dev.yml
API_URL ?= http://0.0.0.0:5080
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
	@if [ -f /.dockerenv ]; then \
		echo "Dentro do devcontainer não usamos docker logs. Testando Ollama via HTTP em $(OLLAMA_BASE_URL)..."; \
		curl -fsS $(OLLAMA_BASE_URL)/api/tags; \
	else \
		$(COMPOSE_DEV) logs -f ollama; \
	fi

logs-rabbitmq:
	@if [ -f /.dockerenv ]; then \
		echo "Dentro do devcontainer, acesse RabbitMQ Management em http://localhost:15672 ou use curl -u guest:guest http://rabbitmq:15672/api/overview"; \
	else \
		$(COMPOSE_DEV) logs -f rabbitmq; \
	fi

logs-postgres:
	@if [ -f /.dockerenv ]; then \
		echo "Dentro do devcontainer, teste o banco via DNS: getent hosts postgres"; \
	else \
		$(COMPOSE_DEV) logs -f postgres; \
	fi

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
		dotnet run --project src/MeetingAgent.Api --no-launch-profile --urls $(API_URL); \
	else \
		$(COMPOSE_DEV) exec dotnet_dev dotnet run --project src/MeetingAgent.Api --no-launch-profile --urls $(API_URL); \
	fi

api-watch:
	@if [ -f /.dockerenv ]; then \
		dotnet watch --project src/MeetingAgent.Api run --no-launch-profile --urls $(API_URL); \
	else \
		$(COMPOSE_DEV) exec dotnet_dev dotnet watch --project src/MeetingAgent.Api run --no-launch-profile --urls $(API_URL); \
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

ollama-smoke:
	@curl -fsS $(OLLAMA_BASE_URL)/api/chat \
		-H "Content-Type: application/json" \
		-d '{"model":"$(OLLAMA_MODEL)","stream":false,"messages":[{"role":"user","content":"Responda apenas: ok"}]}'
