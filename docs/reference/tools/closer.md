# Reading a page more closely

## <code>playwright_<wbr>extract</code>

Take pieces out of a page instead of its whole text: a CSS selector or an XPath in, exactly one, and
the text of each element it matches out — or one attribute of each, such as href. For the rows of a
table or the links of a list. A page read within 10 minutes is not visited again.

*read-only · every call adds · may reach outside services.*

| Parameter | Type | Required | Default | Description |
| --- | --- | --- | --- | --- |
| `url` | `string` | yes | — | Absolute http or https address of a public page. |
| `xpath` | `string?` | — | `null` | XPath 1.0 over the rendered markup, lower-case names: //table[@id='prices']//tr, //h2. |
| `css` | `string?` | — | `null` | CSS selector as a browser takes it: table#prices tr, a[href*='/docs/']. |
| `attribute` | `string?` | — | `null` | Attribute to return instead of the text, such as href; a relative address comes back absolute. |
| `limit` | `integer` | — | `50` | Pieces to return at most, 1 to 200; 50 by default. |

<details><summary>JSON Schema</summary>

```json
{
  "type": "object",
  "properties": {
    "url": {
      "description": "Absolute http or https address of a public page.",
      "type": "string"
    },
    "xpath": {
      "description": "XPath 1.0 over the rendered markup, lower-case names: //table[@id=\u0027prices\u0027]//tr, //h2.",
      "type": [
        "string",
        "null"
      ],
      "default": null
    },
    "css": {
      "description": "CSS selector as a browser takes it: table#prices tr, a[href*=\u0027/docs/\u0027].",
      "type": [
        "string",
        "null"
      ],
      "default": null
    },
    "attribute": {
      "description": "Attribute to return instead of the text, such as href; a relative address comes back absolute.",
      "type": [
        "string",
        "null"
      ],
      "default": null
    },
    "limit": {
      "description": "Pieces to return at most, 1 to 200; 50 by default.",
      "type": "integer",
      "default": 50
    }
  },
  "required": [
    "url"
  ]
}
```

</details>

## <code>playwright_<wbr>extract_<wbr>fields</code>

Take a record of named values out of a page. Each field tries its selectors in order, css before
xpath, and the first that matches gives the value. The answer names the selector that matched each
field — keep it and pass it first next time — and lists under missing the fields no selector found:
read the page with playwright_fetch and find those anew.

*read-only · every call adds · may reach outside services.*

| Parameter | Type | Required | Default | Description |
| --- | --- | --- | --- | --- |
| `url` | `string` | yes | — | Absolute http or https address of a public page. |
| `fields` | `array` | yes | — | 1 to 30 fields, each { name, css: [...], xpath: [...], attribute, many } with 1 to 5 selectors a field; attribute such as href instead of the text; many for every match. |

<details><summary>JSON Schema</summary>

```json
{
  "type": "object",
  "properties": {
    "url": {
      "description": "Absolute http or https address of a public page.",
      "type": "string"
    },
    "fields": {
      "description": "1 to 30 fields, each { name, css: [...], xpath: [...], attribute, many } with 1 to 5 selectors a field; attribute such as href instead of the text; many for every match.",
      "type": "array",
      "items": {
        "type": "object",
        "properties": {
          "name": {
            "type": "string"
          },
          "css": {
            "type": [
              "array",
              "null"
            ],
            "items": {
              "type": "string"
            },
            "default": null
          },
          "xPath": {
            "type": [
              "array",
              "null"
            ],
            "items": {
              "type": "string"
            },
            "default": null
          },
          "attribute": {
            "type": [
              "string",
              "null"
            ],
            "default": null
          },
          "many": {
            "type": "boolean",
            "default": false
          },
          "candidates": {
            "type": [
              "array",
              "null"
            ],
            "items": {
              "type": "object",
              "properties": {
                "text": {
                  "type": "string"
                },
                "kind": {
                  "type": "string",
                  "enum": [
                    "Css",
                    "XPath"
                  ]
                }
              },
              "required": [
                "text",
                "kind"
              ]
            }
          }
        },
        "required": [
          "name"
        ]
      }
    }
  },
  "required": [
    "url",
    "fields"
  ]
}
```

</details>

## <code>playwright_<wbr>screenshot</code>

Look at a page instead of reading it: an address in, a picture of what the browser drew out. For
what text cannot carry — a chart, a map, a layout, a page whose content is an image. It is slower
and heavier than playwright_fetch and says nothing a picture does not show, so read first and look
only when the text is not enough.

*read-only · every call adds · may reach outside services.*

| Parameter | Type | Required | Default | Description |
| --- | --- | --- | --- | --- |
| `url` | `string` | yes | — | Absolute http or https address of a public page. |
| `wholePage` | `boolean` | — | `false` | True to picture the page from top to bottom, up to 8000 pixels; false, the default, pictures the screen a visitor first sees. |

<details><summary>JSON Schema</summary>

```json
{
  "type": "object",
  "properties": {
    "url": {
      "description": "Absolute http or https address of a public page.",
      "type": "string"
    },
    "wholePage": {
      "description": "True to picture the page from top to bottom, up to 8000 pixels; false, the default, pictures the screen a visitor first sees.",
      "type": "boolean",
      "default": false
    }
  },
  "required": [
    "url"
  ]
}
```

</details>

## <code>playwright_<wbr>perform</code>

Use a page instead of only reading it: open an address, make a few moves on it, and get the text of
the page that results. For what a page says only after a form is filled in — a registry search, a
filter, a site's own search box. A move the site did not react to is reported with what the page
offered instead: rewrite the move from that rather than repeat it. It does not sign in and does not
solve a challenge.

*may overwrite or delete · every call adds · may reach outside services.*

| Parameter | Type | Required | Default | Description |
| --- | --- | --- | --- | --- |
| `url` | `string` | yes | — | Absolute http or https address of the public page to start on. |
| `moves` | `array` | yes | — | 1 to 12 moves, made in order. action: type (key by key into a field), pick (press the offered suggestion whose text matches value), choose (an option of a real select list, by its visible text), click, or press (a key while the element has the focus; Enter unless value names another). Name the element one way: css, a selector, or label, the visible caption of a field. expect with pattern holds a move to the site's reaction: request (the page asked for a URL containing pattern), appears or vanishes (an element matching the selector in pattern), text (the page came to contain the phrase). withinSeconds is how long that reaction is waited for, 1 to 30; 5 by default. |
| `maxCharacters` | `integer` | — | `20000` | Characters of the resulting page's text to return, 500 to 100000; 20000 by default. |

<details><summary>JSON Schema</summary>

```json
{
  "type": "object",
  "properties": {
    "url": {
      "description": "Absolute http or https address of the public page to start on.",
      "type": "string"
    },
    "moves": {
      "description": "1 to 12 moves, made in order. action: type (key by key into a field), pick (press the offered\nsuggestion whose text matches value), choose (an option of a real select list, by its visible text),\nclick, or press (a key while the element has the focus; Enter unless value names another). Name the\nelement one way: css, a selector, or label, the visible caption of a field. expect with pattern\nholds a move to the site\u0027s reaction: request (the page asked for a URL containing pattern), appears\nor vanishes (an element matching the selector in pattern), text (the page came to contain the\nphrase). withinSeconds is how long that reaction is waited for, 1 to 30; 5 by default.",
      "type": "array",
      "items": {
        "type": "object",
        "properties": {
          "action": {
            "type": "string"
          },
          "css": {
            "type": [
              "string",
              "null"
            ],
            "default": null
          },
          "label": {
            "type": [
              "string",
              "null"
            ],
            "default": null
          },
          "value": {
            "type": [
              "string",
              "null"
            ],
            "default": null
          },
          "expect": {
            "type": [
              "string",
              "null"
            ],
            "default": null
          },
          "pattern": {
            "type": [
              "string",
              "null"
            ],
            "default": null
          },
          "withinSeconds": {
            "type": [
              "integer",
              "null"
            ],
            "default": null
          }
        },
        "required": [
          "action"
        ]
      }
    },
    "maxCharacters": {
      "description": "Characters of the resulting page\u0027s text to return, 500 to 100000; 20000 by default.",
      "type": "integer",
      "default": 20000
    }
  },
  "required": [
    "url",
    "moves"
  ]
}
```

</details>

