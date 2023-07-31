/*	Simple Backup Application
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
using System.Linq;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading.Tasks;

namespace SBA
{
	/**
	 * <summary>Implements reading and writing app configuration from and to a file</summary>
	 */
	public class Config
	{
		//config file names
		const string URLFilePath = "paths.ini";
		const string configFilePath = "config.ini";

		public static List<string> pathsList = new List<string>(); //list of paths selected to copy
		public static string destinationPath = "Type path here or click on \"...\" button"; //selected destination path

		//global settings
		public static bool Overwrite = true; //overwrite existing files?
		public static bool CheckHash = false; //check hash after copying file?
		public static readonly byte MaxSources = 64; //maximum number of source directories to backup

		public static bool isConfigSaved = true; //was current configuration saved?

		/**
		 * <summary>Writes paths (URLs of files to backup and backup directory) into the configuration file</summary>
		 * <param name="console">TextBlock element for displaying events</param>
		 * <returns>Bool. True if saving was successful. False if there was a problem with saving</returns>
		 */
		public bool Save(TextBlock logConsole)
		{
			try
			{
				StreamWriter pathsFile = new StreamWriter(URLFilePath);

				pathsFile.Write($"##This file contains absolute paths of directories{Environment.NewLine}##Paths are contained between \" and \"{Environment.NewLine}#{Environment.NewLine}#{Environment.NewLine}#Destination Directory (directory where files and directories will be copied to e.g.: destination=\"D:\\Backup\")");

				pathsFile.Write($"{Environment.NewLine}destination=\"{destinationPath}\"");

				pathsFile.Write($"{Environment.NewLine}#{Environment.NewLine}#{Environment.NewLine}#Source directories (directories that will be copied){Environment.NewLine}# - Each path has to be started with \'source=\' (e.g. source=\"C:\\Users\\User\"){Environment.NewLine}# - There can be up to {MaxSources} paths. Redundant paths won't be loaded");

				foreach(string p in pathsList)
				{
					pathsFile.Write($"{Environment.NewLine}source=\"{p}\"");
				}

				pathsFile.Close();

				isConfigSaved = true;

				return true;
			}
			catch(Exception exception)
			{
				Logging logging = new Logging();
				logging.Log(logConsole, exception.ToString(), 1);
				return false;
			}
		}

		/**
		 * <summary>Loads paths (URLs of files to backup and backup directory) from configuration file</summary>
		 * <param name="logConsole">TextBlock element for displaying events</param>
		 * <returns>Bool. True if loading was successful. False if there was a problem with loading</returns>
		 */
		public bool Load(TextBlock logConsole)
		{
			try
			{
				StreamReader pathsFile = new StreamReader(URLFilePath);
				bool isErrorInConfig = false; //does file contains at least one error?
				bool isNull = true; // is file empty?
				bool isMaxNumberOfSourcesReached = false;
				string iniPattern = ".*=\".*\".*"; //regex pattern for correct setting
				string line;
				byte sources = 0; //number of read source paths

				while((line = pathsFile.ReadLine()) != null)
				{
					isNull = false;

					if(!string.IsNullOrWhiteSpace(line) && line[0] != '#') //empty lines or lines starting by '#' (comments) are omitted
					{
						if(Regex.IsMatch(line,iniPattern)) //line with correct setting must contain '=' and at least two '"'
						{
							if(line.Contains("destination")) //reading destination path between '"' characters
							{
								string p = line.Substring(line.IndexOf('"') + 1, line.LastIndexOf('"') - line.IndexOf('"') - 1);
								if(Directory.Exists(p))
									destinationPath = p;
								else
									isErrorInConfig = true;
							}
							else if(line.Contains("source") && !isMaxNumberOfSourcesReached) //reading source path between '"' characters if the max number of sources isn't reached yet
							{
								string p = line.Substring(line.IndexOf('"') + 1, line.LastIndexOf('"') - line.IndexOf('"') - 1);

								if(Directory.Exists(p))
								{
									pathsList.Add(p);
									sources++;
								}
								else
								{
									isErrorInConfig = true;
								}

								if(sources >= MaxSources)
									isMaxNumberOfSourcesReached = true;
							}
							else
								isErrorInConfig = true;
						}
						else
							isErrorInConfig = true;
					}
				}

				if(isErrorInConfig)
				{
					Logging logging = new Logging();
					logging.Log(logConsole, $"{URLFilePath} contains an error(s)", 2);
					return false;
				}
				else if(isNull)
				{
					Logging logging = new Logging();
					logging.Log(logConsole, $"{URLFilePath} is empty", 2);
					return false;
				}
				else
				{
					return true;
				}
			}
			catch(FileNotFoundException)
			{
				Logging logging = new Logging();
				logging.Log(logConsole, $"{URLFilePath} not found", 2);
				return true;
			}
			catch(Exception exception)
			{
				Logging logging = new Logging();
				logging.Log(logConsole, exception.ToString(), 2);
				return false;
			}

		}

		/**
		 * <summary>Writes app's configuration into file</summary>
		 * <param name="logConsole">TextBlock element for displaying events</param>
		 */
		public void SaveConfig(TextBlock logConsole)
		{
			try
			{
				StreamWriter pathsFile = new StreamWriter(configFilePath);

				pathsFile.Write($"##This file contains settings{Environment.NewLine}##0 = No (false), 1 = Yes (true){Environment.NewLine}#{Environment.NewLine}##Overwrite if there's file with the same name in destination folder?");

				char zeroOrOne = Overwrite ? '1' : '0';
				pathsFile.Write($"{Environment.NewLine}overwrite={zeroOrOne}");

				pathsFile.Write($"{Environment.NewLine}#{Environment.NewLine}##Check hash (SHA256) values of original and copied files? (check if they're indentical)");

				zeroOrOne = CheckHash ? '1' : '0';
				pathsFile.Write($"{Environment.NewLine}hash={zeroOrOne}");

				pathsFile.Close();
			}
			catch(Exception exception)
			{
				Logging logging = new Logging();
				logging.Log(logConsole, exception.ToString(), 3);
			}
		}

		/**<summary>Reads the app's configuration from file</summary>
		 * <param name="logConsole">TextBlock element for displaying events</param>
		 */
		public void LoadConfig(TextBlock logConsole)
		{
			try
			{
				StreamReader configFile = new StreamReader(configFilePath);
				bool isErrorInConfig = false; //does one or more errors occur in file?
				bool isNull = true; //is file empty?

				string line;
				while((line = configFile.ReadLine()) != null)
				{
					isNull = false;
					if(!string.IsNullOrWhiteSpace(line) && line[0] != '#') //empty lines or lines starting by '#' (comments) are omitted
					{
						if(line.Contains('=') && (line.Contains('0') || line.Contains('1'))) //line with proper setting must contain '=' and '0' or '1'
						{
							char set = line[line.IndexOf('=') + 1];

							if(set == '1')
							{
								if(line.Contains("overwrite"))
									Overwrite = true;
								else if(line.Contains("hash"))
									CheckHash = true;
								else
									isErrorInConfig = true;
							}
							else if(set == '0')
							{
								if(line.Contains("overwrite"))
									Overwrite = false;
								else if(line.Contains("hash"))
									CheckHash = false;
								else
									isErrorInConfig = true;
							}
							else
							{
								isErrorInConfig = true;
							}
						}
						else
						{
							isErrorInConfig = true;
						}
					}
				}

				if(isErrorInConfig)
				{
					Logging logging = new Logging();
					logging.Log(logConsole, $"{configFilePath} contains an error(s)", 4);
				}
				else if(isNull)
				{
					Logging logging = new Logging();
					logging.Log(logConsole, $"{configFilePath} is empty", 4);
				}
				else
				{
					Logging logging = new Logging();
					logging.Log(logConsole, "", 15);
				}
			}
			catch(FileNotFoundException)
			{
				Logging logging = new Logging();
				logging.Log(logConsole, $"{configFilePath} not found", 4);
			}
			catch(Exception exception)
			{
				Logging logging = new Logging();
				logging.Log(logConsole, exception.ToString(), 4);
			}
		}

	}
}
