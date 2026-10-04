# The server as a container: one per machine, beside the browser it reads the web through.
#
#   docker build -t snail/s3l-playwright-mcp .
#   docker run -p 8822:8822 -e SNAIL_MCP_PLAYWRIGHT_WEB__SCRAPING__BROWSER_SERVER=ws://browser:3000/ snail/s3l-playwright-mcp
#
# There is no browser in this image, and that is the point: it holds the Playwright client and
# connects to a browser server — ghcr.io/orldev/ff-juggler — that runs in a container of its own, in
# a network of its own. deploy/compose.yml stands the two up together.

# Pinned to the band global.json asks for: on the floating tag a newer feature band refuses the build.
FROM mcr.microsoft.com/dotnet/sdk:10.0.302 AS build
ARG TARGETARCH
WORKDIR /source

COPY . .

# The architecture is named so that Playwright ships the one node driver this image can run, rather
# than the drivers of every platform it supports: about a hundred megabytes of the difference.
RUN dotnet publish src/Snail.MCP.Playwright.csproj -c Release -o /app -a "$TARGETARCH" --self-contained false -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0.11
WORKDIR /app
COPY --from=build /app .

# Playwright marks its node driver executable when it first starts, which only the owner of the file
# may do. The server runs as an unprivileged user, so the bit is set here — without it every call that
# needs the browser fails with «Permission denied» starting /app/.playwright/node/…/node.
RUN chmod +x /app/.playwright/node/*/node

# Every request at Information buries what matters in a container's log; the server's own warnings stay.
ENV SNAIL_MCP_PLAYWRIGHT_PORT=8822 \
    SNAIL_MCP_PLAYWRIGHT_WEB__SCRAPING__BROWSER_SERVER=ws://browser:3000/ \
    Logging__LogLevel__Default=Information \
    Logging__LogLevel__Microsoft.AspNetCore=Warning \
    Logging__LogLevel__ModelContextProtocol=Warning

EXPOSE 8822
USER app

# What the platform reads to fill in its registry entry: the port and path a session is opened at, the
# tool that tells a live server from one whose browser is missing, and what an operator has to provide.
LABEL mcp.name="playwright" \
      mcp.port="8822" \
      mcp.path="/mcp" \
      mcp.secrets="" \
      mcp.description="Search the web and read pages through a real browser: links for a query, the text of a page, several pages at once and a bounded walk over a site." \
      mcp.diagnose="playwright_diagnose" \
      mcp.requires="A Playwright browser server beside it: the ghcr.io/orldev/ff-juggler:153.0-pw1.62-amd64 container, reachable at the address in SNAIL_MCP_PLAYWRIGHT_WEB__SCRAPING__BROWSER_SERVER." \
      mcp.env.SNAIL_MCP_PLAYWRIGHT_WEB__SCRAPING__BROWSER_SERVER="ws://browser:3000/"

ENTRYPOINT ["dotnet", "Snail.MCP.Playwright.dll"]
