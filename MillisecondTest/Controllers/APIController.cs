using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MillisecondTest.Models;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Auth;
using System.Text;

namespace MillisecondTest.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class APIController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly CloudStorageAccount _storageAccount;
        private readonly CloudQueueClient _queueClient;
        private readonly CloudQueue _messageQueue;
        private const string _queueName = "queue";

        public APIController(IConfiguration configuration)
        {
            _configuration = configuration;
            var accountName =
            _configuration["ConnectionStrings:StrorageConnection:AccountName"];
            var accountKey =
            _configuration["ConnectionStrings:StrorageConnection:AccountKey"];
            _storageAccount = new CloudStorageAccount(
            new StorageCredentials(accountName, accountKey),
            true);
            _queueClient = _storageAccount.CreateCloudQueueClient();
            _messageQueue = _queueClient.GetQueueReference(_queueName);
            _messageQueue.CreateIfNotExistsAsync().Wait();
        }

        // GET: api/API
        [HttpGet]
        public string Get()
        {
            //when running locally, run the controller with this endpoint so the constructor creates the queue client
            return "Millisecond Digital Test by Urho Niemelä";
        }

        // GET: api/API/export
        [HttpGet("export", Name = "Export CSV")]
        public string ExportCSV()
        {
            MillisecondTestContext db = new MillisecondTestContext(_configuration);

            var queryAll = from c in db.Customer
                           select c;

            StringBuilder sb = new StringBuilder();
            sb.Append("Id,Key,Email,Attributes \n"); //add header
            foreach (var row in queryAll)
            {
                sb.Append(row.Id + "," + row.Key + "," + row.Email + "," + row.Attributes +"\n");
            }

            return sb.ToString();
        }

        // POST: api/API
        [HttpPost]
        public string Post([FromBody] DTO data)
        {
            string dataJson = JsonConvert.SerializeObject(data);
            //Queue entry will trigger an azure function that performs the necessary storing operations.
            FeedToQueue(dataJson);

            return "success";
        }

        public async void FeedToQueue(string json)
        {
            CloudQueueMessage message = new CloudQueueMessage(json);
            await _messageQueue.AddMessageAsync(message);
        }
    }

    
}
