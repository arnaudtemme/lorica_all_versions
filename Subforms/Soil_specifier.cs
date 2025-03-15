using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace LORICA4
{
    public partial class Soil_specifier : Form
    {
        private double sum_perc(string one, string two, string three, string four, string five, string six, string seven)
        {
            double sumtext = System.Convert.ToDouble(one) + System.Convert.ToDouble(two) + System.Convert.ToDouble(three) 
                + System.Convert.ToDouble(four) + System.Convert.ToDouble(five) + System.Convert.ToDouble(six) 
                + System.Convert.ToDouble(seven);
            return (sumtext);
        }

        
        public Soil_specifier()
        {
            InitializeComponent();
            this.ControlBox = false;
            total_box.Text = System.Convert.ToString(sum_perc(coarsebox.Text, sandbox.Text, siltbox.Text, claybox.Text, fineclaybox.Text,yombox.Text, oombox.Text));
        }

        public void textBox1_TextChanged(object sender, EventArgs e)
        {
            try
            {
                total_box.Text = System.Convert.ToString(sum_perc(coarsebox.Text, sandbox.Text, siltbox.Text, claybox.Text, fineclaybox.Text, yombox.Text, oombox.Text));
            }
            catch { };
        }

        public void textBox2_TextChanged(object sender, EventArgs e)
        {
            try
            {
                total_box.Text = System.Convert.ToString(sum_perc(coarsebox.Text, sandbox.Text, siltbox.Text, claybox.Text, fineclaybox.Text, yombox.Text, oombox.Text));
            }
            catch { };
        }

        public void textBox3_TextChanged(object sender, EventArgs e)
        {
            try
            {
                total_box.Text = System.Convert.ToString(sum_perc(coarsebox.Text, sandbox.Text, siltbox.Text, claybox.Text, fineclaybox.Text, yombox.Text, oombox.Text));
            }
            catch { };
        }

        public void textBox4_TextChanged(object sender, EventArgs e)
        {
            try
            {
                total_box.Text = System.Convert.ToString(sum_perc(coarsebox.Text, sandbox.Text, siltbox.Text, claybox.Text, fineclaybox.Text, yombox.Text, oombox.Text));
            }
            catch { };
        }

        public void textBox5_TextChanged(object sender, EventArgs e)
        {
            try
            {
                total_box.Text = System.Convert.ToString(sum_perc(coarsebox.Text, sandbox.Text, siltbox.Text, claybox.Text, fineclaybox.Text, yombox.Text, oombox.Text));
            }
            catch { };
        }

        public void button1_Click(object sender, EventArgs e)
        {
            try
            {

                if (100 == sum_perc(coarsebox.Text, sandbox.Text, siltbox.Text, claybox.Text, fineclaybox.Text, yombox.Text, oombox.Text)) 
                {
                    this.Visible = false;
                }
                else
                {
                    MessageBox.Show(" percentages do not add up to 100. Please improve");
                }
            }
            catch { MessageBox.Show(" percentages contain an error. Please improve"); }
        }

        private void yombox_TextChanged(object sender, EventArgs e)
        {
            try
            {
                total_box.Text = System.Convert.ToString(sum_perc(coarsebox.Text, sandbox.Text, siltbox.Text, claybox.Text, fineclaybox.Text, yombox.Text, oombox.Text));
            }
            catch { };
        }

        private void oombox_TextChanged(object sender, EventArgs e)
        {
            try
            {
                total_box.Text = System.Convert.ToString(sum_perc(coarsebox.Text, sandbox.Text, siltbox.Text, claybox.Text, fineclaybox.Text, yombox.Text, oombox.Text));
            }
            catch { };
        }
    }
}
