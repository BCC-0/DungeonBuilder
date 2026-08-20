using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// The SaveException is thrown when one or more of the save conditions is not met.
/// </summary>
public class PlaytestException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlaytestException"/> class.
    /// Creates a new PlaytestException given errors causing the exception.
    /// </summary>
    /// <param name="errors">The errors causing this exception.</param>
    public PlaytestException(IEnumerable<string> errors)
        : base(FormatMessage(errors))
    {
        this.Errors = new List<string>(errors);
    }

    /// <summary>
    /// Gets a list of errors we get when trying to save the builder.
    /// </summary>
    public List<string> Errors { get; }

    private static string FormatMessage(IEnumerable<string> errors)
    {
        return "Can't playtest:\n" + string.Join("\n", errors);
    }
}