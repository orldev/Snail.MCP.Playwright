using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Snail.MCP.Playwright.Configuration;
using Snail.Sdk.Mcp.Hosting;

namespace Snail.MCP.Playwright.Extensions;

/// <summary>Composition root: lists the features to wire; how the web is reached is known one level down.</summary>
public static class McpServerRegistrationExtensions
{
    extension(IHostApplicationBuilder builder)
    {
        /// <summary>Configures the server to answer sessions at an address.</summary>
        public IHostApplicationBuilder ConfigureMcpServerAtAnAddress(McpEndpoint endpoint, ServerConfig config, IConfiguration settings)
        {
            builder.Services
                .AddSingleton(endpoint)
                .AddSingleton(config)
                .AddBrowsing(config, settings)
                .AddMcpTransportAtAnAddress();

            return builder;
        }

        /// <summary>Configures the server to speak stdio to the process that started it.</summary>
        public IHostApplicationBuilder ConfigureMcpServerAsAProgram(McpEndpoint endpoint, ServerConfig config, IConfiguration settings)
        {
            builder.Services
                .AddSingleton(endpoint)
                .AddSingleton(config)
                .AddBrowsing(config, settings)
                .AddMcpTransportAsAProgram();

            return builder;
        }
    }
}
