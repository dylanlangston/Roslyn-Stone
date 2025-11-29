"""Syntax highlighting and code formatting utilities."""

from __future__ import annotations

import json

from pygments import highlight
from pygments.formatters import HtmlFormatter
from pygments.lexers import CSharpLexer, JsonLexer


def format_csharp_code(code: str) -> str:
    """Format C# code with syntax highlighting.

    Args:
        code: C# code to format.

    Returns:
        HTML-formatted code with syntax highlighting.
    """
    try:
        formatter = HtmlFormatter(style="monokai", noclasses=True, cssclass="highlight")
        highlighted = highlight(code, CSharpLexer(), formatter)
        return f'<div style="background: #272822; padding: 10px; border-radius: 8px; overflow-x: auto;">{highlighted}</div>'
    except Exception:
        return f'<pre style="background: #272822; color: #f8f8f2; padding: 10px; border-radius: 8px; overflow-x: auto;"><code>{code}</code></pre>'


def format_json_output(data: object) -> str:
    """Format JSON output with syntax highlighting.

    Args:
        data: Python object to format as JSON.

    Returns:
        HTML-formatted JSON with syntax highlighting.
    """
    try:
        json_str = json.dumps(data, indent=2)
        formatter = HtmlFormatter(style="monokai", noclasses=True, cssclass="highlight")
        highlighted = highlight(json_str, JsonLexer(), formatter)
        return f'<div style="background: #272822; padding: 10px; border-radius: 8px; overflow-x: auto;">{highlighted}</div>'
    except Exception:
        return f'<pre style="background: #272822; color: #f8f8f2; padding: 10px; border-radius: 8px; overflow-x: auto;"><code>{json.dumps(data, indent=2)}</code></pre>'
