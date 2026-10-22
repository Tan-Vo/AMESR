using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

public class BatchTransparentAndAlign
{
    // Fast in-place background removal for sliced grid sprites
    public static void ProcessSingleSprite(string filePath, int threshold = 40, int feather = 10, int targetCanvasSize = 0)
    {
        if (!File.Exists(filePath)) return;

        Bitmap src;
        using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            src = new Bitmap(fs);
        }

        int w = src.Width;
        int h = src.Height;

        Bitmap argb = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        using (Graphics g = Graphics.FromImage(argb))
        {
            g.DrawImage(src, 0, 0, w, h);
        }
        src.Dispose();

        BitmapData data = argb.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        int stride = data.Stride;
        byte[] pixels = new byte[stride * h];
        Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);

        // Helper to get color
        Func<int, int, Color> getCol = (x, y) =>
        {
            int idx = y * stride + x * 4;
            return Color.FromArgb(pixels[idx + 3], pixels[idx + 2], pixels[idx + 1], pixels[idx]);
        };

        // Sample background from borders
        long totR = 0, totG = 0, totB = 0;
        int count = 0;
        int[] samX = { 1, w - 2, w / 2, 2, w - 3, w / 4, (3 * w) / 4 };
        int[] samY = { 1, h - 2, 1, h - 2, h / 2, 2, h - 3 };
        for (int i = 0; i < samX.Length; i++)
        {
            Color c = getCol(samX[i], samY[i]);
            totR += c.R; totG += c.G; totB += c.B;
            count++;
        }
        int bgR = (int)(totR / count);
        int bgG = (int)(totG / count);
        int bgB = (int)(totB / count);

        // Remove corner bracket ticks (within 7px of corners)
        int cornerMargin = Math.Min(8, Math.Min(w / 8, h / 8));
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool isCorner = (x < cornerMargin || x >= w - cornerMargin) && (y < cornerMargin || y >= h - cornerMargin);
                if (isCorner)
                {
                    int pIdx = y * stride + x * 4;
                    // Force corner ticks to background color so BFS wipes them out
                    pixels[pIdx] = (byte)bgB;
                    pixels[pIdx + 1] = (byte)bgG;
                    pixels[pIdx + 2] = (byte)bgR;
                }
            }
        }

        // BFS Flood fill from outer boundaries
        bool[] visited = new bool[w * h];
        Queue<int> q = new Queue<int>();

        for (int x = 0; x < w; x++)
        {
            q.Enqueue(0 * w + x); visited[0 * w + x] = true;
            q.Enqueue((h - 1) * w + x); visited[(h - 1) * w + x] = true;
        }
        for (int y = 0; y < h; y++)
        {
            if (!visited[y * w + 0]) { q.Enqueue(y * w + 0); visited[y * w + 0] = true; }
            if (!visited[y * w + (w - 1)]) { q.Enqueue(y * w + (w - 1)); visited[y * w + (w - 1)] = true; }
        }

        while (q.Count > 0)
        {
            int curr = q.Dequeue();
            int cx = curr % w;
            int cy = curr / w;
            int pIdx = cy * stride + cx * 4;

            int b = pixels[pIdx];
            int gCol = pixels[pIdx + 1];
            int r = pixels[pIdx + 2];

            int dR = r - bgR;
            int dG = gCol - bgG;
            int dB = b - bgB;
            double dist = Math.Sqrt(dR * dR + dG * dG + dB * dB);

            if (dist <= threshold)
            {
                pixels[pIdx + 3] = 0; // Transparent

                int[] nxs = { cx + 1, cx - 1, cx, cx };
                int[] nys = { cy, cy, cy + 1, cy - 1 };
                for (int i = 0; i < 4; i++)
                {
                    int nx = nxs[i];
                    int ny = nys[i];
                    if (nx >= 0 && nx < w && ny >= 0 && ny < h)
                    {
                        int nIdx = ny * w + nx;
                        if (!visited[nIdx])
                        {
                            visited[nIdx] = true;
                            q.Enqueue(nIdx);
                        }
                    }
                }
            }
            else if (dist <= threshold + feather)
            {
                double factor = (dist - threshold) / (double)feather;
                byte newAlpha = (byte)Math.Min(255, Math.Max(0, factor * 255));
                pixels[pIdx + 3] = newAlpha;
            }
        }

        Marshal.Copy(pixels, 0, data.Scan0, pixels.Length);
        argb.UnlockBits(data);

        // If targetCanvasSize specified (e.g. 128), center the sprite
        if (targetCanvasSize > 0)
        {
            // Find content bounding box
            int minX = w, minY = h, maxX = 0, maxY = 0;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (argb.GetPixel(x, y).A > 20)
                    {
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }
            }

            if (minX <= maxX && minY <= maxY)
            {
                int cropW = maxX - minX + 1;
                int cropH = maxY - minY + 1;
                int finalCanvas = Math.Max(targetCanvasSize, Math.Max(cropW + 4, cropH + 4));

                Bitmap canvas = new Bitmap(finalCanvas, finalCanvas, PixelFormat.Format32bppArgb);
                using (Graphics g = Graphics.FromImage(canvas))
                {
                    g.Clear(Color.Transparent);
                    int destX = (finalCanvas - cropW) / 2;
                    int destY = (finalCanvas - cropH) / 2;
                    g.DrawImage(argb,
                        new Rectangle(destX, destY, cropW, cropH),
                        new Rectangle(minX, minY, cropW, cropH),
                        GraphicsUnit.Pixel);
                }
                argb.Dispose();
                canvas.Save(filePath, ImageFormat.Png);
                canvas.Dispose();
                return;
            }
        }

        argb.Save(filePath, ImageFormat.Png);
        argb.Dispose();
    }
}
