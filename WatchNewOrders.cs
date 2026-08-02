using System.IO;
using System.Threading.Tasks;
// using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FreakyFashionMicroServices;

public class WatchNewOrders
{
    private readonly ILogger<WatchNewOrders> _logger;

    public WatchNewOrders(ILogger<WatchNewOrders> logger)
    {
        _logger = logger;
    }
    [Function(nameof(WatchNewOrders))]
    public async Task Run(
       
        [BlobTrigger("orders/{name}", Connection = "AzureWebJobsStorage")] Stream stream, 
        string name)
    {        
        _logger.LogInformation($"[BLOB-TRIGGER] Ny order upptäckt i Azurite! Filnamn: {name}");

        string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage") ?? "UseDevelopmentStorage=true";

        try
        {
            var queueServiceClient = new QueueServiceClient(connectionString);
            var queueClient = queueServiceClient.GetQueueClient("payment-orders");
            
            await queueClient.CreateIfNotExistsAsync();
            // (Crucial: Azure Functions require Queue messages to be Base64)
                var bytes = System.Text.Encoding.UTF8.GetBytes(name);
                string base64XmlMessage = Convert.ToBase64String(bytes);

            await queueClient.SendMessageAsync(base64XmlMessage);
            _logger.LogInformation($"[QUEUE] Filnamnet '{name}' har skickats till kön 'payment-orders'.");
        }
        catch (Exception ex)
        {
            _logger.LogError($"[ERROR] Kunde inte skicka meddelande till kön: {ex.Message}");
        }
    }
}