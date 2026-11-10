# CoffeeNChill Part 3 - Container Deployment

## Published Functions Image

Docker Hub: yashkeshav/coffeenchill-functions:v3.0

Platform: Linux AMD64

Runtime: Azure Functions v4 with .NET 10 isolated worker

Published OCI index digest:
sha256:6746d3b56a4ddeab58336fc6af4e4ea380da0eaafdfb761d0af5208d224b6ef0

The image includes the Part 3 order-status and staff-document
deletion endpoints.

## Local Integration Environment

The Part 3 environment is defined in docker-compose.part3.yml.

It includes:

- Azure Functions using the v3.0 image.
- Azurite 3.37.0 for local storage emulation.
- A private Docker bridge network.
- A dedicated persistent Part 3 storage volume.

### Startup Commands

Run from the repository root:

    docker compose -f docker-compose.part3.yml config --quiet
    docker compose -f docker-compose.part3.yml up -d --no-build
    docker compose -f docker-compose.part3.yml ps
    docker compose -f docker-compose.part3.yml logs --tail 40 functions

### Stop Containers

    docker compose -f docker-compose.part3.yml down

Do not use --volumes unless deleting disposable storage
is explicitly intended.

## Verified Functionality

- Functions image built successfully for Linux AMD64.
- Azure Functions host started successfully in Docker.
- Azurite health check passed.
- Internal GET /api/menu returned HTTP 200.
- Empty Part 3 menu returned an empty JSON array.
- Automatic order-status progression completed successfully.
- Manual Staff updates worked alongside queue processing.
- Final order timestamps were chronological.

The actual ETag 412 collision path was not directly exercised.

## Security

The current Part 3 Compose foundation does not publish
Functions or Azurite ports to the host.

The anonymous Functions endpoints must remain private.
Public traffic must pass through the authenticated API Gateway.

The gateway must enforce JWT authentication and Staff/Admin
role authorization as appropriate.

Real credentials must not be committed to Git.

The Azure Files connection string must be supplied privately
through COFFEENCHILL_AZURE_FILES_CONNECTION_STRING.

Azurite uses development-only credentials and must not
be treated as production cloud storage.

## Outstanding Requirements

- Integrate the ASP.NET Core API Gateway.
- Verify JWT login, registration and role authorization.
- Test protected endpoints using Postman.
- Configure required production Azure storage.
- Finalize the complete production Docker Compose stack.
- Deploy the integrated stack on a Linux VPS.
- Capture deployment, Postman and video evidence.

Local Docker success does not establish VPS deployment.
