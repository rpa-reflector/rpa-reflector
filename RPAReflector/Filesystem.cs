using System;
using System.IO;

namespace RPAReflector
{
    public static class Filesystem
    {
        private static string _tempDirectory = null;
        private static string _webRootDirectory = null;

        public static string GetTemporaryDirectory()
        {
            if (_tempDirectory == null)
            {
                _tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

                if (System.IO.File.Exists(_tempDirectory))
                {
                    return GetTemporaryDirectory();
                }
                else
                {
                    Directory.CreateDirectory(_tempDirectory);
                    return _tempDirectory;
                }
            }
            else
            {
                return _tempDirectory;
            }
        }
        public static string GetWebRootPath()
        {
            if (_webRootDirectory == null)
            {
                _webRootDirectory = Path.Combine(GetTemporaryDirectory(), "web");

                if (!System.IO.File.Exists(_webRootDirectory))
                {
                    Directory.CreateDirectory(_webRootDirectory);
                    Directory.CreateDirectory(Path.Combine(_webRootDirectory, "payload"));
                    return _webRootDirectory;
                }
            }

            return _webRootDirectory;
        }

        public static string GetWebPayloadPath()
        {
            string webPathloadPath = Path.Combine(GetWebRootPath(), "payload");
            if (!System.IO.File.Exists(_webRootDirectory))
            {
                Directory.CreateDirectory(webPathloadPath);

                return webPathloadPath;
            }

            return webPathloadPath;
        }

        public static void DeleteOldPayloadFiles()
        {
            string folderPath = GetWebPayloadPath();
            try
            {
                // Get the current time
                DateTime now = DateTime.Now;

                // Get the directory info
                DirectoryInfo directoryInfo = new DirectoryInfo(folderPath);

                // Get all files in the directory
                FileInfo[] files = directoryInfo.GetFiles();

                foreach (FileInfo file in files)
                {
                    // Calculate the age of the file
                    TimeSpan fileAge = now - file.CreationTime;

                    // Check if the file is older than 2 minutes
                    if (fileAge.TotalMinutes > 2)
                    {
                        // Delete the file
                        file.Delete();
                        Console.WriteLine($"Deleted file: {file.FullName}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

    }
}
