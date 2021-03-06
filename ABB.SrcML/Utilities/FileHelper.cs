﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ABB.SrcML.Utilities {

    /// <summary>
    /// FileHelper contains numerous static methods for working with files
    /// </summary>
    public static class FileHelper {

        /// <summary>
        /// GetCommonPath finds the longest common path for all of the files in
        /// <paramref name="filePaths"/>that is
        /// <paramref name="startingPoint"/>or a parent of it.
        /// </summary>
        /// <param name="startingPoint">The starting point to start searching from</param>
        /// <param name="filePaths">The enumerable of file paths</param>
        /// <returns>The longest path that is common to all
        /// <paramref name="filePaths"/>. If it cannot find one, null is returned.</returns>
        public static string GetCommonPath(string startingPoint, IEnumerable<string> filePaths) {
            if(null == startingPoint)
                throw new ArgumentNullException("startingPoint");
            if(null == filePaths)
                throw new ArgumentNullException("filePaths");
            bool commonPathFound = false;
            string commonPath = startingPoint;
            while(!commonPathFound) {
                commonPathFound = filePaths.All(f => f.StartsWith(commonPath, StringComparison.Ordinal));
                if(commonPathFound) {
                    break;
                }
                commonPath = Path.GetDirectoryName(commonPath);
                if(null == commonPath) {
                    break;
                }
            }
            return commonPath;
        }

        /// <summary>
        /// Finds the longest common path for all of the files in
        /// <paramref name="filePaths"/></summary>
        /// <remarks>
        /// This calls <see cref="GetCommonPath(string,IEnumerable{string})"/> where the first file
        /// in
        /// <paramref name="filePaths"/>is used as the starting point
        /// </remarks>
        /// <param name="filePaths">The enumerable of file paths</param>
        /// <returns>The longest path that is common to all
        /// <paramref name="filePaths"/>. If it cannot find one, null is returned.</returns>
        public static string GetCommonPath(IEnumerable<string> filePaths) {
            string shortest = null;
            try {
                shortest = filePaths.First();
            } catch(InvalidOperationException) {
                shortest = null;
            }

            return GetCommonPath(shortest, filePaths);
        }
    }
}