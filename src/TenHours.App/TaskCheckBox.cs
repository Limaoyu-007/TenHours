using System.Drawing.Drawing2D;

namespace TenHours;

/// <summary>A real accessible CheckBox with a quiet, compact visual treatment.</summary>
internal sealed class TaskCheckBox : CheckBox
{
    private readonly ContextMenuStrip rowMenu = new();
    internal Action<int>? ScrollWheel;

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        if (ScrollWheel is not null) ScrollWheel(e.Delta);
        else base.OnMouseWheel(e);
    }

    internal TaskCheckBox(Action rename, Action remove)
    {
        AutoSize = false;
        FlatStyle = FlatStyle.Flat;
        Cursor = Cursors.Hand;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        rowMenu.Items.Add("编辑任务…", null, (_, _) => rename());
        rowMenu.Items.Add("删除任务", null, (_, _) => remove());
        ContextMenuStrip = rowMenu;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(BackColor);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        float scale = DeviceDpi / 96f;
        float side = 14 * scale;
        var box = new RectangleF(7 * scale, (Height - side) / 2, side, side);
        using var shape = new GraphicsPath();
        shape.AddRoundedRectangle(box, new SizeF(4 * scale, 4 * scale));
        if (Checked)
        {
            using var fill = new SolidBrush(Color.FromArgb(91, 210, 161));
            e.Graphics.FillPath(fill, shape);
            using var tick = new Pen(Color.FromArgb(20, 35, 31), 1.6f * scale) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            e.Graphics.DrawLines(tick, [new PointF(box.X + 3 * scale, box.Y + 7 * scale),
                new PointF(box.X + 6 * scale, box.Y + 10 * scale), new PointF(box.X + 11 * scale, box.Y + 4 * scale)]);
        }
        else
        {
            using var outline = new Pen(Color.FromArgb(86, 101, 120), scale);
            e.Graphics.DrawPath(outline, shape);
        }
        using var strike = Checked ? new Font(Font, FontStyle.Strikeout) : null;
        var textBounds = new Rectangle((int)(31 * scale), 0, Width - (int)(36 * scale), Height);
        TextRenderer.DrawText(e.Graphics, Text, strike ?? Font, textBounds,
            Checked ? Color.FromArgb(116, 133, 147) : Color.FromArgb(215, 224, 235),
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
        if (Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(e.Graphics, Rectangle.Inflate(ClientRectangle, -2, -2));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) rowMenu.Dispose();
        base.Dispose(disposing);
    }
}
