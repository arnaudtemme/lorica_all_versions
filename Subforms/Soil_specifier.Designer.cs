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
            System.Windows.Forms.Label label49;
            System.Windows.Forms.Label label54;
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
            this.upper_particle_fine_clay_textbox = new System.Windows.Forms.TextBox();
            this.upper_particle_clay_textbox = new System.Windows.Forms.TextBox();
            this.upper_particle_silt_textbox = new System.Windows.Forms.TextBox();
            this.upper_particle_sand_textbox = new System.Windows.Forms.TextBox();
            this.upper_particle_coarse_textbox = new System.Windows.Forms.TextBox();
            this.specific_area_fine_clay_textbox = new System.Windows.Forms.TextBox();
            this.specific_area_clay_textbox = new System.Windows.Forms.TextBox();
            this.specific_area_silt_textbox = new System.Windows.Forms.TextBox();
            this.specific_area_sand_textbox = new System.Windows.Forms.TextBox();
            this.specific_area_coarse_textbox = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.yombox = new System.Windows.Forms.TextBox();
            this.oombox = new System.Windows.Forms.TextBox();
            label49 = new System.Windows.Forms.Label();
            label54 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // label49
            // 
            label49.AutoSize = true;
            label49.Location = new System.Drawing.Point(217, 56);
            label49.Name = "label49";
            label49.Size = new System.Drawing.Size(141, 13);
            label49.TabIndex = 29;
            label49.Text = "upper limit of particle size [m]";
            // 
            // label54
            // 
            label54.AutoSize = true;
            label54.Location = new System.Drawing.Point(364, 56);
            label54.Name = "label54";
            label54.Size = new System.Drawing.Size(151, 13);
            label54.TabIndex = 41;
            label54.Text = "specific surface area [m2 / kg]";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(343, 26);
            this.label1.TabIndex = 0;
            this.label1.Text = "Please fill in properties for the five LORICA grain size classes. \r\nThis will be " +
    "used to define any soil existing before the start of simulation.\r\n";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 75);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(88, 13);
            this.label2.TabIndex = 1;
            this.label2.Text = "coarse fragments";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 101);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(30, 13);
            this.label3.TabIndex = 2;
            this.label3.Text = "sand";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(15, 127);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(19, 13);
            this.label4.TabIndex = 3;
            this.label4.Text = "silt";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(12, 153);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(61, 13);
            this.label5.TabIndex = 4;
            this.label5.Text = "coarse clay";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(12, 179);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(46, 13);
            this.label6.TabIndex = 5;
            this.label6.Text = "fine clay";
            // 
            // coarsebox
            // 
            this.coarsebox.Location = new System.Drawing.Point(125, 72);
            this.coarsebox.Name = "coarsebox";
            this.coarsebox.Size = new System.Drawing.Size(82, 20);
            this.coarsebox.TabIndex = 6;
            this.coarsebox.Text = "25";
            this.coarsebox.TextChanged += new System.EventHandler(this.textBox1_TextChanged);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(125, 56);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(82, 13);
            this.label7.TabIndex = 7;
            this.label7.Text = "mass percent [-]";
            // 
            // sandbox
            // 
            this.sandbox.Location = new System.Drawing.Point(125, 98);
            this.sandbox.Name = "sandbox";
            this.sandbox.Size = new System.Drawing.Size(82, 20);
            this.sandbox.TabIndex = 8;
            this.sandbox.Text = "25";
            this.sandbox.TextChanged += new System.EventHandler(this.textBox2_TextChanged);
            // 
            // siltbox
            // 
            this.siltbox.Location = new System.Drawing.Point(125, 124);
            this.siltbox.Name = "siltbox";
            this.siltbox.Size = new System.Drawing.Size(82, 20);
            this.siltbox.TabIndex = 9;
            this.siltbox.Text = "25";
            this.siltbox.TextChanged += new System.EventHandler(this.textBox3_TextChanged);
            // 
            // claybox
            // 
            this.claybox.Location = new System.Drawing.Point(125, 150);
            this.claybox.Name = "claybox";
            this.claybox.Size = new System.Drawing.Size(82, 20);
            this.claybox.TabIndex = 10;
            this.claybox.Text = "25";
            this.claybox.TextChanged += new System.EventHandler(this.textBox4_TextChanged);
            // 
            // fineclaybox
            // 
            this.fineclaybox.Location = new System.Drawing.Point(125, 176);
            this.fineclaybox.Name = "fineclaybox";
            this.fineclaybox.Size = new System.Drawing.Size(82, 20);
            this.fineclaybox.TabIndex = 11;
            this.fineclaybox.Text = "0";
            this.fineclaybox.TextChanged += new System.EventHandler(this.textBox5_TextChanged);
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(15, 257);
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
            this.button1.Location = new System.Drawing.Point(398, 250);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(69, 24);
            this.button1.TabIndex = 14;
            this.button1.Text = "done";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // upper_particle_fine_clay_textbox
            // 
            this.upper_particle_fine_clay_textbox.Location = new System.Drawing.Point(220, 176);
            this.upper_particle_fine_clay_textbox.Name = "upper_particle_fine_clay_textbox";
            this.upper_particle_fine_clay_textbox.Size = new System.Drawing.Size(100, 20);
            this.upper_particle_fine_clay_textbox.TabIndex = 23;
            this.upper_particle_fine_clay_textbox.Text = "0.0000001";
            // 
            // upper_particle_clay_textbox
            // 
            this.upper_particle_clay_textbox.Location = new System.Drawing.Point(220, 150);
            this.upper_particle_clay_textbox.Name = "upper_particle_clay_textbox";
            this.upper_particle_clay_textbox.Size = new System.Drawing.Size(100, 20);
            this.upper_particle_clay_textbox.TabIndex = 22;
            this.upper_particle_clay_textbox.Text = "0.000002";
            // 
            // upper_particle_silt_textbox
            // 
            this.upper_particle_silt_textbox.Location = new System.Drawing.Point(220, 124);
            this.upper_particle_silt_textbox.Name = "upper_particle_silt_textbox";
            this.upper_particle_silt_textbox.Size = new System.Drawing.Size(100, 20);
            this.upper_particle_silt_textbox.TabIndex = 21;
            this.upper_particle_silt_textbox.Text = "0.00005";
            // 
            // upper_particle_sand_textbox
            // 
            this.upper_particle_sand_textbox.Location = new System.Drawing.Point(220, 98);
            this.upper_particle_sand_textbox.Name = "upper_particle_sand_textbox";
            this.upper_particle_sand_textbox.Size = new System.Drawing.Size(100, 20);
            this.upper_particle_sand_textbox.TabIndex = 20;
            this.upper_particle_sand_textbox.Text = "0.002";
            // 
            // upper_particle_coarse_textbox
            // 
            this.upper_particle_coarse_textbox.Location = new System.Drawing.Point(220, 72);
            this.upper_particle_coarse_textbox.Name = "upper_particle_coarse_textbox";
            this.upper_particle_coarse_textbox.Size = new System.Drawing.Size(100, 20);
            this.upper_particle_coarse_textbox.TabIndex = 19;
            this.upper_particle_coarse_textbox.Text = "0.01";
            // 
            // specific_area_fine_clay_textbox
            // 
            this.specific_area_fine_clay_textbox.Location = new System.Drawing.Point(367, 176);
            this.specific_area_fine_clay_textbox.Name = "specific_area_fine_clay_textbox";
            this.specific_area_fine_clay_textbox.Size = new System.Drawing.Size(100, 20);
            this.specific_area_fine_clay_textbox.TabIndex = 35;
            this.specific_area_fine_clay_textbox.Text = "100000";
            // 
            // specific_area_clay_textbox
            // 
            this.specific_area_clay_textbox.Location = new System.Drawing.Point(367, 150);
            this.specific_area_clay_textbox.Name = "specific_area_clay_textbox";
            this.specific_area_clay_textbox.Size = new System.Drawing.Size(100, 20);
            this.specific_area_clay_textbox.TabIndex = 34;
            this.specific_area_clay_textbox.Text = "50000";
            // 
            // specific_area_silt_textbox
            // 
            this.specific_area_silt_textbox.Location = new System.Drawing.Point(367, 124);
            this.specific_area_silt_textbox.Name = "specific_area_silt_textbox";
            this.specific_area_silt_textbox.Size = new System.Drawing.Size(100, 20);
            this.specific_area_silt_textbox.TabIndex = 33;
            this.specific_area_silt_textbox.Text = "1000";
            // 
            // specific_area_sand_textbox
            // 
            this.specific_area_sand_textbox.Location = new System.Drawing.Point(367, 98);
            this.specific_area_sand_textbox.Name = "specific_area_sand_textbox";
            this.specific_area_sand_textbox.Size = new System.Drawing.Size(100, 20);
            this.specific_area_sand_textbox.TabIndex = 32;
            this.specific_area_sand_textbox.Text = "100";
            // 
            // specific_area_coarse_textbox
            // 
            this.specific_area_coarse_textbox.Location = new System.Drawing.Point(367, 72);
            this.specific_area_coarse_textbox.Name = "specific_area_coarse_textbox";
            this.specific_area_coarse_textbox.Size = new System.Drawing.Size(100, 20);
            this.specific_area_coarse_textbox.TabIndex = 31;
            this.specific_area_coarse_textbox.Text = "10";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(12, 204);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(56, 13);
            this.label9.TabIndex = 42;
            this.label9.Text = "young OM";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(12, 231);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(41, 13);
            this.label10.TabIndex = 43;
            this.label10.Text = "old OM";
            // 
            // yombox
            // 
            this.yombox.Location = new System.Drawing.Point(125, 202);
            this.yombox.Name = "yombox";
            this.yombox.Size = new System.Drawing.Size(82, 20);
            this.yombox.TabIndex = 44;
            this.yombox.Text = "0";
            this.yombox.TextChanged += new System.EventHandler(this.yombox_TextChanged);
            // 
            // oombox
            // 
            this.oombox.Location = new System.Drawing.Point(125, 228);
            this.oombox.Name = "oombox";
            this.oombox.Size = new System.Drawing.Size(82, 20);
            this.oombox.TabIndex = 45;
            this.oombox.Text = "0";
            this.oombox.TextChanged += new System.EventHandler(this.oombox_TextChanged);
            // 
            // Soil_specifier
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(535, 289);
            this.Controls.Add(this.oombox);
            this.Controls.Add(this.yombox);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.label9);
            this.Controls.Add(label54);
            this.Controls.Add(this.specific_area_fine_clay_textbox);
            this.Controls.Add(this.specific_area_clay_textbox);
            this.Controls.Add(this.specific_area_silt_textbox);
            this.Controls.Add(this.specific_area_sand_textbox);
            this.Controls.Add(this.specific_area_coarse_textbox);
            this.Controls.Add(label49);
            this.Controls.Add(this.upper_particle_fine_clay_textbox);
            this.Controls.Add(this.upper_particle_clay_textbox);
            this.Controls.Add(this.upper_particle_silt_textbox);
            this.Controls.Add(this.upper_particle_sand_textbox);
            this.Controls.Add(this.upper_particle_coarse_textbox);
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
        public System.Windows.Forms.TextBox upper_particle_fine_clay_textbox;
        public System.Windows.Forms.TextBox upper_particle_clay_textbox;
        public System.Windows.Forms.TextBox upper_particle_silt_textbox;
        public System.Windows.Forms.TextBox upper_particle_sand_textbox;
        public System.Windows.Forms.TextBox upper_particle_coarse_textbox;
        public System.Windows.Forms.TextBox specific_area_fine_clay_textbox;
        public System.Windows.Forms.TextBox specific_area_clay_textbox;
        public System.Windows.Forms.TextBox specific_area_silt_textbox;
        public System.Windows.Forms.TextBox specific_area_sand_textbox;
        public System.Windows.Forms.TextBox specific_area_coarse_textbox;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label10;
        public System.Windows.Forms.TextBox yombox;
        public System.Windows.Forms.TextBox oombox;
    }
}