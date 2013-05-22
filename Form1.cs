using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace ImageManipulation
{
    public partial class Form1 : Form
    {
        string fileImage;
        string fileText;
        Image im;
        Image im_bw;
        Graphics g;
        private Point initialMousePos;

        public Form1()
        {
            InitializeComponent();
            g = this.CreateGraphics();
            fileImage = null;
            fileText = null;
            im = null;
            im_bw = null;
            label_chars.Text = "No text file";
            label_thresh.Text = "Threshold: " + trackThresh.Value.ToString();
        }

        private void load_image_Click(object sender, EventArgs e)
        {
            // User has pressed the button to load an image
            DialogResult result = openFileDialog1.ShowDialog(); // Show the dialog.
            if (result == DialogResult.OK) // Test result.
            {
                fileImage = openFileDialog1.FileName;
                im = Image.FromFile(fileImage);
                pictureBox1.Image = im;
                button_flattenBW.Enabled = true;
            }
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            // User has clicked on the image area
            if (im != null) // If an image has been loaded...
            {
                this.initialMousePos = e.Location;
                Bitmap bmap = new Bitmap(im);
                // Set the value of the black/white threshold to the selected pixel's brightness
                trackThresh.Value = (int)(bmap.GetPixel(e.X, e.Y).GetBrightness() * 256);
                // Update the display
                label_thresh.Text = "Threshold: " + trackThresh.Value.ToString();
            }
        }

        private void trackThresh_Scroll(object sender, EventArgs e)
        {
            // If user changes the black/white slider, update the display
            label_thresh.Text = "Threshold: " + trackThresh.Value.ToString();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Flatten the image to just black and white based on a threshold value
            Bitmap bmap = new Bitmap(im);
            Color col;
            // Keep track of the ratio of black to white
            int whiteCount = 0;
            int blackCount = 0;
            for (int i = 0; i < bmap.Width; i++)
            {
                for (int j = 0; j < bmap.Height; j++)
                {
                    col = bmap.GetPixel(i, j);
                    if (col.GetBrightness() * 256 > trackThresh.Value)
                    {
                        bmap.SetPixel(i, j, Color.White);
                        whiteCount++;
                    }
                    else
                    {
                        bmap.SetPixel(i, j, Color.Black);
                        blackCount++;
                    }
                }
            }
            double percentWhite = whiteCount * 1.0 / (whiteCount + blackCount);
            label_imageratio.Text = ("W/B ratio: " + percentWhite.ToString()).Remove(16);
            im_bw = bmap;
            pictureBox1.Image = im_bw;
            button_load_text.Enabled = true;
        }

        private void button_load_text_Click(object sender, EventArgs e)
        {
            // User has clicked button to load text file
            DialogResult result = openFileDialog1.ShowDialog(); // Show the dialog.
            if (result == DialogResult.OK) // Test result.
            {
                fileText = openFileDialog1.FileName;
                System.IO.StreamReader file = new System.IO.StreamReader(fileText);
                string line;
                int charCount = 0;
                while ((line = file.ReadLine()) != null)
                {
                    charCount += line.Length;
                }
                label_chars.Text = "Characters: " + charCount.ToString();
                button_checktextsize.Enabled = true;
            }
        }

        private void radio_white_CheckedChanged(object sender, EventArgs e)
        {
            if (radio_white.Checked) radio_black.Checked = false;
        }
        private void radio_black_CheckedChanged(object sender, EventArgs e)
        {
            if (radio_black.Checked) radio_white.Checked = false;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // User has clicked button to check amount of text vs size of image
            double textHeight = 0.0;
            System.IO.StreamReader file = new System.IO.StreamReader(fileText);
            string line;
            double width = 0.0;
            Font myFont = new Font("Arial", (float)Convert.ToDouble(font_size.Text));
            while ((line = file.ReadLine()) != null)
            {
                if (textHeight == 0)
                    textHeight = g.MeasureString(line, myFont).Height;
                width += g.MeasureString(line, myFont).Width;
            }
            label_textratio.Text = ("Text/image ratio: "
                + (textHeight * width / (im.Height * im.Width)).ToString()).Remove(23);
            button_fittext.Enabled = true;
        }

        public char safeChar(char c)
        {
            if (char.IsLetterOrDigit(c)) return c;
            if (char.IsPunctuation(c)) return c;
            if (c == ' ') return c;
            return '\n';
        }

        private void button_fittext_Click(object sender, EventArgs e)
        {
            Bitmap bmap = new Bitmap(im.Width, im.Height);
            for (int i = 0; i < bmap.Width; i++)
            {
                for (int j = 0; j < bmap.Height; j++)
                {
                    bmap.SetPixel(i, j, Color.White);
                }
            }
            Bitmap bw = new Bitmap(im_bw);
            System.IO.StreamReader file = new System.IO.StreamReader(fileText);
            Font myFont = new Font("Arial", (float)Convert.ToDouble(font_size.Text));
            double textHeight = g.MeasureString("X", myFont).Height;
            SolidBrush myBrush = new SolidBrush(Color.Black);
            string word = "";
            string nextChar = safeChar(Convert.ToChar(file.Read())).ToString();
            while (nextChar == "\n" || nextChar == "\r")
                nextChar = safeChar(Convert.ToChar(file.Read())).ToString();
            Graphics gr = Graphics.FromImage(bmap);
            Boolean stop = false;

            double marginLeft = textHeight;
            double marginRight = marginLeft;
            double marginTop = textHeight;
            double marginBottom = marginTop;

            double verOff = marginTop;
            double horOff = marginLeft;
            double verSqueeze = 1.0;
            double horSqueeze = 1.0;

            Color target = Color.FromArgb(255, 255, 255);
            if (radio_black.Checked) target = Color.FromArgb(0, 0, 0);

            while (!file.EndOfStream && !stop) 
            {
                //Find next empty pixel
                while(bw.GetPixel((int)horOff,(int)verOff) != target) 
                {
                    horOff += 1;
                    if (horOff >= im.Width - marginLeft - marginRight) 
                    {
                        horOff = marginLeft;
                        verOff += textHeight * verSqueeze;
                    }
                    if (verOff >= im.Height - marginTop - marginBottom) // Ran out of area before we ran out text
                    {
                        stop = true;
                        break;
                    }
                }
                if (!stop)
                {
                    while (bw.GetPixel((int)(horOff + gr.MeasureString(word, myFont).Width
                    * horSqueeze), (int)verOff) == target)
                    {
                        word = word + nextChar;
                        nextChar = safeChar(Convert.ToChar(file.Read())).ToString();
                        while (nextChar == "\n" || nextChar == "\r")
                            nextChar = safeChar(Convert.ToChar(file.Read())).ToString();

                        if ((int)(horOff + gr.MeasureString(word + nextChar, myFont).Width * horSqueeze)
                            >= im.Width - marginLeft - marginRight) break;
                        if ((int)verOff > im.Height - marginTop - marginBottom) break;
                        if (file.EndOfStream) break;
                    }
                }

                gr.DrawString(word, myFont, myBrush, (float)horOff, (float)verOff);
                horOff += gr.MeasureString(word, myFont).Width * horSqueeze;
                word = "";

                if ((int)(horOff + gr.MeasureString(word + nextChar, myFont).Width * horSqueeze)
                            >= im.Width - marginLeft - marginRight)
                {
                    horOff = marginLeft;
                    verOff += textHeight * verSqueeze;
                }
                if ((int)verOff > im.Height - marginTop - marginBottom)
                    break;
            }
            pictureBox1.Image = bmap;
        
        }

        private void button_save_Click(object sender, EventArgs e)
        {
            DialogResult result = saveFileDialog1.ShowDialog(); // Show the dialog.
            if (result == DialogResult.OK) // Test result.
            {
                String fileSaveImage = openFileDialog1.FileName;
                pictureBox1.Image.Save(fileSaveImage);
            }
        }

       

    }
}
