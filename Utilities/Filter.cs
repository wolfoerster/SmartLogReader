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
using Newtonsoft.Json.Linq;

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
            PropertyNames = new List<string>
            {
                "Time",
                "ProcessId",
                "AppDomainId",
                "ThreadId",
                "Level",
                "Class",
                "Method",
                "Message",
                "JSON Props",
            };

            OpCodes = new List<string>
            {
                "=",
                "≠",
                "<",
                ">"
            };
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
            List<IndexValuePair> list = new List<IndexValuePair>
            {
                new IndexValuePair(0, record.TimeString),
                new IndexValuePair(1, record.ProcessId.ToString()),
                new IndexValuePair(2, record.AppDomainId.ToString()),
                new IndexValuePair(3, record.ThreadId.ToString()),
                new IndexValuePair(4, record.LevelString),
                new IndexValuePair(5, Check(record.Logger)),
                new IndexValuePair(6, Check(record.Method)),
                new IndexValuePair(7, record.ShortMessage),
                new IndexValuePair(8, GetJsonProperties(record.Json)),
            };
            return list;
        }

        private static string GetJsonProperties(JObject jObject)
        {
            if (jObject != null)
            {
                try
                {
                    var nogo = new List<string> { "Message", "SourceContext", "MethodName", "ActionName", "RequestPath", "SpanId" };
                    var result = string.Empty;
                    var properties = jObject.Properties();

                    foreach (var property in properties)
                    {
                        var name = property.Name;
                        if (!nogo.Contains(name))
                        {
                            var value = property.Value.ToString();
                            if (!string.IsNullOrWhiteSpace(value))
                            {
                                if (result.Length > 0) result += "\r\n";
                                result += $"{name}:{value}";
                            }
                        }
                    }

                    return result;
                }
                catch
                {
                }
            }

            return null;
        }

        public const string Empty = "<empty>";

        private static string Check(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? Empty : value;
        }

        /// <summary>
        /// Perform a test on a certain property of a record.
        /// </summary>
        public virtual bool Test(Record record)
        {
            switch (PropertyIndex)
            {
                case 0: return Test(record.UtcTime);
                case 1: return Test(record.ProcessId);
                case 2: return Test(record.AppDomainId);
                case 3: return Test(record.ThreadId);
                case 4: return Test(record.LevelString);
                case 5: return Test(Check(record.Logger));
                case 6: return Test(Check(record.Method));
                case 7: return Test(record.Message);
                case 8: return Test(record.Json);
            }
            return true;
        }

        protected virtual bool Test(JObject jObject)
        {
            if (jObject == null || string.IsNullOrWhiteSpace(expectedValue))
                return false;

            var actualValue = GetJsonProperties(jObject);
            var isContained = actualValue.Contains(expectedValue);
            return OpCodeIndex == 0 ? isContained : !isContained;
        }

        /// <summary>
        /// Perform a test on a string property of a record.
        /// </summary>
        protected virtual bool Test(string actualValue)
        {
            if (actualValue == null || expectedValue == null)
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
            if (expectedValue.Length == 0)
                return actualValue.Length == 0;

            string value = expectedValue;
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
                if (actualValue.Length > 1 && ExpectedValue.Length > 1)
                {
                    var s1 = actualValue.Substring(0, 2);
                    var s2 = ExpectedValue.Substring(0, 2);
                    return s1.equals(s2);
                }
                else
                {
                    return false;
                }
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
