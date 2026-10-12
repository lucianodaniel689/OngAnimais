# =============================================================
# Dockerfile da API OngAnimais (.NET 10)
# Build multi-stage: SDK para compilar/publicar, runtime enxuto para executar.
# Contexto de build esperado: a pasta do projeto (OngAnimais/).
# =============================================================

# --- Estágio 1: build/publish ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restaura dependências primeiro (melhor uso do cache de camadas).
# -r linux-x64: a imagem só roda em Linux x64 — assim o publish leva só a libvips nativa de
# Linux (NetVips.Native.linux-x64), sem os ~19 MB da versão Windows usada no desenvolvimento.
COPY OngAnimais.csproj .
RUN dotnet restore OngAnimais.csproj -r linux-x64

# Copia o restante do código e publica em Release (framework-dependent: o runtime vem da imagem).
COPY . .
RUN dotnet publish OngAnimais.csproj -c Release -r linux-x64 --self-contained false --no-restore \
    -o /app/publish /p:UseAppHost=false

# --- Estágio 2: runtime ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Kestrel escuta na 8080 dentro do container (padrão das imagens .NET 8+).
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "OngAnimais.dll"]
