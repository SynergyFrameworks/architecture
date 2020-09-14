namespace MML.Enterprise.Excel
{
    public interface IHeaderFooter
    {
        IHeaderFooterText Header { get;}
        IHeaderFooterText Footer { get;}

        bool AlignWithMargins { get; set; }        
    }
}
