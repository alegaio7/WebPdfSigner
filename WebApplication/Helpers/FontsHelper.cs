using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Linq;

namespace WebTestApplication.Helpers
{
    public class FontsHelper
    {
        private string _fontsPath;
        private List<FontInfo> _fonts;

        public const string FONT_1 = "Brush";

        public class FontInfo
        {
            internal FontInfo(int id, string friendlyName, string fontFile)
            {
                Id = id;
                FriendlyName = friendlyName;
                FontFile = fontFile;
            }

            public int Id { get; private set; }
            public string FriendlyName { get; private set; }
            internal string FontFile { get; private set; }
        }

        public FontsHelper(IWebHostEnvironment env)
        {
            if (env == null)
                throw new ArgumentNullException(nameof(env));

            _fontsPath = Path.Combine(env.WebRootPath, "fonts");
            _fonts = new List<FontInfo>();
            _fonts.Add(new FontInfo(1, FONT_1, "AlexBrush-Regular.ttf"));
            // add more fonts here, and copy the files to the /fonts folder
        }

        public List<FontInfo> GetFontsList()
        {
            return _fonts;
        }

        public FontInfo GetFontInfo(int id)
        {
            var fi = _fonts.FirstOrDefault(x => x.Id == id);
            if (fi is null)
                return _fonts.First();
            else
                return fi;
        }

        public FontInfo GetFontInfo(string friendlyName)
        {
            if (string.IsNullOrEmpty(friendlyName))
                friendlyName = FONT_1;
            var fi = _fonts.FirstOrDefault(x => x.FriendlyName.ToLower() == friendlyName.ToLower());
            if (fi is null)
                return _fonts.First();
            else
                return fi;
        }

        public string GetFontPath(string friendlyName)
        {
            var fi = GetFontInfo(friendlyName);
            return Path.Combine(_fontsPath, fi.FontFile);
        }

        public Stream GetFontStream(string friendlyName)
        {
            return new FileStream(GetFontPath(friendlyName), FileMode.Open, FileAccess.Read);
        }

        public PointF AlignText(string s, Rectangle rect, ContentAlignment a, Graphics g, Font f, StringFormat sformat)
        {
            PointF pf = default;
            var sf = g.MeasureString(s, f, rect.Width, sformat);
            if (a == ContentAlignment.BottomLeft || a == ContentAlignment.MiddleLeft || a == ContentAlignment.TopLeft)
                pf.X = rect.X;
            else if (a == ContentAlignment.BottomCenter || a == ContentAlignment.MiddleCenter || a == ContentAlignment.TopCenter)
                pf.X = rect.Width / 2 - sf.Width / 2 + rect.X;
            else
                pf.X = rect.Width - sf.Width + rect.X;

            if (a == ContentAlignment.TopCenter || a == ContentAlignment.TopLeft || a == ContentAlignment.TopRight)
                pf.Y = rect.Y;
            else if (a == ContentAlignment.MiddleCenter || a == ContentAlignment.MiddleLeft || a == ContentAlignment.MiddleRight)
                pf.Y = rect.Height / 2 - sf.Height / 2 + rect.Y;
            else
                pf.Y = rect.Height - sf.Height + rect.Y;

            return pf;
        }

        public void DrawStringOnImage(string text, Rectangle r, string fontFriendlyName, Image image)
        {
            StringFormat sFormat = StringFormat.GenericDefault;

            // alignments should match with SignaturesHelper.SignFile in API project, to show
            // consistent previews with final signed files.
            sFormat.Alignment = StringAlignment.Center; // horiz.
            sFormat.LineAlignment = StringAlignment.Center; // vertical
            sFormat.FormatFlags = (sFormat.FormatFlags | StringFormatFlags.NoClip) & ~StringFormatFlags.NoWrap;

            var bestFontSize = GetBestFontFit(r, text, fontFriendlyName);
            using (var g = Graphics.FromImage(image))
            {
                using (var pfc = new PrivateFontCollection())
                {
                    var fontFile = GetFontPath(fontFriendlyName);
                    pfc.AddFontFile(fontFile);
                    var fontFamily = pfc.Families.First();
                    using (var f = new Font(fontFamily, bestFontSize, FontStyle.Regular))
                    {
                        var p = AlignText(text, r, ContentAlignment.MiddleCenter, g, f, sFormat);
                        var sf = g.MeasureString(text, f, r.Width, sFormat);
                        var rf = new RectangleF(p.X, p.Y, sf.Width, sf.Height);
                        g.TextRenderingHint = TextRenderingHint.AntiAlias;
                        g.DrawString(text, f, Brushes.Black, rf, sFormat);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the best fit of a text in a rectangle using the selected font
        /// </summary>
        /// <param name="r"></param>
        /// <param name="text"></param>
        /// <param name="fontFriendlyName"></param>
        /// <returns></returns>
        public float GetBestFontFit(Rectangle r, string text, string fontFriendlyName)
        {
            float bestFontSize;
            using (Bitmap dummy = new Bitmap(r.Width,
                                    r.Height,
                                    System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                using (var g = Graphics.FromImage(dummy))
                {
                    StringFormat sFormat = StringFormat.GenericDefault;
                    sFormat.LineAlignment = StringAlignment.Center;
                    sFormat.Alignment = StringAlignment.Center;
                    sFormat.FormatFlags = (sFormat.FormatFlags | StringFormatFlags.NoClip) & ~StringFormatFlags.NoWrap;
                    bestFontSize = GetBestFontFit(text, g, fontFriendlyName, FontStyle.Regular, sFormat, dummy.Size);
                }
            }
            return bestFontSize;
        }

        /// <summary>
        /// Gets the best fit of a text in a graphics context
        /// </summary>
        /// <param name="text"></param>
        /// <param name="g"></param>
        /// <param name="fontFriendlyName"></param>
        /// <param name="fontStyle"></param>
        /// <param name="sf"></param>
        /// <param name="room"></param>
        /// <returns></returns>
        public float GetBestFontFit(string text, Graphics g, string fontFriendlyName, FontStyle fontStyle, StringFormat sf, SizeF room)
        {
            var spaceUsed = new SizeF();
            float maxfontsize = 42;
            float minfontsize = 6;

            var adjustedSize = maxfontsize;
            var fontFile = GetFontPath(fontFriendlyName);

            using (var pfc = new PrivateFontCollection())
            {
                pfc.AddFontFile(fontFile);
                var fontFamily = pfc.Families.First();

                while (adjustedSize >= minfontsize)
                {
                    using (var f = new Font(fontFamily, adjustedSize, fontStyle))
                    {
                        spaceUsed = g.MeasureString(text, f, Convert.ToInt32(room.Width), sf);
                        if (spaceUsed.Width < room.Width && spaceUsed.Height < room.Height)
                        {
                            return adjustedSize;
                        }
                    }

                    adjustedSize--;
                }
            }
            return adjustedSize;
        }
    }
}
