using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using PrTray.App.Styling;
using PrTray.Core.Presentation;

namespace PrTray.App;

public static class TrayIconFactory
{
    private const int Size = 32;
    private const uint Transparent = 0x00000000;
    private const uint LightOutline = 0xFFF0F6FC;
    private const uint DarkOutline = 0xFF24292F;
    private static readonly uint UnreadCommentsBlue = StatusColors.UnreadComments.ToUInt32();
    private const double BadgeCenterX = 24.5;
    private const double BadgeCenterY = 7.5;

    private static readonly Dictionary<(TrayStatus Status, bool HasUnreadComments), WindowIcon> Cache = [];

    public static WindowIcon Create(TrayStatus status, bool hasUnreadComments)
    {
        if (!Cache.TryGetValue((status, hasUnreadComments), out var icon))
        {
            icon = new WindowIcon(Render(status, hasUnreadComments));
            Cache[(status, hasUnreadComments)] = icon;
        }
        return icon;
    }

    private static WriteableBitmap Render(TrayStatus status, bool hasUnreadComments)
    {
        var pixels = Pixels(status, hasUnreadComments);
        var bitmap = new WriteableBitmap(new PixelSize(Size, Size), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
        using var buffer = bitmap.Lock();
        for (var y = 0; y < Size; y++)
            Marshal.Copy(pixels, y * Size, buffer.Address + y * buffer.RowBytes, Size);
        return bitmap;
    }

    private static int[] Pixels(TrayStatus status, bool hasUnreadComments)
    {
        var center = (Size - 1) / 2.0;
        var pixels = new int[Size * Size];
        for (var y = 0; y < Size; y++)
        {
            for (var x = 0; x < Size; x++)
            {
                var badgeColor = hasUnreadComments ? BadgePixelColor(Distance(x, y, BadgeCenterX, BadgeCenterY)) : null;
                pixels[y * Size + x] = unchecked((int)(badgeColor ?? PixelColor(Distance(x, y, center, center), status)));
            }
        }
        return pixels;
    }

    private static double Distance(double x, double y, double centerX, double centerY) =>
        Math.Sqrt(Math.Pow(x - centerX, 2) + Math.Pow(y - centerY, 2));

    // A small dot in the top-right corner, drawn over the status circle, tells that a PR of mine has unread comments.
    private static uint? BadgePixelColor(double distanceFromBadgeCenter) => distanceFromBadgeCenter switch
    {
        > 7.2 => null,
        > 6 => DarkOutline,
        > 4.5 => LightOutline,
        _ => UnreadCommentsBlue,
    };

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
