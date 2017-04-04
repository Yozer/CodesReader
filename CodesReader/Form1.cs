using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CodesReader.Imaging;

namespace CodesReader
{
    public partial class Form1 : Form
    {
        private List<PictureBox> _boxes = new List<PictureBox>(29);
        public Form1()
        {
            InitializeComponent();

            for (int i = 0; i < 29; i++)
            {
                PictureBox box = new PictureBox
                {
                    Width = 25,
                    Height = 25,
                    Location = new Point(27 + i * 35, 570)
                };

                Controls.Add(box);
                _boxes.Add(box);
            }
        }

        private void selectImage_Click(object sender, EventArgs e)
        {
            segmentedImage.Image?.Dispose();
            foreach (var pictureBox in _boxes)
            {
                pictureBox.Image?.Dispose();
                pictureBox.Image = null;
            }

            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    var bmp = Image.FromFile(ofd.FileName);
                    if (bmp.Width < bmp.Height)
                    {
                        bmp.RotateFlip(RotateFlipType.Rotate270FlipNone);
                    }

                    originalImage.Image = bmp;

                    IImageProcessor processor = new ImageProcessorOpenCv();
                    var result = processor.SegmentCode(ofd.FileName);
                    segmentedImage.Image = result[0];

                    for (int i = 0; i < _boxes.Count; i++)
                    {
                        if (i + 1 >= result.Count) break;
                        _boxes[i].Image = result[i + 1];
                    }
                }
            }
        }
    }
}
