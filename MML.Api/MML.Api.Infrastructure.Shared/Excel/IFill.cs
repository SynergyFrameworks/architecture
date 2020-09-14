using System.Drawing;

namespace MML.Enterprise.Excel
{
    public interface IFill
    {
        FillTypes FillType { get; set; }
        Color Color { get; set; }
    }

    public enum FillTypes
    {
        None,
        Solid,
        DarkGray,
        MediumGray,
        LightGray,
        Gray125,
        Gray0625,
        DarkVertical,
        DarkHorizontal,
        DarkDown,
        DarkUp,
        DarkGrid,
        DarkTrellis,
        LightVertical,
        LightHorizontal,
        LightDown,
        LightUp,
        LightGrid,
        LightTrellis,  
    }
}
