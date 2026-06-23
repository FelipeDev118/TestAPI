# ===== ESTÁGIO 1: COMPILAÇÃO (BUILD) =====
FROM mcr.microsoft.com/dotnet/sdk:3.1 AS build-env
WORKDIR /app

# Copia o arquivo de projeto e restaura as dependências
COPY *.csproj ./
RUN dotnet restore

# Copia o restante dos arquivos e compila a aplicação em modo Release
COPY . ./
RUN dotnet publish -c Release -o out

# ===== CORREÇÃO DO 404: CÓPIA GARANTIDA DO FRONTEND =====
# Garante que a pasta wwwroot seja fisicamente injetada na pasta de publicação final do container
RUN cp -r wwwroot ./out/wwwroot

# ===== ESTÁGIO 2: EXECUÇÃO (RUNTIME) =====
FROM mcr.microsoft.com/dotnet/aspnet:3.1
WORKDIR /app

# Copia os arquivos compilados do primeiro estágio (incluindo agora a wwwroot)
COPY --from=build-env /app/out .

# Expõe a porta que o ASP.NET vai escutar de dentro do contêiner
EXPOSE 5000
ENV ASPNETCORE_URLS=http://*:5000

# Comando para iniciar a aplicação
ENTRYPOINT ["dotnet", "TestAPI.dll"]