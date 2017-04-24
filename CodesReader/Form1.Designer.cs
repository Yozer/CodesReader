namespace CodesReader
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.selectImage = new System.Windows.Forms.Button();
            this.originalImage = new System.Windows.Forms.PictureBox();
            this.segmentedImage = new System.Windows.Forms.PictureBox();
            this.resultLbl = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.originalImage)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.segmentedImage)).BeginInit();
            this.SuspendLayout();
            // 
            // selectImage
            // 
            this.selectImage.Location = new System.Drawing.Point(27, 22);
            this.selectImage.Name = "selectImage";
            this.selectImage.Size = new System.Drawing.Size(120, 42);
            this.selectImage.TabIndex = 0;
            this.selectImage.Text = "Select scan";
            this.selectImage.UseVisualStyleBackColor = true;
            this.selectImage.Click += new System.EventHandler(this.selectImage_Click);
            // 
            // originalImage
            // 
            this.originalImage.Location = new System.Drawing.Point(27, 70);
            this.originalImage.Name = "originalImage";
            this.originalImage.Size = new System.Drawing.Size(790, 427);
            this.originalImage.TabIndex = 1;
            this.originalImage.TabStop = false;
            // 
            // segmentedImage
            // 
            this.segmentedImage.Location = new System.Drawing.Point(27, 513);
            this.segmentedImage.Name = "segmentedImage";
            this.segmentedImage.Size = new System.Drawing.Size(1031, 35);
            this.segmentedImage.TabIndex = 2;
            this.segmentedImage.TabStop = false;
            // 
            // resultLbl
            // 
            this.resultLbl.AutoSize = true;
            this.resultLbl.Font = new System.Drawing.Font("Verdana", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.resultLbl.Location = new System.Drawing.Point(837, 472);
            this.resultLbl.Name = "resultLbl";
            this.resultLbl.Size = new System.Drawing.Size(116, 25);
            this.resultLbl.TabIndex = 3;
            this.resultLbl.Text = "Accuracy:";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1079, 636);
            this.Controls.Add(this.resultLbl);
            this.Controls.Add(this.segmentedImage);
            this.Controls.Add(this.originalImage);
            this.Controls.Add(this.selectImage);
            this.Font = new System.Drawing.Font("Verdana", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Codes Reader v0.1";
            ((System.ComponentModel.ISupportInitialize)(this.originalImage)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.segmentedImage)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button selectImage;
        private System.Windows.Forms.PictureBox originalImage;
        private System.Windows.Forms.PictureBox segmentedImage;
        private System.Windows.Forms.Label resultLbl;
    }
}

