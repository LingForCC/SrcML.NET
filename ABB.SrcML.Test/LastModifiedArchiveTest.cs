﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.IO;
namespace ABB.SrcML.Test {
    [TestFixture]
    class LastModifiedArchiveTest {
        private string testDirectory = "lastmodifiedarchivetests";

        [TestFixtureSetUp]
        public void SetUp() {
            if(!Directory.Exists(testDirectory)) {
                Directory.CreateDirectory(testDirectory);
            }
        }

        [SetUp]
        public void TestSetUp() {
            if(Directory.Exists(testDirectory)) {
                foreach(var file in Directory.GetFiles(testDirectory)) {
                    File.Delete(file);
                }
            }
        }

        [TestFixtureTearDown]
        public void TearDown() {
            if(Directory.Exists(testDirectory)) {
                Directory.Delete(testDirectory, true);
            }
        }

        [Test]
        public void TestAddUpdateDelete() {
            bool receivedFileAdded = false, receivedFileUpdated = false, receivedFileDeleted = false;
            LastModifiedArchive archive = new LastModifiedArchive(Path.Combine(testDirectory, "archive.txt"));
            archive.FileChanged += (sender, e) => {
                switch(e.EventType) {
                    case FileEventType.FileAdded:
                        receivedFileAdded = true;
                        break;
                    case FileEventType.FileChanged:
                        receivedFileUpdated = true;
                        break;
                    case FileEventType.FileDeleted:
                        receivedFileDeleted = true;
                        break;
                }
            };

            string fileFoo = Path.Combine(testDirectory, "foo.txt");
            File.Create(fileFoo).Dispose();
            archive.AddOrUpdateFile(fileFoo);
            Assert.That(receivedFileAdded);

            Assert.That(archive.ContainsFile(fileFoo));
            Assert.IsFalse(archive.IsOutdated(fileFoo));

            File.AppendAllText(fileFoo, "This is bar!\n"); 
            Assert.That(archive.IsOutdated(fileFoo));
            archive.AddOrUpdateFile(fileFoo);
            Assert.That(receivedFileUpdated);

            File.Delete(fileFoo);
            Assert.That(archive.IsOutdated(fileFoo));
            archive.DeleteFile(fileFoo);
            Assert.That(receivedFileDeleted);
            Assert.IsFalse(archive.ContainsFile(fileFoo));
        }

        [Test]
        public void TestRename() {
            bool receivedFileAdd = false, receivedFileRename = false;
            LastModifiedArchive archive = new LastModifiedArchive(Path.Combine(testDirectory, "archive.txt"));
            archive.FileChanged += (sender, e) => {
                switch(e.EventType) {
                    case FileEventType.FileAdded:
                        receivedFileAdd = true;
                        break;
                    case FileEventType.FileRenamed:
                        receivedFileRename = true;
                        break;
                }
            };
            string pathToFoo = Path.Combine(testDirectory, "foo.txt");
            string pathToBar = Path.Combine(testDirectory, "bar.txt");

            File.Create(pathToFoo).Dispose();

            archive.AddOrUpdateFile(pathToFoo);
            Assert.That(archive.ContainsFile(pathToFoo));
            Assert.That(receivedFileAdd);

            File.Move(pathToFoo, pathToBar);
            Assert.That(archive.IsOutdated(pathToFoo));
            archive.RenameFile(pathToFoo, pathToBar);
            Assert.That(receivedFileRename);
            Assert.That(archive.ContainsFile(pathToBar));
            Assert.IsFalse(archive.ContainsFile(pathToFoo));
        }

        [Test]
        public void TestRelativePathInsertWithFullPathCheck() {
            LastModifiedArchive archive = new LastModifiedArchive(Path.Combine(testDirectory, "archive.txt"));
            string relativePathToFoo = Path.Combine(testDirectory, "foo.txt");
            string fullPathToFoo = Path.GetFullPath(relativePathToFoo);
            
            File.Create(relativePathToFoo).Dispose();

            archive.AddOrUpdateFile(relativePathToFoo);
            Assert.That(archive.ContainsFile(fullPathToFoo));
        }
    }
}
