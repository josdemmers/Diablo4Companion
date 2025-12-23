using CommunityToolkit.Mvvm.Messaging.Messages;
using System.Drawing;
using Windows.Win32.Foundation;

namespace D4Companion.Messages
{
    public class MouseUpdatedMessage(MouseUpdatedMessageParams mouseUpdatedMessageParams) : ValueChangedMessage<MouseUpdatedMessageParams>(mouseUpdatedMessageParams)
    {

    }

    public class MouseUpdatedMessageParams
    {
        public int CoordsMouseX { get; set; }
        public int CoordsMouseY { get; set; }
    }

    public class ScreenCaptureReadyMessage(ScreenCaptureReadyMessageParams screenCaptureReadyMessageParams) : ValueChangedMessage<ScreenCaptureReadyMessageParams>(screenCaptureReadyMessageParams)
    {
    }

    public class ScreenCaptureReadyMessageParams
    {
        public Bitmap? CurrentScreen { get; set; }
    }

    public class TakeScreenshotRequestedMessage
    {

    }

    public class WindowHandleUpdatedMessage(WindowHandleUpdatedMessageParams windowHandleUpdatedMessageParams) : ValueChangedMessage<WindowHandleUpdatedMessageParams>(windowHandleUpdatedMessageParams)
    {

    }

    public class WindowHandleUpdatedMessageParams
    {
        public HWND WindowHandle { get; set; }
    }
}
