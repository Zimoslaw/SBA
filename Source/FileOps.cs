/*	EZ Backapp
 *   Copyright (C) 2023 Jakub Niewiarowski
 *
 *   This program is free software: you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation, either version 3 of the License, or
 *   (at your option) any later version.
 *
 *   This program is distributed in the hope that it will be useful,
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *   GNU General Public License for more details.
 *
 *   You should have received a copy of the GNU General Public License
 *   along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace SBA
{
    /**
     * <summary>
     * Implements various operations on files such as counting number and size of file in directory, checking hashes
     * </summary>
     */
    internal class FileOps : DependencyObject
    {
        internal TextBlock logConsole;

        private int numberOfFiles;
        internal int NumberOfFiles
        {
            get { return numberOfFiles; }
        }
        private ulong sizeOfFiles;
        internal ulong SizeOfFiles
        {
            get { return sizeOfFiles; }
        }

        internal FileOps(TextBlock LogConsole)
        {
            numberOfFiles = 0;
            sizeOfFiles = 0;
            logConsole = LogConsole;
        }

        /**
		 * <summary>Counts total number of files and size in bytes of all files in given directory and its subdirectories</summary>
		 * <param name="path">URL of directory</param>
		 */
        internal void CountFilesAndSize(string path)
        {
            try
            {
                DirectoryInfo directory = new DirectoryInfo(path);

                foreach (FileInfo file in directory.GetFiles())
                {
                    numberOfFiles++;
                    sizeOfFiles += (ulong)file.Length;
                }
                foreach (string dir in Directory.GetDirectories(path))
                {
                    CountFilesAndSize(dir);
                }
            }
            catch (UnauthorizedAccessException)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    Logging logging = new Logging();
                    logging.Log(logConsole, $" access to directory: \"{path}\" denied", 7);
                }));
            }
            catch (Exception e)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    Logging logging = new Logging();
                    logging.Log(logConsole, $" in directory \"{path}\": " + e.ToString(), 7);
                }));
            }
        }

        /**
		 * <summary>Compares SHA256 hash of two files</summary>
		 * <param name="file1">First file to compare</param>
		 * <param name="file2">Second file to compare</param>
		 * <returns>Bool. True if files have identical hashes</returns>
		 */
        internal bool CompareFilesHash(FileInfo file1, FileInfo file2)
        {
            using (SHA256 crypto = SHA256.Create())
            {
                try
                {
                    //opening original file and getting its hash array
                    FileStream fileStream = file1.Open(FileMode.Open);
                    fileStream.Position = 0;
                    byte[] hashValue1 = crypto.ComputeHash(fileStream);
                    fileStream.Close();

                    //openign destination file and getting its hash array
                    fileStream = file2.Open(FileMode.Open);
                    fileStream.Position = 0;
                    byte[] hashValue2 = crypto.ComputeHash(fileStream);
                    fileStream.Close();

                    //comparing hash arrays
                    if (hashValue1.SequenceEqual(hashValue2))
                        return true;
                    else
                        return false;
                }
                catch (Exception e)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        Logging logging = new Logging();
                        logging.Log(logConsole, $" of \"{file1.FullName}\" and \"{file2.FullName}\": {e}", 5);
                    }));
                    return false;
                }
            }
        }
    }
}
