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

//#define DEBUG_ADORNERLAYER

using System.Collections;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;
using MyDesigner.Design.Adorners;

namespace MyDesigner.Designer.Controls;

/// <summary>
///     A control that displays adorner panels.
/// </summary>
public sealed class AdornerLayer : Panel, IAdornerLayer
{
    /// <summary>
    /// Helper method to check if a control is descendant of another control (Avalonia 11.x compatibility)
    /// </summary>
    private static bool IsDescendantOfPanel(Control child, Control parent)
    {
        if (child == null || parent == null) return false;
        
        var current = child.Parent;
        while (current != null)
        {
            if (current == parent) return true;
            current = current.Parent;
        }
        return false;
    }

    #region AdornerPanelCollection

    internal sealed class AdornerPanelCollection : ICollection<AdornerPanel>, IReadOnlyCollection<AdornerPanel>
    {
        private readonly AdornerLayer _layer;

        public AdornerPanelCollection(AdornerLayer layer)
        {
            _layer = layer;
        }

        public int Count => _layer.Children.Count;

        public bool IsReadOnly => false;

        public void Add(AdornerPanel item)
        {
            if (item == null)
                throw new ArgumentNullException("item");

            _layer.AddAdorner(item);
        }

        public void Clear()
        {
            _layer.ClearAdorners();
        }

        public bool Contains(AdornerPanel item)
        {
            if (item == null)
                throw new ArgumentNullException("item");

            return item.GetVisualParent() == _layer;
        }

        public void CopyTo(AdornerPanel[] array, int arrayIndex)
        {
            foreach (var panel in this)
                array[arrayIndex++] = panel;
        }

        public bool Remove(AdornerPanel item)
        {
            if (item == null)
                throw new ArgumentNullException("item");

            return _layer.RemoveAdorner(item);
        }

        public IEnumerator<AdornerPanel> GetEnumerator()
        {
            foreach (AdornerPanel panel in _layer.Children) yield return panel;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    #endregion

    private readonly Control _designPanel;

#if DEBUG_ADORNERLAYER
		int _totalAdornerCount;
#endif

    internal AdornerLayer(Control designPanel)
    {
        _designPanel = designPanel;

        LayoutUpdated += OnLayoutUpdated;

        Adorners = new AdornerPanelCollection(this);
        
       
        ZIndex = 1;
    }

    private void OnLayoutUpdated(object sender, EventArgs e)
    {
        UpdateAllAdorners(false);
#if DEBUG_ADORNERLAYER
			Debug.WriteLine("Adorner LayoutUpdated. AdornedElements=" + _dict.Count +
							", visible adorners=" + Children.Count + ", total adorners=" + (_totalAdornerCount));
#endif
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        UpdateAllAdorners(true);
    }

    internal AdornerPanelCollection Adorners { get; }

    private sealed class AdornerInfo
    {
        internal readonly List<AdornerPanel> adorners = new();
        internal bool isVisible;
        internal Rect position;
    }

    // adorned element => AdornerInfo
    private Dictionary<Control, AdornerInfo> _dict = new();

    private void ClearAdorners()
    {
        if (_dict.Count == 0)
            return; // already empty

        Children.Clear();
        _dict = new Dictionary<Control, AdornerInfo>();

#if DEBUG_ADORNERLAYER
			_totalAdornerCount = 0;
			Debug.WriteLine("AdornerLayer cleared.");
#endif
    }

    private AdornerInfo GetOrCreateAdornerInfo(Control adornedElement)
    {
        AdornerInfo info;
        if (!_dict.TryGetValue(adornedElement, out info))
        {
            info = _dict[adornedElement] = new AdornerInfo();
            info.isVisible = IsDescendantOfPanel(adornedElement, _designPanel);
        }

        return info;
    }

    private AdornerInfo GetExistingAdornerInfo(Control adornedElement)
    {
        AdornerInfo info;
        _dict.TryGetValue(adornedElement, out info);
        return info;
    }

    private void AddAdorner(AdornerPanel adornerPanel)
    {
        if (adornerPanel.AdornedElement == null)
            throw new DesignerException("adornerPanel.AdornedElement must be set");

        var info = GetOrCreateAdornerInfo(adornerPanel.AdornedElement);
        info.adorners.Add(adornerPanel);

        if (info.isVisible) AddAdornerToChildren(adornerPanel);

#if DEBUG_ADORNERLAYER
			Debug.WriteLine("Adorner added. AdornedElements=" + _dict.Count +
							", visible adorners=" + Children.Count + ", total adorners=" + (++_totalAdornerCount));
#endif
    }

    private void AddAdornerToChildren(AdornerPanel adornerPanel)
    {
        var children = Children;
        var i = 0;
        for (i = 0; i < children.Count; i++)
        {
            var p = (AdornerPanel)children[i];
            if (p.Order > adornerPanel.Order) break;
        }

        children.Insert(i, adornerPanel);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var infiniteSize = new Size(double.PositiveInfinity, double.PositiveInfinity);
        foreach (AdornerPanel adorner in Children) adorner.Measure(infiniteSize);
        return new Size(0, 0);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        foreach (AdornerPanel adorner in Children)
        {
            if (IsDescendantOfPanel(adorner.AdornedElement, _designPanel))
            {
                var transform = adorner.AdornedElement.TransformToVisual(_designPanel);
                Matrix? rt = null;
                
                if (transform != null)
                {
                    // In Avalonia, transforms work differently
                    // We need to get the matrix from the transform
                    rt = transform.Value;
                    
                    // Handle special cases for Canvas children with zero size
                    if (adorner.AdornedDesignItem != null && adorner.AdornedDesignItem.Parent != null &&
                        adorner.AdornedDesignItem.Parent.View is Canvas && 
                        adorner.AdornedElement.Bounds.Height == 0 && adorner.AdornedElement.Bounds.Width == 0)
                    {
                        var width = adorner.AdornedElement.Width;
                        width = width > 0 ? width : 2.0;
                        var height = adorner.AdornedElement.Height;
                        height = height > 0 ? height : 2.0;
                        var xOffset = rt.Value.M31 - width / 2;
                        var yOffset = rt.Value.M32 - height / 2;
                        rt = new Matrix(rt.Value.M11, rt.Value.M12, rt.Value.M21, rt.Value.M22, xOffset, yOffset);
                    }
                }

                if (rt.HasValue)
                {
                    adorner.RenderTransform = new MatrixTransform(rt.Value);
                }
            }

            adorner.Arrange(new Rect(new Point(0, 0), adorner.DesiredSize));
        }

        return finalSize;
    }

    private bool RemoveAdorner(AdornerPanel adornerPanel)
    {
        if (adornerPanel.AdornedElement == null)
            return false;

        var info = GetExistingAdornerInfo(adornerPanel.AdornedElement);
        if (info == null)
            return false;

        if (info.adorners.Remove(adornerPanel))
        {
            if (info.isVisible) Children.Remove(adornerPanel);

            if (info.adorners.Count == 0) _dict.Remove(adornerPanel.AdornedElement);

#if DEBUG_ADORNERLAYER
				Debug.WriteLine("Adorner removed. AdornedElements=" + _dict.Count +
								", visible adorners=" + Children.Count + ", total adorners=" + (--_totalAdornerCount));
#endif

            return true;
        }

        return false;
    }

    public void UpdateAdornersForElement(Control element, bool forceInvalidate)
    {
        var info = GetExistingAdornerInfo(element);
        if (info != null) UpdateAdornersForElement(element, info, forceInvalidate);
    }

    private Rect GetPositionCache(Control element)
    {
        var t = element.TransformToVisual(_designPanel);
        var p = t?.Transform(new Point(0, 0)) ?? new Point(0, 0);
        return new Rect(p, element.Bounds.Size);
    }

    private void UpdateAdornersForElement(Control element, AdornerInfo info, bool forceInvalidate)
    {
        if (IsDescendantOfPanel(element, _designPanel))
        {
            if (!info.isVisible)
            {
                info.isVisible = true;
                // make adorners visible:
                info.adorners.ForEach(AddAdornerToChildren);
            }

            var c = GetPositionCache(element);
            if (forceInvalidate || !info.position.Equals(c))
            {
                info.position = c;
                foreach (var p in info.adorners) p.InvalidateMeasure();
                InvalidateArrange();
            }
        }
        else
        {
            if (info.isVisible)
            {
                info.isVisible = false;
                // make adorners invisible:
                info.adorners.ForEach(adorner => Children.Remove(adorner));
            }
        }
    }

    private void UpdateAllAdorners(bool forceInvalidate)
    {
        foreach (var pair in _dict) UpdateAdornersForElement(pair.Key, pair.Value, forceInvalidate);
    }
}