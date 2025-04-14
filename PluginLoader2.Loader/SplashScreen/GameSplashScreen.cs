using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace PluginLoader2.Loader.SplashScreen;

internal class GameSplashScreen : ISplashScreen
{
    private const float barWidth = 0.98f; // 98% of width
    private const float barHeight = 0.06f; // 6% of height
    private static readonly Color backgroundColor = Color.FromArgb(4, 4, 4);

    private readonly PictureBox splashScreen;
    private readonly Image defaultImage;
    private readonly PictureBoxSizeMode defaultSizeMode;
    private readonly Size size;

    private Label label;
    private Stream image;

    private readonly RectangleF bar;
    private float barValue = float.NaN;

    public bool IsVisible => true;

    public GameSplashScreen(PictureBox splashScreen)
    {
        this.splashScreen = splashScreen;
        defaultImage = splashScreen.Image;
        defaultSizeMode = splashScreen.SizeMode;


        size = splashScreen.Size;
        SizeF barSize = new SizeF(size.Width * barWidth, size.Height * barHeight);
        float padding = (1 - barWidth) * size.Width * 0.5f;
        PointF barStart = new PointF(padding, size.Height - barSize.Height - padding);
        bar = new RectangleF(barStart, barSize);

    }

    public static ISplashScreen GetSplashScreen()
    {
        if (TryFindScreen(out GameSplashScreen result))
            return result;
        return new NullSplashScreen();
    }

    public static bool TryFindScreen(out GameSplashScreen result)
    {
        FormCollection forms = Application.OpenForms;
        if (forms.Count > 0)
        {
            foreach (Form form in forms)
            {
                Type formType = form.GetType();
                if (formType.FullName == "Keen.VRage.Platform.Windows.Forms.SplashScreen")
                {
                    PictureBox picture = formType.GetField("_pictureBox", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form) as PictureBox;
                    if (picture != null)
                    {
                        result = new GameSplashScreen(picture);
                        return true;
                    }
                }
            }
        }
        result = null;
        return false;
    }

    public void ResetToDefault()
    {
        splashScreen.Paint -= OnPictureBoxDraw;

        splashScreen.Invoke(() =>
        {
            splashScreen.Parent.Controls.Remove(label);

            Image image = splashScreen.Image;

            splashScreen.Image = defaultImage;
            splashScreen.SizeMode = defaultSizeMode;
            splashScreen.Refresh();
            splashScreen.Visible = true;

            Application.DoEvents();

            image.Dispose();
            this.image.Dispose();
        });
    }

    public void TakeControl(Stream image, string text)
    {
        splashScreen.Invoke(() =>
        {
            Font lblFont = new Font(FontFamily.GenericSansSerif, 24, FontStyle.Bold);
            label = new Label
            {
                Name = "PluginLoaderInfo",
                Font = lblFont,
                BackColor = backgroundColor,
                ForeColor = Color.White,
                MaximumSize = size,
                Size = new Size(size.Width, lblFont.Height),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, (int)(bar.Y - lblFont.Height - 1)),
                Text = text,
            };
            splashScreen.Parent.Controls.Add(label);
            label.BringToFront();

            splashScreen.Paint += OnPictureBoxDraw;
            splashScreen.Image = Image.FromStream(image);
            splashScreen.SizeMode = PictureBoxSizeMode.StretchImage;
            splashScreen.Refresh();
            splashScreen.Visible = true;

            this.image = image;

            Application.DoEvents();
        });

    }


    public void SetText(string msg)
    {
        label.Invoke(() => { label.Text = msg; });
    }

    public void SetBarValue(float percent = float.NaN)
    {
        Interlocked.Exchange(ref barValue, percent);
    }

    private void OnPictureBoxDraw(object sender, PaintEventArgs e)
    {
        float barValue = Interlocked.CompareExchange(ref this.barValue, 0, 0);
        if (!float.IsNaN(barValue))
        {
            Graphics graphics = e.Graphics;
            graphics.FillRectangle(Brushes.DarkSlateGray, bar);
            graphics.FillRectangle(Brushes.White, new RectangleF(bar.Location, new SizeF(bar.Width * barValue, bar.Height)));
        }
    }

    public void ShowPopup(string msg)
    {
        splashScreen.Invoke(() =>
        {
            MessageBox.Show(splashScreen.Parent, msg, "Plugin Loader2");
        });
    }
    public DialogResult ShowPopup(string msg, MessageBoxButtons buttons)
    {
        return splashScreen.Invoke(() =>
        {
            return MessageBox.Show(splashScreen.Parent, msg, "Plugin Loader2", buttons);
        });
    }
}
