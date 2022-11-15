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

using System.Windows;
using System.Windows.Controls;

namespace SBA
{
	/// <summary>
	/// Interaction logic for SettingsDialog.xaml
	/// </summary>
	public partial class SettingsDialog : Window
	{
		private readonly MainWindow parent;

		public SettingsDialog(MainWindow Parent)
		{
			parent = Parent;

			InitializeComponent();

			//setting checkboxes
			overwriteCheckbox.IsChecked = Config.Overwrite;
			hashCheckbox.IsChecked = Config.CheckHash;
		}

		/**
		 * <summary>Applies and saves settings to a file <see cref="Config.SaveConfig(bool, bool, TextBlock)"/></summary>
		 */
		private void Ok_Click(object sender, RoutedEventArgs e)
		{
			//applying setings
			Config.Overwrite = overwriteCheckbox.IsChecked == true;
			Config.CheckHash = hashCheckbox.IsChecked == true;

			//saving settings
			Config config = new Config();
			config.SaveConfig(parent.appConsole);

			DialogResult = true;
		}

		/**
		 * <summary>Exits dialog without saving or applying settings</summary>
		 */
		private void Cancel_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
		}
	}
}
