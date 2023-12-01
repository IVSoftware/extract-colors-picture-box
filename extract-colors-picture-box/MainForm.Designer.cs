namespace extract_colors_picture_box
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            tableLayoutPanel = new TableLayoutPanel();
            progressBar = new ProgressBar();
            myCustomPicturebox = new MyCustomPicturebox();
            tableLayoutPanel.SuspendLayout();
            SuspendLayout();
            // 
            // tableLayoutPanel
            // 
            tableLayoutPanel.ColumnCount = 1;
            tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel.Controls.Add(progressBar, 0, 1);
            tableLayoutPanel.Controls.Add(myCustomPicturebox, 0, 0);
            tableLayoutPanel.Dock = DockStyle.Fill;
            tableLayoutPanel.Location = new Point(10, 10);
            tableLayoutPanel.Name = "tableLayoutPanel";
            tableLayoutPanel.RowCount = 2;
            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 88.679245F));
            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 11.320755F));
            tableLayoutPanel.Size = new Size(458, 507);
            tableLayoutPanel.TabIndex = 0;
            // 
            // progressBar
            // 
            progressBar.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            progressBar.Location = new Point(3, 452);
            progressBar.Name = "progressBar";
            progressBar.Size = new Size(452, 52);
            progressBar.TabIndex = 0;
            // 
            // myCustomPicturebox
            // 
            myCustomPicturebox.AllowDrop = true;
            myCustomPicturebox.Dock = DockStyle.Fill;
            myCustomPicturebox.Location = new Point(3, 3);
            myCustomPicturebox.Name = "myCustomPicturebox";
            myCustomPicturebox.Size = new Size(452, 443);
            myCustomPicturebox.TabIndex = 1;
            myCustomPicturebox.TabStop = false;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(478, 527);
            Controls.Add(tableLayoutPanel);
            Name = "MainForm";
            Padding = new Padding(10);
            Text = "Main Form";
            tableLayoutPanel.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel tableLayoutPanel;
        private ProgressBar progressBar;
        private MyCustomPicturebox myCustomPicturebox;
    }
}
