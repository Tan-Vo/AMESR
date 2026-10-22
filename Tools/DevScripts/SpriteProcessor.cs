using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

public class SpriteProcessor
{
    public static void MakeTransparentAndCenter(string srcPath, string destPath, int targetSize = 128, int threshold = 40, bool centerBottom = false)
    {
        using (Bitmap src = new Bitmap(srcPath))
        {
            int w = src.Width;
            int h = src.Height;

            // Sample corner colors for background
            Color c1 = src.GetPixel(0, 0);
            Color c2 = src.GetPixel(w - 1, 0);
            Color c3 = src.GetPixel(0, h - 1);
            Color c4 = src.GetPixel(w - 1, h - 1);
            int bgR = (c1.R + c2.R + c3.R + c4.R) / 4;
            int bgG = (c1.G + c2.G + c3.G + c4.G) / 4;
            int bgB = (c1.B + c2.B + c3.B + c4.B) / 4;

            // Create 32bpp ARGB bitmap
            Bitmap argb = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(argb))
            {
                g.DrawImage(src, 0, 0, w, h);
            }

            // BFS Flood Fill from edges
            bool[,] visited = new bool[w, h];
            Queue<Point> q = new Queue<Point>();

            // Add all boundary pixels to queue
            for (int x = 0; x < w; x++)
            {
                q.Enqueue(new Point(x, 0));
                q.Enqueue(new Point(x, h - 1));
                visited[x, 0] = true;
                visited[x, h - 1] = true;
            }
            for (int y = 0; y < h; y++)
            {
                if (!visited[0, y]) { q.Enqueue(new Point(0, y)); visited[0, y] = true; }
                if (!visited[w - 1, y]) { q.Enqueue(new Point(w - 1, y)); visited[w - 1, y] = true; }
            }

            int[] dx = { 0, 0, 1, -1 };
            int[] dy = { 1, -1, 0, 0 };

            while (q.Count > 0)
            {
                Point pt = q.Dequeue();
                Color c = argb.GetPixel(pt.X, pt.Y);
                double dist = Math.Sqrt((c.R - bgR) * (c.R - bgR) + (c.G - bgG) * (c.G - bgG) + (c.B - bgB) * (c.B - bgB));

                if (dist <= threshold)
                {
                    // Transparent
                    argb.SetPixel(pt.X, pt.Y, Color.FromArgb(0, 0, 0, 0));

                    // Explore 4 neighbors
                    for (int i = 0; i < 4; i++)
                    {
                        int nx = pt.X + dx[i];
                        int ny = pt.Y + dy[i];
                        if (nx >= 0 && nx < w && ny >= 0 && ny < h && !visited[nx, ny])
                        {
                            visited[nx, ny] = true;
                            q.Enqueue(new Point(nx, ny));
                        }
                    }
                }
                else if (dist <= threshold + 15)
                {
                    // Smooth anti-aliased edge
                    int alpha = (int)(((dist - threshold) / 15.0) * 255.0);
                    argb.SetPixel(pt.X, pt.Y, Color.FromArgb(Math.Min(255, Math.Max(0, alpha)), c.R, c.G, c.B));
                }
            }

            // Find non-transparent bounding box
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

            // If empty
            if (minX > maxX || minY > maxY)
            {
                argb.Save(destPath, ImageFormat.Png);
                argb.Dispose();
                return;
            }

            int cropW = maxX - minX + 1;
            int cropH = maxY - minY + 1;

            // Crop the sprite
            Bitmap cropped = argb.Clone(new Rectangle(minX, minY, cropW, cropH), PixelFormat.Format32bppArgb);
            argb.Dispose();

            // Target Canvas (default 128x128 or power of 2)
            int finalSize = Math.Max(targetSize, Math.Max(cropW + 4, cropH + 4));
            Bitmap finalBmp = new Bitmap(finalSize, finalSize, PixelFormat.Format32bppArgb);

            using (Graphics g = Graphics.FromImage(finalBmp))
            {
                g.Clear(Color.Transparent);
                int posX = (finalSize - cropW) / 2;
                int posY = centerBottom ? (finalSize - cropH - 4) : (finalSize - cropH) / 2;
                g.DrawImage(cropped, posX, posY, cropW, cropH);
            }

            cropped.Dispose();
            finalBmp.Save(destPath, ImageFormat.Png);
            finalBmp.Dispose();
        }
    }
}
