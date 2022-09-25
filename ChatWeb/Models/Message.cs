using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ChatWeb.Models
{
    public class Message
    {
        [JsonPropertyName("Id")]
        public Guid Id { get; set; } = Guid.NewGuid();
        [JsonPropertyName("GroupName")]
        public string GroupName { get; set; }
        [JsonPropertyName("UserId")]
        public Guid UserId { get; set; }
        [JsonPropertyName("JsonUser")]
        public string JsonUser { get; set; }
        [NotMapped]
        public User User { get; set; }
        [JsonPropertyName("Text")]
        public string Text { get; set; }
        [JsonPropertyName("CreatedDate")]
        public DateTime? CreatedDate { get; set; }
    }
}
