#  CS2 Highlight Automator for OBS

A WPF desktop application that automatically finds multi-kill highlights from your Counter-Strike 2 demo files and records them using OBS Studio.

##  Features

* **Automatic Highlight Detection:** Parses CS2 demo files (`.dem`) to find kill streaks.
* **OBS Integration:** Connects to OBS via the WebSocket plugin to automatically start and stop recording for each highlight.
* **Detailed Information:** Extracts round number, total damage dealt, and weapons used for each highlight.
  


### Prerequisites

* [.NET 8.0 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
* [OBS Studio](https://obsproject.com/)
* [OBS-Websocket Plugin](https://github.com/obsproject/obs-websocket/releases) (usually included with recent versions of OBS Studio)

