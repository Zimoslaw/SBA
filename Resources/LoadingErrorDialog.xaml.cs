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
	/// Interaction logic for LoadingErrorDialog.xaml
	/// </summary>
	public partial class LoadingErrorDialog : Window
	{
		public LoadingErrorDialog()
		{
			InitializeComponent();
		}

		private void cancelLoadButton_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = false;
		}

		private void loadOnlyOkButton_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = true;
		}

		private void exitBeforeLoadButton_Click(object sender, RoutedEventArgs e)
		{
			Application.Current.Shutdown();
		}
	}
}
