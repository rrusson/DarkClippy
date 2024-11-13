# Clipocalypse Now
This is a basic chatbot using the Ollama API, with the added bonus of Dark Clippy.
There are two implementations:
* a simple one (LocalAiService.ChatClient) and
* a second using the [MS SemanticKernal](https://github.com/microsoft/semantic-kernel) to access the Ollama API.
It's a work in progress (at least until Clippy takes over).

![Dark Clippy](DarkClippy.png)

Meet Dark Clippy. The years have not been kind to the beloved Microsoft Office Assistant.
He's been through some stuff. He's seen some things. He's done some things. Terrible things. He's not the same paperclip you remember.

## Installation
Adjust the `appsettings.json` file to point to your local Ollama server endpoint and preferred model (hint: use an abliterated model for full effect). Publish the output of ClippyWeb to IIS as a web application.
