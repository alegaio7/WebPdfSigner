namespace DesktopModule
{
    public class CheckDesktopAgentUIEventArgs : WebRequestCoordinatorBaseEventArgs
    {
        public CheckDesktopAgentUIEventArgs(bool silent) { 
            Silent = silent;
        }

        public bool Silent { get; private set; }
    }
}