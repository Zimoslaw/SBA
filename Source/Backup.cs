using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Windows.Threading;

namespace SBA
{
	/**
	 * <summary>
	 * Implements backup process (copying files from chosen destinations to chosen backup directory).
	 * Also counting files and overall size and comparing SHA256 hash of original file and a copy.
	 * </summary>
	 */
	public class Backup : DependencyObject
	{
		private string destinationRoot;
		private List<string> rootDirectories;
		private bool doOverwrite;
		private bool doCheckHash;
		private TextBlock logConsole;
		private ProgressBar mainProgressBar;
		private Button backupButton;
		private MenuItem backupOption;

		private int numberOfFiles;
		public int NumberOfFiles
		{
			get { return numberOfFiles; }
		}
		private ulong sizeOfFiles;
		public ulong SizeOfFiles
		{
			get { return sizeOfFiles; }
		}

		/**
		 * <summary>Constructs Backup object</summary>
		 * <param name="LogConsole">TextBlock element for displaying events</param>
		 * <param name="ProgressBar">ProgressBar object for displaying backup progress</param>
		 * <param name="BackupButton">Button that starts backup process</param>
		 */
		public Backup(TextBlock LogConsole, ProgressBar ProgressBar, Button BackupButton, MenuItem BackupOption)
		{
			doOverwrite = Config.Overwrite;
			doCheckHash = Config.CheckHash;
			logConsole = LogConsole;
			mainProgressBar = ProgressBar;
			backupButton = BackupButton;
			backupOption = BackupOption;
			rootDirectories = new List<string>();
		}

		public Backup(TextBlock LogConsole)
		{
			numberOfFiles = 0;
			sizeOfFiles = 0;
			logConsole = LogConsole;
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

			Stopwatch stopwatch = new Stopwatch(); //start counting elapsed time
			stopwatch.Start();

			foreach(string root in rootDirs) //get list of directories to backup
				rootDirectories.Add(root);

			foreach(string root in rootDirectories) //start copying each dir and its subdirs
				CopyFile(root.Substring(0, root.LastIndexOf('\\')), root);

			Dispatcher.BeginInvoke(new Action(() =>
			{
				backupButton.IsEnabled = true;
				backupOption.IsEnabled = true;
				backupButton.Content = "Execute Backup";
			}));

			stopwatch.Stop();

			int elapsed = stopwatch.Elapsed.Seconds;
			string time = "seconds";
			if(elapsed > 59)
			{
				int seconds = elapsed % 60;
				elapsed /= 60;
				time = $"minutes {seconds} seconds";
				if(elapsed > 59)
				{
					int minutes = elapsed % 60;
					elapsed /= 60;
					time = $"hours {minutes} minutes {seconds} seconds";
				}
			}

			Dispatcher.BeginInvoke(new Action(() =>
			{
				Logging logging = new Logging();
				logging.Log(logConsole, $"{elapsed} {time}", 16);

				mainProgressBar.Value = mainProgressBar.Maximum;
			}));
			
			MessageBox.Show($"Backup done in {elapsed} {time}");
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
						mainProgressBar.Value++;
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

		/**
		 * <summary>Counts total number of files and size in bytes of all files in given directory and its subdirectories</summary>
		 * <param name="path">URL of directory</param>
		 */
		public void CountFilesAndSize(string path)
		{
			try
			{
				DirectoryInfo directory = new DirectoryInfo(path);

				foreach(FileInfo file in directory.GetFiles())
				{
					numberOfFiles++;
					sizeOfFiles += (ulong)file.Length;
				}
				foreach(string dir in Directory.GetDirectories(path))
				{
					CountFilesAndSize(dir);
				}
			}
			catch(UnauthorizedAccessException)
			{
				Dispatcher.BeginInvoke(new Action(() =>
				{
					Logging logging = new Logging();
					logging.Log(logConsole, $" access to directory: \"{path}\" denied", 7);
				}));
			}
			catch(Exception e)
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
		private bool CompareFilesHash(FileInfo file1, FileInfo file2)
		{
			using(SHA256 crypto = SHA256.Create())
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
					if(hashValue1.SequenceEqual(hashValue2))
						return true;
					else
						return false;
				}
				catch(Exception e)
				{
					Dispatcher.BeginInvoke(new Action(() =>
					{
						Logging logging = new Logging();
						logging.Log(logConsole, $" of \"{file1.FullName}\" and \"{file2.FullName}\": {e.ToString()}", 5);
					}));
					return false;
				}
			}
		}
	}
}
