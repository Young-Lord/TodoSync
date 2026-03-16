using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace TodoSynchronizer.Core.Models.AcmOjModels
{
    public class AcmOjProblemset
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("course")]
        public AcmOjCourse Course { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("start_time")]
        public DateTime? StartTime { get; set; }

        [JsonProperty("end_time")]
        public DateTime? EndTime { get; set; }

        [JsonProperty("late_submission_deadline")]
        public DateTime? LateSubmissionDeadline { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("problems")]
        public List<AcmOjProblemBrief> Problems { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("html_url")]
        public string HtmlUrl { get; set; }
    }

    public class AcmOjProblemBrief
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("html_url")]
        public string HtmlUrl { get; set; }
    }

    public class AcmOjCourse
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }
    }

    public class AcmOjProblemsetListResponse
    {
        [JsonProperty("problemsets")]
        public List<AcmOjProblemset> Problemsets { get; set; }
    }
}
