using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LogParser.WpfClient.UserControls.ToolBars;

/// <summary>
/// Interaction logic for StandardToolBar.xaml
/// </summary>
public partial class StandardToolBar : ToolBar
{
    public StandardToolBar()
    {
        InitializeComponent();
    }

    public bool RawLogsToggler
    {
        get => (bool)GetValue(RawLogsTogglerProperty);
        set => SetValue(RawLogsTogglerProperty, value);
    }

    // Using a DependencyProperty as the backing store for RawLogsToggler.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty RawLogsTogglerProperty =
        DependencyProperty.Register(
            nameof(RawLogsToggler),
            typeof(bool),
            typeof(StandardToolBar),
            new PropertyMetadata(false));
}
