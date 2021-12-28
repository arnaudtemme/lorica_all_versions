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
        public Soil_specifier()
        {
            InitializeComponent();
            double sum_perc = System.Convert.ToDouble(coarsebox.Text) + System.Convert.ToDouble(sandbox.Text) + System.Convert.ToDouble(siltbox.Text) + System.Convert.ToDouble(claybox.Text) + System.Convert.ToDouble(fineclaybox.Text);
            total_box.Text = System.Convert.ToString(sum_perc);
        }

        public void textBox1_TextChanged(object sender, EventArgs e)
        {
            try
            {
                double sum_perc = System.Convert.ToDouble(coarsebox.Text) + System.Convert.ToDouble(sandbox.Text) + System.Convert.ToDouble(siltbox.Text) + System.Convert.ToDouble(claybox.Text) + System.Convert.ToDouble(fineclaybox.Text);
                total_box.Text = System.Convert.ToString(sum_perc);
            }
            catch { };
        }

        public void textBox2_TextChanged(object sender, EventArgs e)
        {
            try
            {
                double sum_perc = System.Convert.ToDouble(coarsebox.Text) + System.Convert.ToDouble(sandbox.Text) + System.Convert.ToDouble(siltbox.Text) + System.Convert.ToDouble(claybox.Text) + System.Convert.ToDouble(fineclaybox.Text);
                total_box.Text = System.Convert.ToString(sum_perc);
            }
            catch { };
        }

        public void textBox3_TextChanged(object sender, EventArgs e)
        {
            try
            {
                double sum_perc = System.Convert.ToDouble(coarsebox.Text) + System.Convert.ToDouble(sandbox.Text) + System.Convert.ToDouble(siltbox.Text) + System.Convert.ToDouble(claybox.Text) + System.Convert.ToDouble(fineclaybox.Text);
                total_box.Text = System.Convert.ToString(sum_perc);
            }
            catch { };
        }

        public void textBox4_TextChanged(object sender, EventArgs e)
        {
            try
            {
                double sum_perc = System.Convert.ToDouble(coarsebox.Text) + System.Convert.ToDouble(sandbox.Text) + System.Convert.ToDouble(siltbox.Text) + System.Convert.ToDouble(claybox.Text) + System.Convert.ToDouble(fineclaybox.Text);
                total_box.Text = System.Convert.ToString(sum_perc);
            }
            catch { };
        }

        public void textBox5_TextChanged(object sender, EventArgs e)
        {
            try
            {
                double sum_perc = System.Convert.ToDouble(coarsebox.Text) + System.Convert.ToDouble(sandbox.Text) + System.Convert.ToDouble(siltbox.Text) + System.Convert.ToDouble(claybox.Text) + System.Convert.ToDouble(fineclaybox.Text);
                total_box.Text = System.Convert.ToString(sum_perc);
            }
            catch { };
        }

        public void button1_Click(object sender, EventArgs e)
        {
            try
            {
                double sum_perc = System.Convert.ToDouble(coarsebox.Text) + System.Convert.ToDouble(sandbox.Text) + System.Convert.ToDouble(siltbox.Text) + System.Convert.ToDouble(claybox.Text) + System.Convert.ToDouble(fineclaybox.Text);
                if (sum_perc == 100)
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

    }
}
