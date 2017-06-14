using System;
using System.Text;

namespace DD.Research.GliderGun.Api.Utils
{
    /// <summary>
	///     An entry from a Docker container log.
	/// </summary>
	public class DockerLogEntry
	{
        /// <summary>
        ///     The default encoding used in Docker logs.
        /// </summary>
        public static readonly Encoding DefaultEncoding = Encoding.ASCII;

        /// <summary>
        ///     The length of the header for a Docker log entry.
        /// </summary>
        public const int HeaderLength = 8;

        /// <summary>
		///     The 0-based offset of the frame size bytes within the header for a Docker log entry.
		/// </summary>
		public const int HeaderFrameSizeOffset = 4;

        /// <summary>
        ///     The log entry data.
        /// </summary>
        byte[] _data;

		/// <summary>
		///     Create a new <see cref="DockerLogEntry"/>.
		/// </summary>
		/// <param name="streamType">
		///     The type of stream (e.g. STDOUT, STDERR) represented by the <see cref="DockerLogEntry"/>.
		/// </param>
		/// <param name="data">
		///     The log entry data.
		/// </param>
        /// <param name="correlationId">
        ///     An optional message correlation Id.
        /// </param>
		public DockerLogEntry(DockerLogStreamType streamType, byte[] data, string correlationId = null)
		{
			if (data == null)
				throw new ArgumentNullException(nameof(data));

			StreamType = streamType;
			_data = data;
		}

		/// <summary>
		///     A <see cref="DockerLogStreamType"/> identifying the type of stream (e.g. STDOUT / STDERR) represented by the <see cref="DockerLogEntry"/>.
		/// </summary>
		public DockerLogStreamType StreamType { get; }

		/// <summary>
		///     Get the raw log entry data.
		/// </summary>
		public byte[] GetData()
        {
            byte[] data = new byte[_data.Length];
            Array.Copy(_data, data, data.Length);

            return data;
        }

        /// <summary>
        ///     Get the log entry as text.
        /// </summary>
        public string GetText(Encoding encoding = null) => (encoding ?? DefaultEncoding).GetString(_data);

		/// <summary>
		///     Read a <see cref="DockerLogEntry"/> from the specified data.
		/// </summary>
		/// <param name="data">
		///     A <see cref="byte[]"/> containing the data.
		/// </param>
        /// <param name="startIndex">
        ///     The starting index within the data of the log entry.
        /// </param>
		/// <returns>
		///     The log entry (or <c>null</c>, if not enough data is available) and the number of bytes read (including the log entry header).
		/// </returns>
		public static (DockerLogEntry logEntry, int bytesRead) ReadFrom(byte[] data, int startIndex = 0)
		{
			if (data == null)
				throw new ArgumentNullException(nameof(data));

            var (streamType, logEntryLength, isValidHeader) = ReadLogEntryHeader(data, startIndex);
            if (!isValidHeader || data.Length < HeaderLength + logEntryLength)
				return (logEntry: null, bytesRead: 0); // Not enough data.

            byte[] logEntryData = new byte[logEntryLength];
            Array.Copy(
                sourceArray: data,
                sourceIndex: startIndex + HeaderLength,
                destinationArray: logEntryData,
                destinationIndex: 0,
                length: logEntryLength
            );

            return (
				logEntry: new DockerLogEntry(streamType, logEntryData),
				bytesRead: HeaderLength + logEntryLength
			);
		}

		/// <summary>
		///     Read a docker log-entry header from the specified data.
		/// </summary>
		/// <param name="data">
		///     A <see cref="byte[]"/> representing the data.
		/// </param>
        /// <param name="startIndex">
        ///     The starting index within the data of the log entry.
        /// </param>
		/// <returns>
		///     The log entry stream type and length, and a value indicating whether a valid header was found in the data.
		/// </returns>
		/// <remarks>
		///     Each log entry has a header in the following format: [STREAM_TYPE, 0, 0, 0, SIZE1, SIZE2, SIZE3, SIZE4]
		///     
		///     Where STREAM_TYPE is a value from DockerLogEntryStreamType, and SIZE1-SIZE4 make up a big-endian Int32
		///     representing the length of the log entry data (excluding the header).
		///     
		///     See <see href="https://docs.docker.com/engine/api/v1.24/#brief-introduction"/> for further details.
		/// </remarks>
		public static (DockerLogStreamType streamType, int length, bool isValid) ReadLogEntryHeader(byte[] data, int startIndex = 0)
		{
			if (data == null)
				throw new ArgumentNullException(nameof(data));

			if (data.Length - startIndex < HeaderLength)
				return (streamType: DockerLogStreamType.Unknown, length: -1, isValid: false);

			byte[] header = new byte[HeaderLength];
            Array.Copy(
                sourceArray: data,
                sourceIndex: startIndex,
                destinationArray: header,
                destinationIndex: 0,
                length: HeaderLength
            );

            // Switch to little-endian.
            Array.Reverse(header, index: 4, length: 4);

			return (
				streamType: (DockerLogStreamType)header[0],
				length: BitConverter.ToInt32(header, startIndex: 4),
                isValid: true
			);
		}
    }

    /// <summary>
    ///     Well-known stream types for docker log entries.
    /// </summary>
    public enum DockerLogStreamType
        : Byte
    {
        /// <summary>
        ///     Standard input (STDIN).
        /// </summary>
        StdIn = 0,

        /// <summary>
        ///     Standard output (STDOUT).
        /// </summary>
        StdOut = 1,

        /// <summary>
        ///     Standard error (STDERR).
        /// </summary>
        StdErr = 2,

        /// <summary>
        ///     An unknown stream type.
        /// </summary>
        Unknown = 255
    }
}