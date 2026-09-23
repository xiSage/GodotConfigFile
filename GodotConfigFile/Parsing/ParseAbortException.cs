namespace GodotConfigFile;

/// <summary>
/// Thrown inside the parser to unwind to the nearest entry point when an error diagnostic is produced.
/// </summary>
/// <remarks>
/// This is an implementation detail: it never escapes the library. <see cref="ConfigFile.Parse(string, ParseOptions?)"/>
/// converts it into a <see cref="ConfigFileParseException"/>, and <see cref="ConfigFile.ParseWithResult"/> turns it
/// into a result object.
/// </remarks>
internal sealed class ParseAbortException : Exception
{
    /// <summary>Initializes the exception around the diagnostic that caused it.</summary>
    /// <param name="diagnostic">The error diagnostic, already recorded in the bag.</param>
    public ParseAbortException(Diagnostic diagnostic)
        : base(diagnostic.Message)
    {
        Diagnostic = diagnostic;
    }

    /// <summary>Gets the diagnostic that caused the abort.</summary>
    public Diagnostic Diagnostic { get; }
}
