using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

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
                            if (dtm[row, col] != nodata_value)
                            {
                            veg_correction_factor[row, col] = 1;
                            vegetation_type[row, col] = 0; 
                            }
                                
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

                if (Proglacial_checkbox.Checked)
                {
                    filename = this.proglacial_input_filename_textbox.Text;
                    read_integer(filename, age_rast_yr);
                    Debug.WriteLine("read age raster");

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
                        if (dtm[row, col] != nodata_value)
                        {
                            dz_soil[row, col] = 0;
                            if (Creep_Checkbox.Checked) { sum_creep_grid[row, col] = 0; creep[row, col] = 0; }
                            if (Water_ero_checkbox.Checked && only_waterflow_checkbox.Checked == false) { sum_water_erosion[row, col] = 0; total_sed_export = 0; }
                            if (treefall_checkbox.Checked) { dz_treefall[row, col] = 0; treefall_count[row, col] = 0; }
                            if (Biological_weathering_checkbox.Checked) 
                            {
                                bedrock_weathering_m[row, col] = 0; //AleG
                                sum_biological_weathering[row, col] = 0; 
                            }
                            if (Frost_weathering_checkbox.Checked) { sum_frost_weathering[row, col] = 0; }
                            if (Tillage_checkbox.Checked) { sum_tillage[row, col] = 0; total_sum_tillage = 0; dz_till_bd[row, col] = 0; }
                            if (Landslide_checkbox.Checked) //AleG
                            {
                                crrain_m_d[row, col] = 0;
                                ero_slid_m[row, col] = 0;
                                old_SOM_in_transport_kg[row, col] = 0;
                                remaining_vertical_size_m[row, col] = 0;
                                sat_bd_kg_m3[row, col] = 0;
                                sed_slid_m[row, col] = 0;
                                slidestatus[row, col] = 0;
                                young_SOM_in_transport_kg[row, col] = 0;
                                for (int material = 0; material < 5; material++)
                                {
                                    sediment_in_transport_kg[row, col, material] = 0;
                                }
                                //sum_landsliding[row, col] = 0;
                            }
                            if (soildepth_m[row, col] < 0.0 && soildepth_m[row, col] != nodata_value) { soildepth_error += soildepth_m[row, col]; soildepth_m[row, col] = 2; }
                            if (uplift_active_checkbox.Checked) { sum_uplift[row, col] = 0; total_sum_uplift = 0; }
                            if (tilting_active_checkbox.Checked) { sum_tilting[row, col] = 0; total_sum_tilting = 0; }
                            if (check_space_soildepth.Checked != true) { soildepth_m[row, col] = soildepth_value; }
                            if (landslide_active == true) { root_cohesion_kPa_new[row, col] =1; } //AleG
                            if (check_space_till_fields.Checked != true && Tillage_checkbox.Checked)
                            {
                                tillfields[row, col] = 1;

                            }

                            if (Water_ero_checkbox.Checked && only_waterflow_checkbox.Checked == false)
                            {
                                K_fac[row, col] = advection_erodibility;
                            } //WVG K_fac matrix initialisation is needed when landuse is disabled

                            if (check_space_landuse.Checked == true)
                            {



                                //currently, this will throw an exception if landuse is actually spatial //development required //ArT
                                if (landuse[row, col] == 1)
                                {
                                    infil[row, col] *= System.Convert.ToDouble(landuse_determinator.LU1_Inf_textbox.Text);
                                    evapotranspiration[row, col] *= System.Convert.ToDouble(landuse_determinator.LU1_Evap_textbox.Text);
                                    K_fac[row, col] *= System.Convert.ToDouble(landuse_determinator.LU1_Ero_textbox.Text); //AleG
                                    root_cohesion_kPa_new[row, col] = System.Convert.ToDouble(landuse_determinator.LU1_RootC_textbox.Text);//AleG
                                }
                                if (landuse[row, col] == 2)
                                {
                                    infil[row, col] *= System.Convert.ToDouble(landuse_determinator.LU2_Inf_textbox.Text);
                                    evapotranspiration[row, col] *= System.Convert.ToDouble(landuse_determinator.LU2_Evap_textbox.Text);
                                    K_fac[row, col] *= System.Convert.ToDouble(landuse_determinator.LU2_Ero_textbox.Text);
                                    root_cohesion_kPa_new[row, col] = System.Convert.ToDouble(landuse_determinator.LU2_RootC_textbox.Text);
                                }
                                if (landuse[row, col] == 3)
                                {
                                    infil[row, col] *= System.Convert.ToDouble(landuse_determinator.LU3_Inf_textbox.Text);
                                    evapotranspiration[row, col] *= System.Convert.ToDouble(landuse_determinator.LU3_Evap_textbox.Text);
                                    K_fac[row, col] *= System.Convert.ToDouble(landuse_determinator.LU3_Ero_textbox.Text);
                                    root_cohesion_kPa_new[row, col] = System.Convert.ToDouble(landuse_determinator.LU3_RootC_textbox.Text);
                                }
                                if (landuse[row, col] == 4)
                                {
                                    infil[row, col] *= System.Convert.ToDouble(landuse_determinator.LU4_Inf_textbox.Text);
                                    evapotranspiration[row, col] *= System.Convert.ToDouble(landuse_determinator.LU4_Evap_textbox.Text);
                                    K_fac[row, col] *= System.Convert.ToDouble(landuse_determinator.LU4_Ero_textbox.Text);
                                    root_cohesion_kPa_new[row, col] = System.Convert.ToDouble(landuse_determinator.LU4_RootC_textbox.Text);
                                }
                                if (landuse[row, col] == 5)
                                {
                                    infil[row, col] *= System.Convert.ToDouble(landuse_determinator.LU5_Inf_textbox.Text);
                                    evapotranspiration[row, col] *= System.Convert.ToDouble(landuse_determinator.LU5_Evap_textbox.Text);
                                    K_fac[row, col] *= System.Convert.ToDouble(landuse_determinator.LU5_Ero_textbox.Text);
                                    root_cohesion_kPa_new[row, col] = System.Convert.ToDouble(landuse_determinator.LU5_RootC_textbox.Text);
                                }
                                if (landuse[row, col] == 6)
                                {
                                    infil[row, col] *= System.Convert.ToDouble(landuse_determinator.LU6_Inf_textbox.Text);
                                    evapotranspiration[row, col] *= System.Convert.ToDouble(landuse_determinator.LU6_Evap_textbox.Text);
                                    K_fac[row, col] *= System.Convert.ToDouble(landuse_determinator.LU6_Ero_textbox.Text);
                                    root_cohesion_kPa_new[row, col] = System.Convert.ToDouble(landuse_determinator.LU6_RootC_textbox.Text);
                                }
                                if (landuse[row, col] == 7)
                                {
                                    infil[row, col] *= System.Convert.ToDouble(landuse_determinator.LU7_Inf_textbox.Text);
                                    evapotranspiration[row, col] *= System.Convert.ToDouble(landuse_determinator.LU7_Evap_textbox.Text);
                                    K_fac[row, col] *= System.Convert.ToDouble(landuse_determinator.LU7_Ero_textbox.Text);
                                    root_cohesion_kPa_new[row, col] = System.Convert.ToDouble(landuse_determinator.LU7_RootC_textbox.Text);
                                }
                                if (landuse[row, col] == 8)
                                {
                                    infil[row, col] *= System.Convert.ToDouble(landuse_determinator.LU8_Inf_textbox.Text);
                                    evapotranspiration[row, col] *= System.Convert.ToDouble(landuse_determinator.LU8_Evap_textbox.Text);
                                    K_fac[row, col] *= System.Convert.ToDouble(landuse_determinator.LU8_Ero_textbox.Text);
                                    root_cohesion_kPa_new[row, col] = System.Convert.ToDouble(landuse_determinator.LU8_RootC_textbox.Text);
                                }
                                if (landuse[row, col] == 9)
                                {
                                    infil[row, col] *= System.Convert.ToDouble(landuse_determinator.LU9_Inf_textbox.Text);
                                    evapotranspiration[row, col] *= System.Convert.ToDouble(landuse_determinator.LU9_Evap_textbox.Text);
                                    K_fac[row, col] *= System.Convert.ToDouble(landuse_determinator.LU9_Ero_textbox.Text);
                                    root_cohesion_kPa_new[row, col] = System.Convert.ToDouble(landuse_determinator.LU9_RootC_textbox.Text);
                                }
                                if (landuse[row, col] == 10)
                                {
                                    infil[row, col] *= System.Convert.ToDouble(landuse_determinator.LU10_Inf_textbox.Text);
                                    evapotranspiration[row, col] *= System.Convert.ToDouble(landuse_determinator.LU10_Evap_textbox.Text);
                                    K_fac[row, col] *= System.Convert.ToDouble(landuse_determinator.LU10_Ero_textbox.Text);
                                    root_cohesion_kPa_new[row, col] = System.Convert.ToDouble(landuse_determinator.LU10_RootC_textbox.Text);
                                }
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
                                if (dtm[row, col] < dtmfill_A[row, col] && dtm[row, col] != nodata_value)
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
                        if (dtm[row, col] != nodata_value) {
                            dz_soil[row, col] = 0;
                            if (Creep_Checkbox.Checked) { sum_creep_grid[row, col] = 0; creep[row, col] = 0; }
                            if (Water_ero_checkbox.Checked && only_waterflow_checkbox.Checked == false) { sum_water_erosion[row, col] = 0; total_sed_export = 0; }
                            if (treefall_checkbox.Checked) { dz_treefall[row, col] = 0; treefall_count[row, col] = 0; }
                            if (Biological_weathering_checkbox.Checked) 
                            {
                                bedrock_weathering_m[row, col] = 0; //AleG
                                sum_biological_weathering[row, col] = 0;
                            }
                            if (Frost_weathering_checkbox.Checked) { sum_frost_weathering[row, col] = 0; }
                            if (Tillage_checkbox.Checked) { sum_tillage[row, col] = 0; total_sum_tillage = 0; dz_till_bd[row, col] = 0; }
                            if (Landslide_checkbox.Checked) //AleG
                            {
                                crrain_m_d[row, col] = 0;
                                ero_slid_m[row, col] = 0;
                                old_SOM_in_transport_kg[row, col] = 0;
                                remaining_vertical_size_m[row, col] = 0;
                                sat_bd_kg_m3[row, col] = 0;
                                sed_slid_m[row, col] = 0;
                                slidestatus[row, col] = 0;
                                young_SOM_in_transport_kg[row, col] = 0;
                                for (int material = 0; material < 5; material++)
                                {
                                    sediment_in_transport_kg[row, col, material] = 0;
                                }

                                //sum_landsliding[row, col] = 0; 
                            } //AleG total_sum_tillage = 0;
                            if (soildepth_m[row, col] < 0.0 && soildepth_m[row, col] != nodata_value) { soildepth_error += soildepth_m[row, col]; soildepth_m[row, col] = 2; }
                            if (uplift_active_checkbox.Checked) { sum_uplift[row, col] = 0; total_sum_uplift = 0; }
                            if (tilting_active_checkbox.Checked) { sum_tilting[row, col] = 0; total_sum_tilting = 0; }
                            if (check_space_soildepth.Checked != true) { soildepth_m[row, col] = soildepth_value; }
                            if (check_space_till_fields.Checked != true && Tillage_checkbox.Checked)
                            {
                                tillfields[row, col] = 1;

                            }

                            if (Water_ero_checkbox.Checked && only_waterflow_checkbox.Checked == false)
                            {
                                K_fac[row, col] = advection_erodibility;
                            } //WVG K_fac matrix initialisation is needed when landuse is disabled

                            if (Water_ero_checkbox.Checked && check_space_landuse.Checked == true)
                            {
                                //currently, this will throw an exception if landuse is actually spatial //development required //ArT
                                if (landuse[row, col] == 1)
                                {
                                    infil[row, col] *= System.Convert.ToDouble(landuse_determinator.LU1_Inf_textbox.Text);
                                    evapotranspiration[row, col] *= System.Convert.ToDouble(landuse_determinator.LU1_Evap_textbox.Text);
                                    K_fac[row, col] *= System.Convert.ToDouble(landuse_determinator.LU1_Ero_textbox.Text); //AleG
                                    root_cohesion_kPa_new[row, col] = System.Convert.ToDouble(landuse_determinator.LU1_RootC_textbox.Text);//AleG
                                }
                                if (landuse[row, col] == 2)
                                {
                                    infil[row, col] *= System.Convert.ToDouble(landuse_determinator.LU2_Inf_textbox.Text);
                                    evapotranspiration[row, col] *= System.Convert.ToDouble(landuse_determinator.LU2_Evap_textbox.Text);
                                    K_fac[row, col] *= System.Convert.ToDouble(landuse_determinator.LU2_Ero_textbox.Text);
                                    root_cohesion_kPa_new[row, col] = System.Convert.ToDouble(landuse_determinator.LU2_RootC_textbox.Text);
                                }
                                if (landuse[row, col] == 3)
                                {
                                    infil[row, col] *= System.Convert.ToDouble(landuse_determinator.LU3_Inf_textbox.Text);
                                    evapotranspiration[row, col] *= System.Convert.ToDouble(landuse_determinator.LU3_Evap_textbox.Text);
                                    K_fac[row, col] *= System.Convert.ToDouble(landuse_determinator.LU3_Ero_textbox.Text);
                                    root_cohesion_kPa_new[row, col] *= System.Convert.ToDouble(landuse_determinator.LU3_RootC_textbox.Text);
                                }
                                if (landuse[row, col] == 4)
                                {
                                    infil[row, col] *= System.Convert.ToDouble(landuse_determinator.LU4_Inf_textbox.Text);
                                    evapotranspiration[row, col] *= System.Convert.ToDouble(landuse_determinator.LU4_Evap_textbox.Text);
                                    K_fac[row, col] *= System.Convert.ToDouble(landuse_determinator.LU4_Ero_textbox.Text);
                                    root_cohesion_kPa_new[row, col] = System.Convert.ToDouble(landuse_determinator.LU4_RootC_textbox.Text);
                                }
                                if (landuse[row, col] == 5)
                                {
                                    infil[row, col] *= System.Convert.ToDouble(landuse_determinator.LU5_Inf_textbox.Text);
                                    evapotranspiration[row, col] *= System.Convert.ToDouble(landuse_determinator.LU5_Evap_textbox.Text);
                                    K_fac[row, col] *= System.Convert.ToDouble(landuse_determinator.LU5_Ero_textbox.Text);
                                    root_cohesion_kPa_new[row, col] *= System.Convert.ToDouble(landuse_determinator.LU5_RootC_textbox.Text);
                                }
                                if (landuse[row, col] == 6)
                                {
                                    infil[row, col] *= System.Convert.ToDouble(landuse_determinator.LU6_Inf_textbox.Text);
                                    evapotranspiration[row, col] *= System.Convert.ToDouble(landuse_determinator.LU6_Evap_textbox.Text);
                                    K_fac[row, col] *= System.Convert.ToDouble(landuse_determinator.LU6_Ero_textbox.Text);
                                    root_cohesion_kPa_new[row, col] = System.Convert.ToDouble(landuse_determinator.LU6_RootC_textbox.Text);
                                }
                                if (landuse[row, col] == 7)
                                {
                                    infil[row, col] *= System.Convert.ToDouble(landuse_determinator.LU7_Inf_textbox.Text);
                                    evapotranspiration[row, col] *= System.Convert.ToDouble(landuse_determinator.LU7_Evap_textbox.Text);
                                    K_fac[row, col] *= System.Convert.ToDouble(landuse_determinator.LU7_Ero_textbox.Text);
                                    root_cohesion_kPa_new[row, col] *= System.Convert.ToDouble(landuse_determinator.LU7_RootC_textbox.Text);
                                }
                                if (landuse[row, col] == 8)
                                {
                                    infil[row, col] *= System.Convert.ToDouble(landuse_determinator.LU8_Inf_textbox.Text);
                                    evapotranspiration[row, col] *= System.Convert.ToDouble(landuse_determinator.LU8_Evap_textbox.Text);
                                    K_fac[row, col] *= System.Convert.ToDouble(landuse_determinator.LU8_Ero_textbox.Text);
                                    root_cohesion_kPa_new[row, col] = System.Convert.ToDouble(landuse_determinator.LU8_RootC_textbox.Text);
                                }
                                if (landuse[row, col] == 9)
                                {
                                    infil[row, col] *= System.Convert.ToDouble(landuse_determinator.LU9_Inf_textbox.Text);
                                    evapotranspiration[row, col] *= System.Convert.ToDouble(landuse_determinator.LU9_Evap_textbox.Text);
                                    K_fac[row, col] *= System.Convert.ToDouble(landuse_determinator.LU9_Ero_textbox.Text);
                                    root_cohesion_kPa_new[row, col] = System.Convert.ToDouble(landuse_determinator.LU9_RootC_textbox.Text);
                                }
                                if (landuse[row, col] == 10)
                                {
                                    infil[row, col] *= System.Convert.ToDouble(landuse_determinator.LU10_Inf_textbox.Text);
                                    evapotranspiration[row, col] *= System.Convert.ToDouble(landuse_determinator.LU10_Evap_textbox.Text);
                                    K_fac[row, col] *= System.Convert.ToDouble(landuse_determinator.LU10_Ero_textbox.Text);
                                    root_cohesion_kPa_new[row, col] = System.Convert.ToDouble(landuse_determinator.LU10_RootC_textbox.Text); 
                                }
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
                                if (dtm[row, col] < dtmfill_A[row, col] && dtm[row, col] != nodata_value)
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
            upper_particle_size[0] = Convert.ToDouble(soildata.upper_particle_coarse_textbox.Text);
            upper_particle_size[1] = Convert.ToDouble(soildata.upper_particle_sand_textbox.Text);
            upper_particle_size[2] = Convert.ToDouble(soildata.upper_particle_silt_textbox.Text);
            upper_particle_size[3] = Convert.ToDouble(soildata.upper_particle_clay_textbox.Text);
            upper_particle_size[4] = Convert.ToDouble(soildata.upper_particle_fine_clay_textbox.Text);
            //calculate bulk density so that we know how much kg of material goes into a layer.  //ART this will go wrong when there are different textures in different locations, but is faster up until that time.
            double coarsefrac = Convert.ToDouble(soildata.coarsebox.Text) / 100;
            double sandfrac = Convert.ToDouble(soildata.sandbox.Text) / 100;
            double siltfrac = Convert.ToDouble(soildata.siltbox.Text) / 100;
            double clayfrac = Convert.ToDouble(soildata.claybox.Text) / 100;
            double fclayfrac = Convert.ToDouble(soildata.fineclaybox.Text) / 100;
            double yomfrac = Convert.ToDouble(soildata.yombox.Text) / 100; //AleG
            double oomfrac = Convert.ToDouble(soildata.oombox.Text) / 100; //AleG
            double location_bd;
            for (int row = 0; row < nr; row++)
            {
                for (int col = 0; col < nc; col++)
                {
                    if (dtm[row, col] != nodata_value)//AleG
                    {
                        depth_m = 0;
                        if (creep_testing.Checked)
                        {
                            coarsefrac = 0;
                            sandfrac = 1;
                            siltfrac = 0;
                            clayfrac = 0;
                            fclayfrac = 0;
                            yomfrac = 0; //AleG
                            oomfrac = 0; //AleG

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
                                    location_bd = bulk_density_calc_kg_m3(coarsefrac, sandfrac, siltfrac, clayfrac, fclayfrac, yomfrac, oomfrac, depth_m); //AleG
                                    depth_m += layerthickness_m[row, col, soil_layer] / 2;
                                    texture_kg[row, col, soil_layer, 0] = location_bd * layerthickness_m[row, col, soil_layer] * coarsefrac * dx * dx;   //  kg = kg/m3 * m * kg/kg * m * m
                                    texture_kg[row, col, soil_layer, 1] = location_bd * layerthickness_m[row, col, soil_layer] * sandfrac * dx * dx;
                                    texture_kg[row, col, soil_layer, 2] = location_bd * layerthickness_m[row, col, soil_layer] * siltfrac * dx * dx;
                                    texture_kg[row, col, soil_layer, 3] = location_bd * layerthickness_m[row, col, soil_layer] * clayfrac * dx * dx;
                                    texture_kg[row, col, soil_layer, 4] = location_bd * layerthickness_m[row, col, soil_layer] * fclayfrac * dx * dx;
                                    young_SOM_kg[row, col, soil_layer] = location_bd * layerthickness_m[row, col, soil_layer] * yomfrac * dx * dx; //AleG 
                                    old_SOM_kg[row, col, soil_layer] = location_bd * layerthickness_m[row, col, soil_layer] * oomfrac * dx * dx; //AleG 
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
                    }


                } // end col
            } // end row
              // Debug.WriteLine("initialised soil");

        }  //keep this code even when it's unreferenced


        void initialise_soil()
        {
            double depth_m, z_layer_m;
            //Debug.WriteLine("initialising soil");
            // At this point, we know the input soildepth at every location (may be zero). 
            // We do not yet know how many layers that corresponds to.
            // If soildepth is not zero, we will calculate the number of layers and assign thicknesses and material to them.
            int soil_layer, texture_class;
            upper_particle_size[0] = Convert.ToDouble(soildata.upper_particle_coarse_textbox.Text);
            upper_particle_size[1] = Convert.ToDouble(soildata.upper_particle_sand_textbox.Text);
            upper_particle_size[2] = Convert.ToDouble(soildata.upper_particle_silt_textbox.Text);
            upper_particle_size[3] = Convert.ToDouble(soildata.upper_particle_clay_textbox.Text);
            upper_particle_size[4] = Convert.ToDouble(soildata.upper_particle_fine_clay_textbox.Text);
            //calculate bulk density so that we know how much kg of material goes into a layer.  //ART this will go wrong when there are different textures in different locations, but is faster up until that time.
            double coarsefrac = Convert.ToDouble(soildata.coarsebox.Text) / 100;
            double sandfrac = Convert.ToDouble(soildata.sandbox.Text) / 100;
            double siltfrac = Convert.ToDouble(soildata.siltbox.Text) / 100;
            double clayfrac = Convert.ToDouble(soildata.claybox.Text) / 100;
            double fclayfrac = Convert.ToDouble(soildata.fineclaybox.Text) / 100;
            double yomfrac = Convert.ToDouble(soildata.yombox.Text) / 100; //AleG
            double oomfrac = Convert.ToDouble(soildata.oombox.Text) / 100; //AleG
            double location_bd;

            for (int row = 0; row < nr; row++)
            {
                for (int col = 0; col < nc; col++)
                {
                    if (dtm[row, col] != nodata_value)
                    {
                        depth_m = 0;
                        if (creep_testing.Checked)
                        {
                            coarsefrac = 0;
                            sandfrac = 1;
                            siltfrac = 0;
                            clayfrac = 0;
                            fclayfrac = 0;
                            yomfrac = 0; //AleG
                            oomfrac = 0; //AleG

                        }
                        // one algorithm for layer thickness. Increasing layer thickness possible through the layer_z_increase factor MvdM
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
                                if (soil_layer == max_soil_layers - 1)
                                {
                                    layerthickness_m[row, col, soil_layer] = available_soildepth;
                                    available_soildepth = 0;
                                }
                                else
                                {
                                    // calculate and assign layer thickness
                                    z_layer_m = layer_z_surface * Math.Pow(layer_z_increase, soil_layer);

                                    if (OSL_checkbox.Checked & soil_layer == 0)
                                    {
                                        // set first layer thickness to bleaching depth, when OSl particles are traced
                                        z_layer_m = bleaching_depth_m;
                                    }

                                    if (available_soildepth > z_layer_m)
                                    {
                                        layerthickness_m[row, col, soil_layer] = z_layer_m;
                                        available_soildepth -= z_layer_m;
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
                                if (layerthickness_m[row, col, soil_layer] > 0)
                                {
                                    depth_m += layerthickness_m[row, col, soil_layer] / 2;
                                    location_bd = bulk_density_calc_kg_m3(coarsefrac, sandfrac, siltfrac, clayfrac, fclayfrac, yomfrac, oomfrac, depth_m); //AleG
                                    depth_m += layerthickness_m[row, col, soil_layer] / 2;
                                    texture_kg[row, col, soil_layer, 0] = location_bd * layerthickness_m[row, col, soil_layer] * coarsefrac * dx * dx;   //  kg = kg/m3 * m * kg/kg * m * m
                                    texture_kg[row, col, soil_layer, 1] = location_bd * layerthickness_m[row, col, soil_layer] * sandfrac * dx * dx;
                                    texture_kg[row, col, soil_layer, 2] = location_bd * layerthickness_m[row, col, soil_layer] * siltfrac * dx * dx;
                                    texture_kg[row, col, soil_layer, 3] = location_bd * layerthickness_m[row, col, soil_layer] * clayfrac * dx * dx;
                                    texture_kg[row, col, soil_layer, 4] = location_bd * layerthickness_m[row, col, soil_layer] * fclayfrac * dx * dx;
                                    young_SOM_kg[row, col, soil_layer] = location_bd * layerthickness_m[row, col, soil_layer] * yomfrac * dx * dx; //AleG 
                                    old_SOM_kg[row, col, soil_layer] = location_bd * layerthickness_m[row, col, soil_layer] * oomfrac * dx * dx; //AleG 
                                    bulkdensity[row, col, soil_layer] = location_bd;

                                }
                                if (creep_testing.Checked)
                                {
                                    sandfrac -= 1.0 / max_soil_layers;
                                    clayfrac += 1.0 / max_soil_layers;
                                    sandfrac = Math.Max(sandfrac, 0);
                                    clayfrac = Math.Min(clayfrac, 1);
                                }

                                if (OSL_checkbox.Checked)
                                {
                                    int ngrains_layer = Convert.ToInt32(Math.Round(ngrains_kgsand_m2 * texture_kg[row, col, soil_layer, 1])); // grains per kg/m2 of sand
                                    OSL_grainages[row, col, soil_layer] = new int[ngrains_layer];
                                    OSL_depositionages[row, col, soil_layer] = new int[ngrains_layer];
                                    OSL_surfacedcount[row, col, soil_layer] = new int[ngrains_layer];

                                    for (int grain = 0; grain < ngrains_layer; grain++)
                                    {
                                        OSL_grainages[row, col, soil_layer][grain] = start_age;
                                        OSL_depositionages[row, col, soil_layer][grain] = start_age;
                                    }
                                }
                                if (CN_checkbox.Checked)
                                {
                                    CN_atoms_cm2[row, col, soil_layer, 0] = met10Be_inherited * met_10Be_clayfraction;
                                    CN_atoms_cm2[row, col, soil_layer, 1] = met10Be_inherited * (1 - met_10Be_clayfraction);
                                    CN_atoms_cm2[row, col, soil_layer, 2] = is10Be_inherited;
                                    CN_atoms_cm2[row, col, soil_layer, 3] = isC14_inherited;
                                }
                                soil_layer++;
                            } // end available soil depth > 0
                        } // end else 
                        soildepth_m[row, col] = total_soil_thickness(row, col);
                    }


                } // end col
            } // end row
              //Debug.WriteLine("initialised soil");
            writeallsoils(workdir + "\\" + run_number + "_" + t + "_out_allsoils.csv", t);
        }

        void initialise_every_till()
        {
            if (check_time_till_fields.Checked && check_space_till_fields.Checked == false)
            {
                for (int row = 0; row < nr; row++)
                {
                    for (int col = 0; col < nc; col++)
                    {
                        if (dtm[row, col] != nodata_value)//AleG
                        {tillfields[row, col] = 1 * till_record[t]; }
                            
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
                    if (dtm[row, col]!= nodata_value)//AleG
                    { // time runs from 1 to end_time - compensate for that when taking values from records
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

                        if (annual_output_checkbox.Checked)
                        {
                            dz_soil[row, col] = 0;
                            if (Creep_Checkbox.Checked) { sum_creep_grid[row, col] = 0; creep[row, col] = 0; }
                            if (treefall_checkbox.Checked) { dz_treefall[row, col] = 0; treefall_count[row, col] = 0; }
                            if (Water_ero_checkbox.Checked) { sum_water_erosion[row, col] = 0; }
                            if (Proglacial_checkbox.Checked) { meltwater_m[row, col] = 0; glacier_cell[row, col] = 0; } 
                            if (Biological_weathering_checkbox.Checked) 
                            {
                                bedrock_weathering_m[row, col] = 0; //AleG
                                sum_biological_weathering[row, col] = 0; 
                            }
                            if (Frost_weathering_checkbox.Checked) { sum_frost_weathering[row, col] = 0; }
                            if (Tillage_checkbox.Checked) { sum_tillage[row, col] = 0; total_sum_tillage = 0; }
                            if (soildepth_m[row, col] < 0.0) { soildepth_error += soildepth_m[row, col]; soildepth_m[row, col] = 0; }
                        }
                        if (Landslide_checkbox.Checked) //AleG
                        {
                            crrain_m_d[row, col] = 0;
                            ero_slid_m[row, col] = 0;
                            old_SOM_in_transport_kg[row, col] = 0;
                            remaining_vertical_size_m[row, col] = 0;
                            sat_bd_kg_m3[row, col] = 0;
                            sed_slid_m[row, col] = 0;
                            slidestatus[row, col] = 0;
                            young_SOM_in_transport_kg[row, col] = 0;
                            for (int material = 0; material < 5; material++)
                            {
                                sediment_in_transport_kg[row, col, material] = 0;
                            }

                            //sum_landsliding[row, col] = 0; 
                        } //AleG total_sum_tillage = 0; 
                        if (soildepth_m[row, col] < 0.0) { soildepth_error += soildepth_m[row, col]; soildepth_m[row, col] = 0; }
                        if (Water_ero_checkbox.Checked) { waterflow_m3[row, col] = 0.0; } //AleG K_fac[row, col] = 0;
                                                                                          //if (check_space_landuse.Checked)//AleG //if i activate these every cell is zero 
                                                                                          //{
                                                                                          //K_fac[row, col] = 0;
                                                                                          //infil[row, col] = 0;
                                                                                          // evapotranspiration[row, col] = 0;
                                                                                          //root_cohesion_kPa_new[row, col] = 0;
                                                                                          // }
                    }



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
                            if (dtm[row, col] < dtmfill_A[row, col] && dtm[row, col] != nodata_value)
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

        #region update code
        void update_all_layer_thicknesses(int row_update, int col_update)
        {
            for (int layer_update = 0; layer_update < max_soil_layers; layer_update++)
            {
                layerthickness_m[row_update, col_update, layer_update] = thickness_calc(row_update, col_update, layer_update);
                layerthickness_m[row_update, col_update, layer_update] = thickness_calc(row_update, col_update, layer_update);
            }

        }

        void remove_empty_layers(int row2, int col2)
        {
            // mainly after tree fall, there can be empty soil layers at the surface. This module shifts the layers up.
            // displaysoil(row_to, col_to);
            //Debug.WriteLine("rel1");
            // DEVELOP after shifting cells up the script runs through the lower empty cells, moving them up also. with some booleans, this should be prevented. there is no error now, only longer simulation time.  
            try
            {
                //int diagnostic_mode = 1;
                int totalgrains_start = 0;
                if (OSL_checkbox.Checked) { for (int lay = 0; lay < max_soil_layers; lay++) { totalgrains_start += OSL_grainages[row2, col2, lay].Length; } }

                //if (diagnostic_mode == 1) { Debug.WriteLine("entered removing empty layers"); }
                int empty_layers = 0;
                bool shift_layers = false;
                bool top_of_profile = true;

                int n_shifts = 0;
                Decimal mass_before = total_soil_mass_kg_decimal(row2, col2);
                //Debug.WriteLine("rel2");
                for (int lay2 = 0; lay2 < max_soil_layers; lay2++)
                {
                    bool full_layer_shift = false;
                    double layer_mass = total_layer_mass_kg(row2, col2, lay2);
                    if (layer_mass < 0.000000000001 & top_of_profile) // empty layer
                                                     // mind for empty layers at the bottom
                    {
                        shift_layers = true;
                        // if(n_shifts == 0) { displaysoil(row_to, col_to); }
                        // n_shifts += 1;

                        empty_layers++;
                        for (int layert = lay2; layert < max_soil_layers - 1; layert++) // for all underlying layers, shift one up (since there is space anyway)
                        {

                            if (total_layer_mass_kg(row2, col2, layert + 1) > 0) { full_layer_shift = true; }
                            //Debug.WriteLine(layert);
                            transfer_material_between_layers(row2, col2, layert + 1, row2, col2, layert, 1);
                        }
                        
                        // check old layer again, in case multiple layers were empty
                        if (full_layer_shift == true) { lay2--; }
                    }
                    else
                    {
                        // we're past layers with mass, so we're reaching the bottom of the profile. No need to shift empty layers up
                        top_of_profile = false;
                    }
                }
                // Debug.WriteLine("rel3");
                if (shift_layers == true)
                {
                    Decimal mass_after = total_soil_mass_kg_decimal(row2, col2);
                    if (Math.Round(mass_before - mass_after) > Convert.ToDecimal(0.0000001))
                    {
                        Debug.WriteLine("Loss of soil data during removal of empty layers");
                        displaysoil(row2, col2);
                    }

                }

                if (diagnostic_mode == 1) { if (n_shifts > 0) { displaysoil(row2, col2); Debug.WriteLine("n layers shifted: {0} in row {1}, col {2}", n_shifts, row2, col2); } }
                update_all_layer_thicknesses(row2, col2);

                int totalgrains_end = 0;
                if (OSL_checkbox.Checked) { for (int lay = 0; lay < max_soil_layers; lay++) { totalgrains_end += OSL_grainages[row2, col2, lay].Length; } if (totalgrains_end != totalgrains_start) { Debugger.Break(); } }

            } // end try

            catch
            {
                Debug.WriteLine("Error in removing empty layers");
                Debug.WriteLine(" at end of remove_empty_layers: "); displaysoil(row2, col2);

            }
        }

        void soil_update_split_and_combine_layers()
        {
            try
            {
                int layer;
                double depth_m, z_layer_ref_m, old_thickness, new_thickness;
                bool boolsplit, boolcombine;

                
                total_average_soilthickness_m = 0;
                number_soil_thicker_than = 0;
                number_soil_coarser_than = 0;
                local_soil_depth_m = 0;
                local_soil_mass_kg = 0;

                for (row = 0; row < nr; row++)
                {
                    for (col = 0; col < nc; col++)
                    {
                        if (dtm[row, col] != nodata_value)
                        {
                            remove_empty_layers(row, col);
                            update_all_layer_thicknesses(row, col);

                            depth_m = 0;
                            // loop over all layers, except the last one
                            for (layer = 0; layer < (max_soil_layers - 1); layer++)
                            {
                                z_layer_ref_m = layer_z_surface * Math.Pow(layer_z_increase, layer);

                                if (OSL_checkbox.Checked & layer == 0)
                                {
                                    z_layer_ref_m = bleaching_depth_m;
                                }

                                // update surface layer
                                if (layer == 0)
                                {
                                    if (layerthickness_m[row, col, layer] < 0.001)
                                    {
                                        // smaller than one mm, merge with layer below, to avoid numerical problems when always a fraction leaves the profile
                                        combine_layers(row, col, layer, layer + 1);
                                        boolcombine = true;
                                    }
                                    while (layerthickness_m[row, col, layer] > z_layer_ref_m * (1 + tolerance))
                                    {
                                        // too thick, split layer
                                        split_layer(row, col, layer, z_layer_ref_m);
                                        update_all_layer_thicknesses(row, col);
                                        boolsplit = true;
                                    }
                                }
                                else
                                {
                                    if (layerthickness_m[row, col, layer] < (z_layer_ref_m * (1 - tolerance))) // Lower end, combine
                                    {
                                        // always merge with lower neighbour, to keep as much resolution towards the surface, and to avoid infinite splitting and merging of the same layer
                                        combine_layers(row, col, layer, layer + 1);
                                        boolcombine = true;
                                    }
                                    if (layerthickness_m[row, col, layer] > (z_layer_ref_m * (1 + tolerance))) // Higher end, split
                                    {
                                        split_layer(row, col, layer, z_layer_ref_m);
                                        boolsplit = true;
                                    }
                                }
                                depth_m += layerthickness_m[row, col, layer];

                            } // end layer

                            // update time series and matrices
                            if (timeseries.timeseries_number_soil_thicker_checkbox.Checked && System.Convert.ToDouble(timeseries.timeseries_soil_thicker_textbox.Text) < depth_m) { number_soil_thicker_than++; }
                            if (timeseries.total_average_soilthickness_checkbox.Checked) { total_average_soilthickness_m += depth_m; }
                            if (timeseries.timeseries_soil_depth_checkbox.Checked && System.Convert.ToInt32(timeseries.timeseries_soil_cell_row.Text) == row && System.Convert.ToInt32(timeseries.timeseries_soil_cell_col.Text) == col)
                            {
                                local_soil_depth_m = depth_m;
                            }

                            // update dtm and soil thickness map
                            update_all_layer_thicknesses(row, col);
                            old_thickness = soildepth_m[row, col];
                            new_thickness = total_soil_thickness(row, col);
                            dtm[row, col] += new_thickness - old_thickness;
                            soildepth_m[row, col] = new_thickness;
                            dtmchange_m[row, col] += new_thickness - old_thickness;
                            dz_soil[row, col] += new_thickness - old_thickness;
                        } // end dtm != nodata_value
                    } // end col
                } // end row
            }
            catch
            {
                Debug.WriteLine("error in splitting and combining");
            }
        }

        void split_layer(int rowwer, int coller, int lay_split, double z_lay_ref_m)
        {

            try
            {
                int totalgrains_start = 0;
                int grains_splitlayer = 0;
                int laynum;
                double div;
                int[] grains_before = new int[max_soil_layers];
                int[] grains_after = new int[max_soil_layers];


                if (OSL_checkbox.Checked)
                {
                    for (int lay_OSL = 0; lay_OSL < max_soil_layers; lay_OSL++)
                    {
                        totalgrains_start += OSL_grainages[rowwer, coller, lay_OSL].Length;
                        grains_before[lay_OSL] = OSL_grainages[rowwer, coller, lay_OSL].Length;
                    }
                    grains_splitlayer = OSL_grainages[rowwer, coller, lay_split].Length;
                }
                Decimal old_mass_soil, new_mass_soil;
                old_mass_soil = total_soil_mass_kg_decimal(rowwer, coller);

                //splitting will increase the number of layers. If this splits beyond the max number of layers, then combine the bottom two layers
                // This is not a problem when the second last layer will be split, as it will give a part of its material to the bottom layer without creating a new layer
                // so only for the other layers, we need to merge bottom two layers to create space for the split

                if (lay_split < (max_soil_layers - 2)) // if we're not dealing with the second last layer
                {
                    if (total_layer_mass_kg(rowwer, coller, max_soil_layers - 1) > 0) // and, if we are using the lowest possible layer already:
                    {
                        //this breaks now because the lowest layer can be empty due to its encountering a hard layer ArT

                        // merge bottom two layers to free up one layer
                        try { combine_layers(rowwer, coller, max_soil_layers - 2, max_soil_layers - 1); }
                        catch { Debug.WriteLine(" failed to combine layers to prepare for splitting "); }
                    }

                    if (Math.Abs(old_mass_soil - total_soil_mass_kg_decimal(rowwer, coller)) > Convert.ToDecimal(0.0001))
                    {
                        Debug.WriteLine("err_spl_1 {0}", total_soil_mass_kg_decimal(rowwer, coller));
                    }

                    // now we can move all layers down below the one we want to split
                    for (laynum = max_soil_layers - 1; laynum >= lay_split + 2; laynum--)
                    {
                        transfer_material_between_layers(rowwer, coller, laynum - 1, rowwer, coller, laynum, 1);
                    }

                    if (Math.Abs(old_mass_soil - total_soil_mass_kg_decimal(rowwer, coller)) > Convert.ToDecimal(0.00001))
                    {
                        Debug.WriteLine("err_spl_2 {0}", total_soil_mass_kg_decimal(rowwer, coller));
                        // Debugger.Break();
                        // old_mass_soil = total_soil_mass(rowwer, coller);
                    }
                }

                // determine splitting ratio based on variable layer thickness. div is the fraction that stays behind
                // checkbox dat lagen niet te dik of te dun worden MvdM
                div = 1 / (Math.Pow(layer_z_increase, 0) + Math.Pow(layer_z_increase, 1));

                // Two checks:
                // if remaining layer thickness exceeds the tolerated layer thickness. Can happen with big pulses of sediment (landsliding, tree fall)
                // if splitted layer is the second last layer
                // in those cases, div will be based on the reference thickness to avoid too thick layers 
                if ((div * layerthickness_m[rowwer, coller, lay_split]) > (z_lay_ref_m * (1 + tolerance)) |
                    (lay_split) == (max_soil_layers - 2))
                {
                    div = z_lay_ref_m / (layerthickness_m[rowwer, coller, lay_split]);
                }

                if (div > 1) { div = 1; }
                if (div < 0) { div = 0; }
                transfer_material_between_layers(rowwer, coller, lay_split, rowwer, coller, lay_split + 1, 1 - div);

                int totalgrains_end = 0;
                int splitlayers_end = 0;
                if (OSL_checkbox.Checked)
                {
                    for (int lay_OSL = 0; lay_OSL < max_soil_layers; lay_OSL++)
                    {
                        totalgrains_end += OSL_grainages[rowwer, coller, lay_OSL].Length;
                        grains_after[lay_OSL] = OSL_grainages[rowwer, coller, lay_OSL].Length;
                    }
                    if (totalgrains_end != totalgrains_start)
                    {
                        Debug.WriteLine("Grains before: {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}", grains_before[0], grains_before[1], grains_before[2], grains_before[3], grains_before[4], grains_before[5], grains_before[6], grains_before[7], grains_before[8], grains_before[9]);
                        Debug.WriteLine("Grains after: {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}", grains_after[0], grains_after[1], grains_after[2], grains_after[3], grains_after[4], grains_after[5], grains_after[6], grains_after[7], grains_after[8], grains_after[9]);

                        Debugger.Break();
                    }
                    splitlayers_end = OSL_grainages[rowwer, coller, lay_split].Length + OSL_grainages[rowwer, coller, lay_split + 1].Length;
                    if (grains_splitlayer != splitlayers_end)
                    {
                        Debug.WriteLine("Grains before: {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}", grains_before[0], grains_before[1], grains_before[2], grains_before[3], grains_before[4], grains_before[5], grains_before[6], grains_before[7], grains_before[8], grains_before[9]);
                        Debug.WriteLine("Grains after: {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}", grains_after[0], grains_after[1], grains_after[2], grains_after[3], grains_after[4], grains_after[5], grains_after[6], grains_after[7], grains_after[8], grains_after[9]);
                        // Debugger.Break();
                        displaysoil(rowwer, coller);
                        // Debugger.Break();
                    }
                }
            }
            catch
            {
                Debug.WriteLine("Failed at splitting layer at row {0}, col {1} at time {2}", row, col, t);
            }

        }

        void combine_layers(int rowwer, int coller, int lay1, int lay2)  // combines two soil layers into the first, recalculates their new thickness and shifts underlying layers up
        {
            double CN_before = 0, CN_after = 0;
            //if (CN_checkbox.Checked) { CN_before = total_CNs(); }

            

            double mass_before = total_layer_mass_kg(rowwer, coller, lay1) + total_layer_mass_kg(rowwer, coller, lay2);
            try
            {
                int totalgrains_start = 0;
                if (OSL_checkbox.Checked) { for (int lay_OSL = 0; lay_OSL < max_soil_layers; lay_OSL++) { totalgrains_start += OSL_grainages[rowwer, coller, lay_OSL].Length; } }

                decimal old_soil_mass1 = total_soil_mass_kg_decimal(rowwer, coller);

                // transport all material from lay_to to lay_from
                transfer_material_between_layers(rowwer, coller, lay2, rowwer, coller, lay1, 1);
                layerthickness_m[rowwer, coller, lay1] = thickness_calc(rowwer, coller, lay1);

                // move all other layers one layer up, since there is space. layert (lay_to) should be empty and replaced with contents of layert + 1
                for (int layert = lay2; layert < max_soil_layers - 1; layert++)
                {
                    transfer_material_between_layers(rowwer, coller, layert + 1, rowwer, coller, layert, 1);
                }
                //Debug.WriteLine("cl2");

                //now set the last layer to sentinel value of 0
                for (int i = 0; i < 5; i++)
                {
                    texture_kg[rowwer, coller, max_soil_layers - 1, i] = 0;
                }
                old_SOM_kg[rowwer, coller, max_soil_layers - 1] = 0;
                young_SOM_kg[rowwer, coller, max_soil_layers - 1] = 0;
                layerthickness_m[rowwer, coller, max_soil_layers - 1] = 0;
                bulkdensity[rowwer, coller, max_soil_layers - 1] = 0;

                if (CN_checkbox.Checked)
                {
                    for (int cosmo = 0; cosmo < n_cosmo; cosmo++)
                    {
                        CN_atoms_cm2[rowwer, coller, max_soil_layers - 1, cosmo] = 0;
                    }
                    //CN_after = total_CNs();
                    //if (((CN_before - CN_after) / (CN_after + 1)) > 1E-6) { Debugger.Break(); }
                }
                //Debug.WriteLine("cl4");

                decimal new_soil_mass1 = total_soil_mass_kg_decimal(rowwer, coller);
                //if (Math.Abs(old_soil_mass1 - new_soil_mass1) > 0.00000001) { displaysoil(rowwer, coller); Debugger.Break(); }
                double mass_after = total_layer_mass_kg(rowwer, coller, lay1);
                if (Math.Abs(mass_before - mass_after) > 0.0001)
                {
                    Debug.WriteLine("err_cl1");
                }

                int totalgrains_end = 0;
                if (OSL_checkbox.Checked) { for (int lay_OSL = 0; lay_OSL < max_soil_layers; lay_OSL++) { totalgrains_end += OSL_grainages[rowwer, coller, lay_OSL].Length; } if (totalgrains_end != totalgrains_start) { Debugger.Break(); } }
            }
            catch
            {
                Debug.WriteLine("Failed at combining layers {0} {1} at row {2}, col {3} at time {4}", lay1, lay2, rowwer, coller, t);
                Debugger.Break();
            }
        }

        void transfer_material_between_layers(int row_from, int col_from, int lay_from, int row_to, int col_to, int lay_to, double fraction_transport, bool transport_coarse = true)
        {
            double transport_betw_layers;
            // determine whether coarse material gets transported as well. Standard is yes, but some cases it doesn't happen, such as bioturbation by soil fauna
            int tex_start = 0;


            if (!transport_coarse) { tex_start = 1; }
            if (fraction_transport > 0)
            {
                for (int tex = tex_start; tex < n_texture_classes; tex++)
                {
                    transport_betw_layers = texture_kg[row_from, col_from, lay_from, tex] * fraction_transport;
                    texture_kg[row_from, col_from, lay_from, tex] -= transport_betw_layers;
                    texture_kg[row_to, col_to, lay_to, tex] += transport_betw_layers;
                }
                transport_betw_layers = young_SOM_kg[row_from, col_from, lay_from] * fraction_transport;
                young_SOM_kg[row_from, col_from, lay_from] -= transport_betw_layers;
                young_SOM_kg[row_to, col_to, lay_to] += transport_betw_layers;

                transport_betw_layers = old_SOM_kg[row_from, col_from, lay_from] * fraction_transport;
                old_SOM_kg[row_from, col_from, lay_from] -= transport_betw_layers;
                old_SOM_kg[row_to, col_to, lay_to] += transport_betw_layers;

                if (CN_checkbox.Checked)
                {
                    for (int cn = 0; cn < n_cosmo; cn++)
                    {
                        transport_betw_layers = CN_atoms_cm2[row_from, col_from, lay_from, cn] * fraction_transport;
                        CN_atoms_cm2[row_from, col_from, lay_from, cn] -= transport_betw_layers;
                        CN_atoms_cm2[row_to, col_to, lay_to, cn] += transport_betw_layers;
                    }
                }
                if (OSL_checkbox.Checked)
                {
                    transfer_OSL_grains(row_from, col_from, lay_from, row_to, col_to, lay_to, fraction_transport, 0);
                }
            }
            update_all_layer_thicknesses(row_from, col_from);
        }

        double layer_difference(int rowwer, int coller, int lay1, int lay2)   //calculates a simple measure of difference between two soil layers based on the sum of relative differences in a set of properties
        {
            double average_property_value = 0, property_difference = 0, sum_property_difference = 0;

            try
            {
                // double average_property_value = 0, property_difference = 0, sum_property_difference = 0;
                for (int i = 0; i < 5; i++)
                {
                    //account for total soil mass
                    average_property_value = (texture_kg[rowwer, coller, lay1, i] + texture_kg[rowwer, coller, lay2, i]) / 2;
                    property_difference = Math.Abs(texture_kg[rowwer, coller, lay1, i] - texture_kg[rowwer, coller, lay2, i]);
                    sum_property_difference += property_difference / average_property_value;
                }
                average_property_value = (old_SOM_kg[rowwer, coller, lay1] + old_SOM_kg[rowwer, coller, lay2]) / 2;
                property_difference = Math.Abs(old_SOM_kg[rowwer, coller, lay1] - old_SOM_kg[rowwer, coller, lay2]);
                sum_property_difference += property_difference / average_property_value;
                average_property_value = (young_SOM_kg[rowwer, coller, lay1] + young_SOM_kg[rowwer, coller, lay2]) / 2;
                property_difference = Math.Abs(young_SOM_kg[rowwer, coller, lay1] - young_SOM_kg[rowwer, coller, lay2]);
                sum_property_difference += property_difference / average_property_value;
                sum_property_difference /= 7;
                return sum_property_difference;
            }
            catch
            {
                MessageBox.Show("error in calculating layer difference");
            }
            return sum_property_difference;
        }

        double bulk_density_calc_kg_m3(double coarse_mass, double sand_mass, double silt_mass, double clay_mass, double fine_clay_mass, double OMo_mass, double OMy_mass, double depth)
        {
            if (depth == 0) { depth = 0.001; } // reset values of 0 to a thickness of 1 micrometer, to avoid infinite numbers in the calculation of BD
            double bd = 2700, combined_frac, m_finesoil;
            m_finesoil = sand_mass + silt_mass + clay_mass + fine_clay_mass;
            if (m_finesoil > 0)
            {
                combined_frac = sand_mass / m_finesoil + 0.76 * silt_mass / m_finesoil;

                bd = 1000 * (1.35 + 0.00452 * 100 * combined_frac + Math.Pow(44.65 - 100 * combined_frac, 2) * -0.0000614 + 0.06 * Math.Log10(depth));  // in kg/m3

                //now coarse fragment and SOM correction
                bd = (coarse_mass + m_finesoil + OMo_mass + OMy_mass) / ((m_finesoil / bd) + (coarse_mass / 2700) + (OMo_mass + OMy_mass) / 224); // ooit through interface    
            }
            else
            {
                bd = 2700;
            }

            return bd; // in kg/m3

        }

        double thickness_calc(int rowwer, int coller, int lay1)
        {
            // Debug.WriteLine("tc1");
            double thickness, soil_mass = 0;

            // calculate current depth of layer, for bulk density calculations, using current thickness. 
            double depth_m = 0;
            for (int lay_temp = 0; lay_temp < lay1; lay_temp++)
            {
                depth_m += layerthickness_m[rowwer, coller, lay_temp];
            }
            depth_m += layerthickness_m[rowwer, coller, lay1] / 2;
            // Debug.WriteLine("tc2");
            int i;
            //first calculate total soil mass to calculate mass percentages for the size fractions
            for (i = 1; i < 5; i++)
            {
                soil_mass += texture_kg[rowwer, coller, lay1, i];
            }
            soil_mass += old_SOM_kg[rowwer, coller, lay1] + young_SOM_kg[rowwer, coller, lay1];
            // Debug.WriteLine("tc3");
            if (soil_mass > 0)
            {
                bulkdensity[rowwer, coller, lay1] = bulk_density_calc_kg_m3(texture_kg[rowwer, coller, lay1, 0], texture_kg[rowwer, coller, lay1, 1], texture_kg[rowwer, coller, lay1, 2], texture_kg[rowwer, coller, lay1, 3], texture_kg[rowwer, coller, lay1, 4], old_SOM_kg[rowwer, coller, lay1], young_SOM_kg[rowwer, coller, lay1], depth_m);
            }
            else
            {
                //either there is no soil at all, or there is only coarse material
                if (texture_kg[rowwer, coller, lay1, 0] > 0)
                {
                    bulkdensity[rowwer, coller, lay1] = 2700;   //kg/m3
                }
                // Debug.WriteLine("tc5");

            }
            // Debug.WriteLine("tc5");
            soil_mass += texture_kg[rowwer, coller, lay1, 0];
            if (soil_mass == 0)
            {
                thickness = 0;
            }
            else
            {
                thickness = (soil_mass) / (dx * dx * bulkdensity[rowwer, coller, lay1]);  // thickness in m per unit area
            }
            // Debug.WriteLine("tc6");
            if (double.IsNaN(thickness)) { thickness = 0.00000000001; }
            return thickness;
        }

        double calc_thickness_from_mass(double[] textures_kg, double yom_kg, double oom_kg)
        {
            //pdf goes here
            double thickness_m = 0, soil_mass_kg = 0;
            double sand_fraction, silt_fraction, bulk_density;
            //first calculate total soil mass to calculate mass percentages for the fine earth fractions (excluding coarse)
            for (int ir = 1; ir < 5; ir++)
            {
                soil_mass_kg += textures_kg[ir];
            }
            soil_mass_kg += oom_kg + yom_kg;
            sand_fraction = textures_kg[1] / soil_mass_kg;
            silt_fraction = textures_kg[2] / soil_mass_kg;

            //calculate bulk density
            bulk_density = bulk_density_calc_kg_m3(textures_kg[0], textures_kg[1], textures_kg[2], textures_kg[3], textures_kg[4], oom_kg, yom_kg, 0.001); // MM depth of 1 micrometer, because a depth of 0 will result in infinite numbers 

            thickness_m = (soil_mass_kg + textures_kg[0]) / (dx * dx * bulk_density);  // thickness in m per unit area

            return thickness_m;
        }
        #endregion

    }
}
