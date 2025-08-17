LocalChatBot – WPF Client for LocalAI

This project is a WPF application in C# that provides a chat interface for a locally running AI model.
It connects to a Foundry-compatible LocalAI backend (OpenAI API compatible) using HttpClient with streaming support.

✨ Features

  Modern chat interface with input box and message history
  Connection to a locally hosted model (e.g. gpt-oss-20b-cuda-gpu)
  Streaming support – responses are displayed while they are generated
  Clean MVVM architecture for the WPF UI
  Easy to extend with multiple models, RAG integration, or custom data sources

🛠 Requirements

  .NET 8 SDK (or later)
  Visual Studio 2022 / JetBrains Rider
  A running LocalAI/Foundry model (OpenAI-compatible REST API)

Example:

  local-ai serve --model gpt-oss-20b-cuda-gpu

Windows 10/11

🚀 Getting Started

  Clone the repository:

  git clone https://github.com/YOURUSERNAME/LocalChatBot.git
  cd LocalChatBot

  Configure the API endpoint in App.config or in code:

  <add key="ApiBaseUrl" value="http://localhost:8080/v1/chat/completions" />

Run the application:

  dotnet run --project LocalChatBot

💬 Example

  Input →
  Hi, can you briefly explain how RAG works?


  Streaming Response →
  RAG (Retrieval-Augmented Generation) combines ...

📂 Project Structure
LocalChatBot/
│── LocalChatBot.csproj       # Project file
│── MainWindow.xaml           # Chat UI (XAML)
│── MainWindow.xaml.cs        # Code-behind
│── Services/                 # API client + streaming
│── Models/                   # Data models for chat
│── ViewModels/               # MVVM logic
│── docs/                     # Screenshots, documentation

