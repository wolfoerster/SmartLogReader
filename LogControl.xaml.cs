//******************************************************************************************
// Copyright © 2017 Wolfgang Foerster (wolfoerster@gmx.de)
//
// This file is part of the SmartLogReader project which can be found on github.com
//
// SmartLogReader is free software: you can redistribute it and/or modify it under the terms 
// of the GNU General Public License as published by the Free Software Foundation, 
// either version 3 of the License, or (at your option) any later version.
// 
// SmartLogReader is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; 
// without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 
// See the GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <http://www.gnu.org/licenses/>.
//******************************************************************************************
using System.Windows;
using System.Windows.Input;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Threading;

namespace SmartLogReader
{
    /// <summary>
    /// Interaction logic for LogControl.xaml
    /// </summary>
    public partial class LogControl : UserControl
    {
        /// <summary>
        /// 
        /// </summary>
        public LogControl()
        {
            string name = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName;
            log = new SimpleLogger($"{name}.{++instanceCounter}");
            InitializeComponent();
            SnapsToDevicePixels = true;
        }
        private readonly SimpleLogger log;
        private static int instanceCounter;

        /// <summary>
        /// 
        /// </summary>
        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);
            viewModel.SetAsCurrentVM();
        }

        /// <summary>
        /// 
        /// </summary>
        public override string ToString()
        {
            return "LogControl[" + viewModel.DisplayName + "]";
        }

        /// <summary>
        /// 
        /// </summary>
        public LogControlVM ViewModel
        {
            get { return viewModel; }
            set
            {
                Utils.OnlyOnce(viewModel, value);
                DataContext = viewModel = value;
                viewModel.PropertyChanged += ViewModelPropertyChanged;
            }
        }
        private LogControlVM viewModel;

        /// <summary>
        /// 
        /// </summary>
        void ViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            log.Debug(e.PropertyName);
            if (e.PropertyName == "ScrollSelectedIntoView")
            {
                HandleScrollSelectedIntoView();
            }
            else if (e.PropertyName == "SetFocusOnSelected")
            {
                HandleSetFocusOnSelected();
            }
            else if (e.PropertyName == "StringNotFound")
            {
                MessageBox.Show(string.Format("The specified text '{0}' was not found in {1}.", viewModel.SearchText, ToString()),
                    "Text not found", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        void MyListBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (Record record in e.RemovedItems)
                record.IsSelected = false;

            foreach (Record record in e.AddedItems)
                record.IsSelected = true;

            HandleScrollSelectedIntoView();
        }

        /// <summary>
        /// 
        /// </summary>
        private void HandleScrollSelectedIntoView()
        {
            if (myListBox.SelectedItem != null)
            {
                myListBox.ScrollIntoView(myListBox.SelectedItem);
                myListBox.ScrollIntoCenter(myListBox.SelectedItem, myListBox.SelectedIndex);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void HandleSetFocusOnSelected()
        {
            this.Dispatch(() => handleSetFocusOnSelected(), DispatcherPriority.Background);
        }

        private void handleSetFocusOnSelected()
        {
            int index = myListBox.SelectedIndex;
            if (index < 0)
                return;

            DependencyObject dpo = myListBox.ItemContainerGenerator.ContainerFromIndex(index);
            if (dpo == null)
                return;

            UIElement listBoxItem = dpo as UIElement;
            if (listBoxItem == null)
                return;

            listBoxItem.Focus();
            //Record record = myListBox.Items[index] as Record;
            //log.Debug($"Set focus on {0}, item: {1}", ToString(), record.ShortString));
        }

        /// <summary>
        /// 
        /// </summary>
        Record SelectedRecord()
        {
            Record record = myListBox.SelectedItem as Record;
            if (record == null)
                return null;

            Point pt = Mouse.GetPosition(myListBox);
            if (pt.X > myListBox.ActualWidth - 20)//scrollbar width
                return null;

            return record;
        }

        /// <summary>
        /// Open the details window.
        /// </summary>
        void MyListBoxMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
                return;

            Record record = SelectedRecord();
            if (record != null)
            {
                var pt = e.GetPosition(this);
                if (pt.X < 150)
                {
                    Record.UtcTime0 = record.UtcTime;
                    viewModel.RefreshAll();
                    return;
                }

                DetailsWindow win = new DetailsWindow(record.LongString);
                win.Owner = Application.Current.MainWindow;
                Utils.MoveToMouse(win, "Details Window");
                win.Show();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        void MeMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            Record record = SelectedRecord();

            if (record == null)
                ShowFilterDialog(false);
            else
                ShowQuickFilterDialog(record);

            HandleSetFocusOnSelected();
        }

        /// <summary>
        /// 
        /// </summary>
        void ShowFilterDialog(bool useLastTopLeft)
        {
            LogControlVM clone = viewModel.Clone();
            FilterDialog dlg = new FilterDialog(clone);
            Utils.MoveToMouse(dlg, "Configure filter - all OR by default", useLastTopLeft);

            if (dlg.ShowDialog() == true)
                viewModel.ReadFilterSettings(clone);
        }

        /// <summary>
        /// 
        /// </summary>
        void ShowQuickFilterDialog(Record record)
        {
            QuickFilterDialog dlg = new QuickFilterDialog(viewModel, record);
            Utils.MoveToMouse(dlg, "Quick Filter - all AND by default");

            if (dlg.ShowDialog() == true)
                ShowFilterDialog(true);
        }
    }
}
