using Microsoft.UI;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;

namespace DockingControlSample;

public sealed partial class DockingControl : UserControl
{
    private Point _startPointerPosition;
    private TranslateTransform _transform;

    private bool _isDragging = false;
    private Border _currentDraggingPanel;
    private int _panelCount = 0;

    private List<DockArea> _dockingAreas = new();
    private Window? _secondaryWindow;

    public DockingControl()
    {
        this.InitializeComponent();
        CreateNewDockArea(Dock.Top);
    }

    private DockArea CreateNewDockArea(Dock position, int recommendedWidthOrHeight = 0)
    {
        DockArea newDockArea = new()
        {
            Name = $"DockArea_{_dockingAreas.Count + 1}",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        if (recommendedWidthOrHeight > 0)
        {
            if (position == Dock.Left || position == Dock.Right)
            {
                newDockArea.Width = recommendedWidthOrHeight;
            }
            else
            {
                newDockArea.Height = recommendedWidthOrHeight;
            }
        }

        _dockingAreas.Add(newDockArea);
        DCHolder.Children.Add(newDockArea);

        DockPanel.SetDock(newDockArea, position);

        return newDockArea;
    }

    private void OnDragStarted(object sender, PointerRoutedEventArgs e)
    {
        _currentDraggingPanel = sender as Border;

        if (_currentDraggingPanel == null || _isDragging)
        {
            return;
        }

        _startPointerPosition = e.GetCurrentPoint(this).Position;

        if (_currentDraggingPanel.RenderTransform == null || _currentDraggingPanel.RenderTransform is not TranslateTransform)
        {
            _transform = new TranslateTransform();
            _currentDraggingPanel.RenderTransform = _transform;
        }
        else
        {
            _transform = (TranslateTransform)_currentDraggingPanel.RenderTransform;
        }

        _currentDraggingPanel.Opacity = 0.4;
        _isDragging = true;
        _currentDraggingPanel.CapturePointer(e.Pointer);
    }

    private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_isDragging && _currentDraggingPanel != null)
        {
            Point currentPointerPosition = e.GetCurrentPoint(this).Position;
            double offsetX = currentPointerPosition.X - _startPointerPosition.X;
            double offsetY = currentPointerPosition.Y - _startPointerPosition.Y;

            _transform.X += offsetX;
            _transform.Y += offsetY;

            _startPointerPosition = currentPointerPosition;

            ShowDropIndicator(currentPointerPosition);
        }
    }

    private void OnDragCompleted(object sender, PointerRoutedEventArgs e)
    {
        if (_isDragging && _currentDraggingPanel != null)
        {
            _isDragging = false;
            _currentDraggingPanel.Opacity = 1.0;

            if (_currentDraggingPanel.PointerCaptures?.Count > 0)
            {
                _currentDraggingPanel.ReleasePointerCaptures();
            }

            Point pointerPosition = e.GetCurrentPoint(this).Position;

            if (IsPointerOutsideAppWindow(pointerPosition))
            {
                OpenNewWindowWithPanel(_currentDraggingPanel);
                var currentDockArea = FindDockAreaContainingPanel(_currentDraggingPanel);
                RemovePanelFromArea(currentDockArea, _currentDraggingPanel);
                HideDropIndicators();
            }
            else
            {
                HandleDragInsideOrBackToMainWindow(pointerPosition);
            }

            _currentDraggingPanel.RenderTransform = new TranslateTransform();
        }

        _currentDraggingPanel = null;
    }

    private void HandleDragInsideOrBackToMainWindow(Point pointerPosition)
    {
        var currentDockArea = FindDockAreaContainingPanel(_currentDraggingPanel);

        // TODO for more Windows add a tag (thanks to which, we could recognize the actual Window,
        // now only one window open works correctly) and not use Grid for a detection method
        bool isInSecondaryWindow = _currentDraggingPanel.Parent is Grid && _secondaryWindow != null;

        if (currentDockArea != null)
        {
            HandleDragInsideMainWindow(pointerPosition, currentDockArea);
        }
        else
        {
            DockArea closestDockArea = GetClosestDockArea(pointerPosition);
            if (closestDockArea != null)
            {
                HandleDragInsideMainWindow(pointerPosition, closestDockArea);

                if (isInSecondaryWindow)
                {
                    _secondaryWindow?.Close();
                    _secondaryWindow = null;
                }
            }
        }

        HideDropIndicators();
    }

    private void HandleDragInsideMainWindow(Point pointerPosition, DockArea currentDockArea)
    {
        var dockHelpers = FindDockHelpersInVisualTree(currentDockArea);
        bool newDockAreaCreated = false;

        foreach (var dockHelper in dockHelpers)
        {
            GeneralTransform transform = dockHelper.TransformToVisual(this);
            Point dockHelperPosition = transform.TransformPoint(new Point(0, 0));
            Rect dockHelperBounds = new Rect(dockHelperPosition.X, dockHelperPosition.Y,
                                              dockHelper.ActualWidth, dockHelper.ActualHeight);

            if (dockHelperBounds.Contains(pointerPosition))
            {
                if (dockHelper.DockPosition != null)
                {
                    RemovePanelFromArea(currentDockArea, _currentDraggingPanel);

                    var newDockArea = CreateNewDockArea((Dock)dockHelper.DockPosition, 350);
                    SnapToGrid(_currentDraggingPanel, newDockArea);
                    newDockAreaCreated = true;
                }

                HideDropIndicators();
                return;
            }
        }

        if (!newDockAreaCreated)
        {
            var closestArea = GetClosestDockArea(pointerPosition);
            SnapToGrid(_currentDraggingPanel, closestArea);
        }

        HideDropIndicators();
    }

    private void OpenNewWindowWithPanel(Border panel)
    {
        _secondaryWindow = new Window();

        var newContent = new Grid
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };

        if (panel.Parent is Panel parentPanel)
        {
            parentPanel.Children.Remove(panel);
        }

        newContent.Children.Add(panel);

        panel.PointerPressed += OnDragStarted;
        panel.PointerMoved += OnPointerMoved;
        panel.PointerReleased += OnDragCompleted;

        _secondaryWindow.Content = newContent;
        _secondaryWindow.Activate();
    }

    private void RemovePanelFromArea(DockArea dockArea, Border panel)
    {
        if (dockArea is not { })
        {
            return;
        }

        dockArea.RemovePanel(panel);

        if (dockArea.IsEmpty() && _dockingAreas.Count > 1)
        {
            _dockingAreas.Remove(dockArea);
            DCHolder.Children.Remove(dockArea);
        }
    }

    private void SnapToGrid(Border panel, DockArea snapArea)
    {
        if (snapArea != null && panel.Parent != snapArea)
        {
            var currentDockArea = FindDockAreaContainingPanel(panel);
            RemovePanelFromArea(currentDockArea, panel);
            snapArea.AddPanel(panel);
        }
        panel.RenderTransform = new TranslateTransform();
    }

    private DockArea FindDockAreaContainingPanel(Border panel)
    {
        foreach (var area in _dockingAreas)
        {
            if (area.ContainsPanel(panel))
            {
                return area;
            }
        }
        return null;
    }

    private bool IsPointerOutsideAppWindow(Point pointerPosition)
        => (pointerPosition.X < 0 || pointerPosition.Y < 0 ||
            pointerPosition.X > ActualWidth || pointerPosition.Y > ActualHeight);

    private bool IsPointerInsideMainWindow(Point pointerPosition)
        => (pointerPosition.X >= 0 && pointerPosition.Y >= 0 &&
            pointerPosition.X <= ActualWidth && pointerPosition.Y <= ActualHeight);

    private DockArea GetClosestDockArea(Point pointerPosition)
    {
        DockArea closestArea = null;
        double closestDistance = double.MaxValue;

        foreach (var area in _dockingAreas)
        {
            var areaTransform = area.TransformToVisual(this);
            Point areaPosition = areaTransform.TransformPoint(new Point(0, 0));
            var areaRect = new Rect(areaPosition.X, areaPosition.Y, area.ActualWidth, area.ActualHeight);

            if (areaRect.Contains(pointerPosition))
            {
                closestArea = area;
                break;
            }

            var areaCenter = new Point(areaRect.Left + areaRect.Width / 2, areaRect.Top + areaRect.Height / 2);
            double distance = Math.Sqrt(Math.Pow(areaCenter.X - pointerPosition.X, 2) + Math.Pow(areaCenter.Y - pointerPosition.Y, 2));
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestArea = area;
            }
        }

        return closestArea;
    }

    private void ShowDropIndicator(Point pointerPosition)
    {
        var closestArea = GetClosestDockArea(pointerPosition);
        HideDropIndicators();
        closestArea?.ShowDragIndicator();
    }

    private void HideDropIndicators()
    {
        foreach (var area in _dockingAreas)
        {
            area.HideDragIndicator();
        }
    }

    private static IEnumerable<DockHelper> FindDockHelpersInVisualTree(DependencyObject parent)
    {
        var dockHelpers = new List<DockHelper>();
        int childCount = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < childCount; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is DockHelper dockHelper)
            {
                dockHelpers.Add(dockHelper);
            }
            dockHelpers.AddRange(FindDockHelpersInVisualTree(child));
        }
        return dockHelpers;
    }

    private void OnAddPanelClicked(object sender, RoutedEventArgs e)
    {
        Border newPanel = new()
        {
            Background = new SolidColorBrush(Colors.LightCoral),
            Width = 150,
            Height = 100,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(10),
            Name = $"DraggablePanel_{_panelCount++}",
        };

        newPanel.Child = new TextBlock()
        {
            Text = newPanel.Name,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        newPanel.PointerPressed += OnDragStarted;
        newPanel.PointerMoved += OnPointerMoved;
        newPanel.PointerReleased += OnDragCompleted;

        var dockArea = GetClosestDockArea(new Point(0, 0));
        dockArea.AddPanel(newPanel);
    }
}
