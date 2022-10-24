using Microsoft.AspNetCore.Mvc;
using DesktopModule.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesktopModule.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class SignaturesController : ControllerBase
    {
        private IWebRequestsCoordinator _coordinator;
        public SignaturesController(IWebRequestsCoordinator coordinator)
        {
            _coordinator = coordinator;
            CultureHelper.SetLanguage(Globals._currentLang);
        }

        [HttpGet]
        public IActionResult SelectDigitalId()
        {
            var req = _coordinator.SelectDigitalIdRequest();
            var ret = new SelectDigitalIdWebResult();
            if (req.Cancel)
                ret.Message = req.CancelReason;
            else
            {
                ret.Result = true;
                ret.CertificateEncodedBase64 = req.CertificateEncodedBase64;
                ret.CertificateFriendlyName = req.CertificateFriendlyName;
            }
            return Ok(ret);
        }

        [HttpPost]
        public IActionResult SignHashes([FromBody] SignHashesWebRequest request)
        {
            var req = _coordinator.SignHashesRequest(request);
            var ret = new SignHashesWebResult();
            if (req.Cancel)
                ret.Message = req.CancelReason;
            else
            {
                ret.Result = true;
                ret.SignedHashes = req.SignedHashes;
            }
            return Ok(ret);
        }
    }
}
