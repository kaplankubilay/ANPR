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

        ANPRDBEntities _dataBase = new ANPRDBEntities();
        List<PlateRecognition> _plateList = new List<PlateRecognition>();

        private FilterInfoCollection fico;
        private VideoCaptureDevice vcd;

        private void toTextButton_Click(object sender, EventArgs e)
        {
            //if (openFileDialog1.ShowDialog() == DialogResult.OK)
            //{
            //    var image = new Bitmap(openFileDialog1.FileName);
            //    var ocr = new TesseractEngine("./Traineddata", "eng");
            //    var sonuc = ocr.Process(image);
            //    richTextBox1.Text = sonuc.GetText();
            //}
        }

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

        private void yakalaButon_Click(object sender, EventArgs e)
        {
            //SaveFileDialog sfd = new SaveFileDialog();
            //sfd.Filter = "(*.jpg)|*.jpg";
            //DialogResult dr = sfd.ShowDialog();


            //if (dr == DialogResult.OK)//diyalog ok ise resmi kaydet
            //{
            //    pictureBox1.Image.Save(sfd.FileName);
            //}
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (pictureBox1.Image != null)
            {
                var image = new Bitmap(pictureBox1.Image);
                var ocr = new TesseractEngine("./Traineddata", "eng");
                var sonuc = ocr.Process(image);
                if (sonuc != null)
                {
                    string readyText = RemoveSpaceUpper(sonuc.GetText());

                    string result = CleanTextAlgorithm(readyText);

                    if (_plateList.Any(x=>x.Plate == result))
                    {
                        richTextBox1.Text = result;
                        var asd=_plateList.FirstOrDefault(x => x.Plate == result);
                        asd.CreateDate=DateTime.Now;
                        _dataBase.SaveChanges();
                    }
                }
            }
        }

        private string CleanTextAlgorithm(string readyText)
        {
            try
            {
                int count = readyText.Length;
                string result = "";

                for (int i = 0; i < count; i++)
                {
                    if (readyText[i] == '/')
                    {
                        i++;
                        continue;
                    }

                    result += readyText[i];
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
    }
}
