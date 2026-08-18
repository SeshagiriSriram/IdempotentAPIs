@echo off
docker compose down 
docker volume rm baseidempotencycode_mssql_data
docker volume rm baseidempotencycode_rabbitmq_data
docker volume rm baseidempotencycode_redis_node1_data
docker volume rm baseidempotencycode_redis_node2_data
docker volume rm baseidempotencycode_redis_node3_data