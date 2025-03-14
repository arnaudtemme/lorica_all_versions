using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace LORICA4
{
    public partial class Output_profile : Form
    {
        public Output_profile()
        {
            InitializeComponent();
        }

        public double profile1_parameter = 0, profile2_parameter = 0, profile3_parameter = 0;
        
        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (radio_pro1_row.Checked) { radio_pro1_col.Checked = false; }
            else { radio_pro1_col.Checked = true; }
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (radio_pro1_col.Checked) { radio_pro1_row.Checked = false; }
            else { radio_pro1_row.Checked = true; }
        }

        private void radioButton4_CheckedChanged(object sender, EventArgs e)
        {
            if (radio_pro2_row.Checked) { radio_pro2_col.Checked = false; }
            else { radio_pro2_col.Checked = true; }
        }

        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
            if (radio_pro2_col.Checked) { radio_pro2_row.Checked = false; }
            else { radio_pro2_row.Checked = true; }
        }

        private void radioButton6_CheckedChanged(object sender, EventArgs e)
        {
            if (radio_pro3_row.Checked) { radio_pro3_col.Checked = false; }
            else { radio_pro3_col.Checked = true; }
        }

        private void radioButton5_CheckedChanged(object sender, EventArgs e)
        {
            if (radio_pro3_col.Checked) { radio_pro3_row.Checked = false; }
            else { radio_pro3_row.Checked = true; }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (radio_pro1_col.Checked || radio_pro1_row.Checked)
            {
                try { profile1_parameter = System.Convert.ToInt32(p1_row_col_box.Text); }
                catch { 
                    MessageBox.Show("invalid row-col value for profile 1");
                    radio_pro1_col.Checked = false;
                    radio_pro1_row.Checked = false;
                }
                if (profile1_parameter <= 0) { 
                    MessageBox.Show("invalid row-col value for profile 1");
                    radio_pro1_col.Checked = false;
                    radio_pro1_row.Checked = false;
                }
            }
            if (radio_pro2_col.Checked || radio_pro2_row.Checked)
            {
                try { profile2_parameter = System.Convert.ToInt32(p2_row_col_box.Text); }
                catch
                {
                    MessageBox.Show("invalid row-col value for profile 2");
                    radio_pro2_col.Checked = false;
                    radio_pro2_row.Checked = false;
                }
                if (profile2_parameter <= 0)
                {
                    MessageBox.Show("invalid row-col value for profile 2");
                    radio_pro2_col.Checked = false;
                    radio_pro2_row.Checked = false;
                }
            }
            if (radio_pro3_col.Checked || radio_pro3_row.Checked)
            {
                try { profile3_parameter = System.Convert.ToInt32(p3_row_col_box.Text); }
                catch
                {
                    MessageBox.Show("invalid row-col value for profile 3");
                    radio_pro3_col.Checked = false;
                    radio_pro3_row.Checked = false;
                }
                if (profile3_parameter <= 0)
                {
                    MessageBox.Show("invalid row-col value for profile 3");
                    radio_pro3_col.Checked = false;
                    radio_pro3_row.Checked = false;
                }
            }
            this.Visible = false;
        }
    }
}
