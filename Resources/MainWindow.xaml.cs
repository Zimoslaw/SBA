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

using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml;

namespace SBA
{
	public partial class MainWindow : Window
	{
		long numberOfFiles = 0; //calculated number of files to copy
		ulong sizeOfFiles = 0; //calculated size of files to copy

		readonly Config config = new Config(); //configuration
		readonly Logging logging = new Logging(); //logging
		private Backup backup = null; //backup operations

        long stopwatch; //timestamp for backup time counting

		public MainWindow()
		{
			InitializeComponent();
		}

		private void Window_Initialized(object sender, EventArgs e)
		{
			//--------------------------------------loading settings---------------------------------------

			config.LoadConfig(appConsole);

			//-------------------------loading paths from file on application start-------------------------
			if(config.Load(appConsole))
			{
				try
				{
					destinationPath.Text = Config.destinationPath;
					foreach(string path in Config.pathsList)
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
					Config.destinationPath = "";
					Config.pathsList.Clear();
				}
				else //if selected "load only correct paths"
				{
					destinationPath.Text = Config.destinationPath;

					foreach(string path in Config.pathsList)
					{
						InsertPathIntoWindow(path);
					}
				}
			}
			addDirButton.IsEnabled = false;

			//--------------------Creating Backup object----------------------------
			//backup = new Backup(this, appConsole, mainProgressBar, backupButton, backupOption);

        }

		/**
		 * <summary>Checks if destiantion path exists</summary>
		 */
		private bool CheckDestinationPath()
		{
			if(Directory.Exists(Config.destinationPath))
			{
				return true;
			}
			else
			{
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
			foreach(string path in Config.pathsList)
			{
				if(!Directory.Exists(path))
				{
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
            Style style = Application.Current.FindResource("DeleteButton") as Style; //get style od DeleteButton
            removeButton.Style = style;
            removeButton.Margin = new Thickness(0, 4, 4, 4);
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
			FileOps counter = new FileOps(appConsole);

			Thread thread = new Thread(() =>
			{
                counter.CountFilesAndSize(path);

				numberOfFiles += counter.NumberOfFiles;
				sizeOfFiles += counter.SizeOfFiles;

				Dispatcher.BeginInvoke(new Action(() =>
				{
					UpdateFileCountAndSize(numberOfFiles, sizeOfFiles);
					backupButton.IsEnabled = true;
					backupOption.IsEnabled = true;
				}));
			});
			thread.Start();

			Config.isConfigSaved = false;
			addDirButton.IsEnabled = false;
		}

		/**
		 * <summary>Updates backup destination path</summary>
		 */
		private void UpdateDestinationPath(object sender, RoutedEventArgs e)
		{
			Config.destinationPath = destinationPath.Text;
			Config.isConfigSaved = false;
		}

		/**
		 * <summary>Updates info about size and number of all files in source directories</summary>
		 */
		private void UpdateFileCountAndSize(long n, ulong s)
		{
			filesCount.Content = "Number of files: " + n;

			s >>= 20; //size in MB
			if(s > 1024)
			{
				s >>= 10;
				filesSize.Content = "Est. size [GB]: " + s;
			}
			else
				filesSize.Content = "Est. size [MB]: " + s;
		}

		/**
		 * <summary>
		 * Adds directory in TextBox to backup list (directories to backup) and puts it on UI list <see cref="InsertPathIntoWindow(string)"/>.
		 * Checks if direcory exists, if it is already on list and if maximum number of added directories is reached
		 * </summary>
		 */
		private void AddButton_Click(object sender, RoutedEventArgs e)
		{
			if(Config.pathsList.Count() < Config.MaxSources) //count of directories cannot exceed maxSources
			{
				string path = directoryPath.Text;
				if(Directory.Exists(path)) //checking if given directory even exists
				{
					bool dirExists = false; //bool for checking if directory is already in list

					foreach(string p in Config.pathsList) //checking if directory is already in list
					{
						if(p == path) //if exists
						{
							logging.Log(appConsole, "", 11);
							MessageBox.Show("Given path is already in the list");
							dirExists = true;
							break;
						}
					}

					if(!dirExists)
					{
						Config.pathsList.Add(path); //adding paths to list
						InsertPathIntoWindow(path); //instering path into UI
					}
				}
				else
				{
					logging.Log(appConsole, path, 0);
					MessageBox.Show("Given path does not exits");
				}
			}
			else
			{
				logging.Log(appConsole, "", 12);
				MessageBox.Show($"Limit of paths to copy is reached (max. {Config.MaxSources})");
			}
		}

		/**
		 * <summary>Removes source path from UI and backup list</summary>
		 */
		private void RemovePathButton_Click(object sender, RoutedEventArgs e)
		{
			string path = ((Button)sender).Tag.ToString();

			Config.pathsList.Remove(path); //removing from list

			//setting number and size as 'in progress'
			filesCount.Content = "Number of files: Counting";
			filesSize.Content = "Est. size: Counting";

			//subtraction of file count and size
			FileOps counter = new FileOps(appConsole);

			Thread thread = new Thread(() =>
			{
				counter.CountFilesAndSize(path); //counting number of files and their size in chosen directory

				numberOfFiles -= counter.NumberOfFiles;

				if(numberOfFiles < 0)
					numberOfFiles = 0;

				try
				{
					sizeOfFiles = checked(sizeOfFiles - counter.SizeOfFiles);
				}
				catch(Exception exception)
				{
					sizeOfFiles = 0;
					Logging logging = new Logging();
					logging.Log(null, exception.ToString(), 7);
				}

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
			if(Config.pathsList.Count == 0)
			{
				backupButton.IsEnabled = false;
				backupOption.IsEnabled = false;
			}

			Config.isConfigSaved = false;
		}

		/**
		 * <summary>
		 * Starts backup process
		 * Checks if destination paths exists, if there are any source directiories and if there are valid
		 * </summary>
		 */
		private void BackupButton_Click(object sender, RoutedEventArgs e)
		{

			if(CheckDestinationPath()) //does destination path exists
			{
				if(Config.pathsList.Count > 0) //are there any directories on list
				{
					if(CheckSourceList()) //are source directories valid
					{
                        backup = new Backup(this, appConsole, mainProgressBar, backupButton, backupOption);

                        logging.Log(appConsole, "", 17);

						mainProgressBar.Maximum = numberOfFiles;

						DateTimeOffset now = DateTime.Now; //save timestamp for counting elapsed time
						stopwatch = now.ToUnixTimeSeconds();

						try
						{
							Thread copyThread = new Thread(
								new ThreadStart(() =>
								{
									backup.BackupDirectories(Config.pathsList, Config.destinationPath);
									Dispatcher.BeginInvoke(new Action(() =>
									{
										mainProgressBar.Value = -1;
										mainProgressBar.Visibility = Visibility.Hidden;
									}));
								})
							);
							backupButton.IsEnabled = false;
							backupOption.IsEnabled = false;
							cancelButton.IsEnabled = true;
							backupButton.Content = "In progress...";
							cancelButton.Content = "Cancel";
							progress.Text = "0%";
							mainProgressBar.Visibility = Visibility.Visible;
							progress.Visibility = Visibility.Visible;
							cancelButton.Visibility = Visibility.Visible;
							copyThread.Start();
						}
						catch(Exception exception)
						{
							logging.Log(appConsole, exception.ToString(), 5);
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
		 * <summary>Signalises that backup process is done and updates UI</summary>
		 * <param name="wasCanceled">Was copying canceled or backup finished normally</param>
		 */
		public void StopTimeCount(bool wasCanceled)
		{
			DateTimeOffset now = DateTime.Now;
			string time = SecondsToTime(now.ToUnixTimeSeconds() - stopwatch);
            string message = wasCanceled ? $"Backup cancelled at {progress.Text}" : $"Backup done in {time}";

			if(wasCanceled)
			{
                logging.Log(appConsole, "", 8);
                progress.Text = "Canceled";
            }
			else
			{
                logging.Log(appConsole, $"{time}", 16);
                mainProgressBar.Value = mainProgressBar.Maximum;
                progress.Text = "Done";
            }

			backupButton.Content = "Execute Backup";
			backupButton.IsEnabled = true;
			backupOption.IsEnabled = true;
            cancelButton.IsEnabled = false;
            cancelButton.Visibility = Visibility.Hidden;
            elapsedTime.Content = "";

			backup = null;

            MessageBox.Show(message);
		}

		/**
		 * <summary>Updates progress bar and progress text</summary>
		 */
		public void UpdateProgress()
		{
			backupButton.IsEnabled = false;
			backupOption.IsEnabled = false;

			mainProgressBar.Value++;
			progress.Text = $"{Math.Round((mainProgressBar.Value / mainProgressBar.Maximum)*100, 1)}%";

			DateTimeOffset now = DateTime.Now;
			string time = SecondsToTime(now.ToUnixTimeSeconds() - stopwatch);
			elapsedTime.Content = $"Elapsed: {time}";
		}

		/**
		 * <summary>Converts seconds to hours, minutes and seconds</summary>
		 */
		private string SecondsToTime(long secs)
		{
			string time = $"{secs} seconds";
			if(secs > 59)
			{
				long seconds = secs % 60;
				secs /= 60;
				time = $"{secs} minutes {seconds} seconds";
				if(secs > 59)
				{
					long minutes = secs % 60;
					secs /= 60;
					time = $"{secs} hours {minutes} minutes {seconds} seconds";
				}
			}
			return time;
		}

		/**
		 * <summary>Cancels copying operation</summary>
		 */
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
			if (backup == null)
				return;
			backup.CancelBackup();
			cancelButton.IsEnabled = false;
			cancelButton.Content= "Canceling...";
        }

        /**
		 * <summary>
		 * Opens dialog for selecting a folder and puts selected path into TextBox element.
		 * That TextBox is used for adding directory to copy. <see cref="AddButton_Click(object, RoutedEventArgs)"/>
		 * </summary>
		 */
        private void DirButton_Click(object sender, RoutedEventArgs e)
		{
			CommonOpenFileDialog dirSelectionWindow = new CommonOpenFileDialog()
			{
				IsFolderPicker = true
			};

			CommonFileDialogResult dirSelectionResult = dirSelectionWindow.ShowDialog();

			if(dirSelectionResult == CommonFileDialogResult.Ok && !string.IsNullOrWhiteSpace(dirSelectionWindow.FileName))
			{
				directoryPath.Text = dirSelectionWindow.FileName;
			}
		}

		private void DirectoryPath_TextChanged(object sender, TextChangedEventArgs e)
		{
			addDirButton.IsEnabled = true;
		}

		/**
		 * <summary>
		 * Opens dialog for selecting a backup destination folder, puts it in TextBox and sets it as destination directory
		 * </summary>
		 */
		private void DestButton_Click(object sender, RoutedEventArgs e)
		{
			CommonOpenFileDialog destSelectionWindow = new CommonOpenFileDialog
			{
				IsFolderPicker = true
			};

			CommonFileDialogResult dirSelectionResult = destSelectionWindow.ShowDialog();

			if(dirSelectionResult == CommonFileDialogResult.Ok && !string.IsNullOrWhiteSpace(destSelectionWindow.FileName))
			{
				destinationPath.Text = destSelectionWindow.FileName;
				UpdateDestinationPath(null, null);
			}
		}

		/**
		 *<summary>Saves paths into file <see cref="Config.Save(List{string}, string, TextBlock)"/></summary>
		 * 
		 */
		private void SaveButton_Click(object sender, RoutedEventArgs e)
		{
			Config config = new Config();
			if(config.Save(appConsole))
			{
				logging.Log(appConsole, "", 10);
			}
			else
			{
				logging.Log(appConsole, "", 1);
			}
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
			if(!Config.isConfigSaved)
			{
				//show dialog is paths weren't saved to prevent configuration loss
				ExitDialog dialog = new ExitDialog();
				dialog.ShowDialog();
			}
			else
				Application.Current.Shutdown();
		}

        private void Manual_Click(object sender, RoutedEventArgs e)
        {
			ProcessStartInfo manualProcess = new ProcessStartInfo();
			manualProcess.FileName = "https://github.com/Zimoslaw/SBA";
            manualProcess.UseShellExecute = true;

			Process.Start(manualProcess);
        }
    }
}
