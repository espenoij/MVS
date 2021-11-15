using Telerik.Windows.Documents.Fixed.Model.Editing;

namespace HMS_Client
{
    internal class PdfRenderContext
    {
        internal FixedContentEditor drawingSurface;
        internal PdfRenderer facade;
        internal double opacity;

        internal PdfRenderContext(FixedContentEditor drawingSurface, PdfRenderer facade)
        {
            this.drawingSurface = drawingSurface;
            this.facade = facade;
            this.opacity = 1;
        }
    }
}
