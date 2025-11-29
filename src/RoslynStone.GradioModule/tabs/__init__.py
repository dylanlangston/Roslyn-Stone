"""Roslyn-Stone Gradio UI Tabs Package.

Contains UI tab components for the Gradio interface.
"""

from tabs.setup import create_setup_tab
from tabs.tools import create_tools_tab
from tabs.resources import create_resources_tab
from tabs.prompts import create_prompts_tab
from tabs.chat import create_chat_tab

__all__ = [
    "create_setup_tab",
    "create_tools_tab",
    "create_resources_tab",
    "create_prompts_tab",
    "create_chat_tab",
]
