# 1. Etapa de Build (Usa o SDK do .NET 10 para compilar o código)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copia os arquivos de projeto e restaura as dependências
COPY ["src/ProspeccaoLeads.Web/ProspeccaoLeads.Web.csproj", "src/ProspeccaoLeads.Web/"]
COPY ["src/ProspeccaoLeads.Application/ProspeccaoLeads.Application.csproj", "src/ProspeccaoLeads.Application/"]
COPY ["src/ProspeccaoLeads.Domain/ProspeccaoLeads.Domain.csproj", "src/ProspeccaoLeads.Domain/"]
COPY ["src/ProspeccaoLeads.Infrastructure/ProspeccaoLeads.Infrastructure.csproj", "src/ProspeccaoLeads.Infrastructure/"]
RUN dotnet restore "src/ProspeccaoLeads.Web/ProspeccaoLeads.Web.csproj"

# Copia o restante do código e gera os arquivos finais (Publish)
COPY . .
WORKDIR "/src/src/ProspeccaoLeads.Web"
RUN dotnet publish "ProspeccaoLeads.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 2. Etapa de Execução (Usa apenas o Runtime para rodar, deixando a imagem leve)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# O Render e provedores em nuvem utilizam a porta 8080
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Comando para iniciar a aplicação
ENTRYPOINT ["dotnet", "ProspeccaoLeads.Web.dll"]
