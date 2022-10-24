using DesktopModule.Helpers;
using System.Reflection;
using System.Resources;
using System.Security.Cryptography.X509Certificates;

namespace DesktopModule
{
    public partial class Main : Form
    {
        private const string SETTING_WINDOW_STATE = "WindowState";

        private ResourceManager _rm = new ResourceManager(Globals.STRING_RESOURCES, Assembly.GetExecutingAssembly());

        private CancellationTokenSource _hostCanceller = new CancellationTokenSource();

        private WebRequestsCoordinator _webRequestsCoordinator;

        public Main()
        {
            InitializeComponent();
            this.Text = _rm.GetString("Main.Title");
            lblLog.Text = _rm.GetString("Main.lblLog.Text");
            toolTip1.SetToolTip(btnCopy, _rm.GetString("Main.btnCopy.TooltipText"));
            toolTip1.SetToolTip(btnClear, _rm.GetString("Main.btnClear.TooltipText"));

            mnuOpen.Text = _rm.GetString("Main.mnuOpen.Text");
            mnuExit.Text = _rm.GetString("Main.mnuExit.Text");
        }

        private void Main_Load(object sender, EventArgs e)
        {
            try
            {
                lblVersion.Text = VersionInfo.GetVersionString();

                using (var cm = new ConfigManager(Globals.CONFIG_FILE))
                {
                    cm.Form = this;
                    cm.LoadConfig();

                    if (cm.OpenConfigFile())
                    {
                        if (!string.IsNullOrEmpty(Environment.CommandLine) && Environment.CommandLine.Contains("/min"))
                        {
                            WindowState = FormWindowState.Minimized;
                        }
                        else
                        {
                            var ws = (FormWindowState)cm.ReadEnumSetting(SETTING_WINDOW_STATE, typeof(FormWindowState), this.WindowState);
                            if (ws == FormWindowState.Minimized)
                                WindowState = FormWindowState.Minimized;
                            else if (ws == FormWindowState.Maximized)
                                WindowState = FormWindowState.Maximized;
                        }
                    }
                }

                ConfigureWebServer(Globals.DEFAULT_WEBAPI_PORT);
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (MessageBox.Show(_rm.GetString("CLOSE_AGENT"), _rm.GetString("GLOBALS_M_WARNING"), MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.Cancel)
            {
                e.Cancel = true;
                return;
            }

            try
            {
                if (_webRequestsCoordinator != null)
                {
                    _webRequestsCoordinator.SelectDigitalIdEvent -= webRequestsCoordinator_SelectDigitalIdEvent;
                    _webRequestsCoordinator.SignHashesEvent -= webRequestsCoordinator_SignHashesEvent;
                    _webRequestsCoordinator.CheckDesktopAgentEvent -= webRequestsCoordinator_CheckDesktopAgentEvent;
                }

                _hostCanceller.Cancel();

                using (var cm = new ConfigManager(Globals.CONFIG_FILE))
                {
                    cm.Form = this;
                    cm.SaveConfig();
                    if (cm.OpenConfigFile())
                        cm.SaveEnumSetting(SETTING_WINDOW_STATE, this.WindowState);
                }
            }
            catch (Exception)
            {
            }
        }

        private void ShowError(Exception e)
        {
            var s = GetErrorMessage(e);
            ShowError(s);
        }

        private void ShowError(string e)
        {
            MessageBox.Show(e, Globals.M_ERROR, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private string GetErrorMessage(Exception e)
        {
            return e.Message;
        }

        private void ShowNotification(string message, string title, ToolTipIcon icon, int tipTime = 8000)
        {
            if (InvokeRequired)
            {
                Invoke((Action)(() =>
                {
                    ShowNotification(message, title, icon, tipTime);
                }));
                return;
            }

            notifyIcon1.ShowBalloonTip(tipTime, title, message, icon);
        }

        private void AddLog(Exception ex)
        {
            AddLog(ex.GetFullMessage(), Globals.EventType.Error);
        }

        private void AddLog(string data, Globals.EventType eventType = Globals.EventType.Info)
        {
            if (InvokeRequired)
            {
                Invoke((Action)(() =>
                {
                    AddLog(data);
                }));
                return;
            }

            if (txtLog.TextLength > 0)
                txtLog.AppendText(Environment.NewLine);

            txtLog.AppendText(DateTime.Now.ToString("G"));
            txtLog.AppendText(" ");
            if (eventType == Globals.EventType.Warning)
                txtLog.AppendText(_rm.GetString("EVENT_WARNING"));
            else if (eventType == Globals.EventType.Error)
                txtLog.AppendText(_rm.GetString("EVENT_ERROR"));
            else
                txtLog.AppendText(_rm.GetString("EVENT_INFO"));
            txtLog.AppendText(": ");

            txtLog.AppendText(data);
        }

        private void ConfigureWebServer(int port)
        {
            string baseAddress = $"https://localhost:{port}";
            AddLog(_rm.GetString("STARTING_WEB_SERVER"));

            try
            {
                _webRequestsCoordinator = new WebRequestsCoordinator();

                var host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(new[] { baseAddress })
                    .ConfigureWebHostDefaults(webBuilder =>
                    {
                        webBuilder.UseStartup<Startup>();
                        webBuilder.UseUrls(baseAddress);
                    });

                // register webRequestsCoordinator so it can be injected in api controllers
                host.ConfigureServices(s => s.AddSingleton<IWebRequestsCoordinator>(_webRequestsCoordinator));

                // whenever a web request comes in, it is handled by a controller which in turn forwards it
                // to the web requests coordinator. This, through the use of events, communicates the request to the UI (form)
                _webRequestsCoordinator.ReportEvent += webRequestsCoordinator_ReportEvent;
                _webRequestsCoordinator.SelectDigitalIdEvent += webRequestsCoordinator_SelectDigitalIdEvent;
                _webRequestsCoordinator.SignHashesEvent += webRequestsCoordinator_SignHashesEvent;
                _webRequestsCoordinator.CheckDesktopAgentEvent += webRequestsCoordinator_CheckDesktopAgentEvent;

                // runs the web server async, so this UI thread can continue to run
                host.Build().RunAsync(_hostCanceller.Token);

                AddLog(_rm.GetString("WEB_SERVER_STARTED"));
            }
            catch (Exception ex)
            {
                AddLog(ex);
            }
        }

        private void webRequestsCoordinator_ReportEvent(string eventData, Globals.EventType eventType)
        {
            AddLog(eventData, eventType);
        }

        private void webRequestsCoordinator_CheckDesktopAgentEvent(CheckDesktopAgentUIEventArgs e)
        {
            e.Handled = true;
            e.Cancel = false;

            AddLog(_rm.GetString("Event_CheckDesktopAgent"));

            if (!e.Silent)
                ShowNotification(_rm.GetString("DESKTOP_AGENT_STATUS"), _rm.GetString("DESKTOP_AGENT_STATUS_TITLE"), ToolTipIcon.Info);
        }

        private void webRequestsCoordinator_SignHashesEvent(SignHashesUIEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke((Action)(() =>
                {
                    webRequestsCoordinator_SignHashesEvent(e);
                }));
                return;
            }

            try
            {
                e.Handled = true;
                AddLog(_rm.GetString("Event_SignHashes"));

                if (string.IsNullOrEmpty(e.CertificateThumbprint))
                    throw new Exception(_rm.GetString("CERT_THUMBPRINT_EMPTY"));

                if (e.FileHashesToSign is null || e.FileHashesToSign.Count == 0)
                    throw new Exception(_rm.GetString("HASHES_TO_SIGN_NULL"));

                var localHashesCopy = e.FileHashesToSign;
                foreach (var h in localHashesCopy)
                {
                    AddLog(string.Format(_rm.GetString("CHECKING_FILE_HASH"), h.OriginalFile));
                    h.FileHashBin = Convert.FromBase64String(h.FileHash);
                }

                X509Certificate2 cert2 = default;
                using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
                {
                    store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadOnly);
                    var certs = store.Certificates.Find(X509FindType.FindByTimeValid, DateTime.Now, false);

                    foreach (var c in certs)
                    {
                        if (c.Thumbprint == e.CertificateThumbprint)
                        {
                            cert2 = c;
                            break;
                        }
                    }

                    if (cert2 is null)
                        throw new Exception(string.Format(_rm.GetString("CERT_THUMBPRINT_NOT_FOUND"), e.CertificateThumbprint));

                    AddLog(string.Format(_rm.GetString("SIGN_HASHES_USING_CERT"), e.FileHashesToSign.Count, cert2.Subject));

                    var se = new SignHelper();
                    foreach (var h in localHashesCopy)
                    {
                        AddLog(string.Format(_rm.GetString("SIGNING_FILE"), h.OriginalFile));
                        h.SignedFileHash = se.SignData(h.FileHashBin, cert2);
                    }

                    e.FileHashesToSign = localHashesCopy;

                    var s = string.Format(_rm.GetString("SIGNING_FILES_FINISHED"), e.FileHashesToSign.Count);
                    AddLog(s);
                    ShowNotification(s, _rm.GetString("SIGNING_FILES_TITLE"), ToolTipIcon.Info);
                }
            }
            catch (Exception ex)
            {
                e.Cancel = true;
                e.CancelReason = ex.Message;
                AddLog(ex);
            }
        }

        private void webRequestsCoordinator_SelectDigitalIdEvent(SelectDigitalIdUIEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke((Action)(() =>
                {
                    webRequestsCoordinator_SelectDigitalIdEvent(e);
                }));
                return;
            }

            try
            {
                e.Handled = true;
                AddLog(_rm.GetString("Event_SelectDigitalId"));
                using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
                {
                    store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadOnly);
                    var certs = store.Certificates.Find(X509FindType.FindByTimeValid, DateTime.Now, false);
                    var sel = X509Certificate2UI.SelectFromCollection(certs,
                        _rm.GetString("SELECT_DIGITALID_TITLE"),
                        _rm.GetString("SELECT_DIGITALID_LABEL"),
                        X509SelectionFlag.SingleSelection);

                    if (sel is null || sel.Count == 0)
                    {
                        AddLog(_rm.GetString("ACTION_CANCELLED_BY_USER"), Globals.EventType.Warning);
                        e.Cancel = true;
                        return;
                    }

                    var cert = sel[0];
                    var certBytes = cert.GetRawCertData();
                    var pubCertEncoded = Convert.ToBase64String(certBytes); // encode to pass it to the server
                    e.CertificateEncodedBase64 = pubCertEncoded;
                    e.CertificateFriendlyName = cert.Subject;
                }
            }
            catch (Exception ex)
            {
                e.Cancel = true;
                e.CancelReason = ex.Message;
                AddLog(ex);
            }
        }

        private void btnCopy_Click(object sender, EventArgs e)
        {
            if (txtLog.TextLength > 0)
                Clipboard.SetText(txtLog.Text);
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            txtLog.Clear();
        }

        private void Open()
        {
            this.WindowState = FormWindowState.Normal;
            this.BringToFront();
        }

        private void mnuOpen_Click(object sender, EventArgs e)
        {
            Open();
        }

        private void mnuExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void notifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            Open();
        }
    }
}