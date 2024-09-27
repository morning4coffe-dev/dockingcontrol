namespace DockingControlSample;

public sealed partial class DockHelper : UserControl
{
    public DockHelper()
    {
        this.InitializeComponent();
    }

    public Dock? DockPosition
    {
        get { return (Dock?)GetValue(DockPositionProperty); }
        set { SetValue(DockPositionProperty, value); }
    }

    public static readonly DependencyProperty DockPositionProperty =
        DependencyProperty.Register("DockPosition", typeof(Dock?), typeof(DockHelper), null);
}
