using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LORICA4
{
    public partial class Mother_form
    {

        #region calibration code

        private void calib_calculate_maxruns(int calibparacount)
        {
            //this code calculates the total number of runs needed when calibrating
            string calibration_ratio_string = calibration_ratios_textbox.Text;
            string[] ratiowords = calibration_ratio_string.Split(';');
            int ratio;
            for (ratio = 0; ratio < ratiowords.Length; ratio++)
                for (int par = 0; par < calibparacount; par++)
                {
                    try
                    {
                        calib_ratios[par, ratio] = Convert.ToDouble(ratiowords[ratio]);
                    }
                    catch { input_data_error = true; MessageBox.Show("Calibration ratio input error"); }
                }
            try { calib_levels = Convert.ToInt32(calibration_levels_textbox.Text); }
            catch { input_data_error = true; MessageBox.Show("Calibration iterations must be an integer"); }
            maxruns = calib_levels * Convert.ToInt32(Math.Pow(ratiowords.Length, calibparacount));
            Debug.WriteLine(" the number of runs for calibration will be " + maxruns);
        }

        private void calib_shift_and_zoom(int para_number, double zoom_factor, double orig_par_value)
        {
            //this code iinds out whether the best parameter value was on the edge or inside the range explored. Then shifts and zooms out or in , depending
            try
            {
                Debug.WriteLine(" para number " + para_number);
                Debug.WriteLine(" best parameter value " + best_parameters[para_number]);
                Debug.WriteLine(" original parameter value " + orig_par_value);
                double mid_ratio = 0;
                if (calib_ratios.GetLength(1) % 2 == 0) { mid_ratio = (calib_ratios[para_number, Convert.ToInt32(calib_ratios.GetLength(1) / 2) - 1] + calib_ratios[para_number, Convert.ToInt32((calib_ratios.GetLength(1) / 2))]) / 2; }
                else { mid_ratio = calib_ratios[para_number, Convert.ToInt32(calib_ratios.GetLength(1) / 2 - 0.5)]; }
                Debug.WriteLine("mid ratio is " + mid_ratio);
                Double best_ratio = best_parameters[para_number] / orig_par_value;
                Debug.WriteLine("best ratio is " + best_ratio);
                if (best_parameters[para_number] == calib_ratios[para_number, 0] * orig_par_value | best_parameters[para_number] == calib_ratios[para_number, calib_ratios.GetLength(1) - 1] * orig_par_value)
                {
                    //the best parameter ratio (and thus value) was on the edge of the range. We must shift our range sideways (we keep the same ratio between upper and lower ratio - are you still with me?)
                    Debug.WriteLine(" currentpara value was on edge of range");

                    for (int ratio = 0; ratio < calib_ratios.GetLength(1); ratio++)
                    {
                        Debug.WriteLine(" setting ratio " + calib_ratios[para_number, ratio] + " to " + calib_ratios[para_number, ratio] * (best_ratio / mid_ratio));
                        calib_ratios[para_number, ratio] = calib_ratios[para_number, ratio] * (best_ratio / mid_ratio);
                    }
                }
                else
                {
                    //the best parameter ratio (and thus value) NOT on the edge of the range. We must shift to the best observed value and then zoom IN
                    Debug.WriteLine(" currentpara value was NOT on edge of range");
                    for (int ratio = 0; ratio < calib_ratios.GetLength(1); ratio++)
                    {
                        Debug.Write(" setting ratio " + calib_ratios[para_number, ratio] + " to ");
                        calib_ratios[para_number, ratio] = best_ratio + (((calib_ratios[para_number, ratio] / mid_ratio) * best_ratio) - best_ratio) / zoom_factor;
                        Debug.WriteLine(calib_ratios[para_number, ratio]);
                    }
                }
            }
            catch { Debug.WriteLine(" problem adapting parameters and ratios "); }
        }

        private void calib_prepare_report()
        {
            //this code prepares a calibration report
            //it opens and writes headers for a text file on disk
            string FILENAME = workdir + "\\calibration.log";
            using (StreamWriter sw = new StreamWriter(FILENAME))
            {
                try
                {
                    sw.Write("run error");
                    //USER INPUT NEEDED IN FOLLOWING LINE: ENTER THE CALIBRATION PARAMETER NAMES 
                    //THEY WILL BE HEADERS IN THE CALIBRATION REPORT
                    if (version_lux_checkbox.Checked)
                    {
                        sw.WriteLine(" erodibility_K");
                    }
                    if (version_Konza_checkbox.Checked)
                    {
                        sw.WriteLine(" erodibility_K potential_creep_kg P0 k1 k2 Pa");
                    }
                }
                catch { Debug.WriteLine(" issue with writing the header of the calibration log file"); }
            }
            Debug.WriteLine(" calib tst - calib_prepare_rep - added first line to file" + FILENAME);
        }

        private void calib_update_report(double objective_fnct_result)
        {
            //this code updates a calibration report
            //it writes parameters and objective function outcomes to disk
            string FILENAME = workdir + "\\calibration.log";
            using (StreamWriter sw = System.IO.File.AppendText(FILENAME))
            {
                try
                {
                    //USER INPUT NEEDED IN FOLLOWING LINE: ENTER THE CALIBRATION PARAMETERS 

                    if (version_lux_checkbox.Checked)
                    {
                        sw.WriteLine(run_number + " " + objective_fnct_result + " " + advection_erodibility);
                    }
                    if (version_Konza_checkbox.Checked)
                    {
                        sw.WriteLine(run_number + " " + objective_fnct_result + " " + advection_erodibility + " " + potential_creep_kg_m2_y + " " + P0 + " " + k1 + " " + k2 + " " + Pa);
                    }
                }
                catch { Debug.WriteLine(" issue with writing a line in the calibration log file"); }
            }
            Debug.WriteLine(" calib tst - calib_update_rep - added line to file " + FILENAME);
        }

        private void calib_finish_report()
        {
            //this code closes a calibration report
            //it writes the parameters for the best run to disk
            //CALIB_USER : Change the number of parameters referenced (now two)
            Debug.WriteLine(" writing final line and closed file");
            try
            {
                string FILENAME = workdir + "\\calibration.log";
                using (StreamWriter sw = System.IO.File.AppendText(FILENAME))
                {
                    if (version_lux_checkbox.Checked)
                    {
                        //sw.WriteLine(best_run + " " + best_error + " " + best_parameters[0] + " " + best_parameters[1]);
                        sw.WriteLine(best_run + " " + best_error + " " + best_parameters[0]);
                    }
                    if (version_Konza_checkbox.Checked)
                    {
                        sw.WriteLine(best_run + " " + best_error + " " + best_parameters[0] + " " + best_parameters[1] + " " + best_parameters[2] + " " + best_parameters[3] + " " + best_parameters[4]);
                    }
                    Debug.WriteLine(" best run was " + best_run + " with error " + best_error + "m3");
                }
                Debug.WriteLine(" calib tst - calib_finish_rep - wrote final line and closed file");
            }
            catch
            {
                Debug.WriteLine(" calib tst - calib_finish_rep - FAILED to write file");
            }
        }

        private double calib_objective_function_Lux()
        {
            //this code calculates the value of the objective function during calibration and is user-specified. 
            //calibration looks to minimize the value of the objective function by varying parameter values
            //CALIB_USER
            //example for Luxembourg: we want to simulate the correct amount of erosion, over the entire slope
            //Xia, number needs to be adapted
            double simulated_ero_m3 = 0;
            double simulated_ero_kg_m2_y = 0;
            double known_ero_kg_m2_y = 0.0313;
            double total_bulk_density = 0;
            double average_bulk_density_kg_m3 = 0;
            int objective_function_cells = 0;
            for (row = 0; row < nr; row++)
            {
                for (col = 0; col < nc; col++)
                {
                    if (dtm[row, col] != nodata_value)
                    {
                        simulated_ero_m3 -= sum_water_erosion[row, col] * dx * dx;
                        total_bulk_density += bulkdensity[row, col, 0];
                        objective_function_cells++;
                    }
                }
            }
            average_bulk_density_kg_m3 = total_bulk_density / objective_function_cells;
            //temporary hard fix to test if bulkdensities of 0 are throwing off our calculations 
            average_bulk_density_kg_m3 = 1560;
            simulated_ero_kg_m2_y = (average_bulk_density_kg_m3 * simulated_ero_m3) / (objective_function_cells * dx * dx) / end_time;
            ;
            Debug.WriteLine(" calib tst - calib_objective_function - error is " + Math.Abs(known_ero_kg_m2_y - simulated_ero_kg_m2_y) + "kg per m2 per year");
            return Math.Abs(known_ero_kg_m2_y - simulated_ero_kg_m2_y);

        }

        private double domain_sum(string properties)
        {
            //Debug.WriteLine(properties);
            string[] lineArray = properties.Split(new char[] { ',' });
            int lyr; int x;
            double sum = 0;
            for (row = 0; row < nr; row++)
            {
                for (col = 0; col < nc; col++)
                {
                    for (lyr = 0; lyr < max_soil_layers; lyr++)
                    {
                        if (layerthickness_m[row, col, lyr] > 0)
                        {
                            for (x = 0; x < (lineArray.Length); x++)
                            {
                                if (lineArray[x] == "0") sum += texture_kg[row, col, lyr, 0];
                                if (lineArray[x] == "1") sum += texture_kg[row, col, lyr, 1];
                                if (lineArray[x] == "2") sum += texture_kg[row, col, lyr, 2];
                                if (lineArray[x] == "3") sum += texture_kg[row, col, lyr, 3];
                                if (lineArray[x] == "4") sum += texture_kg[row, col, lyr, 4];
                                if (lineArray[x] == "5") sum += young_SOM_kg[row, col, lyr];
                                if (lineArray[x] == "6") sum += old_SOM_kg[row, col, lyr];
                            }
                        }
                    }
                }
            }
            return sum;
        }

        private double calib_objective_function_Konza()
        {
            observations = new double[100, 100];         //may be used for other sets of observations as well
            string input;
            double tttt = 0.00;
            int x, y, xcounter;
            string localfile = "localcalresults.txt";
            string globalfile = "globalcalresults.txt";
            //string obsfile = "obst0b.txt";
            string obsfile = obsfile_textbox.Text;
            if (!System.IO.File.Exists(obsfile))
            {
                MessageBox.Show("No such file: " + obsfile);
                return (-1);
            }
            StreamReader sr = System.IO.File.OpenText(obsfile);
            //read header line (and do nothing with it)
            try
            {
                input = sr.ReadLine();
                //read the rest
                y = 0;
                while ((input = sr.ReadLine()) != null)
                {
                    //Debug.WriteLine(input);
                    string[] lineArray;
                    lineArray = input.Split(new char[] { ',' });
                    xcounter = 0;
                    for (x = 0; x < (lineArray.Length); x++)
                    {
                        //Debug.WriteLine(lineArray[x]);
                        if (lineArray[x] != "")
                        {
                            try
                            {
                                tttt = double.Parse(lineArray[x]);
                            }
                            catch
                            {
                                MessageBox.Show("Incorrect content " + lineArray[x] + " in file " + obsfile);
                                input_data_error = true;
                                return (-1);
                            }
                            //Debug.WriteLine("obs " + y + " " + xcounter + " = " + tttt);
                            observations[y, xcounter] = tttt;
                            xcounter++;
                        }
                    }
                    y++;

                }
                //Array.Resize(observations)
                sr.Close();
                //we now have the observations stored, and the output file prepared
            }
            catch { Debug.WriteLine(" failed to read all observations from file " + obsfile); input_data_error = true; }


            //this function evaluates model performance after every model run
            //we have observations[,] at our disposal, which contains values at all observed locations 

            Debug.WriteLine(" starting to evaluate performance using " + localfile);
            localfile = workdir + "\\" + localfile;
            globalfile = workdir + "\\" + globalfile;
            bool location_errors_requested = false;
            if (location_errors_requested)
            {
                if (!System.IO.File.Exists(localfile))
                {
                    MessageBox.Show("No such observation file: " + localfile);
                    return (-1);
                }
            }
            /* if (!File.Exists(globalfile))
            {
                MessageBox.Show("No such observation file: " + globalfile);
                return (-1);
            } */
            //run, soildepth_e, SOMfract_e, coarsefract_e, clayfract_e, siltfract_e, sandfract_e, average_e
            int obsnumber = 0; int row = 0; int col = 0;
            double localdepth_error, SOM_error, coarse_error, clay_error, silt_error, sand_error;
            double totaldepth_error = 0;
            double SOM_error_depthproduct, coarse_error_depthproduct, sand_error_depthproduct, silt_error_depthproduct, clay_error_depthproduct;
            double all_locations_error = 0;
            double coarsesum, sandsum, siltsum, claysum, OMsum, coarsefract = 0, sandfract = 0, siltfract = 0, clayfract = 0, OMfract = 0, fineearthsum, allmasssum;
            double normal_OM_error, normal_coarse_error, normal_sand_error, normal_silt_error, normal_clay_error, location_error;
            try
            {
                for (row = 0; row < nr; row++)
                {
                    for (col = 0; col < nc; col++)
                    {
                        //Debug.WriteLine("rc " + row + " " + col + " obsnumber " + obsnumber + " next obsrow= " + observations[obsnumber, 0] + " col " + observations[obsnumber, 1]);
                        if (row == observations[obsnumber, 0] && col == observations[obsnumber, 1])
                        {
                            //in this case, we are in a cell where we also have an observation. Time to compare and calculate an error, first for the soil depth
                            localdepth_error = Math.Pow(Math.Abs(soildepth_m[row, 0] - observations[obsnumber, 2]) / (observations[obsnumber, 2] + soildepth_m[row, 0]), 2);
                            //and now for all other properties, where we need to take a complicated average over all layers:
                            SOM_error_depthproduct = 0; coarse_error_depthproduct = 0; sand_error_depthproduct = 0; silt_error_depthproduct = 0; clay_error_depthproduct = 0;
                            int obshorizon = 0;
                            int lyr = 0;
                            double lyr_end_depth_m = 0;
                            double lyr_begin_depth_m = 0;
                            double hor_end_depth_m = 0;
                            double hor_begin_depth_m = 0;
                            double overlap_m = 0;
                            bool horizonchanged = false;
                            //now calulate errors
                            while (layerthickness_m[row, col, lyr] > 0) // as long as we still have layers left
                            {

                                lyr_end_depth_m += layerthickness_m[row, col, lyr];  //we update how deep this layer ends
                                lyr_begin_depth_m = lyr_end_depth_m - layerthickness_m[row, col, lyr];
                                horizonchanged = false;
                                try
                                {
                                    while (observations[obsnumber, 3 + 6 * obshorizon] > 0 && !(lyr_begin_depth_m > observations[obsnumber, 3 + 6 * obshorizon]))
                                    {
                                        horizonchanged = false;
                                        //Debug.WriteLine(" now depth " + lyr_end_depth_m + " obshorizon " + obshorizon + " compared to obs depth " + observations[obsnumber, (3 + 6 * obshorizon)]);
                                        if (lyr_end_depth_m > observations[obsnumber, (3 + 6 * obshorizon)])
                                        {
                                            overlap_m = layerthickness_m[row, col, lyr] + (observations[obsnumber, (3 + 6 * obshorizon)] - lyr_end_depth_m);
                                        }
                                        else
                                        {
                                            overlap_m = layerthickness_m[row, col, lyr];
                                        }
                                        //Debug.WriteLine(" overlap depth = " + overlap_m);
                                        SOM_error = Math.Abs(((young_SOM_kg[row, col, lyr] + old_SOM_kg[row, col, lyr]) / total_layer_fine_earth_om_mass_kg(row, col, lyr)) - observations[obsnumber, 4 + 6 * obshorizon]);
                                        coarse_error = Math.Abs((texture_kg[row, col, lyr, 0] / total_layer_mineral_earth_mass_kg(row, col, lyr)) - observations[obsnumber, 5 + 6 * obshorizon]);
                                        sand_error = Math.Abs((texture_kg[row, col, lyr, 1] / total_layer_fine_earth_mass_kg(row, col, lyr)) - observations[obsnumber, 6 + 6 * obshorizon]);
                                        silt_error = Math.Abs((texture_kg[row, col, lyr, 2] / total_layer_fine_earth_mass_kg(row, col, lyr)) - observations[obsnumber, 7 + 6 * obshorizon]);
                                        clay_error = Math.Abs(((texture_kg[row, col, lyr, 3] + texture_kg[row, col, lyr, 4]) / total_layer_fine_earth_mass_kg(row, col, lyr)) - observations[obsnumber, 8 + 6 * obshorizon]);
                                        //now normalize and add the errors to the error depth products, but account for the fact that only a fraction of this layer was decsribed in this observed horizon
                                        SOM_error_depthproduct += SOM_error * overlap_m;
                                        coarse_error_depthproduct += coarse_error * overlap_m;
                                        sand_error_depthproduct += sand_error * overlap_m;
                                        silt_error_depthproduct += silt_error * overlap_m;
                                        clay_error_depthproduct += clay_error * overlap_m;

                                        if (lyr_end_depth_m > observations[obsnumber, (3 + 6 * obshorizon)])
                                        {
                                            obshorizon++; horizonchanged = true;
                                            //Debug.WriteLine(" moved to next obshorizon : " + obshorizon);
                                            //Debug.WriteLine(" now depth " + lyr_end_depth_m + " obshorizon " + obshorizon + " compared to obs depth " + observations[obsnumber, (3 + 6 * obshorizon)]);
                                        }
                                        else
                                        {
                                            //Debug.WriteLine(" breaking out, going for next layer "); 
                                            break;
                                        }
                                        //alternative ways to calculate all these errors:
                                        //clay_error_depthproduct += clay_error / observations[obsnumber, 8 + 6 * obshorizon] * overlap_m;  / has the disadvantage that if observations[,] = 0, the error is INF
                                        //clay_error_depthproduct += clay_error / ((((texture_kg[row, col, lyr, 3] + texture_kg[row, col, lyr, 4]) / total_layer_fine_earth_mass_kg(row, col, lyr))/2) 
                                        //the one above divides by the mean of obs and sim, which may still be zero, but then the error was also zero. Not sure what that results in, but possibly still unstable
                                        //Debug.WriteLine(" obshorizon now " + obshorizon + " with depth " + observations[obsnumber, 3 + 6 * obshorizon] + " lyr " + lyr + " begin " + lyr_begin_depth_m + " end " + lyr_end_depth_m);
                                    }
                                }
                                catch { Debug.WriteLine("error - failed during calculation of local errors "); }
                                //Debug.WriteLine(" no more horizons eligible or available after hor " + obshorizon + " lyr now " + lyr + " end depth " + lyr_end_depth_m);
                                if (horizonchanged == false)
                                {
                                    lyr++;
                                    //Debug.WriteLine(" increased layer to " + lyr); 
                                }
                                if (lyr == max_soil_layers) { break; }

                            }
                            //Debug.WriteLine("rc" + row + col + " no more layers left after lyr " + lyr);
                            //we now calculated, normalized and depth_summed all errors. Dividing by depth and adding up is the next step
                            normal_OM_error = SOM_error_depthproduct / lyr_end_depth_m;
                            normal_coarse_error = coarse_error_depthproduct / lyr_end_depth_m;
                            normal_sand_error = sand_error_depthproduct / lyr_end_depth_m;
                            normal_silt_error = silt_error_depthproduct / lyr_end_depth_m;
                            normal_clay_error = clay_error_depthproduct / lyr_end_depth_m;
                            //if there was no  simulated layer at all, we just divided by zero up here, and should replace the NaN with a large value
                            int largereplacementerror = 100;
                            if (Double.IsNaN(localdepth_error)) { localdepth_error = largereplacementerror; }
                            if (Double.IsNaN(normal_OM_error)) { normal_OM_error = largereplacementerror; }
                            if (Double.IsNaN(normal_coarse_error)) { normal_coarse_error = largereplacementerror; }
                            if (Double.IsNaN(normal_sand_error)) { normal_sand_error = largereplacementerror; }
                            if (Double.IsNaN(normal_silt_error)) { normal_silt_error = largereplacementerror; }
                            if (Double.IsNaN(normal_clay_error)) { normal_clay_error = largereplacementerror; }
                            location_error = (localdepth_error + normal_OM_error + normal_coarse_error + normal_sand_error + normal_silt_error + normal_clay_error) / 6;
                            all_locations_error += location_error;
                            Debug.WriteLine(row + " " + col + " " + location_error + "  " + all_locations_error + " " + localdepth_error);
                            totaldepth_error += localdepth_error;

                            double NBW = 0;
                            if (bedrock_weathering_active)
                            {
                                if (bedrock_weathering_m[row, col] < 0) //MMS to prevent negative bedrock weathering production
                                {
                                    NBW = 1;
                                }
                                else
                                {
                                    NBW = 0;
                                }
                            }

                            //write this to file for this location

                            if (location_errors_requested)
                            {
                                using (StreamWriter sw = new StreamWriter(localfile, true))
                                {
                                    //sw.Write(run + "," + row + "," + col + "," + location_error + "," + totaldepth_error + "," + normal_OM_error + "," + normal_coarse_error + "," + normal_sand_error + "," + normal_silt_error + "," + normal_clay_error); //MMS_eva
                                    sw.Write(run_number + "," + row + "," + col + "," + location_error + "," + localdepth_error + "," + normal_OM_error + "," + normal_coarse_error + "," + normal_sand_error + "," + normal_silt_error + "," + normal_clay_error + "," + advection_erodibility + "," + potential_creep_kg_m2_y + "," + P0 + "," + k1 + "," + k2 + "," + NBW); //MMS_eva
                                    sw.Write("\r\n");
                                    sw.Close();
                                }
                            }
                            obsnumber++;
                            //Debug.WriteLine("increased obsnumber to " + obsnumber);
                        }
                        else
                        { //do nothing , this is a cell where we have no observations so it gets ignored
                        }
                    }

                }
            }
            catch { Debug.WriteLine("error - failed during calculation of initial errors "); }
            //we now know the sum of normalized errors for all locations. Divide by number of observations (=locations) to compare with errors in the depth-averages for the entire area
            all_locations_error /= obsnumber;
            try
            {
                //the average values of the SOM and texture fractions across ALL rows and cols are calculated in a separate function domain_sum, which takes a peculiar input:
                //a string of all properties separated by comma's. 
                coarsesum = domain_sum($"{0}");
                sandsum = domain_sum($"{1}");
                siltsum = domain_sum($"{2}");
                claysum = domain_sum("3,4");
                OMsum = domain_sum("5,6");
                fineearthsum = domain_sum("1,2,3,4,5,6");
                allmasssum = domain_sum("0,1,2,3,4,5,6");
                coarsefract = coarsesum / allmasssum;
                sandfract = sandsum / fineearthsum;
                siltfract = siltsum / fineearthsum;
                clayfract = claysum / fineearthsum;
                OMfract = OMsum / fineearthsum;
            }
            catch { Debug.WriteLine("  failed during calculation of simulated domain sums"); }
            //the average values of the observations are calculated here
            double OM_obs_depth_sum = 0, coarse_obs_depth_sum = 0, sand_obs_depth_sum = 0, silt_obs_depth_sum = 0, clay_obs_depth_sum = 0, depth_sum = 0;
            double OM_obs_fract = 0, coarse_obs_fract = 0, sand_obs_fract = 0, silt_obs_fract = 0, clay_obs_fract = 0;
            try
            {
                for (i = 0; i < observations.GetLength(0); i++)
                {
                    for (j = 1; j < (observations.GetLength(1) - 3) / 6; j++)
                    {
                        if (observations[i, (j - 1) * 6 + 3] > 0)
                        {   // in other words, if this horizon exists.
                            //Debug.WriteLine(" adding horizon " + j + " from location " + i);
                            OM_obs_depth_sum += observations[i, (j - 1) * 6 + 4] * observations[i, (j - 1) * 6 + 3];
                            coarse_obs_depth_sum += observations[i, (j - 1) * 6 + 5] * observations[i, (j - 1) * 6 + 3];
                            sand_obs_depth_sum += observations[i, (j - 1) * 6 + 6] * observations[i, (j - 1) * 6 + 3];
                            silt_obs_depth_sum += observations[i, (j - 1) * 6 + 7] * observations[i, (j - 1) * 6 + 3];
                            clay_obs_depth_sum += observations[i, (j - 1) * 6 + 8] * observations[i, (j - 1) * 6 + 3];
                            depth_sum += observations[i, (j - 1) * 6 + 3];
                        }
                    }
                }
                Debug.WriteLine(" finished adding observations ");
            }
            catch { Debug.WriteLine(" error - failed during calculation of observed domain sums"); }
            OM_obs_fract = OM_obs_depth_sum / depth_sum;
            coarse_obs_fract = coarse_obs_depth_sum / depth_sum;
            sand_obs_fract = sand_obs_depth_sum / depth_sum;
            silt_obs_fract = silt_obs_depth_sum / depth_sum;
            clay_obs_fract = clay_obs_depth_sum / depth_sum;
            normal_OM_error = Math.Abs(OMfract - OM_obs_fract) / OM_obs_fract;
            normal_coarse_error = Math.Abs(coarsefract - coarse_obs_fract) / coarse_obs_fract;
            normal_sand_error = Math.Abs(sandfract - sand_obs_fract) / sand_obs_fract;
            normal_silt_error = Math.Abs(siltfract - silt_obs_fract) / silt_obs_fract;
            normal_clay_error = Math.Abs(clayfract - clay_obs_fract) / clay_obs_fract;
            double entire_domain_error = (normal_OM_error + normal_coarse_error + normal_sand_error + normal_silt_error + normal_clay_error) / 5;
            /*using (StreamWriter sw = new StreamWriter(globalfile, true))
            {
                sw.Write(run_number + "," + all_locations_error + "," + entire_domain_error + "," + (all_locations_error + entire_domain_error) / 2 + "," + totaldepth_error / observations.GetLength(0) + "," + advection_erodibility + "," + potential_creep_kg + "," + P0 + "," + k1 + "," + k2);
                sw.Write("\r\n");
                sw.Close();
            }
            Debug.WriteLine("calculated and saved errors for run number" + run_number);
            */
            return ((all_locations_error + entire_domain_error) / 2);
        }

        private void calib_update_best_paras()
        {
            //this code updates the recorded set of parameter values that gives the best score for the objective function
            //USERS have to update code here to reflect the parameters they actually vary

            // add/change lines below
            if (version_lux_checkbox.Checked)
            {
                best_parameters[0] = advection_erodibility;
            }
            if (version_Konza_checkbox.Checked)
            {
                //Konza Marte:
                best_parameters[0] = advection_erodibility;
                best_parameters[1] = potential_creep_kg_m2_y;
                best_parameters[2] = P0;
                best_parameters[3] = k1;
                best_parameters[4] = k2;
            }
            Debug.WriteLine(" updated parameter set for best scored run");
        }

        #endregion

    }
}
