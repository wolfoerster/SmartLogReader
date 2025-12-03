//******************************************************************************************
// Copyright © 2021 - 2025 Wolfgang Foerster (wolfoerster@gmx.de)
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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Newtonsoft.Json;
using SmartLogReader.Common;

namespace SmartLogReader
{
    public class ConfigurePluginsVM : ViewModel
    {
        public ConfigurePluginsVM(List<(string, bool)> configuredParsers)
        {
            foreach (var (name, isSelected) in configuredParsers)
                Plugins.Add(new PluginVM { Name = name, IsSelected = isSelected});

            CommandBindings.Add(new CommandBinding(MoveUpCmd, ExecuteMoveUpCmd, CanExecuteMoveUpCmd));
            CommandBindings.Add(new CommandBinding(MoveDownCmd, ExecuteMoveDownCmd, CanExecuteMoveDownCmd));
        }

        public ObservableCollection<PluginVM> Plugins { get; } = new ObservableCollection<PluginVM>();

        public PluginVM SelectedPlugin { get; set; }

        void CanExecuteMoveUpCmd(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = SelectedPlugin != null && SelectedPlugin != Plugins.First();
        }

        void ExecuteMoveUpCmd(object sender, ExecutedRoutedEventArgs e)
        {
            var index = Plugins.IndexOf(SelectedPlugin);
            Plugins.Move(index, index - 1);
        }

        void CanExecuteMoveDownCmd(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = SelectedPlugin != null && SelectedPlugin != Plugins.Last();
        }

        void ExecuteMoveDownCmd(object sender, ExecutedRoutedEventArgs e)
        {
            var index = Plugins.IndexOf(SelectedPlugin);
            Plugins.Move(index, index + 1);
        }
    }
}
