"""Cyberpunk Theme CSS for Roslyn-Stone Gradio UI.

A neon-infused, accessible dark theme for the MCP Testing Interface.
WCAG 2.1 AA compliant with focus states, high contrast support, and reduced motion.
"""

from pygments.formatters import HtmlFormatter


def get_cyberpunk_css() -> str:
    """Generate the complete cyberpunk theme CSS.

    Returns:
        Complete CSS string for the cyberpunk theme.
    """
    # Get Pygments syntax highlighting CSS
    pygments_css = HtmlFormatter(style="monokai").get_style_defs(".highlight")

    return f"""
    /* ==========================================================================
       ROSLYN-STONE CYBERPUNK THEME
       A neon-infused, accessible dark theme for the MCP Testing Interface
       ========================================================================== */

    /* Pygments syntax highlighting */
    {pygments_css}

    /* ==========================================================================
       CSS CUSTOM PROPERTIES (Design Tokens)
       ========================================================================== */
    :root {{
        /* Background colors */
        --cyber-bg-primary: #0a0a12;
        --cyber-bg-secondary: #12121a;
        --cyber-bg-tertiary: #1a1a24;
        --cyber-bg-elevated: #1e1e2a;

        /* Accent colors - neon palette with WCAG AAA contrast (7:1) */
        --cyber-cyan: #00fff9;
        --cyber-cyan-dim: #00c4c0;
        --cyber-magenta: #ff00ff;
        --cyber-magenta-dim: #cc00cc;
        --cyber-purple: #c084fc;
        --cyber-blue: #3b82f6;
        --cyber-pink: #ec4899;

        /* Text colors - WCAG AA compliant (minimum 4.5:1 contrast) */
        --cyber-text-primary: #f0f0f0;
        --cyber-text-secondary: #c8c8d8;
        --cyber-text-muted: #9090a0;

        /* Semantic colors */
        --cyber-success: #00ff88;
        --cyber-error: #ff4466;
        --cyber-warning: #ffcc00;
        --cyber-info: #00b4ff;

        /* Glow effects */
        --glow-cyan: 0 0 10px rgba(0, 255, 249, 0.5), 0 0 20px rgba(0, 255, 249, 0.3), 0 0 30px rgba(0, 255, 249, 0.1);
        --glow-magenta: 0 0 10px rgba(255, 0, 255, 0.5), 0 0 20px rgba(255, 0, 255, 0.3), 0 0 30px rgba(255, 0, 255, 0.1);
        --glow-purple: 0 0 10px rgba(168, 85, 247, 0.5), 0 0 20px rgba(168, 85, 247, 0.3);

        /* Borders */
        --cyber-border: rgba(0, 255, 249, 0.2);
        --cyber-border-bright: rgba(0, 255, 249, 0.5);

        /* Spacing */
        --space-xs: 4px;
        --space-sm: 8px;
        --space-md: 16px;
        --space-lg: 24px;
        --space-xl: 32px;

        /* Border radius */
        --radius-sm: 4px;
        --radius-md: 8px;
        --radius-lg: 12px;
        --radius-xl: 16px;

        /* Transitions */
        --transition-fast: 0.15s ease;
        --transition-normal: 0.3s ease;
        --transition-slow: 0.5s ease;

        /* Typography */
        --font-mono: 'JetBrains Mono', 'Fira Code', 'SF Mono', 'Monaco', 'Menlo', 'Ubuntu Mono', 'Consolas', monospace;
        --font-display: 'Inter', 'SF Pro Display', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
    }}

    /* ==========================================================================
       ANIMATIONS - Cyberpunk effects
       ========================================================================== */

    @keyframes pulse-glow {{
        0%, 100% {{ box-shadow: var(--glow-cyan); opacity: 1; }}
        50% {{ box-shadow: 0 0 5px rgba(0, 255, 249, 0.3), 0 0 10px rgba(0, 255, 249, 0.2); opacity: 0.85; }}
    }}

    @keyframes scan-line {{
        0% {{ transform: translateY(-100%); }}
        100% {{ transform: translateY(100vh); }}
    }}

    @keyframes neon-flicker {{
        0%, 19%, 21%, 23%, 25%, 54%, 56%, 100% {{
            text-shadow: 0 0 4px var(--cyber-cyan), 0 0 10px var(--cyber-cyan), 0 0 20px var(--cyber-cyan);
            opacity: 1;
        }}
        20%, 24%, 55% {{ text-shadow: none; opacity: 0.8; }}
    }}

    @keyframes gradient-shift {{
        0% {{ background-position: 0% 50%; }}
        50% {{ background-position: 100% 50%; }}
        100% {{ background-position: 0% 50%; }}
    }}

    @keyframes status-pulse {{
        0%, 100% {{ box-shadow: 0 0 5px var(--cyber-success), 0 0 10px var(--cyber-success); opacity: 1; }}
        50% {{ box-shadow: 0 0 2px var(--cyber-success); opacity: 0.7; }}
    }}

    /* ==========================================================================
       BASE STYLES
       ========================================================================== */

    .gradio-container {{
        max-width: 1400px !important;
        margin: auto;
        background: var(--cyber-bg-primary) !important;
        font-family: var(--font-display) !important;
        color: var(--cyber-text-primary) !important;
        position: relative;
        min-height: 100vh;
    }}

    .gradio-container::before {{
        content: '';
        position: fixed;
        top: 0; left: 0; right: 0; bottom: 0;
        background-image:
            linear-gradient(rgba(0, 255, 249, 0.03) 1px, transparent 1px),
            linear-gradient(90deg, rgba(0, 255, 249, 0.03) 1px, transparent 1px);
        background-size: 50px 50px;
        pointer-events: none;
        z-index: 0;
    }}

    .gradio-container::after {{
        content: '';
        position: fixed;
        top: 0; left: 0; right: 0;
        height: 2px;
        background: linear-gradient(90deg, transparent, var(--cyber-cyan), transparent);
        opacity: 0.3;
        animation: scan-line 8s linear infinite;
        pointer-events: none;
        z-index: 1000;
    }}

    /* ==========================================================================
       GLOBAL TEXT COLOR OVERRIDES - WCAG AA compliance
       ========================================================================== */

    .gradio-container *:not(.token):not(.hljs-*) {{
        color: var(--cyber-text-primary) !important;
    }}

    .prose, .prose *, .markdown-body, .markdown-body *,
    .gr-prose, .gr-prose *,
    div[data-testid="markdown"], div[data-testid="markdown"] *,
    table, table *, th, td, tr, li, p, span {{
        color: var(--cyber-text-primary) !important;
    }}

    pre, pre *, code, code * {{
        color: var(--cyber-text-primary) !important;
    }}

    .dark, .dark .gradio-container, body.dark {{
        background: var(--cyber-bg-primary) !important;
    }}

    /* ==========================================================================
       TYPOGRAPHY
       ========================================================================== */

    h1 {{
        font-family: var(--font-display) !important;
        font-weight: 900 !important;
        font-size: 2.8rem !important;
        letter-spacing: 2px;
        text-transform: uppercase;
        background: linear-gradient(135deg, var(--cyber-cyan) 0%, var(--cyber-magenta) 50%, var(--cyber-purple) 100%);
        background-size: 200% 200%;
        -webkit-background-clip: text;
        -webkit-text-fill-color: transparent;
        background-clip: text;
        animation: gradient-shift 5s ease infinite;
        position: relative;
        padding-bottom: var(--space-md);
    }}

    h1::after {{
        content: '';
        position: absolute;
        bottom: 0; left: 0; right: 0;
        height: 2px;
        background: linear-gradient(90deg, transparent, var(--cyber-cyan), var(--cyber-magenta), transparent);
        box-shadow: 0 0 10px var(--cyber-cyan);
    }}

    h2 {{
        color: var(--cyber-cyan) !important;
        font-weight: 700 !important;
        font-size: 1.6rem !important;
        margin-top: var(--space-lg) !important;
        margin-bottom: var(--space-md) !important;
        text-shadow: 0 0 10px rgba(0, 255, 249, 0.3);
    }}

    h3 {{
        color: var(--cyber-text-primary) !important;
        font-weight: 600 !important;
        font-size: 1.3rem !important;
        border-left: 3px solid var(--cyber-magenta);
        padding-left: var(--space-md);
        margin-top: var(--space-lg) !important;
    }}

    h4 {{
        color: var(--cyber-purple) !important;
        font-weight: 600 !important;
        font-size: 1.1rem !important;
    }}

    p, .prose {{ color: var(--cyber-text-primary) !important; line-height: 1.7; }}
    strong, b {{ color: var(--cyber-cyan) !important; font-weight: 600; }}

    a {{
        color: var(--cyber-cyan) !important;
        text-decoration: underline !important;
        text-decoration-color: rgba(0, 255, 249, 0.4) !important;
        text-underline-offset: 3px;
        transition: var(--transition-fast);
    }}
    a:hover {{
        color: var(--cyber-magenta) !important;
        text-decoration-color: var(--cyber-magenta) !important;
        text-shadow: 0 0 8px rgba(255, 0, 255, 0.5);
    }}
    a:focus-visible {{
        outline: 2px solid var(--cyber-cyan) !important;
        outline-offset: 2px;
        border-radius: var(--radius-sm);
    }}

    /* ==========================================================================
       TAB NAVIGATION
       ========================================================================== */

    .tab-nav {{
        background: var(--cyber-bg-secondary) !important;
        border-bottom: 1px solid var(--cyber-border) !important;
        padding: var(--space-sm) var(--space-md) 0 !important;
        border-radius: var(--radius-lg) var(--radius-lg) 0 0 !important;
        gap: var(--space-sm) !important;
    }}

    .tab-nav button {{
        font-size: 15px !important;
        font-weight: 500 !important;
        padding: 14px 28px !important;
        border-radius: var(--radius-md) var(--radius-md) 0 0 !important;
        border: 1px solid transparent !important;
        background: transparent !important;
        color: var(--cyber-text-secondary) !important;
        transition: all var(--transition-normal) !important;
        min-height: 48px; min-width: 44px;
    }}

    .tab-nav button:hover {{
        background: rgba(0, 255, 249, 0.1) !important;
        color: var(--cyber-cyan) !important;
        border-color: var(--cyber-border) !important;
    }}

    .tab-nav button[aria-selected="true"] {{
        background: linear-gradient(135deg, rgba(0, 255, 249, 0.15) 0%, rgba(255, 0, 255, 0.1) 100%) !important;
        color: var(--cyber-cyan) !important;
        font-weight: 600 !important;
        border: 1px solid var(--cyber-border-bright) !important;
        border-bottom-color: transparent !important;
        box-shadow: var(--glow-cyan), inset 0 1px 0 rgba(0, 255, 249, 0.2);
    }}

    .tab-nav button:focus-visible {{
        outline: 2px solid var(--cyber-magenta) !important;
        outline-offset: 2px;
        z-index: 10;
    }}

    .tab-container[aria-hidden="true"], .visually-hidden {{
        display: none !important;
    }}

    .tabitem {{
        background: var(--cyber-bg-secondary) !important;
        border: 1px solid var(--cyber-border) !important;
        border-top: none !important;
        border-radius: 0 0 var(--radius-lg) var(--radius-lg) !important;
        padding: var(--space-lg) !important;
    }}

    /* ==========================================================================
       BUTTONS
       ========================================================================== */

    button, .gr-button {{
        font-family: var(--font-display) !important;
        font-weight: 500 !important;
        border-radius: var(--radius-md) !important;
        transition: all var(--transition-normal) !important;
        cursor: pointer;
        min-height: 44px; min-width: 44px;
        padding: var(--space-sm) var(--space-md) !important;
    }}

    button.primary, .gr-button.primary, button[variant="primary"] {{
        background: linear-gradient(135deg, var(--cyber-cyan-dim) 0%, var(--cyber-cyan) 100%) !important;
        color: var(--cyber-bg-primary) !important;
        font-weight: 600 !important;
        border: none !important;
        box-shadow: 0 0 15px rgba(0, 255, 249, 0.4), 0 4px 15px rgba(0, 0, 0, 0.3) !important;
    }}

    button.primary:hover, .gr-button.primary:hover, button[variant="primary"]:hover {{
        background: linear-gradient(135deg, var(--cyber-cyan) 0%, #4dfffc 100%) !important;
        transform: translateY(-2px) !important;
        box-shadow: var(--glow-cyan), 0 6px 20px rgba(0, 0, 0, 0.4) !important;
    }}

    button.primary:focus-visible, .gr-button.primary:focus-visible {{
        outline: 3px solid var(--cyber-magenta) !important;
        outline-offset: 2px;
    }}

    button.secondary, .gr-button.secondary, button:not(.primary):not([variant="primary"]) {{
        background: transparent !important;
        color: var(--cyber-cyan) !important;
        border: 1px solid var(--cyber-border-bright) !important;
    }}

    button.secondary:hover, button:not(.primary):not([variant="primary"]):hover {{
        background: rgba(0, 255, 249, 0.1) !important;
        border-color: var(--cyber-cyan) !important;
        box-shadow: 0 0 10px rgba(0, 255, 249, 0.3) !important;
    }}

    button.sm, .gr-button.sm, button[size="sm"] {{ padding: var(--space-xs) var(--space-sm) !important; font-size: 13px !important; min-height: 36px; }}
    button.lg, .gr-button.lg, button[size="lg"] {{ padding: var(--space-md) var(--space-xl) !important; font-size: 16px !important; min-height: 52px; }}

    /* ==========================================================================
       FORM INPUTS
       ========================================================================== */

    input[type="text"], input[type="password"], textarea, .gr-textbox textarea, .gr-textbox input {{
        background: var(--cyber-bg-tertiary) !important;
        border: 1px solid var(--cyber-border) !important;
        border-radius: var(--radius-md) !important;
        color: var(--cyber-text-primary) !important;
        font-family: var(--font-display) !important;
        padding: var(--space-sm) var(--space-md) !important;
        transition: all var(--transition-fast) !important;
        min-height: 44px;
    }}

    input[type="text"]:hover, textarea:hover {{ border-color: var(--cyber-cyan-dim) !important; }}

    input[type="text"]:focus, textarea:focus {{
        border-color: var(--cyber-cyan) !important;
        box-shadow: 0 0 0 3px rgba(0, 255, 249, 0.15), 0 0 10px rgba(0, 255, 249, 0.2) !important;
        outline: none !important;
    }}

    input::placeholder, textarea::placeholder {{ color: var(--cyber-text-muted) !important; opacity: 1; }}
    label, .gr-label {{ color: var(--cyber-text-secondary) !important; font-weight: 500 !important; font-size: 14px !important; }}

    /* ==========================================================================
       DROPDOWNS
       ========================================================================== */

    .gr-dropdown, select {{
        background: var(--cyber-bg-tertiary) !important;
        border: 1px solid var(--cyber-border) !important;
        border-radius: var(--radius-md) !important;
        color: var(--cyber-text-primary) !important;
        min-height: 44px;
    }}

    .gr-dropdown:focus, select:focus {{
        border-color: var(--cyber-cyan) !important;
        box-shadow: 0 0 0 3px rgba(0, 255, 249, 0.15) !important;
        outline: none !important;
    }}

    .gr-dropdown ul, .dropdown-menu {{
        background: var(--cyber-bg-elevated) !important;
        border: 1px solid var(--cyber-border-bright) !important;
        border-radius: var(--radius-md) !important;
        box-shadow: 0 8px 32px rgba(0, 0, 0, 0.5), var(--glow-cyan) !important;
    }}

    .gr-dropdown li:hover, .dropdown-item:hover {{
        background: rgba(0, 255, 249, 0.15) !important;
        color: var(--cyber-cyan) !important;
    }}

    /* ==========================================================================
       CODE BLOCKS
       ========================================================================== */

    .gr-code, .code-editor, pre, code {{
        font-family: var(--font-mono) !important;
        font-size: 14px !important;
        line-height: 1.6 !important;
        background: var(--cyber-bg-primary) !important;
        border: 1px solid var(--cyber-border) !important;
        border-radius: var(--radius-md) !important;
        color: var(--cyber-text-primary) !important;
    }}

    pre {{ padding: var(--space-md) !important; overflow-x: auto; position: relative; }}

    pre::before {{
        content: '';
        position: absolute;
        top: 0; left: 0;
        width: 3px; height: 100%;
        background: linear-gradient(180deg, var(--cyber-cyan), var(--cyber-magenta));
        border-radius: var(--radius-sm) 0 0 var(--radius-sm);
    }}

    code:not(pre code) {{
        background: rgba(0, 255, 249, 0.1) !important;
        color: var(--cyber-cyan) !important;
        padding: 2px 6px !important;
        border-radius: var(--radius-sm) !important;
        font-size: 0.9em !important;
    }}

    .highlight {{ background: var(--cyber-bg-primary) !important; }}
    .highlight .k, .highlight .kd, .highlight .kn {{ color: var(--cyber-magenta) !important; }}
    .highlight .s, .highlight .s1, .highlight .s2 {{ color: var(--cyber-success) !important; }}
    .highlight .nf, .highlight .nc {{ color: var(--cyber-cyan) !important; }}
    .highlight .mi, .highlight .mf {{ color: var(--cyber-purple) !important; }}

    /* ==========================================================================
       CARDS & PANELS
       ========================================================================== */

    .tool-card, .resource-card, .prompt-card, .gr-panel, .gr-box {{
        background: rgba(18, 18, 26, 0.8) !important;
        backdrop-filter: blur(10px);
        border: 1px solid var(--cyber-border) !important;
        border-radius: var(--radius-lg) !important;
        padding: var(--space-lg) !important;
        margin: var(--space-md) 0 !important;
        box-shadow: 0 4px 20px rgba(0, 0, 0, 0.3), inset 0 1px 0 rgba(255, 255, 255, 0.05) !important;
        transition: all var(--transition-normal) !important;
        position: relative;
        overflow: hidden;
    }}

    .tool-card:hover, .resource-card:hover, .prompt-card:hover {{
        border-color: var(--cyber-border-bright) !important;
        transform: translateY(-2px);
    }}

    .tool-card::before, .resource-card::before, .prompt-card::before {{
        content: '';
        position: absolute;
        top: 0; left: 0; right: 0;
        height: 2px;
        background: linear-gradient(90deg, var(--cyber-cyan), var(--cyber-magenta), var(--cyber-purple));
    }}

    /* ==========================================================================
       ACCORDION
       ========================================================================== */

    .gr-accordion {{ background: var(--cyber-bg-secondary) !important; border: 1px solid var(--cyber-border) !important; border-radius: var(--radius-lg) !important; }}
    .gr-accordion > button {{ background: var(--cyber-bg-tertiary) !important; color: var(--cyber-text-primary) !important; padding: var(--space-md) !important; min-height: 48px; }}
    .gr-accordion > button:hover {{ background: rgba(0, 255, 249, 0.1) !important; }}
    .gr-accordion > button[aria-expanded="true"] {{ background: rgba(0, 255, 249, 0.15) !important; color: var(--cyber-cyan) !important; }}

    /* ==========================================================================
       CHATBOT
       ========================================================================== */

    .gr-chatbot, .chatbot {{ background: var(--cyber-bg-secondary) !important; border: 1px solid var(--cyber-border) !important; border-radius: var(--radius-lg) !important; }}

    .gr-chatbot .user, .chatbot .user-message {{
        background: linear-gradient(135deg, rgba(0, 255, 249, 0.2) 0%, rgba(0, 255, 249, 0.1) 100%) !important;
        border: 1px solid rgba(0, 255, 249, 0.3) !important;
        border-radius: var(--radius-lg) var(--radius-lg) var(--radius-sm) var(--radius-lg) !important;
        color: var(--cyber-text-primary) !important;
        padding: var(--space-md) !important;
    }}

    .gr-chatbot .bot, .chatbot .assistant-message {{
        background: linear-gradient(135deg, rgba(255, 0, 255, 0.15) 0%, rgba(168, 85, 247, 0.1) 100%) !important;
        border: 1px solid rgba(255, 0, 255, 0.2) !important;
        border-radius: var(--radius-lg) var(--radius-lg) var(--radius-lg) var(--radius-sm) !important;
        color: var(--cyber-text-primary) !important;
        padding: var(--space-md) !important;
    }}

    /* ==========================================================================
       CHECKBOX & RADIO
       ========================================================================== */

    input[type="checkbox"], input[type="radio"] {{
        appearance: none; -webkit-appearance: none;
        width: 20px; height: 20px;
        border: 2px solid var(--cyber-border-bright) !important;
        border-radius: var(--radius-sm) !important;
        background: var(--cyber-bg-tertiary) !important;
        cursor: pointer;
    }}

    input[type="checkbox"]:checked, input[type="radio"]:checked {{
        background: var(--cyber-cyan) !important;
        border-color: var(--cyber-cyan) !important;
        box-shadow: 0 0 10px rgba(0, 255, 249, 0.5) !important;
    }}

    input[type="checkbox"]:checked::after {{ content: '✓'; display: block; text-align: center; color: var(--cyber-bg-primary); font-weight: bold; font-size: 14px; line-height: 16px; }}

    /* ==========================================================================
       STATUS INDICATORS
       ========================================================================== */

    .status-indicator, .status-connected {{
        display: inline-block;
        width: 12px; height: 12px;
        border-radius: 50%;
        background-color: var(--cyber-success);
        box-shadow: 0 0 10px var(--cyber-success), 0 0 20px var(--cyber-success);
        animation: status-pulse 2s ease-in-out infinite;
        margin-right: var(--space-sm);
        border: 2px solid rgba(255, 255, 255, 0.3);
    }}

    .status-disconnected {{ background-color: var(--cyber-error) !important; box-shadow: 0 0 10px var(--cyber-error) !important; animation: none; }}
    .status-warning {{ background-color: var(--cyber-warning) !important; box-shadow: 0 0 10px var(--cyber-warning) !important; }}

    /* ==========================================================================
       MARKDOWN CONTENT
       ========================================================================== */

    .markdown-body, .gr-markdown {{ color: var(--cyber-text-primary) !important; line-height: 1.7; }}
    .markdown-body ul, .markdown-body ol {{ padding-left: var(--space-lg); }}
    .markdown-body li {{ margin: var(--space-sm) 0; color: var(--cyber-text-primary) !important; }}
    .markdown-body li::marker {{ color: var(--cyber-cyan); }}

    .markdown-body blockquote {{
        border-left: 4px solid var(--cyber-magenta);
        padding: var(--space-md);
        margin: var(--space-md) 0;
        color: var(--cyber-text-secondary);
        background: rgba(255, 0, 255, 0.05);
        border-radius: 0 var(--radius-md) var(--radius-md) 0;
    }}

    .markdown-body table {{ width: 100%; border-collapse: collapse; margin: var(--space-md) 0; }}
    .markdown-body th {{ background: rgba(0, 255, 249, 0.15); color: var(--cyber-cyan) !important; font-weight: 600; padding: var(--space-sm) var(--space-md); text-align: left; border-bottom: 2px solid var(--cyber-border-bright); }}
    .markdown-body td {{ padding: var(--space-sm) var(--space-md); border-bottom: 1px solid var(--cyber-border); color: var(--cyber-text-primary) !important; }}
    .markdown-body tr:hover td {{ background: rgba(0, 255, 249, 0.05); }}

    .markdown-body hr {{ border: none; height: 1px; background: linear-gradient(90deg, transparent, var(--cyber-cyan), var(--cyber-magenta), transparent); margin: var(--space-xl) 0; }}

    /* ==========================================================================
       SCROLLBARS
       ========================================================================== */

    ::-webkit-scrollbar {{ width: 10px; height: 10px; }}
    ::-webkit-scrollbar-track {{ background: var(--cyber-bg-primary); border-radius: var(--radius-sm); }}
    ::-webkit-scrollbar-thumb {{ background: linear-gradient(180deg, var(--cyber-cyan-dim), var(--cyber-magenta-dim)); border-radius: var(--radius-sm); border: 2px solid var(--cyber-bg-primary); }}
    ::-webkit-scrollbar-thumb:hover {{ background: linear-gradient(180deg, var(--cyber-cyan), var(--cyber-magenta)); }}
    * {{ scrollbar-width: thin; scrollbar-color: var(--cyber-cyan-dim) var(--cyber-bg-primary); }}

    /* ==========================================================================
       FOOTER STATUS BAR
       ========================================================================== */

    .gradio-container > .gr-markdown:last-of-type {{
        background: var(--cyber-bg-secondary) !important;
        border-top: 1px solid var(--cyber-border) !important;
        padding: var(--space-md) var(--space-lg) !important;
        margin-top: var(--space-xl) !important;
        border-radius: var(--radius-md) !important;
        text-align: center;
    }}

    /* ==========================================================================
       FOCUS MANAGEMENT - Accessibility
       ========================================================================== */

    *:focus {{ outline: none; }}
    *:focus-visible {{ outline: 2px solid var(--cyber-cyan) !important; outline-offset: 2px; }}

    .skip-link {{
        position: absolute; top: -40px; left: 0;
        background: var(--cyber-cyan); color: var(--cyber-bg-primary);
        padding: var(--space-sm) var(--space-md);
        z-index: 1000;
        transition: top var(--transition-fast);
    }}
    .skip-link:focus {{ top: 0; }}

    /* ==========================================================================
       HIGH CONTRAST MODE - Accessibility
       ========================================================================== */

    @media (prefers-contrast: high) {{
        :root {{
            --cyber-text-primary: #ffffff;
            --cyber-text-secondary: #d0d0d0;
            --cyber-border: rgba(255, 255, 255, 0.5);
            --cyber-border-bright: rgba(255, 255, 255, 0.8);
        }}
        button.primary, .gr-button.primary {{ border: 2px solid white !important; }}
    }}

    /* ==========================================================================
       REDUCED MOTION - Accessibility
       ========================================================================== */

    @media (prefers-reduced-motion: reduce) {{
        *, *::before, *::after {{
            animation-duration: 0.01ms !important;
            animation-iteration-count: 1 !important;
            transition-duration: 0.01ms !important;
        }}
        .gradio-container::after {{ display: none; }}
    }}

    /* ==========================================================================
       RESPONSIVE DESIGN
       ========================================================================== */

    @media (max-width: 768px) {{
        h1 {{ font-size: 2rem !important; }}
        .tab-nav button {{ padding: var(--space-sm) var(--space-md) !important; font-size: 14px !important; }}
        .gr-row {{ flex-direction: column !important; }}
        .tool-card, .resource-card, .prompt-card {{ padding: var(--space-md) !important; }}
    }}

    /* ==========================================================================
       PRINT STYLES
       ========================================================================== */

    @media print {{
        .gradio-container {{ background: white !important; color: black !important; }}
        .gradio-container::before, .gradio-container::after {{ display: none !important; }}
        h1, h2, h3, h4 {{ color: black !important; -webkit-text-fill-color: black !important; }}
    }}

    /* ==========================================================================
       UTILITY CLASSES
       ========================================================================== */

    .highlight-text {{
        background: linear-gradient(135deg, var(--cyber-cyan) 0%, var(--cyber-magenta) 100%);
        -webkit-background-clip: text;
        -webkit-text-fill-color: transparent;
        background-clip: text;
        font-weight: 600;
    }}

    .gr-loading {{
        background: linear-gradient(90deg, var(--cyber-bg-tertiary) 25%, rgba(0, 255, 249, 0.1) 50%, var(--cyber-bg-tertiary) 75%) !important;
        background-size: 200% 100%;
        animation: gradient-shift 1.5s ease infinite;
    }}

    .result-box {{
        background-color: var(--cyber-bg-primary) !important;
        border: 1px solid var(--cyber-border) !important;
        border-radius: var(--radius-md) !important;
        padding: var(--space-md) !important;
        font-family: var(--font-mono) !important;
        white-space: pre-wrap;
        max-height: 600px;
        overflow-y: auto;
        color: var(--cyber-text-primary) !important;
    }}
    """
