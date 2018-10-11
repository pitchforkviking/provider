namespace Provider.Controllers
{
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using System.Web.Http;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table;
    using System.Threading.Tasks;
    using System;
    using System.Globalization;

    public class ApplicationUser : TableEntity
    {
        public string EmailID { get; set; }

        public string Password { get; set; }

    }

    public class ApplicationEvent : TableEntity
    {
        public string EmailID { get; set; }

        public string EventTitle { get; set; }

        public string EventDate { get; set; }

        public int Days { get; set; }

        public bool IsCountDown { get; set; }

    }

    public class ApplicationEventResponse 
    {
        public string Key { get; set; }
        public string EventTitle { get; set; }
        
        public int Days { get; set; }

        public bool IsCountDown { get; set; }

    }

    public class ProviderController : ApiController
    {
        #region Private Methods

        private int GetDays(string date)
        {
            var currentDate = Convert.ToDateTime(DateTime.Now, CultureInfo.InvariantCulture);
            var eventDate = Convert.ToDateTime(date, CultureInfo.InvariantCulture);

            var days = Math.Abs(currentDate.Subtract(eventDate).Days);

            return days;

        }

        private bool CheckIfIsCountDown(string date)
        {
            var currentDate = Convert.ToDateTime(DateTime.Now, CultureInfo.InvariantCulture);
            var eventDate = Convert.ToDateTime(date, CultureInfo.InvariantCulture);

            return currentDate < eventDate;

        }

        #endregion

        [HttpPost]
        public async Task<IHttpActionResult> SignUp(ApplicationUser applicationUser)
        {
            try
            {
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["ProviderStorage"]);
                CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                CloudTable table = tableClient.GetTableReference("ApplicationUser");
                table.CreateIfNotExists();

                TableOperation retrieveOperation = TableOperation.Retrieve<ApplicationUser>(Convert.ToString(applicationUser.EmailID[0]), applicationUser.EmailID);
                TableResult result = await table.ExecuteAsync(retrieveOperation);
                if (result.Result != null)
                {
                    throw new Exception("You Are Everywhere!");
                }

                applicationUser.PartitionKey = Convert.ToString(applicationUser.EmailID[0]);
                applicationUser.RowKey = applicationUser.EmailID;

                TableOperation insertOperation = TableOperation.Insert(applicationUser);
                await table.ExecuteAsync(insertOperation);

                return Ok();
            }
            catch (Exception exception)
            {
                return BadRequest(exception.Message ?? exception.InnerException.Message);
            }

        }

        [HttpPost]
        public async Task<IHttpActionResult> SignIn(ApplicationUser applicationUser)
        {
            try
            {
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["ProviderStorage"]);
                CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                CloudTable table = tableClient.GetTableReference("ApplicationUser");
                //table.CreateIfNotExists();

                TableOperation retrieveOperation = TableOperation.Retrieve<ApplicationUser>(Convert.ToString(applicationUser.EmailID[0]), applicationUser.EmailID);
                TableResult result = await table.ExecuteAsync(retrieveOperation);
                if (result.Result != null)
                {
                    if (((ApplicationUser)result.Result).Password == applicationUser.Password)
                    {
                        return Ok();
                    }
                    else
                    {
                        return BadRequest("Wrong Password!");
                    }
                }

                return BadRequest("You Don't Exist!");
            }
            catch (Exception exception)
            {
                return BadRequest(exception.Message ?? exception.InnerException.Message);
            }
        }

        [HttpPost]
        public async Task<IHttpActionResult> AddEvent(ApplicationEvent applicationEvent)
        {
            try
            {
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["ProviderStorage"]);
                CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                CloudTable table = tableClient.GetTableReference("ApplicationEvent");
                table.CreateIfNotExists();

                applicationEvent.Days = GetDays(applicationEvent.EventDate);
                applicationEvent.IsCountDown = CheckIfIsCountDown(applicationEvent.EventDate);

                applicationEvent.PartitionKey = Convert.ToString(applicationEvent.EmailID);
                applicationEvent.RowKey = Convert.ToString(Guid.NewGuid());

                TableOperation insertOperation = TableOperation.Insert(applicationEvent);
                await table.ExecuteAsync(insertOperation);

                return Ok();
            }
            catch (Exception exception)
            {
                return BadRequest(exception.Message ?? exception.InnerException.Message);
            }
        }

        [HttpGet]
        public IHttpActionResult GetEvents(string partitionKey)
        {
            try
            {
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["ProviderStorage"]);
                CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                CloudTable table = tableClient.GetTableReference("ApplicationEvent");
                //table.CreateIfNotExists();

                TableQuery<ApplicationEvent> query = new TableQuery<ApplicationEvent>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey));

                List<ApplicationEventResponse> response = new List<ApplicationEventResponse>();

                foreach (ApplicationEvent entity in table.ExecuteQuery(query))
                {
                    ApplicationEventResponse applicationEventResponse = new ApplicationEventResponse
                    {
                        Key = entity.RowKey,
                        Days = entity.Days,
                        EventTitle = entity.EventTitle,
                        IsCountDown = entity.IsCountDown
                    };
                    response.Add(applicationEventResponse);
                }

                return Ok(response);
            }
            catch
            {
                return BadRequest();
            }
        }

        [HttpGet]
        public IHttpActionResult DeleteEvent(string partitionKey, string rowKey)
        {
            try
            {
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["ProviderStorage"]);
                CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                CloudTable table = tableClient.GetTableReference("ApplicationEvent");
                //table.CreateIfNotExists();

                TableOperation retrieveOperation = TableOperation.Retrieve<ApplicationEvent>(partitionKey, rowKey);
                TableResult retrievedResult = table.Execute(retrieveOperation);
                ApplicationEvent deleteEntity = (ApplicationEvent)retrievedResult.Result;
                if (deleteEntity != null)
                {
                    TableOperation deleteOperation = TableOperation.Delete(deleteEntity);
                    table.Execute(deleteOperation);

                    return Ok();
                }
                else
                {
                    return BadRequest("Failed");
                }                
            }
            catch(Exception exception)
            {
                return BadRequest(exception.Message??exception.InnerException.Message);
            }
        }

    }
}
