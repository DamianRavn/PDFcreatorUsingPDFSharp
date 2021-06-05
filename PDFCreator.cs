using System;
using System.Threading;
using System.Collections.Generic;

using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Drawing.Layout;
using System.IO;
using System.Text.RegularExpressions;


//TODO: Text allignment, text boldness
namespace PDFCreator
{
    class PDFCreator
    {
        private static bool isRunning = false;
        public static PDFSharpCreation pdfCreator;

        static void Main()
        {
            Console.Title = "Game Server";
            isRunning = true;
            pdfCreator = new PDFSharpCreation();
            //Test();

            Thread mainThread = new Thread(new ThreadStart(MainThread));
            mainThread.Start();

            Server.Start(1, 26950);
        }

        private static void MainThread()
        {
            Console.WriteLine($"Main thread started. Running at {Constants.TICKS_PER_SEC} ticks per second.");
            DateTime _nextLoop = DateTime.Now;

            while (isRunning)
            {
                while (_nextLoop < DateTime.Now)
                {
                    GameLogic.Update();

                    _nextLoop = _nextLoop.AddMilliseconds(Constants.MS_PER_TICK);

                    if (_nextLoop > DateTime.Now)
                    {
                        Thread.Sleep(_nextLoop - DateTime.Now);
                    }
                }
            }
        }
    }

    class PDFSharpCreation
    {
        Dictionary<string, PDFHolder> documents = new Dictionary<string, PDFHolder>();


        public void CreateDocument(string name, int pageAmount, float width, float height)
        {
            PDFHolder documentClass = new PDFHolder();

            // Create an empty page
            for (int i = 0; i < pageAmount; i++)
            {
                PdfPage page = documentClass.document.AddPage();
                page.Width = width;
                page.Height = height;

                documentClass.graphicsList.Add(XGraphics.FromPdfPage(page));
            }

            if (!documents.ContainsKey(name))
            {
                documents.Add(name, documentClass);
            }
            else
            {
                documents[name] = documentClass;
            }
        }

        public void Reset()
        {
            documents.Clear();
        }

        public void DrawString(string name, string RTFtext, int page, string fontFamily, float fontSize, int alignment, float pivotX, float pivotY, float sizeX, float sizeY, float posX, float posY)
        {
            //XPdfFontOptions options = new XPdfFontOptions(PdfFontEncoding.Unicode);

            float x = SetToPivot(posX, sizeX, pivotX);
            float y = SetToPivot(posY, sizeY, pivotY);

            XFont font = new XFont(fontFamily, fontSize, XFontStyle.Regular);
            XRect rect = new XRect(x, y, sizeX, sizeY);
            XTextFormatter xTextFormatter = new XTextFormatter(documents[name].graphicsList[page]); //The textformatter makes sure the text stays in the rect
            xTextFormatter.Alignment = (XParagraphAlignment)alignment;
            xTextFormatter.DrawString(RTFtext, font, XBrushes.Black,
                  rect,
                  XStringFormats.TopLeft);
        }
        public void DrawImage(string name, string path, int page, float pivotX, float pivotY, float sizeX, float sizeY, float posX, float posY)
        {
            float x = SetToPivot(posX, sizeX, pivotX);
            float y = SetToPivot(posY, sizeY, pivotY);
            XImage image = XImage.FromFile(path);

            var ratioX = (double)sizeX / image.PixelWidth;
            var ratioY = (double)sizeY / image.PixelHeight;
            var ratio = Math.Min(ratioX, ratioY);

            var newWidth = (int)(image.PixelWidth * ratio);
            var newHeight = (int)(image.PixelHeight * ratio); //Preserves the aspect ratio

            documents[name].graphicsList[page].DrawImage(image, x, y, newWidth, newHeight);
        }

        public void SaveDocument(string path, string name)
        {
            string fullpath = $"{path}/{name}.pdf";

             documents[name].document.Save(fullpath);
             Console.WriteLine($"Document saved to {fullpath}");
        }

        private float SetToPivot(float pos, float size, float pivot)
        {
            //top left is 0,0. bottom right is 1,1
            return pos - (size * pivot);
        }

        public class PDFHolder
        {
            public PdfDocument document = new PdfDocument();
            public List<XGraphics> graphicsList = new List<XGraphics>();
        }
    }
}
