using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SBA
{
	/// <summary>
	/// Interaction logic for ExitDialog.xaml
	/// </summary>
	public partial class ExitDialog : Window
	{
		public ExitDialog()
		{
			InitializeComponent();
		}

		private void savePaths_Click(object sender, RoutedEventArgs e)
		{
			//save and exit
			Config config = new Config();
			config.Save(null);
			Application.Current.Shutdown();
		}

		private void exitWithoutSaving_Click(object sender, RoutedEventArgs e)
		{
			//exit witout saving
			Application.Current.Shutdown();
		}
	}
}
