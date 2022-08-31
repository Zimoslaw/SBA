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
	class Config
	{
		const string URLFilePath = "paths.ini";
		const string configFilePath = "config.ini";

		/**
		 * <summary>Writes paths (URLs of files to backup and backup directory) into the configuration file</summary>
		 * <param name="paths">Directories to copy</param>
		 * <param name="dest">Destination path (backup directory)</param>
		 * <param name="console">TextBlock element for displaying events</param>
		 * <returns>Bool. True if saving was successful. False if there was a problem with saving</returns>
		 */
		public bool Save(List<string> paths, string dest, TextBlock console)
		{
			try
			{
				StreamWriter pathsFile = new StreamWriter(URLFilePath);

				pathsFile.Write($"##This file contains absolute paths of directories{Environment.NewLine}##Paths are contained between \" and \"{Environment.NewLine}#{Environment.NewLine}#{Environment.NewLine}#Destination Directory (directory where files and directories will be copied to e.g.: destination=\"D:\\Backup\")");

				pathsFile.Write($"{Environment.NewLine}destination=\"{dest}\"");

				pathsFile.Write($"{Environment.NewLine}#{Environment.NewLine}#{Environment.NewLine}#Source directories (directories that will be copied){Environment.NewLine}# - Each path has to be started with \'source=\' (e.g. source=\"C:\\Users\\User\"){Environment.NewLine}# - There can be up to {MainWindow.MaxSources} paths. Redundant paths won't be loaded");

				foreach(string p in paths)
				{
					pathsFile.Write($"{Environment.NewLine}source=\"{p}\"");
				}

				pathsFile.Close();

				return true;
			}
			catch(Exception exception)
			{
				Logging logging = new Logging();
				logging.Log(console, exception.ToString(), 1);
				return false;
			}
		}

		/**
		 * <summary>Loads paths (URLs of files to backup and backup directory) from configuration file</summary>
		 * <param name="dest">Destination path (backup directory)</param>
		 * <param name="paths">Directories to cop</param>
		 * <param name="logConsole">TextBlock element for displaying events</param>
		 * <returns>Bool. True if loading was successful. False if there was a problem with loading</returns>
		 */
		public bool Load(ref string dest, List<string> paths, TextBlock logConsole)
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
									dest = p;
								else
									isErrorInConfig = true;
							}
							else if(line.Contains("source") && !isMaxNumberOfSourcesReached) //reading source path between '"' characters if the max number of sources isn't reached yet
							{
								string p = line.Substring(line.IndexOf('"') + 1, line.LastIndexOf('"') - line.IndexOf('"') - 1);

								if(Directory.Exists(p))
								{
									paths.Add(p);
									sources++;
								}
								else
								{
									isErrorInConfig = true;
								}

								if(sources >= MainWindow.MaxSources)
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
		 * <param name="overwrite">Overwrite an existing file setting</param>
		 * <param name="checkHash">Compare hash of original and copied file setting</param>
		 * <param name="logConsole">TextBlock element for displaying events</param>
		 */
		public void SaveConfig(bool overwrite, bool checkHash, TextBlock logConsole)
		{
			try
			{
				StreamWriter pathsFile = new StreamWriter(configFilePath);

				pathsFile.Write($"##This file contains settings{Environment.NewLine}##0 = No (false), 1 = Yes (true){Environment.NewLine}#{Environment.NewLine}##Overwrite if there's file with the same name in destination folder?");

				char zeroOrOne = overwrite ? '1' : '0';
				pathsFile.Write($"{Environment.NewLine}overwrite={zeroOrOne}");

				pathsFile.Write($"{Environment.NewLine}#{Environment.NewLine}##Check hash (SHA256) values of original and copied files? (check if they're indentical)");

				zeroOrOne = checkHash ? '1' : '0';
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
		 * <param name="overwrite">Reference to a variable for overwrite an existing file setting</param>
		 * <param name="hash">>Reference to a variable for compare hash of original and copied file setting</param>
		 * <param name="logConsole">TextBlock element for displaying events</param>
		 */
		public void LoadConfig(ref bool overwrite, ref bool hash, TextBlock logConsole)
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
									overwrite = true;
								else if(line.Contains("hash"))
									hash = true;
								else
									isErrorInConfig = true;
							}
							else if(set == '0')
							{
								if(line.Contains("overwrite"))
									overwrite = false;
								else if(line.Contains("hash"))
									hash = false;
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
