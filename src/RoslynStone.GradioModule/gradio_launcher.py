"""Simple Gradio launcher module for CSnakes integration.
This module provides a simple function to start the Gradio server.
"""


def start_gradio_server(base_url: str = "http://localhost:7071", server_port: int = 7860) -> str:
    """Start the Gradio server and return the server URL.

    Args:
        base_url: The base URL of the MCP server
        server_port: Port to run Gradio on

    Returns:
        The Gradio server URL
    """
    try:
        from gradio_app import create_landing_page

        # Create the landing page
        demo = create_landing_page(base_url)

        # Launch in a thread to not block
        demo.launch(
            server_port=server_port,
            server_name="127.0.0.1",
            share=False,
            prevent_thread_lock=True,
            show_error=True,
        )

        return f"http://127.0.0.1:{server_port}"
    except Exception as e:
        return f"Error: {e!s}"


def check_gradio_installed() -> bool:
    """Check if Gradio is installed.

    Returns:
        True if Gradio is available, False otherwise
    """
    try:
        import importlib.util

        return importlib.util.find_spec("gradio") is not None
    except ImportError:
        return False
