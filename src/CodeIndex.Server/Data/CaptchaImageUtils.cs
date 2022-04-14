using System;
using System.IO;
using SkiaSharp;

namespace CodeIndex.Server
{
    public class CaptchaImageUtils
    {
        public static byte[] GenerateCaptchaImage(int width, int height, string captchaCode, Random random)
        {
            using var bitmap = new SKBitmap(width, height); ;
            using var graphics = new SKCanvas(bitmap);
            graphics.DrawColor(GetRandomLightColor(random));
            DrawCaptchaCode(width, height, captchaCode, random, graphics);
            DrawDisorderLine(random, width, height, graphics);
            AdjustRippleEffect(bitmap);

            using var ms = new MemoryStream();
            using var data = SKImage.FromBitmap(bitmap).Encode(SKEncodedImageFormat.Png, 100);
            data.SaveTo(ms);

            return ms.ToArray();
        }

        static int GetFontSize(int imageWidth, int captchCodeCount)
        {
            var averageSize = imageWidth / captchCodeCount;

            return Convert.ToInt32(averageSize);
        }

        static SKColor GetRandomDeepColor(Random random)
        {
            var redlow = 160;
            var greenLow = 100;
            var blueLow = 160;
            return new SKColor((byte)random.Next(redlow), (byte)random.Next(greenLow), (byte)random.Next(blueLow));
        }

        static SKColor GetRandomLightColor(Random random)
        {
            var low = 200;
            var high = 255;

            var nRend = random.Next(high) % (high - low) + low;
            var nGreen = random.Next(high) % (high - low) + low;
            var nBlue = random.Next(high) % (high - low) + low;

            return new SKColor((byte)nRend, (byte)nGreen, (byte)nBlue);
        }

        static void DrawCaptchaCode(int width, int height, string captchaCode, Random random, SKCanvas graphics)
        {
            var fontSize = GetFontSize(width, captchaCode.Length);
            var font = new SKFont(SKTypeface.Default, fontSize);
            var fontBrush = new SKPaint(font);
            fontBrush.FakeBoldText = true;
            for (int i = 0; i < captchaCode.Length; i++)
            {
                var shiftPx1 = i == 0 ? 0 : -fontSize / 6;
                var shiftPx2 = i == captchaCode.Length - 1 ? 0 : fontSize / 6;
                fontBrush.Color = GetRandomDeepColor(random);
                fontBrush.TextSkewX = random.Next(-shiftPx2, -shiftPx1) / 10.0f;

                var x = i * fontSize + random.Next(shiftPx1, shiftPx2) + random.Next(shiftPx1, shiftPx2);
                var maxY = height - fontSize;
                if (maxY < 15)
                {
                    maxY = 15;
                }

                var y = random.Next(0, maxY);
                graphics.DrawText(captchaCode[i].ToString(), x, height - y, fontBrush);
            }
        }

        static void DrawDisorderLine(Random random, int width, int height, SKCanvas graphics)
        {
            var linePen = new SKPaint
            {
                StrokeWidth = 3
            };

            for (int i = 0; i < random.Next(3, 5); i++)
            {
                linePen.Color = GetRandomLightColor(random);

                var startPoint = new SKPoint(random.Next(0, width), random.Next(0, height));
                var endPoint = new SKPoint(random.Next(0, width), random.Next(0, height));
                graphics.DrawLine(startPoint, endPoint, linePen);

                //var bezierPoint1 = new SKPoint(random.Next(0, width), random.Next(0, height));
                //var bezierPoint2 = new SKPoint(random.Next(0, width), random.Next(0, height));
                //graphics.DrawBezier(linePen, startPoint, bezierPoint1, bezierPoint2, endPoint);
            }
        }

        static void AdjustRippleEffect(SKBitmap baseMap)
        {
        }
    }
}
