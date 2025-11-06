namespace GrpcService.Services;

/// <summary>
/// Service for formatting greeting messages.
/// </summary>
public interface IGreetingFormatter
{
    /// <summary>
    /// Formats a greeting message for the given name.
    /// </summary>
    /// <param name="name">The name to greet.</param>
    /// <returns>The formatted greeting message.</returns>
    string FormatGreeting(string name);
}
