# .devcontainer/Dockerfile

FROM mcr.microsoft.com/devcontainers/dotnet:10.0-noble

ENV DEBIAN_FRONTEND=noninteractive
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
ENV DOTNET_NOLOGO=1

USER root

RUN apt-get update -y && \
    apt-get install -y --no-install-recommends \
        zsh \
        git \
        curl \
        sudo \
        ca-certificates \
        openssh-client \
        procps \
        unzip \
        make \
        nano \
        less \
        jq && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Install Starship prompt
RUN sh -c "$(curl -sS https://starship.rs/install.sh)" -y -f

# Create non-root development user
RUN groupadd -g 1001 dotnet_grp && \
    useradd -m -u 1001 -g dotnet_grp -s /bin/zsh dotnet_usr && \
    echo "dotnet_usr ALL=(ALL) NOPASSWD:ALL" > /etc/sudoers.d/dotnet_usr && \
    chmod 0440 /etc/sudoers.d/dotnet_usr

USER dotnet_usr

ENV PATH="$PATH:/home/dotnet_usr/.dotnet/tools"
ENV TMPDIR="/home/dotnet_usr/temp"

RUN mkdir -p "$TMPDIR" && \
    dotnet tool install --global dotnet-ef

COPY --chown=dotnet_usr:dotnet_grp .zshrc /home/dotnet_usr/.zshrc

WORKDIR /workspace

CMD ["tail", "-f", "/dev/null"]