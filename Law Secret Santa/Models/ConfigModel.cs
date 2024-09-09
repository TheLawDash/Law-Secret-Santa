using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Law_Secret_Santa.Models
{
    public class ConfigModel
    {
        [JsonPropertyName("DiscordBotToken")]
        public required string DiscordBotToken { get; set; }
        [JsonPropertyName("AdminRoleId")]
        public required string AdminRoleId { get; set; }
    }
}
