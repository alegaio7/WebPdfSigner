using Microsoft.AspNetCore.Mvc;
using DesktopModule.Helpers;

namespace DesktopModule.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private IWebRequestsCoordinator _coordinator;

        public HealthController(IWebRequestsCoordinator coordinator)
        {
            _coordinator = coordinator;
            CultureHelper.SetLanguage(Globals._currentLang);
        }

        // silent is used by tray icon to avoid showing the desktop notification
        [HttpGet]
        public IActionResult Get(bool silent = false)
        {
            var req = _coordinator.CheckDesktopAgentRequest(silent);
            var ret = new ControllerResultBase();
            if (req.Cancel)
                ret.Message = req.CancelReason;
            else
                ret.Result = true;
            return Ok(ret);
        }
    }
}