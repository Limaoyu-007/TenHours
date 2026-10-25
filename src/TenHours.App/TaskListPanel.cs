using System.Drawing.Drawing2D;

namespace TenHours;

/// <summary>Event-driven scrolling, without a native light-colored scrollbar.</summary>
internal sealed class TaskListPanel : Panel
{
    private int rowHeight = 28;
    private int dragY;
    private int dragOffset;
    internal int ScrollOffset { get; private set; }
    private int ContentHeight => Controls.Count * rowHeight;
    private int Maximum => Math.Max(0, ContentHeight - ClientSize.Height);

    internal TaskListPanel()
    {
        AutoScroll = false;
        DoubleBuffered = true;
        SetStyle(ControlStyles.ResizeRedraw, true);
    }

    internal void Configure(int height, int offset)
    {
        rowHeight = height;
        ScrollTo(offset);
    }

    internal void ScrollTo(int offset)
    {
        ScrollOffset = Math.Clamp(offset, 0, Maximum);
        for (int i = 0; i < Controls.Count; i++) Controls[i].Top = i * rowHeight - ScrollOffset;
        Invalidate();
    }

    internal void ScrollWheel(int delta) => ScrollTo(ScrollOffset - delta / 120 * rowHeight * 3);

    internal void RevealRow(Control row)
    {
        if (row.Top < 0) ScrollTo(ScrollOffset + row.Top);
        else if (row.Bottom > Height) ScrollTo(ScrollOffset + row.Bottom - Height);
    }

    protected override void OnMouseWheel(MouseEventArgs e) => ScrollWheel(e.Delta);

    private RectangleF Thumb
    {
        get
        {
            float width = Math.Max(3, DeviceDpi / 24f);
            float height = Math.Max(rowHeight, Height * (float)Height / Math.Max(1, ContentHeight));
            return new RectangleF(Width - width - 1, Maximum == 0 ? 0 : (Height - height) * ScrollOffset / Maximum, width, height);
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (Maximum == 0) return;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var brush = new SolidBrush(Capture ? Color.FromArgb(117, 140, 161) : Color.FromArgb(65, 81, 100));
        var thumb = Thumb;
        e.Graphics.FillRoundedRectangle(brush, thumb, new SizeF(thumb.Width / 2, thumb.Width / 2));
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left || Maximum == 0 || e.X < Width - 12 * DeviceDpi / 96f) return;
        var thumb = Thumb;
        if (e.Y < thumb.Top || e.Y > thumb.Bottom) ScrollTo(ScrollOffset + (e.Y < thumb.Top ? -Height : Height));
        dragY = e.Y;
        dragOffset = ScrollOffset;
        Capture = true;
        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (Capture) ScrollTo(dragOffset + (int)((e.Y - dragY) * Maximum / Math.Max(1, Height - Thumb.Height)));
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        Capture = false;
        Invalidate();
    }
}
