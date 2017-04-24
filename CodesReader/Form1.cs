using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using SharedDotNet.Classifier;
using SharedDotNet.Compute;
using SharedDotNet.Imaging;

namespace CodesReader
{
    public partial class Form1 : Form
    {
        private List<PictureBox> _boxes = new List<PictureBox>(25);
        private List<Label> _textboxes = new List<Label>(25);
        private readonly string _modelPath = "summary/experiment-16/models/model";
        private readonly IClassifier _classifier;
        public Form1()
        {
            InitializeComponent();
            const int spacing = 41;

            for (int i = 0; i < 25; i++)
            {
                var box = new PictureBox
                {
                    Width = 25,
                    Height = 25,
                    Location = new Point(27 + i * spacing, segmentedImage.Height + segmentedImage.Location.Y + 7),
                    //Image = Image.FromFile(@"D:\dataset\easy\wrong_segmentation\4_predicted=X_DPTK8-MKF9K-T387Y-8M4M6-WGTYB_0.bmp")
                };

                var textBox = new Label
                {
                    AutoSize = false,
                    Width = 40,
                    Height = 45,
                    Location = new Point(box.Left - 5, box.Location.Y + box.Size.Height + 7),
                    //Text = "A",
                    //BorderStyle = BorderStyle.FixedSingle,
                };
                textBox.Font = new Font("Arial", 16.5f, FontStyle.Regular);
                //textBox.Text = "A";
                
                Controls.Add(box);
                Controls.Add(textBox);
                _boxes.Add(box);
                _textboxes.Add(textBox);

            }

            _classifier = new NnLetterClassifier(_modelPath);
        }

        private void selectImage_Click(object sender, EventArgs e)
        {
            segmentedImage.Image?.Dispose();
            foreach (var pictureBox in _boxes)
            {
                pictureBox.Image?.Dispose();
                pictureBox.Image = null;
                pictureBox.BorderStyle = BorderStyle.None;
            }
            _textboxes.ForEach(t => t.Text = string.Empty);

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
                    segmentedImage.Image = result.SegmentedCode;

                    for (int i = 0; i < result.Letters.Count; i++)
                    {
                        _boxes[i].Image = result.Letters[i];
                    }

                    if (result.SegmentedCode == null || result.Letters == null || result.Letters.Count != 25)
                    {
                        resultLbl.Text = "Accuracy: 0/25";
                        return;
                    }

                    _classifier.Recognize(new List<ComputeResult> { result });
                    string correctCode = Path.GetFileNameWithoutExtension(result.ImagePath).Replace("-", string.Empty);

                    int incorrect = 0;
                    for (int i = 0; i < result.Letters.Count; i++)
                    {
                        _textboxes[i].Text = result.PredictedCodeLetters[i].ToString();

                        if (correctCode[i] != result.PredictedCodeLetters[i])
                        {
                            _boxes[i].ForeColor = _textboxes[i].ForeColor = Color.Red;
                            ++incorrect;
                        }
                        else
                        {
                            _boxes[i].ForeColor = _textboxes[i].ForeColor = Color.Black;
                        }
                    }

                    resultLbl.Text = $"Accuracy: {25 - incorrect}/25";
                }
            }
        }
    }
}
