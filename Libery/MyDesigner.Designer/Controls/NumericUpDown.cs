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

using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Styling;

namespace MyDesigner.Designer.Controls;

public class NumericUpDown : TemplatedControl
{
    protected override Type StyleKeyOverride => typeof(NumericUpDown);
    
    public static readonly StyledProperty<int> DecimalPlacesProperty =
        AvaloniaProperty.Register<NumericUpDown, int>(nameof(DecimalPlaces));

    public static readonly StyledProperty<double> MinimumProperty =
        AvaloniaProperty.Register<NumericUpDown, double>(nameof(Minimum));

    public static readonly StyledProperty<double> MaximumProperty =
        AvaloniaProperty.Register<NumericUpDown, double>(nameof(Maximum), 100.0);

    public static readonly StyledProperty<double?> ValueProperty =
        AvaloniaProperty.Register<NumericUpDown, double?>(nameof(Value), 0.0, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<double> SmallChangeProperty =
        AvaloniaProperty.Register<NumericUpDown, double>(nameof(SmallChange), 1.0);

    public static readonly StyledProperty<double> LargeChangeProperty =
        AvaloniaProperty.Register<NumericUpDown, double>(nameof(LargeChange), 10.0);

    private DragRepeatButton downButton;
    private TextBox textBox;
    private DragRepeatButton upButton;

    public int DecimalPlaces
    {
        get => GetValue(DecimalPlacesProperty);
        set => SetValue(DecimalPlacesProperty, value);
    }

    public double Minimum
    {
        get => GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public double Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public double? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public double SmallChange
    {
        get => GetValue(SmallChangeProperty);
        set => SetValue(SmallChangeProperty, value);
    }

    public double LargeChange
    {
        get => GetValue(LargeChangeProperty);
        set => SetValue(LargeChangeProperty, value);
    }

    private bool IsDragging
    {
        get => upButton?.IsDragging == true || downButton?.IsDragging == true;
        set
        {
            if (upButton != null) upButton.IsDragging = value;
            if (downButton != null) downButton.IsDragging = value;
        }
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        upButton = e.NameScope.Find<DragRepeatButton>("PART_UpButton");
        downButton = e.NameScope.Find<DragRepeatButton>("PART_DownButton");
        textBox = e.NameScope.Find<TextBox>("PART_TextBox");

        if (upButton != null)
            upButton.Click += upButton_Click;
        if (downButton != null)
            downButton.Click += downButton_Click;

        if (textBox != null)
            textBox.LostFocus += OnTextBoxLostFocus;

        if (upButton != null && downButton != null)
        {
            var upDrag = new DragListener(upButton);
            var downDrag = new DragListener(downButton);

            upDrag.Started += delegate
            {
                OnDragStarted();
                IsDragging = true;
            };
            upDrag.Changed += delegate { SmallUp(); };
            upDrag.Completed += delegate
            {
                OnDragCompleted();
                IsDragging = false;
            };

            downDrag.Started += delegate
            {
                OnDragStarted();
                IsDragging = true;
            };
            downDrag.Changed += delegate { SmallDown(); };
            downDrag.Completed += delegate
            {
                OnDragCompleted();
                IsDragging = false;
            };
        }

        Print();
    }

    private void downButton_Click(object sender, RoutedEventArgs e)
    {
        if (!IsDragging) SmallDown();
    }

    private void upButton_Click(object sender, RoutedEventArgs e)
    {
        if (!IsDragging) SmallUp();
    }

    protected virtual void OnDragStarted()
    {
    }

    protected virtual void OnDragCompleted()
    {
    }

    public void SmallUp()
    {
        MoveValue(SmallChange);
    }

    public void SmallDown()
    {
        MoveValue(-SmallChange);
    }

    public void LargeUp()
    {
        MoveValue(LargeChange);
    }

    public void LargeDown()
    {
        MoveValue(-LargeChange);
    }

    private void MoveValue(double delta)
    {
        if (!Value.HasValue)
            return;

        double result;
        if (double.IsNaN((double)Value) || double.IsInfinity((double)Value))
            SetValue(delta);
        else if (textBox != null && double.TryParse(textBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
            SetValue(result + delta);
        else
            SetValue((double)Value + delta);
    }

    private void Print()
    {
        if (textBox != null && Value.HasValue)
        {
            textBox.Text = Value?.ToString("F" + DecimalPlaces, CultureInfo.InvariantCulture);
            textBox.CaretIndex = int.MaxValue;
        }
    }

    private void SetValue(double? newValue)
    {
        newValue = CoerceValue(newValue);
        if (Value != newValue && !(Value.HasValue && double.IsNaN(Value.Value) && newValue.HasValue &&
                                   double.IsNaN(newValue.Value)))
            Value = newValue;
    }

    private double? CoerceValue(double? newValue)
    {
        if (!newValue.HasValue)
            return null;

        return Math.Max(Minimum, Math.Min((double)newValue, Maximum));
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        switch (e.Key)
        {
            case Key.Enter:
                SetInputValue();
                textBox?.SelectAll();
                e.Handled = true;
                break;
            case Key.Up:
                SmallUp();
                e.Handled = true;
                break;
            case Key.Down:
                SmallDown();
                e.Handled = true;
                break;
            case Key.PageUp:
                LargeUp();
                e.Handled = true;
                break;
            case Key.PageDown:
                LargeDown();
                e.Handled = true;
                break;
        }
    }

    private void SetInputValue()
    {
        if (textBox == null) return;
        
        double result;
        if (double.TryParse(textBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
            SetValue(result);
        else
            Print();
    }

    private void OnTextBoxLostFocus(object sender, RoutedEventArgs e)
    {
        SetInputValue();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ValueProperty)
        {
            Value = CoerceValue((double?)change.NewValue);
            Print();
        }
        else if (change.Property == SmallChangeProperty && !IsSet(LargeChangeProperty))
        {
            LargeChange = SmallChange * 10;
        }
    }
}

public class DragRepeatButton : RepeatButton
{
    public static readonly StyledProperty<bool> IsDraggingProperty =
        AvaloniaProperty.Register<DragRepeatButton, bool>(nameof(IsDragging));
    protected override Type StyleKeyOverride => typeof(DragRepeatButton);
    public bool IsDragging
    {
        get => GetValue(IsDraggingProperty);
        set => SetValue(IsDraggingProperty, value);
    }
}