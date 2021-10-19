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
                        Console.WriteLine("Drawing String");
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
                //XPen pen = new XPen(XColor.FromArgb(250, 0, 0, 0), 1);
                //gfx.DrawLine(pen, layoutRectangle.TopLeft, layoutRectangle.TopRight);
                //gfx.DrawLine(pen, layoutRectangle.TopRight, layoutRectangle.BottomRight);
                //gfx.DrawLine(pen, layoutRectangle.BottomRight, layoutRectangle.BottomLeft);
                //gfx.DrawLine(pen, layoutRectangle.BottomLeft, layoutRectangle.TopLeft);
                //TextBlockOver(lastSubstring, height);

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
//  private readonly XGraphics _gfx;
//  private string _text;
//  private XFont _font;
//  private double _lineHeight;
//  private double _cyAscent;
//  private double _cyDescent;
//  private double _spaceWidth;
//  private XRect _layoutRectangle;
//  private TextSpacingOptions _textSpacingOptions;
//  private XParagraphAlignment _alignment = XParagraphAlignment.Left;
//  private readonly List<RichTextFormatter.Block> _blocks = new List<RichTextFormatter.Block>();

//  /// <summary>
//  /// Initializes a new instance of the <see cref="T:PdfSharp.Drawing.Layout.RichTextFormatter" /> class.
//  /// </summary>
//  public RichTextFormatter(XGraphics gfx) => this._gfx = gfx != null ? gfx : throw new ArgumentNullException(nameof (gfx));

//  /// <summary>Gets or sets the text.</summary>
//  /// <value>The text.</value>
//  public string Text
//  {
//    get => this._text;
//    set => this._text = value;
//  }

//  /// <summary>Gets or sets the font.</summary>
//  public XFont Font
//  {
//    get => this._font;
//    set
//    {
//      this._font = value != null ? value : throw new ArgumentNullException(nameof (Font));
//      this._lineHeight = this._font.GetHeight();
//      this._cyAscent = this._lineHeight * (double) this._font.CellAscent / (double) this._font.CellSpace;
//      this._cyDescent = this._lineHeight * (double) this._font.CellDescent / (double) this._font.CellSpace;
//      this._spaceWidth = this._gfx.MeasureString("x x", value).Width - this._gfx.MeasureString("xx", value).Width;
//    }
//  }

//  public TextSpacingOptions SpacingOptions { get =>_textSpacingOptions; private set => _textSpacingOptions = value; }

//  /// <summary>Gets or sets the bounding box of the layout.</summary>
//  public XRect LayoutRectangle
//  {
//    get => this._layoutRectangle;
//    set => this._layoutRectangle = value;
//  }

//  /// <summary>Gets or sets the alignment of the text.</summary>
//  public XParagraphAlignment Alignment
//  {
//    get => this._alignment;
//    set => this._alignment = value;
//  }

//  /// <summary>Draws the text.</summary>
//  /// <param name="text">The text to be drawn.</param>
//  /// <param name="font">The font.</param>
//  /// <param name="brush">The text brush.</param>
//  /// <param name="layoutRectangle">The layout rectangle.</param>
//  public void DrawString(string text, XFont font, XBrush brush, XRect layoutRectangle, TextSpacingOptions textSpacingOptions) => this.DrawString(text, font, brush, layoutRectangle, textSpacingOptions, XStringFormats.TopLeft);

//  /// <summary>Draws the text.</summary>
//  /// <param name="text">The text to be drawn.</param>
//  /// <param name="font">The font.</param>
//  /// <param name="brush">The text brush.</param>
//  /// <param name="layoutRectangle">The layout rectangle.</param>
//  public void DrawString(string text, XFont font, XBrush brush, XRect layoutRectangle) => this.DrawString(text, font, brush, layoutRectangle, new TextSpacingOptions(), XStringFormats.TopLeft);

//  /// <summary>Draws the text.</summary>
//  /// <param name="text">The text to be drawn.</param>
//  /// <param name="font">The font.</param>
//  /// <param name="brush">The text brush.</param>
//  /// <param name="layoutRectangle">The layout rectangle.</param>
//  /// <param name="format">The format. Must be <c>XStringFormat.TopLeft</c></param>
//  public void DrawString(
//    string text,
//    XFont font,
//    XBrush brush,
//    XRect layoutRectangle,
//    TextSpacingOptions textSpacingOptions,
//    XStringFormat format)
//  {
//    if (text == null)
//      throw new ArgumentNullException(nameof (text));
//    if (font == null)
//      throw new ArgumentNullException(nameof (font));
//    if (brush == null)
//      throw new ArgumentNullException(nameof (brush));
//    if (format.Alignment != XStringAlignment.Near || format.LineAlignment != XLineAlignment.Near)
//      throw new ArgumentException("Only TopLeft alignment is currently implemented.");
//    this.Text = text;
//    this.Font = font;
//    this.LayoutRectangle = layoutRectangle;
//    this.SpacingOptions = textSpacingOptions;
//    if (text.Length == 0)
//      return;
//    this.CreateBlocks();
//    this.CreateLayout();
//    double x = layoutRectangle.Location.X;
//    double num = layoutRectangle.Location.Y + this._cyAscent;
//    int count = this._blocks.Count;
//    for (int index = 0; index < count; ++index)
//    {
//      RichTextFormatter.Block block = this._blocks[index];
//      if (block.Stop)
//        break;
//      if (block.Type != RichTextFormatter.BlockType.LineBreak)
//        this._gfx.DrawString(block.Text, font, brush, x + block.Location.X, num + block.Location.Y);
//    }
//  }

//  private void CreateBlocks()
//  {
//    this._blocks.Clear();
//    int length1 = this._text.Length;
//    bool flag = false;
//    int startIndex = 0;
//    int length2 = 0;
//    for (int index = 0; index < length1; ++index)
//    {
//      char c = this._text[index];
//      if (c == '\r')
//      {
//        if (index < length1 - 1 && this._text[index + 1] == '\n')
//          ++index;
//        c = '\n';
//      }
//      if (c == '\n')
//      {
//        if (length2 != 0)
//        {
//          string text = this._text.Substring(startIndex, length2);
//          this._blocks.Add(new RichTextFormatter.Block(text, RichTextFormatter.BlockType.Text, this._gfx.MeasureString(text, this._font).Width));
//        }
//        startIndex = index + 1;
//        length2 = 0;
//        this._blocks.Add(new RichTextFormatter.Block(RichTextFormatter.BlockType.LineBreak));
//      }
//      else if (c != ' ' && char.IsWhiteSpace(c))
//      {
//        if (flag)
//        {
//          string text = this._text.Substring(startIndex, length2);
//          this._blocks.Add(new RichTextFormatter.Block(text, RichTextFormatter.BlockType.Text, this._gfx.MeasureString(text, this._font).Width));
//          startIndex = index + 1;
//          length2 = 0;
//        }
//        else
//          ++length2;
//      }
//      else
//      {
//        flag = true;
//        ++length2;
//      }
//    }
//    if (length2 == 0)
//      return;
//    string text1 = this._text.Substring(startIndex, length2);
//    this._blocks.Add(new RichTextFormatter.Block(text1, RichTextFormatter.BlockType.Text, this._gfx.MeasureString(text1, this._font).Width));
//  }

//  private void CreateLayout()
//  {
//    double width1 = this._layoutRectangle.Width;
//    double num1 = this._layoutRectangle.Height - this._cyAscent - this._cyDescent;
//    int num2 = 0;
//    double x = 0.0;
//    double y = 0.0;
//    int count = this._blocks.Count;
//    for (int index = 0; index < count; ++index)
//    {
//      RichTextFormatter.Block block = this._blocks[index];
//      if (block.Type == RichTextFormatter.BlockType.LineBreak)
//      {
//        if (this.Alignment == XParagraphAlignment.Justify)
//          this._blocks[num2].Alignment = XParagraphAlignment.Left;
//        this.AlignLine(num2, index - 1, width1);
//        num2 = index + 1;
//        x = 0.0;
//        y += this._lineHeight;
//        if (y > num1)
//        {
//          block.Stop = true;
//          break;
//        }
//      }
//      else
//      {
//        double width2 = block.Width;
//        if ((x + width2 <= width1 || x == 0.0) && block.Type != RichTextFormatter.BlockType.LineBreak)
//        {
//          block.Location = new XPoint(x, y);
//          x += width2 + this._spaceWidth;
//        }
//        else
//        {
//          this.AlignLine(num2, index - 1, width1);
//          num2 = index;
//          y += this._lineHeight;
//          if (y > num1)
//          {
//            block.Stop = true;
//            break;
//          }
//          block.Location = new XPoint(0.0, y);
//          x = width2 + this._spaceWidth;
//        }
//      }
//    }
//    if (num2 >= count || this.Alignment == XParagraphAlignment.Justify)
//      return;
//    this.AlignLine(num2, count - 1, width1);
//  }

//  /// <summary>Align center, right, or justify.</summary>
//  private void AlignLine(int firstIndex, int lastIndex, double layoutWidth)
//  {
//    XParagraphAlignment alignment = this._blocks[firstIndex].Alignment;
//    if (this._alignment == XParagraphAlignment.Left || alignment == XParagraphAlignment.Left)
//      return;
//    int num1 = lastIndex - firstIndex + 1;
//    if (num1 == 0)
//      return;
//    double num2 = -this._spaceWidth;
//    for (int index = firstIndex; index <= lastIndex; ++index)
//      num2 += this._blocks[index].Width + this._spaceWidth;
//    double width = Math.Max(layoutWidth - num2, 0.0);
//    if (this._alignment != XParagraphAlignment.Justify)
//    {
//      if (this._alignment == XParagraphAlignment.Center)
//        width /= 2.0;
//      for (int index = firstIndex; index <= lastIndex; ++index)
//        this._blocks[index].Location += new XSize(width, 0.0);
//    }
//    else
//    {
//      if (num1 <= 1)
//        return;
//      double num3 = width / (double) (num1 - 1);
//      int index = firstIndex + 1;
//      int num4 = 1;
//      while (index <= lastIndex)
//      {
//        this._blocks[index].Location += new XSize(num3 * (double) num4, 0.0);
//        ++index;
//        ++num4;
//      }
//    }
//  }

//  private enum BlockType
//  {
//    Text,
//    Space,
//    Hyphen,
//    LineBreak,
//    StartTag,
//    EndTag,
//  }

//  /// <summary>Represents a single word.</summary>
//  private class Block
//  {
//    /// <summary>The text represented by this block.</summary>
//    public readonly string Text;
//    /// <summary>The type of the block.</summary>
//    public readonly RichTextFormatter.BlockType Type;
//    /// <summary>The width of the text.</summary>
//    public readonly double Width;
//    /// <summary>
//    /// The location relative to the upper left corner of the layout rectangle.
//    /// </summary>
//    public XPoint Location;
//    /// <summary>The alignment of this line.</summary>
//    public XParagraphAlignment Alignment;
//    /// <summary>
//    /// A flag indicating that this is the last block that fits in the layout rectangle.
//    /// </summary>
//    public bool Stop;

//    /// <summary>
//    /// Initializes a new instance of the <see cref="T:PdfSharp.Drawing.Layout.RichTextFormatter.Block" /> class.
//    /// </summary>
//    /// <param name="text">The text of the block.</param>
//    /// <param name="type">The type of the block.</param>
//    /// <param name="width">The width of the text.</param>
//    public Block(string text, RichTextFormatter.BlockType type, double width)
//    {
//      this.Text = text;
//      this.Type = type;
//      this.Width = width;
//    }

//    /// <summary>
//    /// Initializes a new instance of the <see cref="T:PdfSharp.Drawing.Layout.RichTextFormatter.Block" /> class.
//    /// </summary>
//    /// <param name="type">The type.</param>
//    public Block(RichTextFormatter.BlockType type) => this.Type = type;
//  }
//}

//public struct TextSpacingOptions
//{
//  public double Paragraph;
//  public double Line;
//  public double Word;
//  public double Character;

//  public TextSpacingOptions(double paragraph, double line, double word, double character)
//  {
//    Paragraph = paragraph;
//    Line = line;
//    Word = word;
//    Character = character;
//  }
//}
