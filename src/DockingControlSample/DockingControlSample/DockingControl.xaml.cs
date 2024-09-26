using System.Diagnostics;
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

    private Border _newDockCreator;

    private List<DockArea> _dockingAreas = [];

    public DockingControl()
    {
        this.InitializeComponent();

        CreateDockCreator();

        for (int i = 0; i < 3; i++)
        {
            CreateNewDockArea();
        }
    }

    // creates the "star" for user to create new controls
    private void CreateDockCreator()
    {
        _newDockCreator = new Border
        {
            Background = new SolidColorBrush(Colors.Yellow),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Width = 150,
            Height = 100,
            Visibility = Visibility.Collapsed
        };

        MainGrid.Children.Add(_newDockCreator);
        Grid.SetRow(_newDockCreator, 1);
    }

    private DockArea CreateNewDockArea()
    {
        DockArea newDockArea = new()
        {
            Name = $"DockArea_{_dockingAreas.Count + 1}",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        _dockingAreas.Add(newDockArea);
        DCHolder.Children.Add(newDockArea);

        //DCHolder.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        //Grid.SetColumn(newDockArea, DCHolder.Children.Count-1);
        DockPanel.SetDock(newDockArea, (Dock)new Random().Next(0, 4));

        return newDockArea;
    }

    private void OnDragStarted(object sender, PointerRoutedEventArgs e)
    {
        _currentDraggingPanel = sender as Border; // Get the current dragged panel - change from Border to element when added

        if (_currentDraggingPanel == null || _isDragging)
        {
            return;
        }

        // ensure the panel is not already captured
        if (_currentDraggingPanel.PointerCaptures?.Count > 0)
        {
            return;
        }

        _isDragging = true;

        // initialize the transform if not already set
        if (_currentDraggingPanel.RenderTransform == null || _currentDraggingPanel.RenderTransform is not TranslateTransform)
        {
            _transform = new TranslateTransform();
            _currentDraggingPanel.RenderTransform = _transform;
        }
        else
        {
            _transform = (TranslateTransform)_currentDraggingPanel.RenderTransform;
        }

        // capture the initial pointer position
        _startPointerPosition = e.GetCurrentPoint(this).Position;

        // visual feedback of panel - lower the opacity while dragging
        _currentDraggingPanel.Opacity = 0.4;

        // capture the pointer for drag
        _currentDraggingPanel.CapturePointer(e.Pointer);
    }

    private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_isDragging && _currentDraggingPanel != null)
        {
            // Get the current pointer position
            Point currentPointerPosition = e.GetCurrentPoint(this).Position;

            // Calculate the movement offset
            double offsetX = currentPointerPosition.X - _startPointerPosition.X;
            double offsetY = currentPointerPosition.Y - _startPointerPosition.Y;

            // Update the position of the draggable panel using the transform
            _transform.X += offsetX;
            _transform.Y += offsetY;

            // Update the starting position for the next move
            _startPointerPosition = currentPointerPosition;

            // Show visual feedback
            ShowDropIndicator(currentPointerPosition);
        }
    }

    private void OnDragCompleted(object sender, PointerRoutedEventArgs e)
    {
        if (_isDragging && _currentDraggingPanel != null)
        {
            _isDragging = false;

            // Restore the opacity to normal after dragging
            _currentDraggingPanel.Opacity = 1.0;

            // Release the pointer capture
            if (_currentDraggingPanel.PointerCaptures?.Count > 0)
            {
                _currentDraggingPanel.ReleasePointerCaptures();
            }

            // Get the pointer's position relative to the window
            Point pointerPosition = e.GetCurrentPoint(Window.Current.Content).Position;

            if (IsPointerOutsideAppWindow(pointerPosition))
            {
                OpenNewWindowWithPanel(_currentDraggingPanel);

                var currentDockArea = FindDockAreaContainingPanel(_currentDraggingPanel);
                currentDockArea?.RemovePanel(_currentDraggingPanel);
            }
            else
            {
                GeneralTransform transform = _newDockCreator.TransformToVisual(MainGrid);
                Point newDockCreatorPosition = transform.TransformPoint(new Point(0, 0));

                Rect newDockCreatorBounds = new(newDockCreatorPosition.X, newDockCreatorPosition.Y,
                                                _newDockCreator.ActualWidth, _newDockCreator.ActualHeight);

                if (newDockCreatorBounds.Contains(pointerPosition))
                {
                    var area = CreateNewDockArea();
                    SnapToGrid(_currentDraggingPanel, area);
                }
                else
                {
                    var closestArea = GetClosestDockArea(pointerPosition);
                    SnapToGrid(_currentDraggingPanel, closestArea);
                }
            }

            _currentDraggingPanel.RenderTransform = new TranslateTransform();

            // Hide visual feedback
            HideDropIndicators();
            _newDockCreator.Visibility = Visibility.Collapsed;
        }

        _currentDraggingPanel = null;
    }

    private void ShowDropIndicator(Point pointerPosition)
    {
        var closestArea = GetClosestDockArea(pointerPosition);

        if (closestArea != null)
        {
            //var areaTransform = closestArea.TransformToVisual(this);
            //Point areaPosition = areaTransform.TransformPoint(new Point(0, 0));

            _newDockCreator.Visibility = Visibility.Visible;
        }
        else
        {
            _newDockCreator.Visibility = Visibility.Collapsed;
        }

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

    private static bool IsPointerOutsideAppWindow(Point pointerPosition)
    {
        var mainWindowBounds = Window.Current.Bounds;

        return (pointerPosition.X < 0 || pointerPosition.Y < 0 ||
                pointerPosition.X > mainWindowBounds.Width || pointerPosition.Y > mainWindowBounds.Height);
    }

    private static void OpenNewWindowWithPanel(Border panel)
    {
        Window newWindow = new();

        var newContent = new Grid
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };

        newContent.Children.Add(panel);

        newWindow.Content = newContent;
        newWindow.Activate();
    }

    private void SnapToGrid(Border panel, DockArea snapArea)
    {
        if (snapArea != null && panel.Parent != snapArea)
        {
            var currentDockArea = FindDockAreaContainingPanel(panel);
            currentDockArea?.RemovePanel(panel);

            //FMI - to debug the snap area
            Console.WriteLine($"Area: {panel.Name}, Rect: {snapArea.Name}");

            snapArea.AddPanel(panel);
        }

        panel.RenderTransform = new TranslateTransform();
    }

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
                break; // no need to check further if already inside an area
            }

            // calculate distance to the center of the area for snapping
            var areaCenter = new Point(areaRect.Left + areaRect.Width / 2, areaRect.Top + areaRect.Height / 2);
            double distance = Math.Sqrt(Math.Pow(pointerPosition.X - areaCenter.X, 2) + Math.Pow(pointerPosition.Y - areaCenter.Y, 2));

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestArea = area;
            }
        }

        return closestArea;
    }

    // helper method to find the parent Grid in the visual tree
    private static Grid FindParentGrid(DependencyObject child)
    {
        while (child != null)
        {
            if (child is Grid parentGrid)
            {
                return parentGrid;
            }
            child = VisualTreeHelper.GetParent(child);
        }
        return null;
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

        _dockingAreas[1].AddPanel(newPanel);
    }
}
