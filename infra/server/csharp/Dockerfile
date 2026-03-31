FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ./service-api/service-csharp/Praxis.sln ./service-api/service-csharp/
COPY ./service-api/service-csharp/Directory.Build.props ./service-api/service-csharp/
COPY ./service-api/service-csharp/src/Praxis.Api/Praxis.Api.csproj ./service-api/service-csharp/src/Praxis.Api/
COPY ./service-api/service-csharp/src/Praxis.Application/Praxis.Application.csproj ./service-api/service-csharp/src/Praxis.Application/
COPY ./service-api/service-csharp/src/Praxis.Domain/Praxis.Domain.csproj ./service-api/service-csharp/src/Praxis.Domain/
COPY ./service-api/service-csharp/src/Praxis.Infrastructure/Praxis.Infrastructure.csproj ./service-api/service-csharp/src/Praxis.Infrastructure/
COPY ./service-api/service-csharp/src/Praxis.Worker/Praxis.Worker.csproj ./service-api/service-csharp/src/Praxis.Worker/
COPY ./service-api/service-csharp/tests/Praxis.Domain.Tests/Praxis.Domain.Tests.csproj ./service-api/service-csharp/tests/Praxis.Domain.Tests/
COPY ./service-api/service-csharp/tests/Praxis.IntegrationTests/Praxis.IntegrationTests.csproj ./service-api/service-csharp/tests/Praxis.IntegrationTests/

WORKDIR /src/service-api/service-csharp
RUN dotnet restore ./Praxis.sln

WORKDIR /src
COPY ./service-api/service-csharp/. ./service-api/service-csharp/

WORKDIR /src/service-api/service-csharp
RUN dotnet publish ./src/Praxis.Api/Praxis.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Praxis.Api.dll"]
