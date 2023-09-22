using DataReaderTesting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using ProtoBuf.Data;

namespace HubTesting.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class HubSyncController : ControllerBase
    {
        private readonly ILogger<HubSyncController> _logger;
        private DatabaseReader databaseReader = new DatabaseReader("Data Source=localhost/ORCL;User Id=DB_READDATABASE461;Password=Admin123;");
        public HubSyncController(ILogger<HubSyncController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IActionResult GetProtoCompleteStream()
        {
            MemoryStream stream = databaseReader.GetProtoBufferStream("LOCATION");

            var syncIOFeature = HttpContext.Features.Get<IHttpBodyControlFeature>();
            if (syncIOFeature != null)
            {
                syncIOFeature.AllowSynchronousIO = true;
            }

            // Ensure the response is correctly formatted as application/x-protobuf
            Response.Headers.Add("Content-Type", "application/x-protobuf");

            // Set the Content-Length header to the actual length of the stream
            Response.ContentLength = stream.Length;

            return File(((MemoryStream)stream).ToArray(), "application/x-protobuf");
        }

        [HttpGet]
        public IActionResult GetProtoStreaming()
        {
            var syncIOFeature = HttpContext.Features.Get<IHttpBodyControlFeature>();
            if (syncIOFeature != null)
            {
                syncIOFeature.AllowSynchronousIO = true;
            }

            // Ensure the response is correctly formatted as application/x-protobuf
            Response.Headers.Add("Content-Type", "application/x-protobuf");

            using (var dataReader = databaseReader.GetDataReader("LOCATION"))
            {
                DataSerializer.Serialize(Response.Body, dataReader);
            }
            return new EmptyResult();
        }
    }
}