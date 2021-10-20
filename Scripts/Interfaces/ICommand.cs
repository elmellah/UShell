using System.Collections.Generic;

namespace UShell
{
    public interface ICommand
    {
        string[] GetSyntaxes(string label);
        string[] GetInfos(string label);
        string GetCompletion(string label, string[] args, out List<string> options);

        void Execute(string label, string[] args);
    }
}
