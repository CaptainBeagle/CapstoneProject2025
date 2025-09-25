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
                using (PdfDocument Pdf = PdfDocument.Open(path))
                {
                    foreach (var page in Pdf.GetPages())
                    {
                        var text = ContentOrderTextExtractor.GetText(page);
                        result = result.Append(text);
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
        }

        private void SearchBox_SearchButtonClick(object sender, RoutedEventArgs e)
        {
            SearchPopup.IsOpen = true;

            string searchText = MySearchBox.SearchText;
            string filter = FilterList.SelectionBoxItem.ToString();

            if (SearchPopup != null)
            {
                Results.SearchUsers(searchText, filter);
            }
        }

        //add function to process selected option in filter box

        private void Send_Click(object sender, RoutedEventArgs e)
        {
            string connectionString = "Server=localhost;Database=capstoneprojdb;Uid=root;Pwd=;";    //Database credentials. If this were to be put into production,
                                                                                                    //a secure password would be set up along with other security measures.

            string searchText = MySearchBox.SearchText;
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
    }
}
