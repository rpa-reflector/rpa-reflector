// SPDX-FileCopyrightText: 2026 Reflector Maintainers
// SPDX-License-Identifier: MIT

using Microsoft.VisualStudio.TestTools.UnitTesting;
using RPAReflector;
using System;
using System.IO;
using System.Reflection;

namespace UnitTestProject
{
    /// <summary>
    /// Tests for the Filesystem static helper class.
    /// Because Filesystem uses static state (tempDirectory / webRootDirectory),
    /// each test resets those fields via reflection in TestInitialize so tests
    /// remain independent.
    /// </summary>
    [TestClass]
    public class FilesystemTests
    {
        // Tracks directories created by tests so we can clean up.
        private string _createdTempDir;

        [TestInitialize]
        public void ResetFilesystemState()
        {
            // Capture the current temp dir so we can delete it in cleanup
            _createdTempDir = GetStaticField<string>("tempDirectory");

            // Reset static state so each test gets a fresh directory
            SetStaticField("tempDirectory", null);
            SetStaticField("webRootDirectory", null);
        }

        [TestCleanup]
        public void CleanupCreatedDirectories()
        {
            // Clean up anything created during the test
            string tempDir = GetStaticField<string>("tempDirectory");
            if (tempDir != null && Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
            if (_createdTempDir != null && Directory.Exists(_createdTempDir))
            {
                Directory.Delete(_createdTempDir, recursive: true);
            }

            SetStaticField("tempDirectory", null);
            SetStaticField("webRootDirectory", null);
        }

        // --- GetTemporaryDirectory ---

        [TestMethod]
        public void GetTemporaryDirectory_ReturnsExistingDirectory()
        {
            string path = Filesystem.GetTemporaryDirectory();

            Assert.IsTrue(Directory.Exists(path), "Temporary directory should be created on disk");
        }

        [TestMethod]
        public void GetTemporaryDirectory_CalledTwice_ReturnsSamePath()
        {
            string first  = Filesystem.GetTemporaryDirectory();
            string second = Filesystem.GetTemporaryDirectory();

            Assert.AreEqual(first, second, "Subsequent calls should return the same cached path");
        }

        [TestMethod]
        public void GetTemporaryDirectory_PathIsInsideSystemTempFolder()
        {
            string path = Filesystem.GetTemporaryDirectory();
            string systemTemp = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar);

            StringAssert.StartsWith(
                path,
                systemTemp,
                "Temporary directory should reside inside the system temp folder");
        }

        // --- GetWebRootPath ---

        [TestMethod]
        public void GetWebRootPath_ReturnsExistingDirectory()
        {
            string path = Filesystem.GetWebRootPath();

            Assert.IsTrue(Directory.Exists(path), "Web root directory should be created on disk");
        }

        [TestMethod]
        public void GetWebRootPath_EndsWithWeb()
        {
            string path = Filesystem.GetWebRootPath();

            Assert.AreEqual("web", Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar)));
        }

        [TestMethod]
        public void GetWebRootPath_CreatesPayloadSubdirectory()
        {
            string webRoot = Filesystem.GetWebRootPath();
            string payloadPath = Path.Combine(webRoot, "payload");

            Assert.IsTrue(Directory.Exists(payloadPath), "payload subdirectory should be created under web root");
        }

        // --- GetWebPayloadPath ---

        [TestMethod]
        public void GetWebPayloadPath_ReturnsPathEndingInPayload()
        {
            string path = Filesystem.GetWebPayloadPath();

            Assert.AreEqual("payload", Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar)));
        }

        [TestMethod]
        public void GetWebPayloadPath_DirectoryExists()
        {
            string path = Filesystem.GetWebPayloadPath();

            Assert.IsTrue(Directory.Exists(path));
        }

        // --- DeleteOldPayloadFiles ---

        [TestMethod]
        public void DeleteOldPayloadFiles_DeletesFilesOlderThan2Minutes()
        {
            string payloadPath = Filesystem.GetWebPayloadPath();

            // Create a file and backdate its creation time by 3 minutes
            string oldFile = Path.Combine(payloadPath, "old_file.txt");
            File.WriteAllText(oldFile, "old");
            File.SetCreationTime(oldFile, DateTime.Now.AddMinutes(-3));

            Filesystem.DeleteOldPayloadFiles();

            Assert.IsFalse(File.Exists(oldFile), "Files older than 2 minutes should be deleted");
        }

        [TestMethod]
        public void DeleteOldPayloadFiles_KeepsRecentFiles()
        {
            string payloadPath = Filesystem.GetWebPayloadPath();

            // Create a file with current creation time
            string recentFile = Path.Combine(payloadPath, "recent_file.txt");
            File.WriteAllText(recentFile, "recent");

            Filesystem.DeleteOldPayloadFiles();

            Assert.IsTrue(File.Exists(recentFile), "Recently created files should not be deleted");
        }

        [TestMethod]
        public void DeleteOldPayloadFiles_MixedAges_OnlyDeletesOldOnes()
        {
            string payloadPath = Filesystem.GetWebPayloadPath();

            string oldFile    = Path.Combine(payloadPath, "old.txt");
            string recentFile = Path.Combine(payloadPath, "new.txt");

            File.WriteAllText(oldFile,    "old");
            File.WriteAllText(recentFile, "new");
            File.SetCreationTime(oldFile, DateTime.Now.AddMinutes(-5));

            Filesystem.DeleteOldPayloadFiles();

            Assert.IsFalse(File.Exists(oldFile),    "Old file should have been deleted");
            Assert.IsTrue(File.Exists(recentFile), "Recent file should not have been deleted");
        }

        // --- Helpers ---

        private static T GetStaticField<T>(string fieldName)
        {
            var field = typeof(Filesystem).GetField(fieldName, BindingFlags.Public | BindingFlags.Static);
            return field != null ? (T)field.GetValue(null) : default;
        }

        private static void SetStaticField(string fieldName, object value)
        {
            var field = typeof(Filesystem).GetField(fieldName, BindingFlags.Public | BindingFlags.Static);
            field?.SetValue(null, value);
        }
    }
}