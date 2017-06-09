namespace DD.Research.GliderGun.Api
{    /// <summary>
    ///     Provides constant variables for diagnostics.
    /// </summary>
    internal static class Constants
    {
        /// <summary>
        ///     The activity Id name.
        /// </summary>
        public const string ActivityIdHeaderName = "X-ActivityId";

        /// <summary>
        /// The header name for calling service name
        /// </summary>
        public const string RequestingServiceNameHeaderName = "X-RequestingServiceName";

        /// <summary>
        /// The header name for calling service version
        /// </summary>
        public const string RequestingServiceVersionHeaderName = "X-RequestingServiceVersion";
    }
}