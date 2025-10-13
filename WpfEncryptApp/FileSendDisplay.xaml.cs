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
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Office2013.Drawing.Chart;
using DocumentFormat.OpenXml.Drawing.Charts;


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
        public static string ExtractTextFromDocx(string Path)
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

                    return textBuilder.ToString();
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
                return string.Empty;
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
                //return '\t'.ToString();
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
        public static string ExtractTextFromPdf(string path)
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
                                // check if white space (default implementation of 'Filter')
                                if (string.IsNullOrWhiteSpace(candidate.Value))
                                {
                                    // pivot and candidate letters cannot belong to the same word 
                                    // if candidate letter is null or white space.
                                    // ('FilterPivot' already checks if the pivot is null or white space by default)
                                    return false;
                                }

                                // check for height difference
                                var maxHeight = Math.Max(pivot.PointSize, candidate.PointSize);
                                var minHeight = Math.Min(pivot.PointSize, candidate.PointSize);
                                if (minHeight != 0 && maxHeight / minHeight > 1.5)
                                {
                                    // pivot and candidate letters cannot belong to the same word 
                                    // if one letter is more than twice the size of the other.
                                    return false;
                                }

                                // check for colour difference
                                var pivotRgb = pivot.Color.ToRGBValues();
                                var candidateRgb = candidate.Color.ToRGBValues();
                                if (!pivotRgb.Equals(candidateRgb))
                                {
                                    // pivot and candidate letters cannot belong to the same word 
                                    // if they don't have the same colour.
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

                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string ID = reader.GetString(0);
                            if (ID != null && ID != LoginPage.Userid)
                            {
                                Data = Content.Text;
                                uID = ID;
                                this.DialogResult = true;
                                this.Close();
                            }
                            else if (ID == LoginPage.Userid)
                            {
                                MessageBox.Show("Cannot send file to yourself.");
                            }
                            else
                            {
                                MessageBox.Show("Invalid User Credentials. Check for typos");
                            }
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
