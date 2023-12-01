# Extract Colors from Picture Box

The source project has several issues. Let's take them one by one.

___
**Bottleneck**

The source project is attempting to update a progress bar. But the reason for needing one seems to be the terrible bottleneck caused by `uniqueColors.Contains(pixelColor)` in this `ExtractUniqueColorsAsync(Image image)` that we can fix using an indexed collection. 

```
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
```
___

**Progress**

The main form hosts a progress bar. The `MyCustomPicturebox` class should be the provider of a `ProgressChanged` event and not require an `Action()` to be injected.

```
class MyCustomPicturebox : Control
{
    public event ProgressChangedEventHandler? ProgressChanged;
    .
    .
}
```

Then, when you consider that (for example) a 1932 x 2576 image could potentially have close to 5 million unique colors but the progress bar has only 100 increments, it seems more reasonable to fire the event only when the integer value of progress changes.

```
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
```





