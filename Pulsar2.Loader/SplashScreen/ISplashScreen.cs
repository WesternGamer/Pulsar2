using System.IO;
using System.Windows.Forms;

namespace Pulsar2.Loader.SplashScreen;

internal interface ISplashScreen
{
    bool IsVisible { get; }

    void ShowPopup(string msg);
    DialogResult ShowPopup(string msg, MessageBoxButtons buttons);
    void ResetToDefault();
    void SetBarValue(float percent = float.NaN);
    void SetText(string msg);
    void TakeControl(Stream image, string text);
}
