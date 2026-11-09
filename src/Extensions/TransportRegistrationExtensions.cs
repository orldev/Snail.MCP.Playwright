using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.AspNetCore;
using Snail.MCP.Playwright.Application;
using Snail.MCP.Playwright.Tools;
using Snail.MCP.Playwright.Tools.Prompts;
using Snail.Sdk.Mcp.Hosting;
using Snail.Sdk.Mcp.Hosting.Discovery;
using Snail.Sdk.Mcp.Hosting.Sessions;
using Snail.Sdk.Mcp.Http;

namespace Snail.MCP.Playwright.Extensions;

/// <summary>Driving adapter: the tools reach the model over the transport the deployment asked for.</summary>
/// <remarks>
/// Which transport is wanted is said by calling for it, never inferred inside, so the two differ in one
/// thing only: at an address a request may carry a grant in its URL (<c>?tool=</c>), as a program there is no URL to
/// carry anything and every tool is the caller's.
/// </remarks>
public static class TransportRegistrationExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>The server answering calls at an address, each request on its own.</summary>
        /// <remarks>
        /// Stateless on purpose, not by the SDK's default (which it is since the 2026-07-28 revision): the
        /// platform's worker posts every call to one address and the server never asks a client anything back,
        /// so a session would be state kept for nobody, and a client of the sessionless revision is served the
        /// same way as one that still opens with initialize. The grant travels in the query of every request, so
        /// nothing is lost without a session: <c>WithSessionAddress</c> reads it from the request being served.
        /// Written down on 2026-10-16, when the audit found the mode inherited.
        /// </remarks>
        public IServiceCollection AddMcpTransportAtAnAddress()
        {
            services.AddMcpTools()
                .WithHttpTransport(http => http.SessionMode = HttpServerSessionMode.Stateless)
                .WithSessionAddress();

            return services;
        }

        /// <summary>The server speaking stdio to the process that started it.</summary>
        public IServiceCollection AddMcpTransportAsAProgram()
        {
            services.AddMcpTools().WithStdioServerTransport();

            return services;
        }

        /// <summary>
        /// The tools, behind the grant of the address, with their arguments checked before they run and every
        /// failure answered in the envelope.
        /// </summary>
        /// <remarks>
        /// Without the check a parameter the tool does not take was dropped without a word — a model writing
        /// <c>address</c> for <c>url</c> was answered as if it had sent nothing — and a missing one came back as the
        /// MCP SDK's bare error.
        /// </remarks>
        private IMcpServerBuilder AddMcpTools() =>
            services
                .AddMcpServer(server =>
                {
                    server.ServerInfo = ServerIdentity.Of(typeof(Health).Assembly);
                    server.ServerInstructions = ServerGuidance.Instructions;
                })
                .ExplainFailures()
                .WithSessionGrant()
                .WithArgumentCheck()
                .WithTools<SearchTools>()
                .WithTools<PageTools>()
                .WithTools<CloserTools>()
                .WithTools<DiagnosticsTools>();
    }
}
