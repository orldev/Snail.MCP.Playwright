# Tools

9 tools in 4 families.
The names, descriptions and schemas are generated from the server itself, so they are exactly what the model receives in `tools/list`.

An MCP server that lets an agent search the web and read pages through a real browser. It answers two questions and keeps them apart: where is this written about (queries in, links out) and what does this page say (an address in, its text out). A search never returns the content of a page, and a read never takes a query. Everything else it offers is a way of reading more, or more closely.

Every answer is JSON with an `ok` flag. A failure carries `error`, a `hint` saying what that kind of failure calls for, a `type` a program branches on — `arguments`, `privateAddress`, `unanswered`, `nothingRead`, or the kind of a page that failed, such as `blocked` or `robotsDisallowed` — and whatever was learned before it; in the protocol it is flagged `isError`.

| Tool | Family | What it does |
| --- | --- | --- |
| [`playwright_search`](search.md#playwright_search) | Searching the web | Find where something is written about: queries in, links out — title, address, a short snippet and the engine that offered each. |
| [`playwright_fetch`](pages.md#playwright_fetch) | Reading pages and sites | Read one page: its address in, its title and readable text out, read by a real browser as a person sees it. |
| [`playwright_fetch_many`](pages.md#playwright_fetch_many) | Reading pages and sites | Read several pages in one call: addresses in, a piece of each page's text out, and a page that failed listed beside the others rather than failing the call. |
| [`playwright_fetch_site`](pages.md#playwright_fetch_site) | Reading pages and sites | Read a site rather than a page: one address to start from, and the pages of that site are found in its own sitemap, or by following its links when it publish… |
| [`playwright_extract`](closer.md#playwright_extract) | Reading a page more closely | Take pieces out of a page instead of its whole text: a CSS selector or an XPath in, exactly one, and the text of each element it matches out — or one attribu… |
| [`playwright_extract_fields`](closer.md#playwright_extract_fields) | Reading a page more closely | Take a record of named values out of a page. |
| [`playwright_screenshot`](closer.md#playwright_screenshot) | Reading a page more closely | Look at a page instead of reading it: an address in, a picture of what the browser drew out. |
| [`playwright_perform`](closer.md#playwright_perform) | Reading a page more closely | Use a page instead of only reading it: open an address, make a few moves on it, and get the text of the page that results. |
| [`playwright_diagnose`](diagnostics.md#playwright_diagnose) | Diagnostics | Say what state this server is in: its version, whether the browser it reads pages through answers, and which search engines it asks. |
