namespace ShowdownReplayScouter.Core.ReplayAnalyzers.LogAnalysis.Handlers
{
    /// <summary>
    /// Handles the log lines of one concern (e.g. items), every line is passed to every handler.
    /// A handler is created per replay and may keep state between lines.
    /// </summary>
    internal interface ILogLineHandler
    {
        void Handle(ProtocolLine line, AnalysisContext context);
    }
}
