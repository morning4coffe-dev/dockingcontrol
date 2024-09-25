using Microsoft.UI;
using Windows.UI;

namespace DockingControlSample;

public sealed partial class DockArea : UserControl
{
    public DockArea()
    {
        this.InitializeComponent();

        Random random = new Random();
        byte r = (byte)random.Next(256);
        byte g = (byte)random.Next(256);
        byte b = (byte)random.Next(256);
        PanelContainer.BorderBrush = new SolidColorBrush(Color.FromArgb(255, r, g, b));

        HideDragIndicator();
    }


    public void AddPanel(Border panel)
    {
        PanelContainer.Children.Add(panel);
    }

    public bool ContainsPanel(Border panel)
    {
        return PanelContainer.Children.Contains(panel);
    }

    public void RemovePanel(Border panel)
    {
        PanelContainer.Children.Remove(panel);
    }

    public void ShowDragIndicator()
    {
        PanelContainer.Background = PanelContainer.BorderBrush;
        PanelContainer.Background.Opacity = 0.5;
    }

    public void HideDragIndicator()
    {
        PanelContainer.Background = new SolidColorBrush(Colors.Transparent);
    }
}
