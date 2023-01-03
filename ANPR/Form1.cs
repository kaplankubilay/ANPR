using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Entity;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tesseract;
using AForge.Video;
using AForge.Video.DirectShow;
using AForge.Imaging;

namespace ANPR
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private ANPRDBEntities _dataBase = new ANPRDBEntities();
        private List<PlateRecognition> _plateList = new List<PlateRecognition>();

        private FilterInfoCollection fico;
        private VideoCaptureDevice vcd;

        private void Form1_Load(object sender, EventArgs e)
        {
            fico = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            foreach (FilterInfo item in fico)
            {
                comboBox1.Items.Add(item.Name);
                comboBox1.SelectedIndex = 0;
            }
        }

        private void baslatButon_Click(object sender, EventArgs e)
        {
            //vcd = new VideoCaptureDevice(fico[comboBox1.SelectedIndex].MonikerString);
            //vcd.NewFrame += Vcd_NewFrame; //hareketli görüntü sağlar
            //vcd.Start();
            //timer1.Start();
            //_plateList = _dataBase.PlateRecognition.ToList();
            StartApplication();
        }

        private void StartApplication()
        {
            vcd = new VideoCaptureDevice(fico[comboBox1.SelectedIndex].MonikerString);
            vcd.NewFrame += Vcd_NewFrame; //hareketli görüntü sağlar
            vcd.Start();
            timer1.Start();
            _plateList = _dataBase.PlateRecognition.ToList();
        }

        /// <summary>
        /// kameradan alınan çerçeve aktarıldı.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void Vcd_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            pictureBox1.Image = (Bitmap)eventArgs.Frame.Clone();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            try
            {
                if (pictureBox1.Image != null)
                {
                    var image = new Bitmap(pictureBox1.Image);
                    var ocr = new TesseractEngine("./Traineddata", "eng");
                    var sonuc = ocr.Process(image);
                    if (sonuc != null)
                    {
                        richTextBox2.Text = sonuc.GetText();

                        //string readyText = RemoveSpaceUpper(sonuc.GetText());
                        //string result = CleanTextAlgorithm(readyText);

                        string cleanedText = CleanTextAlgorithm(sonuc.GetText());

                        string result = DinamikStringListOlusturma(cleanedText, _plateList);

                        if (_plateList.Any(x => x.Plate == result))
                        {
                            richTextBox1.Text = result;
                            var asd = _plateList.FirstOrDefault(x => x.Plate == result);
                            asd.CreateDate = DateTime.Now;
                            _dataBase.SaveChanges();
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                throw;
            }
        }

        private static string DinamikStringListOlusturma(string sliceString, List<PlateRecognition> plates)
        {
            //bosluk sayısı + 1 parca sayısıdır
            int pieceCount = 1;

            for (int i = 0; i < sliceString.Length; i++)
            {
                if (sliceString.Substring(i, 1) == " ")
                {
                    pieceCount++;
                }
            }

            List<string> items = new List<string>();

            string[] allPieces = new string[pieceCount];

            allPieces = sliceString.Split(' ');

            for (int i = 0; i < allPieces.Length; i++)
            {
                items.Add(allPieces[i]);
            }

            foreach (PlateRecognition plate in plates)
            {
                int findPieceCount = 0;
                foreach (string item in items)
                {
                    if (plate.Plate.Contains(item) && item != "")
                    {
                        findPieceCount++;
                    }

                    if (findPieceCount >= 2)
                    {
                        return plate.Plate;
                    }
                }
            }

            return "";
        }

        /// <summary>
        /// Gelen text in filtreleme algoritmaları
        /// </summary>
        /// <param name="readyText"></param>
        /// <returns></returns>
        private string CleanTextAlgorithm(string readyText)
        {
            try
            {
                string result = DiffirentCharacterDelete(readyText);

                //string returnResult = PlateFormatAccord(result);

                //return returnResult;

                return result;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        /// <summary>
        /// Gelen string i Turk plakasına uyarlamak için kullanılan algoritma.
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        private string PlateFormatAccord(string result)
        {
            try
            {
                int cleanedTextLenght = result.Length;
                bool firstTwoCharIsNumber = false; //ilk iki karakter rakam mı
                int staticNumberCountOnPlate = 0; //ilk iki karakter rakam olmak zorunda
                string temp = "";//plakadaki ilk iki rakam
                int tempCount = 0;//ilk iki karakterden sonrada ardışık olarak plaka sonunda rakamlar olabilir o yüzden birden fazla tekrar yapmamalı.
                string returnResult = "";

                for (int i = 0; i < cleanedTextLenght; i++)
                {
                    if (result[i] == '0' || result[i] == '1' ||
                        result[i] == '2' || result[i] == '3' ||
                        result[i] == '4' || result[i] == '5' ||
                        result[i] == '6' || result[i] == '7' ||
                        result[i] == '8' || result[i] == '9')
                    {
                        staticNumberCountOnPlate++;
                        temp += result[i];
                    }
                    else if (staticNumberCountOnPlate == 2)
                    {
                        firstTwoCharIsNumber = true;
                    }
                    else
                    {
                        staticNumberCountOnPlate = staticNumberCountOnPlate == 0 ? 0 : staticNumberCountOnPlate--;
                        temp = "";
                    }

                    if (firstTwoCharIsNumber && tempCount == 0)
                    {
                        returnResult = temp;
                        tempCount++;
                    }

                    if (tempCount == 1)
                    {
                        returnResult += result[i];
                    }
                }

                return returnResult;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        /// <summary>
        /// Tanımlı olan karakterler dışında gelen karakterleri temizleme metodu
        /// </summary>
        /// <param name="readyText"></param>
        /// <returns></returns>
        private string DiffirentCharacterDelete(string readyText)
        {
            try
            {
                int dirtyDataLength = readyText.Length;
                string result = "";
                //gelen veri kirli karakterlerden arindirilir.
                for (int i = 0; i < dirtyDataLength; i++)
                {
                    if (readyText[i] == '0' || readyText[i] == '1' ||
                        readyText[i] == '2' || readyText[i] == '3' ||
                        readyText[i] == '4' || readyText[i] == '5' ||
                        readyText[i] == '6' || readyText[i] == '7' ||
                        readyText[i] == '8' || readyText[i] == '9' ||
                        readyText[i] == 'A' || readyText[i] == 'B' ||
                        readyText[i] == 'C' || readyText[i] == 'D' ||
                        readyText[i] == 'E' || readyText[i] == 'F' ||
                        readyText[i] == 'G' || readyText[i] == 'H' ||
                        readyText[i] == 'I' || readyText[i] == 'J' ||
                        readyText[i] == 'K' || readyText[i] == 'L' ||
                        readyText[i] == 'M' || readyText[i] == 'N' ||
                        readyText[i] == 'O' || readyText[i] == 'P' ||
                        readyText[i] == 'R' || readyText[i] == 'S' ||
                        readyText[i] == 'T' || readyText[i] == 'U' ||
                        readyText[i] == 'V' || readyText[i] == 'Y' ||
                        readyText[i] == 'Z' || readyText[i] == 'W' ||
                        readyText[i] == 'X' || readyText[i] == ' ')
                    {
                        result += readyText[i];
                    }
                    else
                    {
                        continue;
                    }
                }

                return result;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private static string RemoveSpaceUpper(string input)
        {
            try
            {
                return new string(input.ToCharArray()
                    .Where(c => !Char.IsWhiteSpace(c))
                    .ToArray()).ToUpper();
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void yenile_btn_Click(object sender, EventArgs e)
        {
            richTextBox1.Clear();
            StartApplication();
        }
    }
}