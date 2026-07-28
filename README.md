# Azure Functions Order Processor

A serverless background Microservice built with **.NET 8** and **Azure Functions** designed to process e-commerce orders asynchronously.
This service handles unique order number generation, formats transaction data, and uploads secure transaction logs to **Azure Blob Storage** immediately after a successful database commit.

This Microservice works in tandem with the main E-Commerce Web API to decouple heavy I/O operations from the primary user-facing server.

---

## Architectural Overview

By moving the order file generation and cloud storage logging out of the main API and into this serverless function, the system achieves:
1. **High Scalability:** Handles bursts of completed orders without degrading the performance of the main web application.
2. **Decoupled Responsibilities:** The main API focuses on lightning-fast in-memory cart management and atomic SQL writes, while this function handles cloud storage operations.
3. **Resilience:** If cloud storage experiences latency, the order processing is isolated and won't block the customer's checkout experience.

```text
   [Main Web API] ──(SQL Commit Success)──> [Invoke Azure Function]
   │┌───────────────────────────────┴───────────────────────────────┐▼▼
   [Generate Unique Order Number] [Serialize Order to JSON]
   ││└───────────────────────────────┬───────────────────────────────┘▼
   [Upload to Azure Blob Storage]

```
   ---

## Features

* **Serverless Execution:** Scales automatically using the Azure Functions consumption model.
* **Secure Document Storage:** Uploads structured JSON transaction logs directly to private Azure Blob containers.
* **Robust Configuration:** Implements strict data isolation utilizing environment variables and secure connection strings.
* **Fail-Safe Integrity:** Structured to ensure no storage attempts occur unless the primary database layer has finalized the order transaction.

---

## Tech Stack

* **Language:** C#
* **Framework:** .NET 8.0 (Isolated Worker Process)
* **SDK:** Microsoft.Azure.Functions.Worker
* **Cloud Services:** Azure Functions, Azure Blob Storage

---

## Local Development Setup

To run this function locally alongside the main Web API, you need to configure local emulation environment.

1. Ensure **Azurite** (Azure Storage Emulator) is running in the background.
2. Create a `local.settings.json` file in the root directory (this file is excluded from source control via `.gitignore` for security):

```json
{
    "IsEncrypted": false,
    "Values": {
        "AzureWebJobsStorage": "UseDevelopmentStorage=true",
        "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated"
    }
}
```

3. Open your terminal and start the function host:
```bash
func start
```

---

## Clean Architecture & Security Notes

* **Data Isolation:** This project strictly uses a `.gitignore` profile to prevent sensitive credentials, local database keys, or connection strings (`local.settings.json`) from ever leaking into public source control.
* **Idempotency:** Works with the upstream Web API to ensure that data pipeline events execute deterministically based on finalized database IDs.
   
