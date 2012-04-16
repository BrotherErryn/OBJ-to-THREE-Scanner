using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimpleMvvmToolkit;

namespace OBJScanner.Models
{
	public class ConversionModel : ViewModelBase<ConversionModel>
	{
		private DateTime _whenProcessed = DateTime.Now;
		public DateTime WhenProcessed
		{
			get { return _whenProcessed; }
			set
			{
				if (value != WhenProcessed)
				{
					_whenProcessed = value;
					NotifyPropertyChanged(p => p.WhenProcessed);
				}
			}
		}

		private string _sourceFile = null;
		public string SourceFile
		{
			get { return _sourceFile; }
			set
			{
				if (value != SourceFile)
				{
					_sourceFile = value;
					NotifyPropertyChanged(p => p.SourceFile);
				}
			}
		}

		private string _destFile = null;
		public string DestinationFile
		{
			get { return _destFile; }
			set
			{
				if (value != DestinationFile)
				{
					_destFile = value;
					NotifyPropertyChanged(p => p.DestinationFile);
				}
			}
		}

		private string _output = null;
		public string ConversionOutput
		{
			get { return _output; }
			set
			{
				if (value != ConversionOutput)
				{
					_output = value;
					NotifyPropertyChanged(p => p.ConversionOutput);
				}
			}
		}

		public ConversionModel() { }
	}
}
