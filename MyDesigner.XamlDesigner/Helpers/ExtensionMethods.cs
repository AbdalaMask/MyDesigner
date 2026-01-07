// Copyright (c) 2019 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Collections;
using Avalonia.Input;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace MyDesigner.XamlDesigner
{
    static class ExtensionMethods
    {
        public static IEnumerable<string> Paths(this IDataObject data)
        {
            if (data.Contains(DataFormats.Files))
            {
                var files = data.GetFiles();
                if (files != null)
                {
                    foreach (var file in files)
                    {
                        yield return file.Path.LocalPath;
                    }
                }
            }
        }

        public static T GetObject<T>(this IDataObject data)
        {
            return (T)data.Get(typeof(T).FullName);
        }

        public static Stream ToStream(this string s)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(s));
        }

        public static void AddRange<T>(this ObservableCollection<T> col, IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                col.Add(item);
            }
        }

        public static void KeepSyncronizedWith<S>(this IList target, ObservableCollection<S> source, Func<S, object> convert)
        {
            target.Clear();
            foreach (var item in source)
            {
                target.Add(convert(item));
            }

            source.CollectionChanged += delegate(object sender, NotifyCollectionChangedEventArgs e)
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        target.Add(convert((S)e.NewItems[0]));
                        break;

                    case NotifyCollectionChangedAction.Remove:
                        target.RemoveAt(e.OldStartingIndex);
                        break;

                    case NotifyCollectionChangedAction.Move:
                        target.RemoveAt(e.OldStartingIndex);
                        target.Insert(e.NewStartingIndex, e.NewItems[0]);
                        break;

                    case NotifyCollectionChangedAction.Replace:
                        target[e.NewStartingIndex] = convert((S)e.NewItems[0]);
                        break;

                    case NotifyCollectionChangedAction.Reset:
                        target.Clear();
                        break;
                }
            };
        }

        public static object GetDataContext(this RoutedEventArgs e)
        {
            var f = e.Source as Control;
            if (f != null) return f.DataContext;
            return null;
        }
    }
}