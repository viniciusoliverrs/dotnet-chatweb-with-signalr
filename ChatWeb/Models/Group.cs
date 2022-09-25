using System.Text.Json.Serialization;

namespace ChatWeb.Models
{
    public class Group
    {
        [JsonPropertyName("Id")]
        public Guid Id { get; set; } = Guid.NewGuid();
        [JsonPropertyName("Name")]
        public string Name { get; set; }
        [JsonPropertyName("Users")]
        public string Users { get; set; }
    }
}
