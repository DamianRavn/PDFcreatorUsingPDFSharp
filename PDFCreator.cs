using System;
using System.Threading;
using System.Collections.Generic;

using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Drawing.Layout;

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

        private static void Test()
        {
            string testName = "test2";
            pdfCreator.CreateDocument(testName, 2, 9100, 9750);
            pdfCreator.DrawImage(testName,"E:/Libraries/Documents/GitHub/DamianRavn_CV_/DamianRavn_CV_/Assets/Images/Circle.png", 0, 1, 1, 500, 500, 500, 500);
            //pdfCreator.DrawString(testName, "halø meæ då", 1, "LiberationSans SDF", 0, 0, 60, 400, 400, 50, 50);
            pdfCreator.SaveDocument(testName);
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

            documents.Add(name, documentClass);
        }

        public void DrawString(string name, string RTFtext, int page, string fontFamily, float fontSize, float pivotX, float pivotY, float sizeX, float sizeY, float posX, float posY)
        {
            //XPdfFontOptions options = new XPdfFontOptions(PdfFontEncoding.Unicode);

            float x = SetToPivot(posX, sizeX, pivotX);
            float y = SetToPivot(posY, sizeY, pivotY);

            XFont font = new XFont(fontFamily, fontSize, XFontStyle.Regular);
            XRect rect = new XRect(x, y, sizeX, sizeY);
            XTextFormatter xTextFormatter = new XTextFormatter(documents[name].graphicsList[page]);
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
            var newHeight = (int)(image.PixelHeight * ratio);

            documents[name].graphicsList[page].DrawImage(image, x, y, newWidth, newHeight);
        }

        public void SaveDocument(string name)
        {
            documents[name].document.Save($"E:/Libraries/Desktop/{name}.pdf");
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
