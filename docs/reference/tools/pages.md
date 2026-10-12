# Reading pages and sites

## <code>playwright_<wbr>fetch</code>

Read one page: its address in, its title and readable text out, read by a real browser as a
person sees it. A long text comes in pieces: next is the position to pass as from for the following piece, and null when
the page has ended; the pieces of a page read within 10 minutes cost the site a single visit. A
failure names its kind — blocked, forbidden by robots.txt, empty, timed out — and says what to do
instead of repeating the call.

*read-only · every call adds · may reach outside services.*

| Parameter | Type | Required | Default | Description |
| --- | --- | --- | --- | --- |
| `url` | `string` | yes | — | Absolute http or https address of a public page. |
| `from` | `integer` | — | `0` | Position in the text to start at: 0 by default, or the next of the previous piece. |
| `maxCharacters` | `integer` | — | `20000` | Characters of text to return, 500 to 100000; 20000 by default. |

<details><summary>JSON Schema</summary>

```json
{
  "type": "object",
  "properties": {
    "url": {
      "description": "Absolute http or https address of a public page.",
      "type": "string"
    },
    "from": {
      "description": "Position in the text to start at: 0 by default, or the next of the previous piece.",
      "type": "integer",
      "default": 0
    },
    "maxCharacters": {
      "description": "Characters of text to return, 500 to 100000; 20000 by default.",
      "type": "integer",
      "default": 20000
    }
  },
  "required": [
    "url"
  ]
}
```

</details>

## <code>playwright_<wbr>fetch_<wbr>many</code>

Read several pages in one call: addresses in, a piece of each page's text out, and a page that
failed listed beside the others rather than failing the call. Use it after a search, to read the
results worth reading at once. A page cut short carries next: pass it to playwright_fetch as from
to read on.

*read-only · every call adds · may reach outside services.*

| Parameter | Type | Required | Default | Description |
| --- | --- | --- | --- | --- |
| `urls` | `array` | yes | — | 1 to 10 absolute http or https addresses of public pages. |
| `maxCharactersEach` | `integer` | — | `4000` | Characters of text to return for each page, 500 to 20000; 4000 by default. |

<details><summary>JSON Schema</summary>

```json
{
  "type": "object",
  "properties": {
    "urls": {
      "description": "1 to 10 absolute http or https addresses of public pages.",
      "type": "array",
      "items": {
        "type": "string"
      }
    },
    "maxCharactersEach": {
      "description": "Characters of text to return for each page, 500 to 20000; 4000 by default.",
      "type": "integer",
      "default": 4000
    }
  },
  "required": [
    "urls"
  ]
}
```

</details>

## <code>playwright_<wbr>fetch_<wbr>site</code>

Read a site rather than a page: one address to start from, and the pages of that site are found in
its own sitemap, or by following its links when it publishes none. It is the expensive tool,
minutes and a visit per page: use it only when the question is about a
site as a whole, and playwright_fetch_many when the addresses are already known. The answer says
why it stopped: any stop but Exhausted means the site has more than was read.

*read-only · every call adds · may reach outside services.*

| Parameter | Type | Required | Default | Description |
| --- | --- | --- | --- | --- |
| `url` | `string` | yes | — | Address to start from, which also decides the site: pages on other hosts are not read. |
| `maxPages` | `integer` | — | `10` | Pages to read at most, 1 to 50; 10 by default. |
| `maxDepth` | `integer` | — | `2` | Links from the start to follow when the site has no sitemap, 0 to 5; 2 by default. |
| `seconds` | `integer` | — | `120` | Seconds the walk may take, 30 to 600; 120 by default. What was read when they run out is returned. |
| `maxCharactersEach` | `integer` | — | `2000` | Characters of text to return for each page, 500 to 20000; 2000 by default. |

<details><summary>JSON Schema</summary>

```json
{
  "type": "object",
  "properties": {
    "url": {
      "description": "Address to start from, which also decides the site: pages on other hosts are not read.",
      "type": "string"
    },
    "maxPages": {
      "description": "Pages to read at most, 1 to 50; 10 by default.",
      "type": "integer",
      "default": 10
    },
    "maxDepth": {
      "description": "Links from the start to follow when the site has no sitemap, 0 to 5; 2 by default.",
      "type": "integer",
      "default": 2
    },
    "seconds": {
      "description": "Seconds the walk may take, 30 to 600; 120 by default. What was read when they run out is returned.",
      "type": "integer",
      "default": 120
    },
    "maxCharactersEach": {
      "description": "Characters of text to return for each page, 500 to 20000; 2000 by default.",
      "type": "integer",
      "default": 2000
    }
  },
  "required": [
    "url"
  ]
}
```

</details>

