using System;
using System.Collections.Generic;
using PdfSharp.Drawing;
using PdfSharp.Drawing.Layout;
using System.Linq;
using HtmlAgilityPack;

namespace PDFCreator
{
    public class RichTextFormatter
    {
        private XFont _defaultFont;
        private XFont _currentFont;
        private readonly Dictionary<string, Action> _tagDic = new Dictionary<string, Action>();

        public XFont Font 
        { 
            get => _currentFont;
            set
            { 
                _defaultFont = value;
                _currentFont = value;
            } 
        }

        public RichTextFormatter()
        {
            _tagDic["b"] = () => { _currentFont = new XFont(Font.FontFamily.ToString(), Font.Size, XFontStyle.Bold); };
            _tagDic["i"] = () => { _currentFont = new XFont(Font.FontFamily.ToString(), Font.Size, XFontStyle.Italic); };
            _tagDic["#text"] = () => { _currentFont = _defaultFont; };
        }

        public void DrawString(
            string text,
            XGraphics gfx,
            XBrush brush,
            XRect layoutRectangle,
            XParagraphAlignment alignment,
            TextSpacingOptions textSpacingOptions,
            XStringFormat format)
        {
            //Use HtmlAgilityPack to seperate tags
            var doc = new HtmlDocument();
            doc.LoadHtml(text);
            var parsedText = ParseText(doc.DocumentNode.FirstChild, gfx, layoutRectangle, textSpacingOptions);

            for (int i = 0; i < parsedText.Count; i++)
            {
                var currentSentence = parsedText[i];
                currentSentence.width = currentSentence.sentenceParts.Select(x => x.width).Sum();
                double offset = ParagraphAlignemt(layoutRectangle.Width - currentSentence.width, alignment);
                double accumilatedWidth = layoutRectangle.TopLeft.X;
                for (int j = 0; j < currentSentence.sentenceParts.Count; j++)
                {
                    var currentTextBlock = currentSentence.sentenceParts[j];
                    if (_tagDic.ContainsKey(currentTextBlock.tag))
                    {
                        _tagDic[currentTextBlock.tag]();
                        gfx.DrawString(currentTextBlock.text, _currentFont, brush,  accumilatedWidth + offset, layoutRectangle.TopLeft.Y + currentTextBlock.heightPos);
                    }
                    else
                    {
                        Console.WriteLine("Dictionary does not contain the Tag: " + currentTextBlock.tag);
                    }
                    accumilatedWidth += currentTextBlock.width;
                }
            }
        }
        
        private List<SentenceBlock> ParseText(HtmlNode firstNode, XGraphics gfx, XRect layoutRectangle, TextSpacingOptions textSpacingOptions)
        {
            var currentNode = firstNode;
            List<SentenceBlock> parsedText = new List<SentenceBlock>();
            double fontHeight = Font.Size;
            double height = fontHeight;
            double width = 0;

            SentenceBlock currentSentence = new SentenceBlock();
            TextBlock currentTextBlock = new TextBlock();

            double MeasureString(string text, string tag)
            {
                if (_tagDic.ContainsKey(tag))
                {
                    _tagDic[currentNode.Name]();
                }
                else
                {
                    Console.WriteLine("Dictionary does not contain the Tag: " + currentNode.Name);
                }
                return gfx.MeasureString(text, Font).Width;
            }


            void SentenceOver(string text, double heightPos, double extraHeight)
            {
                TextBlockOver(text, heightPos);
                parsedText.Add(currentSentence);
                currentSentence = new SentenceBlock();

                height += Font.GetHeight() + extraHeight;
                width = 0;
            }
            void TextBlockOver(string text, double heightPos)
            {
                currentTextBlock.tag = currentNode.Name;
                currentTextBlock.text += text;
                currentTextBlock.heightPos = heightPos;

                currentTextBlock.width = MeasureString(currentTextBlock.text, currentTextBlock.tag);
                currentSentence.sentenceParts.Add(currentTextBlock);

                width += currentTextBlock.width;

                currentTextBlock = new TextBlock();
            }

            
            //Go through all the nodes. since this is rich text and not html, i can just go through siblings
            while (currentNode != null)
            {
                int substringStartIndex = 0;
                int substringWordIndex = 0;
                string currentText = currentNode.InnerText;
                Console.WriteLine($"current node : {currentText}");
                for (int i = 0; i < currentText.Length; i++)
                {
                    char currentChar = currentText[i];
                    if ( currentChar == '\r')
                    {
                        //Sometimes linebreak is \r\n annoyingly enough.
                        if (i+1 != currentText.Length && currentText[i+1] == '\n')
                        {
                            currentText = currentText.Remove(i, 1);
                        }
                        currentChar = '\n';
                    }
                    //deal with linebreak
                    if (currentChar == '\n')
                    {
                        //The sentence is over.
                        var stringIndexDiff = i - substringStartIndex;
                        string substring = currentText.Substring(substringStartIndex, stringIndexDiff);
                        SentenceOver(substring, height, textSpacingOptions.Paragraph);

                        //Clean up
                        substringStartIndex += stringIndexDiff+1;
                    }
                    else if (currentChar != ' ' && char.IsWhiteSpace(currentChar))
                    {
                        string substring = currentText.Substring(substringStartIndex, i - substringStartIndex);
                        

                        double stringLength = MeasureString(substring, currentNode.Name);

                        if (stringLength + width <= layoutRectangle.Width)
                        {
                            substringWordIndex = i+1;
                        }
                        else
                        {
                            var stringIndexDiff = substringWordIndex - substringStartIndex;
                            SentenceOver(currentText.Substring(substringStartIndex, stringIndexDiff), height, textSpacingOptions.Line);
                            substringStartIndex += stringIndexDiff;
                        }
                    }
                }
                //Check if last word also fit:
                string lastSubstring = currentText.Substring(substringStartIndex, currentText.Length - substringStartIndex);

                double lastStringLength = MeasureString(lastSubstring, currentNode.Name);

                if (lastStringLength + width <= layoutRectangle.Width)
                {
                    TextBlockOver(lastSubstring, height);
                }
                else
                {
                    lastSubstring = currentText.Substring(substringStartIndex, substringWordIndex - substringStartIndex);
                    SentenceOver(lastSubstring, height, textSpacingOptions.Paragraph);
                    TextBlockOver(currentText.Substring(substringWordIndex, currentText.Length - substringWordIndex), height);
                }

                currentNode = currentNode.NextSibling;
            }
            parsedText.Add(currentSentence);
            return parsedText;
        }
        
        private double ParagraphAlignemt(double diff, XParagraphAlignment alignment)
        {
            double x = 0;
            switch (alignment)
            {
                case XParagraphAlignment.Default:
                    break;
                case XParagraphAlignment.Left:
                    break;
                case XParagraphAlignment.Center:
                    x = diff / 2;
                    break;
                case XParagraphAlignment.Right:
                    x = diff;
                    break;
                case XParagraphAlignment.Justify:
                    break;
                default:
                    break;
            }
            return x;
        }
    }
    

    public class SentenceBlock
    {
        public List<TextBlock> sentenceParts;
        public double width;

        public SentenceBlock()
        {
            sentenceParts = new List<TextBlock>();
            width = 0;
        }
    }
    public struct TextBlock
    {
        public string text;
        public double heightPos;
        public double width;
        public string tag;

    }
    public struct TextSpacingOptions
    {
        public double Paragraph;
        public double Line;
        public double Word;
        public double Character;

        public TextSpacingOptions(double paragraph, double line, double word, double character)
        {
            Paragraph = paragraph;
            Line = line;
            Word = word;
            Character = character;
        }
    }
}