
# CoffeeNChill-CLDV6212

## Standalone Container Execution

### 1. Azurite Storage Emulator
To run the Azurite storage emulator in an isolated container binding the default storage ports, use:
`docker run -p 10000:10000 -p 10001:10001 -p 10002:10002 mcr.microsoft.com/azure-storage/azurite`

### 2. Azure Functions Containerization 
To pull and run the published .NET 10 Isolated container independently, use:
`docker run -p 7170:80 -e AzureWebJobsStorage="UseDevelopmentStorage=true" yashkeshav/coffeenchill-functions:v1.0`


