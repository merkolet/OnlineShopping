#!/bin/bash

echo "Waiting for Kafka to be ready..."
while ! kafka-topics --bootstrap-server kafka:9092 --list; do
    echo "Kafka is not ready yet. Waiting..."
    sleep 5
done

echo "Creating Kafka topics..."

# Create order-payment-requests topic
kafka-topics --bootstrap-server kafka:9092 \
    --create \
    --topic order-payment-requests \
    --partitions 1 \
    --replication-factor 1 \
    --if-not-exists

# Create payments-events topic
kafka-topics --bootstrap-server kafka:9092 \
    --create \
    --topic payments-events \
    --partitions 1 \
    --replication-factor 1 \
    --if-not-exists

# Create payment-status-updates topic
kafka-topics --bootstrap-server kafka:9092 \
    --create \
    --topic payment-status-updates \
    --partitions 1 \
    --replication-factor 1 \
    --if-not-exists

echo "Kafka topics created successfully." 