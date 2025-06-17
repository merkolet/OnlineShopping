FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["OrdersService/OrdersService.csproj", "OrdersService/"]
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore "OrdersService/OrdersService.csproj"
COPY . .
WORKDIR "/src/OrdersService"
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet build "OrdersService.csproj" -c Release -o /app/build

FROM build AS publish
WORKDIR /src/OrdersService
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish "OrdersService.csproj" -c Release -o /app/out

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/out .
COPY OrdersService/entrypoint.sh .
RUN chmod +x ./entrypoint.sh && \
    apt-get update && \
    apt-get install -y curl postgresql-client netcat-traditional && \
    rm -rf /var/lib/apt/lists/*
ENTRYPOINT ["/bin/bash", "./entrypoint.sh"] 