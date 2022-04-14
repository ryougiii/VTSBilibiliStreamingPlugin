using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class TransparentWindow : MonoBehaviour
{
    [SerializeField]
    private Material m_Material;

    private struct MARGINS
    {
        public int cxLeftWidth;
        public int cxRightWidth;
        public int cyTopHeight;
        public int cyBottomHeight;
    }

    // Define function signatures to import from Windows APIs

    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

    [DllImport("Dwmapi.dll")]
    private static extern uint DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS margins);
    [DllImport("user32.dll")]
    private static extern int SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int y, int Width, int Height, int flags);
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    // Definitions of window styles
    const int GWL_STYLE = -16;
    const int GWL_EXSTYLE = -20;
    const uint WS_POPUP = 0x80000000;
    const uint WS_VISIBLE = 0x10000000;
    const uint WS_BORDER = 0x00800000;

    const uint WS_CAPTION = 0x00C00000;
    const uint WS_SYSMENU = 0x00080000;
    const uint WS_MINIMIZEBOX = 0x00020000;
    const uint WS_MAXIMIZEBOX = 0x00010000;
    const uint WS_THICKFRAME = 0x00040000;
    const uint WS_EX_APPWINDOW = 0x00040000;

    void Start()
    {
        GameObject.Find("Main Camera").GetComponent<Camera>().backgroundColor = new Color(1, 1, 1);

    }

    // Pass the output of the camera to the custom material
    // for chroma replacement
    void OnRenderImage(RenderTexture from, RenderTexture to)
    {
        Graphics.Blit(from, to, m_Material);
    }
    public static bool windowOpacity = false;
    public static bool windowsetTop = false;
    public static void setWindowOpacity()
    {
        windowOpacity = !windowOpacity;
        if (windowOpacity)
        {
            GameObject.Find("Main Camera").GetComponent<Camera>().backgroundColor = new Color(0, 0, 0);
            var margins = new MARGINS() { cxLeftWidth = -1 };

            // Get a handle to the window
            var hwnd = GetActiveWindow();

            // Set properties of the window
            // See: https://msdn.microsoft.com/en-us/library/windows/desktop/ms633591%28v=vs.85%29.aspx
            SetWindowLong(hwnd, GWL_STYLE, WS_POPUP | WS_VISIBLE);

            // Extend the window into the client area
            //See: https://msdn.microsoft.com/en-us/library/windows/desktop/aa969512%28v=vs.85%29.aspx 
            DwmExtendFrameIntoClientArea(hwnd, ref margins);
        }
        else
        {
            GameObject.Find("Main Camera").GetComponent<Camera>().backgroundColor = new Color(1, 1, 1);
            // var margins = new MARGINS() { cxLeftWidth = -1 };

            // Get a handle to the window
            var hwnd = GetActiveWindow();

            // Set properties of the window
            // See: https://msdn.microsoft.com/en-us/library/windows/desktop/ms633591%28v=vs.85%29.aspx
            SetWindowLong(hwnd, GWL_STYLE, WS_POPUP | WS_VISIBLE | WS_BORDER | WS_CAPTION | WS_SYSMENU | WS_MAXIMIZEBOX | WS_MINIMIZEBOX | WS_THICKFRAME);
        }
    }

    public static void setWindowsettop()
    {
        windowsetTop = !windowsetTop;
        if (windowsetTop)
        {
            SetWindowPos(GetForegroundWindow(), -1, 0, 0, 0, 0, 1 | 2);
        }else{
            SetWindowPos(GetForegroundWindow(), -2, 0, 0, 0, 0, 1 | 2);
        }
    }


}