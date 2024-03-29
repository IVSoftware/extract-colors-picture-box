using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace extract_colors_picture_box
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            myCustomPicturebox.ProgressChanged += (sender, e) =>
            {
                if (!this.IsDisposed) BeginInvoke(() =>
                {
                    progressBar.Visible = e.ProgressPercentage != 100;
                    progressBar.Value = e.ProgressPercentage;
                    progressBar.Invalidate();
                });
                // Use with caution!
                Application.DoEvents();
            };
        }
    }
    class MyCustomPicturebox : Control
    {
        public MyCustomPicturebox()
        {
            AllowDrop = true;
            Size = new Size(450, 450);
        }
        public event ProgressChangedEventHandler? ProgressChanged;

        Dictionary<Color, int> _extractedColorsHistogram = new Dictionary<Color, int>();

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            int borderWidth = 2;
            Rectangle borderRect = new Rectangle(borderWidth, borderWidth, Width - 2 * borderWidth, Height - 2 * borderWidth);
            Pen p = new Pen(Color.Red, borderWidth);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.DrawEllipse(p, borderRect);
            if (_displayImage != null)
            {
                // Dispose GraphicPath per Jimi comment.
                using (GraphicsPath path = new GraphicsPath())
                {
                    path.AddEllipse(borderRect);
                    Region region = new Region(path);
                    e.Graphics.Clip = region;
                    e.Graphics.DrawImage(_displayImage, borderRect);
                    e.Graphics.ResetClip();
                }
            }
            if(_busy.Wait(0))
            {
                try
                {
                    DrawExtractedColors(e.Graphics, borderRect);
                }
                finally
                {
                    _busy.Release();
                }
            }
        }

        protected override void OnDragEnter(DragEventArgs e)
        {
            base.OnDragEnter(e);
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        SemaphoreSlim _busy = new SemaphoreSlim(1, 1);
        protected override async void OnDragDrop(DragEventArgs e)
        {
            if (_busy.Wait(0))
            {
                try
                {
                    if (e.Data.GetData(DataFormats.FileDrop) is string[] files && files.FirstOrDefault() is string file)
                    {
                        _rawImage = (Bitmap)Image.FromFile(file);
                        _displayImage = localResizeImage();
                        Invalidate();
                        await ExtractUniqueColorsAsync(_rawImage);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading the image: {ex.Message}");
                }
                finally
                {
                    _busy.Release();
                    Invalidate();
                }
            }
            Bitmap localResizeImage()
            {
                var destRect = new Rectangle(0, 0, Width, Height);
                var destImage = new Bitmap(Width, Height);
                destImage.SetResolution(_rawImage.HorizontalResolution, _rawImage.VerticalResolution);
                using (var graphics = Graphics.FromImage(destImage))
                {
                    graphics.CompositingMode = CompositingMode.SourceCopy;
                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.SmoothingMode = SmoothingMode.HighQuality;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    using (var wrapMode = new ImageAttributes())
                    {
                        wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                        graphics.DrawImage(_rawImage, destRect, 0, 0, _rawImage.Width, _rawImage.Height, GraphicsUnit.Pixel, wrapMode);
                    }
                }
                return destImage;
            }
        }
        private Bitmap? _rawImage = null;
        private Bitmap? _displayImage = null;

        private async Task ExtractUniqueColorsAsync(Image image)
        {
            Dictionary<Color, int> uniqueColors = new Dictionary<Color, int>();
            var stopwatch = Stopwatch.StartNew();
            int counter = 0;
            if (image != null)
            {
                Bitmap bitmap = new Bitmap(image);
                int width = bitmap.Width;
                int height = bitmap.Height;

                Rectangle rect = new Rectangle(0, 0, width, height);
                BitmapData bitmapData = bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

                var benchmarkList = new List<Color>();
                await Task.Run(() =>
                {
                    try
                    {
                        unsafe
                        {
                            byte* ptr = (byte*)bitmapData.Scan0;

                            for (int y = 0; y < height; y++)
                            {
                                for (int x = 0; x < width; x++)
                                {
                                    int offset = y * bitmapData.Stride + x * 4;
                                    byte blue = ptr[offset];
                                    byte green = ptr[offset + 1];
                                    byte red = ptr[offset + 2];
                                    byte alpha = ptr[offset + 3];

                                    Color pixelColor = Color.FromArgb(alpha, red, green, blue);

#if true
                                    // Takes ~ 1 second for 1932 x 2576
                                    if(uniqueColors.TryGetValue(pixelColor, out int value))
                                    {
                                        uniqueColors[pixelColor] = value + 1;
                                    }
                                    else
                                    {
                                        uniqueColors[pixelColor] = 1;
                                    }
                                    var progressPreview = Convert.ToInt32((double)(y * width + x) / (width * height) * 100);
                                    // Send only when integer changes.
                                    if (progressPreview != _prevProgress)
                                    {
                                        ProgressChanged?.Invoke(
                                            this,
                                            new ProgressChangedEventArgs(
                                                progressPreview, stopwatch.Elapsed));
                                        _prevProgress = progressPreview;
                                    }
#else
                                    // Takes ~ 2 minutes for 1932 x 2576
                                    if (!benchmarkList.Contains(pixelColor))
                                    {
                                        benchmarkList.Add(pixelColor);
                                        ProgressChanged?.Invoke(
                                            this,
                                            new ProgressChangedEventArgs(
                                                Convert.ToInt32((double)(y * width + x) / (width * height) * 100), UniqueColors.ToArray()));
                                    }

#endif
                                    if (MouseButtons != MouseButtons.None) throw new TaskCanceledException();
                                }
                            }
                        }
                    }
                    catch (TaskCanceledException ex) { }
                    finally 
                    {
                        ProgressChanged?.Invoke(
                            this,
                            new ProgressChangedEventArgs(
                                100, // Ensure 100% is sent.
                                uniqueColors.ToArray()));
                    }
                });
                bitmap.UnlockBits(bitmapData);
                stopwatch.Stop();
                { }
            }
            _extractedColorsHistogram = uniqueColors;
        }
        int _prevProgress = 100;
        private void DrawExtractedColors(Graphics g, Rectangle circleRect)
        {
            var stopwatch = Stopwatch.StartNew();
            var colors = _extractedColorsHistogram.Keys.ToArray();
            _prevProgress = -1;
            if (_extractedColorsHistogram.Any())
            {
                float angle = 360f / colors.Length;
                for (int i = 0; i < colors.Length; i++)
                {
                    using (SolidBrush brush = new SolidBrush(colors[i]))
                    {
                        float startAngle = i * angle;
                        g.FillPie(brush, circleRect, startAngle, angle);
                    }
                    var progressPreview = i * 100 / colors.Length;
                    if (progressPreview != _prevProgress)
                    {
                        ProgressChanged?.Invoke(
                            this,
                            new ProgressChangedEventArgs(
                                progressPreview, stopwatch.Elapsed));
                        _prevProgress = progressPreview;
                    }
                }
            }
            ProgressChanged?.Invoke(
                this,
                new ProgressChangedEventArgs(
                    100, stopwatch.Elapsed));
        }
    }
}
