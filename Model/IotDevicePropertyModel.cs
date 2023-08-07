using Newtonsoft.Json;

namespace MqttMessageHandler.Model
{
    public class IotDevicePropertyModel
    {
        public string ResourceId { get; set; } = string.Empty;
        [JsonProperty("deviceId")]
        public string DeviceId { get; set; } = string.Empty;
        [JsonProperty("property")]
        public string Property { get; set; } = string.Empty;
        [JsonProperty("value")]
        public Nullable<double> Value { get; set; }
        [JsonProperty("timestamp")]
        public Nullable<DateTimeOffset> Timestamp { get; set; }
    }

    public partial class IoTMqttPayloadModel
    {
        [JsonProperty("timestamp")]
        public DateTimeOffset Timestamp { get; set; }

        [JsonProperty("value")]
        public double Value { get; set; }
    }
}
