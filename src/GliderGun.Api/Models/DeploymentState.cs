namespace DD.Research.GliderGun.Api.Models
{
    /// <summary>
    ///     Represents the state of a deployment.
    /// </summary>
    public enum DeploymentState
    {
        /// <summary>
        ///     An unknown deployment state.
        /// </summary>
        /// <remarks>
        ///     Used to detect uninitialised values; do not use this value directly.
        /// </remarks>
        Unknown     = 0,

        /// <summary>
        ///     The deployment is requested.
        /// </summary>
        Initiated = 1,

        /// <summary>
        ///     The deployment is running.
        /// </summary>
        Running     = 2,

        /// <summary>
        ///     The deployment completed successfully.
        /// </summary>
        Successful  = 3,

        /// <summary>
        ///     The deployment failed.
        /// </summary>
        Failed      = 4,

        /// <summary>
        ///     The deployment is requested.
        /// </summary>
        Deleted = 5,
    }
}