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
using System.Collections.Generic;
using System.IO;
using System.Windows.Controls;
using System.Windows.Threading;

namespace SBA
{
	/**
	 * <summary>
	 * Implements backup process (copying files from chosen destinations to chosen backup directory).
	 * Also counting files and overall size and comparing SHA256 hash of original file and a copy.
	 * </summary>
	 */
	internal class Backup : FileOps
	{
		private string destinationRoot;
		private List<string> rootDirectories;
		private bool doOverwrite;
		private bool doCheckHash;
		private bool cancel = false;
		private MainWindow mainWindow;
		private ProgressBar mainProgressBar;
		private Button backupButton;
		private MenuItem backupOption;

		/**
		 * <summary>Constructs Backup object</summary>
		 * <param name="LogConsole">TextBlock element for displaying events</param>
		 * <param name="ProgressBar">ProgressBar object for displaying backup progress</param>
		 * <param name="BackupButton">Button that starts backup process</param>
		 */
		public Backup(MainWindow MainWindow, TextBlock LogConsole, ProgressBar ProgressBar, Button BackupButton, MenuItem BackupOption) : base(LogConsole)
		{
			doOverwrite = Config.Overwrite;
			doCheckHash = Config.CheckHash;
			mainWindow = MainWindow;
			mainProgressBar = ProgressBar;
			backupButton = BackupButton;
			backupOption = BackupOption;
			rootDirectories = new List<string>();
		}

		/**
		 * <summary>
		 * Start a backup process of given directories to given destination.
		 * Logs events. Increses progress bar value.
		 * Displays message box when backup ends with elapsed time end enables backup button.
		 * </summary>
		 * <param name="rootDirs">ULRs of directories to copy</param>
		 * <param name="destination">Destination URL</param>
		 */
		public void BackupDirectories(List<string> rootDirs, string destination)
		{
			destinationRoot = destination;

			foreach(string root in rootDirs) //get list of directories to backup
				rootDirectories.Add(root);

			foreach(string root in rootDirectories) //start copying each dir and its subdirs
				CopyFile(root.Substring(0, root.LastIndexOf('\\')), root);

			Dispatcher.BeginInvoke(new Action(() =>
			{
				mainWindow.StopTimeCount(cancel);
			}));
		}

		/**
		 * <summary>
		 * Copies a file with given path to destination directory.
		 * If path is a directory, gets all files and directories and copies them recursively.
		 * </summary>
		 * <param name="root">Part of the file's URL to replace by destination URL</param>
		 * <param name="path">URL of the file to copy</param>
		 */
		private void CopyFile(string root, string path)
		{
            if (cancel) //If cancelation button was pressed, stop copying
                return;

            try
			{
				if(File.Exists(path))
				{
					try
					{
						if(doOverwrite)
						{
							File.Copy(path, path.Replace(root, destinationRoot), true); //Copying file (with replacing)

							if(doCheckHash) //compare SHA256 hashes if user wants
							{
								//compare hashes for first time
								if(!CompareFilesHash(new FileInfo(path), new FileInfo(path.Replace(root, destinationRoot)))) 
								{
									//if hashes doesn't match, log it
									Dispatcher.BeginInvoke(new Action(() =>
									{
										Logging logging = new Logging();
										logging.Log(logConsole, $": Hash values of \"{path}\" and \"{path.Replace(root, destinationRoot)}\" DOES NOT MATCH. Copying file again...", 6);
									}));

									//copy file again
									File.Copy(path, path.Replace(root, destinationRoot), true);
									//compare hashes for second time
									if(!CompareFilesHash(new FileInfo(path), new FileInfo(path.Replace(root, destinationRoot)))) 
									{
										//if hashes still doesn't match log it and go on
										Dispatcher.BeginInvoke(new Action(() =>
										{
											Logging logging = new Logging();
											logging.Log(logConsole, $": Hash values of \"{path}\" and \"{path.Replace(root, destinationRoot)}\" DOES NOT MATCH (even after re-copying file)", 6);
										}));
									}
								}
							}
						}
						else
						{
							if(!File.Exists(path.Replace(root, destinationRoot)))
								File.Copy(path, path.Replace(root, destinationRoot), false); //Copying file (without replacing)
						}
							
					}
					catch(UnauthorizedAccessException)
					{
						Dispatcher.BeginInvoke(new Action(() =>
						{
							Logging logging = new Logging();
							logging.Log(logConsole, $"Access to file \"{path}\" denied. Could not copy this file.", 5);
						}));
					}
					catch(Exception exception)
					{
						Dispatcher.BeginInvoke(new Action(() =>
						{
							Logging logging = new Logging();
							logging.Log(logConsole, $"Conserning file: {path}{Environment.NewLine}Exception:" + exception.ToString(), 5);
						}));
					}

					Dispatcher.BeginInvoke(new Action(() =>
					{
						mainWindow.UpdateProgress();
					}));
				}
				else //if path doesn't point to a file, it's consider as directory
				{
					if(!Directory.Exists(path.Replace(root, destinationRoot)))
						Directory.CreateDirectory(path.Replace(root, destinationRoot));

					foreach(string file in Directory.GetFiles(path))
					{
						CopyFile(root, file);
					}
					foreach(string dir in Directory.GetDirectories(path))
					{
						CopyFile (root, dir);
					}
				}
			}
			catch(UnauthorizedAccessException)
			{
				Dispatcher.BeginInvoke(new Action(() =>
				{
					Logging logging = new Logging();
					logging.Log(logConsole, $"Access to directory or file \"{path}\" denied. Could not copy this directory/file.", 5);
				}));
			}
			catch(Exception exception)
			{
				Dispatcher.BeginInvoke(new Action(() =>
				{
					Logging logging = new Logging();
					logging.Log(logConsole, exception.ToString(), 5);
				}));
			}
		}

		public void CancelBackup()
		{
			cancel = true;
		}
	}
}
