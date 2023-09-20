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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

namespace SBA
{
	/**
	 * <summary>Implements logging to a file and console (StackPanel)</summary>
	 */
	public class Logging
	{
		const string logFilePath = "logs.txt";

        /**
		 * <summary>Method for logging events in file (logs.txt) and displaying them in a textblock element</summary>
		 * <param name="console">Textblock for displaying logs. If NULL, logs won't be shown in UI</param>
		 * <param name="s">Additional information</param>
		 * <param name="n">Error or info number. Described in Logging class</param>
		 */
        public void Log(StackPanel console, string s, int n)
		{

			string date = DateTime.Today.ToShortDateString() + " " + DateTime.Now.ToLongTimeString();

			string msg;

			switch(n)
			{
				//--------------------------------errors-------------------------
				case 0: //path doesn't exist
					msg = $"Given path does not exist: {s}";
				break;
				case 1: //Error saving paths
					msg = $"Error when saving paths: {s}";
				break;
				case 2: //Error loading paths
					msg = $"Error when loading paths: {s}";
				break;
				case 3: //Error saving config
					msg = $"Error when saving configuration: {s}";
				break;
				case 4: //Error loading config
					msg = $"Error when loading configuration: {s}";
				break;
				case 5: //Error in backup process
					msg = $"Error when copying files: {s}";
				break;
				case 6: //Error in comparing hashes
					msg = $"Error while comparing hashes{s}";
				break;
				case 7: //Error while counting files 
					msg = $"Error while counting files{s}";
				break;
                case 8: //Error while counting files 
                    msg = $"Backup canceled";
                break;
                //-------------------------------infos---------------------------
                case 10: //Saving paths OK
					msg = $"Saving paths was successful";
				break;
				case 11: //Selected path already in list
					msg = $"Given path is already in list";
					break;
				case 12: //Limit of selected paths has been reached
					msg = $"Limit of paths to copy is reached";
				break;
				case 13: //Loading paths successful
					msg = $"Loading paths was successful";	
				break;
				case 14: //saving configuration successful
					msg = $"Saving configuration was successful";
				break;
				case 15: //Loading configuration successful
					msg = $"Loading configuration was successful";
				break;
				case 16: //Loading configuration successful
					msg = $"Backup done. Elapsed time: {s}";
				break;
				case 17: //Loading configuration successful
					msg = $"Backup process started...";
				break;
				case 18: //Loading configuration successful
					msg = $"Destination directory set to: {s}";
				break;
				default:
					msg = $"Unknown error";
				break;
			}

            //Insert message into console
            DockPanel logLine = new DockPanel //Console line UI element
            {
                Margin = new Thickness(0, 0, 0, 4)
            };

            Label timeLabel = new Label //Date and time in console line
            {
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 0),
                Padding = new Thickness(0, 0, 0, 0),
                Foreground = new SolidColorBrush(Colors.Gray),
                HorizontalAlignment = HorizontalAlignment.Left,
                Content = date
			};

			Label logLabel = new Label //Log message
			{
				FontSize = 12,
				Margin = new Thickness(4, 0, 0, 0),
				Padding = new Thickness(0, 0, 0, 0),
				HorizontalAlignment = HorizontalAlignment.Left,
                Content = msg
			};

			if (n < 10)
				logLabel.Foreground = new SolidColorBrush(Colors.Crimson); //When log is error, paint its message red

			logLine.Children.Add(timeLabel);
			logLine.Children.Add(logLabel);

            console?.Children.Insert(console.Children.Count, logLine);

			if (console.Children.Count > 1000) //Delete first log from UI when there are too many
				console.Children.RemoveAt(0);

            //Insert message with date into log file
            msg = "[" + date + "] " + msg + Environment.NewLine;

            if (n < 10)
				File.AppendAllText(logFilePath, $"ERROR\t{msg}");
			else
				File.AppendAllText(logFilePath, $"INFO\t{msg}");
		}
	}
}
