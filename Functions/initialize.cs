using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LORICA4
{
    public partial class Mother_form
    {
        #region initialisation code

        void initialize_once_testing()
        {
            if (daily_water.Checked && input_data_error == false)
            {
                try
                {
                    filename = this.dailyP.Text;
                    if (memory_records_d == false) { makedailyrecords(filename); }
                    // Debug.WriteLine("P_all record created successfully");

                    int namelength = filename.Length;
                    string fn = filename.Substring(0, (namelength - 4));
                    filename = fn + "_sc" + P_scen + ".csv";

                    read_record(filename, P_all);
                    // Debug.WriteLine("P_all read successfully");

                    //filename = this.dailyET0.Text;
                    //if (memory_records_d == false) { makedailyrecords(filename); }
                    //read_record(filename, ET0_all);
                    //Debug.WriteLine("ET0_all read successfully");

                    filename = this.dailyD.Text;
                    if (memory_records_d == false) { makedailyrecords(filename); }
                    read_record(filename, D_all);
                    // Debug.WriteLine("D_all read successfully");

                    filename = this.dailyT_avg.Text;
                    if (memory_records_d == false) { makedailyrecords(filename); }
                    read_record(filename, Tavg_all);
                    // Debug.WriteLine("Tavg_all read successfully");

                    filename = this.dailyT_min.Text;
                    if (memory_records_d == false) { makedailyrecords(filename); }
                    read_record(filename, Tmin_all);
                    // Debug.WriteLine("Tmin_all read successfully");

                    filename = this.dailyT_max.Text;
                    if (memory_records_d == false) { makedailyrecords(filename); }
                    read_record(filename, Tmax_all);
                    // Debug.WriteLine("Tmax_all read successfully");


                    Array.Clear(Ra_rcm, 0, Ra_rcm.Length); //Ra_rcm = new double[nr, nc, 12];
                    Array.Clear(OFy_m, 0, OFy_m.Length); //OFy_m = new double[nr, nc, 10]; // 0: outflow, 1:8 flow to neighbours, 9: inflow
                    Array.Clear(Iy, 0, Iy.Length); //Iy = new double[nr, nc];
                    Array.Clear(waterfactor, 0, waterfactor.Length); //waterfactor = new double[nr, nc];
                    Array.Clear(pond_y, 0, pond_y.Length); //pond_y = new double[nr, nc];
                    Array.Clear(outflow_y, 0, outflow_y.Length);// outflow_y = new double[nr, nc];
                    Array.Clear(total_outflow_y, 0, total_outflow_y.Length); //total_outflow_y = new double[nr, nc];
                    Array.Clear(water_balance_m, 0, water_balance_m.Length); //water_balance_m = new double[nr, nc, 5]; // 1: rainfall, 2: actual ET, 3: runon, 4: runoff, 5: I    //where else is this reference ???
                    Array.Clear(ETay, 0, ETay.Length); //ETay = new double[nr, nc];
                    Array.Clear(ET0y, 0, ET0y.Length);// ET0y = new double[nr, nc];
                    Array.Clear(veg_correction_factor, 0, veg_correction_factor.Length); //veg_correction_factor = new double[nr, nc];
                    Array.Clear(waterflow_m3, 0, waterflow_m3.Length); //waterflow_m3 = new double[nr, nc];
                    Array.Clear(vegetation_type, 0, vegetation_type.Length); //vegetation_type = new int[nr, nc];

                    for (int row = 0; row < nr; row++)
                    {
                        for (int col = 0; col < nc; col++)
                        {
                            veg_correction_factor[row, col] = 1;
                            vegetation_type[row, col] = 0;
                        }
                    }
                    /*
                    Ks_topsoil_mh = new double[nr, nc];
                    Ks_md = new double[nr, nc, max_soil_layers];
                    stagdepth = new double[nr, nc];
                    */
                    snow_m = 0;
                    snow_start_m = 0;
                    snowfall_m = 0;
                    snowmelt_factor_mTd = Convert.ToDouble(snowmelt_factor_textbox.Text);
                    snow_threshold_C = Convert.ToDouble(snow_threshold_textbox.Text);
                }
                catch { Debug.WriteLine(" problem preparing for hourly water balance "); }
            }
            if (input_data_error == false)
            {
                if (check_space_soildepth.Checked)
                {
                    filename = this.soildepth_input_filename_textbox.Text;
                    read_double(filename, soildepth_m);
                    Debug.WriteLine("read soildepth");
                }
                if (check_space_till_fields.Checked)
                {
                    filename = this.tillfields_input_filename_textbox.Text;
                    read_integer(filename, tillfields);
                    Debug.WriteLine("read tillfields");
                }
                if (check_space_landuse.Checked)
                {
                    filename = this.landuse_input_filename_textbox.Text;
                    read_integer(filename, landuse);
                    Debug.WriteLine("read landuse");
                }
                if (check_space_evap.Checked)
                {
                    filename = this.evap_input_filename_textbox.Text;
                    read_double(filename, evapotranspiration);
                }
                if (check_space_infil.Checked)
                {
                    filename = this.infil_input_filename_textbox.Text;
                    read_double(filename, infil);
                }
                if (check_space_rain.Checked)
                {
                    filename = this.rain_input_filename_textbox.Text;
                    read_double(filename, rain_m);
                }
                // If required, read timeseries instead.
                if (check_time_landuse.Checked)
                {
                    filename = this.landuse_input_filename_textbox.Text;
                    read_integer(filename, landuse);
                }
                if (check_time_evap.Checked)
                {
                    filename = this.evap_input_filename_textbox.Text;
                    if (memory_records == false) { makerecords(filename); }
                    read_record(filename, evap_record);
                }
                if (check_time_infil.Checked)
                {
                    filename = this.infil_input_filename_textbox.Text;
                    if (memory_records == false) { makerecords(filename); }
                    read_record(filename, infil_record);
                }
                if (check_time_rain.Checked)
                {
                    filename = this.rain_input_filename_textbox.Text;
                    if (memory_records == false) { makerecords(filename); }
                    read_record(filename, rainfall_record);
                }
                if (check_time_T.Checked)
                {
                    filename = this.temp_input_filename_textbox.Text;
                    if (memory_records == false) { makerecords(filename); }
                    read_record(filename, temp_record);
                }
                if (check_time_till_fields.Checked)
                {
                    filename = this.tillfields_input_filename_textbox.Text;
                    if (memory_records == false) { makerecords(filename); }
                    read_record(filename, till_record);
                    Debug.WriteLine("Tillage time parameters read");
                }
            }


            try
            {
                for (int row = 0; row < nr; row++)
                {
                    for (int col = 0; col < nc; col++)
                    {
                        dz_soil[row, col] = 0;
                        if (Creep_Checkbox.Checked) { sum_creep_grid[row, col] = 0; creep[row, col] = 0; }
                        if (Water_ero_checkbox.Checked && only_waterflow_checkbox.Checked == false) { sum_water_erosion[row, col] = 0; total_sed_export = 0; }
                        if (treefall_checkbox.Checked) { dz_treefall[row, col] = 0; treefall_count[row, col] = 0; }
                        if (Biological_weathering_checkbox.Checked) { sum_biological_weathering[row, col] = 0; }
                        if (Frost_weathering_checkbox.Checked) { sum_frost_weathering[row, col] = 0; }
                        if (Tillage_checkbox.Checked) { sum_tillage[row, col] = 0; total_sum_tillage = 0; dz_till_bd[row, col] = 0; }
                        if (Landslide_checkbox.Checked) { sum_landsliding[row, col] = 0; total_sum_tillage = 0; }
                        if (soildepth_m[row, col] < 0.0 && soildepth_m[row, col] != -9999) { soildepth_error += soildepth_m[row, col]; soildepth_m[row, col] = 0; }
                        if (uplift_active_checkbox.Checked) { sum_uplift[row, col] = 0; total_sum_uplift = 0; }
                        if (tilting_active_checkbox.Checked) { sum_tilting[row, col] = 0; total_sum_tilting = 0; }
                        if (check_space_soildepth.Checked != true) { soildepth_m[row, col] = soildepth_value; }
                        if (check_space_till_fields.Checked != true && Tillage_checkbox.Checked)
                        {
                            tillfields[row, col] = 1;

                        }

                        if (Water_ero_checkbox.Checked && only_waterflow_checkbox.Checked == false)
                        {
                            K_fac[row, col] = advection_erodibility; P_fac[row, col] = P_act;
                        } //WVG K_fac matrix initialisation is needed when landuse is disabled

                        if (Water_ero_checkbox.Checked && check_space_landuse.Checked == true)
                        {
                            //currently, this will throw an exception if landuse is actually spatial //development required //ArT
                            if (landuse[row, col] == 1)
                            {
                                infil[row, col] *= System.Convert.ToDouble(landuse_determinator.LU1_Inf_textbox.Text);
                                evapotranspiration[row, col] *= System.Convert.ToDouble(landuse_determinator.LU1_Evap_textbox.Text);
                                K_fac[row, col] *= System.Convert.ToDouble(landuse_determinator.LU1_Ero_textbox.Text);
                                P_fac[row, col] *= System.Convert.ToDouble(landuse_determinator.LU1_Dep_textbox.Text);
                            }
                            if (landuse[row, col] == 2)
                            {
                                infil[row, col] *= System.Convert.ToDouble(landuse_determinator.LU2_Inf_textbox.Text);
                                evapotranspiration[row, col] *= System.Convert.ToDouble(landuse_determinator.LU2_Evap_textbox.Text);
                                K_fac[row, col] *= System.Convert.ToDouble(landuse_determinator.LU2_Ero_textbox.Text);
                                P_fac[row, col] *= System.Convert.ToDouble(landuse_determinator.LU2_Dep_textbox.Text);
                            }
                            if (landuse[row, col] == 3)
                            {
                                infil[row, col] *= System.Convert.ToDouble(landuse_determinator.LU3_Inf_textbox.Text);
                                evapotranspiration[row, col] *= System.Convert.ToDouble(landuse_determinator.LU3_Evap_textbox.Text);
                                K_fac[row, col] *= System.Convert.ToDouble(landuse_determinator.LU3_Ero_textbox.Text);
                                P_fac[row, col] *= System.Convert.ToDouble(landuse_determinator.LU3_Dep_textbox.Text);
                            }
                            if (landuse[row, col] == 4)
                            {
                                infil[row, col] *= System.Convert.ToDouble(landuse_determinator.LU4_Inf_textbox.Text);
                                evapotranspiration[row, col] *= System.Convert.ToDouble(landuse_determinator.LU4_Evap_textbox.Text);
                                K_fac[row, col] *= System.Convert.ToDouble(landuse_determinator.LU4_Ero_textbox.Text);
                                P_fac[row, col] *= System.Convert.ToDouble(landuse_determinator.LU4_Dep_textbox.Text);
                            }
                            if (landuse[row, col] == 5)
                            {
                                infil[row, col] *= System.Convert.ToDouble(landuse_determinator.LU5_Inf_textbox.Text);
                                evapotranspiration[row, col] *= System.Convert.ToDouble(landuse_determinator.LU5_Evap_textbox.Text);
                                K_fac[row, col] *= System.Convert.ToDouble(landuse_determinator.LU5_Ero_textbox.Text);
                                P_fac[row, col] *= System.Convert.ToDouble(landuse_determinator.LU5_Dep_textbox.Text);
                            }
                            if (landuse[row, col] == 6)
                            {
                                infil[row, col] *= System.Convert.ToDouble(landuse_determinator.LU6_Inf_textbox.Text);
                                evapotranspiration[row, col] *= System.Convert.ToDouble(landuse_determinator.LU6_Evap_textbox.Text);
                                K_fac[row, col] *= System.Convert.ToDouble(landuse_determinator.LU6_Ero_textbox.Text);
                                P_fac[row, col] *= System.Convert.ToDouble(landuse_determinator.LU6_Dep_textbox.Text);
                            }
                            if (landuse[row, col] == 7)
                            {
                                infil[row, col] *= System.Convert.ToDouble(landuse_determinator.LU7_Inf_textbox.Text);
                                evapotranspiration[row, col] *= System.Convert.ToDouble(landuse_determinator.LU7_Evap_textbox.Text);
                                K_fac[row, col] *= System.Convert.ToDouble(landuse_determinator.LU7_Ero_textbox.Text);
                                P_fac[row, col] *= System.Convert.ToDouble(landuse_determinator.LU7_Dep_textbox.Text);
                            }
                            if (landuse[row, col] == 8)
                            {
                                infil[row, col] *= System.Convert.ToDouble(landuse_determinator.LU8_Inf_textbox.Text);
                                evapotranspiration[row, col] *= System.Convert.ToDouble(landuse_determinator.LU8_Evap_textbox.Text);
                                K_fac[row, col] *= System.Convert.ToDouble(landuse_determinator.LU8_Ero_textbox.Text);
                                P_fac[row, col] *= System.Convert.ToDouble(landuse_determinator.LU8_Dep_textbox.Text);
                            }
                            if (landuse[row, col] == 9)
                            {
                                infil[row, col] *= System.Convert.ToDouble(landuse_determinator.LU9_Inf_textbox.Text);
                                evapotranspiration[row, col] *= System.Convert.ToDouble(landuse_determinator.LU9_Evap_textbox.Text);
                                K_fac[row, col] *= System.Convert.ToDouble(landuse_determinator.LU9_Ero_textbox.Text);
                                P_fac[row, col] *= System.Convert.ToDouble(landuse_determinator.LU9_Dep_textbox.Text);
                            }
                            if (landuse[row, col] == 10)
                            {
                                infil[row, col] *= System.Convert.ToDouble(landuse_determinator.LU10_Inf_textbox.Text);
                                evapotranspiration[row, col] *= System.Convert.ToDouble(landuse_determinator.LU10_Evap_textbox.Text);
                                K_fac[row, col] *= System.Convert.ToDouble(landuse_determinator.LU10_Ero_textbox.Text);
                                P_fac[row, col] *= System.Convert.ToDouble(landuse_determinator.LU10_Dep_textbox.Text);
                            }
                        }
                    } //for
                } //for

                initialise_soil();
                if (findnegativetexture())
                {
                    Debug.WriteLine("err_ini1");
                    // Debugger.Break(); 
                }

                // displaysoil(0, 0);
                // Debug.WriteLine("Total catchment mass = " + total_catchment_mass_decimal());

                //displaysoil(50, 0);
                //writesoil(0, 0);
            } //try
            catch { Debug.WriteLine(" problem assigning starting values to matrices "); }

            if (fill_sinks_before_checkbox.Checked)
            {
                try
                {
                    findsinks();
                    if (numberofsinks > 0)
                    {
                        searchdepressions();
                        //define_fillheight_new();
                        for (int row = 0; row < nr; row++)
                        {
                            for (int col = 0; col < nc; col++)
                            {
                                if (dtm[row, col] < dtmfill_A[row, col] && dtm[row, col] != -9999)
                                {
                                    dtm[row, col] = dtmfill_A[row, col];
                                }
                            }
                        }
                    }
                }
                catch { Debug.WriteLine(" problem with sink definition "); }
            }

            // Timeseries preparation
            try
            {
                number_of_outputs = 0;
                if (timeseries.timeseries_cell_waterflow_check.Checked) { timeseries_order[1] = number_of_outputs; number_of_outputs++; }
                if (timeseries.timeseries_cell_altitude_check.Checked) { timeseries_order[2] = number_of_outputs; number_of_outputs++; }
                if (timeseries.timeseries_net_ero_check.Checked) { timeseries_order[3] = number_of_outputs; number_of_outputs++; }
                if (timeseries.timeseries_number_dep_check.Checked) { timeseries_order[4] = number_of_outputs; number_of_outputs++; }
                if (timeseries.timeseries_number_erosion_check.Checked) { timeseries_order[5] = number_of_outputs; number_of_outputs++; }
                if (timeseries.timeseries_number_waterflow_check.Checked) { timeseries_order[6] = number_of_outputs; number_of_outputs++; }
                if (timeseries.timeseries_SDR_check.Checked) { timeseries_order[7] = number_of_outputs; number_of_outputs++; }
                if (timeseries.timeseries_total_average_alt_check.Checked) { timeseries_order[8] = number_of_outputs; number_of_outputs++; }
                if (timeseries.timeseries_total_dep_check.Checked) { timeseries_order[9] = number_of_outputs; number_of_outputs++; }
                if (timeseries.timeseries_total_ero_check.Checked) { timeseries_order[10] = number_of_outputs; number_of_outputs++; }
                if (timeseries.timeseries_total_evap_check.Checked) { timeseries_order[11] = number_of_outputs; number_of_outputs++; }
                if (timeseries.timeseries_total_infil_check.Checked) { timeseries_order[12] = number_of_outputs; number_of_outputs++; }
                if (timeseries.timeseries_total_outflow_check.Checked) { timeseries_order[13] = number_of_outputs; number_of_outputs++; }
                if (timeseries.timeseries_total_rain_check.Checked) { timeseries_order[14] = number_of_outputs; number_of_outputs++; }

                if (timeseries.timeseries_outflow_cells_checkbox.Checked) { timeseries_order[15] = number_of_outputs; number_of_outputs++; }
                if (timeseries.timeseries_sedexport_checkbox.Checked) { timeseries_order[16] = number_of_outputs; number_of_outputs++; }
                if (timeseries.timeseries_sedexport_checkbox.Checked) { timeseries_order[17] = number_of_outputs; number_of_outputs++; }
                if (timeseries.timeseries_sedexport_checkbox.Checked) { timeseries_order[18] = number_of_outputs; number_of_outputs++; }
                if (timeseries.timeseries_sedexport_checkbox.Checked) { timeseries_order[19] = number_of_outputs; number_of_outputs++; }
                if (timeseries.timeseries_sedexport_checkbox.Checked) { timeseries_order[20] = number_of_outputs; number_of_outputs++; }
                if (timeseries.timeseries_sedexport_checkbox.Checked) { timeseries_order[21] = number_of_outputs; number_of_outputs++; }
                if (timeseries.timeseries_sedexport_checkbox.Checked) { timeseries_order[22] = number_of_outputs; number_of_outputs++; }

                if (timeseries.total_phys_weath_checkbox.Checked) { timeseries_order[23] = number_of_outputs; number_of_outputs++; }
                if (timeseries.total_chem_weath_checkbox.Checked) { timeseries_order[24] = number_of_outputs; number_of_outputs++; }
                if (timeseries.total_fine_formed_checkbox.Checked) { timeseries_order[25] = number_of_outputs; number_of_outputs++; }
                if (timeseries.total_fine_eluviated_checkbox.Checked) { timeseries_order[26] = number_of_outputs; number_of_outputs++; }
                if (timeseries.total_mass_bioturbed_checkbox.Checked) { timeseries_order[27] = number_of_outputs; number_of_outputs++; }
                if (timeseries.total_OM_input_checkbox.Checked) { timeseries_order[28] = number_of_outputs; number_of_outputs++; }
                if (timeseries.total_average_soilthickness_checkbox.Checked) { timeseries_order[29] = number_of_outputs; number_of_outputs++; }
                if (timeseries.timeseries_number_soil_thicker_checkbox.Checked) { timeseries_order[30] = number_of_outputs; number_of_outputs++; }
                if (timeseries.timeseries_coarser_checkbox.Checked) { timeseries_order[31] = number_of_outputs; number_of_outputs++; }
                if (timeseries.timeseries_soil_depth_checkbox.Checked) { timeseries_order[32] = number_of_outputs; number_of_outputs++; }

                if (timeseries.timeseries_soil_mass_checkbox.Checked) { timeseries_order[33] = number_of_outputs; number_of_outputs++; }
                Debug.WriteLine("timeseries preparation was succesful");
            }
            catch { Debug.WriteLine("timeseries preparation was unsuccesful"); }

            //if ((Final_output_checkbox.Checked && t == end_time) || (Regular_output_checkbox.Checked && (t % (int.Parse(Box_years_output.Text)) == 0)))
            //Debug.WriteLine(" successfully ended initialisations  ");
        }
        void initialise_once()        //fills the inputgrids with values
        {

            if (daily_water.Checked && input_data_error == false)
            {
                try
                {
                    filename = this.dailyP.Text;
                    if (memory_records_d == false) { makedailyrecords(filename); }
                    // Debug.WriteLine("P_all record created successfully");

                    int namelength = filename.Length;
                    string fn = filename.Substring(0, (namelength - 4));
                    filename = fn + "_sc" + P_scen + ".csv";

                    read_record(filename, P_all);
                    // Debug.WriteLine("P_all read successfully");

                    //filename = this.dailyET0.Text;
                    //if (memory_records_d == false) { makedailyrecords(filename); }
                    //read_record(filename, ET0_all);
                    //Debug.WriteLine("ET0_all read successfully");

                    filename = this.dailyD.Text;
                    if (memory_records_d == false) { makedailyrecords(filename); }
                    read_record(filename, D_all);
                    // Debug.WriteLine("D_all read successfully");

                    filename = this.dailyT_avg.Text;
                    if (memory_records_d == false) { makedailyrecords(filename); }
                    read_record(filename, Tavg_all);
                    // Debug.WriteLine("Tavg_all read successfully");

                    filename = this.dailyT_min.Text;
                    if (memory_records_d == false) { makedailyrecords(filename); }
                    read_record(filename, Tmin_all);
                    // Debug.WriteLine("Tmin_all read successfully");

                    filename = this.dailyT_max.Text;
                    if (memory_records_d == false) { makedailyrecords(filename); }
                    read_record(filename, Tmax_all);
                    // Debug.WriteLine("Tmax_all read successfully");

                    //// Hargreaves extraterrestrial radiation
                    //// http://www.fao.org/docrep/X0490E/x0490e07.htm#radiation
                    //double dr, delta, ws;
                    //double lat_st = Math.PI/180*(System.Convert.ToDouble(latitude_deg.Text)+ System.Convert.ToDouble(latitude_min.Text) / 60); // latitude in radians

                    //for (double day_ra = 1; day_ra <= 365; day_ra++)
                    //{
                    //    dr = 1 + 0.033 * Math.Cos(2 * Math.PI * (day_ra / 365)); //inverse relative distance Earth-Sun
                    //    delta = 0.409 * Math.Sin(2 * Math.PI * (day_ra / 365) - 1.39); //solar declination [rad].
                    //    ws = Math.Acos(-Math.Tan(lat_st) * Math.Tan(delta)); // sunset hour angle [rad]
                    //    Ra_ann[Convert.ToInt16(day_ra - 1)] = (24 * 60 / Math.PI * 0.082 * dr * (ws * Math.Sin(lat_st) * Math.Sin(delta) + Math.Cos(lat_st) * Math.Cos(delta) * Math.Sin(ws))) * 0.408; // extraterrestrial radiation [mm d-1]
                    //}

                    Ra_rcm = new double[nr, nc, 12];
                    OFy_m = new double[nr, nc, 10]; // 0: outflow, 1:8 flow to neighbours, 9: inflow
                    Iy = new double[nr, nc];
                    waterfactor = new double[nr, nc];
                    pond_y = new double[nr, nc];
                    outflow_y = new double[nr, nc];
                    total_outflow_y = new double[nr, nc];
                    water_balance_m = new double[nr, nc, 5]; // 1: rainfall, 2: actual ET, 3: runon, 4: runoff, 5: I
                    ETay = new double[nr, nc];
                    ET0y = new double[nr, nc];
                    veg_correction_factor = new double[nr, nc];
                    waterflow_m3 = new double[nr, nc];
                    vegetation_type = new int[nr, nc];

                    for (int row = 0; row < nr; row++)
                    {
                        for (int col = 0; col < nc; col++)
                        {
                            veg_correction_factor[row, col] = 1;
                            vegetation_type[row, col] = 0;
                        }
                    }

                    Ks_topsoil_m_h = new double[nr, nc];
                    Ks_md = new double[nr, nc, max_soil_layers];
                    stagdepth = new double[nr, nc];
                    snow_m = 0;
                    snow_start_m = 0;
                    snowfall_m = 0;
                    snowmelt_factor_mTd = Convert.ToDouble(snowmelt_factor_textbox.Text);
                    snow_threshold_C = Convert.ToDouble(snow_threshold_textbox.Text);
                    // snowmelt factor now assumed to be 0.004 m per degree per day, as a mean from the following paper: Hock 2003, https://www.sciencedirect.com/science/article/pii/S0022169403002579#BIB50
                    // DEVELOP I didn't take the correct parameter (DDF instead of Fm, see paper). Change to right parameter based on the paper of Gottlieb, 1980. Other developments are varying melt factors, based on month, incoming radiation etc. (See Hock 2003)
                }
                catch { Debug.WriteLine(" problem preparing for hourly water balance "); }
            }

            if (check_space_soildepth.Checked && input_data_error == false)
            {
                filename = this.soildepth_input_filename_textbox.Text;
                read_double(filename, soildepth_m);
                Debug.WriteLine("read soildepth");
            }
            if (check_space_till_fields.Checked && input_data_error == false)
            {
                filename = this.tillfields_input_filename_textbox.Text;
                read_integer(filename, tillfields);
                Debug.WriteLine("read tillfields");
            }
            if (check_space_landuse.Checked && input_data_error == false)
            {
                filename = this.landuse_input_filename_textbox.Text;
                read_integer(filename, landuse);
                Debug.WriteLine("read landuse");
            }
            if (check_space_evap.Checked && input_data_error == false)
            {
                filename = this.evap_input_filename_textbox.Text;
                read_double(filename, evapotranspiration);
            }
            if (check_space_infil.Checked && input_data_error == false)
            {
                filename = this.infil_input_filename_textbox.Text;
                read_double(filename, infil);
            }
            if (check_space_rain.Checked && input_data_error == false)
            {
                filename = this.rain_input_filename_textbox.Text;
                read_double(filename, rain_m);
            }
            // If required, read timeseries instead.
            if (check_time_landuse.Checked && input_data_error == false)
            {
                filename = this.landuse_input_filename_textbox.Text;
                read_integer(filename, landuse);
            }
            if (check_time_evap.Checked && input_data_error == false)
            {
                filename = this.evap_input_filename_textbox.Text;
                if (memory_records == false) { makerecords(filename); }
                read_record(filename, evap_record);
            }
            if (check_time_infil.Checked && input_data_error == false)
            {
                filename = this.infil_input_filename_textbox.Text;
                if (memory_records == false) { makerecords(filename); }
                read_record(filename, infil_record);
            }
            if (check_time_rain.Checked && input_data_error == false)
            {
                filename = this.rain_input_filename_textbox.Text;
                if (memory_records == false) { makerecords(filename); }
                read_record(filename, rainfall_record);
            }
            if (check_time_T.Checked && input_data_error == false)
            {
                filename = this.temp_input_filename_textbox.Text;
                if (memory_records == false) { makerecords(filename); }
                read_record(filename, temp_record);
            }

            if (check_time_till_fields.Checked && input_data_error == false)
            {
                filename = this.tillfields_input_filename_textbox.Text;
                if (memory_records == false) { makerecords(filename); }
                read_record(filename, till_record);
                Debug.WriteLine("Tillage time parameters read");
            }

            try
            {
                // Debug.WriteLine(" assigning starting values for geomorph  ");
                for (int row = 0; row < nr; row++)
                {
                    for (int col = 0; col < nc; col++)
                    {
                        dz_soil[row, col] = 0;
                        if (Creep_Checkbox.Checked) { sum_creep_grid[row, col] = 0; creep[row, col] = 0; }
                        if (Water_ero_checkbox.Checked && only_waterflow_checkbox.Checked == false) { sum_water_erosion[row, col] = 0; total_sed_export = 0; }
                        if (treefall_checkbox.Checked) { dz_treefall[row, col] = 0; treefall_count[row, col] = 0; }
                        if (Biological_weathering_checkbox.Checked) { sum_biological_weathering[row, col] = 0; }
                        if (Frost_weathering_checkbox.Checked) { sum_frost_weathering[row, col] = 0; }
                        if (Tillage_checkbox.Checked) { sum_tillage[row, col] = 0; total_sum_tillage = 0; dz_till_bd[row, col] = 0; }
                        if (Landslide_checkbox.Checked) { sum_landsliding[row, col] = 0; total_sum_tillage = 0; }
                        if (soildepth_m[row, col] < 0.0 && soildepth_m[row, col] != -9999) { soildepth_error += soildepth_m[row, col]; soildepth_m[row, col] = 0; }
                        if (uplift_active_checkbox.Checked) { sum_uplift[row, col] = 0; total_sum_uplift = 0; }
                        if (tilting_active_checkbox.Checked) { sum_tilting[row, col] = 0; total_sum_tilting = 0; }
                        if (check_space_soildepth.Checked != true) { soildepth_m[row, col] = soildepth_value; }
                        if (check_space_till_fields.Checked != true && Tillage_checkbox.Checked)
                        {
                            tillfields[row, col] = 1;

                        }

                        if (Water_ero_checkbox.Checked && only_waterflow_checkbox.Checked == false)
                        {
                            K_fac[row, col] = advection_erodibility; P_fac[row, col] = P_act;
                        } //WVG K_fac matrix initialisation is needed when landuse is disabled

                        if (Water_ero_checkbox.Checked && check_space_landuse.Checked == true)
                        {
                            //currently, this will throw an exception if landuse is actually spatial //development required //ArT
                            if (landuse[row, col] == 1)
                            {
                                infil[row, col] *= System.Convert.ToDouble(landuse_determinator.LU1_Inf_textbox.Text);
                                evapotranspiration[row, col] *= System.Convert.ToDouble(landuse_determinator.LU1_Evap_textbox.Text);
                                K_fac[row, col] *= System.Convert.ToDouble(landuse_determinator.LU1_Ero_textbox.Text);
                                P_fac[row, col] *= System.Convert.ToDouble(landuse_determinator.LU1_Dep_textbox.Text);
                            }
                            if (landuse[row, col] == 2)
                            {
                                infil[row, col] *= System.Convert.ToDouble(landuse_determinator.LU2_Inf_textbox.Text);
                                evapotranspiration[row, col] *= System.Convert.ToDouble(landuse_determinator.LU2_Evap_textbox.Text);
                                K_fac[row, col] *= System.Convert.ToDouble(landuse_determinator.LU2_Ero_textbox.Text);
                                P_fac[row, col] *= System.Convert.ToDouble(landuse_determinator.LU2_Dep_textbox.Text);
                            }
                            if (landuse[row, col] == 3)
                            {
                                infil[row, col] *= System.Convert.ToDouble(landuse_determinator.LU3_Inf_textbox.Text);
                                evapotranspiration[row, col] *= System.Convert.ToDouble(landuse_determinator.LU3_Evap_textbox.Text);
                                K_fac[row, col] *= System.Convert.ToDouble(landuse_determinator.LU3_Ero_textbox.Text);
                                P_fac[row, col] *= System.Convert.ToDouble(landuse_determinator.LU3_Dep_textbox.Text);
                            }
                            if (landuse[row, col] == 4)
                            {
                                infil[row, col] *= System.Convert.ToDouble(landuse_determinator.LU4_Inf_textbox.Text);
                                evapotranspiration[row, col] *= System.Convert.ToDouble(landuse_determinator.LU4_Evap_textbox.Text);
                                K_fac[row, col] *= System.Convert.ToDouble(landuse_determinator.LU4_Ero_textbox.Text);
                                P_fac[row, col] *= System.Convert.ToDouble(landuse_determinator.LU4_Dep_textbox.Text);
                            }
                            if (landuse[row, col] == 5)
                            {
                                infil[row, col] *= System.Convert.ToDouble(landuse_determinator.LU5_Inf_textbox.Text);
                                evapotranspiration[row, col] *= System.Convert.ToDouble(landuse_determinator.LU5_Evap_textbox.Text);
                                K_fac[row, col] *= System.Convert.ToDouble(landuse_determinator.LU5_Ero_textbox.Text);
                                P_fac[row, col] *= System.Convert.ToDouble(landuse_determinator.LU5_Dep_textbox.Text);
                            }
                            if (landuse[row, col] == 6)
                            {
                                infil[row, col] *= System.Convert.ToDouble(landuse_determinator.LU6_Inf_textbox.Text);
                                evapotranspiration[row, col] *= System.Convert.ToDouble(landuse_determinator.LU6_Evap_textbox.Text);
                                K_fac[row, col] *= System.Convert.ToDouble(landuse_determinator.LU6_Ero_textbox.Text);
                                P_fac[row, col] *= System.Convert.ToDouble(landuse_determinator.LU6_Dep_textbox.Text);
                            }
                            if (landuse[row, col] == 7)
                            {
                                infil[row, col] *= System.Convert.ToDouble(landuse_determinator.LU7_Inf_textbox.Text);
                                evapotranspiration[row, col] *= System.Convert.ToDouble(landuse_determinator.LU7_Evap_textbox.Text);
                                K_fac[row, col] *= System.Convert.ToDouble(landuse_determinator.LU7_Ero_textbox.Text);
                                P_fac[row, col] *= System.Convert.ToDouble(landuse_determinator.LU7_Dep_textbox.Text);
                            }
                            if (landuse[row, col] == 8)
                            {
                                infil[row, col] *= System.Convert.ToDouble(landuse_determinator.LU8_Inf_textbox.Text);
                                evapotranspiration[row, col] *= System.Convert.ToDouble(landuse_determinator.LU8_Evap_textbox.Text);
                                K_fac[row, col] *= System.Convert.ToDouble(landuse_determinator.LU8_Ero_textbox.Text);
                                P_fac[row, col] *= System.Convert.ToDouble(landuse_determinator.LU8_Dep_textbox.Text);
                            }
                            if (landuse[row, col] == 9)
                            {
                                infil[row, col] *= System.Convert.ToDouble(landuse_determinator.LU9_Inf_textbox.Text);
                                evapotranspiration[row, col] *= System.Convert.ToDouble(landuse_determinator.LU9_Evap_textbox.Text);
                                K_fac[row, col] *= System.Convert.ToDouble(landuse_determinator.LU9_Ero_textbox.Text);
                                P_fac[row, col] *= System.Convert.ToDouble(landuse_determinator.LU9_Dep_textbox.Text);
                            }
                            if (landuse[row, col] == 10)
                            {
                                infil[row, col] *= System.Convert.ToDouble(landuse_determinator.LU10_Inf_textbox.Text);
                                evapotranspiration[row, col] *= System.Convert.ToDouble(landuse_determinator.LU10_Evap_textbox.Text);
                                K_fac[row, col] *= System.Convert.ToDouble(landuse_determinator.LU10_Ero_textbox.Text);
                                P_fac[row, col] *= System.Convert.ToDouble(landuse_determinator.LU10_Dep_textbox.Text);
                            }
                        }
                    } //for
                } //for
                  // Debug.WriteLine(" assigned starting values for geomorph  ");
                  // Debug.WriteLine("before initialise soil {0}", texture_kg[0, 0, 0, 2]);

                initialise_soil();
                //Debug.WriteLine("after initialise soil {0}", texture_kg[0, 0, 0, 2]);
                if (findnegativetexture())
                {
                    Debug.WriteLine("err_ini1");
                    // Debugger.Break(); 
                }

                // displaysoil(0, 0);
                // Debug.WriteLine("Total catchment mass = " + total_catchment_mass_decimal());

                //displaysoil(50, 0);
                //writesoil(0, 0);
            } //try
            catch { Debug.WriteLine(" problem assigning starting values to matrices "); }

            if (fill_sinks_before_checkbox.Checked)
            {
                try
                {
                    findsinks();
                    if (numberofsinks > 0)
                    {
                        searchdepressions();
                        //define_fillheight_new();
                        for (int row = 0; row < nr; row++)
                        {
                            for (int col = 0; col < nc; col++)
                            {
                                if (dtm[row, col] < dtmfill_A[row, col] && dtm[row, col] != -9999)
                                {
                                    dtm[row, col] = dtmfill_A[row, col];
                                }
                            }
                        }
                    }
                }
                catch { Debug.WriteLine(" problem with sink definition "); }
            }
            // Timeseries preparation
            try
            {
                number_of_outputs = 0;
                if (timeseries.timeseries_cell_waterflow_check.Checked) { timeseries_order[1] = number_of_outputs; number_of_outputs++; }
                if (timeseries.timeseries_cell_altitude_check.Checked) { timeseries_order[2] = number_of_outputs; number_of_outputs++; }
                if (timeseries.timeseries_net_ero_check.Checked) { timeseries_order[3] = number_of_outputs; number_of_outputs++; }
                if (timeseries.timeseries_number_dep_check.Checked) { timeseries_order[4] = number_of_outputs; number_of_outputs++; }
                if (timeseries.timeseries_number_erosion_check.Checked) { timeseries_order[5] = number_of_outputs; number_of_outputs++; }
                if (timeseries.timeseries_number_waterflow_check.Checked) { timeseries_order[6] = number_of_outputs; number_of_outputs++; }
                if (timeseries.timeseries_SDR_check.Checked) { timeseries_order[7] = number_of_outputs; number_of_outputs++; }
                if (timeseries.timeseries_total_average_alt_check.Checked) { timeseries_order[8] = number_of_outputs; number_of_outputs++; }
                if (timeseries.timeseries_total_dep_check.Checked) { timeseries_order[9] = number_of_outputs; number_of_outputs++; }
                if (timeseries.timeseries_total_ero_check.Checked) { timeseries_order[10] = number_of_outputs; number_of_outputs++; }
                if (timeseries.timeseries_total_evap_check.Checked) { timeseries_order[11] = number_of_outputs; number_of_outputs++; }
                if (timeseries.timeseries_total_infil_check.Checked) { timeseries_order[12] = number_of_outputs; number_of_outputs++; }
                if (timeseries.timeseries_total_outflow_check.Checked) { timeseries_order[13] = number_of_outputs; number_of_outputs++; }
                if (timeseries.timeseries_total_rain_check.Checked) { timeseries_order[14] = number_of_outputs; number_of_outputs++; }

                if (timeseries.timeseries_outflow_cells_checkbox.Checked) { timeseries_order[15] = number_of_outputs; number_of_outputs++; }
                if (timeseries.timeseries_sedexport_checkbox.Checked) { timeseries_order[16] = number_of_outputs; number_of_outputs++; }
                if (timeseries.timeseries_sedexport_checkbox.Checked) { timeseries_order[17] = number_of_outputs; number_of_outputs++; }
                if (timeseries.timeseries_sedexport_checkbox.Checked) { timeseries_order[18] = number_of_outputs; number_of_outputs++; }
                if (timeseries.timeseries_sedexport_checkbox.Checked) { timeseries_order[19] = number_of_outputs; number_of_outputs++; }
                if (timeseries.timeseries_sedexport_checkbox.Checked) { timeseries_order[20] = number_of_outputs; number_of_outputs++; }
                if (timeseries.timeseries_sedexport_checkbox.Checked) { timeseries_order[21] = number_of_outputs; number_of_outputs++; }
                if (timeseries.timeseries_sedexport_checkbox.Checked) { timeseries_order[22] = number_of_outputs; number_of_outputs++; }

                if (timeseries.total_phys_weath_checkbox.Checked) { timeseries_order[23] = number_of_outputs; number_of_outputs++; }
                if (timeseries.total_chem_weath_checkbox.Checked) { timeseries_order[24] = number_of_outputs; number_of_outputs++; }
                if (timeseries.total_fine_formed_checkbox.Checked) { timeseries_order[25] = number_of_outputs; number_of_outputs++; }
                if (timeseries.total_fine_eluviated_checkbox.Checked) { timeseries_order[26] = number_of_outputs; number_of_outputs++; }
                if (timeseries.total_mass_bioturbed_checkbox.Checked) { timeseries_order[27] = number_of_outputs; number_of_outputs++; }
                if (timeseries.total_OM_input_checkbox.Checked) { timeseries_order[28] = number_of_outputs; number_of_outputs++; }
                if (timeseries.total_average_soilthickness_checkbox.Checked) { timeseries_order[29] = number_of_outputs; number_of_outputs++; }
                if (timeseries.timeseries_number_soil_thicker_checkbox.Checked) { timeseries_order[30] = number_of_outputs; number_of_outputs++; }
                if (timeseries.timeseries_coarser_checkbox.Checked) { timeseries_order[31] = number_of_outputs; number_of_outputs++; }
                if (timeseries.timeseries_soil_depth_checkbox.Checked) { timeseries_order[32] = number_of_outputs; number_of_outputs++; }
                if (timeseries.timeseries_soil_mass_checkbox.Checked) { timeseries_order[33] = number_of_outputs; number_of_outputs++; }
                Debug.WriteLine("timeseries preparation was really succesful");
            }
            catch { Debug.WriteLine("timeseries preparation was unsuccesful"); }

            //if ((Final_output_checkbox.Checked && t == end_time) || (Regular_output_checkbox.Checked && (t % (int.Parse(Box_years_output.Text)) == 0)))
            //Debug.WriteLine(" successfully ended initialisations  ");
        }

        void initialise_soil_standard()
        {
            double depth_m;
            Debug.WriteLine("initialising soil");
            // At this point, we know the input soildepth at every location (may be zero). 
            // We do not yet know how many layers that corresponds to.
            // If soildepth is not zero, we will calculate the number of layers and assign thicknesses and material to them.
            int soil_layer, texture_class;
            upper_particle_size[0] = Convert.ToDouble(upper_particle_coarse_textbox.Text);
            upper_particle_size[1] = Convert.ToDouble(upper_particle_sand_textbox.Text);
            upper_particle_size[2] = Convert.ToDouble(upper_particle_silt_textbox.Text);
            upper_particle_size[3] = Convert.ToDouble(upper_particle_clay_textbox.Text);
            upper_particle_size[4] = Convert.ToDouble(upper_particle_fine_clay_textbox.Text);
            //calculate bulk density so that we know how much kg of material goes into a layer.  //ART this will go wrong when there are different textures in different locations, but is faster up until that time.
            double coarsefrac = Convert.ToDouble(soildata.coarsebox.Text) / 100;
            double sandfrac = Convert.ToDouble(soildata.sandbox.Text) / 100;
            double siltfrac = Convert.ToDouble(soildata.siltbox.Text) / 100;
            double clayfrac = Convert.ToDouble(soildata.claybox.Text) / 100;
            double fclayfrac = Convert.ToDouble(soildata.fineclaybox.Text) / 100;
            double location_bd;
            for (int row = 0; row < nr; row++)
            {
                for (int col = 0; col < nc; col++)
                {
                    depth_m = 0;
                    if (creep_testing.Checked)
                    {
                        coarsefrac = 0;
                        sandfrac = 1;
                        siltfrac = 0;
                        clayfrac = 0;
                        fclayfrac = 0;

                    }

                    if (soildepth_m[row, col] == 0)
                    {

                        for (soil_layer = 0; soil_layer < max_soil_layers; soil_layer++)
                        {
                            for (texture_class = 0; texture_class < n_texture_classes; texture_class++)
                            {
                                texture_kg[row, col, soil_layer, texture_class] = 0;
                            }
                            young_SOM_kg[row, col, soil_layer] = 0;
                            old_SOM_kg[row, col, soil_layer] = 0;
                            bulkdensity[row, col, soil_layer] = 0;
                            layerthickness_m[row, col, soil_layer] = -1;
                        }
                    }
                    else
                    {

                        //now assign thicknesses and material to layer.
                        double available_soildepth = soildepth_m[row, col];
                        soil_layer = 0;

                        while (available_soildepth > 0)
                        {
                            // 0-50 cm    min 2.5   insteek 5    maximum 10 cm       n=10    bovenste laag geen minimum (sediment HOEFT niet meteen weggemiddeld te worden - pas als nodig)
                            // 50-200 cm  min 10    insteek 15    maximum 50 cm      n=10
                            // daarna     min 50    insteek 100  geen max            n=5
                            // If max_soil_layers is smaller than the sum of the perfect layers in each of the three ' packages' , then we simply make the lowest layer very thick.
                            //if (soil_layer < 10 && soil_layer < max_soil_layers - 1)
                            /*
                            if (soil_layer < 40 && soil_layer < max_soil_layers - 1)
                            {
                                if (available_soildepth > 0.05)
                                {
                                    layerthickness_m[row, col, soil_layer] = 0.05;
                                    available_soildepth -= 0.05;
                                }
                                else
                                {
                                    layerthickness_m[row, col, soil_layer] = available_soildepth;
                                    available_soildepth = 0;
                                }
                            }
                            */
                            if (soil_layer < 10 && soil_layer < max_soil_layers - 1)
                            {
                                if (available_soildepth > 0.05) //
                                {
                                    layerthickness_m[row, col, soil_layer] = 0.05; // 
                                    available_soildepth -= 0.05;
                                }
                                else
                                {
                                    layerthickness_m[row, col, soil_layer] = available_soildepth;
                                    available_soildepth = 0;
                                }
                            }
                            if (soil_layer > 9 && soil_layer < 20 && soil_layer < max_soil_layers - 1)
                            {
                                if (available_soildepth > 0.15) // was 0.25
                                {
                                    layerthickness_m[row, col, soil_layer] = 0.15; // was 0.15
                                    available_soildepth -= 0.15;
                                }
                                else
                                {
                                    layerthickness_m[row, col, soil_layer] = available_soildepth;
                                    available_soildepth = 0;
                                }
                            }
                            if (soil_layer > 19 && soil_layer < max_soil_layers && soil_layer < max_soil_layers - 1) // Rest
                            {
                                if (available_soildepth > 0.5) // was 1
                                {
                                    layerthickness_m[row, col, soil_layer] = 0.5; // was 1
                                    available_soildepth -= 0.5; // was 1
                                }
                                else
                                {
                                    layerthickness_m[row, col, soil_layer] = available_soildepth;
                                    available_soildepth = 0;
                                }
                            }

                            if (soil_layer == max_soil_layers - 1)
                            {
                                layerthickness_m[row, col, soil_layer] = available_soildepth;
                                available_soildepth = 0;
                            }

                            if (layerthickness_m[row, col, soil_layer] != 0)
                            {
                                depth_m += layerthickness_m[row, col, soil_layer] / 2;
                                location_bd = bulk_density_calc_kg_m3(coarsefrac, sandfrac, siltfrac, clayfrac, fclayfrac, 0, 0, depth_m);
                                depth_m += layerthickness_m[row, col, soil_layer] / 2;
                                texture_kg[row, col, soil_layer, 0] = location_bd * layerthickness_m[row, col, soil_layer] * coarsefrac * dx * dx;   //  kg = kg/m3 * m * kg/kg * m * m
                                texture_kg[row, col, soil_layer, 1] = location_bd * layerthickness_m[row, col, soil_layer] * sandfrac * dx * dx;
                                texture_kg[row, col, soil_layer, 2] = location_bd * layerthickness_m[row, col, soil_layer] * siltfrac * dx * dx;
                                texture_kg[row, col, soil_layer, 3] = location_bd * layerthickness_m[row, col, soil_layer] * clayfrac * dx * dx;
                                texture_kg[row, col, soil_layer, 4] = location_bd * layerthickness_m[row, col, soil_layer] * fclayfrac * dx * dx;
                                bulkdensity[row, col, soil_layer] = location_bd;

                            }
                            if (creep_testing.Checked)
                            {
                                sandfrac -= 0.05;
                                clayfrac += 0.05;

                                sandfrac = Math.Max(sandfrac, 0);
                                clayfrac = Math.Min(clayfrac, 1);
                            }

                            soil_layer++;

                        } // end availabke soil depth > 0
                    } // end else 

                } // end col
            } // end row
              // Debug.WriteLine("initialised soil");

        }  //keep this code even when it's unreferenced

        void initialise_soil()
        {
            double depth_m;
            //Debug.WriteLine("initialising soil");
            // At this point, we know the input soildepth at every location (may be zero). 
            // We do not yet know how many layers that corresponds to.
            // If soildepth is not zero, we will calculate the number of layers and assign thicknesses and material to them.
            int soil_layer, texture_class;
            upper_particle_size[0] = Convert.ToDouble(upper_particle_coarse_textbox.Text);
            upper_particle_size[1] = Convert.ToDouble(upper_particle_sand_textbox.Text);
            upper_particle_size[2] = Convert.ToDouble(upper_particle_silt_textbox.Text);
            upper_particle_size[3] = Convert.ToDouble(upper_particle_clay_textbox.Text);
            upper_particle_size[4] = Convert.ToDouble(upper_particle_fine_clay_textbox.Text);
            //calculate bulk density so that we know how much kg of material goes into a layer.  //ART this will go wrong when there are different textures in different locations, but is faster up until that time.
            double coarsefrac = Convert.ToDouble(soildata.coarsebox.Text) / 100;
            double sandfrac = Convert.ToDouble(soildata.sandbox.Text) / 100;
            double siltfrac = Convert.ToDouble(soildata.siltbox.Text) / 100;
            double clayfrac = Convert.ToDouble(soildata.claybox.Text) / 100;
            double fclayfrac = Convert.ToDouble(soildata.fineclaybox.Text) / 100;
            double location_bd;

            for (int row = 0; row < nr; row++)
            {
                for (int col = 0; col < nc; col++)
                {
                    depth_m = 0;
                    if (creep_testing.Checked)
                    {
                        coarsefrac = 0;
                        sandfrac = 1;
                        siltfrac = 0;
                        clayfrac = 0;
                        fclayfrac = 0;

                    }

                    if (soildepth_m[row, col] == 0)
                    {

                        for (soil_layer = 0; soil_layer < max_soil_layers; soil_layer++)
                        {
                            for (texture_class = 0; texture_class < n_texture_classes; texture_class++)
                            {
                                texture_kg[row, col, soil_layer, texture_class] = 0;
                            }
                            young_SOM_kg[row, col, soil_layer] = 0;
                            old_SOM_kg[row, col, soil_layer] = 0;
                            bulkdensity[row, col, soil_layer] = 0;
                            layerthickness_m[row, col, soil_layer] = -1;
                        }
                    }
                    else
                    {
                        //now assign thicknesses and material to layer.
                        double available_soildepth = soildepth_m[row, col];
                        soil_layer = 0;

                        if (checkbox_layer_thickness.Checked) // MvdM if layer thickness is fixed
                        {
                            while (available_soildepth > 0)
                            {
                                if (soil_layer == max_soil_layers - 1)
                                {
                                    layerthickness_m[row, col, soil_layer] = available_soildepth;
                                    available_soildepth = 0;
                                }
                                else
                                {
                                    if (available_soildepth > dz_standard)
                                    {
                                        layerthickness_m[row, col, soil_layer] = dz_standard;
                                        available_soildepth -= dz_standard;
                                    }
                                    else
                                    {
                                        layerthickness_m[row, col, soil_layer] = available_soildepth;
                                        available_soildepth = 0;
                                    }
                                }

                                //now limit layerthicknes to hardlayer limitations if needed
                                if (blocks_active == 1)
                                {
                                    if (dtm[row, col] >= hardlayerelevation_m)
                                    {
                                        double currentdepth = (dtm[row, col] - depth_m - layerthickness_m[row, col, soil_layer]);
                                        if (currentdepth < hardlayerelevation_m && currentdepth > (hardlayerelevation_m - hardlayerthickness_m))
                                        {
                                            layerthickness_m[row, col, soil_layer] = (dtm[row, col] - depth_m) - hardlayerelevation_m;
                                            //Debug.WriteLine(" limited layerthickness and soildepth to account for proximity of hardlayer  in " + row + " " + col);
                                            //Debug.WriteLine(" hardlayerelevation_m " + hardlayerelevation_m + ", " + ((dtm[row, col] - depth_m) - hardlayerelevation_m) + " under the top of this layer");
                                            //Debug.WriteLine(" dtm " + dtm[row, col] + " currentdepth " + currentdepth + " available_soildepth " + available_soildepth + " depth_m " + depth_m);
                                            //Debug.WriteLine(" adapted layerthickness is " + layerthickness_m[row, col, soil_layer]);
                                            available_soildepth = 0;

                                            //this ensures that soils stay thinner on top of hardlayers, and don't continue under them.
                                        }
                                    }
                                }
                                if (layerthickness_m[row, col, soil_layer] != 0)
                                {
                                    depth_m += layerthickness_m[row, col, soil_layer] / 2;
                                    location_bd = bulk_density_calc_kg_m3(coarsefrac, sandfrac, siltfrac, clayfrac, fclayfrac, 0, 0, depth_m);
                                    depth_m += layerthickness_m[row, col, soil_layer] / 2;
                                    texture_kg[row, col, soil_layer, 0] = location_bd * layerthickness_m[row, col, soil_layer] * coarsefrac * dx * dx;   //  kg = kg/m3 * m * kg/kg * m * m
                                    texture_kg[row, col, soil_layer, 1] = location_bd * layerthickness_m[row, col, soil_layer] * sandfrac * dx * dx;
                                    texture_kg[row, col, soil_layer, 2] = location_bd * layerthickness_m[row, col, soil_layer] * siltfrac * dx * dx;
                                    texture_kg[row, col, soil_layer, 3] = location_bd * layerthickness_m[row, col, soil_layer] * clayfrac * dx * dx;
                                    texture_kg[row, col, soil_layer, 4] = location_bd * layerthickness_m[row, col, soil_layer] * fclayfrac * dx * dx;
                                    bulkdensity[row, col, soil_layer] = location_bd;

                                }
                                if (creep_testing.Checked)
                                {
                                    //bool change_creep_testing = true;
                                    //if(max_soil_layers>10 & soil_layer<10)
                                    //{
                                    //    change_creep_testing = false;
                                    //}
                                    //if (change_creep_testing) 
                                    //{
                                    //    sandfrac = 0;
                                    //    clayfrac = 1;
                                    //}
                                    sandfrac -= 1.0 / max_soil_layers;
                                    clayfrac += 1.0 / max_soil_layers;
                                    sandfrac = Math.Max(sandfrac, 0);
                                    clayfrac = Math.Min(clayfrac, 1);
                                }
                                soil_layer++;
                            } // end available soil depth > 0
                        } // end if fixed layer thickness
                        else
                        { // start variable layer thickness
                            while (available_soildepth > 0)
                            {
                                // 0-50 cm    min 2.5   insteek 5    maximum 10 cm       n=10    bovenste laag geen minimum (sediment HOEFT niet meteen weggemiddeld te worden - pas als nodig)
                                // 50-200 cm  min 10    insteek 15    maximum 50 cm      n=10
                                // daarna     min 50    insteek 100  geen max            n=5
                                // If max_soil_layers is smaller than the sum of the perfect layers in each of the three ' packages' , then we simply make the lowest layer very thick.
                                //if (soil_layer < 10 && soil_layer < max_soil_layers - 1)
                                /*
                                if (soil_layer < 40 && soil_layer < max_soil_layers - 1)
                                {
                                    if (available_soildepth > 0.05)
                                    {
                                        layerthickness_m[row, col, soil_layer] = 0.05;
                                        available_soildepth -= 0.05;
                                    }
                                    else
                                    {
                                        layerthickness_m[row, col, soil_layer] = available_soildepth;
                                        available_soildepth = 0;
                                    }
                                }
                                */
                                if (soil_layer < 10 && soil_layer < max_soil_layers - 1)
                                {
                                    if (available_soildepth > 0.05) //
                                    {
                                        layerthickness_m[row, col, soil_layer] = 0.05; // 
                                        available_soildepth -= 0.05;
                                    }
                                    else
                                    {
                                        layerthickness_m[row, col, soil_layer] = available_soildepth;
                                        available_soildepth = 0;
                                    }
                                }
                                if (soil_layer > 9 && soil_layer < 20 && soil_layer < max_soil_layers - 1)
                                {
                                    if (available_soildepth > 0.15) // was 0.25
                                    {
                                        layerthickness_m[row, col, soil_layer] = 0.15; // was 0.15
                                        available_soildepth -= 0.15;
                                    }
                                    else
                                    {
                                        layerthickness_m[row, col, soil_layer] = available_soildepth;
                                        available_soildepth = 0;
                                    }
                                }
                                if (soil_layer > 19 && soil_layer < max_soil_layers && soil_layer < max_soil_layers - 1) // Rest
                                {
                                    if (available_soildepth > 0.5) // was 1
                                    {
                                        layerthickness_m[row, col, soil_layer] = 0.5; // was 1
                                        available_soildepth -= 0.5; // was 1
                                    }
                                    else
                                    {
                                        layerthickness_m[row, col, soil_layer] = available_soildepth;
                                        available_soildepth = 0;
                                    }
                                }

                                if (soil_layer == max_soil_layers - 1)
                                {
                                    layerthickness_m[row, col, soil_layer] = available_soildepth;
                                    available_soildepth = 0;
                                }

                                if (layerthickness_m[row, col, soil_layer] != 0)
                                {
                                    depth_m += layerthickness_m[row, col, soil_layer] / 2;
                                    location_bd = bulk_density_calc_kg_m3(coarsefrac, sandfrac, siltfrac, clayfrac, fclayfrac, 0, 0, depth_m);
                                    depth_m += layerthickness_m[row, col, soil_layer] / 2;
                                    texture_kg[row, col, soil_layer, 0] = location_bd * layerthickness_m[row, col, soil_layer] * coarsefrac * dx * dx;   //  kg = kg/m3 * m * kg/kg * m * m
                                    texture_kg[row, col, soil_layer, 1] = location_bd * layerthickness_m[row, col, soil_layer] * sandfrac * dx * dx;
                                    texture_kg[row, col, soil_layer, 2] = location_bd * layerthickness_m[row, col, soil_layer] * siltfrac * dx * dx;
                                    texture_kg[row, col, soil_layer, 3] = location_bd * layerthickness_m[row, col, soil_layer] * clayfrac * dx * dx;
                                    texture_kg[row, col, soil_layer, 4] = location_bd * layerthickness_m[row, col, soil_layer] * fclayfrac * dx * dx;
                                    bulkdensity[row, col, soil_layer] = location_bd;

                                }
                                if (creep_testing.Checked)
                                {
                                    sandfrac -= 0.05;
                                    clayfrac += 0.05;

                                    sandfrac = Math.Max(sandfrac, 0);
                                    clayfrac = Math.Min(clayfrac, 1);
                                }

                                soil_layer++;

                            } // end availabke soil depth > 0
                        }    // end variable layer thickness              
                    } // end else 
                    soildepth_m[row, col] = total_soil_thickness(row, col);

                } // end col
            } // end row
              //Debug.WriteLine("initialised soil");
            if (OSL_checkbox.Checked)
            {
                for (int row = 0; row < nr; row++)
                {
                    for (int col = 0; col < nc; col++)
                    {
                        for (int lay = 0; lay < max_soil_layers; lay++)
                        {
                            int ngrains_layer = Convert.ToInt32(Math.Round(ngrains_kgsand_m2 * texture_kg[row, col, lay, 1])); // grains per kg/m2 of sand
                            OSL_grainages[row, col, lay] = new int[ngrains_layer];
                            OSL_depositionages[row, col, lay] = new int[ngrains_layer];
                            OSL_surfacedcount[row, col, lay] = new int[ngrains_layer];

                            for (int grain = 0; grain < ngrains_layer; grain++)
                            {
                                OSL_grainages[row, col, lay][grain] = start_age;
                                OSL_depositionages[row, col, lay][grain] = start_age;
                            }
                        }
                    }
                }
            }

        } // adapted for standard thickness

        void initialise_every_till()
        {
            if (check_time_till_fields.Checked && check_space_till_fields.Checked == false)
            {
                for (int row = 0; row < nr; row++)
                {
                    for (int col = 0; col < nc; col++)
                    {
                        tillfields[row, col] = 1 * till_record[t];
                    }
                }

            }
        }

        void initialise_every()       //fills the inputgrids with values
        {
            int corrected_t;
            for (int row = 0; row < nr; row++)
            {
                for (int col = 0; col < nc; col++)
                {
                    // time runs from 1 to end_time - compensate for that when taking values from records
                    // also compensate for records shorter than end_time
                    if (check_time_rain.Checked)
                    {
                        corrected_t = t;
                        while (corrected_t > rainfall_record.Length) { corrected_t -= rainfall_record.Length; }

                        rain_value_m = 0.001 * rainfall_record[corrected_t]; //from mm (in record) to m (LORICA)   
                                                                             // mvdm -1 weggehaald van corrected_t, leidde tot OutOfRange errors
                                                                             // changed rain[row, col] to rain_value_m, due to errors, this is not spatial, but temporal variation
                                                                             //this should be improved for when rainfall is not also spatially variable //ArT
                    }
                    if (check_time_infil.Checked)
                    {
                        corrected_t = t;
                        while (corrected_t > infil_record.Length) { corrected_t -= infil_record.Length; }
                        infil_value_m = 0.001 * infil_record[corrected_t];
                    }
                    if (check_time_evap.Checked)
                    {
                        corrected_t = t;
                        while (corrected_t > evap_record.Length) { corrected_t -= evap_record.Length; }
                        evap_value_m = 0.001 * evap_record[corrected_t];
                    }

                    if (check_time_T.Checked)
                    {
                        corrected_t = t;
                        while (corrected_t > temp_record.Length) { corrected_t -= temp_record.Length; }

                        rain_value_m = 0.001 * temp_record[corrected_t]; //from mm (in record) to m (LORICA)   // mvdm -1 weggehaald van corrected_t, leidde tot OutOfRange errors
                        temp_value_C = temp_record[corrected_t];
                        // changed rain[row, col] to rain_value_m, due to errors, this is not spatial, but temporal variation
                        //this should be improved for when rainfall is not also spatially variable //ArT
                    }

                    if (annual_output_checkbox.Checked)
                    {
                        dz_soil[row, col] = 0;
                        if (Creep_Checkbox.Checked) { sum_creep_grid[row, col] = 0; creep[row, col] = 0; }
                        if (treefall_checkbox.Checked) { dz_treefall[row, col] = 0; treefall_count[row, col] = 0; }
                        if (Water_ero_checkbox.Checked) { sum_water_erosion[row, col] = 0; }
                        if (Biological_weathering_checkbox.Checked) { sum_biological_weathering[row, col] = 0; }
                        if (Frost_weathering_checkbox.Checked) { sum_frost_weathering[row, col] = 0; }
                        if (Tillage_checkbox.Checked) { sum_tillage[row, col] = 0; total_sum_tillage = 0; }
                        if (soildepth_m[row, col] < 0.0) { soildepth_error += soildepth_m[row, col]; soildepth_m[row, col] = 0; }
                    }
                    if (soildepth_m[row, col] < 0.0) { soildepth_error += soildepth_m[row, col]; soildepth_m[row, col] = 0; }
                    if (Water_ero_checkbox.Checked) { waterflow_m3[row, col] = 0.0; }

                } //for
            } //for

            if (fill_sinks_during_checkbox.Checked)
            {
                findsinks();
                if (numberofsinks > 0)
                {
                    searchdepressions();
                    //define_fillheight_new();
                    for (int row = 0; row < nr; row++)
                    {
                        for (int col = 0; col < nc; col++)
                        {
                            if (dtm[row, col] < dtmfill_A[row, col] && dtm[row, col] != -9999)
                            {
                                dtmchange_m[row, col] += dtmfill_A[row, col] - dtm[row, col];
                                dtm[row, col] = dtmfill_A[row, col];
                            }
                        }
                    }
                }
            }
            //Debug.WriteLine("initialised every");
        }

        #endregion
    }
}
