﻿/******************************************************************************
 * Copyright (c) 2013 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine (ABB Group) - Initial implementation
 *****************************************************************************/

using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace ABB.SrcML.Test {

    [TestFixture]
    public class DirectoryScanningMonitorTests {
        private const string monitorFolder = "monitor";
        private const int numStartingFiles = 100;
        private const string testFolder = "test";

        #region test setup

        [TearDown]
        public void TestCleanup() {
            Directory.Delete(monitorFolder, true);
            Directory.Delete(testFolder, true);
        }

        [SetUp]
        public void TestSetup() {
            Directory.CreateDirectory(monitorFolder);
            Directory.CreateDirectory(testFolder);
            for(int i = 0; i < numStartingFiles; i++) {
                File.Create(Path.Combine(testFolder, String.Format("{0}.txt", i))).Close();
            }
        }

        #endregion test setup

        [Test, ExpectedException(ExpectedException = typeof(DirectoryScanningMonitorSubDirectoryException))]
        public void TestAddSubdirectory() {
            var archive = new LastModifiedArchive(monitorFolder);
            DirectoryScanningMonitor monitor = new DirectoryScanningMonitor(monitorFolder, archive);

            monitor.AddDirectory(testFolder);
            monitor.AddDirectory(Path.Combine(testFolder, "test"));
        }

        [Test]
        public void TestFileChanges() {
            var archive = new LastModifiedArchive(monitorFolder);
            DirectoryScanningMonitor monitor = new DirectoryScanningMonitor(monitorFolder, archive);
            monitor.ScanInterval = 1;
            monitor.AddDirectory(testFolder);
            monitor.UpdateArchives();

            AutoResetEvent are = new AutoResetEvent(false);
            var expectedEventType = FileEventType.FileAdded;
            var expectedFileName = Path.GetFullPath(Path.Combine(testFolder, "new.txt"));
            monitor.FileChanged += (o, e) => {
                if(e.EventType == expectedEventType && e.FilePath == expectedFileName) {
                    are.Set();
                }
            };
            monitor.StartMonitoring();

            Assert.IsTrue(monitor.IsReady, "monitory is not ready");
            Assert.IsTrue(archive.IsReady, "archive is not ready");

            File.Create(expectedFileName).Close();
            Assert.IsTrue(are.WaitOne(1500));

            expectedEventType = FileEventType.FileChanged;
            var expectedLastWriteTime = DateTime.Now;
            File.SetLastWriteTime(expectedFileName, expectedLastWriteTime);
            Assert.IsTrue(are.WaitOne(1500));
            Assert.AreEqual(expectedLastWriteTime, archive.GetLastModifiedTime(expectedFileName));

            expectedEventType = FileEventType.FileDeleted;
            File.Delete(expectedFileName);
            Assert.IsTrue(are.WaitOne(1500));
        }

        [Test]
        public void TestIsMonitoringFile() {
            var archive = new LastModifiedArchive(monitorFolder);
            DirectoryScanningMonitor monitor = new DirectoryScanningMonitor(monitorFolder, archive);
            monitor.AddDirectory(testFolder);

            foreach(var fileName in Directory.EnumerateFiles(testFolder)) {
                Assert.IsTrue(monitor.IsMonitoringFile(fileName), "should be able to use the file name with the relative path");
                Assert.IsTrue(monitor.IsMonitoringFile(Path.GetFullPath(fileName)), "should be able to find the file name with the absolute path");
            }
        }

        [Test]
        public void TestRemoveDirectory() {
            var archive = new LastModifiedArchive(monitorFolder);
            DirectoryScanningMonitor monitor = new DirectoryScanningMonitor(monitorFolder, archive);
            monitor.AddDirectory(testFolder);
            monitor.UpdateArchives();

            Assert.AreEqual(numStartingFiles, monitor.GetArchivedFiles().Count());
            monitor.RemoveDirectory("test1");
            Assert.AreEqual(numStartingFiles, monitor.GetArchivedFiles().Count());

            monitor.RemoveDirectory(testFolder);
            Assert.AreEqual(0, monitor.GetArchivedFiles().Count());
            foreach(var fileName in Directory.EnumerateFiles(testFolder)) {
                Assert.IsTrue(File.Exists(fileName));
            }
        }

        [Test]
        public void TestStartup() {
            var archive = new LastModifiedArchive(monitorFolder);
            DirectoryScanningMonitor monitor = new DirectoryScanningMonitor(monitorFolder, archive);
            monitor.AddDirectory(testFolder);
            monitor.UpdateArchives();
            monitor.StartMonitoring();

            Assert.IsTrue(monitor.IsReady, "monitor is not ready");
            Assert.IsTrue(archive.IsReady, "archive is not ready");
            monitor.StopMonitoring();

            Assert.AreEqual(numStartingFiles, archive.GetFiles().Count(), String.Format("only found {0} files in the archive", archive.GetFiles().Count()));

            foreach(var fileName in Directory.EnumerateFiles(testFolder)) {
                Assert.IsTrue(archive.ContainsFile(fileName));
                Assert.IsFalse(archive.IsOutdated(fileName));
                Assert.AreEqual(File.GetLastWriteTime(fileName), archive.GetLastModifiedTime(fileName));
            }
        }
    }
}