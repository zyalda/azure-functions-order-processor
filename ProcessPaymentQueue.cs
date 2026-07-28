using System;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json.Nodes;
using Azure.Storage.Queues;
using Azure.Storage.Blobs;
using System.Text.Json;

namespace FreakyFashionMicroServices;

public class ProcessPaymentQueue
{
    private readonly ILogger<ProcessPaymentQueue> _logger;

    public ProcessPaymentQueue(ILogger<ProcessPaymentQueue> logger)
    {
        _logger = logger;
    }

    [Function(nameof(ProcessPaymentQueue))]
    public async Task Run([QueueTrigger("payment-orders", Connection = "AzureWebJobsStorage")] QueueMessage message)
    {
       string fileName = message.MessageText;
        _logger.LogInformation($"[QUEUE-TRIGGER] Triggered! Processing file: {fileName}");

        try
        {
            string connectionString = "UseDevelopmentStorage=true";
            
            var blobServiceClient = new BlobServiceClient(connectionString);

            //GET THE FILE: Locate in the "orders" container
            var originalContainer = blobServiceClient.GetBlobContainerClient("orders");
            var originalBlobClient = originalContainer.GetBlobClient(fileName);

            if (!await originalBlobClient.ExistsAsync())
            {
                _logger.LogWarning($"[WARNING] Could not find file {fileName} in orders container.");
                return;
            }

            //READ JSON: Download the file content into a string
            var downloadResponse = await originalBlobClient.DownloadContentAsync();
            string rawJson = downloadResponse.Value.Content.ToString();

            //SIMULATE BANK: Wait 2 seconds to simulate Klarna/Stripe
            _logger.LogInformation($"[BANK] Contacting payment gateway to process payment for {fileName}...");
            await Task.Delay(2000);
            _logger.LogInformation($"[SUCCESS] Payment APPROVED for {fileName}!");

            //SAVE PAID ORDER: Create and save to the new container 'paymentorder'
            var paymentContainer = blobServiceClient.GetBlobContainerClient("paymentorder");
            await paymentContainer.CreateIfNotExistsAsync();

            var paidBlobClient = paymentContainer.GetBlobClient($"paid-{fileName}");
            await paidBlobClient.UploadAsync(BinaryData.FromString(rawJson), overwrite: true);
            _logger.LogInformation($"[SAVED] Paid order saved to 'paymentorder'.");

            //CLEANUP: Delete the original from orders container to keep it clean
            await originalBlobClient.DeleteIfExistsAsync();
            _logger.LogInformation($"[CLEANUP] Original file {fileName} removed from orders container.");
        }
        catch (Exception ex)
        {
            _logger.LogError($"[ERROR] Fel vid hantering av betalningskö: {ex.Message}");
            throw;
        }
    }
}