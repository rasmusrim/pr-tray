using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using PrTray.Core.Presentation;

namespace PrTray.App;

public static class TrayIconFactory
{
    private const int Size = 32;
    private const uint Transparent = 0x00000000;
    private const uint LightOutline = 0xFFF0F6FC;
    private const uint DarkOutline = 0xFF24292F;

    private static readonly Dictionary<TrayStatus, WindowIcon> Cache = [];

    public static WindowIcon Create(TrayStatus status)
    {
        if (!Cache.TryGetValue(status, out var icon))
        {
            icon = new WindowIcon(Render(status));
            Cache[status] = icon;
        }
        return icon;
    }

    private static WriteableBitmap Render(TrayStatus status)
    {
        var center = (Size - 1) / 2.0;
        var pixels = new int[Size * Size];
        for (var y = 0; y < Size; y++)
        {
            for (var x = 0; x < Size; x++)
            {
                var distanceFromCenter = Math.Sqrt(Math.Pow(x - center, 2) + Math.Pow(y - center, 2));
                pixels[y * Size + x] = unchecked((int)PixelColor(distanceFromCenter, status));
            }
        }

        var bitmap = new WriteableBitmap(new PixelSize(Size, Size), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
        using var buffer = bitmap.Lock();
        for (var y = 0; y < Size; y++)
            Marshal.Copy(pixels, y * Size, buffer.Address + y * buffer.RowBytes, Size);
        return bitmap;
    }

    private static uint PixelColor(double distanceFromCenter, TrayStatus status) => distanceFromCenter switch
    {
        > 15.5 => Transparent,
        > 14 => DarkOutline,
        > 12 => LightOutline,
        _ => FillColor(status),
    };

    private static uint FillColor(TrayStatus status) => status switch
    {
        TrayStatus.Approved => 0xFF2EA043,
        TrayStatus.NeedsAttention => 0xFFDA3633,
        TrayStatus.Error => Transparent,
        _ => 0xFF6E7681,
    };
}
