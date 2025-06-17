#!/bin/bash

set -e

# Wait for the database to be ready
until pg_isready -h payments-db -p 5432 -U postgres
do
  echo "Waiting for payments-db..."
  sleep 1
done

echo "payments-db is ready! Starting PaymentsService..."

# Execute the main application
exec dotnet PaymentsService.dll 