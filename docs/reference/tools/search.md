# Searching the web

## <code>playwright_<wbr>search</code>

Find where something is written about: queries in, links out — title, address, a short snippet and
the engine that offered each. It never returns the content of a page; read a result with
playwright_fetch. Several phrasings go in one call.
The engines field says what each engine did: an empty list when engines answered means nothing
matched, while no engine answering is an error that says when to ask again.

*read-only · every call adds · may reach outside services.*

| Parameter | Type | Required | Default | Description |
| --- | --- | --- | --- | --- |
| `queries` | `array` | yes | — | 1 to 5 search queries, each up to 400 characters. Plain words work with every engine; operators such as site: or filetype: only with the engines read through the browser. |
| `limit` | `integer` | — | `10` | How many links to return, 1 to 30; 10 by default. |

<details><summary>JSON Schema</summary>

```json
{
  "type": "object",
  "properties": {
    "queries": {
      "description": "1 to 5 search queries, each up to 400 characters. Plain words work with every engine; operators such as site: or filetype: only with the engines read through the browser.",
      "type": "array",
      "items": {
        "type": "string"
      }
    },
    "limit": {
      "description": "How many links to return, 1 to 30; 10 by default.",
      "type": "integer",
      "default": 10
    }
  },
  "required": [
    "queries"
  ]
}
```

</details>

