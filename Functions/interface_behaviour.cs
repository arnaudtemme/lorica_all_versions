using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LORICA4
{
    public partial class Mother_form
    {

        #region interface behaviour code

        private void End_button_Click(object sender, EventArgs e)
        {
            this.Close();
        }

         private void timeseries_button_Click(object sender, EventArgs e)
        {
            timeseries.Visible = true;
        }

        private void landuse_determinator_button_Click(object sender, EventArgs e)
        {
            landuse_determinator.Visible = true;
        }

        private void Water_ero_checkbox_CheckedChanged(object sender, EventArgs e)
        {
            if (Water_ero_checkbox.Checked == false)
            {
                only_waterflow_checkbox.Enabled = false;
                only_waterflow_checkbox.Checked = false;
            }
            if (Water_ero_checkbox.Checked == true) { only_waterflow_checkbox.Enabled = true; }
        }

        private void Form1_Load(object sender, System.EventArgs e)
        {
            //JMW <20040929 -start>
            this.Text = basetext;
            //DoingGraphics = false;
            //JMW <20040929 - end>

        }

        private void check_cnst_soildepth_CheckedChanged_1(object sender, EventArgs e)
        {
            if (check_space_soildepth.Checked == true) // time can never be true,  because the model calculates soildepth
            {
                soildepth_constant_value_box.Enabled = false;
                soildepth_input_filename_textbox.Enabled = true;
            }
            else
            {
                soildepth_constant_value_box.Enabled = true;
                soildepth_input_filename_textbox.Enabled = false;
            }
        }

        private void check_cnst_landuse_CheckedChanged_1(object sender, EventArgs e)
        {
            if (check_space_landuse.Checked == true)
            {
                landuse_constant_value_box.Enabled = false;
                landuse_input_filename_textbox.Enabled = true;
                check_time_landuse.Checked = false;
            }
            if (check_space_landuse.Checked == false && check_time_landuse.Checked == false)
            {
                landuse_constant_value_box.Enabled = true;
                landuse_input_filename_textbox.Enabled = false;
            }
        }

        private void check_cnst_till_fields_CheckedChanged(object sender, EventArgs e)
        {

            if (check_space_till_fields.Checked == true)
            {
                tillfields_constant_textbox.Enabled = false;
                tillfields_input_filename_textbox.Enabled = true;
                check_time_till_fields.Checked = false;
            }
            if (check_space_till_fields.Checked == false && check_time_till_fields.Checked == false)
            {
                tillfields_constant_textbox.Enabled = true;
                tillfields_input_filename_textbox.Enabled = false;
            }
        }

        private void check_cnst_rain_CheckedChanged_1(object sender, EventArgs e)
        {
            if (check_space_rain.Checked == true)
            {
                rainfall_constant_value_box.Enabled = false;
                rain_input_filename_textbox.Enabled = true;
                check_time_rain.Checked = false;
            }
            if (check_space_rain.Checked == false && check_time_rain.Checked == false)
            {
                rainfall_constant_value_box.Enabled = true;
                rain_input_filename_textbox.Enabled = false;
            }
        }

        private void check_cnst_infil_CheckedChanged(object sender, EventArgs e)
        {
            if (check_space_infil.Checked == true)
            {
                infil_constant_value_box.Enabled = false;
                infil_input_filename_textbox.Enabled = true;
                check_time_infil.Checked = false;
            }
            if (check_space_infil.Checked == false && check_time_infil.Checked == false)
            {
                infil_constant_value_box.Enabled = true;
                infil_input_filename_textbox.Enabled = false;
            }
        }

        private void check_cnst_evap_CheckedChanged(object sender, EventArgs e)
        {
            if (check_space_evap.Checked == true)
            {
                evap_constant_value_box.Enabled = false;
                evap_input_filename_textbox.Enabled = true;
                check_time_evap.Checked = false;
            }
            if (check_space_evap.Checked == false && check_time_evap.Checked == false)
            {
                evap_constant_value_box.Enabled = true;
                evap_input_filename_textbox.Enabled = false;
            }
        }

        private void check_time_landuse_CheckedChanged(object sender, EventArgs e)
        {
            if (check_time_landuse.Checked == true)
            {
                landuse_constant_value_box.Enabled = false;
                landuse_input_filename_textbox.Enabled = true;
                check_space_landuse.Checked = false;
            }
            if (check_space_landuse.Checked == false && check_time_landuse.Checked == false)
            {
                landuse_constant_value_box.Enabled = true;
                landuse_input_filename_textbox.Enabled = false;
            }
        }

        private void check_time_tillage_CheckedChanged(object sender, EventArgs e)
        {
            if (check_time_till_fields.Checked == true) // time can only be true when space is also true
            {

                tillfields_constant_textbox.Enabled = false;
                tillfields_input_filename_textbox.Enabled = true;
                check_space_till_fields.Checked = false;
            }
            if (check_space_till_fields.Checked == false && check_time_till_fields.Checked == false)
            {
                tillfields_constant_textbox.Enabled = true;
                tillfields_input_filename_textbox.Enabled = false;
            }
        }

        private void check_time_rain_CheckedChanged(object sender, EventArgs e)
        {
            if (check_time_rain.Checked == true)
            {
                rainfall_constant_value_box.Enabled = false;
                rain_input_filename_textbox.Enabled = true;
                check_space_rain.Checked = false;
            }
            if (check_space_rain.Checked == false && check_time_rain.Checked == false)
            {
                rainfall_constant_value_box.Enabled = true;
                rain_input_filename_textbox.Enabled = false;
            }
        }

        private void check_time_infil_CheckedChanged(object sender, EventArgs e)
        {
            if (check_time_infil.Checked == true)
            {
                infil_constant_value_box.Enabled = false;
                infil_input_filename_textbox.Enabled = true;
                check_space_infil.Checked = false;
            }
            if (check_space_infil.Checked == false && check_time_infil.Checked == false)
            {
                infil_constant_value_box.Enabled = true;
                infil_input_filename_textbox.Enabled = false;
            }
        }

        private void check_time_evap_CheckedChanged(object sender, EventArgs e)
        {
            if (check_time_evap.Checked == true)
            {
                evap_constant_value_box.Enabled = false;
                evap_input_filename_textbox.Enabled = true;
                check_space_evap.Checked = false;
            }
            if (check_space_evap.Checked == false && check_time_evap.Checked == false)
            {
                evap_constant_value_box.Enabled = true;
                evap_input_filename_textbox.Enabled = false;
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {

            //MessageBox.Show()

            /*"Input filenames are not available when both f(row,col) and f(t) are unchecked. In that case, only single values are input
LORICA will use filename in the following way:

1. When f(row,col) is checked but f(t) is not checked, filename is the ascii grid that will be read.
Example: filename = use.asc ; LORICA will read use.asc

2. When f(row,col) and f(t) are checked, filename is the prefix for 
a series of ascii grid files with the timestep following the prefix. 
Example: filename = use.asc ; LORICA will read use1.asc, use2.asc, use3.asc etc

3. When f(row,col) is not checked, but f(t) is checked, filename is the text file containing 
(spatially uniform) timeseries. The number of values in this file should at least equal 
the number of timesteps in the run. LORICA will start using the first value.
Example: rainfall.asc can look like:
0.67
0.54
0.87
0.70
" */
        }

        private void dtm_input_filename_textbox_Click(object sender, EventArgs e)
        {

            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.InitialDirectory = workdir;
            openFileDialog1.Filter = "Ascii grids (*.asc)|*.asc|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.RestoreDirectory = false;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                dtm_input_filename_textbox.Text = openFileDialog1.FileName;
            }
        }

        private void soildepth_input_filename_textbox_TextChanged(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.InitialDirectory = workdir;
            openFileDialog1.Filter = "Ascii grids (*.asc)|*.asc|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.RestoreDirectory = false;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                soildepth_input_filename_textbox.Text = openFileDialog1.FileName;
            }
        }

        private void landuse_input_filename_textbox_TextChanged(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.InitialDirectory = workdir;
            openFileDialog1.Filter = "Ascii grids (*.asc)|*.asc|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.RestoreDirectory = false;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                landuse_input_filename_textbox.Text = openFileDialog1.FileName;
            }
        }

        private void tillfields_input_filename_textbox_TextChanged(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.InitialDirectory = workdir;
            openFileDialog1.Filter = "Ascii grids (*.asc)|*.asc|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.RestoreDirectory = false;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                tillfields_input_filename_textbox.Text = openFileDialog1.FileName;
            }
        }

        private void rain_input_filename_textbox_TextChanged(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.InitialDirectory = workdir;
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.RestoreDirectory = false;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                rain_input_filename_textbox.Text = openFileDialog1.FileName;
            }
        }

        private void infil_input_filename_textbox_TextChanged(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.InitialDirectory = workdir;
            openFileDialog1.Filter = "Ascii grids (*.asc)|*.asc|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.RestoreDirectory = false;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                infil_input_filename_textbox.Text = openFileDialog1.FileName;
            }
        }

        private void evap_input_filename_textbox_TextChanged(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.InitialDirectory = workdir;
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.RestoreDirectory = false;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                evap_input_filename_textbox.Text = openFileDialog1.FileName;
            }
        }

        private void dailyP_TextChanged(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.InitialDirectory = workdir;
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.RestoreDirectory = false;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                dailyP.Text = openFileDialog1.FileName;
            }
        }

        private void dailyET0_TextChanged(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.InitialDirectory = workdir;
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.RestoreDirectory = false;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                dailyET0.Text = openFileDialog1.FileName;
            }
        }

        private void dailyD_TextChanged(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.InitialDirectory = workdir;
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.RestoreDirectory = false;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                dailyD.Text = openFileDialog1.FileName;
            }
        }

        private void proglacial_input_filename_textbox_TextChanged(object sender, EventArgs e)  
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.InitialDirectory = workdir;
            openFileDialog1.Filter = "Ascii grids (*.asc)|*.asc|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.RestoreDirectory = false;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                proglacial_input_filename_textbox.Text = openFileDialog1.FileName;
            }
        }

        private void coarsemap_input_filename_textbox_TextChanged(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.InitialDirectory = workdir;
            openFileDialog1.Filter = "Ascii grids (*.asc)|*.asc|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.RestoreDirectory = false;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                coarsemap_input_filename_textbox.Text = openFileDialog1.FileName;
            }

        }

        private void soil_specify_button_Click(object sender, EventArgs e)
        {
            soildata.Visible = true;
        }

        #endregion

    }
}
