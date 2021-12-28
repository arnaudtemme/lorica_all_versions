namespace LORICA4
{
    partial class Soil_specifier
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Soil_specifier));
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.coarsebox = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.sandbox = new System.Windows.Forms.TextBox();
            this.siltbox = new System.Windows.Forms.TextBox();
            this.claybox = new System.Windows.Forms.TextBox();
            this.fineclaybox = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.total_box = new System.Windows.Forms.TextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(233, 91);
            this.label1.TabIndex = 0;
            this.label1.Text = resources.GetString("label1.Text");
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 138);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(88, 13);
            this.label2.TabIndex = 1;
            this.label2.Text = "coarse fragments";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 161);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(30, 13);
            this.label3.TabIndex = 2;
            this.label3.Text = "sand";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 185);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(19, 13);
            this.label4.TabIndex = 3;
            this.label4.Text = "silt";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(12, 209);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(61, 13);
            this.label5.TabIndex = 4;
            this.label5.Text = "coarse clay";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(12, 231);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(46, 13);
            this.label6.TabIndex = 5;
            this.label6.Text = "fine clay";
            // 
            // coarsebox
            // 
            this.coarsebox.Location = new System.Drawing.Point(125, 135);
            this.coarsebox.Name = "coarsebox";
            this.coarsebox.Size = new System.Drawing.Size(82, 20);
            this.coarsebox.TabIndex = 6;
            this.coarsebox.Text = "25";
            this.coarsebox.TextChanged += new System.EventHandler(this.textBox1_TextChanged);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(79, 119);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(88, 13);
            this.label7.TabIndex = 7;
            this.label7.Text = "mass percentage";
            // 
            // sandbox
            // 
            this.sandbox.Location = new System.Drawing.Point(125, 158);
            this.sandbox.Name = "sandbox";
            this.sandbox.Size = new System.Drawing.Size(82, 20);
            this.sandbox.TabIndex = 8;
            this.sandbox.Text = "25";
            this.sandbox.TextChanged += new System.EventHandler(this.textBox2_TextChanged);
            // 
            // siltbox
            // 
            this.siltbox.Location = new System.Drawing.Point(125, 182);
            this.siltbox.Name = "siltbox";
            this.siltbox.Size = new System.Drawing.Size(82, 20);
            this.siltbox.TabIndex = 9;
            this.siltbox.Text = "25";
            this.siltbox.TextChanged += new System.EventHandler(this.textBox3_TextChanged);
            // 
            // claybox
            // 
            this.claybox.Location = new System.Drawing.Point(125, 206);
            this.claybox.Name = "claybox";
            this.claybox.Size = new System.Drawing.Size(82, 20);
            this.claybox.TabIndex = 10;
            this.claybox.Text = "25";
            this.claybox.TextChanged += new System.EventHandler(this.textBox4_TextChanged);
            // 
            // fineclaybox
            // 
            this.fineclaybox.Location = new System.Drawing.Point(125, 228);
            this.fineclaybox.Name = "fineclaybox";
            this.fineclaybox.Size = new System.Drawing.Size(82, 20);
            this.fineclaybox.TabIndex = 11;
            this.fineclaybox.Text = "0";
            this.fineclaybox.TextChanged += new System.EventHandler(this.textBox5_TextChanged);
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(12, 257);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(27, 13);
            this.label8.TabIndex = 12;
            this.label8.Text = "total";
            // 
            // total_box
            // 
            this.total_box.Location = new System.Drawing.Point(125, 254);
            this.total_box.Name = "total_box";
            this.total_box.ReadOnly = true;
            this.total_box.Size = new System.Drawing.Size(82, 20);
            this.total_box.TabIndex = 13;
            this.total_box.Text = "0";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(138, 280);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(69, 24);
            this.button1.TabIndex = 14;
            this.button1.Text = "ready";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // Soil_specifier
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(259, 311);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.total_box);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.fineclaybox);
            this.Controls.Add(this.claybox);
            this.Controls.Add(this.siltbox);
            this.Controls.Add(this.sandbox);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.coarsebox);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Name = "Soil_specifier";
            this.Text = "Soil_specifier";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        public System.Windows.Forms.TextBox coarsebox;
        private System.Windows.Forms.Label label7;
        public System.Windows.Forms.TextBox sandbox;
        public System.Windows.Forms.TextBox siltbox;
        public System.Windows.Forms.TextBox claybox;
        public System.Windows.Forms.TextBox fineclaybox;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox total_box;
        private System.Windows.Forms.Button button1;
    }
}