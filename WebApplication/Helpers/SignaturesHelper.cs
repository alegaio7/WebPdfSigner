using System.Drawing;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Parsing;
using Syncfusion.Pdf.Security;
using WebTestApplication.Signatures;
using WebTestApplication.WebApi;

namespace WebTestApplication.Helpers
{
    public class SignaturesHelper
    {
        private FontsHelper _fh;
        private IWebHostEnvironment _env;

        public SignaturesHelper(FontsHelper fh, IWebHostEnvironment env)
        {
            _fh = fh;
            _env = env;
        }

        public static SizeF GetProportionalSize(SizeF originalSize, SizeF targetSize)
        {
            var propx = targetSize.Width / originalSize.Width;
            var propy = targetSize.Height / originalSize.Height;
            float min = Math.Min(propx, propy);
            SizeF ret = new SizeF(originalSize.Width * min, originalSize.Height * min);
            return ret;
        }

        public FilePreparedForSigning PrepareFileForLocalSigning(
            SignatureBox signatureBox,
            string fontName,
            X509Certificate2 clientPubCert,
            string file,
            PrepareForSigningRequest request)
        {

            Bitmap signatureImage = default;
            StringBuilder signText = new StringBuilder();
            PdfBitmap image = default;
            byte[] signatureImageBytes = request.SignatureImage;
            MemoryStream signatureImageStream = default;
            PdfLoadedDocument pdfDocument = default;
            PdfTrueTypeFont ttf = default;

            try
            {
                var ret = new FilePreparedForSigning();
                var pdfRectangle = new Rectangle();
                pdfRectangle.X = signatureBox.x;
                pdfRectangle.Y = signatureBox.y;
                pdfRectangle.Width = signatureBox.w;
                pdfRectangle.Height = signatureBox.h;

                if (pdfRectangle.Width < Globals.MIN_PDF_SIGNATURE_AREA_WIDTH ||
                    pdfRectangle.Height < Globals.MIN_PDF_SIGNATURE_AREA_HEIGHT ||
                    pdfRectangle.Width > Globals.MAX_PDF_SIGNATURE_AREA_WIDTH ||
                    pdfRectangle.Height > Globals.MAX_PDF_SIGNATURE_AREA_HEIGHT)
                    throw new Exception("Invalid PDF signature area");

                byte[] pdfBytesToSign = null;
                using (var fsInput = new FileStream(file, FileMode.Open, FileAccess.Read))
                {
                    //Load an existing PDF document.
                    using (pdfDocument = new PdfLoadedDocument(fsInput))
                    {
                        if (pdfDocument.PageCount < 1)
                            throw new Exception(string.Format("File {0} has no pages", file));

                        PdfLoadedForm form = pdfDocument.Form;
                        var page = pdfDocument.Pages[0];
                        var rot = (int)page.Rotation * 90;
                        var invRot = (360 - rot) % 360; // calculates the inverse rotation: the rotations that should be made to PDF objects to look good in a rotated page

                        Rectangle surface = new Rectangle(0, 0, (int)page.Size.Width, (int)page.Size.Height);
                        pdfRectangle = RotateRectangleTo0(pdfRectangle, invRot, surface);

                        if (signatureImageBytes != null && signatureImageBytes.Length > 0)
                        {
                            signatureImageStream = new MemoryStream(signatureImageBytes); //don't use 'using'; the stream must be kept opened
                            signatureImage = new Bitmap(signatureImageStream);

                            // some PDFs are rotated, so rotate the image if needed to look good in the final PDF
                            if (invRot == 90)
                                signatureImage.RotateFlip(RotateFlipType.Rotate90FlipNone);
                            else if (invRot == 180)
                                signatureImage.RotateFlip(RotateFlipType.Rotate180FlipNone);
                            else if (invRot == 270)
                                signatureImage.RotateFlip(RotateFlipType.Rotate270FlipNone);

                            if (invRot != 0)
                                using (var ms = new MemoryStream())
                                {
                                    signatureImage.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                                    signatureImageBytes = ms.ToArray();
                                }
                        }

                        // Creates a digital signature.
                        PdfSignature signature = new PdfSignature(pdfDocument, pdfDocument.Pages[0], null, Globals.LOCAL_SIGNATURE_NAME);

                        signature.Bounds = new Syncfusion.Drawing.RectangleF(pdfRectangle.X, pdfRectangle.Y, pdfRectangle.Width, pdfRectangle.Height);
                        signature.Settings.CryptographicStandard = CryptographicStandard.CADES;
                        signature.Settings.DigestAlgorithm = DigestAlgorithm.SHA256;

                        if (!string.IsNullOrEmpty(request.NameInSignature))
                        {
                            signature.SignedName = request.NameInSignature; // this is a signature attribute
                            signText.Append(request.NameInSignature);   // this is the text drawn in the graphic part of the signature
                        }
                        if (signText.Length > 0)
                            signText.Append(Environment.NewLine);

                        signText.Append($"DN: {clientPubCert.Subject}");

                        RectangleF textRect = new RectangleF(new Point(0, 0), pdfRectangle.Size); //use 0,0 since coordinates are relative to signature, not to page. 
                        textRect = RotateRectangleTo0(Rectangle.Round(textRect), invRot, surface);
                        textRect.X = 0;
                        textRect.Y = 0;

                        var imageRect = signature.Bounds;
                        imageRect.X = 0;
                        imageRect.Y = 0;

                        // if there's an image included with the signature, divide the available space in half
                        // one half for the image, the other for the text
                        if (signatureImage != null)
                        {
                            if (rot == 90 || rot == 270)
                            {
                                imageRect.Height /= 2; // image rect is in unrotated pdf coordinates, while
                                textRect.Width /= 2; // text rect is still in view coordinates
                                if (rot == 90)
                                    imageRect.Y += imageRect.Height; // offset image half-size to bottom
                                else
                                    textRect.Y += textRect.Height; // offset text half-size to bottom // UNTESTED
                            }
                            else
                            {
                                imageRect.Width /= 2;
                                textRect.X += textRect.Width / 2;
                                textRect.Width /= 2;
                            }

                            using (var tmps = new MemoryStream(signatureImageBytes))
                                image = new PdfBitmap(tmps);
                            //image.Quality = 100; // not avail. in net core?
                            var imgRect = new SizeF(imageRect.Size.Width, imageRect.Size.Height);
                            var bestFitSize = GetProportionalSize(signatureImage.Size, imgRect);
                            var bestFitSizeSF = new Syncfusion.Drawing.SizeF(bestFitSize.Width, bestFitSize.Height);
                            var bestFitRect = new Syncfusion.Drawing.RectangleF(new Syncfusion.Drawing.PointF(imageRect.X, imageRect.Y), bestFitSizeSF); //use 0,0 since coordinates are relative to signature, not to page.
                            signature.Appearance.Normal.Graphics.DrawImage(image, bestFitRect);
                        }

                        float bestFontSize = 0;
                        using (Bitmap dummy = new Bitmap(
                            Convert.ToInt32(textRect.Width * Globals.SCREEN_PPI / Globals.PDF_PPI),
                            Convert.ToInt32(textRect.Height * Globals.SCREEN_PPI / Globals.PDF_PPI),
                            System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                        {
                            using (var g = Graphics.FromImage(dummy))
                            {
                                StringFormat sFormat = StringFormat.GenericDefault;
                                sFormat.Alignment = StringAlignment.Near;
                                sFormat.FormatFlags = (StringFormatFlags)(((int)sFormat.FormatFlags | (int)StringFormatFlags.NoClip) & ~(int)StringFormatFlags.NoWrap);
                                bestFontSize = _fh.GetBestFontFit(signText.ToString(), g, fontName, FontStyle.Regular, sFormat, dummy.Size);
                            }
                        }

                        PdfStringFormat pdfSf = new PdfStringFormat();
                        pdfSf.Alignment = PdfTextAlignment.Center;
                        pdfSf.LineAlignment = PdfVerticalAlignment.Middle;

                        signature.Appearance.Normal.Graphics.Save();
                        if (rot == 90)
                        {
                            signature.Appearance.Normal.Graphics.TranslateTransform(0, textRect.Width);
                            signature.Appearance.Normal.Graphics.RotateTransform(-90);
                        }
                        else if (rot == 270)
                        {
                            signature.Appearance.Normal.Graphics.TranslateTransform(0, textRect.Width * -1); // untested
                            signature.Appearance.Normal.Graphics.RotateTransform(90);
                        }

                        ttf = new PdfTrueTypeFont(_fh.GetFontPath(fontName), bestFontSize);
                        signature.Appearance.Normal.Graphics.DrawString(
                            signText.ToString(),
                            ttf,
                            new PdfSolidBrush(new PdfColor(Syncfusion.Drawing.Color.Black)),
                            new Syncfusion.Drawing.RectangleF(textRect.X, textRect.Y, textRect.Width, textRect.Height),
                            pdfSf);
                        signature.Appearance.Normal.Graphics.Restore();

                        //Create an external signer.
                        var emptySignature = new SignEmpty("SHA256");

                        signature.AddExternalSigner(emptySignature, new List<X509Certificate2>() { clientPubCert }, null);

                        var tmpFile = Path.Combine(_env.WebRootPath, "tempFiles", Guid.NewGuid().ToString() + ".pdf");

                        //Saves the document with empty signature in a new file.
                        using (var fsOutput = new FileStream(tmpFile, FileMode.Create, FileAccess.ReadWrite))
                        {
                            pdfDocument.Save(fsOutput);
                            //Closes the document.
                            pdfDocument.Close(true);
                        }

                        pdfBytesToSign = emptySignature.Message;

                        ret.FileHash = pdfBytesToSign;
                        ret.OriginalFile = file;
                        ret.PreparedFile = tmpFile;
                    } // using loadedDocument
                } // using fsInput

                return ret;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (ttf != null)
                    ttf.Dispose();
                if (pdfDocument != null)
                {
                    pdfDocument.Close();
                    pdfDocument.Dispose();
                }
                if (signatureImage != null)
                    signatureImage.Dispose();
                if (signatureImageStream != null)
                    signatureImageStream.Dispose();
            }
        }

        public string CompleteLocalSigning(string preparedFile, byte[] signedBytes, List<X509Certificate2> pbcList)
        {
            try
            {
                using (var fsInput = new FileStream(preparedFile, FileMode.Open, FileAccess.Read))
                {
                    IPdfExternalSigner externalSigner = new ExternalSigner("SHA256", signedBytes);
                    var tmpFile = Path.Combine(_env.WebRootPath, "tempFiles", Guid.NewGuid().ToString() + ".pdf");

                    using (var fsOutput = new FileStream(tmpFile, FileMode.Create, FileAccess.ReadWrite))
                    {
                        PdfSignature.ReplaceEmptySignature(fsInput, string.Empty, fsOutput, Globals.LOCAL_SIGNATURE_NAME, externalSigner, pbcList);
                    }

                    return tmpFile;
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                try
                {
                    File.Delete(preparedFile);
                }
                catch (Exception)
                {
                }
            }
        }

        private Rectangle RotateRectangleTo0(Rectangle r, int currentRot, Rectangle surface)
        {
            if (currentRot == 90)
            {
                var t = r.X;
                r.X = surface.Width - r.Y - r.Height;
                r.Y = surface.Height - t;
                t = r.Width;
                r.Width = r.Height;
                r.Height = t;
            }
            else if (currentRot == 270)
            {
                var t = r.X;
                r.X = r.Y;
                r.Y = surface.Height - t - r.Width;
                t = r.Width;
                r.Width = r.Height;
                r.Height = t;
            }

            return r;
        }
    }
}
