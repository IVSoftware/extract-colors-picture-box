# Extract Colors from Picture Box

If a 1932 x 2575 image is being processed in your `ExtractUniqueColorsAsync` method, it means you're going to do this close to 5 million times (on a list that might have ~5 million other values). 

```
if (!uniqueColors.Contains(pixelColor)) ...
```
You may not need a `ProgressBar` at all if you use an indexed collection of some kind. When I tested this, it seemed to run ~2 orders of magnitude faster.

```
    Dictionary<Color, int> uniqueColors = new Dictionary<Color, int>();
    // Task.Run for each X x Y...
    {...
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
```

You're also sending (again, worst case) 5 Million `ProgressChanged` events for a `ProgressBar` with (probably) 100 increments. It seems reasonable to throttle that down and send the notification only when the integer value of progress changes.

___

**Drawing the pie chart progressively**

[![time lapse images][1]][1]

When you go to draw the pie chart, your objective seems to be adding the colors "one-by-one" which takes several seconds by design. Consider making a critical section to prevent reentrancy, however.

```
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
```
___
**More optimization and thread safety**

While _processing_ the full-resolution `_rawImage` file, this `OnPaint` is _showing_ a pre-clipped and lower resolution `_displayImage` to save time. 

So, suppose you has some kind of `DragDrop` to inject a new image, it might be implemented something like this:

```
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
```

**Update progress bar**

The ~100 progress notifications can be handled like this:

```
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
```


  [1]: https://i.stack.imgur.com/0hKxo.jpg
