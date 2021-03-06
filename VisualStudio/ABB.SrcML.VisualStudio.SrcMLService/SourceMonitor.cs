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

using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ABB.SrcML.VisualStudio.SrcMLService {

    public class SourceMonitor : DirectoryScanningMonitor, IVsRunningDocTableEvents {
        private const int MaxNumberOfFilesInSolutionDirectory = 1000;
        private IVsRunningDocumentTable DocumentTable;
        private uint DocumentTableItemId;
        private ISrcMLArchive sourceArchive;

        /// <summary>
        /// Creates a new source monitor
        /// </summary>
        /// <param name="solution">The solution to monitor</param>
        /// <param name="foldersToMonitor">A list of folders to monitor</param>
        /// <param name="scanInterval">The interval at which to scan the folders (in
        /// seconds) </param>
        /// <param name="baseDirectory">The base directory for this monitor</param>
        /// <param name="defaultArchive">The default archive to route files to</param>
        /// <param name="otherArchives">Other archives to route files to</param>
        public SourceMonitor(Solution solution, double scanInterval, TaskScheduler scheduler, string baseDirectory, IArchive defaultArchive, ISrcMLArchive sourceArchive, params IArchive[] otherArchives)
            : base(DirectoryScanningMonitor.MONITOR_LIST_FILENAME, scanInterval, scheduler, baseDirectory, defaultArchive, otherArchives) {
            RegisterArchive(sourceArchive, false);
            this.sourceArchive = sourceArchive;
            this.MonitoredSolution = solution;
        }

        /// <summary>
        /// Creates a new source monitor
        /// </summary>
        /// <param name="solution">The solution to monitor</param>
        /// <param name="foldersToMonitor">A list of folders to monitor</param>
        /// <param name="baseDirectory">The base directory for this monitor</param>
        /// <param name="defaultArchive">The default archive to route files to</param>
        /// <param name="otherArchives">Other archives to route files to</param>
        public SourceMonitor(Solution solution, string baseDirectory, IArchive defaultArchive, ISrcMLArchive sourceArchive, params IArchive[] otherArchives)
            : this(solution, DEFAULT_SCAN_INTERVAL, TaskScheduler.Default, baseDirectory, defaultArchive, sourceArchive, otherArchives) { }

        /// <summary>
        /// The solution being monitored
        /// </summary>
        public Solution MonitoredSolution { get; private set; }

        /// <summary>
        /// Gets a list of all the files from the solution
        /// </summary>
        /// <param name="solution">The solution to get the files from</param>
        /// <returns>A list of the solution files</returns>
        public static List<string> GetSolutionFiles(Solution solution) {
            if(solution == null)
                throw new ArgumentNullException("solution");

            List<string> solutionFiles = new List<string>();
            var projectEnumerator = solution.Projects.GetEnumerator();
            while(projectEnumerator.MoveNext()) {
                SearchProjectForFiles(solutionFiles, projectEnumerator.Current as Project);
            }
            return solutionFiles;
        }

        /// <summary>
        /// Gets the full directory path that contains the
        /// <paramref name="solution"/></summary>
        /// <param name="solution">The solution</param>
        /// <returns>The absolute path to the directory that contains the solution</returns>
        public static string GetSolutionPath(Solution solution) {
            if(null == solution) {
                throw new ArgumentNullException("solution");
            }

            string solutionFilePath = Path.GetFullPath(Path.GetDirectoryName(solution.FullName));
            var solutionFiles = GetSolutionFiles(solution);
            string commonPath = Utilities.FileHelper.GetCommonPath(solutionFilePath, solutionFiles);
            if(DirectoryIsForbidden(commonPath)) {
                commonPath = solutionFilePath;
            }
            return commonPath;
        }

        /// <summary>
        /// Adds the root directory for the <see cref="MonitoredSolution">monitored solution</see>
        /// to <see cref="DirectoryScanningMonitor.MonitoredDirectories"/>.
        /// </summary>
        public void AddSolutionDirectory() {
            var solutionPath = GetSolutionPath(MonitoredSolution);
            if(!DirectoryHasTooManyFiles(solutionPath)) {
                AddDirectory(GetSolutionPath(MonitoredSolution));
            }
        }

        /// <summary>
        /// Update the
        /// <paramref name="docCookie">referenced file</paramref>
        /// <see cref="DirectoryScanningMonitor.IsMonitoringFile(string)">if it is being
        /// monitored</see>
        /// <paramref name="docCookie">file</paramref> if the
        /// <paramref name="grfAttribs">changed attribute</paramref> is
        /// <see cref="__VSRDTATTRIB.RDTA_DocDataReloaded"/>.
        /// </summary>
        /// <param name="docCookie">The cookie for the changed document</param>
        /// <param name="grfAttribs">The document attributes</param>
        /// <returns>If the method succeeds, it returns <see cref="VSConstants.S_OK"/>. If it fails,
        /// it returns an error code.</returns>
        public int OnAfterAttributeChange(uint docCookie, uint grfAttribs) {
            if(grfAttribs == (uint) __VSRDTATTRIB.RDTA_DocDataReloaded) {
                return UpdateVisualStudioDocument(docCookie);
            }
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Ignored
        /// </summary>
        /// <param name="docCookie">Unused</param>
        /// <param name="pFrame">Unused</param>
        /// <returns>returns see cref="VSConstants.S_OK"/>.</returns>
        public int OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame) {
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Ignored.
        /// </summary>
        /// <param name="docCookie">Unused</param>
        /// <param name="dwRDTLockType">Unused</param>
        /// <param name="dwReadLocksRemaining">Unused</param>
        /// <param name="dwEditLocksRemaining">Unused</param>
        ///<returns>returns <see cref="VSConstants.S_OK"/>.</returns>
        public int OnAfterFirstDocumentLock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining) {
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Respond to visual studio file saves by updating the monitor if the file
        /// <see cref="DirectoryScanningMonitor.IsMonitoringFile(string)">is being monitored</see>
        /// </summary>
        /// <param name="docCookie"></param>
        /// <returns>If the method succeeds, it returns <see cref="VSConstants.S_OK"/>. If it fails,
        /// it returns an error code.</returns>
        public int OnAfterSave(uint docCookie) {
            return UpdateVisualStudioDocument(docCookie);
        }

        /// <summary>
        /// Ignored.
        /// </summary>
        /// <param name="docCookie">Unused</param>
        /// <param name="fFirstShow">Unused</param>
        /// <param name="pFrame">Unused</param>
        /// <returns>returns see cref="VSConstants.S_OK"/>.</returns>
        public int OnBeforeDocumentWindowShow(uint docCookie, int fFirstShow, IVsWindowFrame pFrame) {
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Ignored.
        /// </summary>
        /// <param name="docCookie">Unused</param>
        /// <param name="dwRDTLockType">Unused</param>
        /// <param name="dwReadLocksRemaining">Unused</param>
        /// <param name="dwEditLocksRemaining">Unused</param>
        /// <returns>returns see cref="VSConstants.S_OK"/>.</returns>
        public int OnBeforeLastDocumentUnlock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining) {
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Subscribe to the visual studio <see cref="IVsRunningDocumentTable"/> and then
        /// <see cref="DirectoryScanningMonitor.StartMonitoring()">start monitoring the
        /// directories</see>.
        /// </summary>
        public override void StartMonitoring() {
            SubscribeToEvents();
            base.StartMonitoring();
        }

        /// <summary>
        /// Unsubscribe from the <see cref="IVsRunningDocumentTable"/> and then
        /// <see cref="DirectoryScanningMonitor.StopMonitoring()">stop monitoring the
        /// directories.</see>
        /// </summary>
        public override void StopMonitoring() {
            UnsubscribeFromEvents();
            base.StopMonitoring();
        }

        /// <summary>
        /// Dispose of this source monitor
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);
        }

        private static void SearchProjectForFiles(List<string> solutionFiles, Project project) {
            if(project != null)
                if(project.ProjectItems != null) {
                    var itemEnumerator = project.ProjectItems.GetEnumerator();
                    while(itemEnumerator.MoveNext()) {
                        var item = itemEnumerator.Current as ProjectItem;
                        if(item != null && item.Name != null && item.FileCount > 0) {
                            try {
                                var myPath = Path.GetFullPath(item.FileNames[0]);
                                if(!String.IsNullOrEmpty(Path.GetExtension(myPath)))
                                    solutionFiles.Add(myPath);
                            } catch(ArgumentException) { }
                            var proj = item.SubProject as Project;
                            if(proj != null) {
                                SearchProjectForFiles(solutionFiles, proj);
                            }
                        }
                    }
                }
        }

        private bool DirectoryHasTooManyFiles(string directoryPath) {
            int numberOfFiles = (from filePath in EnumerateDirectory(directoryPath)
                                 where sourceArchive.IsValidFileExtension(filePath)
                                 select filePath).Take(MaxNumberOfFilesInSolutionDirectory).Count();
            return numberOfFiles >= MaxNumberOfFilesInSolutionDirectory;
        }

        /// <summary>
        /// Subscribe to the running document table events
        /// </summary>
        private void SubscribeToEvents() {
            DocumentTable = Package.GetGlobalService(typeof(SVsRunningDocumentTable)) as IVsRunningDocumentTable;
            if(DocumentTable != null) {
                int result = DocumentTable.AdviseRunningDocTableEvents(this, out DocumentTableItemId);
                ErrorHandler.ThrowOnFailure(result);
            }
        }

        /// <summary>
        /// Unsubscribe from the running document table events
        /// </summary>
        private void UnsubscribeFromEvents() {
            if(DocumentTable != null) {
                int result = DocumentTable.UnadviseRunningDocTableEvents(DocumentTableItemId);
                ErrorHandler.Succeeded(result);
            }
        }

        /// <summary>
        /// Update the visual studio document referred to by
        /// <paramref name="docCookie"/></summary>
        /// <param name="docCookie">The identifier for the visula studio document</param>
        /// <returns>The <see cref="VSConstants"/> indicating whether or not the document was
        /// found</returns>
        private int UpdateVisualStudioDocument(uint docCookie) {
            uint flags, readingLocks, editLocks, documentId;
            string filePath;
            IVsHierarchy hierarchy;
            IntPtr documentData;
            int status = DocumentTable.GetDocumentInfo(docCookie, out flags, out readingLocks, out editLocks, out filePath, out hierarchy, out documentId, out documentData);
            var archive = GetArchiveForFile(filePath);
            if(status == VSConstants.S_OK && IsMonitoringFile(filePath) && archive.IsOutdated(filePath)) {
                UpdateFileAsync(filePath);
            }
            return status;
        }
    }
}