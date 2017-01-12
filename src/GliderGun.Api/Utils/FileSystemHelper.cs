using System;
using System.IO;

namespace DD.Research.GliderGun.Api.Utils
{
    /// <summary>
    ///     Helper methods for working with file-system objects.
    /// </summary>
    public static class FileSystemHelper
    {
        /// <summary>
        ///     Get a <see cref="DirectoryInfo"/> representing a subdirectory.
        /// </summary>
        /// <param name="directory">
        ///     The parent directory.
        /// </param>
        /// <param name="path">
        ///     The relative path of the subdirectory, under the parent directory.
        /// </param>
        /// <returns>
        ///     The new <see cref="DirectoryInfo"/>.
        /// </returns>
        public static DirectoryInfo Subdirectory(this DirectoryInfo directory, string path)
        {
            if (directory == null)
                throw new ArgumentNullException(nameof(directory));

            return new DirectoryInfo(
                Path.Combine(directory.FullName, path)
            );
        }

        /// <summary>
        ///     Get a <see cref="FileInfo"/> representing a file.
        /// </summary>
        /// <param name="directory">
        ///     The parent directory.
        /// </param>
        /// <param name="path">
        ///     The relative path of the file, under the parent directory.
        /// </param>
        /// <returns>
        ///     The new <see cref="FileInfo"/>.
        /// </returns>
        public static FileInfo File(this DirectoryInfo directory, string path)
        {
            if (directory == null)
                throw new ArgumentNullException(nameof(directory));

            return new FileInfo(
                Path.Combine(directory.FullName, path)
            );
        }
    }
}