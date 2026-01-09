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

using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using MyDesigner.Designer.themes;

namespace MyDesigner.Designer.Controls;

public partial class ColorPicker : UserControl
{
    public static readonly StyledProperty<Color> ColorProperty =
        AvaloniaProperty.Register<ColorPicker, Color>(nameof(Color), new Color(), 
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<int> HProperty =
        AvaloniaProperty.Register<ColorPicker, int>(nameof(H));

    public static readonly StyledProperty<int> SProperty =
        AvaloniaProperty.Register<ColorPicker, int>(nameof(S));

    public static readonly StyledProperty<int> VProperty =
        AvaloniaProperty.Register<ColorPicker, int>(nameof(V));

    public static readonly StyledProperty<byte> RProperty =
        AvaloniaProperty.Register<ColorPicker, byte>(nameof(R));

    public static readonly StyledProperty<byte> GProperty =
        AvaloniaProperty.Register<ColorPicker, byte>(nameof(G));

    public static readonly StyledProperty<byte> BProperty =
        AvaloniaProperty.Register<ColorPicker, byte>(nameof(B));

    public static readonly StyledProperty<byte> AProperty =
        AvaloniaProperty.Register<ColorPicker, byte>(nameof(A));

    public static readonly StyledProperty<string> HexProperty =
        AvaloniaProperty.Register<ColorPicker, string>(nameof(Hex));

    public static readonly StyledProperty<Color> HueColorProperty =
        AvaloniaProperty.Register<ColorPicker, Color>(nameof(HueColor));

    private bool updating;

    public ColorPicker()
    {
        InitializeComponent();
    }

    public Color Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public int H
    {
        get => GetValue(HProperty);
        set => SetValue(HProperty, value);
    }

    public int S
    {
        get => GetValue(SProperty);
        set => SetValue(SProperty, value);
    }

    public int V
    {
        get => GetValue(VProperty);
        set => SetValue(VProperty, value);
    }

    public byte R
    {
        get => GetValue(RProperty);
        set => SetValue(RProperty, value);
    }

    public byte G
    {
        get => GetValue(GProperty);
        set => SetValue(GProperty, value);
    }

    public byte B
    {
        get => GetValue(BProperty);
        set => SetValue(BProperty, value);
    }

    public byte A
    {
        get => GetValue(AProperty);
        set => SetValue(AProperty, value);
    }

    public string Hex
    {
        get => GetValue(HexProperty);
        set => SetValue(HexProperty, value);
    }

    public Color HueColor
    {
        get => GetValue(HueColorProperty);
        set => SetValue(HueColorProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (updating) return;
        updating = true;
        try
        {
            if (change.Property == ColorProperty)
            {
                UpdateSource(ColorSource.Hsv);
                UpdateRest(ColorSource.Hsv);
            }
            else if (change.Property == HProperty || change.Property == SProperty || change.Property == VProperty)
            {
                var c = ColorHelper.ColorFromHsv(H, S / 100.0, V / 100.0);
                c = Color.FromArgb(A, c.R, c.G, c.B);
                Color = c;
                UpdateRest(ColorSource.Hsv);
            }
            else if (change.Property == RProperty || change.Property == GProperty || change.Property == BProperty ||
                     change.Property == AProperty)
            {
                Color = Color.FromArgb(A, R, G, B);
                UpdateRest(ColorSource.Rgba);
            }
            else if (change.Property == HexProperty)
            {
                Color = ColorHelper.ColorFromString(Hex);
                UpdateRest(ColorSource.Hex);
            }
        }
        finally
        {
            updating = false;
        }
    }

    private void UpdateRest(ColorSource source)
    {
        HueColor = ColorHelper.ColorFromHsv(H, 1, 1);
        UpdateSource((ColorSource)(((int)source + 1) % 3));
        UpdateSource((ColorSource)(((int)source + 2) % 3));
    }

    private void UpdateSource(ColorSource source)
    {
        if (source == ColorSource.Hsv)
        {
            double h, s, v;
            ColorHelper.HsvFromColor(Color, out h, out s, out v);

            H = (int)h;
            S = (int)(s * 100);
            V = (int)(v * 100);
        }
        else if (source == ColorSource.Rgba)
        {
            R = Color.R;
            G = Color.G;
            B = Color.B;
            A = Color.A;
        }
        else
        {
            Hex = ColorHelper.StringFromColor(Color);
        }
    }

    private enum ColorSource
    {
        Hsv,
        Rgba,
        Hex
    }
}

internal class HexTextBox : TextBox
{
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            // In Avalonia, we need to manually update the binding
           // var binding = this.GetBindingExpression(TextProperty);
            //if (binding != null)
            //{
            //    // Force update the binding source
            //    this.GetBindingExpression(TextProperty)?.UpdateSource();
            //}
            SelectAll();
        }
    }
}