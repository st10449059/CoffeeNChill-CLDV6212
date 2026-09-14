# CoffeeNChill API

## Detailed Description of the Application

The CoffeeNChill API is a serverless backend REST API designed for the CLDV6212 POE Part 1 submission. The application modernizes part of the campus canteen system by moving menu management and staff document storage into a cloud-ready backend.

The application provides API endpoints for managing CoffeeNChill menu items. Users can create menu items, retrieve all menu items, retrieve menu items by category, update existing menu items, and delete menu items. These menu records are stored in Azure Table Storage in a table named `MenuItems`.

The application also provides API endpoints for staff document management. Staff documents can be uploaded, listed, and downloaded. These files are stored in an Azure File Share named `staff-docs`.

The API was tested using Postman, verified using Azure Storage Explorer, containerized with Docker, and published to Docker Hub.

---

## Architecture Overview

The CoffeeNChill API uses a serverless backend architecture.

- **Compute (Azure Functions):** Handles HTTP requests and returns API responses.
- **Structured Data (Azure Table Storage):** Stores menu items using the item category as the `PartitionKey` and the item SKU as the `RowKey`.
- **File Storage (Azure File Share):** Stores uploaded staff documents in the `staff-docs` file share.
- **Local Emulation (Azurite):** Provides a local Azure Storage emulator for development and testing.
- **Containerization (Docker):** Runs the Azurite emulator and Azure Functions API as standalone containers.
- **Testing (Postman):** Used to test the required API endpoints.

The project separates the code into `Functions`, `Models`, and `Services`.

The `Functions` folder contains the HTTP-triggered Azure Functions. These files are responsible for handling API routes and returning responses.

The `Models` folder contains the data model used by the application. For Part 1, the main model is `MenuItem`.

The `Services` folder contains the storage logic. `MenuTableService` handles Azure Table Storage operations for menu items. `FileShareService` handles Azure File Share operations for staff documents.

This separation keeps the API logic separate from the Azure Storage logic, making the project easier to understand, maintain, and test.

---

## Project Team and Individual Contributions

Detailed individual contributions can also be verified through the commit history on this GitHub repository.

- **Ayden Jordaan ST10466857:** Set up the Azure Functions project structure, worked on the storage service layer, improved validation and error handling, fixed menu table service issues, and contributed feature commits through GitHub.
- **Yash Keshav ST10449059:** Implemented the menu API endpoints, implemented the staff document API endpoints, configured Docker support, published Docker images to Docker Hub, structured the Postman collection, and helped prepare the README and video demonstration.

---

## Technologies Used

- **Framework:** C# / .NET Azure Functions Isolated Worker
- **Database:** Azure Table Storage for menu records
- **File Storage:** Azure File Share for staff documents
- **Local Emulator:** Azurite
- **Containerization:** Docker
- **Testing:** Postman
- **Storage Verification:** Azure Storage Explorer
- **Version Control:** GitHub
- **Image Registry:** Docker Hub

---

## Why Azurite Emulation

Microsoft's official local emulator for Azure Storage is Azurite. The purpose of using Azurite is to allow the team to build and test Azure Storage functionality locally.

In this project, Azurite is used during local testing and runs inside its own Docker container. This allows the Azure Functions API container to connect to local emulated storage while still using Azure Storage SDKs and Azure Storage concepts.

Part 1 requires standalone Docker execution, so Azurite and the Azure Functions API are started separately using `docker run`. Docker Compose is not used for this part.

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

`docker run -d --name azurite-storage -p 10000:10000 -p 10001:10001 -p 10002:10002 mcr.microsoft.com/azure-storage/azurite`

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

`https://hub.docker.com/r/yashkeshav/coffeenchill-functions`

`https://hub.docker.com/r/yashkeshav/coffeenchill-azurite`

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

`https://youtu.be/4YlkgZoRjlY`

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

`https://github.com/st10449059/CoffeeNChill-CLDV6212`

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
