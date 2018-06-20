namespace ATH.FileUtility.Core.Model
{
    using System;
    using System.IO;
    using System.Threading;
    using Extensions;

    /// <summary>
    /// Class for monitoring a file for changes.
    /// </summary>
    internal class FileMonitor : IDisposable
    {
        private const int DefaultTimerPeriodOneSecond = 1000;

        private readonly string filepath;
        private long fileSize;
        private readonly Timer monitorTimer;
        private readonly int timerPeriodInMilliseconds;

        public delegate void FileSizeDidNotChangeForIntervalDelegate(string filepath);

        /// <summary>
        /// Event triggert after the size of the monitored file did not change for the given interval.
        /// </summary>
        public event FileSizeDidNotChangeForIntervalDelegate FileSizeDidNotChangeForInterval;

        /// <summary>
        /// New FileMonitor instance with default timeout period one second.
        /// </summary>
        /// <param name="filepath">The filepath to monitor.</param>
        public FileMonitor(string filepath)
            : this(filepath, DefaultTimerPeriodOneSecond)
        {
        }

        /// <summary>
        /// New FileMonitor instance.
        /// </summary>
        /// <param name="filepath">The filepath to monitor.</param>
        /// <param name="timerPeriodInMilliseconds">The timeout in milliseconds. After this period of time the FileSizeDidNotChangeForInterval interval is triggered.</param>
        public FileMonitor(string filepath, int timerPeriodInMilliseconds)
        {
            if (string.IsNullOrWhiteSpace(filepath))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(filepath));
            }

            this.filepath = filepath;
            this.timerPeriodInMilliseconds = timerPeriodInMilliseconds;
            this.monitorTimer = new Timer(this.CheckFile, null, Timeout.Infinite, this.timerPeriodInMilliseconds);
        }

        /// <summary>
        /// Starts monitoring
        /// </summary>
        public void Start() 
            => this.monitorTimer.Change(0, this.timerPeriodInMilliseconds);

        private void CheckFile(object state)
        {
            // Using Win32 to accecss filesize while file is blocked by another process.
            // FileSize from FileInfo is just cached metadata sometimes returning the complete size of the file instead
            // of the current size, e.g. the copied amount. The Win32 API is always returning 0 for a file which is beeing copied instead.
            var fileSize = Win32FileSize.Get(this.filepath);

            if (fileSize > this.fileSize)
            {
                this.fileSize = fileSize;
                return;
            }

            if (!this.filepath.IsFilepathAccessible())
            {
                return;
            }

            this.FileSizeDidNotChangeForInterval?.Invoke(this.filepath);
            this.monitorTimer.Change(Timeout.Infinite, Timeout.Infinite);
            this.monitorTimer.Dispose();
        }

        public void Dispose() 
            => this.monitorTimer?.Dispose();
    }
}