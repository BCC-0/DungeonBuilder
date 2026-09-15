using System;

/// <summary>
/// Exception thrown when an invalid value is entered into a runtime editor field.
/// </summary>
public class EditException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EditException"/> class.
    /// </summary>
    /// <param name="message">The message of this error.</param>
    public EditException(string message)
        : base(message)
    {
    }
}