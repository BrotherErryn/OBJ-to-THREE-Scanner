using System.Windows;
using OBJScanner.Models;

namespace OBJScanner
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private ApplicationModel model = new ApplicationModel();
		
		public MainWindow()
		{
			InitializeComponent();

			DataContext = model;
		}

		protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
		{
			model.Dispose();
			model = null;

			base.OnClosing(e);
		}
	}
}
