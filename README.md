# CoffeeNChill Canteen Management System - Part 1

## Overview
Cloud-enabled microservices system built for the CoffeeNChill campus canteen. Part 1 establishes the cloud foundation using Azure Storage (Tables and File Shares) and HTTP-triggered Azure Functions running locally via Azurite emulation inside Docker containers.

---

## Local Setup Instructions
1. Clone the repository to your local machine.
2. Ensure you have Docker Desktop running.
3. Configure your `local.settings.json` file to point to Azurite storage:
   ```json
   {
     "IsEncrypted": false,
     "Values": {
       "AzureWebJobsStorage": "UseDevelopmentStorage=true",
       "FUNCTIONS_WORKER_RUNTIME": "dotnet-worker"
     }
   }
