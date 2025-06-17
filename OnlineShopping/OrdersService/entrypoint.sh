#!/bin/bash

set -e

# Wait for the database to be ready
until pg_isready -h orders-db -p 5432 -U user -d ordersdb
do
  echo "Waiting for orders-db..."
  sleep 1
done

echo "orders-db is ready!"

# Wait for Kafka to be ready
until nc -z kafka 9092
do
  echo "Waiting for Kafka..."
  sleep 1
done

echo "Kafka is ready! Starting OrdersService..."

# Execute the main application
echo "ASPNETCORE_ENVIRONMENT: $ASPNETCORE_ENVIRONMENT"
echo "ASPNETCORE_URLS: $ASPNETCORE_URLS"
echo "ConnectionStrings__DefaultConnection: $ConnectionStrings__DefaultConnection"
echo "Kafka__BootstrapServers: $Kafka__BootstrapServers"
exec dotnet OrdersService.dll 