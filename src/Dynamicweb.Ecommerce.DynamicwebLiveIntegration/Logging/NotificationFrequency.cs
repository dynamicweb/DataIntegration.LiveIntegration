namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Logging
{
  /// <summary>
  /// Defines the log email send frequency.
  /// </summary>
  public enum NotificationFrequency
  {
    /// <summary>
    /// The never
    /// </summary>
    Never = 0,

    /// <summary>
    /// Send once a minute.
    /// </summary>
    Minute = 1,

    /// <summary>
    /// Send once every 5 minutes.
    /// </summary>
    FiveMinutes = 5,

    /// <summary>
    /// Send once every 10 minutes.
    /// </summary>
    TenMinutes = 10,

    /// <summary>
    /// Send once every 15 minutes.
    /// </summary>
    FifteenMinutes = 15,

    /// <summary>
    /// Send once every 30 minutes.
    /// </summary>
    ThirtyMinutes = 30,

    /// <summary>
    /// Send once every hour.
    /// </summary>
    OneHour = 60,

    /// <summary>
    /// Send once every 2 hours.
    /// </summary>
    TwoHours = 120,

    /// <summary>
    /// Send once every 3 hours.
    /// </summary>
    ThreeHours = 180,

    /// <summary>
    /// Send once every 6 hours.
    /// </summary>
    SixHours = 360,

    /// <summary>
    /// Send once every 12 hours.
    /// </summary>
    TwelveHours = 720,

    /// <summary>
    /// Send once a day.
    /// </summary>
    OneDay = 1440
  }
}