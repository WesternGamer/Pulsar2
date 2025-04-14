using System.IO;
using System.Windows.Forms;

namespace PluginLoader2.Loader.SplashScreen;

internal class NullSplashScreen : ISplashScreen
{
    public bool IsVisible => false;

    public void ShowPopup(string msg)
    {
        Form f = GetForm();

        f.Invoke(() =>
        {
            MessageBox.Show(f, msg, "Plugin Loader 2");
        });
    }

    public DialogResult ShowPopup(string msg, MessageBoxButtons buttons)
    {
        Form f = GetForm();

        return f.Invoke(() =>
        {
            return MessageBox.Show(f, msg, "Plugin Loader 2", buttons);
        });
    }

    private static Form GetForm()
    {
        FormCollection forms = Application.OpenForms;

        Form f;
        if (forms.Count > 0)
        {
            f = forms[0];
        }
        else
        {
            f = new Form()
            {
                TopMost = true,
            };
            _ = f.Handle;
        }

        return f;
    }


    public void ResetToDefault()
    {

    }

    public void SetBarValue(float percent = float.NaN)
    {

    }

    public void SetText(string msg)
    {

    }

    public void TakeControl(Stream image, string text)
    {

    }
}
