namespace MML.Enterprise.Excel
{
    public interface IDrawing
    {
        void SetPosition(int top, int left);
        void SetPosition(int row, int rowOffsetPixels, int column, int columnOffsetPixels);
        void SetSize(int percent);
        void SetSize(int pixelWidth, int pixelHeight);
    }
}
