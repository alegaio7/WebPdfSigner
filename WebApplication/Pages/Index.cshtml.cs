using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using System;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.IO.Compression;
using System.Net;
using System.Reflection.Emit;
using System.Security.Cryptography.X509Certificates;
using WebTestApplication.Helpers;
using WebTestApplication.Signatures;
using WebTestApplication.WebApi;

namespace WebTestApplication.Pages
{
    [IgnoreAntiforgeryToken]
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IStringLocalizer<IndexModel> _localizer;
        private IWebHostEnvironment _env;

        public class CAgentSignedHash
        {
            public string OriginalFile { get; set; }
            public string PreparedFile { get; set; }
            public string FileHash { get; set; } // comes base64-encoded from js client
            public string SignedFileHash { get; set; } // comes base64-encoded from js client
        }

        public class CCompleteSigningRequest
        {
            public string CertificateEncodedBase64 { get; set; }
            [Required]
            public List<CAgentSignedHash> SignedFileHashes { get; set; }
        }

        public IndexModel(ILogger<IndexModel> logger, IStringLocalizer<IndexModel> loc, IWebHostEnvironment webHostEnvironment)
        {
            _logger = logger;
            _localizer = loc;
            _env = webHostEnvironment;
        }

        public void OnGet()
        {

        }

        public async Task<IActionResult> OnPostUploadFile(IFormFile file)
        {
            if (file is null)
                return BadRequest(BoxContents(false, message: _localizer["No file received."].Value));

            try
            {
                string ext = Path.GetExtension(file.FileName);
                if (ext != null)
                    ext = ext.ToLower();

                if (ext != ".pdf")
                    return BadRequest(BoxContents(false, message: _localizer["Only pdf files are allowed"].Value));

                string webRootPath = _env.WebRootPath;
                var path = Path.Combine(webRootPath, "tempFiles");

                var tmpFile = Guid.NewGuid().ToString() + ".pdf";
                var tmpFilePath = Path.Combine(path, tmpFile);

                using (var fs = new FileStream(tmpFilePath, FileMode.Create)) {
                    await file.CopyToAsync(fs);
                }

                return new OkObjectResult(BoxContents(true, 
                    new { 
                        File = Path.GetFileName(tmpFile), 
                        OriginalFile = Path.GetFileName(file.FileName)
                    } 
                ));
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, BoxContents(false, message: ex.Message));
            }
        }

        public IActionResult OnPostPrepareFile([FromBody] PrepareForSigningRequest request) { 
            if (request is null)
                return BadRequest(BoxContents(false, message: _localizer["No request received."].Value));

            if (string.IsNullOrEmpty(request.File))
                return BadRequest(BoxContents(false, message: _localizer["No request File received."].Value));

            if (string.IsNullOrEmpty(request.OriginalFile))
                return BadRequest(BoxContents(false, message: _localizer["No request OriginalFile received."].Value));

            if (string.IsNullOrEmpty(request.CertificateEncodedBase64))
                return BadRequest(BoxContents(false, message: _localizer["No digital ID received for signing."].Value));

            string webRootPath = _env.WebRootPath;
            var path = Path.Combine(webRootPath, "tempFiles");

            var tmpFilePath = Path.Combine(path, request.File);
            if (!System.IO.File.Exists(tmpFilePath))
                return BadRequest(BoxContents(false, message: _localizer["No temp file was found with the name "].Value + request.File));

            // begin file preparation
            X509Certificate2 clientPubCert = default;
            SignatureBox signatureBox = default;
            var ret = new PrepareForLocalSigningResult();
            ret.FileHashesToSign = new List<FilePreparedForSigning>();

            // try to recreate the certificate from the base64 encoded info
            try
            {
                var pubCertDecoded = Convert.FromBase64String(request.CertificateEncodedBase64);
                clientPubCert = new X509Certificate2(pubCertDecoded);
                ret.CertificateThumbprint = clientPubCert.Thumbprint;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                throw new Exception(_localizer["Invalid encoded digital ID"].Value);
            }

            try
            {
                var fontId = 1; // hardcode font #1 for now... fonts are handled by FontsHelper
                var fh = new FontsHelper(_env);
                var fi = fh.GetFontInfo(fontId); 
                if (fi is null)
                    throw new Exception($"Font Id {fontId} not found.");

                // convert signature box from SCREEN to PDF coordinates
                var screenRect = new Rectangle(0, 0, 150, 60);
                signatureBox = new SignatureBox()
                {
                    x = screenRect.X * Globals.PDF_PPI / Globals.SCREEN_PPI,
                    y = screenRect.Y * Globals.PDF_PPI / Globals.SCREEN_PPI,
                    w = screenRect.Width * Globals.PDF_PPI / Globals.SCREEN_PPI,
                    h = screenRect.Height * Globals.PDF_PPI / Globals.SCREEN_PPI
                };

                var sigHelper = new SignaturesHelper(fh, _env);
                var prepFile = sigHelper.PrepareFileForLocalSigning(
                    signatureBox,
                    fi.FriendlyName,
                    clientPubCert,
                    tmpFilePath,
                request);

                prepFile.PreparedFile = Path.GetFileName(prepFile.PreparedFile); // don't pass full paths to client
                prepFile.OriginalFile = Path.GetFileName(request.OriginalFile);
                ret.FileHashesToSign.Add(prepFile);
                return new OkObjectResult(BoxContents(true, ret));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                throw;
            }
            finally
            {
                if (clientPubCert != null)
                    clientPubCert.Dispose();
            }
        }

        public IActionResult OnPostCompleteLocalSigning([FromBody] CCompleteSigningRequest request)
        {
            X509Certificate2 clientPubCert = default;
            var ret = new CompleteLocalSigningResult();

            if (request is null || request.SignedFileHashes is null || request.SignedFileHashes.Count == 0)
                return BadRequest(BoxContents(false, _localizer["No data received to complete the file signature."].Value));

            // try to recreate the certificate from the base64 encoded info
            try
            {
                var pubCertDecoded = Convert.FromBase64String(request.CertificateEncodedBase64);
                clientPubCert = new X509Certificate2(pubCertDecoded);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                throw new Exception(_localizer["Invalid encoded digital ID"].Value);
            }

            try
            {
                // this demo handles only one file at a time but the desktop module is prepared to sign several hashes at once
                var sfh = request.SignedFileHashes.First();
                string webRootPath = _env.WebRootPath;
                var path = Path.Combine(webRootPath, "tempFiles");
                var tmpFilePath = Path.Combine(path, sfh.PreparedFile);

                if (!System.IO.File.Exists(tmpFilePath))
                    return BadRequest(BoxContents(false, message: _localizer["No file was prepared with the name "].Value + tmpFilePath));

                var sigHelper = new SignaturesHelper(null, _env); // fontHelper not needed here
                var pbcList = new List<X509Certificate2>() { clientPubCert };
                byte[] h = Convert.FromBase64String(sfh.SignedFileHash);
                var signedFile = sigHelper.CompleteLocalSigning(tmpFilePath, h, pbcList); // prepared file is deleted if successful
                var signedFilename = Path.GetFileNameWithoutExtension(sfh.OriginalFile) + "-signed.pdf";

                var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
                string contentType;
                if (!provider.TryGetContentType(signedFilename, out contentType))
                {
                    contentType = "application/octet-stream";
                }
                return File(System.IO.File.ReadAllBytes(signedFile), contentType, signedFilename);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                throw;
            }
            finally
            {
                if (clientPubCert != null)
                    clientPubCert.Dispose();
            }
        }

        protected GenericJsonResponse BoxContents(bool result, object content = null, string message = null)
        {
            var resp = new GenericJsonResponse() { Result = result, Contents = content, Message = message };
            if (result)
                resp.StatusCode = (int)System.Net.HttpStatusCode.OK;
            else
                resp.StatusCode = (int)System.Net.HttpStatusCode.BadRequest;
            return resp;
        }
    }
}