using CementGB.Mod.Utilities;
using System.Collections.Generic;

namespace Tungsten;
public class ErrorManager
{
    private struct ErrorInfo
    {
        public int lineIdx;
        public int charIdx;
        public string message;

        public ErrorInfo(int line, int charIdx, string message)
        {
            this.lineIdx = line;
            this.charIdx = charIdx;
            this.message = message;
        }
    }

    private readonly List<string> lines = new();
    private string currentLine = "";

    private readonly List<ErrorInfo> unresolvedErrors = new();

    public ErrorManager()
    {
        HadError = false;
    }

    public bool HadError
    {
        get;
        private set;
    }

    public void AddChar(char chr)
    {
        currentLine += chr;
    }

    public void AppendAsLine()
    {
        lines.Add(currentLine);
        currentLine = "";

        ResolveUnhandeledErrors();
    }

    private void ResolveUnhandeledErrors()
    {
        var errors = unresolvedErrors.ToArray();
        unresolvedErrors.Clear();

        foreach (var error in errors)
        {
            RaiseSyntaxError(error.lineIdx, error.charIdx, error.message);
        }
    }

    public void RaiseSyntaxError(int lineIdx, int charIdx, string message)
    {
        HadError = true;

        if (lineIdx >= lines.Count)
        {
            // we might throw an error before a line has been fully processed,
            // in which case we store it so that when the line is processed,
            // we can raise the error
            unresolvedErrors.Add(new ErrorInfo(lineIdx, charIdx, message));
        }
        else
        {
            var line = lines[lineIdx];

            var errorMessage = $"TungstenSyntaxError[ln {lineIdx + 1}]: {message}\n";

            var lineIdxString = (lineIdx + 1).ToString();
            var digits = lineIdxString.Length;
            errorMessage += $"{lineIdxString} | {line}\n";

            var pointerLine = "";
            for (var _ = 0; _ < digits; ++_)
                pointerLine += " ";

            pointerLine += " | ";

            for (var _ = 0; _ < charIdx; ++_)
                pointerLine += " ";

            pointerLine += "^";
            errorMessage += pointerLine;

            LoggingUtilities.Logger.Error(errorMessage);
        }
    }

    public void RaiseRuntimeError(int lineIdx, string message)
    {
        HadError = true;

        if (lineIdx < 0)
        {
            LoggingUtilities.Logger.Error($"TungstenRuntimeError: {message}\n");
            return;
        }

        var errorMessage = $"TungstenRuntimeError[ln {lineIdx + 1}]: {message}\n";
        var line = lines[lineIdx];

        errorMessage += $"{lineIdx + 1} | {line}";

        LoggingUtilities.Logger.Error(errorMessage);
    }
}
