FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["PaymentsService/PaymentsService.csproj", "PaymentsService/"]
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore "PaymentsService/PaymentsService.csproj"
COPY . .
WORKDIR "/src/PaymentsService"
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet build "PaymentsService.csproj" -c Release -o /app/build

FROM build AS publish
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish "PaymentsService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY PaymentsService/entrypoint.sh .
RUN chmod +x ./entrypoint.sh && \
    apt-get update && \
    apt-get install -y curl postgresql-client && \
    rm -rf /var/lib/apt/lists/*
ENTRYPOINT ["/bin/bash", "./entrypoint.sh"]
