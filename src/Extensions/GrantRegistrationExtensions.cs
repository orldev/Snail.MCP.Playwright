using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Snail.MCP.Playwright.Application;

namespace Snail.MCP.Playwright.Extensions;

/// <summary>The grant of a request, checked where calls arrive rather than inside each tool.</summary>
public static class GrantRegistrationExtensions
{
    extension(IMcpServerBuilder builder)
    {
        /// <summary>Refuses a tool the request was not granted, whoever forwarded the call.</summary>
        /// <remarks>
        /// At the door, and not tool by tool: a defence that lives in the tools holds only until somebody
        /// adds one that forwards calls. Here every call passes the same check.
        /// </remarks>
        public IMcpServerBuilder RefuseWhatWasNotGranted()
        {
            builder.WithRequestFilters(filters => filters.AddCallToolFilter(Refusing));

            return builder;
        }
    }

    /// <summary>The call handed on when the request was granted the tool, a refusal naming the grant otherwise.</summary>
    private static McpRequestHandler<CallToolRequestParams, CallToolResult> Refusing(McpRequestHandler<CallToolRequestParams, CallToolResult> next) =>
        async (context, cancellationToken) =>
        {
            var grant = context.Server.Services?.GetService<RequestGrant>();

            if (context.Params?.Name is not { } name || grant is null || grant.Allows(name))
                return await next(context, cancellationToken);

            var refusal = new CallToolResult
            {
                IsError = true,
                Content = [new TextContentBlock { Text = grant.Refusal(name) }]
            };

            return refusal;
        };
}
