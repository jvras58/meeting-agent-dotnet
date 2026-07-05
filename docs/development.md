# Ambiente de Desenvolvimento

Este documento explica como configurar e usar o ambiente de desenvolvimento do **Meeting Agent**.

O projeto foi pensado para funcionar sem depender de uma IDE específica.
Você pode usar VS Code, JetBrains, terminal puro, WSL, Linux, macOS ou qualquer editor.

A entrada principal do ambiente é via:

```bash
make up
make shell
```

---

## 1. Visão geral

O ambiente de desenvolvimento usa Docker Compose para subir:

* container .NET de desenvolvimento;
* PostgreSQL;
* Redis;
* RabbitMQ;
* Ollama;
* Aspire Dashboard, quando configurado.

A aplicação principal roda dentro do container `dotnet_dev`.

```txt
Host / máquina local
   ↓
Docker Compose
   ↓
dotnet_dev
   ↓
/workspace
```

---

## 2. Estrutura esperada

```txt
meeting-agent/
├── .devcontainer/
│   ├── Dockerfile
│   ├── devcontainer.json
│   └── .zshrc
│
├── compose.dev.yml
├── docker-compose.yml
├── Makefile
├── .env.example
├── .env
├── README.md
└── docs/
    └── development.md
```

---

## 3. Pré-requisitos

Para o fluxo recomendado:

```txt
Docker
Docker Compose
Make
```

Você não precisa instalar o .NET SDK na sua máquina local se for usar o container de desenvolvimento.

O SDK .NET fica dentro do container `dotnet_dev`.

---

## 4. Configuração inicial

Copie o arquivo de variáveis de ambiente:

```bash
cp .env.example .env
```

No PowerShell:

```powershell
Copy-Item .env.example .env
```

Depois ajuste os valores necessários no `.env`.

Para desenvolvimento local sem Microsoft Graph configurado, a aplicação pode usar os adapters heurísticos/em memória.

---

## 5. Subir o ambiente

No terminal da sua máquina local, execute:

```bash
make up
```

Esse comando sobe os containers definidos no `compose.dev.yml`.

Sem Makefile:

```bash
docker compose -f compose.dev.yml up -d
```

---

## 6. Entrar no container

No host, execute:

```bash
make shell
```

Sem Makefile:

```bash
docker compose -f compose.dev.yml exec dotnet_dev zsh
```

Dentro do container, o projeto estará disponível em:

```bash
/workspace
```

Você provavelmente verá algo parecido com:

```bash
/workspace via .NET v10.0.100
```

Isso significa que você está dentro do container de desenvolvimento.

---

## 7. Entendendo host vs container

Existem dois contextos diferentes:

```txt
Host / máquina local
   Onde Docker está instalado.

Container dotnet_dev
   Onde o .NET SDK está instalado.
```

Quando você está no **host**, pode executar comandos que usam Docker Compose:

```bash
make up
make shell
make down
make restart
```

Quando você está **dentro do container**, não precisa chamar Docker.
Use os comandos .NET diretamente:

```bash
dotnet restore
dotnet build
dotnet test
dotnet run --project src/MeetingAgent.Api
```

---

## 8. Erro comum: Docker dentro do container

Se você estiver dentro do container e rodar:

```bash
make build
```

pode receber:

```txt
make: docker: Permission denied
make: *** [Makefile:20: build] Error 127
```

ou:

```txt
zsh: command not found: docker
```

Isso acontece porque o Makefile antigo chamava:

```bash
docker compose -f compose.dev.yml exec dotnet_dev dotnet build
```

Mas dentro do container não existe Docker instalado.

Nesse caso, use diretamente:

```bash
dotnet build
```

Ou atualize o Makefile para detectar automaticamente quando está rodando dentro do container.

---

## 9. Makefile recomendado

Use este Makefile para permitir que os comandos funcionem tanto no host quanto dentro do container.

```makefile
COMPOSE_DEV=docker compose -f compose.dev.yml

up:
	$(COMPOSE_DEV) up -d

down:
	$(COMPOSE_DEV) down

restart:
	$(COMPOSE_DEV) down
	$(COMPOSE_DEV) up -d

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

worker:
	@if [ -f /.dockerenv ]; then \
		dotnet run --project src/MeetingAgent.Worker; \
	else \
		$(COMPOSE_DEV) exec dotnet_dev dotnet run --project src/MeetingAgent.Worker; \
	fi

ollama-model:
	$(COMPOSE_DEV) exec ollama ollama pull qwen3:8b
```

Com esse Makefile, estes comandos funcionam no host e dentro do container:

```bash
make restore
make build
make test
make api
make worker
```

Estes comandos devem ser executados no host:

```bash
make up
make down
make restart
make shell
```

---

## 10. Restaurar dependências

Dentro do container:

```bash
dotnet restore
```

Ou com Makefile inteligente:

```bash
make restore
```

---

## 11. Build

Dentro do container:

```bash
dotnet build
```

Ou:

```bash
make build
```

---

## 12. Rodar testes

Dentro do container:

```bash
dotnet test
```

Ou:

```bash
make test
```

---

## 13. Rodar API

Dentro do container:

```bash
dotnet run --project src/MeetingAgent.Api
```

Ou:

```bash
make api
```

A API ficará disponível em:

```txt
http://localhost:5080
```

Swagger/OpenAPI:

```txt
http://localhost:5080/swagger
```

---

## 14. Rodar Worker

Abra outro terminal no host e entre novamente no container:

```bash
make shell
```

Dentro do container:

```bash
dotnet run --project src/MeetingAgent.Worker
```

Ou:

```bash
make worker
```

---

## 15. Rodar API e Worker em paralelo

Terminal 1:

```bash
make shell
dotnet run --project src/MeetingAgent.Api
```

Terminal 2:

```bash
make shell
dotnet run --project src/MeetingAgent.Worker
```

Ou, com Makefile inteligente:

Terminal 1:

```bash
make shell
make api
```

Terminal 2:

```bash
make shell
make worker
```

---

## 16. Derrubar ambiente

No host:

```bash
make down
```

Sem Makefile:

```bash
docker compose -f compose.dev.yml down
```

---

## 17. Reiniciar ambiente

No host:

```bash
make restart
```

Sem Makefile:

```bash
docker compose -f compose.dev.yml down
docker compose -f compose.dev.yml up -d
```

---

## 18. Acessos locais

| Serviço             | URL                           |
| ------------------- | ----------------------------- |
| API                 | http://localhost:5080         |
| Swagger/OpenAPI     | http://localhost:5080/swagger |
| RabbitMQ Management | http://localhost:15672        |
| Ollama              | http://localhost:11434        |
| Aspire Dashboard    | http://localhost:18888        |

---

## 19. Teste rápido da API sem Microsoft Graph

A aplicação possui um endpoint de importação manual de transcript.

Com a API rodando, execute no host ou dentro do container:

```bash
curl -X POST http://localhost:5080/meetings/import \
  -H "Content-Type: application/json" \
  -d @samples/sample-import-request.json
```

Depois liste as reuniões:

```bash
curl http://localhost:5080/meetings
```

Consulte o resumo:

```bash
curl http://localhost:5080/meetings/{meetingId}/summary
```

No PowerShell:

```powershell
Invoke-RestMethod -Method POST `
  -Uri "http://localhost:5080/meetings/import" `
  -ContentType "application/json" `
  -InFile ".\samples\sample-import-request.json"
```

---

## 20. Ollama

Se estiver usando Ollama como provider local, baixe o modelo definido no projeto:

```bash
make ollama-model
```

Ou diretamente:

```bash
docker compose -f compose.dev.yml exec ollama ollama pull qwen3:8b
```

Para listar modelos:

```bash
docker compose -f compose.dev.yml exec ollama ollama list
```

---

## 21. Usando com Dev Container

O arquivo `.devcontainer/devcontainer.json` existe apenas como conveniência.

Ele pode ser usado por ferramentas compatíveis com Dev Containers, mas o projeto não depende disso.

O fluxo principal continua sendo:

```bash
make up
make shell
```

---

## 22. Usando com JetBrains

No JetBrains Rider ou outra IDE da JetBrains, você pode:

1. subir o ambiente com `make up`;
2. abrir o projeto localmente;
3. usar o terminal integrado para `make shell`;
4. rodar `dotnet build`, `dotnet test`, API e Worker dentro do container.

O código continua editável no host porque a pasta local é montada no container em `/workspace`.

---

## 23. Usando com terminal puro

Fluxo recomendado:

```bash
make up
make shell
dotnet restore
dotnet build
dotnet test
dotnet run --project src/MeetingAgent.Api
```

Em outro terminal:

```bash
make shell
dotnet run --project src/MeetingAgent.Worker
```

---

## 24. Troubleshooting

### `docker: command not found`

Você provavelmente está dentro do container.
Use `dotnet build` em vez de `make build`, ou use o Makefile inteligente.

---

### `make: docker: Permission denied`

Você provavelmente está dentro do container tentando chamar Docker.
Use:

```bash
dotnet build
```

---

### `dotnet: command not found`

Você provavelmente está no host sem .NET instalado.

Entre no container:

```bash
make shell
```

---

### Porta da API não abre

Verifique se a API está rodando:

```bash
dotnet run --project src/MeetingAgent.Api
```

Verifique se a porta está mapeada no `compose.dev.yml`.

A porta esperada é:

```txt
5080
```

---

### RabbitMQ Management não abre

Verifique se o container está rodando:

```bash
docker compose -f compose.dev.yml ps
```

Acesse:

```txt
http://localhost:15672
```

Credenciais padrão, se não alteradas:

```txt
guest / guest
```

---

### Ollama não responde

Verifique se o container está ativo:

```bash
docker compose -f compose.dev.yml ps ollama
```

Teste:

```bash
curl http://localhost:11434/api/tags
```

---

## 25. Fluxo recomendado para desenvolvimento diário

No início do dia:

```bash
make up
make shell
```

Dentro do container:

```bash
git status
dotnet restore
dotnet build
dotnet test
```

Para desenvolver API:

```bash
dotnet run --project src/MeetingAgent.Api
```

Para desenvolver Worker:

```bash
dotnet run --project src/MeetingAgent.Worker
```

No fim:

```bash
exit
make down
```

---

## 26. Decisão do projeto

O ambiente de desenvolvimento deve ser:

```txt
Reprodutível
Independente de editor
Baseado em Docker Compose
Com .NET isolado no container
Sem exigir instalação local do SDK
Compatível com VS Code, JetBrains e terminal puro
```

Por isso o fluxo principal é:

```bash
make up
make shell
```

E não:

```bash
dotnet build direto no host
```

A execução direta no host continua possível, mas é considerada alternativa.
