using System;
using System.Windows;
using System.Windows.Media;
using Telerik.Windows.Documents.Fixed.Model.Editing;

namespace SensorMonitorClient
{
    internal static class TextRenderer
    {
        public static void DrawTextBlock(string text, PdfRenderContext context, Brush foreground, double width, double height, FontFamily fontFamily, double fontSize, FontStyle fontStyle, FontWeight fontWeight)
        {
            if (!string.IsNullOrEmpty(text))
            {
                using (context.drawingSurface.SaveProperties())
                {
                    UIElementRendererBase.SetFill(context, foreground, width, height);
                    SetFontFamily(context.drawingSurface, fontFamily, fontStyle, fontWeight);
                    context.drawingSurface.TextProperties.FontSize = fontSize;

                    Block block = new Block();
                    block.TextProperties.CopyFrom(context.drawingSurface.TextProperties);
                    block.GraphicProperties.CopyFrom(context.drawingSurface.GraphicProperties);
                    string[] textLines = text.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

                    foreach (string textLine in textLines)
                    {
                        block.InsertText(textLine);
                        block.InsertLineBreak();
                    }

                    context.drawingSurface.DrawBlock(block);
                }
            }
        }

        private static void SetFontFamily(Telerik.Windows.Documents.Fixed.Model.Editing.FixedContentEditor drawingSurface, System.Windows.Media.FontFamily fontFamily)
        {
            if (!drawingSurface.TextProperties.TrySetFont(fontFamily))
            {
                throw new System.Exception("Unable to set font. Consider embedding the font.");
            }
        }


        private static void SetFontFamily(
            Telerik.Windows.Documents.Fixed.Model.Editing.FixedContentEditor drawingSurface, 
            System.Windows.Media.FontFamily fontFamily,
            System.Windows.FontStyle fontStyle,
            System.Windows.FontWeight fontWeight)
        {
            if (!drawingSurface.TextProperties.TrySetFont(fontFamily, fontStyle, fontWeight))
            {
                throw new System.Exception("Unable to set font. Consider embedding the font.");
            }
        }
    }
}
