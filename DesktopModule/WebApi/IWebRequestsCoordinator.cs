namespace DesktopModule
{
    public interface IWebRequestsCoordinator
    {
        CheckDesktopAgentUIResult CheckDesktopAgentRequest(bool silent);

        SelectDigitalIdUIResult SelectDigitalIdRequest();

        SignHashesUIResult SignHashesRequest(SignHashesWebRequest request);
    }
}