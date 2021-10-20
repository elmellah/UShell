namespace UShell
{
    public interface IConsole
    {
        void Init(bool headless);
        void AddLog(Log log);
        bool ProcessEvent(string label, string[] args);
    }
}
