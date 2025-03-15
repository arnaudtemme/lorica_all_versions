using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace LORICA4
{
    public partial class Mother_form
    {
        void calculate_terrain_derivatives()
        {
            //takes the DTM and calculates key derivatives and writes these to ASCII files
            try { dtm_file(dtm_input_filename_textbox.Text); }
            catch { Debug.WriteLine("could not read DEM for derivative calculation"); }

            //declare rasters and memory
            double[,] ledges, nedges, hedges, hhcliff, hlcliff, slhcliff, sllcliff, terruggedindex, ledgeheight;
            int[,] ledgenames;
            terruggedindex = new double[nr, nc];
            ledges = new double[nr, nc];
            nedges = new double[nr, nc];
            hedges = new double[nr, nc];
            hhcliff = new double[nr, nc];
            hlcliff = new double[nr, nc];
            slhcliff = new double[nr, nc];
            sllcliff = new double[nr, nc];
            ledgeheight = new double[nr, nc];
            ledgenames = new int[nr, nc];
            int runner = 0;

            //Topographic Ruggedness Index (Riley, S.J., DeGloria, S.D., Elliot, R., 1999. A terrain ruggedness index that quantifies topographic heterogeneity. Intermt. J. Sci. 5, 2327.)
            double sum_squared_difference = 0; int num_nbs = 0;
            try
            {
                for (row = 0; row < nr; row++)
                {
                    for (col = 0; col < nc; col++)
                    {
                        if (dtm[row, col] != -9999)
                        {
                            sum_squared_difference = 0;
                            num_nbs = 0;
                            for (i = (-1); i <= 1; i++)
                            {
                                for (j = (-1); j <= 1; j++)
                                {
                                    if (!(i == 0 && j == 0) && (row + i) >= 0 && (col + j) >= 0 && (row + i) < nr && (col + j) < nc)
                                    {
                                        if (dtm[row + i, col + j] != -9999)
                                        {
                                            sum_squared_difference += Math.Pow((dtm[row, col] - dtm[row + i, col + j]), 2);
                                            num_nbs++;
                                        }
                                    }
                                }
                            }
                            if (num_nbs == 0) { terruggedindex[row, col] = -9999; }
                            else { terruggedindex[row, col] = Math.Sqrt(sum_squared_difference) * (8 / num_nbs); }
                        }
                    }
                }
                out_double("ruggednessindex.asc", terruggedindex);
                Debug.WriteLine("terrain ruggedness index calculation and storage successfull");
            }
            catch { Debug.WriteLine("terrain ruggedness index calculation or storage failed"); }

            // Properties of possible ledges on the hillslope above and below each cell.
            //We need to ingest ledge positions
            try { read_integer("ledgenames.asc", ledgenames); Debug.WriteLine("ledgenames read successfully"); }
            catch { Debug.WriteLine("ledgenames not found"); }

            //then calculate local properties of the landscape around ledges. We expect that ledge positions may be up to 1 cell wrong.
            double maxcliffheight = 0;
            for (row = 0; row < nr; row++)
            {
                for (col = 0; col < nc; col++)
                {
                    ledges[row, col] = -9999;
                    nedges[row, col] = -9999;
                    hedges[row, col] = -9999;
                    hhcliff[row, col] = -9999;
                    hlcliff[row, col] = -9999;
                    slhcliff[row, col] = -9999;
                    sllcliff[row, col] = -9999;
                    ledgeheight[row, col] = -9999;
                    if (dtm[row, col] != -9999)
                    {
                        ledges[row, col] = 0;
                        nedges[row, col] = 0;
                        hedges[row, col] = 0;
                        hhcliff[row, col] = 0;
                        hlcliff[row, col] = 0;
                        slhcliff[row, col] = 0;
                        sllcliff[row, col] = 0;
                        ledgeheight[row, col] = 0;
                        if (ledgenames[row, col] != -9999)
                        {
                            try
                            {
                                maxcliffheight = 0;
                                for (i = (-1); i <= 1; i++)
                                {
                                    for (j = (-1); j <= 1; j++)
                                    {
                                        if (!(i == 0 && j == 0) && row + i >= 0 && col + j >= 0 && row + i < nr && col + j < nc)
                                        {
                                            if (dtm[row + i, col + j] != -9999)
                                            {
                                                for (ii = (-1); ii <= 1; ii++)
                                                {
                                                    for (jj = (-1); jj <= 1; jj++)
                                                    {
                                                        if (!(i + ii == 0 && j + jj == 0) && row + i + ii >= 0 && col + j + jj >= 0 && row + i + ii < nr && col + j + jj < nc)
                                                        {
                                                            if (dtm[row + i + ii, col + j + jj] != -9999)
                                                            {
                                                                if (Math.Abs(dtm[row + i + ii, col + j + jj] - dtm[row + i, col + j]) > maxcliffheight) { maxcliffheight = Math.Abs(dtm[row + i + ii, col + j + jj] - dtm[row + i, col + j]); }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                ledgeheight[row, col] = maxcliffheight;
                            }
                            catch { }
                        }
                    }
                }
            }
            Debug.WriteLine("ledgeheights determined");

            for (row = 0; row < nr; row++)
            {
                for (col = 0; col < nc; col++)
                {
                    if (dtm[row, col] != -9999)
                    {
                        if (ledgeheight[row, col] == -9999)
                        {
                            double highestledgeheight = 0;
                            for (i = (-1); i <= 1; i++)
                            {
                                for (j = (-1); j <= 1; j++)
                                {
                                    if (!(i == 0 && j == 0) && row + i >= 0 && col + j >= 0 && row + i < nr && col + j < nc)
                                    {
                                        if (ledgeheight[row + i, col + j] > highestledgeheight)
                                        {
                                            highestledgeheight = ledgeheight[row + i, col + j];
                                        }

                                    }
                                }
                            }
                            ledgeheight[row + i, col + j] = highestledgeheight;
                        }
                    }
                }
            }

            //now, we sort the dtm from high to low and walk through it from high to low to assign ledge properties to 
            comb_sort();
            for (runner = number_of_data_cells - 1; runner >= 0; runner--)
            {     // the index is sorted from low to high values, but flow goes from high to low
                if (index[runner] != -9999)
                {
                    row = row_index[runner]; col = col_index[runner];
                    //Debug.WriteLine("now at row " + row + " col " + col + " alt " + dtm[row, col]);
                    if (ledgenames[row, col] != -9999)
                    {
                        //we are on a ledge. Setting and resetting time
                        hhcliff[row, col] = ledgeheight[row, col];
                        slhcliff[row, col] = hhcliff[row, col] / dx;
                        hedges[row, col]++;
                    }
                    else
                    {
                        double tempslhcliff = 0, steepest = 0, steepness, distance, steepdist = 0;
                        for (i = (-1); i <= 1; i++)
                        {
                            for (j = (-1); j <= 1; j++)
                            {
                                if (!(i == 0 && j == 0) && (row + i >= 0) && (col + j >= 0) && (row + i < nr) && (col + j < nc))
                                {
                                    if (dtm[row + i, col + j] != -9999)
                                    {
                                        if (dtm[row + i, col + j] > dtm[row, col])
                                        {
                                            if (i == 0 || j == 0) { distance = dx; } else { distance = dx * 1.414; }
                                            steepness = (dtm[row + i, col + j] - dtm[row, col]) / distance;
                                            if (steepness > steepest)
                                            {
                                                //we copy the cliffheight from the steepest neighbour cell
                                                steepdist = distance;
                                                steepest = steepness;
                                                hhcliff[row, col] = hhcliff[row + i, col + j]; tempslhcliff = slhcliff[row + i, col + j];
                                                hedges[row, col] = hedges[row + i, col + j];
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        //we now have an updated hhcliff, so we can also update slhcliff
                        slhcliff[row, col] = hhcliff[row, col] / (steepdist + hhcliff[row, col] / tempslhcliff);
                    }
                }
            }
            Debug.WriteLine("downslope variables calculated");

            //now, we walk the other way (from low to high in the DTM). 
            for (runner = 0; runner < number_of_data_cells; runner++)
            {
                if (index[runner] != -9999)
                {
                    row = row_index[runner]; col = col_index[runner];
                    if (ledgenames[row, col] != -9999)
                    {
                        //we are on a ledge. Setting and resetting time
                        hlcliff[row, col] = ledgeheight[row, col];
                        sllcliff[row, col] = hlcliff[row, col] / dx;
                        ledges[row, col]++;
                    }
                    else
                    {
                        double tempsllcliff = 0, steepest = 0, steepness = 0, distance = 0, steepdist = 0;
                        for (i = (-1); i <= 1; i++)
                        {
                            for (j = (-1); j <= 1; j++)
                            {
                                if (!(i == 0 && j == 0) && row + i >= 0 && col + j >= 0 && row + i < nr && col + j < nc)
                                {
                                    if (dtm[row + i, col + j] != -9999)
                                    {
                                        if (dtm[row + i, col + j] < dtm[row, col])
                                        {
                                            if (i == 0 || j == 0) { distance = dx; } else { distance = dx * 1.414; }
                                            steepness = -(dtm[row + i, col + j] - dtm[row, col]) / distance;
                                            if (steepness > steepest)
                                            {
                                                //we copy the cliffheight from the steepest neighbour cell
                                                steepdist = distance;
                                                steepest = steepness;
                                                hlcliff[row, col] = hlcliff[row + i, col + j]; tempsllcliff = sllcliff[row + i, col + j];
                                                ledges[row, col] = ledges[row + i, col + j];
                                            }
                                        }

                                    }
                                }
                            }
                        }
                        //we now have an updated hhcliff, so we can also update slhcliff
                        sllcliff[row, col] = hlcliff[row, col] / (steepdist + hlcliff[row, col] / tempsllcliff);
                    }
                }
            }
            Debug.WriteLine("upslope variables calculated");

            //finally, add up ledges and hedges to get nedges
            for (row = 0; row < nr; row++)
            {
                for (col = 0; col < nc; col++)
                {
                    if (dtm[row, col] != -9999)
                    {
                        nedges[row, col] = ledges[row, col] + hedges[row, col];
                    }
                }
            }

            //now write all these rasters to ascii:
            out_double("ledgeheights.asc", ledgeheight);
            out_double("nedges.asc", nedges);
            out_double("hedges.asc", hedges);
            out_double("ledges.asc", ledges);
            out_double("hhcliff.asc", hhcliff);
            out_double("hlcliff.asc", hlcliff);
            out_double("slhcliff.asc", slhcliff);
            out_double("sllcliff.asc", sllcliff);
            Debug.WriteLine("variables exported to ASCII");
        }
        private void timeseries_output()
        {
            int step;
            string FILENAME = workdir + "\\timeseries.log";
            using (StreamWriter sw = new StreamWriter(FILENAME))
            {
                //geomprph centred
                if (timeseries.timeseries_cell_waterflow_check.Checked) { sw.Write("cell_waterflow "); }
                if (timeseries.timeseries_cell_altitude_check.Checked) { sw.Write("cell_altitude "); }
                if (timeseries.timeseries_net_ero_check.Checked) { sw.Write("net_erosion "); }
                if (timeseries.timeseries_number_dep_check.Checked) { sw.Write("deposited_cells "); }
                if (timeseries.timeseries_number_erosion_check.Checked) { sw.Write("eroded_cells "); }
                if (timeseries.timeseries_number_waterflow_check.Checked) { sw.Write("wet_cells "); }
                if (timeseries.timeseries_SDR_check.Checked) { sw.Write("SDR "); }
                if (timeseries.timeseries_total_average_alt_check.Checked) { sw.Write("average_alt "); }
                if (timeseries.timeseries_total_dep_check.Checked) { sw.Write("total_dep "); }
                if (timeseries.timeseries_total_ero_check.Checked) { sw.Write("total_ero "); }
                if (timeseries.timeseries_total_evap_check.Checked) { sw.Write("total_evap "); }
                if (timeseries.timeseries_total_infil_check.Checked) { sw.Write("total_infil "); }
                if (timeseries.timeseries_total_outflow_check.Checked) { sw.Write("total_outflow "); }
                if (timeseries.timeseries_total_rain_check.Checked) { sw.Write("total_rain "); }
                if (timeseries.timeseries_outflow_cells_checkbox.Checked) { sw.Write("number_out_cells"); sw.Write(" "); }
                if (timeseries.timeseries_sedexport_checkbox.Checked) { sw.Write("out_gravel_kg"); sw.Write(" "); }
                if (timeseries.timeseries_sedexport_checkbox.Checked) { sw.Write("out_sand_kg"); sw.Write(" "); }
                if (timeseries.timeseries_sedexport_checkbox.Checked) { sw.Write("out_silt_kg"); sw.Write(" "); }
                if (timeseries.timeseries_sedexport_checkbox.Checked) { sw.Write("out_clay_kg"); sw.Write(" "); }
                if (timeseries.timeseries_sedexport_checkbox.Checked) { sw.Write("out_fineclay_kg"); sw.Write(" "); }
                if (timeseries.timeseries_sedexport_checkbox.Checked) { sw.Write("out_yom_kg"); sw.Write(" "); }
                if (timeseries.timeseries_sedexport_checkbox.Checked) { sw.Write("out_oom_kg"); sw.Write(" "); }
                //soil_centred
                if (timeseries.total_phys_weath_checkbox.Checked) { sw.Write("phys_weath_kg "); }
                if (timeseries.total_chem_weath_checkbox.Checked) { sw.Write("chem_weath_kg "); }
                if (timeseries.total_fine_formed_checkbox.Checked) { sw.Write("fine_clay_formed_kg "); }
                if (timeseries.total_fine_eluviated_checkbox.Checked) { sw.Write("fine_clay_eluviated_kg "); }
                if (timeseries.total_mass_bioturbed_checkbox.Checked) { sw.Write("mass_bioturbed_kg "); }
                if (timeseries.total_OM_input_checkbox.Checked) { sw.Write("OM_input_kg "); }
                if (timeseries.total_average_soilthickness_checkbox.Checked) { sw.Write("average_soilthickness "); }
                if (timeseries.timeseries_number_soil_thicker_checkbox.Checked) { sw.Write("n_soil_thicker "); }
                if (timeseries.timeseries_number_soil_thicker_checkbox.Checked) { sw.Write("n_soil_coarser "); }
                if (timeseries.timeseries_number_soil_thicker_checkbox.Checked) { sw.Write("soil_thickness_m "); }
                if (timeseries.timeseries_number_soil_thicker_checkbox.Checked) { sw.Write("soil_mass_kg "); }

                //slide centred
                if (timeseries.timeseries_slide_checkbox.Checked) { sw.Write("out_slideero_kg"); sw.Write(" "); }
                if (timeseries.timeseries_slide_checkbox.Checked) { sw.Write("out_slidecont_kg"); sw.Write(" "); }
                if (timeseries.timeseries_slide_checkbox.Checked) { sw.Write("out_slidedepo_kg"); sw.Write(" "); }
                if (timeseries.timeseries_slide_checkbox.Checked) { sw.Write("out_slidelost_kg"); sw.Write(" "); }
                if (timeseries.timeseries_slide_checkbox.Checked) { sw.Write("out_slidemeanintensity_m_d"); sw.Write(" "); }
                sw.Write("\r\n");
                for (step = 0; step <= end_time - 1; step++)
                {
                    if (timeseries.timeseries_cell_waterflow_check.Checked) { sw.Write(timeseries_matrix[step, timeseries_order[1]]); sw.Write(" "); }
                    if (timeseries.timeseries_cell_altitude_check.Checked) { sw.Write(timeseries_matrix[step, timeseries_order[2]]); sw.Write(" "); }
                    if (timeseries.timeseries_net_ero_check.Checked) { sw.Write(timeseries_matrix[step, timeseries_order[3]]); sw.Write(" "); }
                    if (timeseries.timeseries_number_dep_check.Checked) { sw.Write(timeseries_matrix[step, timeseries_order[4]]); sw.Write(" "); }
                    if (timeseries.timeseries_number_erosion_check.Checked) { sw.Write(timeseries_matrix[step, timeseries_order[5]]); sw.Write(" "); }
                    if (timeseries.timeseries_number_waterflow_check.Checked) { sw.Write(timeseries_matrix[step, timeseries_order[6]]); sw.Write(" "); }
                    if (timeseries.timeseries_SDR_check.Checked) { sw.Write(timeseries_matrix[step, timeseries_order[7]]); sw.Write(" "); }
                    if (timeseries.timeseries_total_average_alt_check.Checked) { sw.Write(timeseries_matrix[step, timeseries_order[8]]); sw.Write(" "); }
                    if (timeseries.timeseries_total_dep_check.Checked) { sw.Write(timeseries_matrix[step, timeseries_order[9]]); sw.Write(" "); }
                    if (timeseries.timeseries_total_ero_check.Checked) { sw.Write(timeseries_matrix[step, timeseries_order[10]]); sw.Write(" "); }
                    if (timeseries.timeseries_total_evap_check.Checked) { sw.Write(timeseries_matrix[step, timeseries_order[11]]); sw.Write(" "); }
                    if (timeseries.timeseries_total_infil_check.Checked) { sw.Write(timeseries_matrix[step, timeseries_order[12]]); sw.Write(" "); }
                    if (timeseries.timeseries_total_outflow_check.Checked) { sw.Write(timeseries_matrix[step, timeseries_order[13]]); sw.Write(" "); }
                    if (timeseries.timeseries_total_rain_check.Checked) { sw.Write(timeseries_matrix[step, timeseries_order[14]]); sw.Write(" "); }
                    if (timeseries.timeseries_outflow_cells_checkbox.Checked) { sw.Write(timeseries_matrix[step, timeseries_order[15]]); sw.Write(" "); }
                    if (timeseries.timeseries_sedexport_checkbox.Checked) { sw.Write(timeseries_matrix[step, timeseries_order[16]]); sw.Write(" "); }
                    if (timeseries.timeseries_sedexport_checkbox.Checked) { sw.Write(timeseries_matrix[step, timeseries_order[17]]); sw.Write(" "); }
                    if (timeseries.timeseries_sedexport_checkbox.Checked) { sw.Write(timeseries_matrix[step, timeseries_order[18]]); sw.Write(" "); }
                    if (timeseries.timeseries_sedexport_checkbox.Checked) { sw.Write(timeseries_matrix[step, timeseries_order[19]]); sw.Write(" "); }
                    if (timeseries.timeseries_sedexport_checkbox.Checked) { sw.Write(timeseries_matrix[step, timeseries_order[20]]); sw.Write(" "); }
                    if (timeseries.timeseries_sedexport_checkbox.Checked) { sw.Write(timeseries_matrix[step, timeseries_order[21]]); sw.Write(" "); }
                    if (timeseries.timeseries_sedexport_checkbox.Checked) { sw.Write(timeseries_matrix[step, timeseries_order[22]]); sw.Write(" "); }
                    //soil_centred
                    if (timeseries.total_phys_weath_checkbox.Checked) { sw.Write(timeseries_matrix[step, timeseries_order[23]]); sw.Write(" "); }
                    if (timeseries.total_chem_weath_checkbox.Checked) { sw.Write(timeseries_matrix[step, timeseries_order[24]]); sw.Write(" "); }
                    if (timeseries.total_fine_formed_checkbox.Checked) { sw.Write(timeseries_matrix[step, timeseries_order[25]]); sw.Write(" "); }
                    if (timeseries.total_fine_eluviated_checkbox.Checked) { sw.Write(timeseries_matrix[step, timeseries_order[26]]); sw.Write(" "); }
                    if (timeseries.total_mass_bioturbed_checkbox.Checked) { sw.Write(timeseries_matrix[step, timeseries_order[27]]); sw.Write(" "); }
                    if (timeseries.total_OM_input_checkbox.Checked) { sw.Write(timeseries_matrix[step, timeseries_order[28]]); sw.Write(" "); }
                    if (timeseries.total_average_soilthickness_checkbox.Checked) { sw.Write(timeseries_matrix[step, timeseries_order[29]]); sw.Write(" "); }
                    if (timeseries.timeseries_number_soil_thicker_checkbox.Checked) { sw.Write(timeseries_matrix[step, timeseries_order[30]]); sw.Write(" "); }
                    if (timeseries.timeseries_coarser_checkbox.Checked) { sw.Write(timeseries_matrix[step, timeseries_order[31]]); sw.Write(" "); }
                    if (timeseries.timeseries_soil_depth_checkbox.Checked) { sw.Write(timeseries_matrix[step, timeseries_order[32]]); sw.Write(" "); }
                    if (timeseries.timeseries_soil_mass_checkbox.Checked) { sw.Write(timeseries_matrix[step, timeseries_order[33]]); }
                    //slide centered
                    if (timeseries.timeseries_slide_checkbox.Checked) { sw.Write(timeseries_matrix[step, timeseries_order[34]]); sw.Write(" "); }
                    if (timeseries.timeseries_slide_checkbox.Checked) { sw.Write(timeseries_matrix[step, timeseries_order[35]]); ; sw.Write(" "); }
                    if (timeseries.timeseries_slide_checkbox.Checked) { sw.Write(timeseries_matrix[step, timeseries_order[36]]); sw.Write(" "); }
                    if (timeseries.timeseries_slide_checkbox.Checked) { sw.Write(timeseries_matrix[step, timeseries_order[37]]); sw.Write(" "); }
                    if (timeseries.timeseries_slide_checkbox.Checked) { sw.Write(timeseries_matrix[step, timeseries_order[38]]); ; sw.Write(" "); }
                    sw.Write("\r\n");
                }
            }
        }
        void makerecords(string filename)
        {
            string FILE_NAME = filename;
            string input;
            if (!System.IO.File.Exists(FILE_NAME))
            {
                MessageBox.Show("No such data file " + FILE_NAME);
                input_data_error = true;
                return;
            }
            Debug.WriteLine("reading " + filename + " into record ");
            StreamReader sr = System.IO.File.OpenText(FILE_NAME);

            //read first line: number of timesteps
            input = sr.ReadLine();
            int recordsize = 0;
            try { recordsize = Convert.ToInt32(input); }
            catch
            {
                MessageBox.Show("Wrong value " + input + " in first line of record " + FILE_NAME);
                input_data_error = true;
                return;
            }
            if (check_time_rain.Checked) { rainfall_record = new int[recordsize]; }
            if (check_time_evap.Checked) { evap_record = new int[recordsize]; }
            if (check_time_infil.Checked) { infil_record = new int[recordsize]; }
            if (check_time_T.Checked) { temp_record = new int[recordsize]; }
            if (check_time_till_fields.Checked) { till_record = new int[recordsize]; }

            memory_records = true;
        }
        void makedailyrecords(string filename)
        {
            string FILE_NAME = filename;
            string input;
            if (!System.IO.File.Exists(FILE_NAME))
            {
                MessageBox.Show("No such data file " + FILE_NAME);
                input_data_error = true;
                return;
            }
            Debug.WriteLine("reading " + filename + " into record ");
            StreamReader sr = System.IO.File.OpenText(FILE_NAME);

            //read first line: number of timesteps
            input = sr.ReadLine();
            int recordsize = 0;
            try { recordsize = Convert.ToInt32(input); }
            catch
            {
                MessageBox.Show("Wrong value " + input + " in first line of record " + FILE_NAME);
                input_data_error = true;
                return;
            }

            P_all = new int[recordsize];
            ET0_all = new int[recordsize];
            D_all = new int[recordsize];
            Tavg_all = new int[recordsize];
            Tmin_all = new int[recordsize];
            Tmax_all = new int[recordsize];

            memory_records_d = true;
        }
        void dtm_file_test(string name1)
        {
            string FILE_NAME = name1;
            int z, dem_integer_error = 1;
            string[] lineArray2;
            int sp;
            Debug.WriteLine("Opening DEM" + FILE_NAME);

            int ok = clearmatrices_test(); //reset values of existing memory instead of allocating new memory (saves RAM)
            if (ok == 1)
            { // we have now succesfully made memory reservations for all data layers in the model 
                {
                    int col, row, colcounter;
                    String input;
                    double tttt = 0.00;

                    // load dem again

                    if (!System.IO.File.Exists(FILE_NAME))
                    {
                        Debug.WriteLine("No such DEM data file..");
                        input_data_error = true;
                        return;
                    }

                    StreamReader sr = System.IO.File.OpenText(FILE_NAME);

                    //now skip over the headers.
                    for (z = 1; z <= 6; z++)
                    {
                        input = sr.ReadLine();
                    }
                    row = 0;
                    while ((input = sr.ReadLine()) != null)  // so not until nr is reached, but until the file is empty
                    {
                        //Debug.WriteLine("Line " + row);
                        string[] lineArray;
                        lineArray = input.Split(new char[] { ' ' });   // so we split the string that we read (readline) from file into an array of strings that each contain a number
                        col = 0;
                        for (colcounter = 0; colcounter <= (lineArray.Length - 1); colcounter++)  // the length of LineArray should equal nc, and therefore run from 0 to nc-1
                        {

                            //Debug.WriteLine("Col " + col);
                            if (lineArray[colcounter] != "" && col < nc) // but just to make sure, col counts only the non-empty strings in LineArrary (handy for instance when files are double-spaced)
                            {
                                tttt = double.Parse(lineArray[colcounter]);
                                if (Spitsbergen_case_study.Checked == true) { original_dtm[row, col] = tttt; dtm[row, col] = -9999; }
                                else { dtm[row, col] = tttt; }
                                col++;
                                if (double.Parse(lineArray[colcounter]) - Math.Round(double.Parse(lineArray[colcounter])) != 0)
                                {
                                    dem_integer_error = 0;
                                }
                            }
                        }
                        row++;
                    }
                    sr.Close();
                    if (dem_integer_error == 1) { MessageBox.Show("Warning: Digital Elevation Model may only contain integer values\n LORICA can proceed, but may experience problems"); }

                }
            }
        }
        void dtm_file(string name1)
        {

            string FILE_NAME = name1;
            int z, dem_integer_error = 1;
            string[] lineArray2;
            int sp;
            Debug.WriteLine("Opening DEM " + FILE_NAME);
            //MessageBox.Show("Directory " + Directory.GetCurrentDirectory() );

            if (!System.IO.File.Exists(FILE_NAME))
            {
                MessageBox.Show("No such DEM data file..");
                input_data_error = true;
                return;
            }

            try
            {
                //read headers
                StreamReader sr = System.IO.File.OpenText(FILE_NAME);
                for (z = 1; z <= 6; z++)
                {
                    inputheader[z - 1] = sr.ReadLine();
                    //Debug.WriteLine(inputheader[z - 1]);
                }
                sr.Close();

                if (inputheader[4].Contains("XCELLSIZE"))
                {
                    MessageBox.Show("Error: DEM cell is not square. Restart LORICA and provide a better input");
                    input_data_error = true;
                    return;
                }

                // get nc, nr and dx from input headers

                lineArray2 = inputheader[0].Split(new char[] { ' ' });
                sp = 1;
                while (lineArray2[sp] == "") sp++;
                nc = int.Parse(lineArray2[sp]);

                lineArray2 = inputheader[1].Split(new char[] { ' ' });
                sp = 1;
                while (lineArray2[sp] == "") sp++;
                nr = int.Parse(lineArray2[sp]);

                lineArray2 = inputheader[2].Split(new char[] { ' ' });
                sp = 1;
                while (lineArray2[sp] == "") sp++;
                xcoord = double.Parse(lineArray2[sp]);

                lineArray2 = inputheader[3].Split(new char[] { ' ' });
                sp = 1;
                while (lineArray2[sp] == "") sp++;
                ycoord = double.Parse(lineArray2[sp]);

                lineArray2 = inputheader[4].Split(new char[] { ' ' });
                sp = 1;
                while (lineArray2[sp] == "") sp++;
                dx = double.Parse(lineArray2[sp]);

                if (dx <= 0.1)  //AleG
                {
                    MessageBox.Show("Make sure that the DEM cell resolution is in meters");
                }

                Debug.WriteLine("read DEM: nr = " + nr + " nc = " + nc);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("There is a problem with the header of the DEM file");
                input_data_error = true;
                return;

            }
            int ok = makematrices();
            if (ok == 1)
            { // we have now succesfully made memory reservations for all data layers in the model 

            }
            else
            {
                MessageBox.Show("There is not enough memory for LORICA to run with these settings");
            }
            {

                int col, row, colcounter;
                String input;
                double tttt = 0.00;

                // load dem again

                if (!System.IO.File.Exists(FILE_NAME))
                {
                    Debug.WriteLine("No such DEM data file..");
                    input_data_error = true;
                    return;
                }

                StreamReader sr = System.IO.File.OpenText(FILE_NAME);

                //now skip over the headers.
                for (z = 1; z <= 6; z++)
                {
                    input = sr.ReadLine();
                }
                row = 0;
                while ((input = sr.ReadLine()) != null)  // so not until nr is reached, but until the file is empty
                {

                    string[] lineArray;
                    lineArray = input.Split(new char[] { ' ' });   // so we split the string that we read (readline) from file into an array of strings that each contain a number
                    col = 0;
                    for (colcounter = 0; colcounter <= (lineArray.Length - 1); colcounter++)  // the length of LineArray should equal nc, and therefore run from 0 to nc-1
                    {

                        if (lineArray[colcounter] != "" && col < nc) // but just to make sure, col counts only the non-empty strings in LineArrary (handy for instance when files are double-spaced)
                        {
                            tttt = double.Parse(lineArray[colcounter]);
                            if (Spitsbergen_case_study.Checked == true) { original_dtm[row, col] = tttt; dtm[row, col] = -9999; }
                            else { dtm[row, col] = tttt; }
                            col++;
                            if (double.Parse(lineArray[colcounter]) - Math.Round(double.Parse(lineArray[colcounter])) != 0)
                            {
                                dem_integer_error = 0;
                            }
                        }
                    }
                    row++;

                }
                sr.Close();
                if (dem_integer_error == 1) { MessageBox.Show("Warning: Digital Elevation Model may only contain integer values\n LORICA can proceed, but may experience problems"); }

            }
        }
        void read_double(string name2, double[,] map1)
        {
            string FILE_NAME = name2;
            string input;
            double tttt = 0.00;
            int x, y, xcounter;

            StackTrace stackTrace = new StackTrace();
            Debug.WriteLine(stackTrace.GetFrame(1).GetMethod().Name);
            if (!System.IO.File.Exists(FILE_NAME))
            {
                MessageBox.Show("No such double data file " + FILE_NAME);
                input_data_error = true;
                return;
            }

            StreamReader sr = System.IO.File.OpenText(FILE_NAME);

            //read headers
            for (z = 1; z <= 6; z++)
            {
                input = sr.ReadLine();
            }
            y = 0;

            while ((input = sr.ReadLine()) != null)
            {
                string[] lineArray;
                lineArray = input.Split(new char[] { ' ' });
                xcounter = 0;
                for (x = 0; x <= (lineArray.Length - 1); x++)
                {

                    if (lineArray[x] != "" && xcounter < nc)
                    {

                        try
                        {
                            tttt = double.Parse(lineArray[x]);
                        }
                        catch
                        {
                            MessageBox.Show("Incorrect content " + lineArray[x] + " in file " + FILE_NAME);
                            input_data_error = true;
                            return;
                        }
                        map1[y, xcounter] = tttt;
                        xcounter++;
                    }
                }
                y++;

            }
            sr.Close();
        } // end read_double()
        void read_integer(string name2, int[,] map1)
        {
            string FILE_NAME = name2;
            string input;
            int tttt = 0;
            int x, y, xcounter;
            Debug.WriteLine(" Reading " + FILE_NAME + " from " + Directory.GetCurrentDirectory());
            if (!System.IO.File.Exists(FILE_NAME))
            {
                MessageBox.Show("No such data file " + FILE_NAME);
                input_data_error = true;
                return;
            }
            StreamReader sr = System.IO.File.OpenText(FILE_NAME);

            //read headers
            for (z = 1; z <= 6; z++)
            {
                input = sr.ReadLine();
                /*if (z == 1)
                {
                    string[] lineArray;
                    lineArray = input.Split(new char[] { ' ' });
                    Debug.WriteLine(input + " here " + lineArray[1] + " there " );
                    if (int.Parse(lineArray[1]) != nc)
                    {
                        Debug.WriteLine(filename + " has different cols than the DEM ");
                    }
                }
                if (z == 2)
                {
                    string[] lineArray;
                    lineArray = input.Split(new char[] { ' ' });
                    Debug.WriteLine(lineArray[1]);
                    if (int.Parse(lineArray[1]) != nr)
                    {
                        Debug.WriteLine(filename + " has different rows than the DEM ");
                    }
                } */
            }
            y = 0;
            while ((input = sr.ReadLine()) != null)
            {
                string[] lineArray;
                lineArray = input.Split(new char[] { ' ' });
                xcounter = 0;
                for (x = 0; x <= (lineArray.Length - 1); x++)
                {

                    if (lineArray[x] != "" && xcounter < nc)
                    {
                        try
                        {
                            tttt = int.Parse(lineArray[x]);
                        }
                        catch
                        {
                            MessageBox.Show("Incorrect content " + lineArray[x] + " in file " + FILE_NAME);
                            input_data_error = true;
                            return;
                        }
                        map1[y, xcounter] = tttt;
                        xcounter++;
                    }
                }
                y++;

            }
            sr.Close();
            //Debug.WriteLine("completed reading file" + FILE_NAME);
        } // end read_integer()
        void read_record(string filename, int[] record)
        {
            string FILE_NAME = filename;
            string input;
            int tttt = 0;
            int y;
            if (!System.IO.File.Exists(FILE_NAME))
            {
                MessageBox.Show("No such data file " + FILE_NAME);
                input_data_error = true;
                return;
            }
            // Debug.WriteLine("reading " + filename + " into record ");
            StreamReader sr = System.IO.File.OpenText(FILE_NAME);

            //read first line: number of timesteps
            input = sr.ReadLine();
            y = 0;
            int recordsize = 0;
            try { recordsize = System.Convert.ToInt32(input); }
            catch
            {
                MessageBox.Show("Wrong value " + input + " in first line of record " + FILE_NAME);
                input_data_error = true;
                return;
            }

            // Debug.WriteLine("reading " + filename + " into record of size " + record.Length);

            // the record size is read from the first line and not necessarily equal to the number of timesteps. 
            // Runs will start from beginning of record and repeat when necessary
            while ((input = sr.ReadLine()) != null)
            {
                if (y >= recordsize)
                {
                    MessageBox.Show("record " + FILE_NAME + " contains more values than expected. Extras are ignored");
                    break;
                }

                try { tttt = int.Parse(input); }
                catch
                {
                    MessageBox.Show("Incorrect content " + input + " in file " + FILE_NAME);
                    input_data_error = true;
                    return;
                }
                record[y] = tttt;
                //Debug.WriteLine("value " + y + " in record is " + record[y]);
                y++;
            }
            sr.Close();

        }
        void out_double(string name4, double[,] output)
        {
            int nn, row, col;
            string FILENAME = name4;
            using (StreamWriter sw = new StreamWriter(FILENAME))
            {
                sw.Write("ncols         " + nc);
                sw.Write("\r\n");
                sw.Write("nrows         " + nr);
                sw.Write("\r\n");
                for (nn = 2; nn <= 5; nn++)
                {
                    sw.Write(inputheader[nn]); sw.Write("\r\n");
                    //MessageBox.Show(inputheader[nn]);
                }
                for (row = 0; row < nr; row++)
                {
                    for (col = 0; col < nc; col++)
                    {
                        sw.Write("{0:F6}", output[row, col]);
                        sw.Write(" ");

                    }
                    sw.Write("\r\n");
                }
                sw.Close();
            }

        }
        void out_float(string name4, float[,] output)
        {
            int nn, row, col;
            string FILENAME = name4;
            using (StreamWriter sw = new StreamWriter(FILENAME))
            {
                sw.Write("ncols         " + nc);
                sw.Write("\r\n");
                sw.Write("nrows         " + nr);
                sw.Write("\r\n");
                for (nn = 2; nn <= 5; nn++)
                {
                    sw.Write(inputheader[nn]); sw.Write("\r\n");
                    //MessageBox.Show(inputheader[nn]);
                }
                for (row = 0; row < nr; row++)
                {
                    for (col = 0; col < nc; col++)
                    {
                        sw.Write("{0:F6}", output[row, col]);
                        sw.Write(" ");

                    }
                    sw.Write("\r\n");
                }
                sw.Close();
            }

        }
        void out_blocks(string name4)
        {
            string FILENAME = name4;
            using (StreamWriter sw = new StreamWriter(FILENAME))
            {
                int blocknr = 0;
                sw.WriteLine("blocknr x y size row col");
                foreach (var Block in Blocklist)
                {
                    sw.WriteLine(blocknr + " " + (Block.X_col * dx + xcoord) + " " + (Block.Y_row * dx + ycoord) + " " + Block.Size_m + " " + Math.Floor(Block.Y_row) + " " + Math.Floor(Block.X_col));
                    blocknr++;
                }
                sw.Close();
            }
            Debug.WriteLine(" wrote block locations and sizes to file " + name4);
        } //end out_double
        void out_mf(string name4, double[,,] output)
        {
            int row, col;
            string FILENAME = name4;
            using (StreamWriter sw = new StreamWriter(FILENAME))
            {
                sw.Write("In n1 n2 n3 n4 n5 n6 n7 n8");
                sw.Write("\r\n");
                for (row = 0; row < nr; row++)
                {
                    for (col = 0; col < nc; col++)
                    {
                        for (int dir = 0; dir < 9; dir++)
                        {
                            sw.Write(OFy_m[row, col, dir]);
                            sw.Write(" ");
                        }
                        sw.Write("\r\n");
                    }
                }

                sw.Write("ncols         " + nc);
                sw.Write("\r\n");
                sw.Write("nrows         " + nr);
                sw.Write("\r\n");

                sw.Close();
            }
        }
        void out_integer(string name4, int[,] output)
        {
            int nn, row, col;
            string FILENAME = name4;
            using (StreamWriter sw = new StreamWriter(FILENAME))
            {
                for (nn = 0; nn <= 5; nn++)
                {
                    sw.Write(inputheader[nn]); sw.Write("\n");
                }
                for (row = 0; row < nr; row++)
                {
                    for (col = 0; col < nc; col++)
                    {

                        sw.Write(output[row, col]);
                        sw.Write(" ");
                    }

                    sw.Write("\n");
                }
                sw.Close();
            }
        } //end out_integer
        void out_short(string name4, short[,] output)
        {
            int nn;
            string FILENAME = name4;
            using (StreamWriter sw = new StreamWriter(FILENAME))
            {
                for (nn = 0; nn <= 5; nn++)
                {
                    sw.Write(inputheader[nn]); sw.Write("\n");
                }
                for (int row = 0; row < nr; row++)
                {
                    for (int col = 0; col < nc; col++)
                    {

                        sw.Write(output[row, col]);
                        sw.Write(" ");
                    }

                    sw.Write("\n");
                }
                sw.Close();
            }
        } //end out_short
        void out_profile(string name5, double[,] output, bool row_is_fixed, int row_or_col)
        {
            // WVG 20-10-2010 output a profile file for benefit glorious model of LORICA
            int row, col;
            string FILENAME = name5;
            using (StreamWriter sw = new StreamWriter(FILENAME))

                try
                {
                    if (row_is_fixed)
                    {
                        try
                        {
                            for (col = 0; col < nc; col++)// WVG the number of columns is equal to nc
                            {
                                sw.WriteLine(output[row_or_col, col]);
                            }
                        }
                        catch { Debug.WriteLine("out_profile: error "); }
                    }
                    else  // apparently column is fixed
                    {
                        try
                        {
                            for (row = 0; row < nr; row++)// WVG the number of columns is equal to nc
                            {
                                sw.WriteLine(output[row, row_or_col]);
                            }
                        }
                        catch { Debug.WriteLine("out_profile: error "); }
                    }
                    sw.Close();
                }
                catch { Debug.WriteLine("Profile could not be written"); }

        } //WVG end out_profile
        void writesoil(int row, int col)
        {
            int layer;
            double cumthick, midthick;
            string FILENAME = string.Format("{0}\\t{1}_r{2}_c{3}_out_soil.csv", workdir, t + 1, row, col);
            using (StreamWriter sw = new StreamWriter(FILENAME))
            {
                sw.Write("row, col, t, cumth_m, thick_m, midthick_m, coarse_kg, sand_kg, silt_kg, clay_kg, fine_kg, YOM_kg, OOM_kg, YOM/OOM, f_coarse, f_sand, f_silt, f_clay, f_fineclay");
                sw.Write("\r\n");
                cumthick = 0;
                midthick = 0;
                int t_out = t + 1;
                for (layer = 0; layer < max_soil_layers; layer++) // only the top layer
                {
                    if (layerthickness_m[row, col, layer] > 0)
                    {
                        cumthick += layerthickness_m[row, col, layer];
                        midthick += layerthickness_m[row, col, layer] / 2;
                        double totalweight = texture_kg[row, col, layer, 0] + texture_kg[row, col, layer, 1] + texture_kg[row, col, layer, 2] + texture_kg[row, col, layer, 3] + texture_kg[row, col, layer, 4] + young_SOM_kg[row, col, layer] + old_SOM_kg[row, col, layer];
                        sw.Write(row + "," + col + "," + t_out + "," + cumthick + "," + layerthickness_m[row, col, layer] + "," + midthick + "," + texture_kg[row, col, layer, 0] + "," + texture_kg[row, col, layer, 1] + "," + texture_kg[row, col, layer, 2] + "," + texture_kg[row, col, layer, 3] + "," + texture_kg[row, col, layer, 4] + "," + young_SOM_kg[row, col, layer] + "," + old_SOM_kg[row, col, layer] + "," + young_SOM_kg[row, col, layer] / old_SOM_kg[row, col, layer] + "," + texture_kg[row, col, layer, 0] / totalweight + "," + texture_kg[row, col, layer, 1] / totalweight + "," + texture_kg[row, col, layer, 2] / totalweight + "," + texture_kg[row, col, layer, 3] / totalweight + "," + texture_kg[row, col, layer, 4] / totalweight);
                        sw.Write("\r\n");
                        midthick += layerthickness_m[row, col, layer] / 2;
                    }

                }
                sw.Close();
            }
        }// end writesoil

        //void writeOSLages()
        //{
        //    int layer;
        //    string FILENAME = string.Format("{0}\\t{1}_out_OSL_ages.csv", workdir, t + 1);
        //    using (StreamWriter sw = new StreamWriter(FILENAME))
        //    {
        //        sw.Write("row, col, layer, stabilization_age, deposition_age, layerthickness_m, layermass_kg, z_surface_m");
        //        sw.Write("\r\n");
        //        int t_out = t + 1;
        //        for (int osl_i = 0; osl_i < OSL_age.GetLength(0); osl_i++)
        //        {
        //            double laythick = layerthickness_m[OSL_age[osl_i, 0], OSL_age[osl_i, 1], OSL_age[osl_i, 2]];
        //            double laymass = total_layer_mass(OSL_age[osl_i, 0], OSL_age[osl_i, 1], OSL_age[osl_i, 2]);
        //            sw.Write(OSL_age[osl_i, 0] + "," + OSL_age[osl_i, 1] + "," + OSL_age[osl_i, 2] + "," + OSL_age[osl_i, 3] + "," + OSL_age[osl_i, 4] + "," + laythick + "," + laymass + "," + dtm[OSL_age[osl_i, 0], OSL_age[osl_i, 1]]);
        //            sw.Write("\r\n");
        //        }
        //        sw.Close();
        //    }
        //}
        void writeOSLages_jaggedArray()
        {
            int layer;
            string FILENAME = string.Format("{0}\\t{1}_out_OSL_ages_JA.csv", workdir, t + 1);
            using (StreamWriter sw = new StreamWriter(FILENAME))
            {
                sw.Write("row, col, layer, grain_age, deposition_age, count_surfaced");
                sw.Write("\r\n");
                int t_out = t + 1;
                for (row = 0; row < nr; row++)
                {
                    for (col = 0; col < nc; col++)
                    {
                        for (int lay = 0; lay < max_soil_layers; lay++)
                        {
                            for (int ind = 0; ind < OSL_grainages[row, col, lay].Length; ind++)
                            {
                                double laythick = layerthickness_m[row, col, lay];
                                double laymass = total_layer_mass_kg(row, col, lay);
                                sw.Write(row + "," + col + "," + lay + "," + OSL_grainages[row, col, lay][ind] + "," + OSL_depositionages[row, col, lay][ind] + "," + OSL_surfacedcount[row, col, lay][ind]);
                                sw.Write("\r\n");
                            }
                        }
                    }
                }
                sw.Close();
            }
        }
        void writeallsoils()
        {
            int layer;
            double cumthick, midthick, z_layer;
            string FILENAME = string.Format("{0}\\t{1}_out_allsoils.csv", workdir, t + 1);
            using (StreamWriter sw = new StreamWriter(FILENAME))
            {
                sw.Write("row,col,t,nlayer,cumth_m,thick_m,midthick_m,z,coarse_kg,sand_kg,silt_kg,clay_kg,fine_kg,YOM_kg,OOM_kg,YOM/OOM,f_coarse,f_sand,f_silt,f_clay,f_fineclay,ftotal_clay,f_OM,BD");
                if (CN_checkbox.Checked) { sw.Write(",Be-10_meteoric_clay,Be-10_meteoric_silt,Be-10_meteoric_total,Be-10_insitu,C-14_insitu"); }
                sw.Write("\r\n");
                int t_out = t + 1;
                for (int row = 0; row < nr; row++)
                {
                    for (int col = 0; col < nc; col++)
                    {
                        if (dtm[row, col] != -9999)
                        {
                            cumthick = 0;
                            midthick = 0;
                            z_layer = dtm[row, col];
                            for (layer = 0; layer < max_soil_layers; layer++) // only the top layer
                            {
                                if (layerthickness_m[row, col, layer] > 0)
                                {
                                    cumthick += layerthickness_m[row, col, layer];
                                    midthick += layerthickness_m[row, col, layer] / 2;
                                    double totalweight = texture_kg[row, col, layer, 0] + texture_kg[row, col, layer, 1] + texture_kg[row, col, layer, 2] + texture_kg[row, col, layer, 3] + texture_kg[row, col, layer, 4] + young_SOM_kg[row, col, layer] + old_SOM_kg[row, col, layer];
                                    double totalweight_tex = texture_kg[row, col, layer, 0] + texture_kg[row, col, layer, 1] + texture_kg[row, col, layer, 2] + texture_kg[row, col, layer, 3] + texture_kg[row, col, layer, 4];
                                    sw.Write(row + "," + col + "," + t_out + "," + layer + "," + cumthick + "," + layerthickness_m[row, col, layer] + "," + midthick + "," + z_layer + "," + texture_kg[row, col, layer, 0] + "," + texture_kg[row, col, layer, 1] + "," + texture_kg[row, col, layer, 2] + "," + texture_kg[row, col, layer, 3] + "," + texture_kg[row, col, layer, 4] + "," + young_SOM_kg[row, col, layer] + "," + old_SOM_kg[row, col, layer] + "," + young_SOM_kg[row, col, layer] / old_SOM_kg[row, col, layer] + "," + texture_kg[row, col, layer, 0] / totalweight_tex + "," + texture_kg[row, col, layer, 1] / totalweight_tex + "," + texture_kg[row, col, layer, 2] / totalweight_tex + "," + texture_kg[row, col, layer, 3] / totalweight_tex + "," + texture_kg[row, col, layer, 4] / totalweight_tex + "," + (texture_kg[row, col, layer, 3] + texture_kg[row, col, layer, 4]) / totalweight_tex + "," + (young_SOM_kg[row, col, layer] + old_SOM_kg[row, col, layer]) / (young_SOM_kg[row, col, layer] + old_SOM_kg[row, col, layer] + totalweight_tex) + "," + bulkdensity[row, col, layer]);

                                    if (CN_checkbox.Checked)
                                    {
                                        sw.Write("," + (CN_atoms_cm2[row, col, layer, 0] + "," + CN_atoms_cm2[row, col, layer, 1]) + "," + (CN_atoms_cm2[row, col, layer, 0] + CN_atoms_cm2[row, col, layer, 1]) + "," + CN_atoms_cm2[row, col, layer, 2] + "," + CN_atoms_cm2[row, col, layer, 3]);
                                    }

                                    sw.Write("\r\n");
                                    midthick += layerthickness_m[row, col, layer] / 2;
                                    z_layer -= layerthickness_m[row, col, layer];
                                }

                            }

                        }
                    }
                }
                sw.Close();
            }

        }// end writeallsoils
        void write_longitudinal_profile(int startrow, int startcol, string name4)
        {

            // writes a longitudinal steepest-descent profile starting from a given begin r,c 
            double altidiff, non_lake_altidiff, maxaltidiff;
            int row, col, i, j, non_lake_maxi, non_lake_maxj, maxi, maxj, profilesize = 1000, step;
            double[] profile;
            profile = new double[1000];
            row = startrow;
            col = startcol;
            step = 0;
            string FILENAME = name4;

            while (row - 1 >= 0 && row + 1 < nr && col - 1 >= 0 && col + 1 < nc)
            {   // as long as we have not reached the edge
                Debug.WriteLine("profile now at row %d col %d, alt %.4f\n", row, col, dtm[row, col]);
                altidiff = -9999; non_lake_altidiff = 0; maxaltidiff = 0; non_lake_maxi = 0; non_lake_maxj = 0; maxi = 0; maxj = 0;
                for (i = -1; i <= 1; i++)
                {
                    for (j = -1; j <= 1; j++)
                    {
                        altidiff = dtm[row, col] - dtm[row + i, col + j];
                        //Debug.WriteLine(" profile : nb %d %d, diff %.3f\n",row+i,col+j,altidiff);
                        if (altidiff > maxaltidiff)
                        {
                            maxaltidiff = altidiff; maxi = i; maxj = j;
                        }
                        if (altidiff > non_lake_altidiff && depression[row + i, col + j] == 0)
                        {
                            non_lake_altidiff = altidiff; non_lake_maxi = i; non_lake_maxj = j;
                        }
                    }
                }
                Debug.WriteLine("profile : found lowest nb at %d %d, diff %.3f\n", row + maxi, col + maxj, maxaltidiff);
                if (non_lake_altidiff != 0) { row += non_lake_maxi; col += non_lake_maxj; } //avoid depressions if you can and prevent from falling back
                else { row += maxi; col += maxj; }
                if (maxi == 0 && maxj == 0 || maxaltidiff == 0)
                {
                    Debug.WriteLine("warning : no profile-progress due to sink?\n"); // go straight to the outlet of this depression and count the number of cells in between
                                                                                     //break;
                    maxi = drainingoutlet_row[depression[row, col], 0] - row;
                    maxj = drainingoutlet_col[depression[row, col], 0] - col;
                    for (i = 1; i <= (Math.Abs(maxi) + Math.Abs(maxj)); i++)
                    {
                        profile[step] = dtm[row, col] + (i / (Math.Abs(maxi) + Math.Abs(maxj))) * (depressionlevel[depression[row, col]] - dtm[row, col]);
                        Debug.WriteLine("profile %d now %.6f\n", step, profile[step]);
                        step++;
                    }
                    row += maxi;
                    col += maxj;
                }
                else
                {
                    profile[step] = dtm[row, col];
                    step++;
                    if (step > profilesize - 3) { Debug.WriteLine("warning : profilerecord may be too small\n"); break; }
                }
            }

            using (StreamWriter sw = new StreamWriter(FILENAME))
            {
                for (i = 0; i < step + 1; i++)
                {
                    Debug.WriteLine(profile[i]);
                    sw.Write("{0:F6}", profile[i]);
                }
                sw.Write("\r\n");
            }
            Debug.WriteLine("profile contains %d values\n", step);
        } // end write_profile() 
        void write_full_output(string filecore, int rows, int cols, int layers, int t)
        {
            int layer, row, col;
            string filename = workdir + "\\" + filecore + t + ".lrc";
            //Debug.WriteLine("attempting to write output " + filename + " at t " + t);
            using (StreamWriter sw = new StreamWriter(filename))
            {
                try
                {
                    sw.WriteLine("Lorica output header");
                    sw.WriteLine("year " + t);
                    sw.WriteLine("years " + end_time + " every " + int.Parse(Box_years_output.Text));
                    sw.WriteLine("rows " + rows + " cellsize " + dx + " yllcoord " + ycoord);
                    sw.WriteLine("cols " + cols + " xllcoord " + xcoord);
                    sw.WriteLine("layers " + layers);
                    sw.WriteLine("properties 10");
                    sw.WriteLine("propnames elevation thickness_m density_kg_m3 coarse_kg sand_kg silt_kg clay_kg fineclay_kg youngom_kg oldom_kg");
                    sw.WriteLine("Lorica output content");
                }
                catch { Debug.WriteLine(" issue with writing the header of the full output file for this timestep"); }
                try
                {
                    for (row = 0; row < rows; row++)
                    {
                        for (col = 0; col < cols; col++)
                        {
                            for (layer = 0; layer < layers; layer++)
                            {
                                sw.Write(dtm[row, col]
                                    + "_" + layerthickness_m[row, col, layer]
                                    + "_" + bulkdensity[row, col, layer]
                                    + "_" + texture_kg[row, col, layer, 0]
                                    + "_" + texture_kg[row, col, layer, 1]
                                    + "_" + texture_kg[row, col, layer, 2]
                                    + "_" + texture_kg[row, col, layer, 3]
                                    + "_" + texture_kg[row, col, layer, 4]
                                    + "_" + young_SOM_kg[row, col, layer]
                                    + "_" + old_SOM_kg[row, col, layer]
                                    + ",");
                            }
                            sw.Write("\n");
                        }
                    }
                }
                catch { Debug.WriteLine(" issue with writing the content of the full output file for this timestep"); }
                sw.Close();
            }
        }
        private void menuItemConfigFileOpen_Click(object sender, System.EventArgs e)
        {
            //opens a runfile
            XmlTextReader xreader;
            int read_error = 0;
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.InitialDirectory = workdir;
            openFileDialog1.Filter = "cfg files (*.xml)|*.xml|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.RestoreDirectory = false;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                cfgname = openFileDialog1.FileName;

                xreader = new XmlTextReader(cfgname);

                //Read the file
                if (xreader != null)
                {
                    try
                    {
                        xreader.ReadStartElement("Parms");
                        xreader.ReadStartElement("Processes");
                        xreader.ReadStartElement("Water_erosion");
                        Water_ero_checkbox.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("water_active"));
                        parameter_m_textbox.Text = xreader.ReadElementString("para_m");
                        parameter_n_textbox.Text = xreader.ReadElementString("para_n");
                        parameter_conv_textbox.Text = xreader.ReadElementString("para_p");
                        parameter_K_textbox.Text = xreader.ReadElementString("para_K");
                        erosion_threshold_textbox.Text = xreader.ReadElementString("para_ero_threshold");
                        rock_protection_constant_textbox.Text = xreader.ReadElementString("para_rock_protection_const");
                        bio_protection_constant_textbox.Text = xreader.ReadElementString("para_bio_protection_const");
                        selectivity_constant_textbox.Text = xreader.ReadElementString("para_selectivity");
                        xreader.ReadEndElement();
                    }
                    catch { read_error = 1; Debug.WriteLine("failed reading water ero paras"); }

                    try
                    {
                        xreader.ReadStartElement("Tillage");
                        Tillage_checkbox.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("tillage_active"));
                        parameter_ploughing_depth_textbox.Text = xreader.ReadElementString("para_plough_depth");
                        parameter_tillage_constant_textbox.Text = xreader.ReadElementString("para_tillage_constant");
                        xreader.ReadEndElement();
                    }
                    catch { read_error = 1; Debug.WriteLine("failed reading tillage paras"); }

                    try
                    {
                        xreader.ReadStartElement("Weathering");
                        Biological_weathering_checkbox.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("bio_weathering_active"));
                        parameter_P0_textbox.Text = xreader.ReadElementString("para_P0");
                        parameter_k1_textbox.Text = xreader.ReadElementString("para_k1");
                        parameter_k2_textbox.Text = xreader.ReadElementString("para_k2");
                        parameter_Pa_textbox.Text = xreader.ReadElementString("para_Pa");
                        xreader.ReadEndElement();
                    }
                    catch { read_error = 1; Debug.WriteLine("failed reading weathering paras"); }

                    try
                    {
                        xreader.ReadStartElement("Landsliding");
                        Landslide_checkbox.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("landsliding_active"));
                        radio_ls_absolute.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("radio_ls_absolute"));
                        radio_ls_fraction.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("radio_ls_fraction"));
                        text_ls_abs_rain_intens.Text = xreader.ReadElementString("para_absolute_rain_intens");
                        text_ls_rel_rain_intens.Text = xreader.ReadElementString("para_relative_rain_intens");
                        textBox_ls_coh.Text = xreader.ReadElementString("para_cohesion");
                        textBox_ls_ifr.Text = xreader.ReadElementString("para_friction");
                        textBox_ls_bd.Text = xreader.ReadElementString("para_density");
                        textBox_ls_trans.Text = xreader.ReadElementString("para_transmissivity");
                        xreader.ReadEndElement();
                    }
                    catch { read_error = 1; Debug.WriteLine("failed reading landsliding paras"); }

                    try
                    {
                        xreader.ReadStartElement("Creep");
                        creep_active_checkbox.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("creep_active"));
                        parameter_diffusivity_textbox.Text = xreader.ReadElementString("para_diffusivity");
                        xreader.ReadEndElement();
                    }
                    catch { read_error = 1; Debug.WriteLine("failed reading creep paras"); }

                    try
                    {
                        xreader.ReadStartElement("Tree_fall");
                        treefall_checkbox.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("treefall_active"));
                        tf_W.Text = xreader.ReadElementString("tf_width");
                        tf_D.Text = xreader.ReadElementString("tf_depth");
                        tf_growth.Text = xreader.ReadElementString("tf_growth");
                        tf_age.Text = xreader.ReadElementString("tf_age");
                        tf_freq.Text = xreader.ReadElementString("tf_freq");
                        xreader.ReadEndElement();
                    }
                    catch { read_error = 1; Debug.WriteLine("failed reading tree fall paras"); }

                    try
                    {
                        xreader.ReadStartElement("Blocks");
                        blocks_active_checkbox.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("blocks_active"));
                        hardlayerthickness_textbox.Text = xreader.ReadElementString("hardlayerthickness");
                        hardlayerelevation_textbox.Text = xreader.ReadElementString("hardlayerelevation");
                        hardlayerdensity_textbox.Text = xreader.ReadElementString("hardlayerdensity");
                        hardlayerweath_textbox.Text = xreader.ReadElementString("hardlayerweath");
                        blockweath_textbox.Text = xreader.ReadElementString("blockweath");
                        blocksize_textbox.Text = xreader.ReadElementString("blockminsize");
                        xreader.ReadEndElement();
                    }
                    catch { read_error = 1; Debug.WriteLine("failed reading block paras"); }

                    try
                    {
                        xreader.ReadEndElement();
                        xreader.ReadStartElement("Soil_forming_processes");
                        xreader.ReadStartElement("Physical_weathering");
                        soil_phys_weath_checkbox.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("phys_weath_active"));
                        Physical_weath_C1_textbox.Text = xreader.ReadElementString("weath_rate_constant");
                        physical_weath_constant1.Text = xreader.ReadElementString("constant1");
                        physical_weath_constant2.Text = xreader.ReadElementString("constant2");
                        soildata.upper_particle_coarse_textbox.Text = xreader.ReadElementString("size_coarse");
                        soildata.upper_particle_sand_textbox.Text = xreader.ReadElementString("size_sand");
                        soildata.upper_particle_silt_textbox.Text = xreader.ReadElementString("size_silt");
                        soildata.upper_particle_clay_textbox.Text = xreader.ReadElementString("size_clay");
                        soildata.upper_particle_fine_clay_textbox.Text = xreader.ReadElementString("size_fine");
                        xreader.ReadEndElement();
                    }
                    catch { read_error = 1; Debug.WriteLine("failed reading soil phys weath paras"); }

                    try
                    {
                        xreader.ReadStartElement("Chemical_weathering");
                        soil_chem_weath_checkbox.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("chem_weath_active"));
                        chem_weath_rate_constant_textbox.Text = xreader.ReadElementString("weath_rate_constant");
                        chem_weath_depth_constant_textbox.Text = xreader.ReadElementString("constant3");
                        chem_weath_specific_coefficient_textbox.Text = xreader.ReadElementString("constant4");
                        soildata.specific_area_coarse_textbox.Text = xreader.ReadElementString("surface_coarse");
                        soildata.specific_area_sand_textbox.Text = xreader.ReadElementString("surface_sand");
                        soildata.specific_area_silt_textbox.Text = xreader.ReadElementString("surface_silt");
                        soildata.specific_area_clay_textbox.Text = xreader.ReadElementString("surface_clay");
                        soildata.specific_area_fine_clay_textbox.Text = xreader.ReadElementString("surface_fine_clay");
                        xreader.ReadEndElement();
                    }
                    catch { read_error = 1; Debug.WriteLine("failed reading soil chemical weath paras"); }

                    try
                    {
                        xreader.ReadStartElement("Clay_dynamics");
                        soil_clay_transloc_checkbox.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("clay_dynamics_active"));
                        clay_neoform_constant_textbox.Text = xreader.ReadElementString("neoform_rate_constant");
                        clay_neoform_C1_textbox.Text = xreader.ReadElementString("constant5");
                        clay_neoform_C2_textbox.Text = xreader.ReadElementString("constant6");
                        maximum_eluviation_textbox.Text = xreader.ReadElementString("max_eluviation");
                        eluviation_coefficient_textbox.Text = xreader.ReadElementString("eluviation_coefficient");
                        ct_Jagercikova.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("ct_Jagercikova_active"));
                        ct_v0_Jagercikova.Text = xreader.ReadElementString("ct_v0_Jagercikova");
                        ct_dd_Jagercikova.Text = xreader.ReadElementString("ct_dd_Jagercikova");
                        xreader.ReadEndElement();
                    }
                    catch
                    {
                        read_error = 1; Debug.WriteLine("failed reading clay dynamics paras");
                    }

                    try
                    {
                        xreader.ReadStartElement("Bioturbation");
                        soil_bioturb_checkbox.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("bioturbation_active"));
                        potential_bioturbation_textbox.Text = xreader.ReadElementString("potential_bioturb");
                        bioturbation_depth_decay_textbox.Text = xreader.ReadElementString("bioturb_depth_decay");
                        xreader.ReadEndElement();
                    }
                    catch { read_error = 1; Debug.WriteLine("failed reading water ero paras"); }

                    try
                    {
                        xreader.ReadStartElement("Carboncycle");
                        soil_carbon_cycle_checkbox.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("carboncycle_active"));
                        carbon_input_textbox.Text = xreader.ReadElementString("carbon_input");
                        carbon_depth_decay_textbox.Text = xreader.ReadElementString("carbon_depth_decay");
                        carbon_humification_fraction_textbox.Text = xreader.ReadElementString("carbon_hum_fraction");
                        carbon_y_decomp_rate_textbox.Text = xreader.ReadElementString("carbon_y_decomp");
                        carbon_y_depth_decay_textbox.Text = xreader.ReadElementString("carbon_y_depth_decay");
                        carbon_o_decomp_rate_textbox.Text = xreader.ReadElementString("carbon_o_decomp");
                        carbon_o_depth_decay_textbox.Text = xreader.ReadElementString("carbon_o_depth_decay");
                        xreader.ReadEndElement();
                        xreader.ReadEndElement();
                    }
                    catch { read_error = 1; Debug.WriteLine("failed reading carbon cycle paras"); }

                    try
                    {
                        xreader.ReadStartElement("Geochronological_tracers");

                        OSL_checkbox.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("OSL_active"));
                        ngrains_textbox.Text = xreader.ReadElementString("ngrains");
                        bleachingdepth_textbox.Text = xreader.ReadElementString("bleachingdepth");
                        OSL_inherited_textbox.Text = xreader.ReadElementString("inherited_age");
                        CN_checkbox.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("CN_active"));
                        metBe10_input_textbox.Text = xreader.ReadElementString("metBe10_input_rate");
                        met10Be_dd.Text = xreader.ReadElementString("metBe10_dd");
                        Be10_decay_textbox.Text = xreader.ReadElementString("Be10_decay");
                        met_10Be_clayfrac.Text = xreader.ReadElementString("metBe10_clay");
                        metBe10_inherited_textbox.Text = xreader.ReadElementString("met10Be_inherited");

                        isBe10_sp_input_textbox.Text = xreader.ReadElementString("isBe10_sp_input");
                        isBe10_mu_input_textbox.Text = xreader.ReadElementString("isBe10_mu_input");
                        isBe10_inherited_textbox.Text = xreader.ReadElementString("isBe10_inherited");

                        attenuationlength_sp_textbox.Text = xreader.ReadElementString("attlength_sp");
                        attenuationlength_mu_textbox.Text = xreader.ReadElementString("attlength_mu");

                        isC14_sp_input_textbox.Text = xreader.ReadElementString("isC14_sp_input");
                        isC14_mu_input_textbox.Text = xreader.ReadElementString("isC14_mu_input");
                        C14_decay_textbox.Text = xreader.ReadElementString("C14_decay");
                        isC14_inherited_textbox.Text = xreader.ReadElementString("isC14_inherited");

                        xreader.ReadEndElement();
                    }
                    catch { read_error = 1; Debug.WriteLine("failed reading geochron paras"); }

                    try
                    {
                        xreader.ReadStartElement("Inputs");
                        check_space_DTM.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("check_space_DTM"));
                        check_space_soildepth.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("check_space_soil"));
                        check_space_landuse.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("check_space_landuse"));
                        check_space_till_fields.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("check_space_tillfields"));
                        check_space_rain.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("check_space_rain"));
                        check_space_infil.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("check_space_infil"));
                        check_space_evap.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("check_space_evap"));
                        check_time_landuse.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("check_time_landuse"));
                        check_time_till_fields.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("check_time_tillfields"));
                        check_time_rain.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("check_time_rain"));
                        check_time_infil.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("check_time_infil"));
                        check_time_evap.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("check_time_evap"));
                    }
                    catch { read_error = 1; Debug.WriteLine("failed reading input paras"); }

                    try
                    {
                        daily_water.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("dailywater"));
                        dailyP.Text = xreader.ReadElementString("dailyP");
                        dailyET0.Text = xreader.ReadElementString("dailyET0");
                        dailyD.Text = xreader.ReadElementString("dailyD");
                        dailyT_avg.Text = xreader.ReadElementString("dailyT_avg");
                        dailyT_min.Text = xreader.ReadElementString("dailyT_min");
                        dailyT_max.Text = xreader.ReadElementString("dailyT_max");
                        latitude_deg.Text = xreader.ReadElementString("latitude_deg");
                        latitude_min.Text = xreader.ReadElementString("latitude_min");
                        snowmelt_factor_textbox.Text = xreader.ReadElementString("snowmelt_factor");
                        snow_threshold_textbox.Text = xreader.ReadElementString("snowmelt_threshold");
                        daily_n.Text = xreader.ReadElementString("daily_n_years");
                        check_scaling_daily_weather.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("scaledailyweather"));
                    } //MMxml
                    catch { read_error = 1; Debug.WriteLine("xml7.2"); Debug.WriteLine("failed reading hydrolorica paras"); }

                    try
                    {
                        dtm_input_filename_textbox.Text = xreader.ReadElementString("dtm_input_filename");
                        try { dtm_iterate_checkbox.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("check_iterate_DTM")); }
                        catch { }
                        soildepth_input_filename_textbox.Text = xreader.ReadElementString("soildepth_input_filename");
                        landuse_input_filename_textbox.Text = xreader.ReadElementString("landuse_input_filename");
                        tillfields_input_filename_textbox.Text = xreader.ReadElementString("tillfields_input_filename");
                        rain_input_filename_textbox.Text = xreader.ReadElementString("rain_input_filename");
                        infil_input_filename_textbox.Text = xreader.ReadElementString("infil_input_filename");
                        evap_input_filename_textbox.Text = xreader.ReadElementString("evap_input_filename");
                        soildepth_constant_value_box.Text = xreader.ReadElementString("soildepth_constant_value");
                        landuse_constant_value_box.Text = xreader.ReadElementString("landuse_constant_value");
                        tillfields_constant_textbox.Text = xreader.ReadElementString("tillfields_constant_value");
                        rainfall_constant_value_box.Text = xreader.ReadElementString("rain_constant_value");
                        infil_constant_value_box.Text = xreader.ReadElementString("infil_constant_value");
                        evap_constant_value_box.Text = xreader.ReadElementString("evap_constant_value");
                        checkbox_layer_thickness.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("checkbox_layer_thickness"));
                        textbox_max_soil_layers.Text = xreader.ReadElementString("max_soil_layers");
                        textbox_layer_thickness.Text = xreader.ReadElementString("layer_thickness");
                        fill_sinks_before_checkbox.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("check_fill_sinks_before"));
                        fill_sinks_during_checkbox.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("check_fill_sinks_during"));
                        xreader.ReadEndElement();
                    }
                    catch { read_error = 1; Debug.WriteLine("failed reading more input paras"); }

                    try
                    {
                        xreader.ReadStartElement("Run");
                        runs_checkbox.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("runs_radiobutton"));
                        Number_runs_textbox.Text = xreader.ReadElementString("number_runs");
                        xreader.ReadStartElement("Specialsettings");
                        Spitsbergen_case_study.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("Spitsbergen"));
                        version_lux_checkbox.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("Luxembourg"));
                        luxlitter_checkbox.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("Luxlitter"));
                        version_Konza_checkbox.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("Konza"));
                        OSL_checkbox.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("OSL_tracing"));
                        CN_checkbox.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("CN_tracing"));
                        xreader.ReadEndElement();
                    }
                    catch { read_error = 1; Debug.WriteLine("xm29"); Debug.WriteLine("failed reading run paras"); }

                    try
                    {
                        xreader.ReadStartElement("CalibrationSensitivity");

                        Calibration_button.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("calibration_active_button"));
                        num_cal_paras_textbox.Text = xreader.ReadElementString("calibration_num_paras_string");
                        calibration_ratios_textbox.Text = xreader.ReadElementString("calibration_ratios_string");
                        calibration_levels_textbox.Text = xreader.ReadElementString("calibration_levels");
                        calibration_ratio_reduction_parameter_textbox.Text = xreader.ReadElementString("calibration_ratio_reduction_per_level");
                        obsfile_textbox.Text = xreader.ReadElementString("calibration_observations_file");
                        xreader.ReadEndElement();
                    }
                    catch { read_error = 2; Debug.WriteLine("failed reading calib paras"); }

                    try
                    {
                        xreader.ReadEndElement();
                        xreader.ReadStartElement("Output");
                        xreader.ReadStartElement("File_Output");
                        xreader.ReadStartElement("Moment_of_Output");
                        Final_output_checkbox.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("final_output_checkbox"));
                        Regular_output_checkbox.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("regular_output_checkbox"));
                        Box_years_output.Text = xreader.ReadElementString("years_between_outputs");
                        xreader.ReadEndElement();
                        xreader.ReadStartElement("Type_of_Output");

                        cumulative_output_checkbox.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("cumulative"));
                        annual_output_checkbox.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("annual"));
                        xreader.ReadEndElement();
                        xreader.ReadStartElement("Maps_to_Output");
                        Altitude_output_checkbox.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("alti"));
                        Alt_change_output_checkbox.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("altichange"));
                        Soildepth_output_checkbox.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("soildepth"));
                        all_process_output_checkbox.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("all_processes"));
                        water_output_checkbox.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("waterflow"));
                        depressions_output_checkbox.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("depressions"));
                        xreader.ReadEndElement();
                        xreader.ReadEndElement();
                        xreader.ReadStartElement("Other_outputs");
                        xreader.ReadStartElement("Timeseries");
                        timeseries.timeseries_total_ero_check.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("total_erosion"));
                        timeseries.timeseries_total_dep_check.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("total_deposition"));
                        timeseries.timeseries_net_ero_check.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("net_erosion"));
                        timeseries.timeseries_sedexport_checkbox.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("sed_export"));
                        timeseries.timeseries_slide_checkbox.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("slide"));
                        timeseries.timeseries_SDR_check.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("SDR"));
                        timeseries.timeseries_total_average_alt_check.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("total_average_alt"));
                        timeseries.timeseries_total_rain_check.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("total_rain"));
                        timeseries.timeseries_total_infil_check.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("total_infil"));
                        timeseries.timeseries_total_evap_check.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("total_evap"));
                        timeseries.timeseries_total_outflow_check.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("total_outflow"));
                        timeseries.timeseries_number_waterflow_check.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("wet_cells"));
                        timeseries.timeseries_number_erosion_check.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("eroded_cells"));
                        timeseries.timeseries_number_dep_check.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("deposited_cells"));
                        timeseries.timeseries_outflow_cells_checkbox.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("outflow_cells"));
                        timeseries.timeseries_cell_altitude_check.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("cell_altitude"));
                        timeseries.timeseries_cell_waterflow_check.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("cell_waterflow"));
                        timeseries.timeseries_textbox_waterflow_threshold.Text = xreader.ReadElementString("waterflow_threshold");
                        timeseries.timeseries_textbox_erosion_threshold.Text = xreader.ReadElementString("erosion_threshold");
                        timeseries.timeseries_textbox_deposition_threshold.Text = xreader.ReadElementString("deposition_threshold");
                        timeseries.timeseries_textbox_cell_row.Text = xreader.ReadElementString("cell_row");
                        timeseries.timeseries_textbox_cell_col.Text = xreader.ReadElementString("cell_col");
                        timeseries.total_OM_input_checkbox.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("total_OM_input"));
                        timeseries.total_average_soilthickness_checkbox.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("total_average_soil_thickness"));
                        timeseries.total_phys_weath_checkbox.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("total_phys_weath"));
                        timeseries.total_chem_weath_checkbox.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("total_chem_weath"));
                        timeseries.total_fine_formed_checkbox.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("total_fine_formed"));
                        timeseries.total_fine_eluviated_checkbox.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("total_fine_eluviated"));
                        timeseries.total_mass_bioturbed_checkbox.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("total_mass_bioturbed"));
                        timeseries.timeseries_soil_depth_checkbox.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("timeseries_soil_depth"));
                        timeseries.timeseries_soil_mass_checkbox.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("timeseries_soil_mass"));
                        timeseries.timeseries_coarser_checkbox.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("timeseries_coarser"));
                        timeseries.timeseries_number_soil_thicker_checkbox.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("timeseries_thicker"));
                        timeseries.timeseries_soil_cell_col.Text = xreader.ReadElementString("soil_cell");
                        timeseries.timeseries_soil_cell_row.Text = xreader.ReadElementString("soil_col");
                        timeseries.timeseries_soil_coarser_fraction_textbox.Text = xreader.ReadElementString("coarser_fraction");
                        timeseries.timeseries_soil_thicker_textbox.Text = xreader.ReadElementString("thicker_threshold");
                        xreader.ReadEndElement();
                        xreader.ReadStartElement("Profiles");
                        profile.radio_pro1_row.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("profile1_row"));
                        profile.radio_pro1_col.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("profile1_col"));
                        profile.radio_pro2_row.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("profile2_row"));
                        profile.radio_pro2_col.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("profile2_col"));
                        profile.radio_pro3_row.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("profile3_row"));
                        profile.radio_pro3_col.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("profile3_col"));
                        profile.p1_row_col_box.Text = xreader.ReadElementString("p1_number");
                        profile.p2_row_col_box.Text = xreader.ReadElementString("p2_number");
                        profile.p3_row_col_box.Text = xreader.ReadElementString("p3_number");
                        profile.check_waterflow_profile1.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("p1_waterflow"));
                        profile.check_altitude_profile1.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("p1_altitude"));
                        profile.check_waterflow_profile2.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("p2_waterflow"));
                        profile.check_altitude_profile2.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("p2_altitude"));
                        profile.check_waterflow_profile3.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("p3_waterflow"));
                        profile.check_altitude_profile3.Checked = XmlConvert.ToBoolean(xreader.ReadElementString("p3_altitude"));
                        xreader.ReadEndElement();
                        xreader.ReadEndElement();
                    }
                    catch { read_error = 1; Debug.WriteLine("failed reading output paras"); }

                    try
                    {
                        xreader.ReadStartElement("Soilfractions");
                        soildata.coarsebox.Text = xreader.ReadElementString("coarsefrac");
                        soildata.sandbox.Text = xreader.ReadElementString("sandfrac");
                        soildata.siltbox.Text = xreader.ReadElementString("siltfrac");
                        soildata.claybox.Text = xreader.ReadElementString("clayfrac");
                        soildata.fineclaybox.Text = xreader.ReadElementString("fclayfrac");
                        xreader.ReadEndElement();
                    }
                    catch { read_error = 1; Debug.WriteLine("failed reading soil frac paras"); }

                    if (read_error == 1) { MessageBox.Show("warning : not all runfile data could be read.\r\n LORICA can continue"); }
                    if (read_error == 2) { MessageBox.Show("Error in new XML lines"); }

                    xreader.Close();

                    this.Text = basetext + " (" + Path.GetFileName(cfgname) + ")";
                    start_button.Enabled = true;
                    tabControl1.Visible = true;

                }
            }
        }
        private void menuItemConfigFileSave_Click(object sender, System.EventArgs e)
        {
            XmlTextWriter xwriter;

            if ((sender == menuItemConfigFileSaveAs) || (cfgname == null))
            {

                SaveFileDialog saveFileDialog1 = new SaveFileDialog();

                saveFileDialog1.InitialDirectory = workdir;
                saveFileDialog1.Filter = "cfg files (*.xml)|*.xml|All files (*.*)|*.*";
                saveFileDialog1.FilterIndex = 1;
                saveFileDialog1.RestoreDirectory = false;

                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    cfgname = saveFileDialog1.FileName;
                }
            }
            if (cfgname != null)
            {

                //Create a new XmlTextWriter.
                xwriter = new XmlTextWriter(cfgname, System.Text.Encoding.UTF8);
                //Write the beginning of the document including the 
                //document declaration. Standalone is true. 
                //Use indentation for readability.
                xwriter.Formatting = Formatting.Indented;
                xwriter.Indentation = 4;

                xwriter.WriteStartDocument(true);

                //Write the beginning of the "data" element. This is 
                //the opening tag to our data 
                xwriter.WriteStartElement("Parms");
                xwriter.WriteStartElement("Processes");
                xwriter.WriteStartElement("Water_erosion");
                xwriter.WriteElementString("water_active", XmlConvert.ToString(Water_ero_checkbox.Checked));
                xwriter.WriteElementString("para_m", parameter_m_textbox.Text);
                xwriter.WriteElementString("para_n", parameter_n_textbox.Text);
                xwriter.WriteElementString("para_p", parameter_conv_textbox.Text);
                xwriter.WriteElementString("para_K", parameter_K_textbox.Text);
                xwriter.WriteElementString("para_ero_threshold", erosion_threshold_textbox.Text);
                xwriter.WriteElementString("para_rock_protection_const", rock_protection_constant_textbox.Text);
                xwriter.WriteElementString("para_bio_protection_const", bio_protection_constant_textbox.Text);
                xwriter.WriteElementString("para_selectivity", selectivity_constant_textbox.Text);
                xwriter.WriteEndElement();

                xwriter.WriteStartElement("Tillage");
                xwriter.WriteElementString("tillage_active", XmlConvert.ToString(Tillage_checkbox.Checked));
                xwriter.WriteElementString("para_plough_depth", parameter_ploughing_depth_textbox.Text);
                xwriter.WriteElementString("para_tillage_constant", parameter_tillage_constant_textbox.Text);
                xwriter.WriteEndElement();

                xwriter.WriteStartElement("Weathering");
                xwriter.WriteElementString("bio_weathering_active", XmlConvert.ToString(Biological_weathering_checkbox.Checked));
                xwriter.WriteElementString("para_P0", parameter_P0_textbox.Text);
                xwriter.WriteElementString("para_k1", parameter_k1_textbox.Text);
                xwriter.WriteElementString("para_k2", parameter_k2_textbox.Text);
                xwriter.WriteElementString("para_Pa", parameter_Pa_textbox.Text);
                xwriter.WriteEndElement();

                xwriter.WriteStartElement("Landsliding");
                xwriter.WriteElementString("landsliding_active", XmlConvert.ToString(Landslide_checkbox.Checked));
                xwriter.WriteElementString("radio_ls_absolute", XmlConvert.ToString(radio_ls_absolute.Checked));
                xwriter.WriteElementString("radio_ls_fraction", XmlConvert.ToString(radio_ls_fraction.Checked));
                xwriter.WriteElementString("para_absolute_rain_intens", text_ls_abs_rain_intens.Text);
                xwriter.WriteElementString("para_relative_rain_intens", text_ls_rel_rain_intens.Text);
                xwriter.WriteElementString("para_cohesion", textBox_ls_coh.Text);
                xwriter.WriteElementString("para_friction", textBox_ls_ifr.Text);
                xwriter.WriteElementString("para_density", textBox_ls_bd.Text);
                xwriter.WriteElementString("para_transmissivity", textBox_ls_trans.Text);
                xwriter.WriteEndElement();

                xwriter.WriteStartElement("Creep");
                xwriter.WriteElementString("creep_active", XmlConvert.ToString(creep_active_checkbox.Checked));
                xwriter.WriteElementString("para_diffusivity", parameter_diffusivity_textbox.Text);
                xwriter.WriteEndElement();

                xwriter.WriteStartElement("Tree_fall");
                xwriter.WriteElementString("treefall_active", XmlConvert.ToString(treefall_checkbox.Checked));
                xwriter.WriteElementString("tf_width", tf_W.Text);
                xwriter.WriteElementString("tf_depth", tf_D.Text);
                xwriter.WriteElementString("tf_growth", tf_growth.Text);
                xwriter.WriteElementString("tf_age", tf_age.Text);
                xwriter.WriteElementString("tf_freq", tf_freq.Text);
                xwriter.WriteEndElement();

                xwriter.WriteStartElement("Blocks");
                xwriter.WriteElementString("blocks_active", XmlConvert.ToString(treefall_checkbox.Checked));
                xwriter.WriteElementString("hardlayerthickness", hardlayerthickness_textbox.Text);
                xwriter.WriteElementString("hardlayerelevation", hardlayerelevation_textbox.Text);
                xwriter.WriteElementString("hardlayerdensity", hardlayerdensity_textbox.Text);
                xwriter.WriteElementString("hardlayerweath", hardlayerweath_textbox.Text);
                xwriter.WriteElementString("blockweath", blockweath_textbox.Text);
                xwriter.WriteElementString("blockminsize", blocksize_textbox.Text);
                xwriter.WriteEndElement();

                xwriter.WriteEndElement();

                xwriter.WriteStartElement("Soil_forming_processes");
                xwriter.WriteStartElement("Physical_weathering");
                xwriter.WriteElementString("phys_weath_active", XmlConvert.ToString(soil_phys_weath_checkbox.Checked));
                xwriter.WriteElementString("weath_rate_constant", Physical_weath_C1_textbox.Text);
                xwriter.WriteElementString("constant1", physical_weath_constant1.Text);
                xwriter.WriteElementString("constant2", physical_weath_constant2.Text);
                xwriter.WriteElementString("size_coarse", soildata.upper_particle_coarse_textbox.Text);
                xwriter.WriteElementString("size_sand", soildata.upper_particle_sand_textbox.Text);
                xwriter.WriteElementString("size_silt", soildata.upper_particle_silt_textbox.Text);
                xwriter.WriteElementString("size_clay", soildata.upper_particle_clay_textbox.Text);
                xwriter.WriteElementString("size_fine", soildata.upper_particle_fine_clay_textbox.Text);
                xwriter.WriteEndElement();

                xwriter.WriteStartElement("Chemical_weathering");
                xwriter.WriteElementString("chem_weath_active", XmlConvert.ToString(soil_chem_weath_checkbox.Checked));
                xwriter.WriteElementString("weath_rate_constant", chem_weath_rate_constant_textbox.Text);
                xwriter.WriteElementString("constant3", chem_weath_depth_constant_textbox.Text);
                xwriter.WriteElementString("constant4", chem_weath_specific_coefficient_textbox.Text);
                xwriter.WriteElementString("surface_coarse", soildata.specific_area_coarse_textbox.Text);
                xwriter.WriteElementString("surface_sand", soildata.specific_area_sand_textbox.Text);
                xwriter.WriteElementString("surface_silt", soildata.specific_area_silt_textbox.Text);
                xwriter.WriteElementString("surface_clay", soildata.specific_area_clay_textbox.Text);
                xwriter.WriteElementString("surface_fine_clay", soildata.specific_area_fine_clay_textbox.Text);
                xwriter.WriteEndElement();

                xwriter.WriteStartElement("Clay_dynamics");
                xwriter.WriteElementString("clay_dynamics_active", XmlConvert.ToString(soil_clay_transloc_checkbox.Checked));
                xwriter.WriteElementString("neoform_rate_constant", clay_neoform_constant_textbox.Text);
                xwriter.WriteElementString("constant5", clay_neoform_C1_textbox.Text);
                xwriter.WriteElementString("constant6", clay_neoform_C2_textbox.Text);
                xwriter.WriteElementString("max_eluviation", maximum_eluviation_textbox.Text);
                xwriter.WriteElementString("eluviation_coefficient", eluviation_coefficient_textbox.Text);
                xwriter.WriteElementString("ct_Jagercikova_active", XmlConvert.ToString(ct_Jagercikova.Checked));
                xwriter.WriteElementString("ct_v0_Jagercikova", ct_v0_Jagercikova.Text);
                xwriter.WriteElementString("ct_dd_Jagercikova", ct_dd_Jagercikova.Text);
                xwriter.WriteEndElement();

                xwriter.WriteStartElement("Bioturbation");
                xwriter.WriteElementString("bioturbation_active", XmlConvert.ToString(soil_bioturb_checkbox.Checked));
                xwriter.WriteElementString("potential_bioturb", potential_bioturbation_textbox.Text);
                xwriter.WriteElementString("bioturb_depth_decay", bioturbation_depth_decay_textbox.Text);
                xwriter.WriteEndElement();

                xwriter.WriteStartElement("Carboncycle");
                xwriter.WriteElementString("carboncycle_active", XmlConvert.ToString(soil_carbon_cycle_checkbox.Checked));
                xwriter.WriteElementString("carbon_input", carbon_input_textbox.Text);
                xwriter.WriteElementString("carbon_depth_decay", carbon_depth_decay_textbox.Text);
                xwriter.WriteElementString("carbon_hum_fraction", carbon_humification_fraction_textbox.Text);
                xwriter.WriteElementString("carbon_y_decomp", carbon_y_decomp_rate_textbox.Text);
                xwriter.WriteElementString("carbon_y_depth_decay", carbon_y_depth_decay_textbox.Text);
                xwriter.WriteElementString("carbon_o_decomp", carbon_o_decomp_rate_textbox.Text);
                xwriter.WriteElementString("carbon_o_depth_decay", carbon_o_depth_decay_textbox.Text);
                xwriter.WriteEndElement();

                xwriter.WriteEndElement();

                xwriter.WriteStartElement("Geochronological_tracers");
                xwriter.WriteElementString("OSL_active", XmlConvert.ToString(OSL_checkbox.Checked));
                xwriter.WriteElementString("ngrains", ngrains_textbox.Text);
                xwriter.WriteElementString("bleachingdepth", bleachingdepth_textbox.Text);
                xwriter.WriteElementString("inherited_age", OSL_inherited_textbox.Text);
                xwriter.WriteElementString("CN_active", XmlConvert.ToString(CN_checkbox.Checked));
                xwriter.WriteElementString("metBe10_input_rate", metBe10_input_textbox.Text);
                xwriter.WriteElementString("metBe10_dd", met10Be_dd.Text);
                xwriter.WriteElementString("Be10_decay", Be10_decay_textbox.Text);
                xwriter.WriteElementString("metBe10_clay", met_10Be_clayfrac.Text);
                xwriter.WriteElementString("met10Be_inherited", metBe10_inherited_textbox.Text);
                xwriter.WriteElementString("isBe10_sp_input", isBe10_sp_input_textbox.Text);
                xwriter.WriteElementString("isBe10_mu_input", isBe10_mu_input_textbox.Text);
                xwriter.WriteElementString("isBe10_inherited", isBe10_inherited_textbox.Text);
                xwriter.WriteElementString("attlength_sp", attenuationlength_sp_textbox.Text);
                xwriter.WriteElementString("attlength_mu", attenuationlength_mu_textbox.Text);
                xwriter.WriteElementString("isC14_sp_input", isC14_sp_input_textbox.Text);
                xwriter.WriteElementString("isC14_mu_input", isC14_mu_input_textbox.Text);
                xwriter.WriteElementString("C14_decay", C14_decay_textbox.Text);
                xwriter.WriteElementString("isC14_inherited", isC14_inherited_textbox.Text);

                xwriter.WriteEndElement();

                xwriter.WriteStartElement("Inputs");
                xwriter.WriteElementString("check_space_DTM", XmlConvert.ToString(check_space_DTM.Checked));
                xwriter.WriteElementString("check_space_soil", XmlConvert.ToString(check_space_soildepth.Checked));
                xwriter.WriteElementString("check_space_landuse", XmlConvert.ToString(check_space_landuse.Checked));
                xwriter.WriteElementString("check_space_tillfields", XmlConvert.ToString(check_space_till_fields.Checked));
                xwriter.WriteElementString("check_space_rain", XmlConvert.ToString(check_space_rain.Checked));
                xwriter.WriteElementString("check_space_infil", XmlConvert.ToString(check_space_infil.Checked));
                xwriter.WriteElementString("check_space_evap", XmlConvert.ToString(check_space_evap.Checked));
                xwriter.WriteElementString("check_time_landuse", XmlConvert.ToString(check_time_landuse.Checked));
                xwriter.WriteElementString("check_time_tillfields", XmlConvert.ToString(check_time_till_fields.Checked));
                xwriter.WriteElementString("check_time_rain", XmlConvert.ToString(check_time_rain.Checked));
                xwriter.WriteElementString("check_time_infil", XmlConvert.ToString(check_time_infil.Checked));
                xwriter.WriteElementString("check_time_evap", XmlConvert.ToString(check_time_evap.Checked));

                xwriter.WriteElementString("dailywater", XmlConvert.ToString(daily_water.Checked));
                xwriter.WriteElementString("dailyP", dailyP.Text);
                xwriter.WriteElementString("dailyET0", dailyET0.Text);
                xwriter.WriteElementString("dailyD", dailyD.Text);
                xwriter.WriteElementString("dailyT_avg", dailyT_avg.Text);
                xwriter.WriteElementString("dailyT_min", dailyT_min.Text);
                xwriter.WriteElementString("dailyT_max", dailyT_max.Text);
                xwriter.WriteElementString("latitude_deg", latitude_deg.Text);
                xwriter.WriteElementString("latitude_min", latitude_min.Text);
                xwriter.WriteElementString("snowmelt_factor", snowmelt_factor_textbox.Text);
                xwriter.WriteElementString("snowmelt_threshold", snow_threshold_textbox.Text);
                xwriter.WriteElementString("daily_n_years", daily_n.Text);
                xwriter.WriteElementString("scaledailyweather", XmlConvert.ToString(check_scaling_daily_weather.Checked));

                xwriter.WriteElementString("dtm_input_filename", dtm_input_filename_textbox.Text);
                if (("input[name='dtm_iterate_checkbox']").Length > 0) { xwriter.WriteElementString("check_iterate_DTM", XmlConvert.ToString(dtm_iterate_checkbox.Checked)); }
                xwriter.WriteElementString("soildepth_input_filename", soildepth_input_filename_textbox.Text);
                xwriter.WriteElementString("landuse_input_filename", landuse_input_filename_textbox.Text);
                xwriter.WriteElementString("tillfields_input_filename", tillfields_input_filename_textbox.Text);
                xwriter.WriteElementString("rain_input_filename", rain_input_filename_textbox.Text);
                xwriter.WriteElementString("infil_input_filename", infil_input_filename_textbox.Text);
                xwriter.WriteElementString("evap_input_filename", evap_input_filename_textbox.Text);

                xwriter.WriteElementString("soildepth_constant_value", soildepth_constant_value_box.Text);
                xwriter.WriteElementString("landuse_constant_value", landuse_constant_value_box.Text);
                xwriter.WriteElementString("tillfields_constant_value", tillfields_constant_textbox.Text);
                xwriter.WriteElementString("rain_constant_value", rainfall_constant_value_box.Text);
                xwriter.WriteElementString("infil_constant_value", infil_constant_value_box.Text);
                xwriter.WriteElementString("evap_constant_value", evap_constant_value_box.Text);
                xwriter.WriteElementString("checkbox_layer_thickness", XmlConvert.ToString(checkbox_layer_thickness.Checked));
                xwriter.WriteElementString("max_soil_layers", textbox_max_soil_layers.Text);
                xwriter.WriteElementString("layer_thickness", textbox_layer_thickness.Text);
                xwriter.WriteElementString("check_fill_sinks_before", XmlConvert.ToString(fill_sinks_before_checkbox.Checked));
                xwriter.WriteElementString("check_fill_sinks_during", XmlConvert.ToString(fill_sinks_during_checkbox.Checked));

                xwriter.WriteEndElement();

                xwriter.WriteStartElement("Run");
                xwriter.WriteElementString("runs_radiobutton", XmlConvert.ToString(runs_checkbox.Checked));
                xwriter.WriteElementString("number_runs", Number_runs_textbox.Text);

                xwriter.WriteStartElement("Specialsettings");
                xwriter.WriteElementString("Spitsbergen", XmlConvert.ToString(Spitsbergen_case_study.Checked));
                xwriter.WriteElementString("Luxembourg", XmlConvert.ToString(version_lux_checkbox.Checked));
                xwriter.WriteElementString("Luxlitter", XmlConvert.ToString(luxlitter_checkbox.Checked));
                xwriter.WriteElementString("Konza", XmlConvert.ToString(version_Konza_checkbox.Checked));
                xwriter.WriteElementString("OSL_tracing", XmlConvert.ToString(OSL_checkbox.Checked));
                xwriter.WriteElementString("CN_tracing", XmlConvert.ToString(CN_checkbox.Checked));
                //xwriter.WriteElementString("other", XmlConvert.ToString(runs_checkbox.Checked));
                xwriter.WriteEndElement();

                xwriter.WriteStartElement("CalibrationSensitivity");
                xwriter.WriteElementString("calibration_active_button", XmlConvert.ToString(Calibration_button.Checked));
                xwriter.WriteElementString("calibration_num_paras_string", num_cal_paras_textbox.Text);
                xwriter.WriteElementString("calibration_ratios_string", calibration_ratios_textbox.Text);
                xwriter.WriteElementString("calibration_levels", calibration_levels_textbox.Text);
                xwriter.WriteElementString("calibration_ratio_reduction_per_level", calibration_ratio_reduction_parameter_textbox.Text);
                xwriter.WriteElementString("calibration_observations_file", obsfile_textbox.Text);
                xwriter.WriteEndElement();

                xwriter.WriteEndElement();

                xwriter.WriteStartElement("Output");

                xwriter.WriteStartElement("File_Output");

                xwriter.WriteStartElement("Moment_of_Output");
                xwriter.WriteElementString("final_output_checkbox", XmlConvert.ToString(Final_output_checkbox.Checked));
                xwriter.WriteElementString("regular_output_checkbox", XmlConvert.ToString(Regular_output_checkbox.Checked));
                xwriter.WriteElementString("years_between_outputs", Box_years_output.Text);
                xwriter.WriteEndElement();

                xwriter.WriteStartElement("Type_of_Output");
                xwriter.WriteElementString("cumulative", XmlConvert.ToString(cumulative_output_checkbox.Checked));
                xwriter.WriteElementString("annual", XmlConvert.ToString(annual_output_checkbox.Checked));
                xwriter.WriteEndElement();

                xwriter.WriteStartElement("Maps_to_Output");
                xwriter.WriteElementString("alti", XmlConvert.ToString(Altitude_output_checkbox.Checked));
                xwriter.WriteElementString("altichange", XmlConvert.ToString(Alt_change_output_checkbox.Checked));
                xwriter.WriteElementString("soildepth", XmlConvert.ToString(Soildepth_output_checkbox.Checked));
                xwriter.WriteElementString("all_processes", XmlConvert.ToString(all_process_output_checkbox.Checked));
                xwriter.WriteElementString("waterflow", XmlConvert.ToString(water_output_checkbox.Checked));
                xwriter.WriteElementString("depressions", XmlConvert.ToString(depressions_output_checkbox.Checked));
                xwriter.WriteEndElement();

                xwriter.WriteEndElement();

                xwriter.WriteStartElement("Other_outputs");

                xwriter.WriteStartElement("Timeseries");
                xwriter.WriteElementString("total_erosion", XmlConvert.ToString(timeseries.timeseries_total_ero_check.Checked));
                xwriter.WriteElementString("total_deposition", XmlConvert.ToString(timeseries.timeseries_total_dep_check.Checked));
                xwriter.WriteElementString("net_erosion", XmlConvert.ToString(timeseries.timeseries_net_ero_check.Checked));
                xwriter.WriteElementString("sed_export", XmlConvert.ToString(timeseries.timeseries_sedexport_checkbox.Checked));
                xwriter.WriteElementString("slide", XmlConvert.ToString(timeseries.timeseries_slide_checkbox.Checked));
                xwriter.WriteElementString("SDR", XmlConvert.ToString(timeseries.timeseries_SDR_check.Checked));
                xwriter.WriteElementString("total_average_alt", XmlConvert.ToString(timeseries.timeseries_total_average_alt_check.Checked));
                xwriter.WriteElementString("total_rain", XmlConvert.ToString(timeseries.timeseries_total_rain_check.Checked));
                xwriter.WriteElementString("total_infil", XmlConvert.ToString(timeseries.timeseries_total_infil_check.Checked));
                xwriter.WriteElementString("total_evap", XmlConvert.ToString(timeseries.timeseries_total_evap_check.Checked));
                xwriter.WriteElementString("total_outflow", XmlConvert.ToString(timeseries.timeseries_total_outflow_check.Checked));
                xwriter.WriteElementString("wet_cells", XmlConvert.ToString(timeseries.timeseries_number_waterflow_check.Checked));
                xwriter.WriteElementString("eroded_cells", XmlConvert.ToString(timeseries.timeseries_number_erosion_check.Checked));
                xwriter.WriteElementString("deposited_cells", XmlConvert.ToString(timeseries.timeseries_number_dep_check.Checked));
                xwriter.WriteElementString("outflow_cells", XmlConvert.ToString(timeseries.timeseries_outflow_cells_checkbox.Checked));
                xwriter.WriteElementString("cell_altitude", XmlConvert.ToString(timeseries.timeseries_cell_altitude_check.Checked));
                xwriter.WriteElementString("cell_waterflow", XmlConvert.ToString(timeseries.timeseries_cell_waterflow_check.Checked));
                xwriter.WriteElementString("waterflow_threshold", timeseries.timeseries_textbox_waterflow_threshold.Text);
                xwriter.WriteElementString("erosion_threshold", timeseries.timeseries_textbox_erosion_threshold.Text);
                xwriter.WriteElementString("deposition_threshold", timeseries.timeseries_textbox_deposition_threshold.Text);
                xwriter.WriteElementString("cell_row", timeseries.timeseries_textbox_cell_row.Text);
                xwriter.WriteElementString("cell_col", timeseries.timeseries_textbox_cell_col.Text);
                xwriter.WriteElementString("total_OM_input", XmlConvert.ToString(timeseries.total_OM_input_checkbox.Checked));
                xwriter.WriteElementString("total_average_soil_thickness", XmlConvert.ToString(timeseries.total_average_soilthickness_checkbox.Checked));
                xwriter.WriteElementString("total_phys_weath", XmlConvert.ToString(timeseries.total_phys_weath_checkbox.Checked));
                xwriter.WriteElementString("total_chem_weath", XmlConvert.ToString(timeseries.total_chem_weath_checkbox.Checked));
                xwriter.WriteElementString("total_fine_formed", XmlConvert.ToString(timeseries.total_fine_formed_checkbox.Checked));
                xwriter.WriteElementString("total_fine_eluviated", XmlConvert.ToString(timeseries.total_fine_eluviated_checkbox.Checked));
                xwriter.WriteElementString("total_mass_bioturbed", XmlConvert.ToString(timeseries.total_mass_bioturbed_checkbox.Checked));
                xwriter.WriteElementString("timeseries_soil_depth", XmlConvert.ToString(timeseries.timeseries_soil_depth_checkbox.Checked));
                xwriter.WriteElementString("timeseries_soil_mass", XmlConvert.ToString(timeseries.timeseries_soil_mass_checkbox.Checked));
                xwriter.WriteElementString("timeseries_coarser", XmlConvert.ToString(timeseries.timeseries_coarser_checkbox.Checked));
                xwriter.WriteElementString("timeseries_thicker", XmlConvert.ToString(timeseries.timeseries_number_soil_thicker_checkbox.Checked));
                xwriter.WriteElementString("soil_cell", timeseries.timeseries_soil_cell_col.Text);
                xwriter.WriteElementString("soil_col", timeseries.timeseries_soil_cell_row.Text);
                xwriter.WriteElementString("coarser_fraction", timeseries.timeseries_soil_coarser_fraction_textbox.Text);
                xwriter.WriteElementString("thicker_threshold", timeseries.timeseries_soil_thicker_textbox.Text);
                xwriter.WriteEndElement();

                xwriter.WriteStartElement("Profiles");
                xwriter.WriteElementString("profile1_row", XmlConvert.ToString(profile.radio_pro1_row.Checked));
                xwriter.WriteElementString("profile1_col", XmlConvert.ToString(profile.radio_pro1_col.Checked));
                xwriter.WriteElementString("profile2_row", XmlConvert.ToString(profile.radio_pro2_row.Checked));
                xwriter.WriteElementString("profile2_col", XmlConvert.ToString(profile.radio_pro2_col.Checked));
                xwriter.WriteElementString("profile3_row", XmlConvert.ToString(profile.radio_pro3_row.Checked));
                xwriter.WriteElementString("profile3_col", XmlConvert.ToString(profile.radio_pro3_col.Checked));
                xwriter.WriteElementString("p1_number", profile.p1_row_col_box.Text);
                xwriter.WriteElementString("p2_number", profile.p2_row_col_box.Text);
                xwriter.WriteElementString("p3_number", profile.p3_row_col_box.Text);
                xwriter.WriteElementString("p1_waterflow", XmlConvert.ToString(profile.check_waterflow_profile1.Checked));
                xwriter.WriteElementString("p1_altitude", XmlConvert.ToString(profile.check_altitude_profile1.Checked));
                xwriter.WriteElementString("p2_waterflow", XmlConvert.ToString(profile.check_waterflow_profile2.Checked));
                xwriter.WriteElementString("p2_altitude", XmlConvert.ToString(profile.check_altitude_profile2.Checked));
                xwriter.WriteElementString("p3_waterflow", XmlConvert.ToString(profile.check_waterflow_profile3.Checked));
                xwriter.WriteElementString("p3_altitude", XmlConvert.ToString(profile.check_altitude_profile3.Checked));
                xwriter.WriteEndElement();

                xwriter.WriteEndElement();

                xwriter.WriteStartElement("Soilfractions");
                xwriter.WriteElementString("coarsefrac", soildata.coarsebox.Text);
                xwriter.WriteElementString("sandfrac", soildata.sandbox.Text);
                xwriter.WriteElementString("siltfrac", soildata.siltbox.Text);
                xwriter.WriteElementString("clayfrac", soildata.claybox.Text);
                xwriter.WriteElementString("fclayfrac", soildata.fineclaybox.Text);
                xwriter.WriteEndElement();
                //End the document
                xwriter.WriteEndDocument();

                //Flush the xml document to the underlying stream and
                //close the underlying stream. The data will not be
                //written out to the stream until either the Flush()
                //method is called or the Close() method is called.
                xwriter.Close();

                this.Text = basetext + " (" + Path.GetFileName(cfgname) + ")";
            }
        }
        private void read_soil_elevation_distance_from_output(int time, string dir)
        {
            // read latest output and start calculating from there
            dir = dir + "\\";

            initialise_once();

            filename = dir + "0_" + time + "_out_dtm.asc";
            read_double(filename, dtm);
            Debug.WriteLine("read dtm");

            filename = dir + "0_" + time + "_out_soildepth.asc";
            read_double(filename, soildepth_m);
            Debug.WriteLine("read soildepth");

            filename = dir + "0_" + time + "_out_change.asc";
            read_double(filename, dtmchange_m);
            Debug.WriteLine("read dtm change");

            if (Water_ero_checkbox.Checked)
            {
                filename = dir + "0_" + time + "_out_water_erosion.asc";
                read_double(filename, sum_water_erosion);
                Debug.WriteLine("read water erosion");
            }

            if (Tillage_checkbox.Checked)
            {
                filename = dir + "0_" + time + "_out_tillage.asc";
                read_double(filename, sum_tillage);
                Debug.WriteLine("read sum_tillage");
            }

            if (Creep_Checkbox.Checked)
            {
                filename = dir + "0_" + time + "_out_creep.asc";
                read_double(filename, creep);
                Debug.WriteLine("read creep");
            }

            if (treefall_checkbox.Checked)
            {
                filename = dir + "0_" + time + "_out_dz_treefall.asc";
                read_double(filename, dz_treefall);
                Debug.WriteLine("read dz_treefall");
            }

            filename = dir + "0_" + time + "_out_dz_soil.asc";
            read_double(filename, dz_soil);
            Debug.WriteLine("read sum_dz_soil");
            // SOIL INFORMATION
            // reset old info
            for (row = 0; row < nr; row++)
            {
                for (col = 0; col < nc; col++)
                {
                    for (int lay = 0; lay < max_soil_layers; lay++)
                    {
                        texture_kg[row, col, lay, 0] = 0;
                        texture_kg[row, col, lay, 1] = 0;
                        texture_kg[row, col, lay, 2] = 0;
                        texture_kg[row, col, lay, 3] = 0;
                        texture_kg[row, col, lay, 4] = 0;
                        young_SOM_kg[row, col, lay] = 0;
                        old_SOM_kg[row, col, lay] = 0;
                        layerthickness_m[row, col, lay] = 0;
                        bulkdensity[row, col, lay] = 0;
                    }
                }
            }

            using (var reader = new StreamReader(dir + "t" + time + "_out_allsoils.csv"))
            {
                int row, col, lay;
                // discard first line (header)    
                var line = reader.ReadLine();
                var values = line.Split(',');

                while (!reader.EndOfStream)
                {
                    line = reader.ReadLine();
                    values = line.Split(',');

                    row = Convert.ToInt32(values[0]);
                    col = Convert.ToInt32(values[1]);
                    lay = Convert.ToInt32(values[3]);

                    texture_kg[row, col, lay, 0] = Convert.ToDouble(values[8]); //coarse
                    texture_kg[row, col, lay, 1] = Convert.ToDouble(values[9]); // sand
                    texture_kg[row, col, lay, 2] = Convert.ToDouble(values[10]); // silt
                    texture_kg[row, col, lay, 3] = Convert.ToDouble(values[11]); // clay
                    texture_kg[row, col, lay, 4] = Convert.ToDouble(values[12]); // fine clay
                    young_SOM_kg[row, col, lay] = Convert.ToDouble(values[13]); // young SOM
                    old_SOM_kg[row, col, lay] = Convert.ToDouble(values[14]); // old SOM
                    layerthickness_m[row, col, lay] = Convert.ToDouble(values[5]); // thickness
                    bulkdensity[row, col, lay] = Convert.ToDouble(values[23]); // bulk density

                }
            }
        }
    }
}
