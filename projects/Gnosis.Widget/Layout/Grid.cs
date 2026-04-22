using Gnosis.Widget.Element;
using Gnosis.Widget.Render;

namespace Gnosis.Widget.Layout;

public sealed class Grid : ContainerElement
{
    #region 属性

    public List<GridRowDefinition> RowDefinitions { get; } = [new GridRowDefinition(GridLength.Auto)];

    public List<GridColumnDefinition> ColumnDefinitions { get; } = [new GridColumnDefinition(GridLength.Auto)];

    public float RowSpacing { get; set; } = 0;

    public float ColumnSpacing { get; set; } = 0;

    #endregion

    #region 附加属性

    private readonly Dictionary<WidgetElement, int> _rowMap = new();
    private readonly Dictionary<WidgetElement, int> _columnMap = new();
    private readonly Dictionary<WidgetElement, int> _rowSpanMap = new();
    private readonly Dictionary<WidgetElement, int> _columnSpanMap = new();

    public static void SetRow(WidgetElement element, int row)
    {
        if (element.Parent is Grid grid)
        {
            grid._rowMap[element] = row;
        }
    }

    public static void SetColumn(WidgetElement element, int column)
    {
        if (element.Parent is Grid grid)
        {
            grid._columnMap[element] = column;
        }
    }

    public static void SetRowSpan(WidgetElement element, int span)
    {
        if (element.Parent is Grid grid)
        {
            grid._rowSpanMap[element] = span;
        }
    }

    public static void SetColumnSpan(WidgetElement element, int span)
    {
        if (element.Parent is Grid grid)
        {
            grid._columnSpanMap[element] = span;
        }
    }

    public static int GetRow(WidgetElement element)
    {
        if (element.Parent is Grid grid && grid._rowMap.TryGetValue(element, out var row))
        {
            return row;
        }
        return 0;
    }

    public static int GetColumn(WidgetElement element)
    {
        if (element.Parent is Grid grid && grid._columnMap.TryGetValue(element, out var col))
        {
            return col;
        }
        return 0;
    }

    public static int GetRowSpan(WidgetElement element)
    {
        if (element.Parent is Grid grid && grid._rowSpanMap.TryGetValue(element, out var span))
        {
            return span;
        }
        return 1;
    }

    public static int GetColumnSpan(WidgetElement element)
    {
        if (element.Parent is Grid grid && grid._columnSpanMap.TryGetValue(element, out var span))
        {
            return span;
        }
        return 1;
    }

    #endregion

    #region 布局方法

    protected override Size MeasureChildren(Size availableSize)
    {
        var rowCount = RowDefinitions.Count;
        var colCount = ColumnDefinitions.Count;

        var rowHeights = new float[rowCount];
        var colWidths = new float[colCount];

        for (var i = 0; i < rowCount; i++)
        {
            if (RowDefinitions[i].Height.Type == GridUnitType.Pixel)
            {
                rowHeights[i] = RowDefinitions[i].Height.Value;
            }
        }

        for (var i = 0; i < colCount; i++)
        {
            if (ColumnDefinitions[i].Width.Type == GridUnitType.Pixel)
            {
                colWidths[i] = ColumnDefinitions[i].Width.Value;
            }
        }

        foreach (var child in Children)
        {
            if (child.Visibility == Visibility.Collapsed)
            {
                continue;
            }

            var row = GetRow(child);
            var col = GetColumn(child);
            var rowSpan = GetRowSpan(child);
            var colSpan = GetColumnSpan(child);

            var childAvailable = new Size(
                col < colCount && ColumnDefinitions[col].Width.Type == GridUnitType.Pixel
                    ? ColumnDefinitions[col].Width.Value
                    : availableSize.Width,
                row < rowCount && RowDefinitions[row].Height.Type == GridUnitType.Pixel
                    ? RowDefinitions[row].Height.Value
                    : availableSize.Height
            );

            child.Measure(childAvailable);

            for (var c = col; c < Math.Min(col + colSpan, colCount); c++)
            {
                if (ColumnDefinitions[c].Width.Type == GridUnitType.Auto)
                {
                    colWidths[c] = Math.Max(colWidths[c], child.DesiredSize.Width / colSpan);
                }
            }

            for (var r = row; r < Math.Min(row + rowSpan, rowCount); r++)
            {
                if (RowDefinitions[r].Height.Type == GridUnitType.Auto)
                {
                    rowHeights[r] = Math.Max(rowHeights[r], child.DesiredSize.Height / rowSpan);
                }
            }
        }

        var usedWidth = colWidths.Sum() + Math.Max(0, colCount - 1) * ColumnSpacing;
        var usedHeight = rowHeights.Sum() + Math.Max(0, rowCount - 1) * RowSpacing;
        var remainingWidth = Math.Max(0, availableSize.Width - usedWidth);
        var remainingHeight = Math.Max(0, availableSize.Height - usedHeight);

        var totalStarWidth = 0f;
        var totalStarHeight = 0f;

        for (var i = 0; i < colCount; i++)
        {
            if (ColumnDefinitions[i].Width.Type == GridUnitType.Star)
            {
                totalStarWidth += ColumnDefinitions[i].Width.Value;
            }
        }

        for (var i = 0; i < rowCount; i++)
        {
            if (RowDefinitions[i].Height.Type == GridUnitType.Star)
            {
                totalStarHeight += RowDefinitions[i].Height.Value;
            }
        }

        for (var i = 0; i < colCount; i++)
        {
            if (ColumnDefinitions[i].Width.Type == GridUnitType.Star && totalStarWidth > 0)
            {
                colWidths[i] = remainingWidth * (ColumnDefinitions[i].Width.Value / totalStarWidth);
            }
        }

        for (var i = 0; i < rowCount; i++)
        {
            if (RowDefinitions[i].Height.Type == GridUnitType.Star && totalStarHeight > 0)
            {
                rowHeights[i] = remainingHeight * (RowDefinitions[i].Height.Value / totalStarHeight);
            }
        }

        _computedColWidths = colWidths;
        _computedRowHeights = rowHeights;

        var totalWidth = colWidths.Sum() + Math.Max(0, colCount - 1) * ColumnSpacing;
        var totalHeight = rowHeights.Sum() + Math.Max(0, rowCount - 1) * RowSpacing;

        return new Size(totalWidth, totalHeight);
    }

    protected override void ArrangeChildren(Rect contentRect)
    {
        if (_computedColWidths == null || _computedRowHeights == null)
        {
            return;
        }

        var rowCount = _computedRowHeights.Length;
        var colCount = _computedColWidths.Length;

        var colX = new float[colCount];
        var rowY = new float[rowCount];

        colX[0] = contentRect.X;
        for (var i = 1; i < colCount; i++)
        {
            colX[i] = colX[i - 1] + _computedColWidths[i - 1] + ColumnSpacing;
        }

        rowY[0] = contentRect.Y;
        for (var i = 1; i < rowCount; i++)
        {
            rowY[i] = rowY[i - 1] + _computedRowHeights[i - 1] + RowSpacing;
        }

        foreach (var child in Children)
        {
            if (child.Visibility == Visibility.Collapsed)
            {
                continue;
            }

            var row = Math.Min(GetRow(child), rowCount - 1);
            var col = Math.Min(GetColumn(child), colCount - 1);
            var rowSpan = Math.Min(GetRowSpan(child), rowCount - row);
            var colSpan = Math.Min(GetColumnSpan(child), colCount - col);

            var x = colX[col];
            var y = rowY[row];
            var width = 0f;
            var height = 0f;

            for (var c = col; c < col + colSpan; c++)
            {
                width += _computedColWidths[c];
                if (c > col)
                {
                    width += ColumnSpacing;
                }
            }

            for (var r = row; r < row + rowSpan; r++)
            {
                height += _computedRowHeights[r];
                if (r > row)
                {
                    height += RowSpacing;
                }
            }

            child.Arrange(new Rect(x, y, width, height));
        }
    }

    #endregion

    #region 私有字段

    private float[]? _computedColWidths;
    private float[]? _computedRowHeights;

    #endregion
}
