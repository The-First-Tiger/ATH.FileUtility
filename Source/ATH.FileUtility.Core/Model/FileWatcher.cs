namespace ATH.FileUtility.Core.Model
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// Class for monitoring folders for filechanges.
    /// </summary>
    public class FileWatcher : IDisposable
    {
        private readonly string folderPathToWatchForFileChanges;

        private readonly string fileFilterString;

        private readonly int fileDidNotChangeTimePeriodInMilliseconds;

        private readonly bool includeSubdirectories;

        private FileSystemWatcher fileSystemWatcher;
        private readonly bool isSubscribed;
        private readonly object @lock;

        private readonly Dictionary<string, FileMonitor> fileMonitors;

        public delegate void FileIsCreatedAndAccessibleEventHandler(string filepath);
        public delegate void FileSystemWatcherErrorEventHandler(Exception exception);

        /// <summary>
        /// Event triggered on errors.
        /// </summary>
        public event FileSystemWatcherErrorEventHandler ErrorOccured;

        /// <summary>
        /// Event triggered after a file in the monitored folder is created and accessible.
        /// </summary>
        public event FileIsCreatedAndAccessibleEventHandler FileIsCreatedAndAccessible;

        /// <summary>
        /// New Filewatcher for monitoring a given folderpath.
        /// </summary>
        /// <param name="folderPathToWatchForFileChanges">The folderpath to monitor.</param>
        /// <param name="fileFilterString">The filefilter. Only filechanges of files matching this filter are reported.</param>
        /// <param name="fileDidNotChangeTimePeriodInMilliseconds">Timeout in milliseconds a file must not change before being reported as accessible.</param>
        /// <param name="includeSubdirectories">Boolean indicating if subdirectories are included.</param>
        public FileWatcher(string folderPathToWatchForFileChanges, string fileFilterString, int fileDidNotChangeTimePeriodInMilliseconds = 1000, bool includeSubdirectories = true)
        {
            if (string.IsNullOrWhiteSpace(folderPathToWatchForFileChanges))
            {
                throw new ArgumentException(
                    "Value cannot be null or whitespace.", nameof(folderPathToWatchForFileChanges));
            }

            if (string.IsNullOrWhiteSpace(fileFilterString))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(fileFilterString));
            }

            this.folderPathToWatchForFileChanges = folderPathToWatchForFileChanges;
            this.fileFilterString = fileFilterString;
            this.fileDidNotChangeTimePeriodInMilliseconds = fileDidNotChangeTimePeriodInMilliseconds;
            this.includeSubdirectories = includeSubdirectories;

            this.@lock = new object();
            this.fileMonitors = new Dictionary<string, FileMonitor>();
        }

        /// <summary>
        /// Starts monitoring the given folderpath.
        /// </summary>
        public void Start()
        {
            this.fileSystemWatcher = new FileSystemWatcher(this.folderPathToWatchForFileChanges, this.fileFilterString)
                                     {
                                         IncludeSubdirectories = this.includeSubdirectories,
                                         NotifyFilter =
                                             NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.LastWrite
                                     };

            this.fileSystemWatcher.Created += this.FileSystemWatcherCreated;
            this.fileSystemWatcher.Changed += this.FileSystemWatcherChanged;
            this.fileSystemWatcher.Error += this.FileSystemWatcherError;

            this.fileSystemWatcher.EnableRaisingEvents = true;
        }

        /// <summary>
        /// Stops monitoring the given folderpath.
        /// </summary>
        public void Stop()
        {
            this.fileSystemWatcher.EnableRaisingEvents = false;

            this.fileSystemWatcher.Created -= this.FileSystemWatcherCreated;
            this.fileSystemWatcher.Changed -= this.FileSystemWatcherChanged;
            this.fileSystemWatcher.Error -= this.FileSystemWatcherError;

            this.fileSystemWatcher.Dispose();
            this.fileSystemWatcher = null;
        }

        private void OnFileIsCreatedAndAccessible(string filepath)
            => this.FileIsCreatedAndAccessible?.Invoke(filepath);

        private void FileSystemWatcherCreated(object sender, FileSystemEventArgs fileSystemEventArgs)
            => this.HandleChange(fileSystemEventArgs.FullPath);

        private void FileSystemWatcherChanged(object sender, FileSystemEventArgs fileSystemEventArgs)
            => this.HandleChange(fileSystemEventArgs.FullPath);

        private void FileSystemWatcherError(object sender, ErrorEventArgs e)
        {
            var exception = e.GetException();

            this.ErrorOccured?.Invoke(exception);

            this.Stop();
            this.Start();
        }

        private void HandleChange(string filepath)
        {
            if (this.fileMonitors.ContainsKey(filepath))
            {
                return;
            }

            var fileMonitor = new FileMonitor(filepath, this.fileDidNotChangeTimePeriodInMilliseconds);
            fileMonitor.FileSizeDidNotChangeForInterval += (path) =>
            {
                this.OnFileIsCreatedAndAccessible(path);
                this.fileMonitors.Remove(path);
            };

            this.fileMonitors.Add(filepath, fileMonitor);

            fileMonitor.Start();
        }

        public void Dispose()
        {
            foreach (var fileMonitor in this.fileMonitors)
            {
                fileMonitor.Value?.Dispose();
            }

            this.fileSystemWatcher?.Dispose();
        }
    }
}
