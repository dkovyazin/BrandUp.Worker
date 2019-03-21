using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;

namespace BrandUp.Worker.Models
{
    public class PushTaskRequest
    {
        [Required]
        [JsonProperty(ItemTypeNameHandling = TypeNameHandling.Auto, Required = Required.Always, TypeNameHandling = TypeNameHandling.Auto)]
        public object TaskModel { get; set; }
    }

    public class PushTaskResponse
    {
        [Required]
        public Guid TaskId { get; set; }
    }
}