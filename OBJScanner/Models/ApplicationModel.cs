using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using SimpleMvvmToolkit;
using System.Windows.Forms;
using System.IO;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows.Threading;
using System.Windows;
using System.Security.Cryptography;

namespace OBJScanner.Models
{
	public class ApplicationModel : ViewModelBase<ApplicationModel>, IDisposable
	{

		public IEnumerable<string> AlignmentOptions
		{
			get { return new List<string> { "bottom", "center", "centerxz", "none", "top" }; }
		}

		private FileSystemWatcher _watcher = null;

		public ICommand SelectInput { get; set; }
		public ICommand SelectOutput { get; set; }
		public ICommand SelectPython { get; set; }

		public string InputPath
		{
			get { return Properties.Settings.Default.InputPath; }
			set
			{
				if (value != InputPath)
				{
					Properties.Settings.Default.InputPath = value;
					CreateWatcher();
					NotifyPropertyChanged(p => p.InputPath);
				}
			}
		}
		public string OutputPath
		{
			get { return Properties.Settings.Default.OutputPath; }
			set
			{
				if (value != OutputPath)
				{
					Properties.Settings.Default.OutputPath = value;
					NotifyPropertyChanged(p => p.OutputPath);
				}
			}
		}
		public string Python
		{
			get { return Properties.Settings.Default.Python; }
			set
			{
				if (value != Python)
				{
					Properties.Settings.Default.Python = value;
					NotifyPropertyChanged(p => p.Python);
				}
			}
		}
		public bool ScanSubfolders
		{
			get { return Properties.Settings.Default.ScanSubfolders; }
			set
			{
				if (value != ScanSubfolders)
				{
					Properties.Settings.Default.ScanSubfolders = value;
					CreateWatcher();
					NotifyPropertyChanged(p => p.ScanSubfolders);
				}
			}
		}
		public bool ReplicateOutputStructure
		{
			get { return Properties.Settings.Default.ReplicateOutputStructure; }
			set
			{
				if (value != ReplicateOutputStructure)
				{
					Properties.Settings.Default.ReplicateOutputStructure = value;
					NotifyPropertyChanged(p => p.ReplicateOutputStructure);
				}
			}
		}

		public string Alignment
		{
			get { return Properties.Settings.Default.Alignment; }
			set
			{
				if (value != Alignment)
				{
					Properties.Settings.Default.Alignment = value;
					NotifyPropertyChanged(p => p.Alignment);
				}
			}
		}

		private ObservableCollection<ConversionModel> _conversionHistory = new ObservableCollection<ConversionModel>();
		public ObservableCollection<ConversionModel> ConversionHistory
		{
			get { return _conversionHistory; }
		}

		public ApplicationModel() 
		{
			InitializeCommands();
			CreateWatcher();
		}

		private void InitializeCommands()
		{
			SelectInput = new DelegateCommand(() => {
				var dialog = new FolderBrowserDialog { SelectedPath = InputPath };
				if (dialog.ShowDialog() != DialogResult.Cancel)
				{
					InputPath = dialog.SelectedPath;
				}
			});

			SelectOutput = new DelegateCommand(() =>
			{
				var dialog = new FolderBrowserDialog { SelectedPath = OutputPath };
				if (dialog.ShowDialog() != DialogResult.Cancel)
				{
					OutputPath = dialog.SelectedPath;
				}
			});

			SelectPython = new DelegateCommand(() => 
			{
				var dialog = new OpenFileDialog {
					Filter = "Python Executable (python*.exe)|python*.exe",
					FileName = Python
				};
				if (dialog.ShowDialog() != DialogResult.Cancel)
				{
					Python = dialog.FileName;
				}
			});
		}

		private void CreateWatcher()
		{
			if (_watcher != null)
			{
				_watcher.Dispose();
				_watcher = null;
			}
			if (!Directory.Exists(InputPath))
				return;

			_watcher = new FileSystemWatcher();
			_watcher.Path = InputPath;
			_watcher.Filter = "*.obj";
			_watcher.NotifyFilter = NotifyFilters.LastWrite;
			_watcher.IncludeSubdirectories = ScanSubfolders;

			_watcher.Changed += new FileSystemEventHandler(_watcher_Changed);
			_watcher.Created += new FileSystemEventHandler(_watcher_Created);

			_watcher.EnableRaisingEvents = true;
		}

		private void ProcessFile(string sourceFileName)
		{
			FileInfo baseSourceFI = new FileInfo(InputPath);
			FileInfo sourceFI = new FileInfo(sourceFileName);
			FileInfo destFI = new FileInfo(OutputPath);

			ConversionModel history = new ConversionModel();

			string destination = sourceFI.FullName.Replace(baseSourceFI.FullName, destFI.FullName);

			//python.exe convert.py -i "%1" -o "%1.js"
			//string outpath = OutputPath;
			//if (outpath.Substring(outpath.Length - 1, 1) != "/")
			//    outpath += "/";

			try
			{
				destination = Regex.Replace(destination, @".obj", @".js", RegexOptions.IgnoreCase);
				history.DestinationFile = destination;
				history.SourceFile = sourceFileName;

				FileInfo final = new FileInfo(destination);

				string scriptpath = String.Format(@"{0}\", System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));

				//[-a center|top|bottom] [-s smooth|flat] [-t ascii|binary] [-d invert|normal]
				//string cmd = String.Format("\"{0}\" \"{1}convert.py\" -t binary -i \"{2}\" -o \"{3}\"", 
				string cmd = String.Format("\"{0}\" \"{1}convert.py\" -i \"{2}\" -o \"{3}\" -a \"{4}\"",
					Properties.Settings.Default.Python,
					scriptpath,
					sourceFileName,
					destination,
					Alignment
				);

				if(final.Exists)
					final.Delete();

				if (!Directory.Exists(final.DirectoryName))
					Directory.CreateDirectory(final.DirectoryName);

				Process p = new Process();
				p.StartInfo.UseShellExecute = false;
				p.StartInfo.FileName = cmd;
				p.StartInfo.RedirectStandardOutput = true;
				p.StartInfo.RedirectStandardError = true;
				p.StartInfo.CreateNoWindow = true;
				//p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

				p.Start();
				p.WaitForExit();

				string result = p.StandardOutput.ReadToEnd();
				string eresult = p.StandardError.ReadToEnd();

				history.ConversionOutput = result ?? eresult;
			}
			catch (Exception ex)
			{
				history.ConversionOutput = ex.Message;
			}

			System.Windows.Application.Current.Dispatcher.BeginInvoke((Action)delegate()
			{
				ConversionHistory.Add(history);
			});
		}

		private void _watcher_Created(object sender, FileSystemEventArgs e)
		{
			ProcessFile(e.FullPath);
		}

		Dictionary<string, string> _fileHashes = new Dictionary<string,string>();
		private void _watcher_Changed(object sender, FileSystemEventArgs e)
		{
			string hash = GetMD5HashFromFile(e.FullPath);

			if (_fileHashes.ContainsKey(e.FullPath))
			{
				if (_fileHashes[e.FullPath] == hash)
					return;
			}
			_fileHashes[e.FullPath] = hash;			
			ProcessFile(e.FullPath);
		}

		protected string GetMD5HashFromFile(string fileName)
		{
			using (var stream = new BufferedStream(File.OpenRead(fileName), 1200000))
			{
				MD5 md5 = new MD5CryptoServiceProvider();
				byte[] retVal = md5.ComputeHash(stream);
				
				StringBuilder sb = new StringBuilder();
				for (int i = 0; i < retVal.Length; i++)
				{
					sb.Append(retVal[i].ToString("x2"));
				}
				return sb.ToString();
			}
		}

		public void Dispose()
		{
			Properties.Settings.Default.Save();
		}
	}
}
