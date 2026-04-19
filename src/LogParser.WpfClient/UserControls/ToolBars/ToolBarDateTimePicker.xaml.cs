using System;
using System.Windows;
using System.Windows.Controls;
using MaterialDesignThemes.Wpf;

namespace LogParser.WpfClient.UserControls.ToolBars;
/// <summary>
/// Interaction logic for ToolBarTimePicker.xaml
/// </summary>
public partial class ToolBarDateTimePicker : UserControl
{
    public ToolBarDateTimePicker()
    {
        InitializeComponent();
    }



    public string? Hint
    {
        get { return (string?)GetValue(HintProperty); }
        set { SetValue(HintProperty, value); }
    }

    // Using a DependencyProperty as the backing store for Hint.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty HintProperty =
        DependencyProperty.Register(nameof(Hint), typeof(string), typeof(ToolBarDateTimePicker), new PropertyMetadata(default));



    public DateTime? DateTime
    {
        get => (DateTime?)GetValue(DateTimeProperty);
        set => SetValue(DateTimeProperty, value);
    }

    // Using a DependencyProperty as the backing store for DateTime.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty DateTimeProperty =
        DependencyProperty.Register(
            nameof(DateTime),
            typeof(DateTime?),
            typeof(ToolBarDateTimePicker),
            new PropertyMetadata(default));


    public void ClockDialogOpenedEventHandler(object sender, DialogOpenedEventArgs eventArgs)
    {
        m_Calendar.SelectedDate = DateTime;
        m_Clock.Time = DateTime ?? default;
    }

    public void ClockDialogClosingEventHandler(object sender, DialogClosingEventArgs eventArgs)
    {
        if (Equals(eventArgs.Parameter, "1")
            && m_Calendar.SelectedDate is DateTime selectedDate)
        {
            DateTime = selectedDate.AddSeconds(m_Clock.Time.TimeOfDay.TotalSeconds);
        }
    }
}
