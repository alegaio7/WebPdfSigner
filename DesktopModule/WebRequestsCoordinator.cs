using DesktopModule.Helpers;
using System.Reflection;
using System.Resources;

namespace DesktopModule
{
    /// <summary>
    /// Handles requests from the browser and raises events that are handled by a desktop UI (a form)
    /// </summary>
    public class WebRequestsCoordinator : IWebRequestsCoordinator
    {
        private ResourceManager _rm = new ResourceManager(Globals.STRING_RESOURCES, Assembly.GetExecutingAssembly());

        public delegate void ReportEventDelegate(string eventData, Globals.EventType eventType);

        public event ReportEventDelegate ReportEvent;

        private void ReportEventData(Exception ex)
        {
            ReportEventData(ex.GetFullMessage(), Globals.EventType.Error);
        }

        private void ReportEventData(string data, Globals.EventType type = Globals.EventType.Info)
        {
            try
            {
                if (ReportEvent != null)
                    ReportEvent.Invoke(data, type);
            }
            catch (Exception)
            {
            }
        }

        #region ICoordinator impl

        private void HandleInvoke(MulticastDelegate ev, object[] args = null)
        {
            try
            {
                if (ev is null)
                    return;
                var list = ev.GetInvocationList();
                foreach (var d in list)
                {
                    try
                    {
                        d.Method.Invoke(d.Target, args);
                    }
                    catch (Exception ex)
                    {
                        ReportEventData($"Error calling delegate from Coordinator: {ex.Message}", Globals.EventType.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                ReportEventData($"Error reporting event from Coordinator: {ex.Message}", Globals.EventType.Error);
            }
        }

        public delegate void SelectDigitalIdEventDelegate(SelectDigitalIdUIEventArgs e);

        public event SelectDigitalIdEventDelegate SelectDigitalIdEvent;

        public SelectDigitalIdUIResult SelectDigitalIdRequest()
        {
            var r = new SelectDigitalIdUIResult() { Cancel = true, CancelReason = _rm.GetString("EVENT_NOT_HANDLED") };
            if (SelectDigitalIdEvent != null)
            {
                var e = new SelectDigitalIdUIEventArgs();
                HandleInvoke(SelectDigitalIdEvent, new[] { e });
                if (!e.Handled)
                    return r;

                if (!e.Cancel)
                {
                    r.CertificateEncodedBase64 = e.CertificateEncodedBase64;
                    r.CertificateFriendlyName = e.CertificateFriendlyName;
                    r.Cancel = false;
                }
                else
                {
                    if (string.IsNullOrEmpty(e.CancelReason))
                        r.CancelReason = _rm.GetString("ACTION_CANCELLED_BY_USER");
                    else
                        r.CancelReason = e.CancelReason;
                }
            }
            return r;
        }

        public delegate void SignHashesEventDelegate(SignHashesUIEventArgs e);

        public event SignHashesEventDelegate SignHashesEvent;

        public SignHashesUIResult SignHashesRequest(SignHashesWebRequest request)
        {
            var r = new SignHashesUIResult() { Cancel = true, CancelReason = _rm.GetString("EVENT_NOT_HANDLED") };
            if (SignHashesEvent != null)
            {
                var e = new SignHashesUIEventArgs();
                e.CertificateThumbprint = request.CertificateThumbprint;
                e.FileHashesToSign = request.FileHashesToSign;

                HandleInvoke(SignHashesEvent, new[] { e });
                if (!e.Handled)
                    return r;

                if (!e.Cancel)
                {
                    r.SignedHashes = e.FileHashesToSign;
                    r.Cancel = false;
                }
                else
                {
                    if (string.IsNullOrEmpty(e.CancelReason))
                        r.CancelReason = _rm.GetString("ACTION_CANCELLED_BY_USER");
                    else
                        r.CancelReason = e.CancelReason;
                }
            }
            return r;
        }

        public delegate void CheckDesktopAgentEventDelegate(CheckDesktopAgentUIEventArgs e);

        public event CheckDesktopAgentEventDelegate CheckDesktopAgentEvent;

        public CheckDesktopAgentUIResult CheckDesktopAgentRequest(bool silent)
        {
            var r = new CheckDesktopAgentUIResult() { Cancel = true, CancelReason = _rm.GetString("EVENT_NOT_HANDLED") };
            if (CheckDesktopAgentEvent != null)
            {
                var e = new CheckDesktopAgentUIEventArgs(silent);
                HandleInvoke(CheckDesktopAgentEvent, new[] { e });
                if (!e.Handled)
                    return r;

                r.Cancel = e.Cancel;
            }
            return r;
        }

        #endregion ICoordinator impl
    }
}