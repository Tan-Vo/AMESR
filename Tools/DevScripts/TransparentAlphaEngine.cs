using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

public class TransparentAlphaEngine
{
    // High-performance boundary-connected flood fill background remover
    public static Bitmap RemoveBackground(Bitmap src, int threshold = 40, int feather = 12)
    {
        int w = src.Width;
        int h = src.Height;

        Bitmap argb = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        using (Graphics g = Graphics.FromImage(argb))
        {
            g.DrawImage(src, 0, 0, w, h);
        }

        BitmapData data = argb.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        int stride = data.Stride;
        byte[] pixels = new byte[stride * h];
        Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);

        // Helper to get RGB
        Func<int, int, Color> getCol = (x, y) =>
        {
            int idx = y * stride + x * 4;
            return Color.FromArgb(pixels[idx + 3], pixels[idx + 2], pixels[idx + 1], pixels[idx]);
        };

        // Sample background from borders
        long totalR = 0, totalG = 0, totalB = 0;
        int sampleCount = 0;
        // Sample corners and border midpoints
        int[] xs = { 0, w - 1, w / 2, 0, w - 1, w / 4, (3 * w) / 4 };
        int[] ys = { 0, h - 1, 0, h - 1, h / 2, 0, h - 1 };
        for (int i = 0; i < xs.Length; i++)
        {
            Color c = getCol(xs[i], ys[i]);
            totalR += c.R;
            totalG += c.G;
            totalB += c.B;
            sampleCount++;
        }
        int bgR = (int)(totalR / sampleCount);
        int bgG = (int)(totalG / sampleCount);
        int bgB = (int)(totalB / sampleCount);

        // BFS Flood fill from outer boundaries
        bool[] visited = new bool[w * h];
        Queue<int> q = new Queue<int>();

        for (int x = 0; x < w; x++)
        {
            int idxTop = 0 * w + x;
            int idxBot = (h - 1) * w + x;
            q.Enqueue(idxTop); visited[idxTop] = true;
            q.Enqueue(idxBot); visited[idxBot] = true;
        }
        for (int y = 0; y < h; y++)
        {
            int idxL = y * w + 0;
            int idxR = y * w + (w - 1);
            if (!visited[idxL]) { q.Enqueue(idxL); visited[idxL] = true; }
            if (!visited[idxR]) { q.Enqueue(idxR); visited[idxR] = true; }
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
                // Fully transparent
                pixels[pIdx + 3] = 0;

                // Enqueue 4 neighbors
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
                // Anti-aliased edge
                double factor = (dist - threshold) / (double)feather;
                byte newAlpha = (byte)Math.Min(255, Math.Max(0, factor * 255));
                pixels[pIdx + 3] = newAlpha;
            }
        }

        Marshal.Copy(pixels, 0, data.Scan0, pixels.Length);
        argb.UnlockBits(data);
        return argb;
    }

    // Process and center sprite onto uniform canvas with consistent pivot
    public static void ProcessAndAlign(string inputPath, string outputPath, int canvasW = 128, int canvasH = 128, int threshold = 42, bool pinBottom = false, int bottomPadding = 6)
    {
        using (Bitmap raw = new Bitmap(inputPath))
        {
            using (Bitmap keyed = RemoveBackground(raw, threshold))
            {
                int w = keyed.Width;
                int h = keyed.Height;

                // Find content bounding box
                int minX = w, minY = h, maxX = 0, maxY = 0;
                BitmapData d = keyed.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                int stride = d.Stride;
                byte[] px = new byte[stride * h];
                Marshal.Copy(d.Scan0, px, 0, px.Length);
                keyed.UnlockBits(d);

                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        if (px[y * stride + x * 4 + 3] > 15)
                        {
                            if (x < minX) minX = x;
                            if (x > maxX) maxX = x;
                            if (y < minY) minY = y;
                            if (y > maxY) maxY = y;
                        }
                    }
                }

                // If blank
                if (minX > maxX || minY > maxY)
                {
                    keyed.Save(outputPath, ImageFormat.Png);
                    return;
                }

                int cropW = maxX - minX + 1;
                int cropH = maxY - minY + 1;

                // Enforce canvas size
                int outW = Math.Max(canvasW, cropW + 4);
                int outH = Math.Max(canvasH, cropH + 4);

                Bitmap canvas = new Bitmap(outW, outH, PixelFormat.Format32bppArgb);
                using (Graphics g = Graphics.FromImage(canvas))
                {
                    g.Clear(Color.Transparent);

                    int destX = (outW - cropW) / 2;
                    int destY = pinBottom ? (outH - cropH - bottomPadding) : (outH - cropH) / 2;

                    g.DrawImage(keyed,
                        new Rectangle(destX, destY, cropW, cropH),
                        new Rectangle(minX, minY, cropW, cropH),
                        GraphicsUnit.Pixel);
                }

                canvas.Save(outputPath, ImageFormat.Png);
                canvas.Dispose();
            }
        }
    }
}
