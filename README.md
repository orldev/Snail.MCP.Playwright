# Snail.MCP.Playwright

An MCP server that lets an agent search the web and read pages through a real browser.

It answers two questions and keeps them apart: *where is this written about* — queries in, links out — and
*what does this page say* — an address in, its text out. A search never returns the content of a page, and
a read never takes a query. Everything else it offers is a way of reading more, or more closely.

It is built on [Snail.Toolkit.Web](https://github.com/orldev/Snail.Toolkit.Web) and a Playwright browser
server, `ghcr.io/orldev/ff-juggler`, which runs in a container of its own. The server holds no browser.

## Tools

| Tool | Question it answers | Cost |
| --- | --- | --- |
| `playwright_search` | Where is this written about? 1–5 queries → links with title, snippet and engine. | seconds |
| `playwright_fetch` | What does this page say? One address → its text, a piece at a time. | seconds |
| `playwright_fetch_many` | What do these pages say? Up to 10 addresses in one call. | tens of seconds |
| `playwright_fetch_site` | What does this site say? A bounded walk: sitemap first, links otherwise. | up to minutes |
| `playwright_extract` | Which pieces of this page? A CSS selector or an XPath → the text of each match, or one attribute. | free after a fetch |
| `playwright_extract_fields` | What are these values on this page? Named fields, each with selectors tried in order → a record, the selector that matched each field and the fields none found. | free after a fetch |
| `playwright_screenshot` | What does this page show? A JPEG of the screen or of the whole page. | seconds |
| `playwright_perform` | What does it say once the form is filled in? Up to 12 moves, then the resulting text. | seconds per move |
| `playwright_diagnose` | Is the browser behind the server alive? | instant |

Every answer is JSON with an `ok` flag. A failure carries `error`, a `hint` saying what that kind of failure
calls for, a `type` a program branches on, and whatever was learned before it; in the protocol it is flagged `isError`.

| `type` | When |
|---|---|
| `arguments` | the call cannot be served as written: a number out of range, an address that is not one, a move or an expression that is not valid, `from` beyond the end of the page |
| `privateAddress` | the address is behind the public web — loopback, a private range, a name with no dot — and is not read |
| `unanswered` | no search engine answered; each engine's failure is in `data.engines` |
| `nothingRead` | not one address of `playwright_fetch_many` was read; each one's failure is in `data.failed` |
| `blocked`, `timeout`, `navigation`, `browserUnavailable`, `proxy`, `robotsDisallowed`, `empty`, `unreadable`, `unexpected` | a page failed, by the toolkit's kind; `data.kind` keeps it as the toolkit spells it (`Blocked`) |

Four things about the answers are deliberate:

- **An empty answer explains itself.** "The engines answered and nothing matched" is an answer; "no engine
  answered" is an error that says when to ask again. A walk over a site says why it stopped.
- **A long page comes in pieces.** `next` is the position to pass as `from`; the pieces of one page read
  within ten minutes cost the site a single visit, and so does an `extract` after a `fetch`.
- **A page read again and again keeps its selectors.** `playwright_extract_fields` names the selector that
  matched each field; a collection repeated every month passes those first and a broader one behind them, and a
  site that was rebuilt shows up as fields under `missing` rather than as a record quietly half empty. CSS and
  XPath are named as such, never guessed from the shape of the string.
- **A refusal is not retried around.** The server spaces its requests to a host, waits as long as a site
  asks it to, and reports a wall as `blocked` with the advice to use another source.

## Run it

### Beside its browser, in Docker

```bash
docker compose -f deploy/compose.yml up -d --build
curl -i http://127.0.0.1:8822/healthz      # 200 once the browser answers
```

That stands up the server and the browser as a pair. A client opens sessions at
`http://127.0.0.1:8822/mcp`. The browser lives in a network of its own, where the only other thing is this
server — it opens whatever address a model asked for, and from there a link to an internal service leads
nowhere.

There is no token on `/mcp`: the address the server is published at is the whole of its access control,
which is why the compose file publishes it on the loopback.

`/healthz` asks whether the pair is alive, and asks it cheaply: a plain request at the browser server's
address, no browser rented, so a server busy with four pages still answers `200` at once. `playwright_diagnose`
asks the fuller question — it rents a browser and reports its version — and waits for a free one to do so.

### As a program, for one client

```jsonc
// .mcp.json — the browser container has to be running on this machine
{
  "mcpServers": {
    "playwright": {
      "command": "dotnet",
      "args": ["/path/to/Snail.MCP.Playwright/src/bin/Release/net10.0/Snail.MCP.Playwright.dll"]
    }
  }
}
```

```bash
docker run -d --name playwright-browser --init --shm-size=2g -p 3000:3000 ghcr.io/orldev/ff-juggler:153.0-pw1.62-amd64
```

With no port configured the server speaks stdio to the process that started it and connects to the browser
at `ws://localhost:3000/`.

## Configuration

The settings come from the settings file and from the environment variables under `SNAIL_MCP_PLAYWRIGHT_`, and
a variable wins over the file. A file named in `$SNAIL_MCP_PLAYWRIGHT_CONFIG` is the only one read, and when it is
missing no file is; otherwise the file is the first of these that exists: `./.s3l-mcp-playwright.json`,
`./s3l-mcp-playwright.json`, `~/.config/s3l-mcp-playwright/config.json`, `~/.s3l-mcp-playwright.json`. A machine's
settings, and above all anything secret such as a proxy that carries its credentials, belong in
`~/.config/s3l-mcp-playwright/config.json` (`chmod 600`), never in a launchd job or in a client's registration, which
travel wherever they are copied. Its fields are the variables' names after the prefix, a double underscore nesting:

```json
{ "Engines": "DuckDuckGo,Bing", "Web": { "Identity": { "Proxies": ["http://user:password@proxy:8080"] } } }
```

Every variable begins with `SNAIL_MCP_PLAYWRIGHT_`. A settings file may nest the server's own settings, the
endpoint's among them, under `Snail`; a key at the root wins. The server's own settings:

| Variable | Default | Meaning |
| --- | --- | --- |
| `PORT` | — | Port to answer sessions on at `/mcp`; unset or `0` means stdio. Inside a container the port is opened on every interface, anywhere else on loopback. |
| `TRANSPORT` | — | `Http` or `Stdio`, said outright; with `Http` the server listens at `URL`. |
| `URL` | `http://127.0.0.1:8822/` | Where HTTP listens when `TRANSPORT` is `Http`; a `URL` alone does not mean HTTP. Outside a container it must be a loopback address. |
| `LISTENS_ON_EVERY_INTERFACE` | `false` | Refused outside a container, as a `0.0.0.0` URL or one beyond loopback is: the server has no token, so it opens the network only from inside its image. |
| `ENGINES` | `DuckDuckGo,Bing,Brave` | Search engines to ask. `Google` exists and is off by default: it answers by address, not by query. |
| `SEARCH_SECONDS` | `60` | How long one search may take; an engine that does not open in time is not asked and says so. |
| `ALLOW_PRIVATE_ADDRESSES` | `false` | Whether addresses inside the deployment's own network may be read. |
| `PAGE_MEMORY_MINUTES` | `10` | How long a page that was read is remembered; `0` forgets at once. |
| `PAGE_MEMORY_PAGES` | `16` | Pages remembered at once. |
| `PAGE_MEMORY_MEGABYTES` | `64` | Megabytes the remembered pages may hold together; the oldest give way first. |
| `BROWSER_PROBE_SECONDS` | `10` | How long `playwright_diagnose` waits for the browser. |

The toolkit's settings travel under the same prefix: `WEB__SECTION__NAME` is the toolkit's
`Web:Section:Name`. The ones a deployment is likely to touch:

| Variable | Default | Meaning |
| --- | --- | --- |
| `WEB__SCRAPING__BROWSER_SERVER` | `ws://localhost:3000/` | The Playwright browser server. The image sets `ws://browser:3000/`. |
| `WEB__IDENTITY__FINGERPRINTS__0__USER_AGENT` | Firefox 153.0 on Linux | Has to name the browser that is really behind the server. |
| `WEB__IDENTITY__FINGERPRINTS__0__TIMEZONE` | `America/New_York` | Moves together with `LOCALE` and `ACCEPT_LANGUAGE` when the exit moves. |
| `WEB__POLITENESS__MIN_DELAY_PER_HOST` | `00:00:01` | The gap left between two requests to one host. |
| `WEB__ROBOTS__IS_OBEYED` | `false` | Whether `robots.txt` is obeyed when reading a page. |
| `WEB__SCRAPING__NAVIGATION_TIMEOUT_MS` | `45000` | How long one page may take to load. |
| `WEB__IDENTITY__PROXIES__0` | — | An exit to go out through, for the browser and the HTTP engine alike. |

Two of these are a pair that must not be split. The browser image and the Playwright client inside the
toolkit have to be the same Playwright version — the image tag says which, `…-pw1.62-…` — and the user
agent has to name the browser in the image. The toolkit refuses to start on a mismatch of the second kind;
a mismatch of the first is answered `428` by the browser server.

Only public `http` and `https` addresses are read. A literal address in a private range, the loopback and
a host name without a dot are refused before anything is sent, because `robots.txt` and the sitemap are
fetched by this process, which stands inside the deployment's network. A public name that *resolves* to a
private address is not caught by that rule; the browser's own network is what answers it.

## Snail.Swarm.Agent

The platform reaches the server at an address and names the tools a role may use in it
(`…/mcp?tool=playwright_search&tool=playwright_fetch`); the server lists those alone and refuses anything else at
the door, in the answer envelope with `type` `notGranted`.
What the platform needs is in `deploy/swarm/`:

- `catalog-entry.json` — the entry for `src/Dashboard/Mcp/catalog.json`;
- `compose.yml` — the two services for `deploy/compose.yml`, the browser in its own network.

Two facts measured here that the platform's defaults do not cover. The server needs **512 MB**: it idles
at 105 MB and reached 255 MB after one search and five pages, of which the Playwright driver is 82 MB —
the generic Nomad template gives 256. And the browser is a container the generic template does not start:
under Nomad it needs a job of its own, the way the sandbox has one.

A role that researches needs three tools, not eight — every declared tool is paid for in every step:

```json
{ "servers": ["playwright"], "tools": ["playwright.playwright_search", "playwright.playwright_fetch", "playwright.playwright_fetch_many"] }
```

## Developing

```bash
dotnet build
dotnet test                                                    # no browser needed
SNAIL_LIVE_BROWSER_SERVER=ws://localhost:3000/ dotnet test     # adds the live checks
```

Snail.Toolkit.Web comes from NuGet, as every other package does.

What a model reads lives in `src/Tools/Prompts/`: `ToolDescriptions.cs` before a call, `Messages.cs` after
one, `ServerGuidance.cs` once at the start. Guard tests hold those texts to the code — every limit a
description states, every failure kind the toolkit can report, every move `playwright_perform` accepts —
and the tool list has a budget, because it is read into every session whether or not a tool is used. The
budget is 6144 characters over the names and descriptions of the tools and their parameters, and the nine
tools stand two characters under it: a word added to any description starts with a word taken out elsewhere.

## License

MIT. See [LICENSE](LICENSE).
