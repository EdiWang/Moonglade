#!/bin/bash

echo "Configuring Docker network..."

docker network inspect moongladenetwork >/dev/null 2>&1 || docker network create --subnet=172.20.0.0/16 moongladenetwork

CONTAINER_NAME="sqlexpress"
NETWORK_NAME="moongladenetwork"

if docker network inspect "$NETWORK_NAME" | grep -q "\"Name\": \"$CONTAINER_NAME\""; then
  echo "Container $CONTAINER_NAME is already connected to $NETWORK_NAME"
else
  echo "Container $CONTAINER_NAME is not connected to $NETWORK_NAME, connecting..."
  docker network connect "$NETWORK_NAME" "$CONTAINER_NAME"
  
  if [ $? -eq 0 ]; then
    echo "Container $CONTAINER_NAME connected to $NETWORK_NAME"
  else
    echo "Error connecting container $CONTAINER_NAME to $NETWORK_NAME"
  fi
fi

echo "Creating database..."
container_id=$(docker run -d --network moongladenetwork mcr.microsoft.com/mssql-tools sleep infinity)
sleep 5

docker exec -it $container_id /opt/mssql-tools/bin/sqlcmd -S sqlexpress -U sa -P Work@996 -Q "IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'moonglade') CREATE DATABASE [moonglade]"

docker stop $container_id
docker rm $container_id

# Check if the database was created successfully
if [ $? -eq 0 ]; then
    echo "Database created successfully."
else
    echo "Failed to create database."
fi
