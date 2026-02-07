# MCP Server Integration - Implementation Summary

## Overview

This document provides a technical summary of the MCP (Model Context Protocol) server integration implemented in DarkClippy. The implementation is **self-contained** with no external SDK dependencies - it uses a custom stdio-based client that works out-of-the-box.

## Design Philosophy

**Simple & Self-Contained**: No MCP Gateway, no external reverse proxy, no complicated setup. Just enable a server in configuration and it works.

**Custom Implementation**: Uses custom JSON-RPC over stdio communication instead of relying on preview SDKs with unstable APIs.

**Standard .NET Libraries**: Only uses System.Diagnostics.Process, HttpClient, and JSON serialization - no third-party MCP packages.

## Implementation Details

### Architecture

The implementation follows clean architecture principles with clear separation of concerns:

```
┌─────────────────────────────────────────────────────────┐
│                     ClippyWeb                            │
│  - LlmSetup.cs: Initializes MCP servers at startup      │
│  - appsettings.json: Configuration for MCP servers      │
└──────────────────────┬──────────────────────────────────┘
                       │
                       v
┌─────────────────────────────────────────────────────────┐
│              SemanticKernelHelper                        │
│  - ChatClientFactory: Creates clients with MCP plugins  │
│  - SemanticKernelClient: Integrates MCP as plugins      │
│  - McpServerRegistry: Manages MCP server instances      │
│  - StdioMcpServer: Custom stdio MCP implementation      │
│  - McpModels: JSON-RPC protocol models                  │
└──────────────────────┬──────────────────────────────────┘
                       │
                       v
┌─────────────────────────────────────────────────────────┐
│               SharedInterfaces                           │
│  - IMcpServer: MCP server contract                      │
│  - IMcpServerRegistry: Registry contract                │
│  - McpServerConfiguration: Configuration model          │
└─────────────────────────────────────────────────────────┘
```

### Key Components

#### 1. Configuration (McpServerConfiguration)
- Defines MCP server configuration including command, arguments, and environment variables
- Supports stdio-based servers with process spawning
- Allows enabling/disabling servers via configuration (disabled by default)
- No external Gateway or proxy configuration needed

#### 2. MCP Server Interface (IMcpServer)
- Abstraction for MCP server implementations
- Defines contract for server initialization and tool discovery
- Includes CreatePluginAsync for Semantic Kernel integration
- Supports IAsyncDisposable for proper cleanup

#### 3. Stdio MCP Server (StdioMcpServer)
- **Custom implementation** - no external SDK dependencies
- Manages MCP server process lifecycle (spawn, communicate, dispose)
- Implements JSON-RPC 2.0 protocol over stdin/stdout
- Creates Semantic Kernel plugins from MCP tools automatically
- Handles Windows-specific npx resolution (.cmd extension)
- Thread-safe communication via SemaphoreSlim
- Sanitizes plugin names to comply with Semantic Kernel requirements

#### 4. MCP Protocol Models (McpModels.cs)
- JSON-RPC request/response structures
- MCP tool definitions with input schemas
- Tool call parameters and results
- All using System.Text.Json serialization

#### 5. MCP Server Registry (McpServerRegistry)
- Thread-safe registry for managing multiple MCP servers
- Supports filtering by enabled status
- Singleton lifetime in DI container

#### 6. Integration with Semantic Kernel
- MCP servers are converted to Semantic Kernel plugins via CreatePluginAsync
- Each MCP tool becomes a KernelFunction
- Plugins are added to the kernel at client creation time
- Tools become available to Dark Clippy during conversations
- Function invocation calls back to MCP server via JSON-RPC

### Configuration Example

```json
{
  "McpServers": [
    {
      "Name": "weather",
      "Description": "Provides weather forecasts and geolocation",
      "ServerType": "stdio",
      "Command": "npx",
      "Arguments": [ "-y", "@modelcontextprotocol/server-weather" ],
      "Enabled": true
    }
  ]
}
```

### Startup Flow

1. Application starts, reads `appsettings.json`
2. `InitializeMcpServersAsync` in `LlmSetup.cs` runs:
   - Creates temporary service provider for logger
   - Loads MCP server configurations from appsettings
   - For each enabled server:
     - Creates StdioMcpServer instance
     - Calls InitializeAsync (spawns process)
     - Registers in McpServerRegistry
   - Returns registry as singleton
3. `ChatClientFactory` receives MCP registry via DI
4. When creating a chat client:
   - Factory gets enabled MCP servers from registry
   - Passes servers to `SemanticKernelClient` constructor
   - For each server, calls CreatePluginAsync
   - Plugins are added to kernel
   - LLM can now invoke MCP tools

### Communication Flow

```
User -> DarkClippy -> LLM -> Semantic Kernel
                                    |
                                    v
                            MCP Tool Function
                                    |
                                    v
                            StdioMcpServer.CallToolAsync
                                    |
                                    v
                            JSON-RPC Request -> stdin
                                    |
                                    v
                            MCP Server Process (npx)
                                    |
                                    v
                            JSON-RPC Response <- stdout
                                    |
                                    v
                            Parse & Return Result
                                    |
                                    v
                            Back to LLM -> User
```

### Extensibility

The design is highly extensible:

**Adding a new MCP server:**
1. Add configuration entry to `appsettings.json`
2. Set `Enabled: true`
3. Restart application

No code changes, no SDK updates, no Gateway configuration!

**Supporting new transport types:**
1. Create new class implementing `IMcpServer`
2. Add initialization logic in `LlmSetup.InitializeMcpServersAsync`

**Custom MCP tools:**
1. MCP servers define their own tools via JSON-RPC
2. StdioMcpServer automatically discovers and registers them
3. No code changes needed in DarkClippy

### Security Considerations

- API keys stored in environment variables (not hardcoded in appsettings.json)
- Servers run in separate processes (process isolation)
- Servers disabled by default (opt-in security model)
- Configuration validation at startup
- Errors logged but don't crash application
- Proper disposal of resources via IAsyncDisposable

### Testing

Added comprehensive test coverage:
- `McpServerRegistryTests`: 5 tests for registry functionality
- `StdioMcpServerTests`: 4 tests for server implementation
- All existing tests still pass (83 total)
- Integration tests verify end-to-end MCP tool invocation

### Performance Considerations

- MCP servers initialized once at startup (not per request)
- Registry is a singleton (shared across application)
- Chat clients cached per session
- Process reuse for stdio servers (long-running)
- JSON serialization overhead minimal (System.Text.Json)
- Minimal overhead when MCP disabled (early exit in registry)

### Error Handling

- Graceful degradation: Failed MCP server initialization doesn't crash app
- Detailed logging for troubleshooting (Information, Warning, Error levels)
- Each server error isolated from others
- User-friendly error messages
- 30-second timeout on JSON-RPC calls
- Process cleanup on disposal even if errors occur

## Technology Choices

### Why Custom Implementation Instead of SDK?

1. **Stability**: Preview SDKs have unstable APIs
2. **Simplicity**: JSON-RPC over stdio is straightforward
3. **Control**: Full control over process management
4. **Dependencies**: Zero third-party MCP packages
5. **Works Today**: No waiting for SDK maturity

### Why No Gateway?

1. **Complexity**: Gateway adds deployment/configuration overhead
2. **Out-of-box**: User said they want it to "just work"
3. **Development**: Simpler for developers to clone and run
4. **Dependencies**: One less moving part to manage

## Dependencies

**Zero new external dependencies added:**
- Uses existing Semantic Kernel infrastructure
- Uses built-in System.Diagnostics.Process for process management
- Uses System.Text.Json for JSON-RPC serialization
- Configuration via existing appsettings.json

**Runtime requirements:**
- Node.js + npm (for npx-based MCP servers)
- Internet connection (for first-time package download)

## Backward Compatibility

- MCP integration is fully optional
- Default configuration has all servers disabled
- Existing functionality unaffected
- No breaking changes to public APIs
