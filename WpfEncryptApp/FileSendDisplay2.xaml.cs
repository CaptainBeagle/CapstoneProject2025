using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Spreadsheet;
using MySql.Data.MySqlClient;
using MySqlX.XDevAPI.Common;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;
using UglyToad.PdfPig.DocumentLayoutAnalysis.ReadingOrderDetector;
using PageSize = DocumentFormat.OpenXml.Wordprocessing.PageSize;
using Style = DocumentFormat.OpenXml.Wordprocessing.Style;
using Run = DocumentFormat.OpenXml.Wordprocessing.Style;
using SixLabors.ImageSharp;
using System.Collections;


namespace WpfEncryptApp
{
    /// <summary>
    /// Interaction logic for FileSendDisplay.xaml
    /// </summary>
    public partial class FileSendDisplay : Window
    {
        private static string dataContent;
        private static string Userid;
        public static string Data
        {
            get { return dataContent; }
            set { dataContent = value; }
        }
        public static string uID
        {
            get { return Userid; } 
            set { Userid = value; }
        }
        //extracts text from docx files
        public string ExtractTextFromDocx(string Path)
        {
            if (!File.Exists(Path))
            {
                Console.WriteLine($"Error: The file was not found at path: {Path}");
                return string.Empty;
            }

            try
            {
                using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(Path, false))
                {
                    var mainPart = wordDoc.MainDocumentPart;
                    if (mainPart?.Document?.Body == null)
                    {
                        return string.Empty;
                    }

                    //default Doc dimensions
                    var body = mainPart.Document.Body;
                    uint pageWidthTwips = 12240;
                    uint leftMarginTwips = 1440;
                    uint rightMarginTwips = 1440;
                    const int TwipsPerCharacter = 120;

                    var sectionProperties = body.GetFirstChild<SectionProperties>();
                    if (sectionProperties != null)
                    {
                        var pageSize = sectionProperties.GetFirstChild<PageSize>();
                        var pageMargin = sectionProperties.GetFirstChild<PageMargin>();
                        if (pageSize?.Width?.Value != null)
                        {
                            pageWidthTwips = pageSize.Width.Value;
                        }
                        if (pageMargin?.Left?.Value != null)
                        {
                            leftMarginTwips = pageMargin.Left.Value;
                        }
                        if (pageMargin?.Right?.Value != null)
                        {
                            rightMarginTwips = pageMargin.Right.Value;
                        }
                    }
                    uint textWidthTwips = pageWidthTwips - leftMarginTwips - rightMarginTwips;
                    int availableSpace = (int)(textWidthTwips / TwipsPerCharacter);


                    string normalStyleJustification = null;
                    var stylePart = mainPart.StyleDefinitionsPart;
                    if (stylePart?.Styles != null)
                    {
                        var normalStyle = stylePart.Styles.Elements<Style>().FirstOrDefault(s => s.StyleId == "Normal");
                        if (normalStyle?.StyleParagraphProperties?.Justification != null)
                        {
                            normalStyleJustification = normalStyle.StyleParagraphProperties.Justification.Val.ToString();
                        }
                    }

                    var listCounters = new Dictionary<string, int>();
                    bool isprevparalist = false;
                    int? prevnumId = null;
                    int? prevlvl = null;

                    //defining the paragraph's alignment
                    StringBuilder textBuilder = new StringBuilder();
                    foreach (DocumentFormat.OpenXml.Wordprocessing.Paragraph paragraph in mainPart.Document.Body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>())
                    {
                        string text = paragraph.InnerText;
                        string alignment = normalStyleJustification ?? "left";
                        string prefix = string.Empty;
                        bool islist = false;

                        DocumentFormat.OpenXml.Wordprocessing.ParagraphProperties paraProperties = paragraph.ParagraphProperties;
                        if (paraProperties != null)
                        {
                            if (paraProperties.Justification != null)
                            {
                                alignment = paraProperties.Justification.Val.ToString();
                            }
                            else if (paraProperties.ParagraphStyleId != null)
                            {
                                string styleId = paraProperties.ParagraphStyleId.Val.Value;
                                if (stylePart?.Styles != null)
                                {
                                    var paraStyle = stylePart.Styles.Elements<Style>().FirstOrDefault(s => s.StyleId == styleId);
                                    if (paraStyle?.StyleParagraphProperties?.Justification != null)
                                    {
                                        alignment = paraStyle.StyleParagraphProperties.Justification.Val.ToString();
                                    }
                                }
                            }
                        }
                        

                        //extract list labels (if any)
                        var numberingPart = mainPart.NumberingDefinitionsPart;
                        NumberingProperties numprop = null;

                        if (paraProperties != null)
                        {
                            numprop = paraProperties.GetFirstChild<NumberingProperties>();
                        }
                        

                        if (numprop != null && paraProperties.ParagraphStyleId != null)
                        {
                            string styleid = paraProperties.ParagraphStyleId.Val.Value;
                            if (stylePart?.Styles != null)
                            {
                                NumberingId stylenumid = null;
                                NumberingLevelReference stylelvl = null;
                                var currentstyle = stylePart.Styles.Elements<Style>().FirstOrDefault(s => s.StyleId == styleid);

                                while (currentstyle != null)
                                {
                                    if (currentstyle?.StyleParagraphProperties?.NumberingProperties != null)
                                    {
                                        stylenumid = currentstyle.StyleParagraphProperties.NumberingProperties.GetFirstChild<NumberingId>();
                                        stylelvl = currentstyle.StyleParagraphProperties.NumberingProperties.GetFirstChild<NumberingLevelReference>();
                                        break;
                                    }
                                    else
                                    {
                                        currentstyle = null;
                                    }
                                }

                                if (stylenumid != null && stylelvl != null)
                                {
                                    numprop = new NumberingProperties (stylenumid, stylelvl);
                                }
                            }
                        }

                        if (numprop != null && numberingPart != null)
                        {
                            var numId = numprop.GetFirstChild<NumberingId>();
                            var lvl = numprop.GetFirstChild<NumberingLevelReference>();
                            islist = numId != null && lvl != null;

                            if (islist)
                            {
                                int numidval = numId.Val.Value;
                                int lvlval = lvl.Val.Value;
                                string counterkey = $"{numidval}_{lvlval}";

                                if (!isprevparalist)
                                {
                                    for (int i = lvlval; i < 9; i++)
                                    {
                                        listCounters[$"{numidval}_{i}"] = 0;
                                    }
                                    listCounters[counterkey] = 1;
                                }
                                else
                                {
                                   if (listCounters.ContainsKey(counterkey))
                                   {
                                        listCounters[counterkey]++;
                                   }
                                   else
                                   {
                                        listCounters[counterkey] = 1;
                                   }
                                }

                                var numinst = numberingPart.Numbering.Elements<NumberingInstance>().FirstOrDefault(n => n.NumberID?.Value == numId.Val?.Value);
                                if (numinst?.AbstractNumId?.Val != null)
                                {
                                    var abstractnum = numberingPart.Numbering.Elements<AbstractNum>().FirstOrDefault(a => a.AbstractNumberId?.Value == numinst.AbstractNumId?.Val?.Value);
                                    var level = abstractnum?.Elements<DocumentFormat.OpenXml.Wordprocessing.Level>().FirstOrDefault(l => l.LevelIndex?.Value == lvl.Val.Value);

                                    if (level?.LevelText?.Val != null)
                                    {
                                        string listformat = level.LevelText.Val.Value;

                                        //checking if list is bulletpoint or numbered
                                        if (level.NumberingFormat?.Val == NumberFormatValues.Bullet)
                                        {
                                            prefix = listformat;
                                        }
                                        else if (level.NumberingFormat?.Val == NumberFormatValues.Decimal || level.NumberingFormat?.Val == NumberFormatValues.LowerLetter)
                                        {
                                            prefix = listCounters[counterkey] + ". ";
                                        }
                                    }
                                }
                            }
                        }
                        isprevparalist = islist;

                        //format and append the text
                        string propertext = prefix + text;
                        switch (alignment.ToLower())
                        {
                            case "right":
                                textBuilder.Append(propertext.PadLeft(availableSpace));
                                break;
                            case "center":
                                int padding = availableSpace - text.Length;
                                if (padding > 0)
                                {
                                    int halfpadding = padding / 2;
                                    textBuilder.Append(new string(' ', halfpadding) + propertext + new string(' ', padding - halfpadding));
                                }
                                else
                                {
                                    textBuilder.Append(propertext);
                                }
                                break;
                            default:
                                textBuilder.Append(propertext.PadRight(availableSpace));
                                break;
                        }
                        textBuilder.AppendLine();
                    }

                    //add logic to extract images (only run if no content in textBuilder.ToString()
                    if(string.IsNullOrWhiteSpace(textBuilder.ToString()))
                    {
                        var docimgs = new List<byte[]>();
                        var imgs = mainPart.Document.Descendants<DocumentFormat.OpenXml.Drawing.Wordprocessing.Inline>();

                        foreach (var img in imgs)
                        {
                            Blip blip = img.Descendants<Blip>().FirstOrDefault();

                            if(blip.Embed?.Value != null)
                            {
                                string embedID = blip.Embed.Value;

                                ImagePart imgpart = (ImagePart)mainPart.GetPartById(embedID);

                                if(imgpart != null)
                                {
                                    using(Stream stream = imgpart.GetStream())
                                    {
                                        docimgs.Add(ReadStreamFully(stream));
                                    }
                                }
                            }
                        }
                        foreach (byte[] imagebytes in docimgs)
                        {
                            var image = new BitmapImage();
                            using (var ms = new MemoryStream(imagebytes))
                            {
                                image.BeginInit();
                                image.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                                image.StreamSource = ms;
                                image.EndInit();
                                image.Freeze();
                            }
                            var imagecontrol = new System.Windows.Controls.Image();
                            imagecontrol.Source = image;
                            DataHolder.Children.Add(imagecontrol);
                        }
                    }
                    return textBuilder.ToString();
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
                return string.Empty;
            }
        }

        private static byte[] ReadStreamFully(Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }

        //extracts text from xlsx files
        public static string ExtractTextFromExcel(string Path)
        {
            if(!File.Exists(Path))
            {
                MessageBox.Show("File not found at path: " + Path);
                return string.Empty;
            }

            StringBuilder result = new StringBuilder();
            try
            {
                using (SpreadsheetDocument spreadsheet = SpreadsheetDocument.Open(Path, false))
                {
                    //preparing parts of the spreadsheet for display
                    
                    WorkbookPart WBPart = spreadsheet.WorkbookPart;
                    if (WBPart == null)
                    {
                        MessageBox.Show("Trouble retrieving Workbook.");
                        return string.Empty;
                    }
                    SharedStringTablePart STPart = WBPart.GetPartsOfType<SharedStringTablePart>().FirstOrDefault();
                    SharedStringTable StringTable = STPart?.SharedStringTable;

                    //getting the data in the sheet
                    foreach (Sheet sheet in WBPart.Workbook.Descendants<Sheet>()) 
                    {
                        WorksheetPart WSheetPart = (WorksheetPart)WBPart.GetPartById(sheet.Id);
                        Worksheet worksheet = WSheetPart.Worksheet;
                        SheetData SData = worksheet.GetFirstChild<SheetData>();

                        foreach (Row r in SData.Elements<Row>())
                        {
                            bool firstcell = true;
                            int currentcolumnindex = 0;
                            foreach (Cell c in r.Elements<Cell>())
                            {
                                int cellcolumnindex = 0;
                                if (!firstcell)
                                {
                                    result.Append('\t');
                                }
                                if (string.IsNullOrEmpty(c.CellReference))
                                {
                                    cellcolumnindex = -1;
                                }
                                
                                string columnname = "";
                                foreach (char a in c.CellReference.ToString())
                                {
                                    if (char.IsLetter(a))
                                    {
                                        columnname += a;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }

                                int columnindex = 0;
                                int power = 1;

                                for (int i = columnname.Length - 1; i >= 0; i--)
                                {
                                    columnindex += (columnname[i] - 'A' + 1) * power;
                                    power *= 26;
                                }

                                cellcolumnindex = columnindex + 1;

                                while (currentcolumnindex < cellcolumnindex)
                                {
                                    result.Append('\t');
                                    currentcolumnindex++;
                                }
                                result.Append(GetCellValue(c, StringTable));
                                currentcolumnindex = cellcolumnindex + 1;
                                firstcell = false;
                            }
                            result.AppendLine();    //Separating rows of data
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("An error occurred while processing the document: " + ex.Message);
                return string.Empty;
            }
            return result.ToString();
        }

        private static string GetCellValue(Cell cell, SharedStringTable stringtable)
        {
            if (cell.CellValue == null)
            {
                return string.Empty;
            }

            string val = cell.CellValue.InnerText;

            if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
            {
                if (stringtable != null)
                {
                    return stringtable.ElementAt(int.Parse(val)).InnerText;
                }
                return string.Empty;
            }
            else
            {
                return val;
            }
        }

        //Extracts text from pdf files
        public string ExtractTextFromPdf(string path)
        {   
            if (!File.Exists(path))
            {
                Console.WriteLine($"Error: The file was not found at path: {path}");
                return string.Empty;
            }

            StringBuilder result = new StringBuilder();

            try
            {
                const double ToleranceMulti = 2.86;

                const double MaxGapToleranceMultiplier = 2.0;

                using (PdfDocument Pdf = PdfDocument.Open(path))
                {
                    foreach (var page in Pdf.GetPages())
                    {
                        var letters = page.Letters;

                        var wordExtractorOptions = new NearestNeighbourWordExtractor.NearestNeighbourWordExtractorOptions()
                        {
                            Filter = (pivot, candidate) =>
                            {
                                if (string.IsNullOrWhiteSpace(candidate.Value))
                                {
                                    return false;
                                }

                                //check for height difference
                                var maxHeight = Math.Max(pivot.PointSize, candidate.PointSize);
                                var minHeight = Math.Min(pivot.PointSize, candidate.PointSize);
                                if (minHeight != 0 && maxHeight / minHeight > 1.5)
                                {
                                    return false;
                                }

                                //check for colour difference
                                var pivotRgb = pivot.Color.ToRGBValues();
                                var candidateRgb = candidate.Color.ToRGBValues();
                                if (!pivotRgb.Equals(candidateRgb))
                                {
                                    return false;
                                }

                                return true;
                            }
                        };
                        var wordExtractor = new NearestNeighbourWordExtractor(wordExtractorOptions);
                        var words = wordExtractor.GetWords(letters);

                        //finding the average length of the space character
                        var spaces = letters.Where(l => l.Value == " " && l.Width > 0).ToList();
                        double avgspace = 0;
                        if (spaces.Any())
                        {
                            avgspace = spaces.Average(l => l.Width);
                        }
                        else
                        {
                            var printletters = letters.Where(l => !string.IsNullOrWhiteSpace(l.Value) && l.Width > 0);
                            if (printletters.Any())
                            {
                                avgspace = printletters.Average(l => l.Width) * 0.5;
                            }
                            else
                            {
                                avgspace = 6;
                            }
                        }

                        //Calculate the median height for all words.
                        var allMedians = words
                            .Select(w => (w.BoundingBox.Top + w.BoundingBox.Bottom) / 2)
                            .OrderByDescending(m => m)
                            .ToList();

                        //Find the vertical difference (gap) between consecutive lines.
                        var lineGaps = new List<double>();
                        for (int i = 0; i < allMedians.Count - 1; i++)
                        {
                            //Only calculate gap if the difference is significant (i.e., not words on the same line)
                            double gap = allMedians[i] - allMedians[i + 1];
                            if (gap > 1.0)
                            {
                                lineGaps.Add(gap);
                            }
                        }

                        //group the gaps
                        var dominantLineHeightGroup = lineGaps
                            .GroupBy(g => Math.Round(g / 2.0) * 2.0)
                            .OrderByDescending(g => g.Count())
                            .FirstOrDefault();

                        //Use the key of the largest group, or a safe default like 12.0m (a common PDF font size/line height)
                        double fixedLineHeight = dominantLineHeightGroup?.Key ?? 12.0;

                        //Sanity check: ensure it's a positive number
                        if (fixedLineHeight < 5.0) fixedLineHeight = 12.0;

                        
                        var textWords = new List<Word>();
                        var underscoreWords = new List<Word>();

                        foreach (var word in words)
                        {
                            if (word.Text.Length > 2 && word.Text.All(c => c == '_' || c == '-'))
                            {
                                underscoreWords.Add(word);
                            }
                            else
                            {
                                textWords.Add(word);
                            }
                        }

                        //align normal words by position of average median line of words
                        var textWordsWithMedian = textWords
                            .Select(w => new
                            {
                                Word = w,
                                MedianY = (w.BoundingBox.Top + w.BoundingBox.Bottom) / 2
                            })
                            .ToList();

                        var cleanLines = textWordsWithMedian
                            .GroupBy(w => Math.Round(w.MedianY / fixedLineHeight) * fixedLineHeight)
                            .OrderByDescending(g => g.Key)
                            .ToDictionary(g => g.Key, g => g.ToList());

                        //snap underscores to nearest line
                        foreach (var uWord in underscoreWords)
                        {
                            var uMedianY = (uWord.BoundingBox.Top + uWord.BoundingBox.Bottom) / 2;

                            var nearestLineKey = cleanLines.Keys
                                .Where(key => key > uMedianY)
                                .OrderBy(key => key - uMedianY)
                                .FirstOrDefault();

                            if (cleanLines.ContainsKey(nearestLineKey))
                            {
                                cleanLines[nearestLineKey].Add(new { Word = uWord, MedianY = uMedianY });
                            }
                        }

                        //the final lines
                        foreach (var linegroup in cleanLines.OrderByDescending(kvp => kvp.Key).Select(kvp => kvp.Value))
                        {
                            var wordsOnLine = linegroup
                                .Select(w => w.Word)
                                .OrderBy(w => w.BoundingBox.Left)
                                .ToList();

                            //horizontal allignment
                            double currentX = 0;
                            foreach (var word in wordsOnLine)
                            {
                                double SpaceWidth = avgspace;
                                double reqspace = word.BoundingBox.Left - currentX;
                                double maxGapSize = SpaceWidth * MaxGapToleranceMultiplier;

                                if (reqspace > SpaceWidth)
                                {
                                    int insertspaces = (int)Math.Round(reqspace / SpaceWidth);
                                    result.Append(' ', insertspaces);
                                }
                                else if (reqspace > 0)
                                {
                                    result.Append(' ');
                                }
                                result.Append(word.Text.Normalize(NormalizationForm.FormKC));
                                currentX = word.BoundingBox.Right;
                            }
                            result.AppendLine();
                        }

                        if (string.IsNullOrEmpty(result.ToString()))
                        {
                            List<byte[]> imagelist = ExtractImagesFromPDF(path);
                            foreach (byte[] image in imagelist)
                            {
                                //create a bitmap image and image control for each image in the pdf
                                var bitmapImage = new BitmapImage();
                                using (var ms = new MemoryStream(image))
                                {
                                    bitmapImage.BeginInit();
                                    bitmapImage.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                                    bitmapImage.StreamSource = ms;
                                    bitmapImage.EndInit();
                                    bitmapImage.Freeze();
                                }
                                var imagecontrol = new System.Windows.Controls.Image();
                                imagecontrol.Source = bitmapImage;
                                DataHolder.Children.Add(imagecontrol);
                            }
                        }
                    }
                }
            }
            catch
            {
                MessageBox.Show("Something went wrong while processing the file");
                return string.Empty;
            }
            return result.ToString();
        }

        public static List<byte[]> ExtractImagesFromPDF(string path)
        {
            var imagebyteslist = new List<byte[]>();

            using (PdfDocument Pdf = PdfDocument.Open(path))
            {
                foreach (var page in Pdf.GetPages())
                {
                    IEnumerable<IPdfImage> images = page.GetImages();
                    foreach (var image in images)
                    {
                        byte[] pngdata = GetStandardImageBytes(image);
                        if (pngdata != null)
                        {
                            imagebyteslist.Add(pngdata);
                        }
                    }
                }
            }
            return imagebyteslist;
        }

        private static byte[] GetStandardImageBytes(IPdfImage image)
        {
            if (image.TryGetPng(out var rawbytes))
            {
                using (var ms = new MemoryStream(rawbytes))
                {
                    try
                    {
                        using (var imagesharp = SixLabors.ImageSharp.Image.Load(ms))
                        using (var outms = new MemoryStream())
                        {
                            imagesharp.SaveAsPng(outms);
                            return outms.ToArray();
                        }
                    }
                    catch
                    {
                        return null;
                    }
                }
            }
            else
            {
                rawbytes = image.RawBytes.ToArray();
                return rawbytes;
            }
        }

        public static string ExtractTextFromTxt (string path)
        {
            try
            {
                using StreamReader reader = new StreamReader(path);
                string text = reader.ReadToEnd();
                return text;
            }
            catch
            {
                MessageBox.Show("There was an issue reading the file");
                return string.Empty;
            }
        }

        public static string CombineImgData(Panel DataHolder)
        {
            var base64list = new List<string>();
            const string separator = "|-IMG-|";

            foreach (var imagecon in DataHolder.Children.OfType<System.Windows.Controls.Image>())
            {
                var bitmapsource = imagecon.Source as BitmapSource;
                if (bitmapsource == null) continue;

                byte[] imgbytes;
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmapsource));

                using (var ms = new MemoryStream())
                {
                    encoder.Save(ms);
                    imgbytes = ms.ToArray();
                }

                string base64string = Convert.ToBase64String(imgbytes);
                base64list.Add(base64string);
            }
            return string.Join(separator, base64list);
        }

        public FileSendDisplay()
        {
            InitializeComponent();
            if(Home.DarkLight == true)
            {
                Display.Background = new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#FF3C3B3B"));
                UL.Foreground = new SolidColorBrush(System.Windows.Media.Colors.White);
                FL.Foreground = new SolidColorBrush(System.Windows.Media.Colors.White);
                Content.Foreground = new SolidColorBrush(System.Windows.Media.Colors.White);
            }
            else
            {
                Display.Background = new SolidColorBrush(System.Windows.Media.Colors.White);
                UL.Foreground = new SolidColorBrush(System.Windows.Media.Colors.Black);
                FL.Foreground = new SolidColorBrush(System.Windows.Media.Colors.Black);
                Content.Foreground = new SolidColorBrush(System.Windows.Media.Colors.Black);
            }
            //Determines file extension and use right method for extension
            string FilePath = Home.filename;
            
            if (System.IO.Path.GetExtension(FilePath) == ".docx")
            {
                Content.Text = ExtractTextFromDocx(FilePath);
            }
            
            if (System.IO.Path.GetExtension(FilePath) == ".xlsx")
            {
                Content.Text = ExtractTextFromExcel(FilePath);
            }

            if (System.IO.Path.GetExtension(FilePath) == ".pdf")
            {
                Content.Text = ExtractTextFromPdf(FilePath);
            }

            if (System.IO.Path.GetExtension(FilePath) == ".txt")
            {
                Content.Text = ExtractTextFromTxt(FilePath);
            }
        }

        private void Send_Click(object sender, RoutedEventArgs e)
        {
            string connectionString = "Server=localhost;Database=capstoneprojdb;Uid=root;Pwd=;";    //Database credentials. If this were to be put into production,
                                                                                                    //a secure password would be set up along with other security measures.

            string searchText = MySearchBox.SearchText;
            if (string.IsNullOrEmpty(searchText))
            {
                MessageBox.Show("You must specify who you are sending the file to using the search box below.");
                return;
            }
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            using (MySqlCommand command = new MySqlCommand("SELECT UserID FROM users WHERE CONCAT(FirstName, ' ', LastName) = @search ", connection))
            {
                try
                {
                    connection.Open();

                    command.Parameters.AddWithValue("@search", searchText);

                    bool exist = DataHolder.Children.OfType<System.Windows.Controls.Image>().Any();

                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        string ID = "";
                        while (reader.Read())
                        {
                            ID = reader.GetString(0);
                            if (ID != null && ID != LoginPage.Userid)
                            {
                                if (!string.IsNullOrEmpty(Content.Text) && exist == false)
                                {
                                    Data = Content.Text;
                                }
                                else
                                {
                                    if (DataHolder.Children.Count > 0)
                                    {
                                        if (exist)
                                        {
                                            //call method to combine strings
                                            Data = CombineImgData(DataHolder);
                                        }
                                    }
                                }
                                uID = ID;
                                this.DialogResult = true;
                                this.Close();
                            }
                            else if (ID == LoginPage.Userid)
                            {
                                MessageBox.Show("Cannot send file to yourself.");
                            }
                        }
                        if (string.IsNullOrEmpty(ID))
                        {
                            MessageBox.Show("Invalid User Credentials. Check for typos");
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message);
                }

            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void MySearchBox_SearchTextChanged(object sender, RoutedEventArgs e)
        {
            SearchPopup.IsOpen = true;

            string searchText = MySearchBox.SearchText;
            string filter = FilterList.SelectionBoxItem.ToString();

            if (SearchPopup != null)
            {
                Results.SearchUsers(searchText, filter);
            }
        }

        private void FilterList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0)
            {
                return;
            }
            else
            {
                ComboBoxItem selecteditem = e.AddedItems[0] as ComboBoxItem;

                if (selecteditem != null)
                {
                    string searchText = MySearchBox.SearchText;
                    string filter = selecteditem.Content?.ToString() ?? "";

                    if (SearchPopup != null)
                    {
                        SearchPopup.IsOpen = true;
                        Results.SearchUsers(searchText, filter);
                    }
                }
            }
        }
    }
}
