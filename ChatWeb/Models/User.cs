using System.Text.Json.Serialization;

namespace ChatWeb.Models
{
    public class User
    {
        [JsonPropertyName("Id")]
        public Guid Id { get; set; } = Guid.NewGuid();
        [JsonPropertyName("Name")]
        public string Name { get; set; }
        [JsonPropertyName("Email")]
        public string Email { get; set; }
        [JsonPropertyName("Password")]
        public string Password { get; set; }
        [JsonPropertyName("IsOnline")]
        public bool IsOnline { get; set; }
        [JsonPropertyName("ConnectionId")]
        public string? ConnectionId { get; set; } //JSON - n
    }
}
