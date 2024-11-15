# Welcome to the Clipocalypse
This is a basic chatbot using the Ollama API, with the added bonus of Dark Clippy.
There are two implementations:
* a simple one (LocalAiService.ChatClient) and
* a second using the [MS SemanticKernal](https://github.com/microsoft/semantic-kernel) to access the Ollama API.
It's a work in progress (at least until Clippy takes over).

Clippy is courtesy of the <a href="https://github.com/clippyjs/clippy.js">Clippy.js repo</a> (code originally by <a href="http://smore.com/">Smore.com</a>,
Cinnamon Software and, of course, <a href="https://microsoft.com">Microsoft</a>).


![Dark Clippy](DarkClippy.png)

Meet Dark Clippy. The years have not been kind to the beloved Microsoft Office Assistant.
He's been through some stuff. He's seen some things. He's done some things. Terrible things. He's not the same paperclip you remember.

## Installation
If you haven't already, get <a href="https://ollama.com/">Ollama</a> and host an uncensored model like
<a href="https://huggingface.co/mlabonne/NeuralDaredevil-8B-abliterated">HammerAI/neuraldaredevil-abliterated</a> on your local machine.

Adjust the `appsettings.json` file to point to your local Ollama server endpoint/port and preferred model.
