// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Windows.Foundation;

namespace DockingControlSample;

/// <summary>
/// Defines an area where you can arrange child elements either horizontally or vertically, relative to each other.
/// </summary>
public partial class DockPanel : Panel
{
    private static void DockChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        var senderElement = sender as FrameworkElement;

        //var dockPanel = senderElement?.FindParent<DockPanel>();
        var dockPanel = VisualTreeHelper.GetParent(senderElement) as DockPanel;

        dockPanel?.InvalidateArrange();
    }

    private static void OnPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        var dockPanel = (DockPanel)sender;
        dockPanel.InvalidateMeasure();
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Children.Count is 0)
            return finalSize;

        var currentBounds = new Rect(
            Padding.Left,
            Padding.Top,
            GetPositiveOrZero(finalSize.Width - Padding.Left - Padding.Right),
            GetPositiveOrZero(finalSize.Height - Padding.Top - Padding.Bottom));
        var childrenCount = LastChildFill ? Children.Count - 1 : Children.Count;

        for (var index = 0; index < childrenCount; ++index)
        {
            var child = Children[index];
            var dock = (Dock)child.GetValue(DockProperty);
            double width, height;
            switch (dock)
            {
                case Dock.Left:

                    width = Math.Min(child.DesiredSize.Width, currentBounds.Width);
                    child.Arrange(new Rect(currentBounds.X, currentBounds.Y, width, currentBounds.Height));
                    width += HorizontalSpacing;
                    currentBounds.X += width;
                    currentBounds.Width = GetPositiveOrZero(currentBounds.Width - width);

                    break;
                case Dock.Top:

                    height = Math.Min(child.DesiredSize.Height, currentBounds.Height);
                    child.Arrange(new Rect(currentBounds.X, currentBounds.Y, currentBounds.Width, height));
                    height += VerticalSpacing;
                    currentBounds.Y += height;
                    currentBounds.Height = GetPositiveOrZero(currentBounds.Height - height);

                    break;
                case Dock.Right:

                    width = Math.Min(child.DesiredSize.Width, currentBounds.Width);
                    child.Arrange(new Rect(currentBounds.X + currentBounds.Width - width, currentBounds.Y, width, currentBounds.Height));
                    width += HorizontalSpacing;
                    currentBounds.Width = GetPositiveOrZero(currentBounds.Width - width);

                    break;
                case Dock.Bottom:

                    height = Math.Min(child.DesiredSize.Height, currentBounds.Height);
                    child.Arrange(new Rect(currentBounds.X, currentBounds.Y + currentBounds.Height - height, currentBounds.Width, height));
                    height += VerticalSpacing;
                    currentBounds.Height = GetPositiveOrZero(currentBounds.Height - height);

                    break;
            }
        }

        if (LastChildFill)
        {
            var child = Children[Children.Count - 1];
            child.Arrange(new Rect(currentBounds.X, currentBounds.Y, currentBounds.Width, currentBounds.Height));
        }

        return finalSize;
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        var parentWidth = 0.0;
        var parentHeight = 0.0;
        var accumulatedWidth = Padding.Left + Padding.Right;
        var accumulatedHeight = Padding.Top + Padding.Bottom;
    
        var leftSpacing = false;
        var topSpacing = false;
        var rightSpacing = false;
        var bottomSpacing = false;
        var childrenCount = LastChildFill ? Children.Count - 1 : Children.Count;
    
        for (var index = 0; index < childrenCount; ++index)
        {
            var child = Children[index];
            var childConstraint = new Size(
                GetPositiveOrZero(availableSize.Width - accumulatedWidth),
                GetPositiveOrZero(availableSize.Height - accumulatedHeight));
    
            child.Measure(childConstraint);
            var childDesiredSize = child.DesiredSize;
    
            switch ((Dock)child.GetValue(DockProperty))
            {
                case Dock.Left:
                    leftSpacing = true;
                    parentHeight = Math.Max(parentHeight, accumulatedHeight + childDesiredSize.Height);
                    if (childConstraint.Width is not 0)
                        accumulatedWidth += HorizontalSpacing;
                    accumulatedWidth += childDesiredSize.Width;
                    break;
    
                case Dock.Right:
                    rightSpacing = true;
                    parentHeight = Math.Max(parentHeight, accumulatedHeight + childDesiredSize.Height);
                    if (childConstraint.Width is not 0)
                        accumulatedWidth += HorizontalSpacing;
                    accumulatedWidth += childDesiredSize.Width;
                    break;
    
                case Dock.Top:
                    topSpacing = true;
                    parentWidth = Math.Max(parentWidth, accumulatedWidth + childDesiredSize.Width);
                    if (childConstraint.Height is not 0)
                        accumulatedHeight += VerticalSpacing;
                    accumulatedHeight += childDesiredSize.Height;
                    break;
    
                case Dock.Bottom:
                    bottomSpacing = true;
                    parentWidth = Math.Max(parentWidth, accumulatedWidth + childDesiredSize.Width);
                    if (childConstraint.Height is not 0)
                        accumulatedHeight += VerticalSpacing;
                    accumulatedHeight += childDesiredSize.Height;
                    break;
            }
        }
    
        if (LastChildFill)
        {
            var child = Children[Children.Count - 1];
            var childConstraint = new Size(
                GetPositiveOrZero(availableSize.Width - accumulatedWidth),
                GetPositiveOrZero(availableSize.Height - accumulatedHeight));
    
            child.Measure(childConstraint);
            var childDesiredSize = child.DesiredSize;
            parentHeight = Math.Max(parentHeight, accumulatedHeight + childDesiredSize.Height);
            parentWidth = Math.Max(parentWidth, accumulatedWidth + childDesiredSize.Width);
            accumulatedHeight += childDesiredSize.Height;
            accumulatedWidth += childDesiredSize.Width;
        }
        else
        {
            if (leftSpacing || rightSpacing)
                accumulatedWidth -= HorizontalSpacing;
            if (bottomSpacing || topSpacing)
                accumulatedHeight -= VerticalSpacing;
        }
    
        parentWidth = Math.Min(availableSize.Width, Math.Max(parentWidth, accumulatedWidth));
        parentHeight = Math.Min(availableSize.Height, Math.Max(parentHeight, accumulatedHeight));
        return new Size(parentWidth, parentHeight);
    }

    private static double GetPositiveOrZero(double value) => Math.Max(value, 0);
}
