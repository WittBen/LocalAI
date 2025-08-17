using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;


namespace LocalChatBot
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    private static readonly HttpClient client = new HttpClient() { Timeout = TimeSpan.FromMinutes(5) };
    private const int CharacterStreamDelay = 30;
    private const string FoundryApiUrl = "http://localhost:5273/v1/chat/completions";
    private readonly List<ChatMessage> conversationHistory;

    public MainWindow()
    {
      InitializeComponent();
      conversationHistory = new List<ChatMessage>();
    }

    private async void SubmitButton_Click(object sender, RoutedEventArgs e)
    {
      if (string.IsNullOrWhiteSpace(InputTextBox.Text)) return;

      SubmitButton.IsEnabled = false;
      StatusTextBlock.Text = "Sending request to the model...";

      string userInput = InputTextBox.Text;
      InputTextBox.Text = "";

      try
      {
        var userMessage = new ChatMessage { Role = "user", Content = userInput };
        conversationHistory.Add(userMessage);
        AddMessageToUI(userMessage);

        string modelName = "gpt-oss-20b-cuda-gpu";

        var requestBody = new
        {
          model = modelName,
          messages = conversationHistory,
          temperature = 0.7,
          max_tokens = 2048,
          stream = true // This parameter enables streaming
        };

        string jsonRequest = JsonConvert.SerializeObject(requestBody);
        HttpContent content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, FoundryApiUrl)
        {
          Content = content
        };

        HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        if (response.IsSuccessStatusCode)
        {
          StatusTextBlock.Text = "Receiving response...";
          await ProcessStreamedResponse(response);
          StatusTextBlock.Text = "Response received.";
        }
        else
        {
          conversationHistory.Remove(userMessage);
          AddMessageToUI(new ChatMessage { Role = "error", Content = $"Error: {response.StatusCode}" });
          string error = await response.Content.ReadAsStringAsync();
          MessageBox.Show($"API request failed: {response.StatusCode}\n{error}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
          StatusTextBlock.Text = "Error.";
        }
      }
      catch (Exception ex)
      {
        AddMessageToUI(new ChatMessage { Role = "error", Content = $"An unexpected error occurred: {ex.Message}" });
        MessageBox.Show($"An unexpected error occurred:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        StatusTextBlock.Text = "Error.";
      }
      finally
      {
        SubmitButton.IsEnabled = true;
      }
    }

    private async Task ProcessStreamedResponse(HttpResponseMessage response)
    {
      var assistantMessage = new ChatMessage { Role = "assistant", Content = "" };
      conversationHistory.Add(assistantMessage);

      TextBlock assistantTextBlock = null;
      await Dispatcher.InvokeAsync(() =>
      {
        assistantTextBlock = CreateMessageTextBlock(assistantMessage);
        ConversationStackPanel.Children.Add(assistantTextBlock);
      });

      var streamBuffer = new StringBuilder();
      var displayedCharacterCount = 0;

      const string startMarker = "<|channel|>final<|message|>";
      const string endMarker = "<|return|>";

      using (var stream = await response.Content.ReadAsStreamAsync())
      using (var reader = new StreamReader(stream))
      {
        while (!reader.EndOfStream)
        {
          string line = await reader.ReadLineAsync();
          if (string.IsNullOrWhiteSpace(line) || line.StartsWith("data: [DONE]")) continue;
          if (line.StartsWith("data: ")) line = line.Substring(6);

          try
          {
            var responseObject = JObject.Parse(line);
            var delta = responseObject["choices"]?[0]?["delta"]?["content"]?.ToString();

            if (!string.IsNullOrEmpty(delta))
            {
              streamBuffer.Append(delta);
              string rawResponse = streamBuffer.ToString();

              int startIndex = rawResponse.IndexOf(startMarker);
              if (startIndex == -1) continue;

              int textStartIndex = startIndex + startMarker.Length;
              int endIndex = rawResponse.IndexOf(endMarker, textStartIndex);

              string finalMessage = (endIndex != -1)
                  ? rawResponse.Substring(textStartIndex, endIndex - textStartIndex)
                  : rawResponse.Substring(textStartIndex);

              if (finalMessage.Length > displayedCharacterCount)
              {
                string newTextToShow = finalMessage.Substring(displayedCharacterCount);

                foreach (char character in newTextToShow)
                {
                  await Dispatcher.InvokeAsync(() =>
                  {
                    assistantTextBlock.Text += character;
                    ChatScrollViewer.ScrollToEnd();
                  });
                  await Task.Delay(CharacterStreamDelay);
                }

                displayedCharacterCount = finalMessage.Length;
              }

              if (endIndex != -1)
              {
                assistantMessage.Content = finalMessage.Trim();
                break;
              }
            }
          }
          catch (JsonReaderException)
          {
            // Skip malformed lines silently
          }
        }
      }

      if (string.IsNullOrEmpty(assistantMessage.Content))
      {
        string finalRawResponse = streamBuffer.ToString();
        int startIndex = finalRawResponse.IndexOf(startMarker);
        if (startIndex != -1)
        {
          int textStartIndex = startIndex + startMarker.Length;
          assistantMessage.Content = finalRawResponse.Substring(textStartIndex).Trim();
        }
      }
    }

    private void AddMessageToUI(ChatMessage message)
    {
      var textBlock = CreateMessageTextBlock(message);
      ConversationStackPanel.Children.Add(textBlock);
      ChatScrollViewer.ScrollToEnd();
    }

    private TextBlock CreateMessageTextBlock(ChatMessage message)
    {
      var textBlock = new TextBlock
      {
        TextWrapping = TextWrapping.Wrap,
        Margin = new Thickness(5),
        Padding = new Thickness(10),
        MaxWidth = 500
      };

      switch (message.Role)
      {
        case "user":
          textBlock.Text = message.Content;
          textBlock.HorizontalAlignment = HorizontalAlignment.Right;
          textBlock.Background = new SolidColorBrush(Color.FromRgb(225, 245, 254)); // Light Blue
          textBlock.Foreground = Brushes.Black;
          break;
        case "assistant":
          textBlock.Text = message.Content;
          textBlock.HorizontalAlignment = HorizontalAlignment.Left;
          textBlock.Background = new SolidColorBrush(Color.FromRgb(241, 241, 241)); // Light Gray
          textBlock.Foreground = Brushes.Black;
          break;
        default: // error or system
          textBlock.Text = message.Content;
          textBlock.HorizontalAlignment = HorizontalAlignment.Center;
          textBlock.Background = Brushes.MistyRose;
          textBlock.Foreground = Brushes.DarkRed;
          break;
      }
      return textBlock;
    }
  }
}