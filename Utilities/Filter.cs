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
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SmartLogReader
{
    /// <summary>
    /// A filter is testing a record against a certain property.
    /// </summary>
    public class Filter : Notifier
    {
        /// <summary>
        /// Static c'tor to create required lists only once.
        /// </summary>
        static Filter()
        {
            PropertyNames = new List<string>();
            PropertyNames.Add("Time");
            PropertyNames.Add("Level");
            PropertyNames.Add("ProcessId");
            PropertyNames.Add("AppDomainId");
            PropertyNames.Add("ThreadId");
            PropertyNames.Add("Logger");
            PropertyNames.Add("Method");
            PropertyNames.Add("Message");

            OpCodes = new List<string>();
            OpCodes.Add("=");
            OpCodes.Add("≠");
            OpCodes.Add("<");
            OpCodes.Add(">");
        }
        static public List<string> PropertyNames { get; set; }
        static public List<string> OpCodes { get; set; }

        public Filter()
        {
        }

        public Filter(int propertyIndex, int opCodeIndex, string expectedValue)
            : this()
        {
            PropertyIndex = propertyIndex;
            OpCodeIndex = opCodeIndex;
            ExpectedValue = expectedValue;
        }

        /// <summary>
        /// Get all properties of a record as string.
        /// </summary>
        public static List<IndexValuePair> GetRecordProperties(Record record)
        {
            List<IndexValuePair> list = new List<IndexValuePair>();
            list.Add(new IndexValuePair(0, record.TimeString));
            list.Add(new IndexValuePair(1, record.LevelString));
            list.Add(new IndexValuePair(2, record.ProcessId.ToString()));
            list.Add(new IndexValuePair(3, record.AppDomainId.ToString()));
            list.Add(new IndexValuePair(4, record.ThreadId.ToString()));
            list.Add(new IndexValuePair(5, record.Logger));
            list.Add(new IndexValuePair(6, record.Method));
            list.Add(new IndexValuePair(7, record.ShortMessage));
            return list;
        }

        /// <summary>
        /// Perform a test on a certain property of a record.
        /// </summary>
        public virtual bool Test(Record record)
        {
            switch (PropertyIndex)
            {
                case 0: return Test(record.UtcTime);
                case 1: return Test(record.LevelString);
                case 2: return Test(record.ProcessId);
                case 3: return Test(record.AppDomainId);
                case 4: return Test(record.ThreadId);
                case 5: return Test(record.Logger);
                case 6: return Test(record.Method);
                case 7: return Test(record.Message);
            }
            return true;
        }

        /// <summary>
        /// Perform a test on a string property of a record.
        /// </summary>
        protected virtual bool Test(string actualValue)
        {
            if (string.IsNullOrEmpty(actualValue) || string.IsNullOrEmpty(expectedValue))
                return false;

            switch (OpCodeIndex)
            {
                case 0: return IsEqual(actualValue, expectedValue);
                case 1: return !IsEqual(actualValue, expectedValue);
                case 2: return actualValue.CompareTo(expectedValue) < 0;
                case 3: return actualValue.CompareTo(expectedValue) > 0;
            }
            return false;
        }

        /// <summary>
        /// Perform a test on a string property of a record. 
        /// 'bla*' means string starts with 'bla', 
        /// '*bla' means string ends with 'bla', 
        /// '*bla*' means string contains 'bla', 
        /// 'bla' means string equals 'bla'.
        /// </summary>
        bool IsEqual(string actualValue, string expectedValue)
        {
            string value = expectedValue;
            if (string.IsNullOrEmpty(value))
                return false;

            bool wildBegin = value[0] == '*';
            bool wildEnd = value[value.Length - 1] == '*';

            value = value.Trim(new char[] { '*' });
            if (string.IsNullOrEmpty(value))
                return true;

            if (wildBegin && wildEnd)
                return actualValue.contains(value);

            if (wildBegin)
                return actualValue.endsWith(value);

            if (wildEnd)
                return actualValue.startsWith(value);

            return actualValue.equals(value);
        }

        /// <summary>
        /// Perform a test on a integer property of a record.
        /// </summary>
        bool Test(int actualValue)
        {
            switch (OpCodeIndex)
            {
                case 0: return actualValue == expectedInt;
                case 1: return actualValue < expectedInt;
                case 2: return actualValue > expectedInt;
            }
            return false;
        }

        /// <summary>
        /// Perform a test on a DateTime property of a record.
        /// </summary>
        bool Test(DateTime actualValue)
        {
            switch (OpCodeIndex)
            {
                case 0: return actualValue == expectedTime;
                case 1: return actualValue < expectedTime;
                case 2: return actualValue > expectedTime;
            }
            return false;
        }

        public int PropertyIndex { get; set; }

        public string PropertyName
        {
            get { return PropertyNames[PropertyIndex]; }
        }

        public int OpCodeIndex { get; set; }

        public string OpCode
        {
            get { return OpCodes[OpCodeIndex]; }
        }

        public string ExpectedValue
        {
            get { return expectedValue; }
            set
            {
                expectedValue = value;
                int.TryParse(value, out expectedInt);
                DateTime.TryParse(value, out expectedTime);
            }
        }
        int expectedInt;
        string expectedValue;
        DateTime expectedTime;

        public bool AndNext
        {
            get { return andNext; }
            set
            {
                if (andNext != value)
                {
                    andNext = value;
                    OnPropertyChanged();
                }
            }
        }
        bool andNext;

        public override string ToString()
        {
            return PropertyName + " " + OpCode + " " + ExpectedValue;
        }
    }

    public class FilterCollection : ObservableCollection<Filter>
    {
        public FilterCollection()
        {
        }

        public FilterCollection(IEnumerable<Filter> collection)
            : base(collection)
        {
        }

        public FilterCollection(List<Filter> collection)
            : base(collection)
        {
        }

        /// <summary>
        /// Remove the specified filter and, if this is the last of an 'and' chain, set AndNext of the previous filter to false.
        /// </summary>
        new public bool Remove(Filter filter)
        {
            int index = IndexOf(filter);

            //--- if it's the last in an 'and' chain, it's AndNext is false
            if (index > 0 && !filter.AndNext)
            {
                Filter previousFilter = this[index - 1];
                previousFilter.AndNext = false;
            }

            return base.Remove(filter);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class ColorSpec : Filter
    {
        public ColorSpec()
        {
        }

        public ColorSpec(int propertyIndex, int opCodeIndex, string expectedValue)
            : base(propertyIndex, opCodeIndex, expectedValue)
        {
        }

        public int ColorIndex
        {
            get { return colorIndex; }
            set
            {
                if (colorIndex != value)
                {
                    colorIndex = value;
                    OnPropertyChanged();
                }
            }
        }
        private int colorIndex;

        protected override bool Test(string actualValue)
        {
            if (string.IsNullOrEmpty(actualValue) || string.IsNullOrEmpty(ExpectedValue))
                return false;

            if (OpCodeIndex == 0)
            {
                var s1 = actualValue.Substring(0, 2);
                var s2 = ExpectedValue.Substring(0, 2);
                return s1.equals(s2);
            }

            return base.Test(actualValue);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class ColorSpecCollection : ObservableCollection<ColorSpec>
    {
        public ColorSpecCollection()
        {
        }

        public ColorSpecCollection(IEnumerable<ColorSpec> collection)
            : base(collection)
        {
        }

        public ColorSpecCollection(List<ColorSpec> collection)
            : base(collection)
        {
        }
    }
}
