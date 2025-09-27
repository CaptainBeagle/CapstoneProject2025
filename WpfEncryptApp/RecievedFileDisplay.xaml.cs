using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using AWS.Cryptography.EncryptionSDK;
using AWS.Cryptography.MaterialProviders;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Win32;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Writer;
using K4os.Compression.LZ4.Internal;
using DocumentFormat.OpenXml.Drawing;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Type1;

namespace WpfEncryptApp
{
    /// <summary>
    /// Interaction logic for RecievedFileDisplay.xaml
    /// </summary>
    public partial class RecievedFileDisplay : System.Windows.Controls.Page
    {
        public RecievedFileDisplay(MemoryStream data, string sender, string title)
        {
            InitializeComponent();
            ChangeAppearance();
            FileTitle.Text = title;
            if (Home.ToOrFrom == false)
            {
                FileSender.Text = "From: " + sender;
            }
            else
            {
                FileSender.Text = "To: " + sender;
            }

            // Instantiate the AWS Encryption SDK and material providers
            var esdk = new ESDK(new AwsEncryptionSdkConfig());
            var mpl = new MaterialProviders(new MaterialProvidersConfig());

            var keyNameSpace = "KeyNS01";   //These values are important identifiers for the encryption key. These same values must be used to generate a corresponding key for decryption.
            var keyName = "Key01";

            string privatekeypath = "C:\\Users\\Rhian\\OneDrive\\Desktop\\GitRepository\\CapstoneProject2025\\WpfEncryptApp\\private_key.pem";
            byte[] rawprivatekey = System.IO.File.ReadAllBytes(privatekeypath); //getting data from private key file

            var encryptionContext = new Dictionary<string, string>()
            {
                {"purpose", "Send Message"}
            };

            var decryptKeyringInput = new CreateRawRsaKeyringInput
            {
                KeyNamespace = keyNameSpace,
                KeyName = keyName,
                PrivateKey = new MemoryStream(rawprivatekey),
                PaddingScheme = PaddingScheme.OAEP_SHA384_MGF1
            };

            var dkeyring = mpl.CreateRawRsaKeyring(decryptKeyringInput);

            //Define the decrypt input object
            var decryptInput = new DecryptInput
            {
                Ciphertext = data,
                Keyring = dkeyring,
                EncryptionContext = encryptionContext
            };

            //calling the Decrypt function
            try
            {
                var decryptOutput = esdk.Decrypt(decryptInput);
                using (MemoryStream plaintextStream = (MemoryStream)decryptOutput.Plaintext)    //separating the decrypted message to be displayed in the MainWindow
                {
                    byte[] plaintextBytes = plaintextStream.ToArray();
                    string decryptedString = Encoding.UTF8.GetString(plaintextBytes);
                    Content.Text = decryptedString;
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }

        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            if (Home.ToOrFrom == false)
            {
                Home home = new Home();
                NavigationService.Navigate(home);
            }
            else
            {
                ViewSentFiles viewsent = new ViewSentFiles();
                NavigationService.Navigate(viewsent);
            }
        }

        private void ChangeAppearance()
        {
            if (Home.DarkLight == true)
            {
                Display.Background = new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#FF3C3B3B"));
                FileTitle.Foreground = new SolidColorBrush(System.Windows.Media.Colors.White);
                FileSender.Foreground = new SolidColorBrush(System.Windows.Media.Colors.White);
                Content.Foreground = new SolidColorBrush(System.Windows.Media.Colors.White);
            }
            else
            {
                Display.Background = new SolidColorBrush(System.Windows.Media.Colors.White);
                FileTitle.Foreground = new SolidColorBrush(System.Windows.Media.Colors.Black);
                FileSender.Foreground = new SolidColorBrush(System.Windows.Media.Colors.Black);
                Content.Foreground = new SolidColorBrush(System.Windows.Media.Colors.Black);
            }
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            if (FileTitle.Text.Contains(".docx"))
            {
                //Create Word Doc file on computer with content
                using (MemoryStream mem = new MemoryStream())
                {
                    using (WordprocessingDocument wordDocument =
                        WordprocessingDocument.Create(mem, WordprocessingDocumentType.Document, true))
                    {
                        //Add a main document part. 
                        MainDocumentPart mainPart = wordDocument.AddMainDocumentPart();
                        //Create the document structure and add some text.
                        mainPart.Document = new Document();
                        Body docBody = new Body();

                        //Preserve content over multiple lines
                        string[] lines = Content.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

                        //Put each line of content into a new paragraph
                        foreach (string line in lines)
                        {
                            DocumentFormat.OpenXml.Wordprocessing.Paragraph p = new DocumentFormat.OpenXml.Wordprocessing.Paragraph();
                            DocumentFormat.OpenXml.Wordprocessing.Run r = new DocumentFormat.OpenXml.Wordprocessing.Run();

                            DocumentFormat.OpenXml.Wordprocessing.Text t = new DocumentFormat.OpenXml.Wordprocessing.Text(line);
                            r.Append(t);
                            p.Append(r);
                            docBody.Append(p);
                        }
                        mainPart.Document.Append(docBody);
                    }
                    Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();
                    saveFileDialog.FileName = "Document";
                    saveFileDialog.DefaultExt = ".docx";
                    saveFileDialog.Filter = "Word documents (.docx)|*.docx";

                    if (saveFileDialog.ShowDialog() == true)
                    {
                        string filePath = saveFileDialog.FileName;

                        try
                        {
                            File.WriteAllBytes(filePath, mem.ToArray());
                            MessageBox.Show("Document saved successfully!");
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"An error occurred while saving the document: {ex.Message}");
                        }
                    }
                }
            }
            else if (FileTitle.Text.Contains(".xlsx"))
            {
                //Create Excel file on computer with content
                using MemoryStream mem = new MemoryStream();
                {
                    using (SpreadsheetDocument spreadsheetDocument = SpreadsheetDocument.Create(mem, SpreadsheetDocumentType.Workbook))
                    {
                        //Add a WorkbookPart to the document
                        WorkbookPart workbookPart = spreadsheetDocument.AddWorkbookPart();
                        workbookPart.Workbook = new Workbook();

                        //Add a WorksheetPart to the WorkbookPart
                        WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                        worksheetPart.Worksheet = new Worksheet(new SheetData());

                        //Separate content into lines
                        string[] rows = Content.Text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

                        //Add Sheets to the Workbook
                        Sheets sheets = spreadsheetDocument.WorkbookPart.Workbook.AppendChild(new Sheets());
                        Sheet sheet = new Sheet() { Id = spreadsheetDocument.WorkbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = "MySheet" };
                        sheets.Append(sheet);

                        SheetData sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();

                        foreach (string rowText in rows)
                        {
                            //Separate further into individual cells
                            string[] cells = rowText.Split('\t');

                            Row newRow = new Row();

                            foreach (string cellText in cells)
                            {
                                Cell newCell = new Cell
                                {
                                    CellValue = new CellValue(cellText),
                                    DataType = CellValues.String
                                };
                                newRow.Append(newCell);
                            }
                            sheetData.Append(newRow);
                        }
                    }
                    SaveFileDialog saveFileDialog = new SaveFileDialog();
                    saveFileDialog.Filter = "Excel Files (*.xlsx)|*.xlsx";
                    saveFileDialog.DefaultExt = ".xlsx";
                    saveFileDialog.FileName = "Document";

                    if (saveFileDialog.ShowDialog() == true)
                    {
                        string filePath = saveFileDialog.FileName;

                        try
                        {
                            File.WriteAllBytes(filePath, mem.ToArray());
                            MessageBox.Show("Spreadsheet saved successfully!");
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"An error occurred while saving the file: {ex.Message}");
                        }
                    }
                }
            }
            else
            {
                //Create PDF file on computer with content
                PdfDocumentBuilder builder = new PdfDocumentBuilder();
                PdfPageBuilder page = builder.AddPage(UglyToad.PdfPig.Content.PageSize.A4);
                PdfDocumentBuilder.AddedFont font = builder.AddStandard14Font(UglyToad.PdfPig.Fonts.Standard14Fonts.Standard14Font.Helvetica);

                string NewText = Content.Text.Replace("\r\n", "\n").Replace("\r", "\n");
                string[] lines = NewText.Split("\n");

                int PageHeight = 842;
                int PageWidth = 1150;
                int LeftMargin = 25;
                int RightMargin = 25;
                int TopMargin = 50;
                int BottomMargin = 50;
                int startX = LeftMargin;
                double startY = PageHeight - TopMargin;
                int fontsize = 12;
                double linespacing = fontsize * 1.5;
                int MaxWidth = PageWidth - LeftMargin - RightMargin;

                foreach (string line in lines)
                {
                    if (startY - linespacing < BottomMargin)
                    {
                        startY = PageHeight - TopMargin;

                        page = builder.AddPage(UglyToad.PdfPig.Content.PageSize.A4);
                    }

                    if (string.IsNullOrWhiteSpace(line))
                    {
                        startY -= linespacing;
                        continue;
                    }

                    UglyToad.PdfPig.Core.PdfPoint position = new UglyToad.PdfPig.Core.PdfPoint(startX, startY);

                    string[] words = line.Split(' ');

                    string ReLine = "";

                    foreach (string word in words)
                    {
                        string testline = string.IsNullOrEmpty(ReLine) ? word : ReLine + " " + word;

                        var letters = page.MeasureText(testline, fontsize, position, font);

                        var letters2 = page.MeasureText(word, fontsize, position, font);

                        double textwidth = 0;

                        double wordwidth = 0;

                        if (letters.Any())
                        {
                            var firstletterx = letters.First().Location.X;
                            var lastletterx = letters.Last().Location.X + letters.Last().Width;

                            textwidth = lastletterx - startX;
                        }

                        if (letters2.Any())
                        {
                            var firstletterx = letters.First().Location.X;
                            var lastletterx = letters.Last().Location.X + letters.Last().Width;

                            wordwidth = lastletterx - startX;
                        }

                        if (textwidth > MaxWidth)
                        {
                            if (startY - linespacing < BottomMargin)
                            {
                                startY = PageHeight - TopMargin;

                                page = builder.AddPage(UglyToad.PdfPig.Content.PageSize.A4);
                            }

                            position = new UglyToad.PdfPig.Core.PdfPoint(startX, startY);
                            page.AddText(ReLine, fontsize, position, font);
                            startY -= linespacing;
                            ReLine = word;
                            continue;
                        }
                        else
                        {
                            ReLine += " " + word;
                        }
                    }

                    if(!string.IsNullOrEmpty(ReLine))
                    {
                        if (startY - linespacing < BottomMargin)
                        {
                            startY = PageHeight - TopMargin;

                            page = builder.AddPage(UglyToad.PdfPig.Content.PageSize.A4);
                        }
                        position = new UglyToad.PdfPig.Core.PdfPoint(startX, startY);
                        page.AddText(ReLine, fontsize, position, font);
                        startY -= linespacing;
                    }
                }

                byte[] documentBytes = builder.Build();
                
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "PDF Files (*.pdf)|*.pdf";
                saveFileDialog.DefaultExt = ".pdf";
                saveFileDialog.FileName = "Document";

                if (saveFileDialog.ShowDialog() == true)
                {
                    string filePath = saveFileDialog.FileName;

                    try
                    {
                        File.WriteAllBytes(filePath, documentBytes);
                        MessageBox.Show("PDF saved successfully!");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"An error occurred while saving the file: {ex.Message}");
                    }
                }
            }
        }
    }
}
