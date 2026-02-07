# MCP Server Integration Guide

## Overview

DarkClippy supports integration with MCP (Model Context Protocol) servers, allowing you to extend Clippy's capabilities with external tools for weather, news, geolocation, and more. The implementation uses a simple, self-contained approach with **no external dependencies** - just configure and run!

## How It Works

DarkClippy uses a custom stdio-based client that:
1. Spawns MCP server processes (like `npx @modelcontextprotocol/server-weather`)
2. Communicates via JSON-RPC over stdin/stdout
3. Converts MCP tools into Semantic Kernel plugins
4. Makes them available to Dark Clippy during conversations

**No Gateway Required!** The solution works out-of-the-box when you pull the repo - just enable a server in configuration and it will automatically start.

## Prerequisites

To use MCP servers, you need:
- **Node.js and npm** (for npx-based MCP servers)
- **Internet connection** (for npx to download packages on first run)

That's it! No reverse proxy setup, no Gateway installation, no complicated configuration.

## Configuration

MCP servers are configured in `appsettings.json` under the `McpServers` section. Each server configuration includes:

- **Name**: Unique identifier for the server
- **Description**: Human-readable description of the server's capabilities
- **ServerType**: Type of server transport (currently supports "stdio")
- **Command**: Command to execute to start the MCP server (e.g., "npx")
- **Arguments**: Array of arguments to pass to the command
- **EnvironmentVariables**: Dictionary of environment variables (optional)
- **WorkingDirectory**: Working directory for the server process (optional)
- **Enabled**: Boolean flag to enable/disable the server (default: false for safety)

### Example Configuration

```json
{
  "McpServers": [
    {
      "Name": "weather",
      "Description": "Provides weather forecasts, current conditions, and geolocation services",
      "ServerType": "stdio",
      "Command": "npx",
      "Arguments": [ "-y", "@modelcontextprotocol/server-weather" ],
      "Enabled": true
    },
    {
      "Name": "brave-search",
      "Description": "Web search and news using Brave Search API",
      "ServerType": "stdio",
      "Command": "npx",
      "Arguments": [ "-y", "@modelcontextprotocol/server-brave-search" ],
      "EnvironmentVariables": {
        "BRAVE_API_KEY": "your-api-key-here"
      },
      "Enabled": false
    }
  ]
}
```

## Available MCP Servers

### Weather Server

**Package**: `@modelcontextprotocol/server-weather`

Provides:
- Current weather conditions
- Weather forecasts
- Geolocation services

**Installation**: None required (uses npx)

**Configuration**:
```json
{
  "Name": "weather",
  "Description": "Provides weather forecasts, current conditions, and geolocation services",
  "ServerType": "stdio",
  "Command": "npx",
  "Arguments": [ "-y", "@modelcontextprotocol/server-weather" ],
  "Enabled": true
}
```

### Brave Search Server

**Package**: `@modelcontextprotocol/server-brave-search`

Provides:
- Web search
- News search
- Local search

**Prerequisites**: Brave Search API key (get one at https://brave.com/search/api/)

**Configuration**:
```json
{
  "Name": "brave-search",
  "Description": "Web search and news using Brave Search API",
  "ServerType": "stdio",
  "Command": "npx",
  "Arguments": [ "-y", "@modelcontextprotocol/server-brave-search" ],
  "EnvironmentVariables": {
    "BRAVE_API_KEY": "your-api-key-here"
  },
  "Enabled": true
}
```

## Adding New MCP Servers

To add a new MCP server:

1. Find an MCP server package (see [MCP Market](https://mcpmarket.com/server) for available servers)
2. Add a new configuration entry to the `McpServers` array in `appsettings.json`
3. Set `Enabled` to `true`
4. Restart the application

### Example: Adding Google Maps Server

```json
{
  "Name": "google-maps",
  "Description": "Location services and directions",
  "ServerType": "stdio",
  "Command": "npx",
  "Arguments": [ "-y", "@modelcontextprotocol/server-google-maps" ],
  "EnvironmentVariables": {
    "GOOGLE_MAPS_API_KEY": "your-api-key-here"
  },
  "Enabled": true
}
```

## Disabling MCP Servers

To disable an MCP server, set `Enabled` to `false` in the configuration:

```json
{
  "Name": "weather",
  "Enabled": false
}
```

## Troubleshooting

### Server Fails to Initialize

Check the application logs for detailed error messages. Common issues:

1. **Missing command**: Ensure `npx` is installed and available in PATH
2. **Invalid package**: Verify the package name is correct
3. **Missing API keys**: Check that required environment variables are set
4. **Network issues**: Some servers require internet connectivity

### Server Not Responding

- Verify the server process is running
- Check for errors in the application logs
- Try restarting the application

## Architecture

The MCP integration uses the following components:

- **IMcpServer**: Interface for MCP server implementations
- **IMcpServerRegistry**: Registry for managing multiple MCP servers
- **StdioMcpServer**: Implementation for stdio-based MCP servers
- **McpServerConfiguration**: Configuration model for MCP servers
- **Integration with Semantic Kernel**: MCP servers are exposed as Semantic Kernel plugins

MCP servers are initialized at application startup and registered as plugins in the Semantic Kernel, making their tools available to Dark Clippy during conversations.

## Security Considerations

- Store API keys in environment variables or secure configuration providers, not directly in `appsettings.json`
- Only enable MCP servers from trusted sources
- Review the capabilities of each MCP server before enabling
- Use separate API keys for development and production environments
