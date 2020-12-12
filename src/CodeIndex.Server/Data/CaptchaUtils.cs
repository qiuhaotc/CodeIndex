using System;

namespace CodeIndex.Server
{
    public static class CaptchaUtils
    {
        static readonly char[] characters = new[] { '2', '3', '4', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'J', 'K', 'L', 'M', 'N', 'P', 'Q', 'R', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'm', 'n', 'o', 'p', 'q', 'r', 't', 'u', 'v', 'w', 'x', 'y', 'z' };
        static readonly Random random = new Random();

        public static string GenerateCaptcha(int width, int height, out byte[] captchaImages)
        {
            var captcha = string.Empty;

            for (var i = 0; i < 6; i++)
            {
                captcha += characters[random.Next(0, characters.Length)];
            }

            captchaImages = CaptchaImageUtils.GenerateCaptchaImage(width, height, captcha, random);

            return captcha;
        }
    }
}
