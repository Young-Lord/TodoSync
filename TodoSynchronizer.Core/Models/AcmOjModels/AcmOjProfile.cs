using Newtonsoft.Json;

namespace TodoSynchronizer.Core.Models.AcmOjModels
{
    public class AcmOjProfile
    {
        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("friendly_name")]
        public string FriendlyName { get; set; }

        [JsonProperty("student_id")]
        public string StudentId { get; set; }
    }
}
