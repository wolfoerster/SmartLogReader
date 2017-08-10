using System.Windows;
using System.Windows.Input;
using System.ComponentModel;
using System.Windows.Threading;
using System;
using System.Windows.Data;
using System.IO;
using System.Xml.Serialization;

namespace WatLogReader
{
	/// <summary>
	/// Interaction logic for Window1.xaml
	/// </summary>
	public partial class WindowX : Window
	{
		public WindowX()
		{
			someUIElement = this;
			InitializeComponent();
			WindowStartupLocation = WindowStartupLocation.CenterScreen;
			Loaded += MeLoaded;

			timer.Interval = TimeSpan.FromSeconds(2);
			timer.Tick += timer_Tick;
			timer.Start();
		}
		static public UIElement someUIElement;
		DispatcherTimer timer = new DispatcherTimer(DispatcherPriority.Render);

		void timer_Tick(object sender, EventArgs e)
		{
			Utils.Log("WindowX {0}", ++ii);
		}
		int ii;

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if (e.Key == Key.Escape)
				Close();
		}

		void MeLoaded(object sender, RoutedEventArgs e)
		{
			string dir = Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]);

			ViewModelX viewModel = new ViewModelX();
			viewModel.PropertyChanged += ViewmodelPropertyChanged;
			CommandBindings.AddRange(viewModel.CommandBindings);

			viewModel.Reader = new LogReader();
			viewModel.Reader.LoadFile(dir + "\\WatLogReader.log");

			DataContext = myLogControl.ViewModel = viewModel;
		}

		void ViewmodelPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "IsFilterEnabled")
			{
				HandleFilterChanged();
			}
		}

		void HandleFilterChanged()
		{
			(DataContext as ViewModelX).HandleRecordsChanged(false);
		}
	}

	public class ViewModelX : LogControlVM
	{
		public ViewModelX()
		{
			CommandBindings.Add(new CommandBinding(ConfigureCmd, ExecuteConfigureCmd, CanExecuteConfigureCmd));
		}

		void CanExecuteConfigureCmd(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = true;
		}

		void ExecuteConfigureCmd(object sender, ExecutedRoutedEventArgs e)
		{
			string xml = Utils.ToXML(this);
			ViewModelX clone = Utils.FromXML<ViewModelX>(xml);

			FilterDialog dlg = new FilterDialog(clone);
			Utils.MoveToMouse(dlg, "Configure filter");

			if (dlg.ShowDialog() == true)
			{
				ReadFilterSettings(clone);
				SetFocusOnSelected();
			}
		}

		[XmlIgnore]
		public LogReader Reader
		{
			get { return reader; }
			set
			{
				Utils.OnlyOnce(reader, value);
				reader = value;
				reader.PropertyChanged += ReaderPropertyChanged;

				Records = reader.Records;
				RecordsView.CurrentChanged += RecordsViewCurrentChanged;
			}
		}
		LogReader reader;

		void RecordsViewCurrentChanged(object sender, EventArgs e)
		{
			ListCollectionView view = sender as ListCollectionView;
			if (view == null || dontCare != 0)
				return;

			int index = view.CurrentPosition;
			FollowTail = index < 0;
		}

		[XmlIgnore]
		public bool FollowTail
		{
			get { return followTail; }
			set
			{
				if (followTail != value)
				{
					followTail = value;
					FirePropertyChanged("FollowTail");
					CheckScrolling();
				}
			}
		}
		bool followTail = true;

		[XmlIgnore]
		public string ReaderFileName
		{
			get { return reader.FileName; }
			set { }
		}

		[XmlIgnore]
		public string ReaderFileInfo
		{
			get { return string.IsNullOrEmpty(DisplayName) ? null : DisplayName + " " + reader.RecordsCounter; }
		}

		/// <summary>
		/// 
		/// </summary>
		void ReaderPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			//--- need to do it on the UI thread!
			WindowX.someUIElement.Dispatcher.BeginInvoke(DispatcherPriority.Render,
				(DispatcherOperationCallback)delegate(object obj)
				{
					readerPropertyChanged(sender, e);
					return null;
				},
				null);
		}

		void readerPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "RecordsChanged")
			{
				HandleRecordsChanged();
			}
			else if (e.PropertyName == "FileName")
			{
				HandleReaderFileNameChanged();
			}
			//else if (e.PropertyName == "LoadFile")
			//{
			//	reader.LoadFile(LastFile);
			//}
			//else if (e.PropertyName == "NoLastFile")
			//{
			//	HandleNoLastFile();
			//}
			else if (e.PropertyName == "ShowProgress")
			{
				FirePropertyChanged("ShowProgress");
				FirePropertyChanged("ReaderFileInfo");
			}
		}

		/// <summary>
		/// 
		/// </summary>
		void HandleReaderFileNameChanged()
		{
			DisplayName = Path.GetFileName(reader.FileName);
			FirePropertyChanged("ReaderFileName");
			FirePropertyChanged("ReaderFileInfo");
		}

		/// <summary>
		/// 
		/// </summary>
		public void HandleRecordsChanged(bool mode = true)
		{
			dontCare++;
			Refresh();
			CheckScrolling();
			dontCare--;
		}
		int dontCare;

		/// <summary>
		/// 
		/// </summary>
		public void CheckScrolling()
		{
			if (FollowTail)
			{
				dontCare++;
				ScrollToBottom();
				dontCare--;
			}
		}
	}
}
