/*	Simple Backup Application
 *   Copyright (C) 2022 Jakub Niewiarowski
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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.IO;
using System.Security.Cryptography;
using System.Threading;

namespace SBA
{
	public partial class MainWindow : Window
	{
		List<string> pathsList = new List<string>(); //list of paths selected to copy
		string destPath = "Type path here or click on \"...\" button"; //selected destination path
		int numberOfFiles = 0; //calculated number of files to copy
		ulong sizeOfFiles = 0; //calculated size of files to copy

		//global settings
		public static bool Overwrite = true; //overwrite existing files?
		public static bool CheckHash = false; //check hash after copying file?
		public const byte MaxSources = 64; //maximum number of source directories to backup

		public MainWindow()
		{
			InitializeComponent();
		}

		private void Window_Initialized(object sender, EventArgs e)
		{
			//--------------------------------------loading settings---------------------------------------
			Config config = new Config();
			Logging logging = new Logging();

			config.LoadConfig(ref Overwrite, ref CheckHash, appConsole);

			//-------------------------loading paths from file on application start-------------------------
			if(config.Load(ref destPath, pathsList, appConsole))
			{
				try
				{
					destinationPath.Text = destPath;
					foreach(string path in pathsList)
					{
						InsertPathIntoWindow(path);
					}
					logging.Log(appConsole, "", 13);
				}
				catch(Exception exception)
				{
					logging.Log(appConsole, exception.ToString(), 2);
				}
			}
			else
			{
				//displaying dialog box with option after encountering error in loading
				LoadingErrorDialog dialog = new LoadingErrorDialog();
				Initialized -= Window_Initialized;

				dialog.ShowDialog();
				

				if(dialog.DialogResult != true) //if selected "don't load"
				{
					destPath = "";
					pathsList.Clear();
				}
				else //if selected "load only correct paths"
				{
					destinationPath.Text = destPath;

					foreach(string path in pathsList)
					{
						InsertPathIntoWindow(path);
					}
				}
			}
		}

		/**
		 * <summary>
		 * Opens dialog for selecting a folder and puts selected path into TextBox element.
		 * That TextBox is used for adding directory to copy. <see cref="AddButton_Click(object, RoutedEventArgs)"/>
		 * </summary>
		 */
		private void DirButton_Click(object sender, RoutedEventArgs e)
		{
			CommonOpenFileDialog dirSelectionWindow = new CommonOpenFileDialog();
			dirSelectionWindow.IsFolderPicker = true;

			CommonFileDialogResult dirSelectionResult = dirSelectionWindow.ShowDialog();

			if(dirSelectionResult == CommonFileDialogResult.Ok && !string.IsNullOrWhiteSpace(dirSelectionWindow.FileName))
			{
				directoryPath.Text = dirSelectionWindow.FileName;
			}
		}

		/**
		 * <summary>
		 * Opens dialog for selecting a backup destination folder, puts it in TextBox and sets it as destination directory
		 * </summary>
		 */
		private void DestButton_Click(object sender, RoutedEventArgs e)
		{
			CommonOpenFileDialog destSelectionWindow = new CommonOpenFileDialog();
			destSelectionWindow.IsFolderPicker = true;

			CommonFileDialogResult dirSelectionResult = destSelectionWindow.ShowDialog();

			if(dirSelectionResult == CommonFileDialogResult.Ok && !string.IsNullOrWhiteSpace(destSelectionWindow.FileName))
			{
				destinationPath.Text = destSelectionWindow.FileName;
				UpdateDestinationPath(null, null);
			}
		}

		/**
		 * <summary>
		 * Adds directory in TextBox to backup list (directories to backup) and puts it on UI list <see cref="InsertPathIntoWindow(string)"/>.
		 * Checks if direcory exists, if it is already on list and if maximum number of added directories is reached
		 * </summary>
		 */
		private void AddButton_Click(object sender, RoutedEventArgs e)
		{
			if(pathsList.Count() < MaxSources) //count of directories cannot exceed maxSources
			{
				string path = directoryPath.Text;
				if(Directory.Exists(path)) //checking if given directory even exists
				{
					bool dirExists = false; //bool for checking if directory is already in list

					foreach(string p in pathsList) //checking if directory is already in list
					{
						if(p == path) //if exists
						{
							Logging logging = new Logging();
							logging.Log(appConsole, "", 11);
							MessageBox.Show("Given path is already in the list");
							dirExists = true;
							break;
						}
					}

					if(!dirExists)
					{
						pathsList.Add(path); //adding paths to list
						InsertPathIntoWindow(path); //instering path into UI
					}
				}
				else
				{
					Logging logging = new Logging();
					logging.Log(appConsole, path, 0);
					MessageBox.Show("Given path does not exits");
				}
			}
			else
			{
				Logging logging = new Logging();
				logging.Log(appConsole, "", 12);
				MessageBox.Show($"Limit of paths to copy is reached (max. {MaxSources})");
			}
		}

		/**
		 * <summary>Checks if destiantion path exists</summary>
		 */
		private bool CheckDestinationPath()
		{
			if(Directory.Exists(destPath))
			{
				return true;
			}
			else
			{
				Logging logging = new Logging();
				logging.Log(appConsole, destinationPath.Text, 0);
				MessageBox.Show("Destination path does not exist");
				return false;
			}
		}

		/**
		 * <summary>Checks if source paths exist</summary>
		 */
		private bool CheckSourceList()
		{
			foreach(string path in pathsList)
			{
				if(!Directory.Exists(path))
				{
					Logging logging = new Logging();
					logging.Log(appConsole, path, 0);
					MessageBox.Show("Given path does not exits");
				}
			}
			return true;
		}

		/**
		 * <summary>Inserts source directory from TextBox into UI with removing option</summary>
		 */
		private void InsertPathIntoWindow(string path)
		{
			DockPanel directoryBox = new DockPanel(); //container for textblock and remove button
			Button removeButton = new Button(); //button for removing each directory from backup
			TextBlock selectedDirectory = new TextBlock(); //textblock for displaying each of selected directiories to backup

			//initializing template of buttons for removing directories from backup 
			removeButton.Margin = new Thickness(4, 4, 4, 4);
			removeButton.HorizontalAlignment = HorizontalAlignment.Right;
			removeButton.FontSize = 16;
			removeButton.Content = "Delete";
			removeButton.Click += new RoutedEventHandler(RemovePathButton_Click);

			//initializing template of textblocks for displaying each of selected directiories to backup
			selectedDirectory.FontSize = 16;
			selectedDirectory.VerticalAlignment = VerticalAlignment.Center;

			selectedDirectory.Text = path;
			removeButton.Tag = path; //for passing given path in argument of removing function

			directoryBox.Children.Add(removeButton);
			directoryBox.Children.Add(selectedDirectory);
			selectedDirs.Children.Insert(0, directoryBox);

			//setting number and size as 'in progress'
			filesCount.Content = "Number of files: Counting";
			filesSize.Content = "Est. size: Counting";

			//disabling backup option for counting time
			backupButton.IsEnabled = false;
			backupOption.IsEnabled = false;

			//adding number of files and their size
			Backup backup = new Backup(appConsole);

			Thread thread = new Thread(() =>
			{
				backup.CountFilesAndSize(path);

				numberOfFiles += backup.NumberOfFiles;
				sizeOfFiles += backup.SizeOfFiles;

				Dispatcher.BeginInvoke(new Action(() =>
				{
					UpdateFileCountAndSize(numberOfFiles, sizeOfFiles);
					backupButton.IsEnabled = true;
					backupOption.IsEnabled = true;
				}));
			});
			thread.Start();
		}

		/**
		 * <summary>Updates backup destination path</summary>
		 */
		private void UpdateDestinationPath(object sender, RoutedEventArgs e)
		{
			destPath = destinationPath.Text;
		}

		/**
		 *<summary>Saves paths into file <see cref="Config.Save(List{string}, string, TextBlock)"/></summary>
		 * 
		 */
		private void SaveButton_Click(object sender, RoutedEventArgs e)
		{
			Config config = new Config();
			Logging logging = new Logging();
			if(config.Save(pathsList, destPath, appConsole))
			{
				logging.Log(appConsole, "", 10);
			}
			else
			{
				logging.Log(appConsole, "", 1);
			}
		}

		/**
		 * <summary>Removes source path from UI and backup list</summary>
		 */
		private void RemovePathButton_Click(object sender, RoutedEventArgs e)
		{
			string path = ((Button)sender).Tag.ToString();

			pathsList.Remove(path); //removing from list

			//setting number and size as 'in progress'
			filesCount.Content = "Number of files: Counting";
			filesSize.Content = "Est. size: Counting";

			//subtraction of file count and size
			Backup backup = new Backup(appConsole);

			Thread thread = new Thread(() =>
			{
				backup.CountFilesAndSize(path); //counting number of files and their size in chosen directory

				numberOfFiles -= backup.NumberOfFiles;
				sizeOfFiles -= backup.SizeOfFiles;

				Dispatcher.BeginInvoke(new Action(() =>
				{
					UpdateFileCountAndSize(numberOfFiles, sizeOfFiles);
				}));
			});
			thread.Start();

			//removing from UI
			object toRemove = ((Button)sender).Parent;
			DockPanel childToRemove = toRemove as DockPanel;
			selectedDirs.Children.Remove(childToRemove);

			//checking if there's any source paths left
			if(pathsList.Count == 0)
			{
				backupButton.IsEnabled = false;
				backupOption.IsEnabled = false;
			}
		}

		/**
		 * <summary>
		 * Starts backup process
		 * Checks if destination paths exists, if there are any source directiories and if there are valid
		 * </summary>
		 */
		private void BackupButton_Click(object sender, RoutedEventArgs e)
		{
			Logging logging = new Logging();

			if(CheckDestinationPath()) //does destination path exists
			{
				if(pathsList.Count > 0) //are there any directories on list
				{
					if(CheckSourceList()) //are source directories valid
					{
						logging.Log(appConsole, "", 17);

						mainProgressBar.Maximum = numberOfFiles;

						try
						{
							Backup backup = new Backup(appConsole, mainProgressBar, backupButton, backupOption);

							Thread copyThread = new Thread(
								new ThreadStart(() =>
								{
									backup.BackupDirectories(pathsList, destPath);
									Dispatcher.BeginInvoke(new Action(() =>
									{
										mainProgressBar.Value = -1;
										mainProgressBar.Visibility = Visibility.Hidden;
									}));
								})
							);
							backupButton.IsEnabled = false;
							backupOption.IsEnabled = false;
							mainProgressBar.Visibility = Visibility.Visible;
							copyThread.Start();
						}
						catch(Exception ex)
						{
							logging.Log(appConsole, ex.ToString(), 5);
						}
					}
				}
				else
				{
					MessageBox.Show("There is no source directories to copy!");
					backupButton.IsEnabled = false;
					backupOption.IsEnabled = false;
				}
			}
		}

		/**
		 * <summary>Updates info about size and number of all files in source directories</summary>
		 */
		private void UpdateFileCountAndSize(int n, ulong s)
		{
			filesCount.Content = "Number of files: " + n;

			s = s >> 20; //size in MB
			if(s > 1024)
			{
				s = s >> 10;
				filesSize.Content = "Est. size [GB]: " + s;
			}
			else
				filesSize.Content = "Est. size [MB]: " + s;
		}

		/**
		 * <summary>Opens settings dialog</summary>
		 */
		private void Settings_Click(object sender, RoutedEventArgs e)
		{
			SettingsDialog dialog = new SettingsDialog(this);
			dialog.ShowDialog();
		}

		/**
		 * <summary>Opens About window</summary>
		 */
		private void About_Click(object sender, RoutedEventArgs e)
		{
			AboutWindow dialog = new AboutWindow();
			dialog.Show();
		}

		/**
		 * <summary>Exits application</summary>
		 */
		private void Exit_Click(object sender, RoutedEventArgs e)
		{
			Application.Current.Shutdown();
		}
	}
}
