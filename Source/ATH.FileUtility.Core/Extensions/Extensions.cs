namespace ATH.FileUtility.Core.Extensions
{
    using System.IO;

    internal static class Extensions
    {
        /// <summary>
        /// Checks a given filepath for accessibility.
        /// </summary>
        /// <param name="filepath">Filepath to check</param>
        /// <returns>True if the file was successfully accessed. False if some other process is blocking the file.</returns>
        internal static bool IsFilepathAccessible(this string filepath)
        {
            if (!File.Exists(filepath))
            {
                return false;
            }

            try
            {
                using (var filestream = File.Open(filepath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    return true;
                }
            }
            catch (IOException)
            {
                return false;
            }
        }
    }
}
