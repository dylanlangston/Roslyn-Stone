"""
Simple Gradio launcher module for CSnakes integration.
This module provides a simple function to start the Gradio server.
"""


def start_gradio_server(base_url: str = "http://localhost:7071", server_port: int = 7860) -> str:
    """
    Start the Gradio server and return the server URL.
    
    Args:
        base_url: The base URL of the MCP server
        server_port: Port to run Gradio on
    
    Returns:
        The Gradio server URL
    """
    try:
        import gradio as gr
        from gradio_app import create_landing_page
        
        print(f"[Gradio Launcher] Starting Gradio server on port {server_port} with base_url={base_url}", flush=True)
        
        # Create the landing page
        demo = create_landing_page(base_url)
        
        # Launch in a thread to not block
        demo.launch(
            server_port=server_port,
            server_name="127.0.0.1",
            share=False,
            prevent_thread_lock=True,
            show_error=True
        )
        
        result = f"http://127.0.0.1:{server_port}"
        print(f"[Gradio Launcher] Successfully started Gradio at {result}", flush=True)
        return result
    except Exception as e:
        error_msg = f"Error: {str(e)}"
        print(f"[Gradio Launcher] Failed to start: {error_msg}", flush=True)
        return error_msg


def check_gradio_installed() -> bool:
    """
    Check if Gradio is installed.
    
    Returns:
        True if Gradio is available, False otherwise
    """
    try:
        import gradio
        return True
    except ImportError:
        return False
