#!/bin/bash

sudo mkdir /var/opt/mssql
if [ $? -ne 0 ]; then
  echo "Failed to create directory /var/opt/mssql"
  exit 1
fi

sudo docker run \
--restart unless-stopped \
-e "ACCEPT_EULA=Y" \
-e 'MSSQL_PID=Express' \
-e "SA_PASSWORD=Work@996" \
-p 1433:1433 \
--name sqlexpress \
-h sqlexpress \
-v /var/opt/mssql/data:/var/opt/mssql/data \
-v /var/opt/mssql/log:/var/opt/mssql/log \
-v /var/opt/mssql/secrets:/var/opt/mssql/secrets \
-d mcr.microsoft.com/mssql/server:2022-latest

if [ $? -ne 0 ]; then
  echo "Failed to run Docker container"
  exit 1
fi

sleep 2

sudo chmod 777 -R /var/opt/mssql
if [ $? -ne 0 ]; then
  echo "Failed to change permissions to 777 for /var/opt/mssql"
  exit 1
fi

echo "SQL Server Express installed successfully"
