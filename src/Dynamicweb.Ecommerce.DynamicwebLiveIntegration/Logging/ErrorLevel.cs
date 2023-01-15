namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Logging
{
  /// <summary>
  /// Specifies the error level for logging.
  /// </summary>
  public enum ErrorLevel
  {
    /// <summary>
    /// Log response and request content.
    /// </summary>
    DebugInfo,

    /// <summary>
    /// Log web service connection errors.
    /// </summary>
    ConnectionError,

    /// <summary>
    /// Log response errors.
    /// </summary>
    ResponseError,

    /// <summary>
    /// Logs general errors.
    /// </summary>
    Error,

    /// <summary>
    /// Determines if error logs should be sent by email.
    /// </summary>
    EmailSend
  }
}