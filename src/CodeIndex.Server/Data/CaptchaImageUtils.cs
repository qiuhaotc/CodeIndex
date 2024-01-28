using System;
using System.IO;
using System.Linq;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace CodeIndex.Server
{
    public class CaptchaImageUtils
    {
        public static byte[] GenerateCaptchaImage(int width, int height, string captchaCode, Random random)
        {
            var fontSize = GetFontSize(width, captchaCode.Length);
            var fondFamily = SystemFonts.Collection.Families.FirstOrDefault(u => u.Name == "Consolas");
            fondFamily = fondFamily == default ? SystemFonts.Collection.Families.Last() : fondFamily;
            var font = SystemFonts.CreateFont(fondFamily.Name, fontSize);

            using var image = new Image<Rgba32>(width, height, GetRandomLightColor(random));
            DrawCaptchaCode(height, captchaCode, fontSize, font, random, image);
            DrawDisorderLine(width, height, image, random);

            using var ms = new MemoryStream();
            image.SaveAsPng(ms);
            return ms.ToArray();
        }

        static void DrawCaptchaCode(int height, string captchaCode, int fontSize, Font font, Random random, Image<Rgba32> image)
        {
            for (int i = 0; i < captchaCode.Length; i++)
            {
                var shiftPx = fontSize / 6;
                var x = random.Next(-shiftPx, shiftPx) + random.Next(-shiftPx, shiftPx);
                if (x < 0 && i == 0)
                {
                    x = 0;
                }

                x += i * fontSize;

                var maxY = height - fontSize;
                if (maxY < 0)
                {
                    maxY = 0;
                }

                var y = random.Next(0, maxY);

                image.Mutate(operation => operation.DrawText(captchaCode[i].ToString(), font, GetRandomDeepColor(random), new PointF(x, y)));
            }
        }

        static Color GetRandomDeepColor(Random random)
        {
            var redlow = 160;
            var greenLow = 100;
            var blueLow = 160;
            return Color.FromRgb((byte)random.Next(redlow), (byte)random.Next(greenLow), (byte)random.Next(blueLow));
        }

        static Color GetRandomLightColor(Random random)
        {
            const int low = 200;
            const int high = 255;

            var nRend = random.Next(high) % (high - low) + low;
            var nGreen = random.Next(high) % (high - low) + low;
            var nBlue = random.Next(high) % (high - low) + low;

            return Color.FromRgb((byte)nRend, (byte)nGreen, (byte)nBlue);
        }

        static int GetFontSize(int imageWidth, int captchCodeCount)
        {
            var averageSize = imageWidth / captchCodeCount;

            return Convert.ToInt32(averageSize);
        }

        static void DrawDisorderLine(int width, int height, Image graphics, Random random)
        {
            for (int i = 0; i < random.Next(3, 5); i++)
            {
                var linePen = new SolidPen(new SolidBrush(GetRandomLightColor(random)), 3);
                var startPoint = new Point(random.Next(0, width), random.Next(0, height));
                var endPoint = new Point(random.Next(0, width), random.Next(0, height));
                graphics.Mutate(operation => operation.DrawLine(linePen, startPoint, endPoint));

                //var bezierPoint1 = new Point(random.Next(0, width), random.Next(0, height));
                //var bezierPoint2 = new Point(random.Next(0, width), random.Next(0, height));
                //var bezierPoint3 = new Point(random.Next(0, width), random.Next(0, height));
                //var bezierPoint4 = new Point(random.Next(0, width), random.Next(0, height));
                //graphics.Mutate(operation => operation.DrawBeziers(linePen, bezierPoint1, bezierPoint2, bezierPoint3, bezierPoint4));
            }
        }
    }
}