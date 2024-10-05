using Microsoft.UI;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI;

namespace DockingControlSample;

public sealed partial class DockArea : UserControl
{
    public DockArea()
    {
        this.InitializeComponent();

        Random random = new();
        byte r = (byte)random.Next(256);
        byte g = (byte)random.Next(256);
        byte b = (byte)random.Next(256);
        PanelContainer.BorderBrush = new SolidColorBrush(Color.FromArgb(255, r, g, b));

        HideDragIndicator();
    }

    public void AddPanel(TabViewItem panel)
    {
        PanelContainer.TabItems.Add(panel);
    }

    public bool ContainsPanel(TabViewItem panel)
    {
        return PanelContainer.TabItems.Contains(panel);
    }

    public void RemovePanel(TabViewItem panel)
    {
        PanelContainer.TabItems.Remove(panel);
    }

    public void ShowDragIndicator()
    {
        PanelContainer.Background = PanelContainer.BorderBrush;
        PanelContainer.Background.Opacity = 0.2;

        DockHelperStar.Visibility = Visibility.Visible;
    }

    public void HideDragIndicator()
    {
        PanelContainer.Background = new SolidColorBrush(Colors.Transparent);

        DockHelperStar.Visibility = Visibility.Collapsed;
    }

    public bool IsEmpty()
        => PanelContainer.TabItems.Count == 0;

    private void PanelContainer_TabStripDrop(object sender, DragEventArgs e)
    {
        //e.AcceptedOperation = DataPackageOperation.Move;

        //if (e.Data.GetView().AvailableFormats.Contains("UNODataFormat"))
        //{
        //    e.Data.GetView().GetDataAsync("UNODataFormat").AsTask().ContinueWith(task =>
        //    {
        //        if (task.Result is TabViewItem panel)
        //        {
        //            AddPanel(panel);
        //        }
        //    });
        //}

        //HideDragIndicator();
    }
}
