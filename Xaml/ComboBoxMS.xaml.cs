//***************************************************************************************
// Copyright © 2017 Wolfgang Foerster (wolfoerster@gmx.de)
//
//***************************************************************************************
using System.Text;
using System.Windows;
using System.Collections;
using System.Windows.Controls;
using System.Collections.Generic;

namespace SmartLogReader
{
	/// <summary>
	/// Interaction logic for ComboBoxMS.xaml
	/// </summary>
	public partial class ComboBoxMS : UserControl
	{
		public ComboBoxMS()
		{
			InitializeComponent();
			myTextList = new List<SelectableText>();
		}
		List<SelectableText> myTextList;

		#region SelectableText

		class SelectableText : Notifier
		{
			public SelectableText(string text)
			{
				Text = text;
			}

			public string Text
			{
				get { return text; }
				set
				{
					text = value;
					FirePropertyChanged("Text");
				}
			}
			private string text;

			public bool IsSelected
			{
				get { return isSelected; }
				set
				{
					isSelected = value;
					FirePropertyChanged("IsSelected");
				}
			}
			private bool isSelected;
		}

		#endregion SelectableText

		#region ItemsSource

		public IList ItemsSource
		{
			get { return (IList)GetValue(ItemsSourceProperty); }
			set { SetValue(ItemsSourceProperty, value); }
		}

		public static readonly DependencyProperty ItemsSourceProperty =
			 DependencyProperty.Register("ItemsSource", typeof(IList), typeof(ComboBoxMS),
			 new FrameworkPropertyMetadata(null, OnItemsSourceChanged));

		private static void OnItemsSourceChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
		{
			ComboBoxMS comboBox = obj as ComboBoxMS;
			comboBox.MyItemsSourceChanged();
		}

		private void MyItemsSourceChanged()
		{
			myTextList.Clear();
			myTextList.Add(new SelectableText(All));

			foreach (var item in ItemsSource)
			{
				SelectableText text = new SelectableText(item.ToString());
				myTextList.Add(text);
			}

			MyComboBox.ItemsSource = myTextList;
		}

		#endregion ItemsSoucre

		#region SelectedIndices

		public ulong SelectedIndices
		{
			get { return (ulong)GetValue(SelectedIndicesProperty); }
			set { SetValue(SelectedIndicesProperty, value); }
		}

		public static readonly DependencyProperty SelectedIndicesProperty =
			DependencyProperty.Register("SelectedIndices", typeof(ulong), typeof(ComboBoxMS),
			new FrameworkPropertyMetadata(ulong.MaxValue, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedIndicesChanged));

		private static void OnSelectedIndicesChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
		{
			ComboBoxMS comboBox = obj as ComboBoxMS;
			comboBox.SelectNodes();
			comboBox.SetText();
		}

		#endregion SelectedIndices

		#region Text

		public string Text
		{
			get { return (string)GetValue(TextProperty); }
			set { SetValue(TextProperty, value); }
		}

		public static readonly DependencyProperty TextProperty =
		   DependencyProperty.Register("Text", typeof(string), typeof(ComboBoxMS), new UIPropertyMetadata(string.Empty));

		#endregion Text

		#region DefaultText

		public static readonly DependencyProperty DefaultTextProperty =
			DependencyProperty.Register("DefaultText", typeof(string), typeof(ComboBoxMS), new UIPropertyMetadata(string.Empty));

		public string DefaultText
		{
			get { return (string)GetValue(DefaultTextProperty); }
			set { SetValue(DefaultTextProperty, value); }
		}

		#endregion DefaultText

		#region Methods

		string All = "All properties";

		private void OnCheckBoxClick(object sender, RoutedEventArgs e)
		{
			CheckBox checkBox = (CheckBox)sender;

			if ((checkBox.Content as string) == All)
			{
				foreach (SelectableText text in myTextList)
				{
					text.IsSelected = checkBox.IsChecked.Value;
				}
			}
			else
			{
				int count = 0;
				for (int i = 1; i < myTextList.Count; i++)
				{
					SelectableText text = myTextList[i];
					if (text.IsSelected)
						count++;
				}
				myTextList[0].IsSelected = count == myTextList.Count - 1;
			}
			SetSelectedIndices();
			SetText();
		}

		private void SelectNodes()
		{
			int count = 0;
			ulong selectedIndices = SelectedIndices;

			for (int i = 1; i < myTextList.Count; i++)
			{
				ulong bit = (ulong)(1 << (i - 1));
				ulong res = selectedIndices & bit;
				if (myTextList[i].IsSelected = res != 0)
					count++;
			}
			myTextList[0].IsSelected = count == myTextList.Count - 1;
		}

		private void SetSelectedIndices()
		{
			ulong selectedIndices = 0;
			for (int i = 1; i < myTextList.Count; i++)
			{
				SelectableText text = myTextList[i];
				if (text.IsSelected)
				{
					ulong bit = (ulong)(1 << (i - 1));
					selectedIndices |= bit;
				}
			}
			SelectedIndices = selectedIndices;
		}

		private void SetText()
		{
			StringBuilder sb = new StringBuilder();
			if (myTextList[0].IsSelected)
			{
				sb.Append(All);
			}
			else
			{
				for (int i = 1; i < myTextList.Count; i++)
				{
					SelectableText text = myTextList[i];
					if (text.IsSelected)
					{
						sb.Append(text.Text);
						sb.Append(',');
					}
				}
			}
			Text = sb.ToString().TrimEnd(new char[] { ',' });

			if (string.IsNullOrEmpty(Text))
			{
				Text = DefaultText;
			}
		}

		#endregion
	}
}
