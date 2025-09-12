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
using UglyToad.PdfPig.Content;


namespace WpfEncryptApp
{
    /// <summary>
    /// Interaction logic for FileSendDisplay.xaml
    /// </summary>
    public partial class FileSendDisplay : Window
    {
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
                    if (mainPart == null || mainPart.Document == null || mainPart.Document.Body == null)
                    {
                        return string.Empty;
                    }

                    //The following code formats the displayed output to be the same as the original word document.
                    StringBuilder textBuilder = new StringBuilder();

                    // Iterate through each paragraph in the document's body
                    foreach (DocumentFormat.OpenXml.Wordprocessing.Paragraph paragraph in mainPart.Document.Body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>())
                    {
                        // Append the text of the current paragraph
                        textBuilder.Append(paragraph.InnerText);

                        // Append a new line to separate paragraphs
                        textBuilder.AppendLine();
                    }

                    return textBuilder.ToString();
                }
            }
            catch (System.Exception ex)
            {
                //Console log to report the exception.
                Console.WriteLine($"An error occurred while processing the document: {ex.Message}");
                return string.Empty;
            }
        }

        //extracts text from xlsx files
        public static string ExtractTextFromExcel(string Path)
        {
            if(!File.Exists(Path))
            {
                Console.WriteLine($"Error: The file was not found at path: {Path}");
                return string.Empty;
            }

            StringBuilder result = new StringBuilder();
            try
            {
                using (SpreadsheetDocument spreadsheet = SpreadsheetDocument.Open(Path, false))
                {
                    //preparing parts of the spreadsheet for display
                    
                    WorkbookPart WBPart = spreadsheet.WorkbookPart;
                    SharedStringTablePart STPart = WBPart.GetPartsOfType<SharedStringTablePart>().FirstOrDefault();
                    SharedStringTable StringTable = null;
                    if ( STPart != null ) 
                    {
                        StringTable = STPart.SharedStringTable;
                    }

                    //getting the data in the sheet
                    foreach (WorksheetPart WSheetPart in WBPart.WorksheetParts) 
                    { 
                        Worksheet sheet = WSheetPart.Worksheet;
                        SheetData SData = sheet.GetFirstChild<SheetData>();

                        foreach (Row r in SData.Elements<Row>())
                        {
                            bool firstCell = true;
                            foreach (Cell c in r.Elements<Cell>())
                            {
                                if (!firstCell)
                                {
                                    result.Append('\t');    //Add tab space between cells
                                }
                                result.Append(GetCellValue(c, StringTable));
                                firstCell = false;
                            }
                            result.AppendLine();    //Separating rows of data
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                //Console log to report the exception.
                Console.WriteLine($"An error occurred while processing the document: {ex.Message}");
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
                return stringtable.ElementAt(int.Parse(val)).InnerText;
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

            using (PdfDocument Pdf = PdfDocument.Open(path))
            {
                foreach (UglyToad.PdfPig.Content.Page page in Pdf.GetPages())
                {
                    result = result.Append(page.Text);
                    result = result.AppendLine();
                }
            }
            return result.ToString();
        }

        public FileSendDisplay()
        {
            InitializeComponent();
            
            //Determines file extension and use right method for extension
            string FilePath = Home.filename;
            
            if (System.IO.Path.GetExtension(FilePath) == ".docx")
            {
                Content.Text = ExtractTextFromDocx(FilePath);
            }
            
            if (System.IO.Path.GetExtension(FilePath) == ".xlxs")
            {
                Content.Text = ExtractTextFromExcel(FilePath);
            }

            if (System.IO.Path.GetExtension(FilePath) == ".pdf")
            {
                Content.Text = ExtractTextFromPdf(FilePath);
            }

            //add ability to select recipient and send/cancel
            //Need DB access to query for users in users table (try to add filter/search functions)
        }

        private void SearchBox_SearchButtonClick(object sender, RoutedEventArgs e)
        {
            string searchText = (MySearchBox.FindName("SearchContent") as TextBox)?.Text;

            // Pass the text to the popup content control and run the query
            if (SearchPopup != null)
            {
                Results.SearchUsers(searchText);
                SearchPopup.IsOpen = true;
            }
        }
    }
}
