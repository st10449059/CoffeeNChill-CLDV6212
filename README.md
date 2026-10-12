# CoffeeNChill — POE Part 2

**CLDV6212**
- Ayden Jordaan — ST10466857
- Yash Keshav — ST10449059

## Part 2 Demonstration

YouTube: INSERT THE NEW PART 2 VIDEO LINK HERE

## Part 2 Overview

Part 2 adds asynchronous order processing to CoffeeNChill.
An HTTP producer validates and queues orders. A queue-triggered
Azure Function stores each order and advances its status through
Received → Preparing → Ready → Collected.

The application runs through Docker Compose using the published
image `yashkeshav/coffeenchill-functions:v2.0` and Microsoft Azurite.
Azurite provides local Queue and Table storage. Staff documents
continue to use a real Azure Files share.

## Order Processing

- `POST /api/orders/queue` validates the order and returns 202
  after successfully enqueueing it.
- Messages are serialized as JSON and encoded using Base64.
- The producer generates the order timestamp in UTC.
- Invalid payloads return 400; oversized messages return 413.
- Azure storage request failures are handled with a 503 response.
- `ProcessOrderQueue` consumes `order-processing-queue`.
- The `Orders` table uses the UTC order date as PartitionKey
  and OrderId as RowKey.
- Status changes and lifecycle timestamps are stored and logged.
- Failed processing is logged and retried. After five unsuccessful
  dequeue attempts, messages are moved to the poison queue.
- `GET /api/orders/{orderDate}/{orderId}` retrieves order status.

## Part 2 Changelog

- Added validated order submission and Base64 queue messages.
- Added server-generated timestamps and queue-size protection.
- Added queue-triggered processing and persistent order tracking.
- Added lifecycle transitions, retry handling, and poison handling.
- Added the order-status endpoint.
- Added Docker Compose with a published v2.0 application image,
  custom bridge network, and persistent Azurite volume.
- Improved incomplete and malformed menu-update validation.
- Expanded Postman regression tests and added a PDF test fixture.
- Verified published-container tests and persistence after
  container recreation.

## Part 2 Setup

### Prerequisites

- Docker Desktop running Linux containers.
- Git, Postman, and Azure Storage Explorer.
- An Azure Storage account supporting Azure Files.
- Available ports: 7170, 10000, 10001, and 10002.

Run commands from the Part 2 folder containing
`docker-compose.yml` and `CoffeeNChill.Functions`.
Stop Visual Studio debugging and any competing standalone
Azurite instance before starting the Docker stack.

### Configure Azure Files

In PowerShell, enter the real Azure Files connection string
privately when prompted:

```powershell
$secret = Read-Host "Azure Files connection string" -AsSecureString
$env:AZURE_FILES_CONNECTION_STRING = [System.Net.NetworkCredential]::new("", $secret).Password
Remove-Variable secret
```

Do not commit or display the connection string.
Keep this terminal open; repeat configuration in a new session.

### Prepare Storage and Start

Create the external volume before the first startup.
An existing volume with this name is reused:

```powershell
docker volume create coffeenchill-part2-data
```

Start the application with one command:

```powershell
docker compose up -d --pull always
```

Check startup:

```powershell
docker compose ps
```

Wait for Azurite to become healthy and Functions to be Up.
The API is available at `http://localhost:7170`.

Compose pulls `yashkeshav/coffeenchill-functions:v2.0`,
uses a custom bridge network, and mounts persistent Azurite
storage at `/data`. No local application build is required.

## Part 2 Testing

Import a collection from `tests`:

- `CoffeeNChill-Part2-Ayden.postman_collection.json`
- `CoffeeNChill-Part2-Yash.postman_collection.json`

Set collection variable `baseUrl` to `http://localhost:7170`
after import, even if its exported value is blank.
For uploads, select `tests/fixtures/staff-document-test.pdf`
from the local checkout.

Run these checks in order:

1. Send **Order Status Test** with a fresh OrderId.
   Expect 202. Its response script captures the order ID/date.
2. Wait approximately 20 seconds, then send **Completed Order**.
   Expect 200, Collected, and all four assertions passing.
   If processing is unfinished, wait and resend only the GET.
3. Send negative-price and missing-customer requests: expect 400.
4. Send the oversized-order request: expect 413.
5. Create a disposable menu item before testing updates.
   Incomplete and malformed updates should return 400.
   Read the item again to verify its original values remain.
6. Send a full update: expect 200 and verify the stored values.
   Delete only the disposable item: expect 204 and verify absence.
7. Upload a PDF: expect 201. Download using the returned filename
   or `{{uploadedFileName}}`: expect 200 and PDF content.

Some requests require manual sequencing and processing waits.
Saved requests without assertions require response inspection.

### Logs, Storage, and Persistence

Inspect `order-processing-queue`, `Orders`, and the poison queue
in Storage Explorer. Match order IDs with processing logs:

```powershell
docker compose logs --tail 100 functions
```

For persistence, record a completed order's ID and timestamps:

```powershell
docker compose down
docker compose up -d
docker compose ps
```

Retrieve the same order after startup. Its status and lifecycle
timestamps should remain unchanged. Do not delete or prune the
storage volume.

---## Historical Part 1 Documentation

The instructions below describe Part 1, including its v1.0 images
and port 7071. Use the Part 2 setup instructions above for Part 2.
## Detailed Description of the Application
*CLDV6212*
- Ayden Jordaan — ST10466857
- Yash Keshav — ST10449059

# CoffeeNChill — POE Part 2
## Part 2 Setup

### Prerequisites

- Docker Desktop running Linux containers.
- Git, Postman, and Azure Storage Explorer.
- An Azure Storage account supporting Azure Files.
- Available ports: 7170, 10000, 10001, and 10002.

Run commands from the Part 2 folder containing
docker-compose.yml and CoffeeNChill.Functions.
Stop Visual Studio debugging and any competing standalone
Azurite instance before starting the Docker stack.

### Configure Azure Files

In PowerShell, enter the real Azure Files connection string
privately when prompted:

powershell
$secret = Read-Host "Azure Files connection string" -AsSecureString
$env:AZURE_FILES_CONNECTION_STRING = [System.Net.NetworkCredential]::new("", $secret).Password
Remove-Variable secret


Do not commit or display the connection string.
Keep this terminal open; repeat configuration in a new session.

### Prepare Storage and Start

Create the external volume before the first startup.
An existing volume with this name is reused:

powershell
docker volume create coffeenchill-part2-data


Start the application with one command:

powershell
docker compose up -d --pull always


Check startup:

powershell
docker compose ps


Wait for Azurite to become healthy and Functions to be Up.
The API is available at http://localhost:7170.

Compose pulls yashkeshav/coffeenchill-functions:v2.0,
uses a custom bridge network, and mounts persistent Azurite
storage at /data. No local application build is required.

## Part 2 Testing

Import a collection from tests:

- CoffeeNChill-Part2-Ayden.postman_collection.json
- CoffeeNChill-Part2-Yash.postman_collection.json

Set collection variable baseUrl to http://localhost:7170
after import, even if its exported value is blank.
For uploads, select tests/fixtures/staff-document-test.pdf
from the local checkout.

Run these checks in order:

1. Send *Order Status Test* with a fresh OrderId.
   Expect 202. Its response script captures the order ID/date.
2. Wait approximately 20 seconds, then send *Completed Order*.
   Expect 200, Collected, and all four assertions passing.
   If processing is unfinished, wait and resend only the GET.
3. Send negative-price and missing-customer requests: expect 400.
4. Send the oversized-order request: expect 413.
5. Create a disposable menu item before testing updates.
   Incomplete and malformed updates should return 400.
   Read the item again to verify its original values remain.
6. Send a full update: expect 200 and verify the stored values.
   Delete only the disposable item: expect 204 and verify absence.
7. Upload a PDF: expect 201. Download using the returned filename
   or {{uploadedFileName}}: expect 200 and PDF content.

Some requests require manual sequencing and processing waits.
Saved requests without assertions require response inspection.

### Logs, Storage, and Persistence

Inspect order-processing-queue, Orders, and the poison queue
in Storage Explorer. Match order IDs with processing logs:

powershell
docker compose logs --tail 100 functions


For persistence, record a completed order's ID and timestamps:

powershell
docker compose down
docker compose up -d
docker compose ps


Retrieve the same order after startup. Its status and lifecycle
timestamps should remain unchanged. Do not delete or prune the
storage volume.

---
## Historical Part 1 Documentation

The instructions below describe Part 1, including its v1.0 images
and port 7071. Use the Part 2 setup instructions above for Part 2.


## Part 2 Demonstration

YouTube: https://youtu.be/ea6mKETFA74

## Part 2 Overview

Part 2 adds asynchronous order processing to CoffeeNChill.
An HTTP producer validates and queues orders. A queue-triggered
Azure Function stores each order and advances its status through
Received → Preparing → Ready → Collected.

The application runs through Docker Compose using the published
image yashkeshav/coffeenchill-functions:v2.0 and Microsoft Azurite.
Azurite provides local Queue and Table storage. Staff documents
continue to use a real Azure Files share.

## Order Processing

- POST /api/orders/queue validates the order and returns 202
  after successfully enqueueing it.
- Messages are serialized as JSON and encoded using Base64.
- The producer generates the order timestamp in UTC.
- Invalid payloads return 400; oversized messages return 413.
- Azure storage request failures are handled with a 503 response.
- ProcessOrderQueue consumes order-processing-queue.
- The Orders table uses the UTC order date as PartitionKey
  and OrderId as RowKey.
- Status changes and lifecycle timestamps are stored and logged.
- Failed processing is logged and retried. After five unsuccessful
  dequeue attempts, messages are moved to the poison queue.
- GET /api/orders/{orderDate}/{orderId} retrieves order status.

## Part 2 Changelog

- Added validated order submission and Base64 queue messages.
- Added server-generated timestamps and queue-size protection.
- Added queue-triggered processing and persistent order tracking.
- Added lifecycle transitions, retry handling, and poison handling.
- Added the order-status endpoint.
- Added Docker Compose with a published v2.0 application image,
  custom bridge network, and persistent Azurite volume.
- Improved incomplete and malformed menu-update validation.
- Expanded Postman regression tests and added a PDF test fixture.
- Verified published-container tests and persistence after
  container recreation.

---

## local.settings.json and Security

The `local.settings.json` file is used for local configuration.

For local Azurite development, the main configuration uses `UseDevelopmentStorage=true` for `AzureWebJobsStorage`.

The staff document service uses `AzureStorageConnectionString` for the Azure File Share connection.

Real Azure connection strings should not be committed to GitHub. When the application is run in Docker, the required connection values are passed in as environment variables instead of being hardcoded into the source code.

---

## Prerequisites

Before setting up this project, ensure the following software is installed on your development machine:

1. **Docker Desktop:** Required to run Azurite and the Azure Functions API as containers.
2. **.NET SDK:** Required to build and work with the Azure Functions project.
3. **Postman:** Required for testing the API endpoints.
4. **Azure Storage Explorer:** Required to verify Azure Table Storage and Azure File Share data.
5. **Git:** Required to clone and manage the GitHub repository.

---

## Setup Instructions

Follow this step-by-step guide to run the project locally.

### Step 1: Clone the Repository

Open your terminal or command prompt and clone the repository:

`git clone https://github.com/st10449059/CoffeeNChill-CLDV6212.git`

Move into the project folder:

`cd CoffeeNChill-CLDV6212`

### Step 2: Start the Azurite Emulator

Start Azurite as a standalone Docker container:

`docker run -d --name azurite-storage -p 10000:10000 -p 10001:10001 -p 10002:10002 yashkeshav/coffeenchill-azurite:v1.0`

Check that the container is running:

`docker ps`

Do not stop the Azurite container while testing the API.

### Step 3: Build the Azure Functions Image

Open a terminal in the `CoffeeNChill.Functions` folder:

`cd CoffeeNChill.Functions`

Build the Docker image:

`docker build -t yashkeshav/coffeenchill-functions:v1.0 .`

### Step 4: Run the Azure Functions Container

Run the Azure Functions API container:

`docker run -d --name coffeenchill-api -p 7071:80 -e AzureWebJobsStorage="DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErcZ4I6tq/K1SZFPTOtr/KBHBeKSGMGw==;BlobEndpoint=http://host.docker.internal:10000/devstoreaccount1;QueueEndpoint=http://host.docker.internal:10001/devstoreaccount1;TableEndpoint=http://host.docker.internal:10002/devstoreaccount1;" -e AzureStorageConnectionString="<YOUR_AZURE_STORAGE_CONNECTION_STRING>" yashkeshav/coffeenchill-functions:v1.0`

Check that both containers are running:

`docker ps`

The API will run on:

`http://localhost:7071`

---

## Docker Details

This application is fully containerized for consistent local execution.

Running via Docker:

1. Ensure Docker Desktop is running.
2. Start the Azurite container.
3. Open a terminal in the `CoffeeNChill.Functions` project folder.
4. Build the Functions image using `docker build`.
5. Run the Functions container using `docker run`.
6. Use `docker ps` to confirm that both containers are running.

Functions image:

`yashkeshav/coffeenchill-functions:v1.0`

Azurite image:

`yashkeshav/coffeenchill-azurite:v1.0`

Docker Hub links:

[CoffeeNChill Functions Docker Image](https://hub.docker.com/r/yashkeshav/coffeenchill-functions)

[CoffeeNChill Azurite Docker Image](https://hub.docker.com/r/yashkeshav/coffeenchill-azurite)

Screenshots showing the Docker containers running are included in the submitted Word document.

---

## Menu (Table Storage)

Menu items are persistently stored using Azure Table Storage. The database structure uses the item's category as the `PartitionKey` to group similar items and a unique SKU as the `RowKey`.

Example menu item:

- `PartitionKey`: Hot Drinks
- `RowKey`: COF-001
- `Name`: Cappuccino
- `Description`: Rich espresso with steamed milk foam
- `Price`: 35.00
- `IsAvailable`: true

Endpoints:

- `POST /api/menu` - Create a new menu item.
- `GET /api/menu` - Retrieve all menu items.
- `GET /api/menu/category/{category}` - Retrieve menu items by category.
- `PUT /api/menu/{category}/{id}` - Update an existing menu item.
- `DELETE /api/menu/{category}/{id}` - Delete a menu item.

The menu functions include validation and return appropriate HTTP responses for successful and unsuccessful requests.

---

## Documents (Azure File Share)

Operational staff documents are stored in Azure File Share inside the `staff-docs` share.

These documents can include recipe sheets, cleaning manuals, health and safety policies, and other operational documents used by the canteen.

Endpoints:

- `POST /api/documents/upload` - Upload a staff document.
- `GET /api/documents` - List all uploaded staff documents.
- `GET /api/documents/download/{fileName}` - Download a specific staff document.

The upload endpoint was tested through Postman using a file upload request. The list endpoint returns stored file information, and the download endpoint returns the selected file back to the client.

---

## API Testing

A Postman collection is included in the `docs` directory of this repository.

`docs/CoffeeNChill - Part 1 API.postman_collection.json`

The collection is structured into menu endpoint requests and staff document endpoint requests.

Menu endpoint requests included:

- `POST /api/menu`
- `GET /api/menu`
- `GET /api/menu/category/{category}`
- `PUT /api/menu/{category}/{id}`
- `DELETE /api/menu/{category}/{id}`

Staff document endpoint requests included:

- `POST /api/documents/upload`
- `GET /api/documents`
- `GET /api/documents/download/{fileName}`

The API was tested through Postman while the Docker containers were running on:

`http://localhost:7071`

Successful storage results were also verified in Azure Storage Explorer.

Screenshots showing the Postman requests and Azure Storage Explorer verification are included in the submitted Word document.

---

## YouTube Demonstration

The Part 1 video demonstration is available here:

[Watch the CoffeeNChill Part 1 YouTube Demonstration](https://youtu.be/4YlkgZoRjlY)

The video demonstrates:

- Project structure
- Code architecture
- Azurite running in Docker
- Azure Functions API running in Docker
- Postman endpoint testing
- Azure Storage Explorer verification
- Docker Hub image tags
- GitHub commit history

---

## GitHub Repository

[Open the CoffeeNChill GitHub Repository](https://github.com/st10449059/CoffeeNChill-CLDV6212)

The GitHub commit history shows contributions from both group members.

---

## References and Acknowledgements

### Coursework and Institutional Resources

- Class notes and code examples provided during lectures.
- CLDV6212 POE brief and marking rubric.
- Lecturer Part 1 complete file structure guide.

### Official Microsoft Documentation

- Azure Functions documentation: https://learn.microsoft.com/en-us/azure/azure-functions/
- Azure Table Storage documentation: https://learn.microsoft.com/en-us/azure/storage/tables/
- Azure Files documentation: https://learn.microsoft.com/en-us/azure/storage/files/
- Azurite documentation: https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite
- Azure SDK for .NET documentation: https://learn.microsoft.com/en-us/dotnet/azure/

### Development and Testing Tools

- Postman documentation: https://learning.postman.com/docs/
- Docker documentation: https://docs.docker.com/
- Docker Hub: https://hub.docker.com/
- Azure Storage Explorer documentation: https://learn.microsoft.com/en-us/azure/vs-azure-tools-storage-manage-with-storage-explorer

---

## Conclusion

CoffeeNChill Part 1 implements the required Azure Functions backend for menu management and staff document storage.

The project demonstrates Azure Table Storage, Azure File Share, Azurite, Docker, Docker Hub, Postman testing, Azure Storage Explorer verification, and GitHub version control.
