using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace extract_colors_picture_box
{
    // https://unsplash.com/photos/scenery-of-waterfalls-dnStBR008JM
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

            int borderWidth = 2; // Border width
            Rectangle borderRect = new Rectangle(borderWidth, borderWidth, Width - 2 * borderWidth, Height - 2 * borderWidth);
            Pen p = new Pen(Color.Red, borderWidth);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // Draw the circular red border
            e.Graphics.DrawEllipse(p, borderRect);

            if (_displayImage != null)
            {
                // Create a circular region to clip the image
                using (GraphicsPath path = new GraphicsPath())
                {
                    path.AddEllipse(borderRect);
                    Region region = new Region(path);
                    e.Graphics.Clip = region;

                    // Draw the clipped image
                    e.Graphics.DrawImage(_displayImage, borderRect);

                    // Reset the clip region
                    e.Graphics.ResetClip();
                }
            }
            // Draw extracted colors inside the circle
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
            uniqueColors.Clear();
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
            var colors = _extractedColorsHistogram.Keys.ToArray(); ;
            if (_extractedColorsHistogram.Any())
            {
                float angle = 360f / colors.Length;
                for (int i = 1; i < colors.Length; i++)
                {
                    using (SolidBrush brush = new SolidBrush(colors[i]))
                    {
                        float startAngle = i * angle;
                        g.FillPie(brush, circleRect, startAngle, angle);
                    }
                }
            }
        }
    }
}
