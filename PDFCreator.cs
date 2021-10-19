using System;
using System.Threading;
using System.Collections.Generic;

using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Drawing.Layout;
using System.IO;
using System.Text.RegularExpressions;
using PdfSharp;


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
            //pdfCreator.CreateDocument("e", 1, 0, 0);
            //string test = "Hello. This is <b > bold aight </b>\n This is <i> Italiccc!</i> \r\n Yep, that's just < how it is.>";
            //pdfCreator.DrawRTFTagString("e", test, 0, "Calibri", 16, 1, 1, 10, 10, 0, 0, 0.2f, 0.2f, 0.2f, 0.2f);
            Thread mainThread = new Thread(new ThreadStart(MainThread));
            mainThread.Start();

            Server.Start(1, 5050);
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
        RichTextFormatter textFormatter; //Easier to cache

        public void CreateDocument(string name, int pageAmount, double width, double height)
        {
            PDFHolder documentClass = new PDFHolder();

            // Create an empty page
            for (int i = 0; i < pageAmount; i++)
            {
                PdfPage page = documentClass.document.AddPage();
                page.Size = PageSize.A4;

                documentClass.graphicsList.Add(XGraphics.FromPdfPage(page));
            }

            textFormatter = new RichTextFormatter();
            documents[name] = documentClass;
        }

        public void Reset()
        {
            documents.Clear();
        }

        public void DrawRTFTagString(string name, string RTFtext, int page, string fontFamily, double fontSize, int fontStyle, int alignment, double lineSpace, double paragraphSpace, double pivotX, double pivotY, double percentageSizeX, double percentageSizeY, double percentagePosX, double percentagePosY)
        {
            XSize pageSize = documents[name].graphicsList[page].PageSize;
            double posX = percentagePosX * pageSize.Width;
            double posY = percentagePosY * pageSize.Height;
            double sizeX = percentageSizeX * pageSize.Width;
            double sizeY = percentageSizeY * pageSize.Height;
            
            double x = SetToPivot(posX, sizeX, pivotX);
            double y = SetToPivot(posY, sizeY, pivotY);

            XFont font = new XFont(fontFamily, fontSize, (XFontStyle)fontStyle);
            XRect rect = new XRect(x, y, sizeX, sizeY);
            TextSpacingOptions spacingoptions = new TextSpacingOptions(paragraphSpace, lineSpace, 0f, 0f);
            textFormatter.Font = font; //The textformatter makes sure the text stays in the rect
            textFormatter.DrawString(RTFtext, documents[name].graphicsList[page], XBrushes.Black,
                  rect,
                  (XParagraphAlignment)alignment,
                  spacingoptions,
                  XStringFormats.TopLeft);
        }
        public void DrawImage(string name, string path, int page, double pivotX, double pivotY, double percentageSizeX, double percentageSizeY, double percentagePosX, double percentagePosY)
        {
            XSize pageSize = documents[name].graphicsList[page].PageSize;
            double posX = percentagePosX * pageSize.Width;
            double posY = percentagePosY * pageSize.Height;
            double sizeX = percentageSizeX * pageSize.Width;
            double sizeY = percentageSizeY * pageSize.Height;
            double x = SetToPivot(posX, sizeX, pivotX);
            double y = SetToPivot(posY, sizeY, pivotY);
            XImage image = XImage.FromFile(path);

            var ratioX = sizeX / image.PixelWidth;
            var ratioY = sizeY / image.PixelHeight;
            var ratio = Math.Min(ratioX, ratioY);

            var newWidth = image.PixelWidth * ratio;
            var newHeight = image.PixelHeight * ratio; //Preserves the aspect ratio
            var newX = x + (sizeX - newWidth)/2;
            var newY = y + (sizeY - newHeight)/2; //Have to adjust because making it smaller fucks with pos
            
            documents[name].graphicsList[page].DrawImage(image, newX, newY, newWidth, newHeight);
        }

        public void SaveDocument(string path, string name)
        {
            string fullpath = $"{path}/{name}.pdf";

            try
            {
                documents[name].document.Save(fullpath);
                Console.WriteLine($"Document saved to {fullpath}");
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Could not save: {exception}");
            }
             
        }

        private double SetToPivot(double pos, double size, double pivot)
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
