using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace LORICA4
{
    public partial class Output_timeseries : Form
    {
        public double timeseries_waterflow_threshold = 0;
        public double timeseries_deposition_threshold = 0;
        public double timeseries_erosion_threshold = 0;

        public Output_timeseries()
        {
            InitializeComponent();
        }

        private void Timeseries_output_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            if(timeseries_number_waterflow_check.Checked){
                try { timeseries_waterflow_threshold = System.Convert.ToDouble(timeseries_textbox_waterflow_threshold.Text); }
                catch { MessageBox.Show("invalid waterflow threshold"); }
                if (timeseries_waterflow_threshold < 0) { MessageBox.Show("invalid waterflow threshold"); }
            }
            if (timeseries_number_dep_check.Checked)
            {
                try { timeseries_deposition_threshold = System.Convert.ToDouble(timeseries_textbox_deposition_threshold.Text); }
                catch { MessageBox.Show("invalid deposition threshold"); }
                if (timeseries_deposition_threshold < 0) { MessageBox.Show("invalid deposition threshold"); }
            }
            if (timeseries_number_erosion_check.Checked)
            {
                try { timeseries_erosion_threshold = System.Convert.ToDouble(timeseries_textbox_erosion_threshold.Text); }
                catch { MessageBox.Show("invalid erosion threshold"); }
                if (timeseries_erosion_threshold < 0) { MessageBox.Show("invalid erosion threshold"); }
            }
            this.Visible = false;
        }

    }
}
