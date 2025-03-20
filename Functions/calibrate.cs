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


        private double calib_objective_function_CarboZALF()
        {
            //this code calculates the value of the objective function during calibration and is user-specified. 
            //calibration looks to minimize the value of the objective function by varying parameter values
            //CALIB_USER
            //example for Luxembourg: we want to simulate the correct amount of erosion, over the entire slope
            //Xia, number needs to be adapted
            bool calib_erodep = false;
            bool calib_OSL = true;
            bool calib_CN = false;
            bool calib_stabages = false;
            if (CarboZALF_calib_stabilizationages_checkbox.Checked) { calib_stabages = true; }
            double sim_ero_m = 0, obs_ero_m, sim_depo_m = 0, obs_depo_m, error_CZ = 0;
            int obj_fun_cells_ero = 0, obj_fun_cells_depo = 0;

            if (calib_erodep)
            {
                // calibration values derived from Van der Meij et al., 2017, approach 2c
                obs_ero_m = -0.30; // average for all eroding positions
                obs_depo_m = 0.51; // average  colluvium thickness
                for (row = 0; row < nr; row++)
                {
                    for (col = 0; col < nc; col++)
                    {
                        if (dtm[row, col] != -9999 & dtmchange_m[row, col] < 0)
                        {
                            sim_ero_m += dtmchange_m[row, col];
                            obj_fun_cells_ero++;
                        }
                        if (dtm[row, col] != -9999 & dtmchange_m[row, col] > 0)
                        {
                            sim_depo_m += dtmchange_m[row, col];
                            obj_fun_cells_depo++;
                        }
                    }
                }
                sim_ero_m /= obj_fun_cells_ero; // average elevation change in eroding locations
                sim_depo_m /= obj_fun_cells_depo; // average elevation change in deposition locations
                error_CZ = Math.Abs(((obs_ero_m - sim_ero_m) + (obs_depo_m - sim_depo_m)) / 2); // average error of erosion and deposition
            }
            if (calib_OSL)
            {
                double cum_age_error = 0;

                // Calculate reference depths from the former soil surface, not from the top of the colluvium, to prevent colluvial samples ending up in the former soil.
                // The selected methoid can result in measured sample located above the simulated colluvium. These will get a large penalty in the calibration 
                // This approach is better suited for deposition rates, because colluvium builds up from the bottom to the top

                if (calib_stabages) // calibration with mode of stabilization ages
                {
                    // calibration old colluvium ( > 300 a)
                    // Location P2
                    cum_age_error += calib_function_CarboZALF_OSL(14, 17, 0.025, 3700); // NCL7317038
                    cum_age_error += calib_function_CarboZALF_OSL(14, 17, 0.125, 3301); // NCL7317039
                    cum_age_error += calib_function_CarboZALF_OSL(14, 17, 0.225, 2227); // NCL7317040

                    // Location P3
                    cum_age_error += calib_function_CarboZALF_OSL(27, 17, 0.055, 1898); // NCL7317069
                    cum_age_error += calib_function_CarboZALF_OSL(27, 17, 0.155, 1731); // NCL7317145
                    cum_age_error += calib_function_CarboZALF_OSL(27, 17, 0.255, 1048); // NCL7317146


                    // calibration young colluvium (<= 300 a)
                    // Location P2
                    cum_age_error += calib_function_CarboZALF_OSL(14, 17, 0.325, 120); // NCL7317041
                    cum_age_error += calib_function_CarboZALF_OSL(14, 17, 0.425, 27); // NCL7317042
                    cum_age_error += calib_function_CarboZALF_OSL(14, 17, 0.605, 18); // NCL7317068

                    // Location P3
                    cum_age_error += calib_function_CarboZALF_OSL(27, 17, 0.355, 44); // NCL7317147
                    cum_age_error += calib_function_CarboZALF_OSL(27, 17, 0.455, 40); // NCL7317070

                    // Location BP5
                    cum_age_error += calib_function_CarboZALF_OSL(14, 23, 0.075, 159); // NCL7317062
                    cum_age_error += calib_function_CarboZALF_OSL(14, 23, 0.175, 135); // NCL7317063
                    cum_age_error += calib_function_CarboZALF_OSL(14, 23, 0.275, 99); // NCL7317064
                    cum_age_error += calib_function_CarboZALF_OSL(14, 23, 0.375, 87); // NCL7317065
                    cum_age_error += calib_function_CarboZALF_OSL(14, 23, 0.475, 73); // NCL7317066
                    cum_age_error += calib_function_CarboZALF_OSL(14, 23, 0.575, 37); // NCL7317067

                    // Location BP6
                    cum_age_error += calib_function_CarboZALF_OSL(17, 20, 0.045, 160); // NCL7317152
                    cum_age_error += calib_function_CarboZALF_OSL(17, 20, 0.175, 136); // NCL7317153
                    cum_age_error += calib_function_CarboZALF_OSL(17, 20, 0.305, 121); // NCL7317154
                    cum_age_error += calib_function_CarboZALF_OSL(17, 20, 0.425, 103); // NCL7317155
                    cum_age_error += calib_function_CarboZALF_OSL(17, 20, 0.545, 92); // NCL7317156

                    // Location BP8
                    cum_age_error += calib_function_CarboZALF_OSL(18, 22, 0.045, 258); // NCL7317142
                    cum_age_error += calib_function_CarboZALF_OSL(18, 22, 0.145, 227); // NCL7317148
                    cum_age_error += calib_function_CarboZALF_OSL(18, 22, 0.245, 197); // NCL7317143
                    cum_age_error += calib_function_CarboZALF_OSL(18, 22, 0.345, 139); // NCL7317149
                    cum_age_error += calib_function_CarboZALF_OSL(18, 22, 0.445, 117); // NCL7317150

                }
                else // calibration with mode of measured ages
                {
                    // calibration old colluvium ( > 300 a)
                    // Location P2
                    cum_age_error += calib_function_CarboZALF_OSL(14, 17, 0.025, 3832); // NCL7317038
                    cum_age_error += calib_function_CarboZALF_OSL(14, 17, 0.125, 3606); // NCL7317039
                    cum_age_error += calib_function_CarboZALF_OSL(14, 17, 0.225, 3895); // NCL7317040
                    cum_age_error += calib_function_CarboZALF_OSL(14, 17, 0.325, 2170); // NCL7317041

                    // Location P3
                    cum_age_error += calib_function_CarboZALF_OSL(27, 17, 0.055, 3265); // NCL7317069
                    cum_age_error += calib_function_CarboZALF_OSL(27, 17, 0.155, 2650); // NCL7317145
                    cum_age_error += calib_function_CarboZALF_OSL(27, 17, 0.255, 1425); // NCL7317146


                    // calibration young colluvium (<= 300 a)
                    // Location P2
                    cum_age_error += calib_function_CarboZALF_OSL(14, 17, 0.425, 66); // NCL7317042
                    cum_age_error += calib_function_CarboZALF_OSL(14, 17, 0.605, 46); // NCL7317068

                    // Location P3
                    cum_age_error += calib_function_CarboZALF_OSL(27, 17, 0.355, 41); // NCL7317147
                    cum_age_error += calib_function_CarboZALF_OSL(27, 17, 0.455, 44); // NCL7317070

                    // Location BP5
                    cum_age_error += calib_function_CarboZALF_OSL(14, 23, 0.075, 170); // NCL7317062
                    cum_age_error += calib_function_CarboZALF_OSL(14, 23, 0.175, 127); // NCL7317063
                    cum_age_error += calib_function_CarboZALF_OSL(14, 23, 0.275, 87); // NCL7317064
                    cum_age_error += calib_function_CarboZALF_OSL(14, 23, 0.375, 89); // NCL7317065
                    cum_age_error += calib_function_CarboZALF_OSL(14, 23, 0.475, 75); // NCL7317066
                    cum_age_error += calib_function_CarboZALF_OSL(14, 23, 0.575, 58); // NCL7317067

                    // Location BP6
                    cum_age_error += calib_function_CarboZALF_OSL(17, 20, 0.045, 201); // NCL7317152
                    cum_age_error += calib_function_CarboZALF_OSL(17, 20, 0.175, 137); // NCL7317153
                    cum_age_error += calib_function_CarboZALF_OSL(17, 20, 0.305, 136); // NCL7317154
                    cum_age_error += calib_function_CarboZALF_OSL(17, 20, 0.425, 107); // NCL7317155
                    cum_age_error += calib_function_CarboZALF_OSL(17, 20, 0.545, 76); // NCL7317156

                    // Location BP8
                    cum_age_error += calib_function_CarboZALF_OSL(18, 22, 0.045, 458); // NCL7317142
                    cum_age_error += calib_function_CarboZALF_OSL(18, 22, 0.145, 282); // NCL7317148
                    cum_age_error += calib_function_CarboZALF_OSL(18, 22, 0.245, 232); // NCL7317143
                    cum_age_error += calib_function_CarboZALF_OSL(18, 22, 0.345, 146); // NCL7317149
                    cum_age_error += calib_function_CarboZALF_OSL(18, 22, 0.445, 126); // NCL7317150 
                }
                error_CZ = cum_age_error;
            }
            Debug.WriteLine(" calib tst - calib_objective_function - error is " + error_CZ + " a in total");
            return Math.Abs(error_CZ);
        }

        private double calib_function_CarboZALF_OSL(int row_cal, int col_cal, double sample_depth_from_fAh, double age_ref)
        {
            double depth_z, refdepth, penalty, age_model;
            int lay_cal;

            refdepth = dtm[row_cal, col_cal] - dtmchange_m[row_cal, col_cal] + sample_depth_from_fAh;
            depth_z = dtm[row_cal, col_cal];
            lay_cal = -1;
            age_model = end_time * 2; // dummy age for when colluvium is simulated too thin. 1*end_time not suitable, because it can be closer to the older ages than younger modelled ages
            //dummy_penalty = age_ref / (end_time - age_ref + 1); // Penalty for samples where colluvium is simulated too thin
            //                                                     // Calculate penalty based on measured age relative to simulation time.
            //                                                     // The older, the higher the penalty. Penalties are generally higher than the ones calculated below.
            //                                                     // Penalty never below 1, made sure by adding 1 to the reference age

            // Select the layer that corresponds to the measured sample depth. 
            if (refdepth < depth_z) // if measured sample is  present in the simulated colluvium
            {
                lay_cal = 0;
                //while (!(refdepth < depth_z & (refdepth >= (z - layerthickness_m[row_cal, col_cal, lay_cal])))) // determine the layer in which the sample should be located
                while (refdepth < depth_z) // while the depth of the sample is located above the upper depth of the layer, move to the next layer
                {
                    lay_cal++;
                    depth_z -= layerthickness_m[row_cal, col_cal, lay_cal];
                }
                try                 // calculate simulated age mode
                {
                    int[] ages_cal = OSL_grainages[row_cal, col_cal, lay_cal];
                    ages_cal = ages_cal.Where(e => e < start_age).ToArray(); // remove old grains from the selection, Focus on the younger grains. younger age is never larger than start_age, so it's a good reference point
                    double[] ages_cal_d = ages_cal.Select(x => (double)x).ToArray();

                    if (ages_cal_d.Length > 1)
                    {
                        double[,] density = KernelDensityEstimation(ages_cal_d);

                        double[] density_prob = new double[density.GetLength(0)];
                        double[] density_value = new double[density.GetLength(0)]; ;

                        for (int it = 0; it < density.GetLength(0); it++)
                        {
                            density_value[it] = density[it, 0];
                            density_prob[it] = density[it, 1];
                        }
                        int[] indices_prob = new int[density_value.Length];
                        for (int ii = 0; ii < density_value.Length; ii++) { indices_prob[ii] = ii; }
                        Array.Sort(density_prob, indices_prob);

                        age_model = density_value[indices_prob[indices_prob.Length - 1]];
                    }
                    else
                    {
                        if (ages_cal_d.Length == 1)
                        { // only one rejuvenated grain. Use that age as reference
                            age_model = ages_cal_d[0];
                        }
                        else
                        {
                            // no grains present for this sample. This means the sample is too close to the surface (layer = 0), or there are no young grains (poor bleaching, non-eroded layer). So penalty is based on the dummy age
                        }
                    }
                }

                catch
                {
                    Debug.WriteLine("Error in calculating age densities");
                }
            }
            else // sample is located outside of simulated colluvium, use runtime as age estimate for a high penalty
            {

            }
            // penalty = Math.Pow((age_model - age_ref),2); // calculate squared error
            penalty = Math.Abs((age_model - age_ref)); // calculate absolute error

            return (penalty);
        }


        private double calib_objective_function_bioturbation()
        {
            double cum_age_error = 0;

            // Termites
            cum_age_error += calib_function_bioturbation_OSL(0.02, 16);
            cum_age_error += calib_function_bioturbation_OSL(0.17, 85);
            cum_age_error += calib_function_bioturbation_OSL(0.27, 546);
            cum_age_error += calib_function_bioturbation_OSL(0.39, 1135);
            cum_age_error += calib_function_bioturbation_OSL(0.49, 1218);
            cum_age_error += calib_function_bioturbation_OSL(0.59, 1504);
            cum_age_error += calib_function_bioturbation_OSL(0.73, 2055);
            cum_age_error += calib_function_bioturbation_OSL(0.83, 2899);
            cum_age_error += calib_function_bioturbation_OSL(0.92, 2525);
            cum_age_error += calib_function_bioturbation_OSL(1.02, 3237);


            // Develop for other processes and datasets

            Debug.WriteLine(" calib tst - calib_objective_function - error is " + cum_age_error + " a in total");
            return cum_age_error;
        }

        private double calib_function_bioturbation_OSL(double depth, double age_ref)
        {
            int row_cal = 0, col_cal = 0;
            // Calculate rerpesentative depth for layer
            int lay_cal = 0;
            double depth_ref = 0;
            double depth_dist = 999;
            double penalty = -1;

            bool stop_it = false;
            while (!stop_it)
            {
                depth_ref += layerthickness_m[row_cal, col_cal, lay_cal] / 2;
                if (Math.Abs(depth - depth_ref) < depth_dist) // if distance between layers is decreasing
                {
                    depth_dist = Math.Abs(depth - depth_ref);
                    lay_cal++;
                }
                else // if distance between layers in increasing again
                {
                    stop_it = true;
                }
                depth_ref += layerthickness_m[row_cal, col_cal, lay_cal] / 2;
            }

            // calculate age penalty
            try
            {
                int iii = 0;
                int[] ages_cal = OSL_grainages[row_cal, col_cal, lay_cal];
                ages_cal = ages_cal.Where(e => e < end_time).ToArray(); // Remove grains that equal run time, focus on the younger ages
                double[] ages_cal_d = ages_cal.Select(x => (double)x).ToArray();
                //for (iii = 0; iii < ages_cal_d.Length; iii++)
                //{
                //    Debug.Write(ages_cal_d[iii] + ",");
                //}
                //Debug.WriteLine("");
                if (ages_cal_d.Length > 1)
                {
                    double[,] density = KernelDensityEstimation(ages_cal_d);

                    double[] density_prob = new double[density.GetLength(0)];
                    double[] density_value = new double[density.GetLength(0)]; ;

                    for (int it = 0; it < density.GetLength(0); it++)
                    {
                        density_value[it] = density[it, 0];
                        density_prob[it] = density[it, 1];
                    }
                    int[] indices_prob = new int[density_value.Length];
                    for (int ii = 0; ii < density_value.Length; ii++) { indices_prob[ii] = ii; }
                    Array.Sort(density_prob, indices_prob);

                    double ages_mode = density_value[indices_prob[indices_prob.Length - 1]];

                    penalty = Math.Abs(ages_mode - age_ref) / age_ref;
                }
                else
                {
                    if (ages_cal_d.Length == 1)
                    { // only one rejuvenated grain. Use that age as reference
                        penalty = Math.Abs(ages_cal_d[0] - age_ref) / age_ref;
                    }
                    else
                    {
                        // no grains present for this sample. This means the sample is too close to the surface (layer = 0), or there are no young grains (poor bleaching, non-eroded layer). So penalty is based on measured age
                        // Debugger.Break();
                        penalty = end_time / (end_time - age_ref + 1);
                    }
                }
            }
            catch
            {
                Debug.WriteLine("Error in calculating age densities");
                penalty = 999;
            }
            return (penalty);

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

        void lessivage_calibration(int row, int col, int cal)
        {
            //Debug.Write(Cclay + " " + max_eluviation + " " + ct_depthdec + " ");
            //double[] lp4 = new double[,]{0.09, 0.09,0.09, 0.10, 0.16, 0.16, 0.19, 0.19, 0.19, 0.16, 0.16, 0.16, 0.16, 0.13, 0.13, 0.13, 0.13, 0.13, 0.13, 0.13};

            double[,] lp4 = new double[6, 2] { { .31, .09 }, { .45, .10 }, { .62, .16 }, { .90, .19 }, { 1.35, .16 }, { 2.0, .13 } };
            double depth, err, rmse_ct, me_ct, lp4_clay;
            int lp4_row = 0;
            int layercount = 0;
            rmse_ct = 0;
            me_ct = 0;
            depth = 0;
            Debug.WriteLine(rmse_ct + ", " + me_ct);
            for (int layer = 0; layer < max_soil_layers; layer++)
            {
                if (layerthickness_m[row, col, layer] > 0)
                {
                    depth += layerthickness_m[row, col, layer] / 2;
                    if (depth <= lp4[5, 0])
                    {
                        double totalweight = texture_kg[row, col, layer, 1] + texture_kg[row, col, layer, 2] + texture_kg[row, col, layer, 3] + texture_kg[row, col, layer, 4]; // calibrate on fine soil fraction only
                        while (depth > lp4[lp4_row, 0])
                        {
                            lp4_row++;
                        }
                        lp4_clay = lp4[lp4_row, 1];

                        err = ((texture_kg[row, col, layer, 3] + texture_kg[row, col, layer, 4]) / totalweight) - lp4_clay;
                        rmse_ct += err * err;
                        me_ct += err;
                        layercount += 1;
                    }

                    depth += layerthickness_m[row, col, layer] / 2;
                }

            }
            Debug.WriteLine(layercount);
            rmse_ct = Math.Pow(rmse_ct / layercount, .5);
            me_ct = me_ct / layercount;
            //Debug.Write(rmse_ct + " " + me_ct);
            //Debug.WriteLine("");//start on new line
            lessivage_errors[cal, 0] = Cclay;
            lessivage_errors[cal, 1] = max_eluviation;
            lessivage_errors[cal, 2] = ct_depthdec;
            lessivage_errors[cal, 3] = rmse_ct;
            lessivage_errors[cal, 4] = me_ct;
        }

        public static double[,] KernelDensityEstimation(double[] data)
        {
            // from: https://gist.github.com/ksandric/e91860143f1dd378645c01d518ddf013 

            // probability density function (PDF) signal analysis
            // Works like ksdensity in mathlab. 
            // KDE performs kernel density estimation (KDE)on one - dimensional data
            // http://en.wikipedia.org/wiki/Kernel_density_estimation

            // Input:	-data: input data, one-dimensional
            //          -sigma: bandwidth(sometimes called "h")
            //          -nsteps: optional number of abscis points.If nsteps is an
            //          array, the abscis points will be taken directly from it. (default 100)
            // Output:	-x: equispaced abscis points
            //          -y: estimates of p(x)

            // This function is part of the Kernel Methods Toolbox(KMBOX) for MATLAB. 
            // http://sourceforge.net/p/kmbox
            // Converted to C# code by ksandric

            // edit MvdM. Calculate bandwidth (sigma), based on Silverman's rule of thumb (R's standard setting), instead of a given parameter
            double avg = data.Average();
            double stdev = Math.Sqrt(data.Average(v => Math.Pow(v - avg, 2)));
            double q1 = QUARTILE(data, 25);
            double q3 = QUARTILE(data, 75);

            double sigma = 0.9 * Math.Min(stdev, ((q3 - q1) / 1.34)) * Math.Pow(data.Length, -0.2);

            double MAX = Double.MinValue, MIN = Double.MaxValue;
            int N = data.Length; // number of data points
            // Find MIN MAX values in data
            for (int i = 0; i < N; i++)
            {
                if (MAX < data[i])
                {
                    MAX = data[i];
                }
                if (MIN > data[i])
                {
                    MIN = data[i];
                }
            }
            MIN = Math.Floor(MIN);
            MAX = Math.Ceiling(MAX);
            int nsteps = Convert.ToInt32(Math.Round(MAX)) - Convert.ToInt32(Math.Round(MIN)); // Calculate number of steps (yearly

            double[,] result = new double[nsteps, 2];
            double[] x = new double[nsteps], y = new double[nsteps];


            // Like MATLAB linspace(MIN, MAX, nsteps);
            x[0] = MIN;
            for (int i = 1; i < nsteps; i++)
            {
                x[i] = x[i - 1] + ((MAX - MIN) / nsteps);
            }

            // kernel density estimation
            double c = 1.0 / (Math.Sqrt(2 * Math.PI * sigma * sigma));
            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < nsteps; j++)
                {
                    y[j] = y[j] + 1.0 / N * c * Math.Exp(-(data[i] - x[j]) * (data[i] - x[j]) / (2 * sigma * sigma));
                }
            }

            // compilation of the X,Y to result. Good for creating plot(x, y)
            for (int i = 0; i < nsteps; i++)
            {
                result[i, 0] = x[i];
                result[i, 1] = y[i];
            }
            return result;
        }

        internal static double QUARTILE(double[] array, double quantile)
        {
            // from https://stackoverflow.com/questions/31451446/worksheetfunction-quartile-equivalent-in-c-sharp
            Array.Sort(array);


            if (quantile >= 100.0d) return array[array.Length - 1];

            double position = (double)(array.Length + 1) * quantile / 100.0;
            double leftNumber = 0.0d, rightNumber = 0.0d;

            double n = quantile / 100.0d * (array.Length - 1) + 1.0d;

            if (position >= 1)
            {
                leftNumber = array[(int)System.Math.Floor(n) - 1];
                rightNumber = array[(int)System.Math.Floor(n)];
            }
            else
            {
                leftNumber = array[0]; // first data
                rightNumber = array[1]; // first data
            }

            if (leftNumber == rightNumber)
                return leftNumber;
            else
            {
                double part = n - System.Math.Floor(n);
                return leftNumber + part * (rightNumber - leftNumber);
            }
        }

        #endregion

    }
}
