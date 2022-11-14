using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace SBA
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		private void Application_Exit(object sender, ExitEventArgs e)
		{
			Console.WriteLine("Exiting...");
			if(!Config.isConfigSaved)
			{
				//show dialog is paths weren't saved to prevent configuration loss
				ExitDialog dialog = new ExitDialog();
				dialog.ShowDialog();
			}
		}
	}
}
