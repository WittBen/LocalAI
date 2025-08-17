using Newtonsoft.Json;

namespace LocalChatBot
{
  public class ChatMessage
  {
    [JsonProperty("role")]
    public string Role { get; set; }
    [JsonProperty("content")]
    public string Content { get; set; }
    public string Rationale { get; set; } 
  }
}