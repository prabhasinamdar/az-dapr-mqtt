using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using MqttMessageHandler.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace MqttMessageHandler.Controllers
{
    [ApiController]
    public class MqttHandlerController : ControllerBase
    {
        private readonly EventHubProducerClient producer;
        private readonly ILogger<MqttHandlerController> _logger;
        private readonly IConfiguration _configuration;

        EventHubProducerClientOptions producerOptions = new EventHubProducerClientOptions
        {
            ConnectionOptions = new EventHubConnectionOptions
            {
                TransportType = EventHubsTransportType.AmqpWebSockets
            },
            RetryOptions = new EventHubsRetryOptions
            {
                Mode = EventHubsRetryMode.Exponential,
                MaximumRetries = 5,
                Delay = TimeSpan.FromMilliseconds(800),
                MaximumDelay = TimeSpan.FromSeconds(10)
            }
        };

        public MqttHandlerController(ILogger<MqttHandlerController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            producer = new EventHubProducerClient(
                        _configuration.GetValue<string>("MQTTEHConnectionString"),
                        _configuration.GetValue<string>("MQTTEHName"),
                        producerOptions);
        }

        [HttpPost("/mqttmessagehandler")]
        public async Task<IActionResult> ProcessMqttPayload()
        {
            try
            {
                string topicName = "";
                //in case the topic name is wildcard then you can retrieve the specific topic name from the request header
                if (Request.Headers["Topic"] != "")
                {
                    topicName = Request.Headers["Topic"].ToString();
                }

                using var mqttpayload = new StreamReader(Request.Body);
                var content = await mqttpayload.ReadToEndAsync();
                var mqttPayloadModel = JsonConvert.DeserializeObject<IotDevicePropertyModel>(content);

                if (mqttPayloadModel != null && !string.IsNullOrEmpty(topicName))
                {
                    var topicarray = topicName.Split('/');

                    IotDevicePropertyModel iotDevicePropertyModel = new IotDevicePropertyModel()
                    {
                        DeviceId = topicarray[8],       //iot device id
                        Property = topicarray[9],       //property name
                        Value = mqttPayloadModel.Value,
                        Timestamp = mqttPayloadModel.Timestamp
                    };

                    string ehJson = JsonConvert.SerializeObject(iotDevicePropertyModel);

                    using EventDataBatch eventBatch = await producer.CreateBatchAsync();
                    var eventData = new EventData(ehJson);

                    if (!eventBatch.TryAdd(eventData))
                    {
                        throw new Exception($"The event could not be added.");
                    }
                    await producer.SendAsync(eventBatch);
                    _logger.LogInformation($"mqtt payload sent to event hub for topic{topicName}");
                }
            }
            catch (Exception ex) 
            { 
                _logger.LogCritical("Exception in processing message from mqtt", ex); 
                return Problem(ex.Message); 
            }
            return Ok(string.Empty);
        }
    }
}
