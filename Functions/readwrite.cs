using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.Xml;

namespace LORICA4
{
    public partial class Mother_form
    {
        void calculate_terrain_derivatives()
        {
            //only takes the DTM and calculates key derivatives and writes these to ASCII files
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
                        if (dtm[row, col] != nodata_value)
                        {
                            sum_squared_difference = 0;
                            num_nbs = 0;
                            for (i = (-1); i <= 1; i++)
                            {
                                for (j = (-1); j <= 1; j++)
                                {
                                    if (!(i == 0 && j == 0) && (row + i) >= 0 && (col + j) >= 0 && (row + i) < nr && (col + j) < nc)
                                    {
                                        if (dtm[row + i, col + j] != nodata_value)
                                        {
                                            sum_squared_difference += Math.Pow((dtm[row, col] - dtm[row + i, col + j]), 2);
                                            num_nbs++;
                                        }
                                    }
                                }
                            }
                            if (num_nbs == 0) { terruggedindex[row, col] = nodata_value; }
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
                    ledges[row, col] = nodata_value;
                    nedges[row, col] = nodata_value;
                    hedges[row, col] = nodata_value;
                    hhcliff[row, col] = nodata_value;
                    hlcliff[row, col] = nodata_value;
                    slhcliff[row, col] = nodata_value;
                    sllcliff[row, col] = nodata_value;
                    ledgeheight[row, col] = nodata_value;
                    if (dtm[row, col] != nodata_value)
                    {
                        ledges[row, col] = 0;
                        nedges[row, col] = 0;
                        hedges[row, col] = 0;
                        hhcliff[row, col] = 0;
                        hlcliff[row, col] = 0;
                        slhcliff[row, col] = 0;
                        sllcliff[row, col] = 0;
                        ledgeheight[row, col] = 0;
                        if (ledgenames[row, col] != nodata_value)
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
                                            if (dtm[row + i, col + j] != nodata_value)
                                            {
                                                for (ii = (-1); ii <= 1; ii++)
                                                {
                                                    for (jj = (-1); jj <= 1; jj++)
                                                    {
                                                        if (!(i + ii == 0 && j + jj == 0) && row + i + ii >= 0 && col + j + jj >= 0 && row + i + ii < nr && col + j + jj < nc)
                                                        {
                                                            if (dtm[row + i + ii, col + j + jj] != nodata_value)
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
                    if (dtm[row, col] != nodata_value)
                    {
                        if (ledgeheight[row, col] == nodata_value)
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
                if (index[runner] != nodata_value)
                {
                    row = row_index[runner]; col = col_index[runner];
                    //Debug.WriteLine("now at row " + row + " col " + col + " alt " + dtm[row, col]);
                    if (ledgenames[row, col] != nodata_value)
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
                                    if (dtm[row + i, col + j] != nodata_value)
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
                if (index[runner] != nodata_value)
                {
                    row = row_index[runner]; col = col_index[runner];
                    if (ledgenames[row, col] != nodata_value)
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
                                    if (dtm[row + i, col + j] != nodata_value)
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
                    if (dtm[row, col] != nodata_value)
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
        
        /*void dtm_file_test(string name1)
        {
            string FILE_NAME = name1;
            int z, dem_integer_error = 1;
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
                                if (Spitsbergen_case_study.Checked == true) { original_dtm[row, col] = tttt; dtm[row, col] = nodata_value; }
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
        } */
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

                Debug.WriteLine("DEM extent: nr = " + nr + " nc = " + nc);

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

                if (dx <= 0.1)  //
                                //
                {
                    MessageBox.Show("Make sure that the DEM cell resolution is in meters");
                    start_button.Enabled = true;
                }

                lineArray2 = inputheader[5].Split(new char[] { ' ' });
                sp = 1;
                while (lineArray2[sp] == "") sp++;
                try
                {
                    nodata_value = int.Parse(lineArray2[sp]);
                }
                catch
                {
                    MessageBox.Show("The nodata value of the input ascii DEM must be an integer. Please improve and try again");
                    start_button.Enabled = true;
                }


            }
            catch (Exception)
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
                            if (Spitsbergen_case_study.Checked == true) { original_dtm[row, col] = tttt; dtm[row, col] = nodata_value; }
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
                if (dem_integer_error == 1) { MessageBox.Show("Warning: Digital Elevation Model seems to only contain integer values\n LORICA can proceed, but may experience problems"); }

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

            for (int row = 0; row < nr; row++)
            {
                for (int col = 0; col < nc; col++)
                {
                    if (dtm[row, col] == nodata_value) //AleG
                    {
                        map1[row, col] = nodata_value;
                    }
                }
            }

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

            for (int row = 0; row < nr; row++)
            {
                for (int col = 0; col < nc; col++)
                {
                    if (dtm[row, col] == nodata_value) //AleG
                    {
                        map1[row, col] = nodata_value;
                    }
                }
            }
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

        public void Proglacial_Error() //Proglacial 
        {
            bool is_ProCheckboxChecked = Proglacial_checkbox.Checked;
            string uploaded_Age_Raster = proglacial_input_filename_textbox.Text;

            // If checkbox is not checked but an age raster was uploaded
            if (!is_ProCheckboxChecked && !string.IsNullOrEmpty(uploaded_Age_Raster))
            {
                MessageBox.Show("Age raster uploaded but Proglacial mode not active");

            }
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
                }
                for (row = 0; row < nr; row++)
                {
                    for (col = 0; col < nc; col++)
                    {
                        if (dtm[row, col] != nodata_value) //AleG 
                        {
                            sw.Write("{0:F6}", output[row, col]);
                            sw.Write(" ");
                        }
                        else
                        {
                            sw.Write("{0:F6}", nodata_value); //AleG
                            sw.Write(" ");
                        }

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
                        if (dtm[row, col] != nodata_value) //AleG 
                        {
                            sw.Write(output[row, col]);
                            sw.Write(" ");
                        }

                        else //AleG
                        {
                            sw.Write(nodata_value);
                            sw.Write(" ");
                        }

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
                        if (dtm[row, col] != nodata_value) //AleG 
                        {
                            sw.Write(output[row, col]);
                            sw.Write(" ");
                        }

                        else //AleG
                        {
                            sw.Write(nodata_value);
                            sw.Write(" ");
                        }

                    }

                    sw.Write("\n");
                }
                sw.Close();
            }
        } //end out_short
        void out_bool(string name4, bool[,] output)
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
                }
                for (row = 0; row < nr; row++)
                {
                    for (col = 0; col < nc; col++)
                    {
                        if (dtm[row, col] != nodata_value) //AleG 
                        {
                            sw.Write("{0:F6}", output[row, col]);
                            sw.Write(" ");
                        }
                        else
                        {
                            sw.Write("{0:F6}", nodata_value); //AleG
                            sw.Write(" ");
                        }

                    }
                    sw.Write("\r\n");
                }
                sw.Close();
            }

        }

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



        void writeallsoils(string FILENAME, int t_corr)
        {
            int layer;
            double cumthick, midthick, z_layer;

            using (StreamWriter sw = new StreamWriter(FILENAME))
            {
                sw.Write("row,col,t,nlayer,cumth_m,thick_m,midthick_m,z,coarse_kg,sand_kg,silt_kg,clay_kg,fine_kg,YOM_kg,OOM_kg,YOM/OOM,f_coarse,f_sand,f_silt,f_clay,f_fineclay,ftotal_clay,f_OM,BD");
                if (CN_checkbox.Checked) { sw.Write(",Be-10_meteoric_clay,Be-10_meteoric_silt,Be-10_meteoric_total,Be-10_insitu,C-14_insitu"); }
                sw.Write("\r\n");
                //int t_corr = t;
                //if(t_corr > 0) { t_corr += 1; }
                for (int row = 0; row < nr; row++)
                {
                    for (int col = 0; col < nc; col++)
                    {
                        if (dtm[row, col] != nodata_value)
                        {
                            cumthick = 0;
                            midthick = 0;
                            z_layer = dtm[row, col];
                            for (layer = 0; layer < max_soil_layers; layer++) // only the top layer
                            {
                                if (layerthickness_m[row, col, layer] >= 0)
                                {
                                    cumthick += layerthickness_m[row, col, layer];
                                    midthick += layerthickness_m[row, col, layer] / 2;
                                    double totalweight = texture_kg[row, col, layer, 0] + texture_kg[row, col, layer, 1] + texture_kg[row, col, layer, 2] + texture_kg[row, col, layer, 3] + texture_kg[row, col, layer, 4] + young_SOM_kg[row, col, layer] + old_SOM_kg[row, col, layer];
                                    double totalweight_tex = texture_kg[row, col, layer, 0] + texture_kg[row, col, layer, 1] + texture_kg[row, col, layer, 2] + texture_kg[row, col, layer, 3] + texture_kg[row, col, layer, 4];
                                    sw.Write(row + "," + col + "," + t_corr + "," + layer + "," + cumthick + "," + layerthickness_m[row, col, layer] + "," + midthick + "," + z_layer + "," + texture_kg[row, col, layer, 0] + "," + texture_kg[row, col, layer, 1] + "," + texture_kg[row, col, layer, 2] + "," + texture_kg[row, col, layer, 3] + "," + texture_kg[row, col, layer, 4] + "," + young_SOM_kg[row, col, layer] + "," + old_SOM_kg[row, col, layer] + "," + young_SOM_kg[row, col, layer] / old_SOM_kg[row, col, layer] + "," + texture_kg[row, col, layer, 0] / totalweight_tex + "," + texture_kg[row, col, layer, 1] / totalweight_tex + "," + texture_kg[row, col, layer, 2] / totalweight_tex + "," + texture_kg[row, col, layer, 3] / totalweight_tex + "," + texture_kg[row, col, layer, 4] / totalweight_tex + "," + (texture_kg[row, col, layer, 3] + texture_kg[row, col, layer, 4]) / totalweight_tex + "," + (young_SOM_kg[row, col, layer] + old_SOM_kg[row, col, layer]) / (young_SOM_kg[row, col, layer] + old_SOM_kg[row, col, layer] + totalweight_tex) + "," + bulkdensity[row, col, layer]);

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
                altidiff = large_negative_number; non_lake_altidiff = 0; maxaltidiff = 0; non_lake_maxi = 0; non_lake_maxj = 0; maxi = 0; maxj = 0;
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
            var ofd = new OpenFileDialog
            {
                InitialDirectory = workdir,
                Filter = "cfg files (*.xml)|*.xml|All files (*.*)|*.*",
                FilterIndex = 1,
                RestoreDirectory = false
            };
            if (ofd.ShowDialog() != DialogResult.OK) return;
            cfgname = ofd.FileName;

            var settings = new XmlReaderSettings
            {
                IgnoreComments = true,
                IgnoreWhitespace = true,
                DtdProcessing = DtdProcessing.Prohibit,
                ConformanceLevel = ConformanceLevel.Document,
                CheckCharacters = true
            };

            int read_error = 0;
            int totalExpected = 0, totalFound = 0;
            var missing = new List<string>();

            // Helper: expected keys list
            List<string> K(params string[] keys) => new List<string>(keys);

            // Read all leaf values (including empty) inside a section subtree
            Dictionary<string, string> ReadDict(XmlReader xr)
            {
                var d = new Dictionary<string, string>(StringComparer.Ordinal);
                xr.MoveToContent();
                int rootDepth = xr.Depth; xr.Read();

                while (!xr.EOF && xr.Depth > rootDepth)
                {
                    if (xr.NodeType != XmlNodeType.Element) { xr.Read(); continue; }

                    string name = xr.Name;

                    if (xr.IsEmptyElement)
                    {
                        d[name] = string.Empty;
                        xr.Read();
                        continue;
                    }

                    xr.Read(); // into content
                    if (xr.NodeType == XmlNodeType.Text || xr.NodeType == XmlNodeType.CDATA)
                    {
                        d[name] = xr.Value ?? string.Empty;
                        xr.Read(); // should be EndElement
                        if (!xr.EOF && xr.NodeType == XmlNodeType.EndElement) xr.Read();
                        continue;
                    }

                    // Complex element; loop will traverse its children
                }
                return d;
            }

            bool TryGetBool(Dictionary<string, string> d, string key, bool current, out bool v)
            {
                v = current;
                string s; bool parsed;
                if (d.TryGetValue(key, out s) && bool.TryParse(s, out parsed)) { v = parsed; return true; }
                return false;
            }
            bool TryGetInt(Dictionary<string, string> d, string key, int current, out int v)
            {
                v = current;
                string s; int parsed;
                if (d.TryGetValue(key, out s) && int.TryParse(s, out parsed)) { v = parsed; return true; }
                return false;
            }
            string GetStr(Dictionary<string, string> d, string key, string current)
            {
                string s; return d.TryGetValue(key, out s) ? s : current;
            }

            // 1) One-pass scan: collect all sections by name (order/position independent)
            // List every section name you ReadSection for:
            var sectionNames = new HashSet<string>(StringComparer.Ordinal)
    {
        "Water_erosion","Tillage","Weathering","Landsliding","Creep","Tree_fall","Blocks",
        "Physical_weathering","Chemical_weathering","Clay_dynamics","Bioturbation","Carboncycle",
        "Geochronological_tracers","Proglacial","Coarsemap","Inputs","Run","Specialsettings",
        "CalibrationSensitivity","File_Output","Type_of_Output","Maps_to_Output",
        "Timeseries","Soilfractions","Landuse_parameters"
    };

            // Store the last occurrence of each section (there should be one)
            var sections = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);

            try
            {
                using (var scan = XmlReader.Create(cfgname, settings))
                {
                    while (scan.Read())
                    {
                        if (scan.NodeType != XmlNodeType.Element) continue;

                        string name = scan.Name;
                        if (!sectionNames.Contains(name)) continue;

                        using (var sub = scan.ReadSubtree())
                        {
                            var d = ReadDict(sub);
                            sections[name] = d;
                        }
                    }
                }
            }
            catch (XmlException xe)
            {
                Debug.WriteLine("XML error at line " + xe.LineNumber + ", pos " + xe.LinePosition + ": " + xe.Message);
                MessageBox.Show("Failed to read configuration.\nLine " + xe.LineNumber + ", Pos " + xe.LinePosition + ".\n" + xe.Message,
                                "Invalid XML");
                return;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Unexpected error while scanning XML: " + ex);
                MessageBox.Show("Unexpected error while reading configuration.", "Error");
                return;
            }

            // 2) Helper: apply a section from the map and tally keys
            void ApplySection(string name, IList<string> expectedKeys, Action<Dictionary<string, string>> apply, int errCode = 1)
            {
                totalExpected += expectedKeys.Count;

                Dictionary<string, string> d;
                if (sections.TryGetValue(name, out d))
                {
                    foreach (var key in expectedKeys)
                        if (d.ContainsKey(key)) totalFound++;
                    apply(d);
                }
                else
                {
                    read_error = Math.Max(read_error, errCode);
                    string msg = "missing " + name;
                    Debug.WriteLine(msg);
                    missing.Add(msg);
                }
            }

            // 3) Apply all sections (now order and missing no longer cascade)
            ApplySection("Water_erosion", K(
                "water_active", "para_m", "para_n", "para_p", "para_K",
                "para_ero_threshold", "para_rock_protection_const", "para_bio_protection_const", "para_selectivity"
            ), d =>
            {
                bool b;
                if (TryGetBool(d, "water_active", Water_ero_checkbox.Checked, out b)) Water_ero_checkbox.Checked = b;
                parameter_m_textbox.Text = GetStr(d, "para_m", parameter_m_textbox.Text);
                parameter_n_textbox.Text = GetStr(d, "para_n", parameter_n_textbox.Text);
                parameter_conv_textbox.Text = GetStr(d, "para_p", parameter_conv_textbox.Text);
                parameter_K_textbox.Text = GetStr(d, "para_K", parameter_K_textbox.Text);
                erosion_threshold_textbox.Text = GetStr(d, "para_ero_threshold", erosion_threshold_textbox.Text);
                rock_protection_constant_textbox.Text = GetStr(d, "para_rock_protection_const", rock_protection_constant_textbox.Text);
                bio_protection_constant_textbox.Text = GetStr(d, "para_bio_protection_const", bio_protection_constant_textbox.Text);
                selectivity_constant_textbox.Text = GetStr(d, "para_selectivity", selectivity_constant_textbox.Text);
            });

            ApplySection("Tillage", K("tillage_active", "para_plough_depth", "para_tillage_constant"), d =>
            {
                bool b;
                if (TryGetBool(d, "tillage_active", Tillage_checkbox.Checked, out b)) Tillage_checkbox.Checked = b;
                parameter_ploughing_depth_textbox.Text = GetStr(d, "para_plough_depth", parameter_ploughing_depth_textbox.Text);
                parameter_tillage_constant_textbox.Text = GetStr(d, "para_tillage_constant", parameter_tillage_constant_textbox.Text);
            });

            ApplySection("Weathering", K("bio_weathering_active", "para_P0", "para_k1", "para_k2", "para_Pa", "rockweath_method"), d =>
            {
                bool b; int i;
                if (TryGetBool(d, "bio_weathering_active", Biological_weathering_checkbox.Checked, out b)) Biological_weathering_checkbox.Checked = b;
                parameter_P0_textbox.Text = GetStr(d, "para_P0", parameter_P0_textbox.Text);
                parameter_k1_textbox.Text = GetStr(d, "para_k1", parameter_k1_textbox.Text);
                parameter_k2_textbox.Text = GetStr(d, "para_k2", parameter_k2_textbox.Text);
                parameter_Pa_textbox.Text = GetStr(d, "para_Pa", parameter_Pa_textbox.Text);
                if (TryGetInt(d, "rockweath_method", rockweath_method_box.SelectedIndex, out i)) rockweath_method_box.SelectedIndex = i;
            });

            ApplySection("Landsliding", K(
                "landsliding_active", "radio_ls_absolute", "radio_ls_fraction",
                "para_absolute_rain_intens", "para_relative_rain_intens",
                "radio_ls_mix_1", "radio_ls_mix_2", "radio_ls_mix_3",
                "minimum_slope_for_movement_tan"
            ), d =>
            {
                bool b;
                if (TryGetBool(d, "landsliding_active", Landslide_checkbox.Checked, out b)) Landslide_checkbox.Checked = b;
                if (TryGetBool(d, "radio_ls_absolute", radio_ls_absolute.Checked, out b)) radio_ls_absolute.Checked = b;
                if (TryGetBool(d, "radio_ls_fraction", radio_ls_fraction.Checked, out b)) radio_ls_fraction.Checked = b;
                text_ls_abs_rain_intens.Text = GetStr(d, "para_absolute_rain_intens", text_ls_abs_rain_intens.Text);
                text_ls_rel_rain_intens.Text = GetStr(d, "para_relative_rain_intens", text_ls_rel_rain_intens.Text);
                if (TryGetBool(d, "radio_ls_mix_1", ls_mix_radio_1.Checked, out b)) ls_mix_radio_1.Checked = b;
                if (TryGetBool(d, "radio_ls_mix_2", ls_mix_radio_2.Checked, out b)) ls_mix_radio_2.Checked = b;
                if (TryGetBool(d, "radio_ls_mix_3", ls_mix_radio_3.Checked, out b)) ls_mix_radio_3.Checked = b;
                minimum_slope_for_movement_tan_textbox.Text = GetStr(d, "minimum_slope_for_movement_tan", minimum_slope_for_movement_tan_textbox.Text);
                runout_ratio_textbox.Text = GetStr(d, "runout_ratio", runout_ratio_textbox.Text); //AleG
                root_coh_box.Text = GetStr(d, "root_coh", root_coh_box.Text); //AleG

            });

            ApplySection("Creep", K("creep_active", "para_diffusivity"), d =>
            {
                bool b;
                if (TryGetBool(d, "creep_active", creep_active_checkbox.Checked, out b)) creep_active_checkbox.Checked = b;
                parameter_diffusivity_textbox.Text = GetStr(d, "para_diffusivity", parameter_diffusivity_textbox.Text);
            });

            ApplySection("Tree_fall", K("treefall_active", "tf_width", "tf_depth", "tf_growth", "tf_age", "tf_freq"), d =>
            {
                bool b;
                if (TryGetBool(d, "treefall_active", treefall_checkbox.Checked, out b)) treefall_checkbox.Checked = b;
                tf_W.Text = GetStr(d, "tf_width", tf_W.Text);
                tf_D.Text = GetStr(d, "tf_depth", tf_D.Text);
                tf_growth.Text = GetStr(d, "tf_growth", tf_growth.Text);
                tf_age.Text = GetStr(d, "tf_age", tf_age.Text);
                tf_freq.Text = GetStr(d, "tf_freq", tf_freq.Text);
            });

            ApplySection("Blocks", K("blocks_active", "hardlayerthickness", "hardlayerelevation", "hardlayerdensity", "hardlayerweath", "blockweath", "blockminsize"), d =>
            {
                bool b;
                if (TryGetBool(d, "blocks_active", blocks_active_checkbox.Checked, out b)) blocks_active_checkbox.Checked = b;
                hardlayerthickness_textbox.Text = GetStr(d, "hardlayerthickness", hardlayerthickness_textbox.Text);
                hardlayerelevation_textbox.Text = GetStr(d, "hardlayerelevation", hardlayerelevation_textbox.Text);
                hardlayerdensity_textbox.Text = GetStr(d, "hardlayerdensity", hardlayerdensity_textbox.Text);
                hardlayerweath_textbox.Text = GetStr(d, "hardlayerweath", hardlayerweath_textbox.Text);
                blockweath_textbox.Text = GetStr(d, "blockweath", blockweath_textbox.Text);
                blocksize_textbox.Text = GetStr(d, "blockminsize", blocksize_textbox.Text);
            });

            ApplySection("Physical_weathering", K("phys_weath_active", "weath_rate_constant", "constant1", "constant2", "size_coarse", "size_sand", "size_silt", "size_clay", "size_fine"), d =>
            {
                bool b;
                if (TryGetBool(d, "phys_weath_active", soil_phys_weath_checkbox.Checked, out b)) soil_phys_weath_checkbox.Checked = b;
                Physical_weath_C1_textbox.Text = GetStr(d, "weath_rate_constant", Physical_weath_C1_textbox.Text);
                physical_weath_constant1.Text = GetStr(d, "constant1", physical_weath_constant1.Text);
                physical_weath_constant2.Text = GetStr(d, "constant2", physical_weath_constant2.Text);
                soildata.upper_particle_coarse_textbox.Text = GetStr(d, "size_coarse", soildata.upper_particle_coarse_textbox.Text);
                soildata.upper_particle_sand_textbox.Text = GetStr(d, "size_sand", soildata.upper_particle_sand_textbox.Text);
                soildata.upper_particle_silt_textbox.Text = GetStr(d, "size_silt", soildata.upper_particle_silt_textbox.Text);
                soildata.upper_particle_clay_textbox.Text = GetStr(d, "size_clay", soildata.upper_particle_clay_textbox.Text);
                soildata.upper_particle_fine_clay_textbox.Text = GetStr(d, "size_fine", soildata.upper_particle_fine_clay_textbox.Text);
            });

            ApplySection("Chemical_weathering", K("chem_weath_active", "chemical_weathering_constant", "constant3", "constant4", "surface_coarse", "surface_sand", "surface_silt", "surface_clay", "surface_fine_clay"), d =>
            {
                bool b;
                if (TryGetBool(d, "chem_weath_active", soil_chem_weath_checkbox.Checked, out b)) soil_chem_weath_checkbox.Checked = b;
                chem_weath_rate_constant_textbox.Text = GetStr(d, "chemical_weathering_constant", chem_weath_rate_constant_textbox.Text);
                chem_weath_depth_constant_textbox.Text = GetStr(d, "constant3", chem_weath_depth_constant_textbox.Text);
                chem_weath_specific_coefficient_textbox.Text = GetStr(d, "constant4", chem_weath_specific_coefficient_textbox.Text);
                clay_neoform_constant_textbox.Text = GetStr(d, "neoform_rate_constant", clay_neoform_constant_textbox.Text);
                clay_neoform_C1_textbox.Text = GetStr(d, "constant5", clay_neoform_C1_textbox.Text);
                clay_neoform_C2_textbox.Text = GetStr(d, "constant6", clay_neoform_C2_textbox.Text);
                soildata.specific_area_coarse_textbox.Text = GetStr(d, "surface_coarse", soildata.specific_area_coarse_textbox.Text);
                soildata.specific_area_sand_textbox.Text = GetStr(d, "surface_sand", soildata.specific_area_sand_textbox.Text);
                soildata.specific_area_silt_textbox.Text = GetStr(d, "surface_silt", soildata.specific_area_silt_textbox.Text);
                soildata.specific_area_clay_textbox.Text = GetStr(d, "surface_clay", soildata.specific_area_clay_textbox.Text);
                soildata.specific_area_fine_clay_textbox.Text = GetStr(d, "surface_fine_clay", soildata.specific_area_fine_clay_textbox.Text);
            });

            ApplySection("Clay_dynamics", K("clay_dynamics_active", "neoform_rate_constant", "constant5", "constant6", "max_eluviation", "eluviation_coefficient", "ct_Jagercikova_active", "ct_v0_Jagercikova", "ct_dd_Jagercikova"), d =>
            {
                bool b;
                if (TryGetBool(d, "clay_dynamics_active", soil_clay_transloc_checkbox.Checked, out b)) soil_clay_transloc_checkbox.Checked = b;
                                maximum_eluviation_textbox.Text = GetStr(d, "max_eluviation", maximum_eluviation_textbox.Text);
                eluviation_coefficient_textbox.Text = GetStr(d, "eluviation_coefficient", eluviation_coefficient_textbox.Text);
                if (TryGetBool(d, "ct_Jagercikova_active", ct_Jagercikova.Checked, out b)) ct_Jagercikova.Checked = b;
                ct_v0_Jagercikova.Text = GetStr(d, "ct_v0_Jagercikova", ct_v0_Jagercikova.Text);
                ct_dd_Jagercikova.Text = GetStr(d, "ct_dd_Jagercikova", ct_dd_Jagercikova.Text);
            });

            ApplySection("Bioturbation", K("bioturbation_active", "potential_bioturb", "bioturb_depth_decay", "bt_depth_function"), d =>
            {
                bool b; int i;
                if (TryGetBool(d, "bioturbation_active", soil_bioturb_checkbox.Checked, out b)) soil_bioturb_checkbox.Checked = b;
                potential_bt_mixing_textbox.Text = GetStr(d, "potential_bioturb", potential_bt_mixing_textbox.Text);
                bt_depth_decay_textbox.Text = GetStr(d, "bioturb_depth_decay", bt_depth_decay_textbox.Text);
                if (TryGetInt(d, "bt_depth_function", bt_depthfunction_box.SelectedIndex, out i)) bt_depthfunction_box.SelectedIndex = i;
            });

            ApplySection("Carboncycle", K("carboncycle_active", "som_cycle_algorithm", "carbon_input", "carbon_depth_decay", "carbon_hum_fraction", "carbon_y_decomp", "carbon_y_depth_decay", "carbon_o_decomp", "carbon_o_depth_decay"), d =>
            {
                bool b; int i;
                if (TryGetBool(d, "carboncycle_active", soil_carbon_cycle_checkbox.Checked, out b)) soil_carbon_cycle_checkbox.Checked = b;
                if (TryGetInt(d, "som_cycle_algorithm", som_cycle_algorithm_box.SelectedIndex, out i)) som_cycle_algorithm_box.SelectedIndex = i;
                carbon_input_textbox.Text = GetStr(d, "carbon_input", carbon_input_textbox.Text);
                carbon_depth_decay_textbox.Text = GetStr(d, "carbon_depth_decay", carbon_depth_decay_textbox.Text);
                carbon_humification_fraction_textbox.Text = GetStr(d, "carbon_hum_fraction", carbon_humification_fraction_textbox.Text);
                carbon_y_decomp_rate_textbox.Text = GetStr(d, "carbon_y_decomp", carbon_y_decomp_rate_textbox.Text);
                carbon_y_depth_decay_textbox.Text = GetStr(d, "carbon_y_depth_decay", carbon_y_depth_decay_textbox.Text);
                carbon_o_decomp_rate_textbox.Text = GetStr(d, "carbon_o_decomp", carbon_o_decomp_rate_textbox.Text);
                carbon_o_depth_decay_textbox.Text = GetStr(d, "carbon_o_depth_decay", carbon_o_depth_decay_textbox.Text);
            });

            ApplySection("Geochronological_tracers", K(
                "OSL_active", "ngrains", "bleachingdepth", "inherited_age",
                "CN_active", "metBe10_input_rate", "metBe10_dd", "Be10_decay",
                "metBe10_clay", "met10Be_inherited",
                "isBe10_sp_input", "isBe10_mu_input", "isBe10_inherited",
                "attlength_sp", "attlength_mu", "isC14_sp_input", "isC14_mu_input", "C14_decay", "isC14_inherited"
            ), d =>
            {
                bool b;
                if (TryGetBool(d, "OSL_active", OSL_checkbox.Checked, out b)) OSL_checkbox.Checked = b;
                ngrains_textbox.Text = GetStr(d, "ngrains", ngrains_textbox.Text);
                bleachingdepth_textbox.Text = GetStr(d, "bleachingdepth", bleachingdepth_textbox.Text);
                OSL_inherited_textbox.Text = GetStr(d, "inherited_age", OSL_inherited_textbox.Text);
                if (TryGetBool(d, "CN_active", CN_checkbox.Checked, out b)) CN_checkbox.Checked = b;

                metBe10_input_textbox.Text = GetStr(d, "metBe10_input_rate", metBe10_input_textbox.Text);
                met10Be_dd.Text = GetStr(d, "metBe10_dd", met10Be_dd.Text);
                Be10_decay_textbox.Text = GetStr(d, "Be10_decay", Be10_decay_textbox.Text);
                met_10Be_clayfrac.Text = GetStr(d, "metBe10_clay", met_10Be_clayfrac.Text);
                metBe10_inherited_textbox.Text = GetStr(d, "met10Be_inherited", metBe10_inherited_textbox.Text);

                isBe10_sp_input_textbox.Text = GetStr(d, "isBe10_sp_input", isBe10_sp_input_textbox.Text);
                isBe10_mu_input_textbox.Text = GetStr(d, "isBe10_mu_input", isBe10_mu_input_textbox.Text);
                isBe10_inherited_textbox.Text = GetStr(d, "isBe10_inherited", isBe10_inherited_textbox.Text);

                attenuationlength_sp_textbox.Text = GetStr(d, "attlength_sp", attenuationlength_sp_textbox.Text);
                attenuationlength_mu_textbox.Text = GetStr(d, "attlength_mu", attenuationlength_mu_textbox.Text);

                isC14_sp_input_textbox.Text = GetStr(d, "isC14_sp_input", isC14_sp_input_textbox.Text);
                isC14_mu_input_textbox.Text = GetStr(d, "isC14_mu_input", isC14_mu_input_textbox.Text);
                C14_decay_textbox.Text = GetStr(d, "C14_decay", C14_decay_textbox.Text);
                isC14_inherited_textbox.Text = GetStr(d, "isC14_inherited", isC14_inherited_textbox.Text);
            });

            ApplySection("Proglacial", K("check_proglacial", "proglacial_input_filename", "melt_rate_m_1971", "melt_rate_m_1972"), d =>
            {
                bool b;
                if (TryGetBool(d, "check_proglacial", Proglacial_checkbox.Checked, out b)) Proglacial_checkbox.Checked = b;
                proglacial_input_filename_textbox.Text = GetStr(d, "proglacial_input_filename", proglacial_input_filename_textbox.Text);
                melt_rate_1971_textbox.Text = GetStr(d, "melt_rate_m_1971", melt_rate_1971_textbox.Text);
                melt_rate_1972_textbox.Text = GetStr(d, "melt_rate_m_1972", melt_rate_1972_textbox.Text);
            });

            ApplySection("Coarsemap", K("check_coarsemap", "coarsemap_input_filename", "sand_ratio", "silt_ratio", "clay_ratio"), d =>
            {
                bool b;
                if (TryGetBool(d, "check_coarsemap", coarsemap_checkbox.Checked, out b)) coarsemap_checkbox.Checked = b;
                coarsemap_input_filename_textbox.Text = GetStr(d, "coarsemap_input_filename", coarsemap_input_filename_textbox.Text);
                coarsemap_sand_ratio_box.Text = GetStr(d, "sand_ratio", coarsemap_sand_ratio_box.Text);
                coarsemap_silt_ratio_box.Text = GetStr(d, "silt_ratio", coarsemap_silt_ratio_box.Text);
                coarsemap_clay_ratio_box.Text = GetStr(d, "clay_ratio", coarsemap_clay_ratio_box.Text);
            });

            ApplySection("Inputs", K(
                "check_space_DTM", "check_space_soil", "check_space_landuse", "check_space_tillfields",
                "check_space_rain", "check_space_infil", "check_space_evap",
                "check_time_landuse", "check_time_tillfields", "check_time_rain", "check_time_infil", "check_time_evap",
                "dailywater", "dailyP", "dailyET0", "dailyD", "dailyT_avg", "dailyT_min", "dailyT_max",
                "latitude_deg", "latitude_min", "snowmelt_factor", "snowmelt_threshold", "daily_n_years", "scaledailyweather",
                "dtm_input_filename", "check_iterate_DTM", "soildepth_input_filename", "landuse_input_filename",
                "tillfields_input_filename", "rain_input_filename", "infil_input_filename", "evap_input_filename",
                "soildepth_constant_value", "landuse_constant_value", "tillfields_constant_value", "rain_constant_value",
                "infil_constant_value", "evap_constant_value",
                "max_soil_layers", "layer_thickness", "layer_thickness_increase",
                "check_fill_sinks_before", "check_fill_sinks_during"
            ), d =>
            {
                bool b;
                if (TryGetBool(d, "check_space_DTM", check_space_DTM.Checked, out b)) check_space_DTM.Checked = b;
                if (TryGetBool(d, "check_space_soil", check_space_soildepth.Checked, out b)) check_space_soildepth.Checked = b;
                if (TryGetBool(d, "check_space_landuse", check_space_landuse.Checked, out b)) check_space_landuse.Checked = b;
                if (TryGetBool(d, "check_space_tillfields", check_space_till_fields.Checked, out b)) check_space_till_fields.Checked = b;
                if (TryGetBool(d, "check_space_rain", check_space_rain.Checked, out b)) check_space_rain.Checked = b;
                if (TryGetBool(d, "check_space_infil", check_space_infil.Checked, out b)) check_space_infil.Checked = b;
                if (TryGetBool(d, "check_space_evap", check_space_evap.Checked, out b)) check_space_evap.Checked = b;
                if (TryGetBool(d, "check_time_landuse", check_time_landuse.Checked, out b)) check_time_landuse.Checked = b;
                if (TryGetBool(d, "check_time_tillfields", check_time_till_fields.Checked, out b)) check_time_till_fields.Checked = b;
                if (TryGetBool(d, "check_time_rain", check_time_rain.Checked, out b)) check_time_rain.Checked = b;
                if (TryGetBool(d, "check_time_infil", check_time_infil.Checked, out b)) check_time_infil.Checked = b;
                if (TryGetBool(d, "check_time_evap", check_time_evap.Checked, out b)) check_time_evap.Checked = b;

                if (TryGetBool(d, "dailywater", daily_water.Checked, out b)) daily_water.Checked = b;
                dailyP.Text = GetStr(d, "dailyP", dailyP.Text);
                dailyET0.Text = GetStr(d, "dailyET0", dailyET0.Text);
                dailyD.Text = GetStr(d, "dailyD", dailyD.Text);
                dailyT_avg.Text = GetStr(d, "dailyT_avg", dailyT_avg.Text);
                dailyT_min.Text = GetStr(d, "dailyT_min", dailyT_min.Text);
                dailyT_max.Text = GetStr(d, "dailyT_max", dailyT_max.Text);
                latitude_deg.Text = GetStr(d, "latitude_deg", latitude_deg.Text);
                latitude_min.Text = GetStr(d, "latitude_min", latitude_min.Text);
                snowmelt_factor_textbox.Text = GetStr(d, "snowmelt_factor", snowmelt_factor_textbox.Text);
                snow_threshold_textbox.Text = GetStr(d, "snowmelt_threshold", snow_threshold_textbox.Text);
                daily_n.Text = GetStr(d, "daily_n_years", daily_n.Text);
                if (TryGetBool(d, "scaledailyweather", check_scaling_daily_weather.Checked, out b)) check_scaling_daily_weather.Checked = b;

                dtm_input_filename_textbox.Text = GetStr(d, "dtm_input_filename", dtm_input_filename_textbox.Text);
                if (TryGetBool(d, "check_iterate_DTM", dtm_iterate_checkbox.Checked, out b)) dtm_iterate_checkbox.Checked = b;
                soildepth_input_filename_textbox.Text = GetStr(d, "soildepth_input_filename", soildepth_input_filename_textbox.Text);
                landuse_input_filename_textbox.Text = GetStr(d, "landuse_input_filename", landuse_input_filename_textbox.Text);
                tillfields_input_filename_textbox.Text = GetStr(d, "tillfields_input_filename", tillfields_input_filename_textbox.Text);
                rain_input_filename_textbox.Text = GetStr(d, "rain_input_filename", rain_input_filename_textbox.Text);
                infil_input_filename_textbox.Text = GetStr(d, "infil_input_filename", infil_input_filename_textbox.Text);
                evap_input_filename_textbox.Text = GetStr(d, "evap_input_filename", evap_input_filename_textbox.Text);

                soildepth_constant_value_box.Text = GetStr(d, "soildepth_constant_value", soildepth_constant_value_box.Text);
                landuse_constant_value_box.Text = GetStr(d, "landuse_constant_value", landuse_constant_value_box.Text);
                tillfields_constant_textbox.Text = GetStr(d, "tillfields_constant_value", tillfields_constant_textbox.Text);
                rainfall_constant_value_box.Text = GetStr(d, "rain_constant_value", rainfall_constant_value_box.Text);
                infil_constant_value_box.Text = GetStr(d, "infil_constant_value", infil_constant_value_box.Text);
                evap_constant_value_box.Text = GetStr(d, "evap_constant_value", evap_constant_value_box.Text);

                textbox_max_soil_layers.Text = GetStr(d, "max_soil_layers", textbox_max_soil_layers.Text);
                textbox_layer_thickness.Text = GetStr(d, "layer_thickness", textbox_layer_thickness.Text);
                textbox_layer_thickness_increase.Text = GetStr(d, "layer_thickness_increase", textbox_layer_thickness_increase.Text);
                if (TryGetBool(d, "check_fill_sinks_before", fill_sinks_before_checkbox.Checked, out b)) fill_sinks_before_checkbox.Checked = b;
                if (TryGetBool(d, "check_fill_sinks_during", fill_sinks_during_checkbox.Checked, out b)) fill_sinks_during_checkbox.Checked = b;
            });

            ApplySection("Run", K("runs_radiobutton", "number_runs"), d =>
            {
                bool b;
                if (TryGetBool(d, "runs_radiobutton", runs_checkbox.Checked, out b)) runs_checkbox.Checked = b;
                Number_runs_textbox.Text = GetStr(d, "number_runs", Number_runs_textbox.Text);
            });

            ApplySection("Specialsettings", K("Spitsbergen", "Luxembourg", "Luxlitter", "Proglacial", "Konza", "OSL_tracing", "CN_tracing"), d =>
            {
                bool b;
                if (TryGetBool(d, "Spitsbergen", Spitsbergen_case_study.Checked, out b)) Spitsbergen_case_study.Checked = b;
                if (TryGetBool(d, "Luxembourg", version_lux_checkbox.Checked, out b)) version_lux_checkbox.Checked = b;
                if (TryGetBool(d, "Luxlitter", luxlitter_checkbox.Checked, out b)) luxlitter_checkbox.Checked = b;
                if (TryGetBool(d, "Proglacial", Proglacial_checkbox.Checked, out b)) Proglacial_checkbox.Checked = b;
                if (TryGetBool(d, "Konza", version_Konza_checkbox.Checked, out b)) version_Konza_checkbox.Checked = b;
                if (TryGetBool(d, "OSL_tracing", OSL_checkbox.Checked, out b)) OSL_checkbox.Checked = b;
                if (TryGetBool(d, "CN_tracing", CN_checkbox.Checked, out b)) CN_checkbox.Checked = b;
            });

            ApplySection("CalibrationSensitivity", K("calibration_active_button", "calibration_num_paras_string", "calibration_ratios_string", "calibration_levels", "calibration_ratio_reduction_per_level", "calibration_observations_file"), d =>
            {
                bool b;
                if (TryGetBool(d, "calibration_active_button", Calibration_button.Checked, out b)) Calibration_button.Checked = b;
                num_cal_paras_textbox.Text = GetStr(d, "calibration_num_paras_string", num_cal_paras_textbox.Text);
                calibration_ratios_textbox.Text = GetStr(d, "calibration_ratios_string", calibration_ratios_textbox.Text);
                calibration_levels_textbox.Text = GetStr(d, "calibration_levels", calibration_levels_textbox.Text);
                calibration_ratio_reduction_parameter_textbox.Text = GetStr(d, "calibration_ratio_reduction_per_level", calibration_ratio_reduction_parameter_textbox.Text);
                obsfile_textbox.Text = GetStr(d, "calibration_observations_file", obsfile_textbox.Text);
            }, errCode: 2);

            ApplySection("File_Output", K("final_output_checkbox", "regular_output_checkbox", "years_between_outputs"), d =>
            {
                bool b;
                if (TryGetBool(d, "final_output_checkbox", Final_output_checkbox.Checked, out b)) Final_output_checkbox.Checked = b;
                if (TryGetBool(d, "regular_output_checkbox", Regular_output_checkbox.Checked, out b)) Regular_output_checkbox.Checked = b;
                Box_years_output.Text = GetStr(d, "years_between_outputs", Box_years_output.Text);
            });

            ApplySection("Type_of_Output", K("cumulative", "annual"), d =>
            {
                bool b;
                if (TryGetBool(d, "cumulative", cumulative_output_checkbox.Checked, out b)) cumulative_output_checkbox.Checked = b;
                if (TryGetBool(d, "annual", annual_output_checkbox.Checked, out b)) annual_output_checkbox.Checked = b;
            });

            ApplySection("Maps_to_Output", K("alti", "altichange", "soildepth", "all_processes", "waterflow", "depressions"), d =>
            {
                bool b;
                if (TryGetBool(d, "alti", Altitude_output_checkbox.Checked, out b)) Altitude_output_checkbox.Checked = b;
                if (TryGetBool(d, "altichange", Alt_change_output_checkbox.Checked, out b)) Alt_change_output_checkbox.Checked = b;
                if (TryGetBool(d, "soildepth", Soildepth_output_checkbox.Checked, out b)) Soildepth_output_checkbox.Checked = b;
                if (TryGetBool(d, "all_processes", all_process_output_checkbox.Checked, out b)) all_process_output_checkbox.Checked = b;
                if (TryGetBool(d, "waterflow", water_output_checkbox.Checked, out b)) water_output_checkbox.Checked = b;
                if (TryGetBool(d, "depressions", depressions_output_checkbox.Checked, out b)) depressions_output_checkbox.Checked = b;
            });

            ApplySection("Timeseries", K(
                "total_erosion", "total_deposition", "net_erosion", "sed_export", "slide", "SDR",
                "total_average_alt", "total_rain", "total_infil", "total_evap", "total_outflow",
                "wet_cells", "eroded_cells", "deposited_cells", "outflow_cells", "cell_altitude", "cell_waterflow",
                "waterflow_threshold", "erosion_threshold", "deposition_threshold", "cell_row", "cell_col",
                "total_OM_input", "total_average_soil_thickness", "total_phys_weath", "total_chem_weath",
                "total_fine_formed", "total_fine_eluviated", "total_mass_bioturbed",
                "timeseries_soil_depth", "timeseries_soil_mass", "timeseries_coarser", "timeseries_thicker",
                "soil_cell", "soil_col", "coarser_fraction", "thicker_threshold"
            ), d =>
            {
                bool b;
                if (TryGetBool(d, "total_erosion", timeseries.timeseries_total_ero_check.Checked, out b)) timeseries.timeseries_total_ero_check.Checked = b;
                if (TryGetBool(d, "total_deposition", timeseries.timeseries_total_dep_check.Checked, out b)) timeseries.timeseries_total_dep_check.Checked = b;
                if (TryGetBool(d, "net_erosion", timeseries.timeseries_net_ero_check.Checked, out b)) timeseries.timeseries_net_ero_check.Checked = b;
                if (TryGetBool(d, "sed_export", timeseries.timeseries_sedexport_checkbox.Checked, out b)) timeseries.timeseries_sedexport_checkbox.Checked = b;
                if (TryGetBool(d, "slide", timeseries.timeseries_slide_checkbox.Checked, out b)) timeseries.timeseries_slide_checkbox.Checked = b;
                if (TryGetBool(d, "SDR", timeseries.timeseries_SDR_check.Checked, out b)) timeseries.timeseries_SDR_check.Checked = b;
                if (TryGetBool(d, "total_average_alt", timeseries.timeseries_total_average_alt_check.Checked, out b)) timeseries.timeseries_total_average_alt_check.Checked = b;
                if (TryGetBool(d, "total_rain", timeseries.timeseries_total_rain_check.Checked, out b)) timeseries.timeseries_total_rain_check.Checked = b;
                if (TryGetBool(d, "total_infil", timeseries.timeseries_total_infil_check.Checked, out b)) timeseries.timeseries_total_infil_check.Checked = b;
                if (TryGetBool(d, "total_evap", timeseries.timeseries_total_evap_check.Checked, out b)) timeseries.timeseries_total_evap_check.Checked = b;
                if (TryGetBool(d, "total_outflow", timeseries.timeseries_total_outflow_check.Checked, out b)) timeseries.timeseries_total_outflow_check.Checked = b;
                if (TryGetBool(d, "wet_cells", timeseries.timeseries_number_waterflow_check.Checked, out b)) timeseries.timeseries_number_waterflow_check.Checked = b;
                if (TryGetBool(d, "eroded_cells", timeseries.timeseries_number_erosion_check.Checked, out b)) timeseries.timeseries_number_erosion_check.Checked = b;
                if (TryGetBool(d, "deposited_cells", timeseries.timeseries_number_dep_check.Checked, out b)) timeseries.timeseries_number_dep_check.Checked = b;
                if (TryGetBool(d, "outflow_cells", timeseries.timeseries_outflow_cells_checkbox.Checked, out b)) timeseries.timeseries_outflow_cells_checkbox.Checked = b;
                if (TryGetBool(d, "cell_altitude", timeseries.timeseries_cell_altitude_check.Checked, out b)) timeseries.timeseries_cell_altitude_check.Checked = b;
                if (TryGetBool(d, "cell_waterflow", timeseries.timeseries_cell_waterflow_check.Checked, out b)) timeseries.timeseries_cell_waterflow_check.Checked = b;

                timeseries.timeseries_textbox_waterflow_threshold.Text = GetStr(d, "waterflow_threshold", timeseries.timeseries_textbox_waterflow_threshold.Text);
                timeseries.timeseries_textbox_erosion_threshold.Text = GetStr(d, "erosion_threshold", timeseries.timeseries_textbox_erosion_threshold.Text);
                timeseries.timeseries_textbox_deposition_threshold.Text = GetStr(d, "deposition_threshold", timeseries.timeseries_textbox_deposition_threshold.Text);
                timeseries.timeseries_textbox_cell_row.Text = GetStr(d, "cell_row", timeseries.timeseries_textbox_cell_row.Text);
                timeseries.timeseries_textbox_cell_col.Text = GetStr(d, "cell_col", timeseries.timeseries_textbox_cell_col.Text);

                if (TryGetBool(d, "total_OM_input", timeseries.total_OM_input_checkbox.Checked, out b)) timeseries.total_OM_input_checkbox.Checked = b;
                if (TryGetBool(d, "total_average_soil_thickness", timeseries.total_average_soilthickness_checkbox.Checked, out b)) timeseries.total_average_soilthickness_checkbox.Checked = b;
                if (TryGetBool(d, "total_phys_weath", timeseries.total_phys_weath_checkbox.Checked, out b)) timeseries.total_phys_weath_checkbox.Checked = b;
                if (TryGetBool(d, "total_chem_weath", timeseries.total_chem_weath_checkbox.Checked, out b)) timeseries.total_chem_weath_checkbox.Checked = b;
                if (TryGetBool(d, "total_fine_formed", timeseries.total_fine_formed_checkbox.Checked, out b)) timeseries.total_fine_formed_checkbox.Checked = b;
                if (TryGetBool(d, "total_fine_eluviated", timeseries.total_fine_eluviated_checkbox.Checked, out b)) timeseries.total_fine_eluviated_checkbox.Checked = b;

                if (TryGetBool(d, "total_mass_bioturbed", timeseries.total_mass_bioturbed_checkbox.Checked, out b)) timeseries.total_mass_bioturbed_checkbox.Checked = b;
                if (TryGetBool(d, "timeseries_soil_depth", timeseries.timeseries_soil_depth_checkbox.Checked, out b)) timeseries.timeseries_soil_depth_checkbox.Checked = b;
                if (TryGetBool(d, "timeseries_soil_mass", timeseries.timeseries_soil_mass_checkbox.Checked, out b)) timeseries.timeseries_soil_mass_checkbox.Checked = b;
                if (TryGetBool(d, "timeseries_coarser", timeseries.timeseries_coarser_checkbox.Checked, out b)) timeseries.timeseries_coarser_checkbox.Checked = b;
                if (TryGetBool(d, "timeseries_thicker", timeseries.timeseries_number_soil_thicker_checkbox.Checked, out b)) timeseries.timeseries_number_soil_thicker_checkbox.Checked = b;

                timeseries.timeseries_soil_cell_col.Text = GetStr(d, "soil_cell", timeseries.timeseries_soil_cell_col.Text);
                timeseries.timeseries_soil_cell_row.Text = GetStr(d, "soil_col", timeseries.timeseries_soil_cell_row.Text);
                timeseries.timeseries_soil_coarser_fraction_textbox.Text = GetStr(d, "coarser_fraction", timeseries.timeseries_soil_coarser_fraction_textbox.Text);
                timeseries.timeseries_soil_thicker_textbox.Text = GetStr(d, "thicker_threshold", timeseries.timeseries_soil_thicker_textbox.Text);
            });

            ApplySection("Soilfractions", K("coarsefrac", "sandfrac", "siltfrac", "clayfrac", "fclayfrac", "yomfrac", "oomfrac"), d =>
            {
                soildata.coarsebox.Text = GetStr(d, "coarsefrac", soildata.coarsebox.Text);
                soildata.sandbox.Text = GetStr(d, "sandfrac", soildata.sandbox.Text);
                soildata.siltbox.Text = GetStr(d, "siltfrac", soildata.siltbox.Text);
                soildata.claybox.Text = GetStr(d, "clayfrac", soildata.claybox.Text);
                soildata.fineclaybox.Text = GetStr(d, "fclayfrac", soildata.fineclaybox.Text);
                soildata.yombox.Text = GetStr(d, "yomfrac", soildata.yombox.Text);
                soildata.oombox.Text = GetStr(d, "oomfrac", soildata.oombox.Text);
            });

            var luKeys = new List<string>();
            for (int i = 1; i <= 10; i++)
            {
                luKeys.Add("LU" + i + "_Ero");
                luKeys.Add("LU" + i + "_Inf");
                luKeys.Add("LU" + i + "_Evap");
                luKeys.Add("LU" + i + "_RootC");
            }
            ApplySection("Landuse_parameters", luKeys, d =>
            {
                for (int i = 1; i <= 10; i++)
                {
                    string eroder = GetStr(d, "LU" + i + "_Ero", null);
                    string inf = GetStr(d, "LU" + i + "_Inf", null);
                    string ev = GetStr(d, "LU" + i + "_Evap", null);
                    string bpr = GetStr(d, "LU" + i + "_BioProt", null);
                    string om = GetStr(d, "LU" + i + "_OM", null);
                    string til = GetStr(d, "LU" + i + "_Till", null);
                    string bt = GetStr(d, "LU" + i + "_Biot", null);
                    string rc = GetStr(d, "LU" + i + "_RootC", null);
                    if (eroder != null) (new[] { landuse_determinator.LU1_Ero_textbox, landuse_determinator.LU2_Ero_textbox, landuse_determinator.LU3_Ero_textbox, landuse_determinator.LU4_Ero_textbox, landuse_determinator.LU5_Ero_textbox, landuse_determinator.LU6_Ero_textbox, landuse_determinator.LU7_Ero_textbox, landuse_determinator.LU8_Ero_textbox, landuse_determinator.LU9_Ero_textbox, landuse_determinator.LU10_Ero_textbox })[i - 1].Text = eroder;
                    if (inf != null) (new[] { landuse_determinator.LU1_Inf_textbox, landuse_determinator.LU2_Inf_textbox, landuse_determinator.LU3_Inf_textbox, landuse_determinator.LU4_Inf_textbox, landuse_determinator.LU5_Inf_textbox, landuse_determinator.LU6_Inf_textbox, landuse_determinator.LU7_Inf_textbox, landuse_determinator.LU8_Inf_textbox, landuse_determinator.LU9_Inf_textbox, landuse_determinator.LU10_Inf_textbox })[i - 1].Text = inf;
                    if (ev != null) (new[] { landuse_determinator.LU1_Evap_textbox, landuse_determinator.LU2_Evap_textbox, landuse_determinator.LU3_Evap_textbox, landuse_determinator.LU4_Evap_textbox, landuse_determinator.LU5_Evap_textbox, landuse_determinator.LU6_Evap_textbox, landuse_determinator.LU7_Evap_textbox, landuse_determinator.LU8_Evap_textbox, landuse_determinator.LU9_Evap_textbox, landuse_determinator.LU10_Evap_textbox })[i - 1].Text = ev;
                    if (bpr != null) (new[] { landuse_determinator.LU1_BioProt_textbox, landuse_determinator.LU2_BioProt_textbox, landuse_determinator.LU3_BioProt_textbox, landuse_determinator.LU4_BioProt_textbox, landuse_determinator.LU5_BioProt_textbox, landuse_determinator.LU6_BioProt_textbox, landuse_determinator.LU7_BioProt_textbox, landuse_determinator.LU8_BioProt_textbox, landuse_determinator.LU9_BioProt_textbox, landuse_determinator.LU10_BioProt_textbox })[i - 1].Text = bpr;
                    if (om != null) (new[] { landuse_determinator.LU1_OM_textbox, landuse_determinator.LU2_OM_textbox, landuse_determinator.LU3_OM_textbox, landuse_determinator.LU4_OM_textbox, landuse_determinator.LU5_OM_textbox, landuse_determinator.LU6_OM_textbox, landuse_determinator.LU7_OM_textbox, landuse_determinator.LU8_OM_textbox, landuse_determinator.LU9_OM_textbox, landuse_determinator.LU10_OM_textbox })[i - 1].Text = om;
                    if (til != null) (new[] { landuse_determinator.LU1_Till_textbox, landuse_determinator.LU2_Till_textbox, landuse_determinator.LU3_Till_textbox, landuse_determinator.LU4_Till_textbox, landuse_determinator.LU5_Till_textbox, landuse_determinator.LU6_Till_textbox, landuse_determinator.LU7_Till_textbox, landuse_determinator.LU8_Till_textbox, landuse_determinator.LU9_Till_textbox, landuse_determinator.LU10_Till_textbox })[i - 1].Text = til;
                    if (bt != null) (new[] { landuse_determinator.LU1_BiotR_textbox, landuse_determinator.LU2_BiotR_textbox, landuse_determinator.LU3_BiotR_textbox, landuse_determinator.LU4_BiotR_textbox, landuse_determinator.LU5_BiotR_textbox, landuse_determinator.LU6_BiotR_textbox, landuse_determinator.LU7_BiotR_textbox, landuse_determinator.LU8_BiotR_textbox, landuse_determinator.LU9_BiotR_textbox, landuse_determinator.LU10_BiotR_textbox })[i - 1].Text = bt;
                    if (rc != null) (new[] { landuse_determinator.LU1_RootC_textbox, landuse_determinator.LU2_RootC_textbox, landuse_determinator.LU3_RootC_textbox, landuse_determinator.LU4_RootC_textbox, landuse_determinator.LU5_RootC_textbox, landuse_determinator.LU6_RootC_textbox, landuse_determinator.LU7_RootC_textbox, landuse_determinator.LU8_RootC_textbox, landuse_determinator.LU9_RootC_textbox, landuse_determinator.LU10_RootC_textbox })[i - 1].Text = rc;
                }
            });

            // UI updates
            this.Text = basetext + " (" + Path.GetFileName(cfgname) + ")";
            start_button.Enabled = true;
            tabControl1.Visible = true;

            // Summary box (no icon), always shown
            double pct = totalExpected > 0 ? (100.0 * totalFound / totalExpected) : 100.0;
            string summary = $"Read {totalFound} of {totalExpected} entries ({pct:F1}%).";
            string details = missing.Count > 0 ? ("\n" + string.Join("\n", missing)) : "";
            string text = summary;

            if (read_error == 2) text = summary + "\nError in some XML lines (CalibrationSensitivity)." + details;
            else if (read_error == 1) text = summary + "\nWarning: not all runfile data could be read.\r\nLORICA can continue" + details;

            MessageBox.Show(this, text, "Configuration");
        }
        private void menuItemConfigFileSave_Click(object sender, System.EventArgs e)
        {
            XmlTextWriter xwriter;

            if ((sender == menuItemConfigFileSaveAs) || (cfgname == null))
            {

                SaveFileDialog saveFileDialog1 = new SaveFileDialog
                {
                    InitialDirectory = workdir,
                    Filter = "cfg files (*.xml)|*.xml|All files (*.*)|*.*",
                    FilterIndex = 1,
                    RestoreDirectory = false
                };

                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    cfgname = saveFileDialog1.FileName;
                }
            }
            if (cfgname != null)
            {

                //Create a new XmlTextWriter.
                xwriter = new XmlTextWriter(cfgname, System.Text.Encoding.UTF8)
                {
                    //Write the beginning of the document including the 
                    //document declaration. Standalone is true. 
                    //Use indentation for readability.
                    Formatting = Formatting.Indented,
                    Indentation = 4
                };

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
                xwriter.WriteElementString("rockweath_method", XmlConvert.ToString(rockweath_method_box.SelectedIndex));//AleG
                xwriter.WriteEndElement();

                xwriter.WriteStartElement("Landsliding");
                xwriter.WriteElementString("landsliding_active", XmlConvert.ToString(Landslide_checkbox.Checked));
                xwriter.WriteElementString("radio_ls_absolute", XmlConvert.ToString(radio_ls_absolute.Checked));
                xwriter.WriteElementString("radio_ls_fraction", XmlConvert.ToString(radio_ls_fraction.Checked));
                xwriter.WriteElementString("para_absolute_rain_intens", text_ls_abs_rain_intens.Text);
                xwriter.WriteElementString("para_relative_rain_intens", text_ls_rel_rain_intens.Text);
                xwriter.WriteElementString("radio_ls_mix_1", XmlConvert.ToString(ls_mix_radio_1.Checked));
                xwriter.WriteElementString("radio_ls_mix_2", XmlConvert.ToString(ls_mix_radio_2.Checked));
                xwriter.WriteElementString("radio_ls_mix_3", XmlConvert.ToString(ls_mix_radio_3.Checked));
                xwriter.WriteElementString("minimum_slope_for_movement_tan", minimum_slope_for_movement_tan_textbox.Text);
                xwriter.WriteElementString("runout_ratio", runout_ratio_textbox.Text);
                xwriter.WriteElementString("root_coh", root_coh_box.Text); //AleG
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
                xwriter.WriteElementString("blocks_active", XmlConvert.ToString(blocks_active_checkbox.Checked));
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
                xwriter.WriteElementString("chemical_weathering_constant", chem_weath_rate_constant_textbox.Text);
                xwriter.WriteElementString("constant3", chem_weath_depth_constant_textbox.Text);
                xwriter.WriteElementString("constant4", chem_weath_specific_coefficient_textbox.Text);
                xwriter.WriteElementString("surface_coarse", soildata.specific_area_coarse_textbox.Text);
                xwriter.WriteElementString("neoform_rate_constant", clay_neoform_constant_textbox.Text);
                xwriter.WriteElementString("constant5", clay_neoform_C1_textbox.Text);
                xwriter.WriteElementString("constant6", clay_neoform_C2_textbox.Text);
                xwriter.WriteElementString("surface_sand", soildata.specific_area_sand_textbox.Text);
                xwriter.WriteElementString("surface_silt", soildata.specific_area_silt_textbox.Text);
                xwriter.WriteElementString("surface_clay", soildata.specific_area_clay_textbox.Text);
                xwriter.WriteElementString("surface_fine_clay", soildata.specific_area_fine_clay_textbox.Text);
                xwriter.WriteEndElement();

                xwriter.WriteStartElement("Clay_dynamics");
                xwriter.WriteElementString("clay_dynamics_active", XmlConvert.ToString(soil_clay_transloc_checkbox.Checked));
                xwriter.WriteElementString("max_eluviation", maximum_eluviation_textbox.Text);
                xwriter.WriteElementString("eluviation_coefficient", eluviation_coefficient_textbox.Text);
                xwriter.WriteElementString("ct_Jagercikova_active", XmlConvert.ToString(ct_Jagercikova.Checked));
                xwriter.WriteElementString("ct_v0_Jagercikova", ct_v0_Jagercikova.Text);
                xwriter.WriteElementString("ct_dd_Jagercikova", ct_dd_Jagercikova.Text);
                xwriter.WriteEndElement();

                xwriter.WriteStartElement("Bioturbation");
                xwriter.WriteElementString("bioturbation_active", XmlConvert.ToString(soil_bioturb_checkbox.Checked));
                xwriter.WriteElementString("potential_bioturb", potential_bt_mixing_textbox.Text);
                xwriter.WriteElementString("bioturb_depth_decay", bt_depth_decay_textbox.Text);
                xwriter.WriteElementString("bt_depth_function", XmlConvert.ToString(bt_depthfunction_box.SelectedIndex)); //AleG
                xwriter.WriteEndElement();

                xwriter.WriteStartElement("Carboncycle");
                xwriter.WriteElementString("carboncycle_active", XmlConvert.ToString(soil_carbon_cycle_checkbox.Checked));
                xwriter.WriteElementString("som_cycle_algorithm", XmlConvert.ToString(som_cycle_algorithm_box.SelectedIndex));//AleG
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

                xwriter.WriteStartElement("Proglacial");
                xwriter.WriteElementString("check_proglacial", XmlConvert.ToString(Proglacial_checkbox.Checked));
                xwriter.WriteElementString("proglacial_input_filename", proglacial_input_filename_textbox.Text); //Proglacial
                xwriter.WriteElementString("melt_rate_m_1971", melt_rate_1971_textbox.Text); //Proglacial
                xwriter.WriteElementString("melt_rate_m_1972", melt_rate_1972_textbox.Text); //Proglacial
                xwriter.WriteEndElement();

                xwriter.WriteStartElement("Coarsemap");
                xwriter.WriteElementString("check_coarsemap", XmlConvert.ToString(coarsemap_checkbox.Checked));
                xwriter.WriteElementString("coarsemap_input_filename", coarsemap_input_filename_textbox.Text); //Coarsemap
                xwriter.WriteElementString("sand_ratio", coarsemap_sand_ratio_box.Text); //Coarsemap
                xwriter.WriteElementString("silt_ratio", coarsemap_silt_ratio_box.Text); //Coarsemap
                xwriter.WriteElementString("clay_ratio", coarsemap_clay_ratio_box.Text); //Coarsemap
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
                xwriter.WriteElementString("check_iterate_DTM", XmlConvert.ToString(dtm_iterate_checkbox.Checked));
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
                xwriter.WriteElementString("max_soil_layers", textbox_max_soil_layers.Text);
                xwriter.WriteElementString("layer_thickness", textbox_layer_thickness.Text);
                xwriter.WriteElementString("layer_thickness_increase", textbox_layer_thickness_increase.Text);
                xwriter.WriteElementString("check_fill_sinks_before", XmlConvert.ToString(fill_sinks_before_checkbox.Checked));
                xwriter.WriteElementString("check_fill_sinks_during", XmlConvert.ToString(fill_sinks_during_checkbox.Checked));

                xwriter.WriteEndElement();

                xwriter.WriteStartElement("Run");
                xwriter.WriteElementString("runs_radiobutton", XmlConvert.ToString(runs_checkbox.Checked));
                xwriter.WriteElementString("number_runs", Number_runs_textbox.Text);
                xwriter.WriteEndElement();

                xwriter.WriteStartElement("Specialsettings");
                xwriter.WriteElementString("Spitsbergen", XmlConvert.ToString(Spitsbergen_case_study.Checked));
                xwriter.WriteElementString("Luxembourg", XmlConvert.ToString(version_lux_checkbox.Checked));
                xwriter.WriteElementString("Luxlitter", XmlConvert.ToString(luxlitter_checkbox.Checked));
                xwriter.WriteElementString("Proglacial", XmlConvert.ToString(Proglacial_checkbox.Checked));
                xwriter.WriteElementString("Konza", XmlConvert.ToString(version_Konza_checkbox.Checked));
                xwriter.WriteElementString("OSL_tracing", XmlConvert.ToString(OSL_checkbox.Checked));
                xwriter.WriteElementString("CN_tracing", XmlConvert.ToString(CN_checkbox.Checked));
                xwriter.WriteEndElement();

                xwriter.WriteStartElement("CalibrationSensitivity");
                xwriter.WriteElementString("calibration_active_button", XmlConvert.ToString(Calibration_button.Checked));
                xwriter.WriteElementString("calibration_num_paras_string", num_cal_paras_textbox.Text);
                xwriter.WriteElementString("calibration_ratios_string", calibration_ratios_textbox.Text);
                xwriter.WriteElementString("calibration_levels", calibration_levels_textbox.Text);
                xwriter.WriteElementString("calibration_ratio_reduction_per_level", calibration_ratio_reduction_parameter_textbox.Text);
                xwriter.WriteElementString("calibration_observations_file", obsfile_textbox.Text);
                xwriter.WriteEndElement();

                xwriter.WriteStartElement("Output");

                xwriter.WriteStartElement("File_Output");

                xwriter.WriteStartElement("Moment_of_Output");
                xwriter.WriteElementString("final_output_checkbox", XmlConvert.ToString(Final_output_checkbox.Checked));
                xwriter.WriteElementString("regular_output_checkbox", XmlConvert.ToString(Regular_output_checkbox.Checked));
                xwriter.WriteElementString("years_between_outputs", Box_years_output.Text);
                xwriter.WriteEndElement(); // </Moment_of_Output>

                xwriter.WriteEndElement(); // </File_Output>

                // Type_of_Output (sibling of File_Output)
                xwriter.WriteStartElement("Type_of_Output");
                xwriter.WriteElementString("cumulative", XmlConvert.ToString(cumulative_output_checkbox.Checked));
                xwriter.WriteElementString("annual", XmlConvert.ToString(annual_output_checkbox.Checked));
                xwriter.WriteEndElement(); // </Type_of_Output>

                // Maps_to_Output (sibling of File_Output)
                xwriter.WriteStartElement("Maps_to_Output");
                xwriter.WriteElementString("alti", XmlConvert.ToString(Altitude_output_checkbox.Checked));
                xwriter.WriteElementString("altichange", XmlConvert.ToString(Alt_change_output_checkbox.Checked));
                xwriter.WriteElementString("soildepth", XmlConvert.ToString(Soildepth_output_checkbox.Checked));
                xwriter.WriteElementString("all_processes", XmlConvert.ToString(all_process_output_checkbox.Checked));
                xwriter.WriteElementString("waterflow", XmlConvert.ToString(water_output_checkbox.Checked));
                xwriter.WriteElementString("depressions", XmlConvert.ToString(depressions_output_checkbox.Checked));
                xwriter.WriteEndElement(); // </Maps_to_Output>

                xwriter.WriteEndElement(); // </Output>

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

                xwriter.WriteEndElement();

                xwriter.WriteStartElement("Soilfractions");
                xwriter.WriteElementString("coarsefrac", soildata.coarsebox.Text);
                xwriter.WriteElementString("sandfrac", soildata.sandbox.Text);
                xwriter.WriteElementString("siltfrac", soildata.siltbox.Text);
                xwriter.WriteElementString("clayfrac", soildata.claybox.Text);
                xwriter.WriteElementString("fclayfrac", soildata.fineclaybox.Text);
                xwriter.WriteElementString("yomfrac", soildata.yombox.Text); //AleG
                xwriter.WriteElementString("oomfrac", soildata.oombox.Text); //AleG
                xwriter.WriteEndElement();

                xwriter.WriteStartElement("Landuse_parameters");
                xwriter.WriteElementString("LU1_Ero", landuse_determinator.LU1_Ero_textbox.Text);
                xwriter.WriteElementString("LU1_Inf", landuse_determinator.LU1_Inf_textbox.Text);
                xwriter.WriteElementString("LU1_Evap", landuse_determinator.LU1_Evap_textbox.Text);
                xwriter.WriteElementString("LU1_BioProt", landuse_determinator.LU1_BioProt_textbox.Text);
                xwriter.WriteElementString("LU1_OM", landuse_determinator.LU1_OM_textbox.Text);
                xwriter.WriteElementString("LU1_Till", landuse_determinator.LU1_Till_textbox.Text);
                xwriter.WriteElementString("LU1_Biot", landuse_determinator.LU1_BiotR_textbox.Text);
                xwriter.WriteElementString("LU1_RootC", landuse_determinator.LU1_RootC_textbox.Text);

                xwriter.WriteElementString("LU2_Ero", landuse_determinator.LU2_Ero_textbox.Text);
                xwriter.WriteElementString("LU2_Inf", landuse_determinator.LU2_Inf_textbox.Text);
                xwriter.WriteElementString("LU2_Evap", landuse_determinator.LU2_Evap_textbox.Text); 
                xwriter.WriteElementString("LU2_BioProt", landuse_determinator.LU2_BioProt_textbox.Text);
                xwriter.WriteElementString("LU2_OM", landuse_determinator.LU2_OM_textbox.Text);
                xwriter.WriteElementString("LU2_Till", landuse_determinator.LU2_Till_textbox.Text);
                xwriter.WriteElementString("LU2_Biot", landuse_determinator.LU2_BiotR_textbox.Text);
                xwriter.WriteElementString("LU2_RootC", landuse_determinator.LU2_RootC_textbox.Text);

                xwriter.WriteElementString("LU3_Ero", landuse_determinator.LU3_Ero_textbox.Text);
                xwriter.WriteElementString("LU3_Inf", landuse_determinator.LU3_Inf_textbox.Text);
                xwriter.WriteElementString("LU3_Evap", landuse_determinator.LU3_Evap_textbox.Text);
                xwriter.WriteElementString("LU3_BioProt", landuse_determinator.LU3_BioProt_textbox.Text);
                xwriter.WriteElementString("LU3_OM", landuse_determinator.LU3_OM_textbox.Text);
                xwriter.WriteElementString("LU3_Till", landuse_determinator.LU3_Till_textbox.Text);
                xwriter.WriteElementString("LU3_Biot", landuse_determinator.LU3_BiotR_textbox.Text);
                xwriter.WriteElementString("LU3_RootC", landuse_determinator.LU3_RootC_textbox.Text);

                xwriter.WriteElementString("LU4_Ero", landuse_determinator.LU4_Ero_textbox.Text);
                xwriter.WriteElementString("LU4_Inf", landuse_determinator.LU4_Inf_textbox.Text);
                xwriter.WriteElementString("LU4_Evap", landuse_determinator.LU4_Evap_textbox.Text);
                xwriter.WriteElementString("LU1_BioProt", landuse_determinator.LU4_BioProt_textbox.Text);
                xwriter.WriteElementString("LU4_OM", landuse_determinator.LU4_OM_textbox.Text);
                xwriter.WriteElementString("LU4_Till", landuse_determinator.LU4_Till_textbox.Text);
                xwriter.WriteElementString("LU4_Biot", landuse_determinator.LU4_BiotR_textbox.Text);
                xwriter.WriteElementString("LU4_RootC", landuse_determinator.LU4_RootC_textbox.Text);

                xwriter.WriteElementString("LU5_Ero", landuse_determinator.LU5_Ero_textbox.Text);
                xwriter.WriteElementString("LU5_Inf", landuse_determinator.LU5_Inf_textbox.Text);
                xwriter.WriteElementString("LU5_Evap", landuse_determinator.LU5_Evap_textbox.Text);
                xwriter.WriteElementString("LU5_BioProt", landuse_determinator.LU5_BioProt_textbox.Text);
                xwriter.WriteElementString("LU5_OM", landuse_determinator.LU5_OM_textbox.Text);
                xwriter.WriteElementString("LU5_Till", landuse_determinator.LU5_Till_textbox.Text);
                xwriter.WriteElementString("LU5_Biot", landuse_determinator.LU5_BiotR_textbox.Text);
                xwriter.WriteElementString("LU5_RootC", landuse_determinator.LU5_RootC_textbox.Text);

                xwriter.WriteElementString("LU6_Ero", landuse_determinator.LU6_Ero_textbox.Text);
                xwriter.WriteElementString("LU6_Inf", landuse_determinator.LU6_Inf_textbox.Text);
                xwriter.WriteElementString("LU6_Evap", landuse_determinator.LU6_Evap_textbox.Text);
                xwriter.WriteElementString("LU6_BioProt", landuse_determinator.LU6_BioProt_textbox.Text);
                xwriter.WriteElementString("LU6_OM", landuse_determinator.LU6_OM_textbox.Text);
                xwriter.WriteElementString("LU6_Till", landuse_determinator.LU6_Till_textbox.Text);
                xwriter.WriteElementString("LU6_Biot", landuse_determinator.LU6_BiotR_textbox.Text);
                xwriter.WriteElementString("LU6_RootC", landuse_determinator.LU6_RootC_textbox.Text);

                xwriter.WriteElementString("LU7_Ero", landuse_determinator.LU7_Ero_textbox.Text);
                xwriter.WriteElementString("LU7_Inf", landuse_determinator.LU7_Inf_textbox.Text);
                xwriter.WriteElementString("LU7_Evap", landuse_determinator.LU7_Evap_textbox.Text);
                xwriter.WriteElementString("LU7_BioProt", landuse_determinator.LU7_BioProt_textbox.Text);
                xwriter.WriteElementString("LU7_OM", landuse_determinator.LU7_OM_textbox.Text);
                xwriter.WriteElementString("LU7_Till", landuse_determinator.LU7_Till_textbox.Text);
                xwriter.WriteElementString("LU7_Biot", landuse_determinator.LU7_BiotR_textbox.Text);
                xwriter.WriteElementString("LU7_RootC", landuse_determinator.LU7_RootC_textbox.Text);

                xwriter.WriteElementString("LU8_Ero", landuse_determinator.LU8_Ero_textbox.Text);
                xwriter.WriteElementString("LU8_Inf", landuse_determinator.LU8_Inf_textbox.Text);
                xwriter.WriteElementString("LU8_Evap", landuse_determinator.LU8_Evap_textbox.Text);
                xwriter.WriteElementString("LU8_BioProt", landuse_determinator.LU8_BioProt_textbox.Text);
                xwriter.WriteElementString("LU8_OM", landuse_determinator.LU8_OM_textbox.Text);
                xwriter.WriteElementString("LU8_Till", landuse_determinator.LU8_Till_textbox.Text);
                xwriter.WriteElementString("LU8_Biot", landuse_determinator.LU8_BiotR_textbox.Text);
                xwriter.WriteElementString("LU8_RootC", landuse_determinator.LU8_RootC_textbox.Text);

                xwriter.WriteElementString("LU9_Ero", landuse_determinator.LU9_Ero_textbox.Text);
                xwriter.WriteElementString("LU9_Inf", landuse_determinator.LU9_Inf_textbox.Text);
                xwriter.WriteElementString("LU9_Evap", landuse_determinator.LU9_Evap_textbox.Text);
                xwriter.WriteElementString("LU9_BioProt", landuse_determinator.LU9_BioProt_textbox.Text);
                xwriter.WriteElementString("LU9_OM", landuse_determinator.LU9_OM_textbox.Text);
                xwriter.WriteElementString("LU9_Till", landuse_determinator.LU9_Till_textbox.Text);
                xwriter.WriteElementString("LU9_Biot", landuse_determinator.LU9_BiotR_textbox.Text);
                xwriter.WriteElementString("LU9_RootC", landuse_determinator.LU9_RootC_textbox.Text);

                xwriter.WriteElementString("LU10_Ero", landuse_determinator.LU10_Ero_textbox.Text);
                xwriter.WriteElementString("LU10_Inf", landuse_determinator.LU10_Inf_textbox.Text);
                xwriter.WriteElementString("LU10_Evap", landuse_determinator.LU10_Evap_textbox.Text);
                xwriter.WriteElementString("LU10_BioProt", landuse_determinator.LU10_BioProt_textbox.Text);
                xwriter.WriteElementString("LU10_OM", landuse_determinator.LU10_OM_textbox.Text);
                xwriter.WriteElementString("LU10_Till", landuse_determinator.LU10_Till_textbox.Text);
                xwriter.WriteElementString("LU10_Biot", landuse_determinator.LU10_BiotR_textbox.Text);
                xwriter.WriteElementString("LU10_RootC", landuse_determinator.LU10_RootC_textbox.Text);

                xwriter.WriteEndElement();
                xwriter.WriteEndElement();
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
            dir += "\\";

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

            if (Proglacial_checkbox.Checked) //Proglacial
            {
                filename = dir + "0_" + time + "_out_meltwater.asc";
                read_double(filename, sum_meltwater_m);
                Debug.WriteLine("read meltwater");
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

        int min_value_int(int[,] map2, int nr, int nc) //Proglacial 
        {
            int loopnr_min = 0;
            int min_val = 0;
            int rast_val = 0;
            int row;
            int col;

            for (row = 0; row < nr; row++)
            {
                for (col = 0; col < nc; col++)
                {
                    if (map2[row, col] != nodata_value)
                    {
                        rast_val = map2[row, col];

                        if (loopnr_min == 0)
                        {
                            min_val = rast_val;
                            loopnr_min++;
                        }
                        else
                        {
                            if (rast_val < min_val)
                            {
                                min_val = rast_val;
                            }
                        }

                    }


                }

            }
            return min_val;

        } // end min_value_int()



        int max_value_int(int[,] map2, int nr, int nc) //Proglacial 
        {
            int loopnr_max = 0;
            int max_val = 0;
            int rast_val = 0;
            int row;
            int col;

            for (row = 0; row < nr; row++)
            {
                for (col = 0; col < nc; col++)
                {
                    if (map2[row, col] != nodata_value)
                    {
                        rast_val = map2[row, col];

                        if (loopnr_max == 0)
                        {
                            max_val = rast_val;
                            loopnr_max++;
                        }
                        else
                        {
                            if (rast_val > max_val)
                            {
                                max_val = rast_val;
                            }
                        }

                    }


                }

            }
            return max_val;

        } // end min_value_int()

    }
}
