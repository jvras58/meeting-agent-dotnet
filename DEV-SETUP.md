# Ambiente de Desenvolvimento

Este projeto pode ser executado em um container de desenvolvimento independente de editor.

Funciona com:

* VS Code;
* JetBrains;
* terminal puro;
* WSL;
* Docker Compose;
* qualquer editor que consiga trabalhar com arquivos locais.

A ideia principal é que o ambiente de desenvolvimento rode dentro de um container Linux com .NET instalado, enquanto serviços auxiliares como PostgreSQL, Redis, RabbitMQ e Ollama também sobem via Docker Compose.

---

## Estrutura esperada

```txt
.devcontainer/
├── Dockerfile
├── devcontainer.json
└── .zshrc

compose.dev.yml
Makefile
```

---

## Entendendo os dois contextos

Existem dois lugares onde você pode executar comandos:

```txt
1. Host / máquina local
   Exemplo: seu terminal no Windows, Linux, macOS ou WSL.

2. Dentro do container dotnet_dev
   Exemplo: terminal mostrando /workspace e .NET instalado.
```

Quando você está **fora do container**, os comandos do `Makefile` usam Docker Compose:

```bash
make build
```

Internamente isso executa algo como:

```bash
docker compose -f compose.dev.yml exec dotnet_dev dotnet build
```

Quando você está **dentro do container**, não é necessário chamar Docker. Basta rodar os comandos `.NET` diretamente:

```bash
dotnet build
```

Isso acontece porque o Docker fica instalado na máquina host, não necessariamente dentro do container.

---

## Subir ambiente

Execute no terminal da sua máquina local:

```bash
make up
```

Esse comando sobe os serviços de desenvolvimento:

* container .NET;
* PostgreSQL;
* Redis;
* RabbitMQ;
* Ollama.

---

## Entrar no container

Execute no host:

```bash
make shell
```

Dentro do container, o diretório do projeto estará montado em:

```bash
/workspace
```

Você provavelmente verá algo parecido com:

```bash
/workspace via .NET v10.0.100
```

Isso significa que você já está dentro do container de desenvolvimento.

---

## Restaurar dependências

### Fora do container

```bash
make restore
```

### Dentro do container

```bash
dotnet restore
```

---

## Rodar build

### Fora do container

```bash
make build
```

### Dentro do container

```bash
dotnet build
```

---

## Rodar testes

### Fora do container

```bash
make test
```

### Dentro do container

```bash
dotnet test
```

---

## Rodar API

### Fora do container

```bash
make api
```

### Dentro do container

```bash
dotnet run --project src/MeetingAgent.Api
```

---

## Rodar Worker

### Fora do container

```bash
make worker
```

### Dentro do container

```bash
dotnet run --project src/MeetingAgent.Worker
```

---

## Reiniciar ambiente

Execute no host:

```bash
make restart
```

Esse comando derruba e sobe novamente os containers.

---

## Derrubar ambiente

Execute no host:

```bash
make down
```

---

## Erro comum: `docker: Permission denied` ou `docker: command not found`

Se você rodar:

```bash
make build
```

e receber algo como:

```txt
make: docker: Permission denied
make: *** [Makefile:20: build] Error 127
```

ou:

```txt
zsh: command not found: docker
```

provavelmente você está tentando executar um comando do host **dentro do container**.

Exemplo de contexto dentro do container:

```bash
/workspace via .NET v10.0.100
```

Nesse caso, use os comandos `.NET` diretamente:

```bash
dotnet restore
dotnet build
dotnet test
```

Para rodar a API:

```bash
dotnet run --project src/MeetingAgent.Api
```

Para rodar o Worker:

```bash
dotnet run --project src/MeetingAgent.Worker
```

---

## Makefile recomendado

Para evitar confusão, o `Makefile` pode detectar automaticamente se está rodando dentro do container.

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
```

Com esse ajuste, os comandos abaixo funcionam tanto no host quanto dentro do container:

```bash
make restore
make build
make test
make api
make worker
```

Apenas estes comandos precisam ser executados no host, porque dependem diretamente do Docker Compose:

```bash
make up
make down
make restart
make shell
```

---

## Fluxo recomendado

### Primeira vez

No host:

```bash
make up
make shell
```

Dentro do container:

```bash
dotnet restore
dotnet build
dotnet test
```

Para iniciar a API:

```bash
dotnet run --project src/MeetingAgent.Api
```

---

## Fluxo usando Makefile inteligente

No host:

```bash
make up
make shell
```

Dentro do container:

```bash
make restore
make build
make api
```

Em outro terminal, para rodar o Worker:

```bash
make shell
make worker
```

---

## Usando sem Makefile

Caso não queira usar `make`, os comandos diretos no host são:

```bash
docker compose -f compose.dev.yml up -d
```

```bash
docker compose -f compose.dev.yml exec dotnet_dev zsh
```

```bash
docker compose -f compose.dev.yml exec dotnet_dev dotnet restore
```

```bash
docker compose -f compose.dev.yml exec dotnet_dev dotnet build
```

```bash
docker compose -f compose.dev.yml exec dotnet_dev dotnet test
```

```bash
docker compose -f compose.dev.yml exec dotnet_dev dotnet run --project src/MeetingAgent.Api
```

```bash
docker compose -f compose.dev.yml exec dotnet_dev dotnet run --project src/MeetingAgent.Worker
```

---

## Observação sobre Dev Container

O arquivo `.devcontainer/devcontainer.json` existe apenas como conveniência para quem quiser abrir o projeto usando Dev Containers.

O fluxo principal do projeto é via Docker Compose e Makefile:

```bash
make up
make shell
```

Assim, o ambiente não fica preso ao VS Code.
