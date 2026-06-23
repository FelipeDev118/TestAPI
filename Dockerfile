# ===== ESTÁGIO 1: COMPILAÇÃO (BUILD) =====
FROM mcr.microsoft.com/dotnet/sdk:3.1 AS build-env
WORKDIR /app

# Copia o arquivo de projeto e restaura as dependências do NuGet
COPY *.csproj ./
RUN dotnet restore

# Copia o restante dos arquivos do projeto
COPY . ./

# Compila a aplicação e publica os binários direto na pasta 'out'
RUN dotnet publish -c Release -o out

# ===== ESTÁGIO 2: EXECUÇÃO (RUNTIME) =====
FROM mcr.microsoft.com/dotnet/aspnet:3.1
WORKDIR /app

# Copia os arquivos compilados e a estrutura do estágio anterior
COPY --from=build-env /app/out .

# Garante de forma nativa a cópia limpa do Front-end (wwwroot) para a execução
COPY --from=build-env /app/wwwroot ./wwwroot

# Expõe a porta interna do container para a Render fazer o roteamento
EXPOSE 5000
ENV ASPNETCORE_URLS=http://*:5000

# ===== AJUSTE DA DLL ALVO =====
# Inicializa o servidor usando a DLL real gerada pelo VerificadorApi.csproj
ENTRYPOINT ["dotnet", "VerificadorApi.dll"]