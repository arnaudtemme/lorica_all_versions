﻿

// HydroLorica v1.0 by W.M. van der Meij and A.J.A.M. Temme, 2020
// Based on Lorica by Vanwalleghem and Temme 2016
// Based on MILESD 2011 by Vanwalleghem et al and on LAPSUS by Temme, Schoorl and colleagues (2006-2011)
// 
// Credits to T.J. Coulthard for interface coding template (the CAESAR model, www.coulthard.org.uk)

//This program is free software; you can redistribute it and/or modify it under the terms of the 
//GNU General Public License as published by the Free Software Foundation;  
//This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; 
//without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 
//See the GNU General Public License (http://www.gnu.org/copyleft/gpl.html) for more details. 
//You should have received a copy of the GNU General Public License along with this program; 
//if not, write to the Free Software Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, 
//MA 02110-1301, USA.

// June 2020

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LORICA4
{
    /// <summary>
    /// Main LORICA interface
    /// </summary>

    public partial class Mother_form : System.Windows.Forms.Form
    {
        [DllImport("msvcrt")]
        static extern int _getch();

        #region global interface parameters
        private TabPage Soil;

        private System.Windows.Forms.MainMenu mainMenu1;
        private System.Windows.Forms.MenuItem menuItemConfigFile;
        private System.Windows.Forms.MenuItem menuItemConfigFileOpen;
        private System.Windows.Forms.MenuItem menuItemConfigFileSave;
        private System.Windows.Forms.MenuItem menuItemConfigFileSaveAs;
        private System.Windows.Forms.StatusBar statusBar1;
        private System.Windows.Forms.StatusBarPanel TimeStatusPanel;
        private System.Windows.Forms.StatusBarPanel ScenarioStatusPanel;
        private System.Windows.Forms.StatusBarPanel InfoStatusPanel;
        private System.Windows.Forms.Button start_button;
        private System.Windows.Forms.Button End_button;
        private System.Windows.Forms.ToolTip toolTip1;
        private OpenFileDialog openFileDialog1;
        private System.Windows.Forms.Label label1;
        private Button button6;
        private TextBox textBox1;
        private TextBox textBox2;
        private System.Windows.Forms.Label label2;
        private TabPage Output;
        private GroupBox groupBox6;
        private GroupBox groupBox1;
        private CheckBox water_output_checkbox;
        private CheckBox all_process_output_checkbox;
        private CheckBox Soildepth_output_checkbox;
        private CheckBox Alt_change_output_checkbox;
        private CheckBox Altitude_output_checkbox;
        private CheckedListBox checkedListBox1;
        private CheckBox Regular_output_checkbox;
        private CheckBox Final_output_checkbox;
        private TextBox Box_years_output;
        private TextBox textBox6;
        private CheckBox UTMsouthcheck;
        private TextBox UTMzonebox;
        private TabPage Run;
        private GroupBox groupBox7;
        private RadioButton runs_checkbox;
        private System.Windows.Forms.Label label16;
        private TextBox Number_runs_textbox;
        private TabPage Input;
        private TextBox tillfields_constant_textbox;
        private TextBox tillfields_input_filename_textbox;
        private TextBox evap_constant_value_box;
        private TextBox evap_input_filename_textbox;
        private TextBox infil_constant_value_box;
        private TextBox infil_input_filename_textbox;
        private TextBox rainfall_constant_value_box;
        private TextBox landuse_constant_value_box;
        private TextBox soildepth_constant_value_box;
        private TextBox landuse_input_filename_textbox;
        private TextBox soildepth_input_filename_textbox;
        private TextBox rain_input_filename_textbox;
        private TextBox dtm_input_filename_textbox;
        private CheckBox check_space_evap;
        private CheckBox check_space_infil;
        private CheckBox check_space_rain;
        private CheckBox check_space_till_fields;
        private CheckBox check_space_landuse;
        private CheckBox check_space_soildepth;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.Label constant_value_label;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label25;
        private System.Windows.Forms.Label label23;
        private TabPage Geomorph;
        private CheckBox Creep_Checkbox;
        private TabControl tabControl1;
        private GroupBox groupBox12;
        private GroupBox groupBox11;
        private System.Windows.Forms.Label label8;
        private RadioButton annual_output_checkbox;
        private RadioButton cumulative_output_checkbox;
        private CheckBox check_space_DTM;
        private CheckBox check_time_evap;
        private CheckBox check_time_infil;
        private CheckBox check_time_rain;
        private CheckBox check_time_till_fields;
        private CheckBox check_time_landuse;
        private System.Windows.Forms.Label label29;
        private Button explain_input_button;
        private MenuItem Menu_About_box;
        private GroupBox groupBox_landuse;
        private Button landuse_determinator_button;
        #endregion

        #region global model parameters
        private Button soil_specify_button;
        private bool merely_calculating_derivatives;
        private System.Windows.Forms.Timer timer1;
        private TextBox textbox_t_intervene;
        private CheckBox checkbox_t_intervene;
        private System.Windows.Forms.Label label_max_soil_layers;
        private TextBox textbox_max_soil_layers;
        private TabControl tabControl3;
        private TabPage Geochron;
        private TabPage tabPage4;
        private TextBox ngrains_textbox;
        private CheckBox OSL_checkbox;
        private TabPage tabPage5;
        private CheckBox CN_checkbox;
        private System.Windows.Forms.Label label121;
        private System.Windows.Forms.Label label122;
        private TextBox bleachingdepth_textbox;
        private System.Windows.Forms.Label label123;
        private TextBox isBe10_sp_input_textbox;
        private TextBox Be10_decay_textbox;
        private TextBox metBe10_input_textbox;
        private System.Windows.Forms.Label label124;
        private System.Windows.Forms.Label label126;
        private System.Windows.Forms.Label label125;
        int save_interval2 = 0;

        private System.ComponentModel.IContainer components;
        Stopwatch stopwatch;
        TimeSpan geo_t, pedo_t, hydro_t, ponding_t, OSL_matrix_t, OSL_JA_t;
        DateTime OSL_matrix_start, OSL_JA_start;
        double[,,,]    //4D matrix for soil texture masses in different x,y and z for t texture classes (x,y,z,t)
                    texture_kg;                 //mass in kg (per voxel = layer * thickness)

        double[,,,] CN_atoms_cm2;        // Keeps track of cosmogenic nuclide stocks. For now 0: meteoric Be, 1: in situ Be, ...
        int n_cosmo = 5;
        double met_10Be_input, met10Be_inherited, met_10Be_clayfraction, is10Be_inherited, isC14_inherited, decay_Be10, P0_10Be_is_sp, P0_10Be_is_mu, decay_C14, P0_14C_is_sp, P0_14C_is_mu, attenuation_length_sp, attenuation_length_mu, met_10Be_adsorptioncoefficient;

        double[,,]     //3D matrices for properties of soil layers in different x y (x,y,z)
                    layerthickness_m,         // : thickness in m 
                    young_SOM_kg,         // : OM mass in kgrams (per voxel = layer * thickness)
                    old_SOM_kg,         // : OM mass in kgrams (per voxel = layer * thickness) 
                    bulkdensity;            // : bulkdensity in kg/m3 (over the voxel = layer * thickness)

        double[,,] sediment_in_transport_kg,         // sediment mass in kg in transport per texture class
                    litter_kg;                     // Litter contents (Luxembourg case study)
        double[,,] CN_in_transport;                // Tracking cosmogenic nuclides

        double[,]   // double matrices - these are huge memory-eaters and should be minimized 
                    // they only get that memory later, and only when needed
                    original_dtm,       //where sealevel interactions are used
                    dtm,                //altitude matrix
                    dtmchange_m,  	    //change in altitude matrix
                    dtmfill_A,
                    og_dtm,           //original dtm accessed for proglacial mode 
                    filled_dtm,       //filled dtm accessed for proglacial mode 
                    dtm_WE,           //original dtm accessed for proglacial mode
                    dz_soil,
                    waterflow_m3,        //discharge matrix
                    K_fac,
                    P_fac,
                    infil,              //infiltration matrix
                    dz_ero_m,             //altitude change due to erosion  (negative values)
                    dz_sed_m,             //altitude change due to sedimentation   (positive values)
                    soildepth_m,
                    young_SOM_in_transport_kg,
                    old_SOM_in_transport_kg,
                    creep,
                    bedrock_weathering_m,
                    frost_weathering,
                    solif,
                    till_result,
                    dz_till_bd,
                    dz_treefall,        // elevation change by tree fall
                    aspect,             //aspect for calculation of hillshade
                    slopeAnalysis,      //for calculation of hillshade
                    Tau,                //for graphics
                    hillshade,          //for graphics
                    sum_water_erosion,
                    sum_biological_weathering,
                    sum_frost_weathering,
                    sum_creep_grid,
                    sum_solifluction,
                    sum_tillage,
                    sum_landsliding,
                    sum_uplift,
                    sum_tilting,
                    veg,
                    veg_correction_factor,
                    evapotranspiration,
                    stslope_radians,		    // matrix with steepest descent local slope [rad]
                    crrain_m_d,             // matrix with critical steady state rainfall for landsliding [m/d]
                    camf,               // matrix with number of contributing draining cells, multiple flow [-]
                    T_fac,              // matrix with transmissivity [m/d] values
                    Cohesion_factor,              // matrix with combined cohesion [-] values
                    Cs_fac,             // matrix with soil cohesion [kPa] values
                    sat_bd_kg_m3,              // matrix with bulk density values [g/cm3]
                    peak_friction_angle_radians,
                    resid_friction_angle_radians,         // matrix with angle of internal friction values [rad]??? or [degrees]???
                    reserv,
                    ero_slid_m,
                    cel_dist,
                    sed_slid_m,
                    sed_bud_m,
                    dh_slid,
                    lake_sed_m,         //the thickness of lake sediment
                    rain_m,
                    timeseries_matrix,
                    lessivage_errors, // for calibration of lessivage
                    tpi,            //topographic position index
                    hornbeam_cover_fraction, //hornbeam fraction Lux
                    observations,
                    root_cohesion_kPa_new,
                    OM_LU,
                    Till_LU,
                    BiotRate_LU,
                    remaining_vertical_size_m;  //for landslides

        int[,]  // integer matrices
                    status_map,         //indicates whether a cell is a sink, a saddle, a flat or a top
                    depression,         //indicates whether a cell belongs to a lake, and which lake
                    slidemap,
                    soilmap,            // integer numbers for soil map
                    watsh,              // watershed;
                    landuse,            //landuse in classes
                    tillfields,         //fields for tillage 
                    treefall_count,     // count number of tree falls
                    age_rast_yr,       //age raster for proglacial mode - raster representing the year in which the glacier was last recorded at a certain location
                    vegetation_type,
                    slidenr;
        short[,] slidestatus;

        int[,,][] OSL_grainages, OSL_depositionages, OSL_surfacedcount;
        int[,][] OSL_grainages_in_transport, OSL_depositionages_in_transport, OSL_surfacedcount_in_transport;
        int ngrains_kgsand_m2, start_age;
        double bleaching_depth_m;

        int[,]
        drainingoutlet_row = new int[numberofsinks, 5],
        drainingoutlet_col = new int[numberofsinks, 5];

        int[] row_index, col_index;  // for sorting the DEM from high to low
        string[] rowcol_index;
        double[] index;

        //sinks and depression parameters:
        //the constant values below may have to be increased for large or strange landscapes and studies
        const int numberofsinks = 10000;           // run the program once to find out the number of sinks. The exact number and any higher number will do....
        const double tangent_of_delta = 0.005;
        const int maxlowestnbs = 100000;
        const double epsilon = 0.000001;
        const double root = 7.07;
        int max_soil_layers;

        double[] local_s_i_t_kg = new double[] { 0, 0, 0, 0, 0 };

        // for layer thicknesses
        double layer_z_surface; // Read from interface
        double layer_z_increase;
        double tolerance = 0.55; // Standard value

        int n_texture_classes = 5;

        double soildepth_error;

        int[] rainfall_record, evap_record, infil_record, till_record, temp_record;
        int[] rainfall_record_d, evap_record_d, duration_record_d;
        int[] zonesize = new int[22], zoneprogress = new int[22];
        int[]
        iloedge = new int[numberofsinks],
        jloedge = new int[numberofsinks],
        iupedge = new int[numberofsinks],
        jupedge = new int[numberofsinks],
        depressionsize = new int[numberofsinks],
        depressionconsidered = new int[numberofsinks],
        rowlowestnb = new int[maxlowestnbs],
        collowestnb = new int[maxlowestnbs];
        double available_for_delta_kg = 0;
        double available_for_delta_m = 0;

        int t, t_intervene, scenario, number_of_data_cells, run_number, nodata_value, large_negative_number = -111111;
        bool crashed,
            creep_active,
            water_ero_active,
            tillage_active,
            landslide_active,
            bedrock_weathering_active,
            frost_weathering_active,
            tilting_active,
            uplift_active,
            soil_phys_weath_active,
            soil_chem_weath_active,
            soil_bioturb_active,
            soil_clay_transloc_active,
            soil_carbon_active,
            input_data_error,
            memory_records,
            new_glacier,   
            memory_records_d;

        int num_out,
                ntr,				//WVG 22-10-2010 number of rows (timesteps) in profile timeseries matrices			
                cross1, 			//WVG 22-10-2010 rows (or in the future columns) of which profiles are wanted
                cross2,
                cross3,
                test,
                numfile,
                nr,
                nc,
                row,
                col,
                i,
                j,
                matrixresult,
                er_ifile,
                flat,
                low,
                high,
                equal,
                alpha,
                beta,
                temp,
                num_str,
                numsinks,
                nb_ok,
                direction,
                round,
                s_ch,
                numtel,
                S1_error,
                S2_error,
                cell_lock,
                tel1,
                tel2,
                tel3,
                tel4,
                depressions_delta,
                depressions_alone,
                depressions_filled,  //counters for logging and reporting # of depressions filled/sedimented into/left alone
                rr,
                rrr,
                cc,
                ccc,
                ii,
                jj,
                twoequals,      // the -equals are counters for different types of sinks
                threeequals,
                moreequals,
                nb_check,
                depressionnumber = 0,
                maxdepressionnumber,
                depressionready,
                iloradius, iupradius,
                jloradius, jupradius,
                nbismemberofdepression,
                z,
                otherdepression,
                otherdepressionsize,
                totaldepressions,
                totaldepressionsize,
                maxsize,
                lowestneighbourcounter,
                numberoflowestneighbours,
                depressionreallyready,
                depressiondrainsout,
                largestdepression,
                rememberrow,
                remembercol,
                search,
                twice_dtm_fill,
                once_dtm_fill,
                three_dtm_fill,
                xrow, xcol, prev_row, prev_col,
                landuse_value,
                graphics_scale = 2,
                number_of_outputs = 0,
                wet_cells, eroded_cells, deposited_cells,
                P_scen,
                landslide_initiation_cells = 0,
                landslide_continuation_cells = 0,
                landslide_deposition_cells = 0,
                age_rast_value, //Proglacial //Value of age raster for proglacial mode
                age_rast_min, //Proglacial //Min value of age raster for proglacial mode
                age_rast_max, //Proglacial //Max value of age raster for proglacial mode
                rast_val, //Proglacial 
                loopnr_min, //Proglacial
                loopnr_max, //Proglacial
                NumParallelThreads;

        //calibration globals
        int maxruns, best_run, calib_levels, user_specified_number_of_calibration_parameters, user_specified_number_of_ratios;
        double reduction_factor, best_error;
        //USER INPUT NEEDED: establish best versions of parameters varied in calibration:
        double[] best_parameters;
        double[,] calib_ratios;
        private Button timeseries_button;
        private System.Windows.Forms.Label label79_cn;
        private System.Windows.Forms.Label label78_cn;
        private TextBox C14_decay_textbox;
        private TextBox isC14_sp_input_textbox;
        private System.Windows.Forms.Label label33_cn;
        private System.Windows.Forms.Label label130;
        private System.Windows.Forms.Label label133_cn;
        private TextBox attenuationlength_sp_textbox;
        private System.Windows.Forms.Label label136_cn;
        private TextBox isC14_inherited_textbox;
        private TextBox isBe10_inherited_textbox;
        private System.Windows.Forms.Label label134;
        private TextBox metBe10_inherited_textbox;
        private System.Windows.Forms.Label label137;
        private TextBox OSL_inherited_textbox;
        private System.Windows.Forms.Label label_met10Be_dd;
        private TextBox met10Be_dd;
        private System.Windows.Forms.Label label138_CN;
        private TextBox met_10Be_clayfrac;
        private System.Windows.Forms.Label label131_cn;
        private TextBox attenuationlength_mu_textbox;
        private System.Windows.Forms.Label label1310_cn;
        private System.Windows.Forms.Label label132_cn;
        private TextBox isC14_mu_input_textbox;
        private TextBox isBe10_mu_input_textbox;
        private System.Windows.Forms.Label label780_cn;
        double[] original_ratios;

        // tectonics
        int lift_type, lift_location, tilt_location;
        int[] timeseries_order = new int[34];
        //long scan_lon, scan_cnt, NRO, NCO;


        double
                potential_creep_kg_m2_y,
                plough_depth,
                annual_weathering,
                dh, diff, dh1, dh_maxi,
                scan_do, dcount, powered_slope_sum,
                dmax, dmin,
                max_allowed_erosion,			// maximum erosion down to neighbour
                maximum_allowed_deposition, dhtemp,
                CSIZE,
                transport_capacity_kg,			// Capacity 
                detachment_rate,
                settlement_rate,
                frac_sed,   // fraction of landslide deposition into lower grids
                frac_bud_m,
                startsed,
                strsed,     // sediment delivered to streams
                T_act,         // Transmissivity
                bulkd_act,     // Bulk Density
                intfr_act,     // Internal Friction Angle
                C_act,         // Combined Cohesion
                erotot_m,      // total landslide erosion
                sedtot,     // total landslide deposition;
                a_ifr, a_coh, a_bd, a_T,  // parameters parent material 1
                b_coh, b_ifr, b_bd, b_T,  // parameters parent material 2
                c_coh, c_ifr, c_bd, c_T,  // parameters parent material 3
                d_coh, d_ifr, d_bd, d_T,  // parameters parent material 4
                e_coh, e_ifr, e_bd, e_T,  // parameters parent material 5
                slopelim,       // slope limit for landslide erosion                          FACTOR 1
                celfrac,        // fraction used in calculation of celdistance (0.4 default)  FACTOR 2
                streamca,       // contributing area, number of cells, for stream development FACTOR 3
                rainfall_intensity,      // threshold critical rainfall value for landslide scenario   FACTOR 4
                dh_tot_m,
                tra_di,
                set_di,
                dx, dy,	  		// grid size in both row and col
                xcoord, ycoord,
                d_x, dh_tol,
                dt = 1,				// time step
                actual_t,      // Time counter for loop
                end_time,      // Total end time of loop
                out_t,
                total_altitude_m,
                total_average_altitude_m,
                total_rain_m, total_evap_m, total_infil_m,
                total_rain_m3, total_evap_m3, total_infil_m3, total_outflow_m3;
        private TabPage Experimental;
        private TabControl tabControl2;
        private TabPage physical;
        private TextBox physical_weath_constant2;
        private TextBox physical_weath_constant1;
        private TextBox Physical_weath_C1_textbox;
        private CheckBox soil_phys_weath_checkbox;
        private TabPage chemical;
        private TextBox chem_weath_specific_coefficient_textbox;
        private TextBox chem_weath_depth_constant_textbox;

       

        private void explain_input_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Input filenames are not available when both f(x,y) and f(t) are unchecked. In that case, \r\nonly single values are input\r\nLORICA will use filename in the following way:\r\n\r\n1. When f(x,y) is checked but f(t) is not checked, filename is the ascii grid that will be read.\r\nExample: filename = example.asc ; LORICA will read example.asc\r\n\r\n2. When f(x,y) and f(t) are checked, filename is the prefix for \r\na series of ascii grid files with the timestep following the prefix. \r\nExample: filename = example.asc ; LORICA will read example1.asc, example2.asc, example3.asc etc\r\n\r\n3. When f(x,y) is not checked, but f(t) is checked, filename is the text file containing \r\n(spatially uniform) timeseries. The number of values in this file should at least equal \r\nthe number of timesteps in the run. LORICA will start using the first value");
        }

        private TextBox chem_weath_rate_constant_textbox;

       

        private CheckBox soil_chem_weath_checkbox;
        private TabPage clay;
        private Label label_layer_thickness;
        private Label label_thickness_ratio;
        private TabPage tabPage1;
        private CheckBox Proglacial_checkbox;
        private TextBox proglacial_input_filename_textbox;
        private Label Proglacial_text;
        private Label label37;

        private void label37_Click(object sender, EventArgs e)
        {

        }

        private TextBox textbox_layer_thickness;

        private void label_thickness_ratio_Click(object sender, EventArgs e)
        {

        }

        private void textbox_layer_thickness_increase_TextChanged(object sender, EventArgs e)
        {

        }

        private void label_layer_thickness_Click(object sender, EventArgs e)
        {

        }

        private TextBox textbox_layer_thickness_increase;
        private CheckBox ct_Jagercikova;

        

        private void groupBox3_Enter(object sender, EventArgs e)
        {

        }

        private Label label109;
        private Label label108;
        private TextBox ct_dd_Jagercikova;
        private TextBox ct_v0_Jagercikova;
        private TextBox ct_depth_decay;
        private TextBox eluviation_coefficient_textbox;
        private TextBox maximum_eluviation_textbox;
        private TextBox clay_neoform_C2_textbox;
        private TextBox clay_neoform_C1_textbox;
        private TextBox clay_neoform_constant_textbox;
        private CheckBox CT_depth_decay_checkbox;
        private CheckBox soil_clay_transloc_checkbox;
        private TabPage bioturbation;
        private TextBox bt_depth_decay_textbox;
        private TextBox potential_bt_mixing_textbox;
        private CheckBox soil_bioturb_checkbox;
        private TabPage carbon;
        private TextBox carbon_o_decomp_rate_textbox;
        private TextBox carbon_y_decomp_rate_textbox;
        private TextBox carbon_o_depth_decay_textbox;
        private TextBox carbon_y_depth_decay_textbox;
        private TextBox carbon_humification_fraction_textbox;
        private TextBox carbon_depth_decay_textbox;
        private TextBox carbon_input_textbox;
        private CheckBox soil_carbon_cycle_checkbox;
        private TabControl Process_tabs;
        private TabPage Water;
        private Label label87;
        private TextBox selectivity_constant_textbox;
        private TextBox bio_protection_constant_textbox;
        private TextBox erosion_threshold_textbox;
        private TextBox rock_protection_constant_textbox;
        private TextBox parameter_n_textbox;
        private TextBox parameter_conv_textbox;
        private TextBox parameter_K_textbox;
        private TextBox parameter_m_textbox;
        private Label label90;
        private Label label91;
        private Label label92;
        private CheckBox only_waterflow_checkbox;
        private PictureBox pictureBox1;
        private Label label12;
        private Label label11;
        private Label label10;
        private Label label9;
        private CheckBox Water_ero_checkbox;
        private TabPage Tillage;
        private PictureBox pictureBox2;
        private Label label20;
        private Label trte;
        private TextBox parameter_tillage_constant_textbox;
        private TextBox parameter_ploughing_depth_textbox;
        private CheckBox Tillage_checkbox;
        private TabPage Creeper;
        private CheckBox creep_testing;
        private PictureBox pictureBox3;
        private Label label19;
        private TextBox parameter_diffusivity_textbox;
        private CheckBox creep_active_checkbox;
        private Label label36;
        private RadioButton radio_ls_fraction;
        private RadioButton radio_ls_absolute;
        private Label label35;
        private Label label34;
        private TextBox text_ls_rel_rain_intens;
        private TextBox textBox_ls_trans;
        private TextBox textBox_ls_bd;
        private TextBox textBox_ls_ifr;
        private TextBox textBox_ls_coh;
        private TextBox text_ls_abs_rain_intens;
        private Label label32;
        private Label label31;
        private Label label30;
        private Label label22;
        private Label label18;
        private PictureBox pictureBox4;
        private CheckBox Landslide_checkbox;
        private TabPage Tectonics;
        private GroupBox groupBox14;
        private GroupBox groupBox16;
        private TextBox text_lift_col_less;
        private TextBox text_lift_col_more;
        private TextBox text_lift_row_less;
        private TextBox text_lift_row_more;
        private RadioButton radio_lift_col_less_than;
        private RadioButton radio_lift_row_more_than;
        private RadioButton radio_lift_col_more_than;
        private RadioButton radio_lift_row_less_than;
        private TextBox Uplift_rate_textbox;
        private CheckBox uplift_active_checkbox;
        private Label label39;
        private GroupBox groupBox4;
        private Label label38;
        private TextBox Tilting_rate_textbox;
        private GroupBox groupBox15;
        private RadioButton radio_tilt_col_max;
        private RadioButton radio_tilt_row_zero;
        private RadioButton radio_tilt_col_zero;
        private RadioButton radio_tilt_row_max;
        private CheckBox tilting_active_checkbox;
        private TabControl tabControl4;
        private TabPage blocks;
        private Label label129;
        private Label label128;
        private Label label127;
        private TextBox blockweath_textbox;
        private TextBox blocksize_textbox;
        private TextBox hardlayerweath_textbox;
        private TextBox hardlayerdensity_textbox;
        private TextBox hardlayerelevation_textbox;
        private TextBox hardlayerthickness_textbox;
        private Label label63;
        private Label label62;
        private Label label61;
        private CheckBox blocks_active_checkbox;
        private TabPage Rock_weathering;
        private ComboBox rockweath_method;
        private PictureBox pictureBox6;
        private GroupBox groupBox10;
        private CheckBox Frost_weathering_checkbox;
        private GroupBox groupBox9;
        private TextBox parameter_k1_textbox;
        private Label label24;
        private Label label26;
        private Label label27;
        private Label label28;
        private TextBox parameter_k2_textbox;
        private TextBox parameter_Pa_textbox;
        private TextBox parameter_P0_textbox;
        private Label label21;
        private CheckBox Biological_weathering_checkbox;
        private TabPage Hydro;
        private CheckBox check_scaling_daily_weather;
        private Label label106;
        private TextBox snow_threshold_textbox;
        private Label label105;
        private TextBox snowmelt_factor_textbox;
        private Label label104;
        private Label latitude_min;
        private Label label103;
        private TextBox latitude_deg;
        private Label label100;
        private Label label101;
        private Label label102;
        private TextBox dailyT_min;
        private TextBox dailyT_max;
        private TextBox dailyT_avg;
        private Label label97;
        private TextBox daily_n;
        private Label label96;
        private Label label93;
        private Label label89;
        private Label label40;
        private TextBox dailyET0;
        private TextBox dailyD;
        private TextBox dailyP;
        private TabPage Options;
        private CheckBox dtm_iterate_checkbox;
        private CheckBox luxlitter_checkbox;
        private CheckBox version_Konza_checkbox;
        private Button button4;
        private CheckBox version_lux_checkbox;
        private GroupBox groupBox2;
        private TextBox num_cal_paras_textbox;
        private Label label33;
        private TextBox obsfile_textbox;
        private TextBox calibration_ratio_reduction_parameter_textbox;
        private Label label119;
        private TextBox calibration_levels_textbox;
        private Label label116;
        private Label label118;
        private Label label117;
        private Label label115;
        private Label label114;
        private RadioButton Calibration_button;
        private Label label113;
        private TextBox calibration_ratios_textbox;
        private CheckBox calibration;
        private GroupBox groupBox8;
        private CheckBox fill_sinks_during_checkbox;
        private CheckBox fill_sinks_before_checkbox;
        private TabPage treefall;
        private TextBox tf_freq;
        private TextBox tf_age;
        private TextBox tf_growth;
        private TextBox tf_D;
        private TextBox tf_W;
        private Label label112;
        private Label label111;
        private Label label110;
        private Label label95;
        private Label label107;
        private CheckBox treefall_checkbox;
        private CheckBox depressions_output_checkbox;
        private GroupBox groupBox5;
        private GroupBox groupBox13;
        private GroupBox groupBox17;
        private CheckBox daily_water;
        private ComboBox som_cycle_algorithm_box;
        private TextBox potential_bt_mounding_textbox;
        private ComboBox bt_depthfunction_box;
        private CheckBox CarboZALF_calib_stabilizationages_checkbox;
        private CheckBox version_CarboZALF_checkbox;
        private CheckBox version_bioturbation_Checkbox;
        private GroupBox groupBox3;
        private CheckBox Spitsbergen_case_study;

        private void obsfile_textbox_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.InitialDirectory = workdir;
            //openFileDialog1.Filter = "Ascii grids (*.asc)|*.asc|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.RestoreDirectory = false;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                obsfile_textbox.Text = openFileDialog1.FileName;
            }
        }

        private void timeseries_button_Click_1(object sender, EventArgs e)
        {
            timeseries.Visible = true;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Debug.Write(" merely_calculating_derivatives");
            merely_calculating_derivatives = true;
            try { calculate_terrain_derivatives(); MessageBox.Show("terrain derivatives calculation succeeded"); }
            catch { MessageBox.Show("terrain derivatives calculation failed"); }
        }

        private void dailyT_max_TextChanged(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.InitialDirectory = workdir;
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.RestoreDirectory = false;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                dailyT_max.Text = openFileDialog1.FileName;
            }

        }

        private void dailyT_min_TextChanged(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.InitialDirectory = workdir;
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.RestoreDirectory = false;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                dailyT_min.Text = openFileDialog1.FileName;
            }

        }

        private void dailyT_avg_TextChanged(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.InitialDirectory = workdir;
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.RestoreDirectory = false;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                dailyT_avg.Text = openFileDialog1.FileName;
            }

        }

        private void daily_water_CheckedChanged(object sender, EventArgs e)
        {
            dailyP.Enabled = (daily_water.CheckState == CheckState.Checked);
            dailyET0.Enabled = (daily_water.CheckState == CheckState.Checked);
            dailyD.Enabled = (daily_water.CheckState == CheckState.Checked);
            daily_n.Enabled = (daily_water.CheckState == CheckState.Checked);
            dailyT_avg.Enabled = (daily_water.CheckState == CheckState.Checked);
            dailyT_min.Enabled = (daily_water.CheckState == CheckState.Checked);
            dailyT_max.Enabled = (daily_water.CheckState == CheckState.Checked);
            latitude_deg.Enabled = (daily_water.CheckState == CheckState.Checked);
            latitude_min.Enabled = (daily_water.CheckState == CheckState.Checked);
            snowmelt_factor_textbox.Enabled = (daily_water.CheckState == CheckState.Checked);
            snow_threshold_textbox.Enabled = (daily_water.CheckState == CheckState.Checked);
        }

        //soil timeseries_variables
        double total_average_soilthickness_m,
            total_phys_weathered_mass_kg,
            total_chem_weathered_mass_kg,
            total_fine_neoformed_mass_kg,
            total_fine_eluviated_mass_kg,
            total_mass_bioturbed_kg,
            total_OM_input_kg,
        local_soil_depth_m,
        local_soil_mass_kg;
        int number_soil_thicker_than,
        number_soil_coarser_than;

        // Water erosion and deposition parameters
        double
        advection_erodibility,
        P_act,
        m, n,				        // capacity slope and discharge exponents
        erosion_threshold_kg,
        rock_protection_constant,
        bio_protection_constant,
        constant_selective_transcap,
        slope_tan,			            // Gradient
        conv_fac,		            // convergence/divergence factor
        dS, desired_change, dztot,	// Difference in sediment/deposition/erosion
        sedtr_loc,                  // Local sediment transport rate
        all_grids,
        fraction,	                // fraction slope by slopesum
        frac_dis,	                // fraction of discharge into lower grid
        sediment_transported,		// fraction of transport rate
        water_out,
        unfulfilled_change, dz_left1, actual_change, 	// unfulfilled sedimentation
        dz_min, mmin,
        dz_max, maxx,		        // maximum lowest neighbour, steepest descent
        dz_bal, dz_bal2,		    // dz balans counter
        sedbal, sedbal2,
        erobal, erobal2,
        erobalto, sedbalto,
        erocnt,
        sedcnt,
        sediment_exported_m,		        // sediment out of our system
        total_Bolsena_sed_influx,

        // Biological weathering parameters  see Minasny and McBratney 2006 Geoderma 133
        P0,                         // m t-1  // weathering rate constant
        k1,                         // t-1
        k2,                         // t-1
        Pa,                         // m t-1  // weathering rate when soildepth = 0

        // Tilting and Uplift parameters
        tilt_intensity, lift_intensity,

        // Tillage parameter
        tilc;

        //Landslide parameters
        double rain_intensity_m_d;

        // Tree fall parameters
        double W_m_max, D_m_max, W_m, D_m, tf_frequency;
        int growth_a_max, age_a_max;

        //Soil physical weathering parameters
        double physical_weathering_constant, weathered_mass_kg, phys_weath_decay_depth_m, Ctwo;
        double[] upper_particle_size = new double[5];

        //Soil chemical weathering parameters
        double chemical_weathering_constant, chem_weath_decay_depth_m, Cfour, Cfive, Csix, neoform_constant;
        double[] specific_area = new double[5];

        //Clay translocation parameters
        double max_eluviation, Cclay, ct_depthdec;

        //Bioturbation parameters
        double potential_bt_mixing_kg_m2_y, potential_bt_mounding_kg_m2_y, bioturbation_decay_depth_m;
        int bt_depth_function;

        //Carbon cycle parameters
        double potential_OM_input,
               OM_input_decay_depth_m,
               humification_fraction,
               potential_young_decomp_rate,
               potential_old_decomp_rate,
               young_OM_decomp_char_decay_depth_m,
               old_CTI_decay_constant,
               old_OM_decomp_char_decay_depth_m,
               young_CTI_decay_constant;
        int som_cycle_algorithm;

        // Decalcification parameters
        double[,,] CO3_kg;   // CaCO3, to track decalcification speed. Does not contribute to texture or soil mass (yet) MM
        double ini_CO3_content_frac;

        double noval,
        sediment_filled_m, depressionvolume_filled_m, sediment_delta_m,   // counters for logging and reporting the filling of depressions
        altidiff, minaltidiff,
        totaldepressionvolume,
        infil_value_m, evap_value_m, rain_value_m, soildepth_value,
        volume_eroded_m, volume_deposited_m,
        sum_normalweathered, sum_frostweathered, sum_soildepth, sum_creep, sum_solif, avg_solif, avg_creep, avg_soildepth,
        sum_ls, total_sum_tillage, total_sum_uplift, total_sum_tilting, total_sed_export;  // counters for logging and reporting through time

        int temp_value_C;

        double depressionsum_sediment_m, depressionsum_water_m, depressionsum_YOM_kg, depressionsum_OOM_kg;
        double[] depressionsum_texture_kg;
        double[,] landslidesum_texture_kg;
        double[] landslidesum_thickness_m;
        double[,] landslidesum_OM_kg;
        double needed_to_fill_depression_m, dhoblique, dhobliquemax1, dhobliquemax2, firstalt, secondalt, dtmlowestnb;
        int dhmax_errors, readynum = 0, memberdepressionnotconsidered, depressionnum = 0, currentdepression;
        int lower_nb_exists, breaker = 0, rowlowestobnb, collowestobnb, II = 0, JJ = 0;
        int startrow, startcol, iloradius2, iupradius2, jupradius2, jloradius2, deltasize;
        int readysearching, iloradius3, iupradius3, jupradius3, jloradius3, couldbesink, omikron, omega;
        int tempx, tempy, obnbchanged;
        double sed_delta_size1 = 0, sed_delta_size2 = 0, sed_delta_size3 = 0;

        //  variables for displaying purposes // straight from Tom Coulthard
        double hue = 360.0;		// Ranges between 0 and 360 degrees
        double sat = 0.90;		// Ranges between 0 and 1.0 (where 1 is 100%)
        double val = 1.0;		// Ranges between 0 and 1.0 (where 1 is 100%)
        double red = 0.0;
        double green = 0.0;
        double blue = 0.0;

        string basetext = "LORICA Landscape Evolution Model";
        string cfgname = null;  //Config file name
        string workdir;
        string timeseries_string = null;

        double[] depressionlevel = new double[numberofsinks],
                    depressionvolume_m = new double[numberofsinks];
        //double SuperMEF = 0, SuperMEF2 = 0;
        double s_tempfactor, s_D, V_factor;
        double c_D;
        double w_P0, w_k1, w_k2, w_Pa;
        double f_soilrate, f_Tmax, f_Tmin, f_max;

        double mem_m;  //the height by which all cells of a current delta need to be raised in order to get rid of the remaining amount of sediment for that delta

        string str, filename, logname, recordname, outfile, f_name, ch, chs;

        string[] inputheader = new string[6];
        double[,] climate_data;

        int diagnostic_mode = 0;
        int number_of_outflow_cells;
        double[] domain_sed_export_kg = new double[5];
        double domain_OOM_export_kg;
        double domain_YOM_export_kg;

        //HARDLAYER AND BLOCK GLOBALS
        int blocks_active = 0;
        int nhardlayers = 1;
        int hardlayerthickness_m = 1;
        int hardlayerelevation_m = 151;
        int hardlayerdensity_kg_m3 = 2500;
        double hardlayer_weath_contrast = 0.2;
        float blockweatheringratio = 0.999f;
        float blocksizethreshold_m = 0.1f;

        //tracking parameters
        double topoconttoroll = 0, creepconttoroll = 0;
        double blocksrolled = 0, blocksproduced = 0;

        //needs to be initialized:
        float[,] hardlayeropenness_fraction;

        public class Block
        {
            public float Y_row { get; set; }
            public float X_col { get; set; }
            public float Size_m { get; set; }
            public double Accumulated_creep_m_0 { get; set; }
            public double Accumulated_creep_m_1 { get; set; }
            public double Accumulated_creep_m_2 { get; set; }
            public double Accumulated_creep_m_3 { get; set; }
            public double Accumulated_creep_m_4 { get; set; }
            public double Accumulated_creep_m_5 { get; set; }
            public double Accumulated_creep_m_6 { get; set; }
            public double Accumulated_creep_m_7 { get; set; }
            //defined with 0 = north, 1 = northeast etc

            public Block(float y_row, float x_col, float size_m,
                double accumulated_creep_m_0,
                double accumulated_creep_m_1,
                double accumulated_creep_m_2,
                double accumulated_creep_m_3,
                double accumulated_creep_m_4,
                double accumulated_creep_m_5,
                double accumulated_creep_m_6,
                double accumulated_creep_m_7)
            {
                Y_row = y_row; X_col = x_col; Size_m = size_m;
                Accumulated_creep_m_0 = accumulated_creep_m_0;
                Accumulated_creep_m_1 = accumulated_creep_m_1;
                Accumulated_creep_m_2 = accumulated_creep_m_2;
                Accumulated_creep_m_3 = accumulated_creep_m_3;
                Accumulated_creep_m_4 = accumulated_creep_m_4;
                Accumulated_creep_m_5 = accumulated_creep_m_5;
                Accumulated_creep_m_6 = accumulated_creep_m_6;
                Accumulated_creep_m_7 = accumulated_creep_m_7;
            }
        }

        List<Block> Blocklist = new List<Block>();

        public class Lakecell
        {
            public Int32 trow { get; set; }
            public Int32 tcol { get; set; }
            public double t_sed_needed_m { get; set; }
            public double t_new_elev_m { get; set; }
            public Lakecell(Int32 row, Int32 col, double sed_needed, double new_elev)
            {
                trow = row; tcol = col; t_sed_needed_m = sed_needed; t_new_elev_m = new_elev;
            }

        }
        List<Lakecell> L_lakecells = new List<Lakecell>();

        #endregion

        public Mother_form()
        {
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();

            //
            // TODO: Add any constructor code after InitializeComponent call
            //
            getProcessorCoreCount();
        }
        private void getProcessorCoreCount()
        {
            int coreCount = 0;
            foreach (var item in new System.Management.ManagementObjectSearcher("Select * from Win32_Processor").Get())
            {
                coreCount += int.Parse(item["NumberOfCores"].ToString());
            }
            Debug.WriteLine("Number Of Cores: {0}", coreCount);
            Debug.WriteLine("The number of processors on this computer is {0}.", Environment.ProcessorCount);
        }

        static bool checkregionalformat()
        {
            // Get the current culture of the system
            CultureInfo currentCulture = CultureInfo.CurrentCulture;

            // Get the default decimal separator 
            string decimalSeparator = currentCulture.NumberFormat.CurrencyDecimalSeparator;

            // Return whether the decimal separator is a dot
            return decimalSeparator == ".";
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.Label label6;
            System.Windows.Forms.Label label76;
            System.Windows.Forms.Label label75;
            System.Windows.Forms.Label label74;
            System.Windows.Forms.Label label81;
            System.Windows.Forms.Label label77;
            System.Windows.Forms.Label label80;
            System.Windows.Forms.Label label82;
            System.Windows.Forms.Label label83;
            System.Windows.Forms.Label label84;
            System.Windows.Forms.Label label86;
            System.Windows.Forms.Label label_bt_mixing_rate;
            System.Windows.Forms.Label label_bt_depthdecay;
            System.Windows.Forms.Label label68;
            System.Windows.Forms.Label label60;
            System.Windows.Forms.Label label67;
            System.Windows.Forms.Label label66;
            System.Windows.Forms.Label label65;
            System.Windows.Forms.Label label64;
            System.Windows.Forms.Label label72;
            System.Windows.Forms.Label eluviation_rate_constant;
            System.Windows.Forms.Label label70;
            System.Windows.Forms.Label label69;
            System.Windows.Forms.Label label13;
            System.Windows.Forms.Label label52;
            System.Windows.Forms.Label label51;
            System.Windows.Forms.Label label50;
            System.Windows.Forms.Label label53;
            System.Windows.Forms.Label label41;
            System.Windows.Forms.Label label42;
            System.Windows.Forms.Label label43;
            System.Windows.Forms.TabPage Landsliding;
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Mother_form));
            System.Windows.Forms.Label SOM_algorthm_textbox;
            System.Windows.Forms.Label label_bt_mounding_rate;
            System.Windows.Forms.Label label_bt_depthfunction;
            this.label36 = new System.Windows.Forms.Label();
            this.radio_ls_fraction = new System.Windows.Forms.RadioButton();
            this.radio_ls_absolute = new System.Windows.Forms.RadioButton();
            this.label35 = new System.Windows.Forms.Label();
            this.label34 = new System.Windows.Forms.Label();
            this.text_ls_rel_rain_intens = new System.Windows.Forms.TextBox();
            this.textBox_ls_trans = new System.Windows.Forms.TextBox();
            this.textBox_ls_bd = new System.Windows.Forms.TextBox();
            this.textBox_ls_ifr = new System.Windows.Forms.TextBox();
            this.textBox_ls_coh = new System.Windows.Forms.TextBox();
            this.text_ls_abs_rain_intens = new System.Windows.Forms.TextBox();
            this.label32 = new System.Windows.Forms.Label();
            this.label31 = new System.Windows.Forms.Label();
            this.label30 = new System.Windows.Forms.Label();
            this.label22 = new System.Windows.Forms.Label();
            this.label18 = new System.Windows.Forms.Label();
            this.pictureBox4 = new System.Windows.Forms.PictureBox();
            this.Landslide_checkbox = new System.Windows.Forms.CheckBox();
            this.mainMenu1 = new System.Windows.Forms.MainMenu(this.components);
            this.menuItemConfigFile = new System.Windows.Forms.MenuItem();
            this.menuItemConfigFileOpen = new System.Windows.Forms.MenuItem();
            this.menuItemConfigFileSaveAs = new System.Windows.Forms.MenuItem();
            this.menuItemConfigFileSave = new System.Windows.Forms.MenuItem();
            this.Menu_About_box = new System.Windows.Forms.MenuItem();
            this.statusBar1 = new System.Windows.Forms.StatusBar();
            this.InfoStatusPanel = new System.Windows.Forms.StatusBarPanel();
            this.TimeStatusPanel = new System.Windows.Forms.StatusBarPanel();
            this.ScenarioStatusPanel = new System.Windows.Forms.StatusBarPanel();
            this.start_button = new System.Windows.Forms.Button();
            this.End_button = new System.Windows.Forms.Button();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.label2 = new System.Windows.Forms.Label();
            this.label25 = new System.Windows.Forms.Label();
            this.label14 = new System.Windows.Forms.Label();
            this.label15 = new System.Windows.Forms.Label();
            this.label17 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.constant_value_label = new System.Windows.Forms.Label();
            this.label29 = new System.Windows.Forms.Label();
            this.groupBox_landuse = new System.Windows.Forms.GroupBox();
            this.landuse_determinator_button = new System.Windows.Forms.Button();
            this.checkbox_t_intervene = new System.Windows.Forms.CheckBox();
            this.groupBox9 = new System.Windows.Forms.GroupBox();
            this.parameter_k1_textbox = new System.Windows.Forms.TextBox();
            this.label24 = new System.Windows.Forms.Label();
            this.label26 = new System.Windows.Forms.Label();
            this.label27 = new System.Windows.Forms.Label();
            this.label28 = new System.Windows.Forms.Label();
            this.parameter_k2_textbox = new System.Windows.Forms.TextBox();
            this.parameter_Pa_textbox = new System.Windows.Forms.TextBox();
            this.parameter_P0_textbox = new System.Windows.Forms.TextBox();
            this.label21 = new System.Windows.Forms.Label();
            this.Biological_weathering_checkbox = new System.Windows.Forms.CheckBox();
            this.fill_sinks_during_checkbox = new System.Windows.Forms.CheckBox();
            this.fill_sinks_before_checkbox = new System.Windows.Forms.CheckBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.label_layer_thickness = new System.Windows.Forms.Label();
            this.label_thickness_ratio = new System.Windows.Forms.Label();
            this.textbox_layer_thickness = new System.Windows.Forms.TextBox();
            this.textbox_layer_thickness_increase = new System.Windows.Forms.TextBox();
            this.Proglacial_text = new System.Windows.Forms.Label();
            this.label37 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.annual_output_checkbox = new System.Windows.Forms.RadioButton();
            this.cumulative_output_checkbox = new System.Windows.Forms.RadioButton();
            this.label5 = new System.Windows.Forms.Label();
            this.explain_input_button = new System.Windows.Forms.Button();
            this.UTMzonebox = new System.Windows.Forms.TextBox();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.label1 = new System.Windows.Forms.Label();
            this.button6 = new System.Windows.Forms.Button();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.Output = new System.Windows.Forms.TabPage();
            this.timeseries_button = new System.Windows.Forms.Button();
            this.groupBox6 = new System.Windows.Forms.GroupBox();
            this.groupBox12 = new System.Windows.Forms.GroupBox();
            this.groupBox11 = new System.Windows.Forms.GroupBox();
            this.Regular_output_checkbox = new System.Windows.Forms.CheckBox();
            this.Final_output_checkbox = new System.Windows.Forms.CheckBox();
            this.Box_years_output = new System.Windows.Forms.TextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.water_output_checkbox = new System.Windows.Forms.CheckBox();
            this.all_process_output_checkbox = new System.Windows.Forms.CheckBox();
            this.Soildepth_output_checkbox = new System.Windows.Forms.CheckBox();
            this.Alt_change_output_checkbox = new System.Windows.Forms.CheckBox();
            this.Altitude_output_checkbox = new System.Windows.Forms.CheckBox();
            this.checkedListBox1 = new System.Windows.Forms.CheckedListBox();
            this.textBox6 = new System.Windows.Forms.TextBox();
            this.UTMsouthcheck = new System.Windows.Forms.CheckBox();
            this.Run = new System.Windows.Forms.TabPage();
            this.groupBox8 = new System.Windows.Forms.GroupBox();
            this.groupBox7 = new System.Windows.Forms.GroupBox();
            this.textbox_t_intervene = new System.Windows.Forms.TextBox();
            this.runs_checkbox = new System.Windows.Forms.RadioButton();
            this.label16 = new System.Windows.Forms.Label();
            this.Number_runs_textbox = new System.Windows.Forms.TextBox();
            this.Input = new System.Windows.Forms.TabPage();
            this.groupBox17 = new System.Windows.Forms.GroupBox();
            this.check_time_evap = new System.Windows.Forms.CheckBox();
            this.check_time_infil = new System.Windows.Forms.CheckBox();
            this.check_time_rain = new System.Windows.Forms.CheckBox();
            this.check_time_till_fields = new System.Windows.Forms.CheckBox();
            this.check_time_landuse = new System.Windows.Forms.CheckBox();
            this.tillfields_constant_textbox = new System.Windows.Forms.TextBox();
            this.tillfields_input_filename_textbox = new System.Windows.Forms.TextBox();
            this.evap_constant_value_box = new System.Windows.Forms.TextBox();
            this.evap_input_filename_textbox = new System.Windows.Forms.TextBox();
            this.infil_constant_value_box = new System.Windows.Forms.TextBox();
            this.infil_input_filename_textbox = new System.Windows.Forms.TextBox();
            this.rainfall_constant_value_box = new System.Windows.Forms.TextBox();
            this.landuse_constant_value_box = new System.Windows.Forms.TextBox();
            this.landuse_input_filename_textbox = new System.Windows.Forms.TextBox();
            this.rain_input_filename_textbox = new System.Windows.Forms.TextBox();
            this.check_space_evap = new System.Windows.Forms.CheckBox();
            this.check_space_infil = new System.Windows.Forms.CheckBox();
            this.check_space_rain = new System.Windows.Forms.CheckBox();
            this.check_space_till_fields = new System.Windows.Forms.CheckBox();
            this.check_space_landuse = new System.Windows.Forms.CheckBox();
            this.groupBox13 = new System.Windows.Forms.GroupBox();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.soil_specify_button = new System.Windows.Forms.Button();
            this.label_max_soil_layers = new System.Windows.Forms.Label();
            this.textbox_max_soil_layers = new System.Windows.Forms.TextBox();
            this.check_space_DTM = new System.Windows.Forms.CheckBox();
            this.soildepth_constant_value_box = new System.Windows.Forms.TextBox();
            this.soildepth_input_filename_textbox = new System.Windows.Forms.TextBox();
            this.dtm_input_filename_textbox = new System.Windows.Forms.TextBox();
            this.check_space_soildepth = new System.Windows.Forms.CheckBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label23 = new System.Windows.Forms.Label();
            this.Geomorph = new System.Windows.Forms.TabPage();
            this.Process_tabs = new System.Windows.Forms.TabControl();
            this.Water = new System.Windows.Forms.TabPage();
            this.label87 = new System.Windows.Forms.Label();
            this.selectivity_constant_textbox = new System.Windows.Forms.TextBox();
            this.bio_protection_constant_textbox = new System.Windows.Forms.TextBox();
            this.erosion_threshold_textbox = new System.Windows.Forms.TextBox();
            this.rock_protection_constant_textbox = new System.Windows.Forms.TextBox();
            this.parameter_n_textbox = new System.Windows.Forms.TextBox();
            this.parameter_conv_textbox = new System.Windows.Forms.TextBox();
            this.parameter_K_textbox = new System.Windows.Forms.TextBox();
            this.parameter_m_textbox = new System.Windows.Forms.TextBox();
            this.label90 = new System.Windows.Forms.Label();
            this.label91 = new System.Windows.Forms.Label();
            this.label92 = new System.Windows.Forms.Label();
            this.only_waterflow_checkbox = new System.Windows.Forms.CheckBox();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.label12 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.Water_ero_checkbox = new System.Windows.Forms.CheckBox();
            this.Tillage = new System.Windows.Forms.TabPage();
            this.pictureBox2 = new System.Windows.Forms.PictureBox();
            this.label20 = new System.Windows.Forms.Label();
            this.trte = new System.Windows.Forms.Label();
            this.parameter_tillage_constant_textbox = new System.Windows.Forms.TextBox();
            this.parameter_ploughing_depth_textbox = new System.Windows.Forms.TextBox();
            this.Tillage_checkbox = new System.Windows.Forms.CheckBox();
            this.Creeper = new System.Windows.Forms.TabPage();
            this.creep_testing = new System.Windows.Forms.CheckBox();
            this.pictureBox3 = new System.Windows.Forms.PictureBox();
            this.label19 = new System.Windows.Forms.Label();
            this.parameter_diffusivity_textbox = new System.Windows.Forms.TextBox();
            this.creep_active_checkbox = new System.Windows.Forms.CheckBox();
            this.Tectonics = new System.Windows.Forms.TabPage();
            this.groupBox14 = new System.Windows.Forms.GroupBox();
            this.groupBox16 = new System.Windows.Forms.GroupBox();
            this.text_lift_col_less = new System.Windows.Forms.TextBox();
            this.text_lift_col_more = new System.Windows.Forms.TextBox();
            this.text_lift_row_less = new System.Windows.Forms.TextBox();
            this.text_lift_row_more = new System.Windows.Forms.TextBox();
            this.radio_lift_col_less_than = new System.Windows.Forms.RadioButton();
            this.radio_lift_row_more_than = new System.Windows.Forms.RadioButton();
            this.radio_lift_col_more_than = new System.Windows.Forms.RadioButton();
            this.radio_lift_row_less_than = new System.Windows.Forms.RadioButton();
            this.Uplift_rate_textbox = new System.Windows.Forms.TextBox();
            this.uplift_active_checkbox = new System.Windows.Forms.CheckBox();
            this.label39 = new System.Windows.Forms.Label();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.label38 = new System.Windows.Forms.Label();
            this.Tilting_rate_textbox = new System.Windows.Forms.TextBox();
            this.groupBox15 = new System.Windows.Forms.GroupBox();
            this.radio_tilt_col_max = new System.Windows.Forms.RadioButton();
            this.radio_tilt_row_zero = new System.Windows.Forms.RadioButton();
            this.radio_tilt_col_zero = new System.Windows.Forms.RadioButton();
            this.radio_tilt_row_max = new System.Windows.Forms.RadioButton();
            this.tilting_active_checkbox = new System.Windows.Forms.CheckBox();
            this.Creep_Checkbox = new System.Windows.Forms.CheckBox();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.Soil = new System.Windows.Forms.TabPage();
            this.tabControl2 = new System.Windows.Forms.TabControl();
            this.Rock_weathering = new System.Windows.Forms.TabPage();
            this.rockweath_method = new System.Windows.Forms.ComboBox();
            this.pictureBox6 = new System.Windows.Forms.PictureBox();
            this.groupBox10 = new System.Windows.Forms.GroupBox();
            this.Frost_weathering_checkbox = new System.Windows.Forms.CheckBox();
            this.physical = new System.Windows.Forms.TabPage();
            this.physical_weath_constant2 = new System.Windows.Forms.TextBox();
            this.physical_weath_constant1 = new System.Windows.Forms.TextBox();
            this.Physical_weath_C1_textbox = new System.Windows.Forms.TextBox();
            this.soil_phys_weath_checkbox = new System.Windows.Forms.CheckBox();
            this.chemical = new System.Windows.Forms.TabPage();
            this.chem_weath_specific_coefficient_textbox = new System.Windows.Forms.TextBox();
            this.chem_weath_depth_constant_textbox = new System.Windows.Forms.TextBox();
            this.chem_weath_rate_constant_textbox = new System.Windows.Forms.TextBox();
            this.soil_chem_weath_checkbox = new System.Windows.Forms.CheckBox();
            this.clay = new System.Windows.Forms.TabPage();
            this.ct_Jagercikova = new System.Windows.Forms.CheckBox();
            this.label109 = new System.Windows.Forms.Label();
            this.label108 = new System.Windows.Forms.Label();
            this.ct_dd_Jagercikova = new System.Windows.Forms.TextBox();
            this.ct_v0_Jagercikova = new System.Windows.Forms.TextBox();
            this.ct_depth_decay = new System.Windows.Forms.TextBox();
            this.eluviation_coefficient_textbox = new System.Windows.Forms.TextBox();
            this.maximum_eluviation_textbox = new System.Windows.Forms.TextBox();
            this.clay_neoform_C2_textbox = new System.Windows.Forms.TextBox();
            this.clay_neoform_C1_textbox = new System.Windows.Forms.TextBox();
            this.clay_neoform_constant_textbox = new System.Windows.Forms.TextBox();
            this.CT_depth_decay_checkbox = new System.Windows.Forms.CheckBox();
            this.soil_clay_transloc_checkbox = new System.Windows.Forms.CheckBox();
            this.bioturbation = new System.Windows.Forms.TabPage();
            this.bt_depthfunction_box = new System.Windows.Forms.ComboBox();
            this.potential_bt_mounding_textbox = new System.Windows.Forms.TextBox();
            this.bt_depth_decay_textbox = new System.Windows.Forms.TextBox();
            this.potential_bt_mixing_textbox = new System.Windows.Forms.TextBox();
            this.soil_bioturb_checkbox = new System.Windows.Forms.CheckBox();
            this.carbon = new System.Windows.Forms.TabPage();
            this.som_cycle_algorithm_box = new System.Windows.Forms.ComboBox();
            this.carbon_o_decomp_rate_textbox = new System.Windows.Forms.TextBox();
            this.carbon_y_decomp_rate_textbox = new System.Windows.Forms.TextBox();
            this.carbon_o_depth_decay_textbox = new System.Windows.Forms.TextBox();
            this.carbon_y_depth_decay_textbox = new System.Windows.Forms.TextBox();
            this.carbon_humification_fraction_textbox = new System.Windows.Forms.TextBox();
            this.carbon_depth_decay_textbox = new System.Windows.Forms.TextBox();
            this.carbon_input_textbox = new System.Windows.Forms.TextBox();
            this.soil_carbon_cycle_checkbox = new System.Windows.Forms.CheckBox();
            this.Geochron = new System.Windows.Forms.TabPage();
            this.tabControl3 = new System.Windows.Forms.TabControl();
            this.tabPage4 = new System.Windows.Forms.TabPage();
            this.label137 = new System.Windows.Forms.Label();
            this.OSL_inherited_textbox = new System.Windows.Forms.TextBox();
            this.label122 = new System.Windows.Forms.Label();
            this.bleachingdepth_textbox = new System.Windows.Forms.TextBox();
            this.label121 = new System.Windows.Forms.Label();
            this.ngrains_textbox = new System.Windows.Forms.TextBox();
            this.OSL_checkbox = new System.Windows.Forms.CheckBox();
            this.tabPage5 = new System.Windows.Forms.TabPage();
            this.label780_cn = new System.Windows.Forms.Label();
            this.label1310_cn = new System.Windows.Forms.Label();
            this.label132_cn = new System.Windows.Forms.Label();
            this.isC14_mu_input_textbox = new System.Windows.Forms.TextBox();
            this.isBe10_mu_input_textbox = new System.Windows.Forms.TextBox();
            this.label131_cn = new System.Windows.Forms.Label();
            this.attenuationlength_mu_textbox = new System.Windows.Forms.TextBox();
            this.label138_CN = new System.Windows.Forms.Label();
            this.met_10Be_clayfrac = new System.Windows.Forms.TextBox();
            this.label_met10Be_dd = new System.Windows.Forms.Label();
            this.met10Be_dd = new System.Windows.Forms.TextBox();
            this.label136_cn = new System.Windows.Forms.Label();
            this.isC14_inherited_textbox = new System.Windows.Forms.TextBox();
            this.isBe10_inherited_textbox = new System.Windows.Forms.TextBox();
            this.label134 = new System.Windows.Forms.Label();
            this.metBe10_inherited_textbox = new System.Windows.Forms.TextBox();
            this.label133_cn = new System.Windows.Forms.Label();
            this.attenuationlength_sp_textbox = new System.Windows.Forms.TextBox();
            this.label130 = new System.Windows.Forms.Label();
            this.label79_cn = new System.Windows.Forms.Label();
            this.label78_cn = new System.Windows.Forms.Label();
            this.C14_decay_textbox = new System.Windows.Forms.TextBox();
            this.isC14_sp_input_textbox = new System.Windows.Forms.TextBox();
            this.label33_cn = new System.Windows.Forms.Label();
            this.label126 = new System.Windows.Forms.Label();
            this.label125 = new System.Windows.Forms.Label();
            this.isBe10_sp_input_textbox = new System.Windows.Forms.TextBox();
            this.Be10_decay_textbox = new System.Windows.Forms.TextBox();
            this.metBe10_input_textbox = new System.Windows.Forms.TextBox();
            this.label124 = new System.Windows.Forms.Label();
            this.label123 = new System.Windows.Forms.Label();
            this.CN_checkbox = new System.Windows.Forms.CheckBox();
            this.Experimental = new System.Windows.Forms.TabPage();
            this.tabControl4 = new System.Windows.Forms.TabControl();
            this.blocks = new System.Windows.Forms.TabPage();
            this.label129 = new System.Windows.Forms.Label();
            this.label128 = new System.Windows.Forms.Label();
            this.label127 = new System.Windows.Forms.Label();
            this.blockweath_textbox = new System.Windows.Forms.TextBox();
            this.blocksize_textbox = new System.Windows.Forms.TextBox();
            this.hardlayerweath_textbox = new System.Windows.Forms.TextBox();
            this.hardlayerdensity_textbox = new System.Windows.Forms.TextBox();
            this.hardlayerelevation_textbox = new System.Windows.Forms.TextBox();
            this.hardlayerthickness_textbox = new System.Windows.Forms.TextBox();
            this.label63 = new System.Windows.Forms.Label();
            this.label62 = new System.Windows.Forms.Label();
            this.label61 = new System.Windows.Forms.Label();
            this.blocks_active_checkbox = new System.Windows.Forms.CheckBox();
            this.Hydro = new System.Windows.Forms.TabPage();
            this.check_scaling_daily_weather = new System.Windows.Forms.CheckBox();
            this.label106 = new System.Windows.Forms.Label();
            this.snow_threshold_textbox = new System.Windows.Forms.TextBox();
            this.label105 = new System.Windows.Forms.Label();
            this.snowmelt_factor_textbox = new System.Windows.Forms.TextBox();
            this.label104 = new System.Windows.Forms.Label();
            this.latitude_min = new System.Windows.Forms.Label();
            this.label103 = new System.Windows.Forms.Label();
            this.latitude_deg = new System.Windows.Forms.TextBox();
            this.label100 = new System.Windows.Forms.Label();
            this.label101 = new System.Windows.Forms.Label();
            this.label102 = new System.Windows.Forms.Label();
            this.dailyT_min = new System.Windows.Forms.TextBox();
            this.dailyT_max = new System.Windows.Forms.TextBox();
            this.dailyT_avg = new System.Windows.Forms.TextBox();
            this.label97 = new System.Windows.Forms.Label();
            this.daily_n = new System.Windows.Forms.TextBox();
            this.label96 = new System.Windows.Forms.Label();
            this.label93 = new System.Windows.Forms.Label();
            this.label89 = new System.Windows.Forms.Label();
            this.label40 = new System.Windows.Forms.Label();
            this.dailyET0 = new System.Windows.Forms.TextBox();
            this.dailyD = new System.Windows.Forms.TextBox();
            this.dailyP = new System.Windows.Forms.TextBox();
            this.treefall = new System.Windows.Forms.TabPage();
            this.tf_freq = new System.Windows.Forms.TextBox();
            this.tf_age = new System.Windows.Forms.TextBox();
            this.tf_growth = new System.Windows.Forms.TextBox();
            this.tf_D = new System.Windows.Forms.TextBox();
            this.tf_W = new System.Windows.Forms.TextBox();
            this.label112 = new System.Windows.Forms.Label();
            this.label111 = new System.Windows.Forms.Label();
            this.label110 = new System.Windows.Forms.Label();
            this.label95 = new System.Windows.Forms.Label();
            this.label107 = new System.Windows.Forms.Label();
            this.treefall_checkbox = new System.Windows.Forms.CheckBox();
            this.Options = new System.Windows.Forms.TabPage();
            this.CarboZALF_calib_stabilizationages_checkbox = new System.Windows.Forms.CheckBox();
            this.daily_water = new System.Windows.Forms.CheckBox();
            this.version_CarboZALF_checkbox = new System.Windows.Forms.CheckBox();
            this.depressions_output_checkbox = new System.Windows.Forms.CheckBox();
            this.luxlitter_checkbox = new System.Windows.Forms.CheckBox();
            this.version_Konza_checkbox = new System.Windows.Forms.CheckBox();
            this.button4 = new System.Windows.Forms.Button();
            this.version_lux_checkbox = new System.Windows.Forms.CheckBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.num_cal_paras_textbox = new System.Windows.Forms.TextBox();
            this.label33 = new System.Windows.Forms.Label();
            this.obsfile_textbox = new System.Windows.Forms.TextBox();
            this.calibration_ratio_reduction_parameter_textbox = new System.Windows.Forms.TextBox();
            this.label119 = new System.Windows.Forms.Label();
            this.calibration_levels_textbox = new System.Windows.Forms.TextBox();
            this.label116 = new System.Windows.Forms.Label();
            this.label118 = new System.Windows.Forms.Label();
            this.label117 = new System.Windows.Forms.Label();
            this.label115 = new System.Windows.Forms.Label();
            this.label114 = new System.Windows.Forms.Label();
            this.Calibration_button = new System.Windows.Forms.RadioButton();
            this.label113 = new System.Windows.Forms.Label();
            this.calibration_ratios_textbox = new System.Windows.Forms.TextBox();
            this.calibration = new System.Windows.Forms.CheckBox();
            this.Spitsbergen_case_study = new System.Windows.Forms.CheckBox();
            this.dtm_iterate_checkbox = new System.Windows.Forms.CheckBox();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.proglacial_input_filename_textbox = new System.Windows.Forms.TextBox();
            this.Proglacial_checkbox = new System.Windows.Forms.CheckBox();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.version_bioturbation_Checkbox = new System.Windows.Forms.CheckBox();
            label6 = new System.Windows.Forms.Label();
            label76 = new System.Windows.Forms.Label();
            label75 = new System.Windows.Forms.Label();
            label74 = new System.Windows.Forms.Label();
            label81 = new System.Windows.Forms.Label();
            label77 = new System.Windows.Forms.Label();
            label80 = new System.Windows.Forms.Label();
            label82 = new System.Windows.Forms.Label();
            label83 = new System.Windows.Forms.Label();
            label84 = new System.Windows.Forms.Label();
            label86 = new System.Windows.Forms.Label();
            label_bt_mixing_rate = new System.Windows.Forms.Label();
            label_bt_depthdecay = new System.Windows.Forms.Label();
            label68 = new System.Windows.Forms.Label();
            label60 = new System.Windows.Forms.Label();
            label67 = new System.Windows.Forms.Label();
            label66 = new System.Windows.Forms.Label();
            label65 = new System.Windows.Forms.Label();
            label64 = new System.Windows.Forms.Label();
            label72 = new System.Windows.Forms.Label();
            eluviation_rate_constant = new System.Windows.Forms.Label();
            label70 = new System.Windows.Forms.Label();
            label69 = new System.Windows.Forms.Label();
            label13 = new System.Windows.Forms.Label();
            label52 = new System.Windows.Forms.Label();
            label51 = new System.Windows.Forms.Label();
            label50 = new System.Windows.Forms.Label();
            label53 = new System.Windows.Forms.Label();
            label41 = new System.Windows.Forms.Label();
            label42 = new System.Windows.Forms.Label();
            label43 = new System.Windows.Forms.Label();
            Landsliding = new System.Windows.Forms.TabPage();
            SOM_algorthm_textbox = new System.Windows.Forms.Label();
            label_bt_mounding_rate = new System.Windows.Forms.Label();
            label_bt_depthfunction = new System.Windows.Forms.Label();
            Landsliding.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox4)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.InfoStatusPanel)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TimeStatusPanel)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ScenarioStatusPanel)).BeginInit();
            this.groupBox_landuse.SuspendLayout();
            this.groupBox9.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.Output.SuspendLayout();
            this.groupBox6.SuspendLayout();
            this.groupBox12.SuspendLayout();
            this.groupBox11.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.Run.SuspendLayout();
            this.groupBox8.SuspendLayout();
            this.groupBox7.SuspendLayout();
            this.Input.SuspendLayout();
            this.groupBox17.SuspendLayout();
            this.groupBox13.SuspendLayout();
            this.groupBox5.SuspendLayout();
            this.Geomorph.SuspendLayout();
            this.Process_tabs.SuspendLayout();
            this.Water.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.Tillage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
            this.Creeper.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox3)).BeginInit();
            this.Tectonics.SuspendLayout();
            this.groupBox14.SuspendLayout();
            this.groupBox16.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.groupBox15.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.Soil.SuspendLayout();
            this.tabControl2.SuspendLayout();
            this.Rock_weathering.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox6)).BeginInit();
            this.groupBox10.SuspendLayout();
            this.physical.SuspendLayout();
            this.chemical.SuspendLayout();
            this.clay.SuspendLayout();
            this.bioturbation.SuspendLayout();
            this.carbon.SuspendLayout();
            this.Geochron.SuspendLayout();
            this.tabControl3.SuspendLayout();
            this.tabPage4.SuspendLayout();
            this.tabPage5.SuspendLayout();
            this.Experimental.SuspendLayout();
            this.tabControl4.SuspendLayout();
            this.blocks.SuspendLayout();
            this.Hydro.SuspendLayout();
            this.treefall.SuspendLayout();
            this.Options.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.SuspendLayout();
            // 
            // label6
            // 
            label6.Location = new System.Drawing.Point(182, 12);
            label6.Name = "label6";
            label6.Size = new System.Drawing.Size(41, 24);
            label6.TabIndex = 109;
            label6.Text = "f(x,y)";
            label6.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.toolTip1.SetToolTip(label6, "Check this for spatially variable inputs");
            // 
            // label76
            // 
            label76.AutoSize = true;
            label76.Location = new System.Drawing.Point(130, 116);
            label76.Name = "label76";
            label76.Size = new System.Drawing.Size(205, 13);
            label76.TabIndex = 62;
            label76.Text = "potential organic matter input [kg / m2 / y]";
            // 
            // label75
            // 
            label75.AutoSize = true;
            label75.Location = new System.Drawing.Point(130, 145);
            label75.Name = "label75";
            label75.Size = new System.Drawing.Size(83, 13);
            label75.TabIndex = 63;
            label75.Text = "decay depth [m]";
            // 
            // label74
            // 
            label74.AutoSize = true;
            label74.Location = new System.Drawing.Point(130, 171);
            label74.Name = "label74";
            label74.Size = new System.Drawing.Size(0, 13);
            label74.TabIndex = 64;
            // 
            // label81
            // 
            label81.AutoSize = true;
            label81.Location = new System.Drawing.Point(130, 171);
            label81.Name = "label81";
            label81.Size = new System.Drawing.Size(113, 13);
            label81.TabIndex = 67;
            label81.Text = "humification fraction [-]";
            // 
            // label77
            // 
            label77.AutoSize = true;
            label77.Location = new System.Drawing.Point(130, 172);
            label77.Name = "label77";
            label77.Size = new System.Drawing.Size(0, 13);
            label77.TabIndex = 69;
            // 
            // label80
            // 
            label80.AutoSize = true;
            label80.Location = new System.Drawing.Point(381, 62);
            label80.Name = "label80";
            label80.Size = new System.Drawing.Size(133, 26);
            label80.TabIndex = 70;
            label80.Text = "decomposition parameters \r\nfor two OM pools:";
            // 
            // label82
            // 
            label82.AutoSize = true;
            label82.Location = new System.Drawing.Point(382, 94);
            label82.Name = "label82";
            label82.Size = new System.Drawing.Size(36, 13);
            label82.TabIndex = 71;
            label82.Text = "young";
            // 
            // label83
            // 
            label83.AutoSize = true;
            label83.Location = new System.Drawing.Point(496, 94);
            label83.Name = "label83";
            label83.Size = new System.Drawing.Size(21, 13);
            label83.TabIndex = 72;
            label83.Text = "old";
            // 
            // label84
            // 
            label84.AutoSize = true;
            label84.Location = new System.Drawing.Point(558, 140);
            label84.Name = "label84";
            label84.Size = new System.Drawing.Size(88, 13);
            label84.TabIndex = 74;
            label84.Text = "decay depths [m]";
            // 
            // label86
            // 
            label86.AutoSize = true;
            label86.Location = new System.Drawing.Point(558, 114);
            label86.Name = "label86";
            label86.Size = new System.Drawing.Size(115, 13);
            label86.TabIndex = 80;
            label86.Text = "decomposition rate [/y]";
            // 
            // label_bt_mixing_rate
            // 
            label_bt_mixing_rate.AutoSize = true;
            label_bt_mixing_rate.Location = new System.Drawing.Point(133, 48);
            label_bt_mixing_rate.Name = "label_bt_mixing_rate";
            label_bt_mixing_rate.Size = new System.Drawing.Size(217, 13);
            label_bt_mixing_rate.TabIndex = 57;
            label_bt_mixing_rate.Text = "potential subsurface mixing rate [kg / m2 / y]";
            // 
            // label_bt_depthdecay
            // 
            label_bt_depthdecay.AutoSize = true;
            label_bt_depthdecay.Location = new System.Drawing.Point(133, 104);
            label_bt_depthdecay.Name = "label_bt_depthdecay";
            label_bt_depthdecay.Size = new System.Drawing.Size(83, 13);
            label_bt_depthdecay.TabIndex = 58;
            label_bt_depthdecay.Text = "decay depth [m]";
            // 
            // label68
            // 
            label68.AutoSize = true;
            label68.Location = new System.Drawing.Point(133, 103);
            label68.Name = "label68";
            label68.Size = new System.Drawing.Size(0, 13);
            label68.TabIndex = 59;
            // 
            // label60
            // 
            label60.AutoSize = true;
            label60.Location = new System.Drawing.Point(23, 59);
            label60.Name = "label60";
            label60.Size = new System.Drawing.Size(110, 13);
            label60.TabIndex = 39;
            label60.Text = "fine clay neoformation";
            // 
            // label67
            // 
            label67.AutoSize = true;
            label67.Location = new System.Drawing.Point(132, 75);
            label67.Name = "label67";
            label67.Size = new System.Drawing.Size(121, 13);
            label67.TabIndex = 43;
            label67.Text = "neoformation constant []";
            // 
            // label66
            // 
            label66.AutoSize = true;
            label66.Location = new System.Drawing.Point(132, 104);
            label66.Name = "label66";
            label66.Size = new System.Drawing.Size(60, 13);
            label66.TabIndex = 44;
            label66.Text = "constant 1 ";
            // 
            // label65
            // 
            label65.AutoSize = true;
            label65.Location = new System.Drawing.Point(132, 130);
            label65.Name = "label65";
            label65.Size = new System.Drawing.Size(0, 13);
            label65.TabIndex = 45;
            // 
            // label64
            // 
            label64.AutoSize = true;
            label64.Location = new System.Drawing.Point(131, 134);
            label64.Name = "label64";
            label64.Size = new System.Drawing.Size(83, 13);
            label64.TabIndex = 46;
            label64.Text = "constant 2 [m-1]";
            // 
            // label72
            // 
            label72.AutoSize = true;
            label72.Location = new System.Drawing.Point(302, 59);
            label72.Name = "label72";
            label72.Size = new System.Drawing.Size(109, 13);
            label72.TabIndex = 47;
            label72.Text = "fine clay translocation";
            // 
            // eluviation_rate_constant
            // 
            eluviation_rate_constant.AutoSize = true;
            eluviation_rate_constant.Location = new System.Drawing.Point(411, 75);
            eluviation_rate_constant.Name = "eluviation_rate_constant";
            eluviation_rate_constant.Size = new System.Drawing.Size(119, 13);
            eluviation_rate_constant.TabIndex = 51;
            eluviation_rate_constant.Text = "maximum eluviation [kg]";
            // 
            // label70
            // 
            label70.AutoSize = true;
            label70.Location = new System.Drawing.Point(411, 104);
            label70.Name = "label70";
            label70.Size = new System.Drawing.Size(97, 13);
            label70.TabIndex = 52;
            label70.Text = "saturation constant";
            // 
            // label69
            // 
            label69.AutoSize = true;
            label69.Location = new System.Drawing.Point(411, 130);
            label69.Name = "label69";
            label69.Size = new System.Drawing.Size(0, 13);
            label69.TabIndex = 53;
            // 
            // label13
            // 
            label13.AutoSize = true;
            label13.Location = new System.Drawing.Point(410, 172);
            label13.Name = "label13";
            label13.Size = new System.Drawing.Size(112, 13);
            label13.TabIndex = 56;
            label13.Text = "Depth decay constant";
            // 
            // label52
            // 
            label52.AutoSize = true;
            label52.Location = new System.Drawing.Point(136, 38);
            label52.Name = "label52";
            label52.Size = new System.Drawing.Size(268, 13);
            label52.TabIndex = 16;
            label52.Text = "weathering rate constant [kg / m2 mineral surface area]";
            // 
            // label51
            // 
            label51.AutoSize = true;
            label51.Location = new System.Drawing.Point(136, 67);
            label51.Name = "label51";
            label51.Size = new System.Drawing.Size(83, 13);
            label51.TabIndex = 17;
            label51.Text = "decay depth [m]";
            // 
            // label50
            // 
            label50.AutoSize = true;
            label50.Location = new System.Drawing.Point(136, 93);
            label50.Name = "label50";
            label50.Size = new System.Drawing.Size(0, 13);
            label50.TabIndex = 18;
            // 
            // label53
            // 
            label53.AutoSize = true;
            label53.Location = new System.Drawing.Point(135, 97);
            label53.Name = "label53";
            label53.Size = new System.Drawing.Size(131, 13);
            label53.TabIndex = 19;
            label53.Text = "specific area coefficient [-]";
            // 
            // label41
            // 
            label41.AutoSize = true;
            label41.Location = new System.Drawing.Point(142, 49);
            label41.Name = "label41";
            label41.Size = new System.Drawing.Size(147, 13);
            label41.TabIndex = 10;
            label41.Text = "weathering rate constant [y-1]";
            // 
            // label42
            // 
            label42.AutoSize = true;
            label42.Location = new System.Drawing.Point(142, 72);
            label42.Name = "label42";
            label42.Size = new System.Drawing.Size(83, 13);
            label42.TabIndex = 11;
            label42.Text = "decay depth [m]";
            // 
            // label43
            // 
            label43.AutoSize = true;
            label43.Location = new System.Drawing.Point(142, 98);
            label43.Name = "label43";
            label43.Size = new System.Drawing.Size(123, 13);
            label43.TabIndex = 12;
            label43.Text = "particle size constant [m]";
            // 
            // Landsliding
            // 
            Landsliding.Controls.Add(this.label36);
            Landsliding.Controls.Add(this.radio_ls_fraction);
            Landsliding.Controls.Add(this.radio_ls_absolute);
            Landsliding.Controls.Add(this.label35);
            Landsliding.Controls.Add(this.label34);
            Landsliding.Controls.Add(this.text_ls_rel_rain_intens);
            Landsliding.Controls.Add(this.textBox_ls_trans);
            Landsliding.Controls.Add(this.textBox_ls_bd);
            Landsliding.Controls.Add(this.textBox_ls_ifr);
            Landsliding.Controls.Add(this.textBox_ls_coh);
            Landsliding.Controls.Add(this.text_ls_abs_rain_intens);
            Landsliding.Controls.Add(this.label32);
            Landsliding.Controls.Add(this.label31);
            Landsliding.Controls.Add(this.label30);
            Landsliding.Controls.Add(this.label22);
            Landsliding.Controls.Add(this.label18);
            Landsliding.Controls.Add(this.pictureBox4);
            Landsliding.Controls.Add(this.Landslide_checkbox);
            Landsliding.Location = new System.Drawing.Point(4, 22);
            Landsliding.Name = "Landsliding";
            Landsliding.Size = new System.Drawing.Size(732, 250);
            Landsliding.TabIndex = 2;
            Landsliding.Text = "Landsliding";
            Landsliding.UseVisualStyleBackColor = true;
            // 
            // label36
            // 
            this.label36.AutoSize = true;
            this.label36.Location = new System.Drawing.Point(49, 115);
            this.label36.Name = "label36";
            this.label36.Size = new System.Drawing.Size(236, 13);
            this.label36.TabIndex = 30;
            this.label36.Text = "Parameters for critical rainfall intensity calculation";
            // 
            // radio_ls_fraction
            // 
            this.radio_ls_fraction.AutoSize = true;
            this.radio_ls_fraction.Checked = true;
            this.radio_ls_fraction.Location = new System.Drawing.Point(53, 83);
            this.radio_ls_fraction.Name = "radio_ls_fraction";
            this.radio_ls_fraction.Size = new System.Drawing.Size(14, 13);
            this.radio_ls_fraction.TabIndex = 29;
            this.radio_ls_fraction.TabStop = true;
            this.radio_ls_fraction.UseVisualStyleBackColor = true;
            // 
            // radio_ls_absolute
            // 
            this.radio_ls_absolute.AutoSize = true;
            this.radio_ls_absolute.Location = new System.Drawing.Point(53, 57);
            this.radio_ls_absolute.Name = "radio_ls_absolute";
            this.radio_ls_absolute.Size = new System.Drawing.Size(14, 13);
            this.radio_ls_absolute.TabIndex = 28;
            this.radio_ls_absolute.UseVisualStyleBackColor = true;
            // 
            // label35
            // 
            this.label35.AutoSize = true;
            this.label35.Enabled = false;
            this.label35.Location = new System.Drawing.Point(138, 57);
            this.label35.Name = "label35";
            this.label35.Size = new System.Drawing.Size(105, 13);
            this.label35.TabIndex = 27;
            this.label35.Text = "Absolute value [m/d]";
            // 
            // label34
            // 
            this.label34.AutoSize = true;
            this.label34.Location = new System.Drawing.Point(138, 83);
            this.label34.Name = "label34";
            this.label34.Size = new System.Drawing.Size(270, 13);
            this.label34.TabIndex = 26;
            this.label34.Text = "Fraction of total annual rainfall [between 1 and 0.00274]";
            // 
            // text_ls_rel_rain_intens
            // 
            this.text_ls_rel_rain_intens.Location = new System.Drawing.Point(79, 80);
            this.text_ls_rel_rain_intens.Name = "text_ls_rel_rain_intens";
            this.text_ls_rel_rain_intens.Size = new System.Drawing.Size(53, 20);
            this.text_ls_rel_rain_intens.TabIndex = 25;
            this.text_ls_rel_rain_intens.Text = "0.1";
            // 
            // textBox_ls_trans
            // 
            this.textBox_ls_trans.Location = new System.Drawing.Point(52, 210);
            this.textBox_ls_trans.Name = "textBox_ls_trans";
            this.textBox_ls_trans.Size = new System.Drawing.Size(53, 20);
            this.textBox_ls_trans.TabIndex = 23;
            this.textBox_ls_trans.Text = "15";
            // 
            // textBox_ls_bd
            // 
            this.textBox_ls_bd.Location = new System.Drawing.Point(52, 184);
            this.textBox_ls_bd.Name = "textBox_ls_bd";
            this.textBox_ls_bd.Size = new System.Drawing.Size(53, 20);
            this.textBox_ls_bd.TabIndex = 21;
            this.textBox_ls_bd.Text = "1.4";
            // 
            // textBox_ls_ifr
            // 
            this.textBox_ls_ifr.Location = new System.Drawing.Point(52, 158);
            this.textBox_ls_ifr.Name = "textBox_ls_ifr";
            this.textBox_ls_ifr.Size = new System.Drawing.Size(53, 20);
            this.textBox_ls_ifr.TabIndex = 19;
            this.textBox_ls_ifr.Text = "0.7";
            // 
            // textBox_ls_coh
            // 
            this.textBox_ls_coh.Location = new System.Drawing.Point(52, 131);
            this.textBox_ls_coh.Name = "textBox_ls_coh";
            this.textBox_ls_coh.Size = new System.Drawing.Size(53, 20);
            this.textBox_ls_coh.TabIndex = 17;
            this.textBox_ls_coh.Text = "0.15";
            // 
            // text_ls_abs_rain_intens
            // 
            this.text_ls_abs_rain_intens.Enabled = false;
            this.text_ls_abs_rain_intens.Location = new System.Drawing.Point(79, 54);
            this.text_ls_abs_rain_intens.Name = "text_ls_abs_rain_intens";
            this.text_ls_abs_rain_intens.Size = new System.Drawing.Size(53, 20);
            this.text_ls_abs_rain_intens.TabIndex = 15;
            this.text_ls_abs_rain_intens.Text = "0.1";
            // 
            // label32
            // 
            this.label32.AutoSize = true;
            this.label32.Location = new System.Drawing.Point(111, 213);
            this.label32.Name = "label32";
            this.label32.Size = new System.Drawing.Size(169, 13);
            this.label32.TabIndex = 24;
            this.label32.Text = "Saturated soil transmissivity [m2/d]";
            // 
            // label31
            // 
            this.label31.AutoSize = true;
            this.label31.Location = new System.Drawing.Point(111, 187);
            this.label31.Name = "label31";
            this.label31.Size = new System.Drawing.Size(105, 13);
            this.label31.TabIndex = 22;
            this.label31.Text = "Bulk density [kg m-3]";
            // 
            // label30
            // 
            this.label30.AutoSize = true;
            this.label30.Location = new System.Drawing.Point(111, 161);
            this.label30.Name = "label30";
            this.label30.Size = new System.Drawing.Size(152, 13);
            this.label30.TabIndex = 20;
            this.label30.Text = "Internal friction angle [degrees]";
            // 
            // label22
            // 
            this.label22.AutoSize = true;
            this.label22.Location = new System.Drawing.Point(111, 134);
            this.label22.Name = "label22";
            this.label22.Size = new System.Drawing.Size(112, 13);
            this.label22.TabIndex = 18;
            this.label22.Text = "Combined cohesion [-]";
            // 
            // label18
            // 
            this.label18.AutoSize = true;
            this.label18.Location = new System.Drawing.Point(50, 38);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(117, 13);
            this.label18.TabIndex = 16;
            this.label18.Text = "Critical rainfall threshold";
            // 
            // pictureBox4
            // 
            this.pictureBox4.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox4.Image")));
            this.pictureBox4.Location = new System.Drawing.Point(480, 57);
            this.pictureBox4.Name = "pictureBox4";
            this.pictureBox4.Size = new System.Drawing.Size(180, 137);
            this.pictureBox4.TabIndex = 14;
            this.pictureBox4.TabStop = false;
            // 
            // Landslide_checkbox
            // 
            this.Landslide_checkbox.AutoSize = true;
            this.Landslide_checkbox.Location = new System.Drawing.Point(26, 14);
            this.Landslide_checkbox.Name = "Landslide_checkbox";
            this.Landslide_checkbox.Size = new System.Drawing.Size(124, 17);
            this.Landslide_checkbox.TabIndex = 1;
            this.Landslide_checkbox.Text = "Activate this process";
            this.Landslide_checkbox.UseVisualStyleBackColor = true;
            // 
            // SOM_algorthm_textbox
            // 
            SOM_algorthm_textbox.AutoSize = true;
            SOM_algorthm_textbox.Location = new System.Drawing.Point(25, 43);
            SOM_algorthm_textbox.Name = "SOM_algorthm_textbox";
            SOM_algorthm_textbox.Size = new System.Drawing.Size(104, 13);
            SOM_algorthm_textbox.TabIndex = 83;
            SOM_algorthm_textbox.Text = "SOM cycle algorithm";
            // 
            // label_bt_mounding_rate
            // 
            label_bt_mounding_rate.AutoSize = true;
            label_bt_mounding_rate.Location = new System.Drawing.Point(134, 74);
            label_bt_mounding_rate.Name = "label_bt_mounding_rate";
            label_bt_mounding_rate.Size = new System.Drawing.Size(179, 13);
            label_bt_mounding_rate.TabIndex = 61;
            label_bt_mounding_rate.Text = "potential mounding rate [kg / m2 / y]";
            // 
            // label_bt_depthfunction
            // 
            label_bt_depthfunction.AutoSize = true;
            label_bt_depthfunction.Location = new System.Drawing.Point(134, 132);
            label_bt_depthfunction.Name = "label_bt_depthfunction";
            label_bt_depthfunction.Size = new System.Drawing.Size(133, 13);
            label_bt_depthfunction.TabIndex = 63;
            label_bt_depthfunction.Text = "bioturbation depth function";
            // 
            // mainMenu1
            // 
            this.mainMenu1.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuItemConfigFile,
            this.Menu_About_box});
            // 
            // menuItemConfigFile
            // 
            this.menuItemConfigFile.Index = 0;
            this.menuItemConfigFile.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuItemConfigFileOpen,
            this.menuItemConfigFileSaveAs,
            this.menuItemConfigFileSave});
            this.menuItemConfigFile.Text = "&Settings";
            // 
            // menuItemConfigFileOpen
            // 
            this.menuItemConfigFileOpen.Index = 0;
            this.menuItemConfigFileOpen.Text = "&Open";
            this.menuItemConfigFileOpen.Click += new System.EventHandler(this.menuItemConfigFileOpen_Click);
            // 
            // menuItemConfigFileSaveAs
            // 
            this.menuItemConfigFileSaveAs.Index = 1;
            this.menuItemConfigFileSaveAs.Text = "Save &As";
            this.menuItemConfigFileSaveAs.Click += new System.EventHandler(this.menuItemConfigFileSave_Click);
            // 
            // menuItemConfigFileSave
            // 
            this.menuItemConfigFileSave.Index = 2;
            this.menuItemConfigFileSave.Text = "&Save";
            this.menuItemConfigFileSave.Click += new System.EventHandler(this.menuItemConfigFileSave_Click);
            // 
            // Menu_About_box
            // 
            this.Menu_About_box.Index = 1;
            this.Menu_About_box.Text = "";
            // 
            // statusBar1
            // 
            this.statusBar1.Location = new System.Drawing.Point(0, 468);
            this.statusBar1.Name = "statusBar1";
            this.statusBar1.Panels.AddRange(new System.Windows.Forms.StatusBarPanel[] {
            this.InfoStatusPanel,
            this.TimeStatusPanel,
            this.ScenarioStatusPanel});
            this.statusBar1.ShowPanels = true;
            this.statusBar1.Size = new System.Drawing.Size(842, 23);
            this.statusBar1.SizingGrip = false;
            this.statusBar1.TabIndex = 144;
            this.statusBar1.Text = "statusBar1";
            // 
            // InfoStatusPanel
            // 
            this.InfoStatusPanel.Name = "InfoStatusPanel";
            this.InfoStatusPanel.Text = "info";
            this.InfoStatusPanel.Width = 200;
            // 
            // TimeStatusPanel
            // 
            this.TimeStatusPanel.Name = "TimeStatusPanel";
            this.TimeStatusPanel.Text = "time";
            this.TimeStatusPanel.Width = 80;
            // 
            // ScenarioStatusPanel
            // 
            this.ScenarioStatusPanel.Name = "ScenarioStatusPanel";
            this.ScenarioStatusPanel.Text = "scen ";
            this.ScenarioStatusPanel.Width = 120;
            // 
            // start_button
            // 
            this.start_button.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.start_button.Location = new System.Drawing.Point(16, 391);
            this.start_button.Name = "start_button";
            this.start_button.Size = new System.Drawing.Size(88, 27);
            this.start_button.TabIndex = 146;
            this.start_button.Text = "Start";
            this.start_button.Click += new System.EventHandler(this.main_loop);
            // 
            // End_button
            // 
            this.End_button.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.End_button.Location = new System.Drawing.Point(110, 391);
            this.End_button.Name = "End_button";
            this.End_button.Size = new System.Drawing.Size(100, 27);
            this.End_button.TabIndex = 147;
            this.End_button.Text = "Quit";
            this.End_button.Click += new System.EventHandler(this.End_button_Click);
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(19, 121);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(104, 24);
            this.label2.TabIndex = 97;
            this.label2.Text = "Rainfall data file";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.toolTip1.SetToolTip(this.label2, "Hourly rainfall data - in an ascii format");
            // 
            // label25
            // 
            this.label25.Location = new System.Drawing.Point(12, 73);
            this.label25.Name = "label25";
            this.label25.Size = new System.Drawing.Size(143, 24);
            this.label25.TabIndex = 97;
            this.label25.Text = "Mean annual rainfall [m]";
            this.label25.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.toolTip1.SetToolTip(this.label25, "Hourly rainfall data - in an ascii format\r\nExample: rainfall.asc can look like:\r\n" +
        "0.67\r\n0.54\r\n0.87\r\n0.70\r\netc");
            // 
            // label14
            // 
            this.label14.Location = new System.Drawing.Point(12, 99);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(143, 24);
            this.label14.TabIndex = 115;
            this.label14.Text = "Mean annual infiltration [m]";
            this.label14.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.toolTip1.SetToolTip(this.label14, "Hourly rainfall data - in an ascii format");
            // 
            // label15
            // 
            this.label15.Location = new System.Drawing.Point(12, 125);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(151, 24);
            this.label15.TabIndex = 118;
            this.label15.Text = "Mean annual evaporation [m]";
            this.label15.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.toolTip1.SetToolTip(this.label15, "Hourly rainfall data - in an ascii format");
            // 
            // label17
            // 
            this.label17.Location = new System.Drawing.Point(12, 49);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(143, 24);
            this.label17.TabIndex = 121;
            this.label17.Text = "tillage fields [1/0]";
            this.label17.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.toolTip1.SetToolTip(this.label17, "Map of tillage (1) and no tillage (0) - in an ascii format");
            // 
            // label8
            // 
            this.label8.Location = new System.Drawing.Point(110, 31);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(54, 25);
            this.label8.TabIndex = 222;
            this.label8.Text = "timesteps";
            this.label8.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.toolTip1.SetToolTip(this.label8, "How often the avi file AND the other data files are saved");
            // 
            // constant_value_label
            // 
            this.constant_value_label.Location = new System.Drawing.Point(407, 12);
            this.constant_value_label.Name = "constant_value_label";
            this.constant_value_label.Size = new System.Drawing.Size(83, 24);
            this.constant_value_label.TabIndex = 110;
            this.constant_value_label.Text = " constant value";
            this.constant_value_label.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.toolTip1.SetToolTip(this.constant_value_label, "\"constant value\" is only available for some inputs and only \r\nwhen neither f(t) n" +
        "or f(x,y) are checked");
            // 
            // label29
            // 
            this.label29.Location = new System.Drawing.Point(217, 12);
            this.label29.Name = "label29";
            this.label29.Size = new System.Drawing.Size(24, 24);
            this.label29.TabIndex = 139;
            this.label29.Text = "f(t)";
            this.label29.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.toolTip1.SetToolTip(this.label29, "Check this for temporally variable inputs");
            // 
            // groupBox_landuse
            // 
            this.groupBox_landuse.Controls.Add(this.landuse_determinator_button);
            this.groupBox_landuse.Location = new System.Drawing.Point(504, 14);
            this.groupBox_landuse.Name = "groupBox_landuse";
            this.groupBox_landuse.Size = new System.Drawing.Size(168, 55);
            this.groupBox_landuse.TabIndex = 136;
            this.groupBox_landuse.TabStop = false;
            this.groupBox_landuse.Text = "for landuse: ";
            this.toolTip1.SetToolTip(this.groupBox_landuse, "LORICA");
            // 
            // landuse_determinator_button
            // 
            this.landuse_determinator_button.Location = new System.Drawing.Point(9, 26);
            this.landuse_determinator_button.Name = "landuse_determinator_button";
            this.landuse_determinator_button.Size = new System.Drawing.Size(110, 23);
            this.landuse_determinator_button.TabIndex = 0;
            this.landuse_determinator_button.Text = "determine effects..";
            this.landuse_determinator_button.UseVisualStyleBackColor = true;
            this.landuse_determinator_button.Click += new System.EventHandler(this.landuse_determinator_button_Click);
            // 
            // checkbox_t_intervene
            // 
            this.checkbox_t_intervene.AutoSize = true;
            this.checkbox_t_intervene.Location = new System.Drawing.Point(54, 61);
            this.checkbox_t_intervene.Name = "checkbox_t_intervene";
            this.checkbox_t_intervene.Size = new System.Drawing.Size(83, 17);
            this.checkbox_t_intervene.TabIndex = 4;
            this.checkbox_t_intervene.Text = "Start at year";
            this.toolTip1.SetToolTip(this.checkbox_t_intervene, "Select this checkbox when you want to run simulations starting with output from a" +
        "n earlier run. Model output should be available in the output directory for the " +
        "indicated year.");
            this.checkbox_t_intervene.UseVisualStyleBackColor = true;
            // 
            // groupBox9
            // 
            this.groupBox9.Controls.Add(this.parameter_k1_textbox);
            this.groupBox9.Controls.Add(this.label24);
            this.groupBox9.Controls.Add(this.label26);
            this.groupBox9.Controls.Add(this.label27);
            this.groupBox9.Controls.Add(this.label28);
            this.groupBox9.Controls.Add(this.parameter_k2_textbox);
            this.groupBox9.Controls.Add(this.parameter_Pa_textbox);
            this.groupBox9.Controls.Add(this.parameter_P0_textbox);
            this.groupBox9.Controls.Add(this.label21);
            this.groupBox9.Controls.Add(this.Biological_weathering_checkbox);
            this.groupBox9.Location = new System.Drawing.Point(12, 13);
            this.groupBox9.Name = "groupBox9";
            this.groupBox9.Size = new System.Drawing.Size(222, 180);
            this.groupBox9.TabIndex = 5;
            this.groupBox9.TabStop = false;
            this.groupBox9.Text = "Biological weathering (humped model)";
            this.toolTip1.SetToolTip(this.groupBox9, resources.GetString("groupBox9.ToolTip"));
            // 
            // parameter_k1_textbox
            // 
            this.parameter_k1_textbox.Location = new System.Drawing.Point(14, 80);
            this.parameter_k1_textbox.Name = "parameter_k1_textbox";
            this.parameter_k1_textbox.Size = new System.Drawing.Size(53, 20);
            this.parameter_k1_textbox.TabIndex = 20;
            this.parameter_k1_textbox.Text = "0.1";
            // 
            // label24
            // 
            this.label24.AutoSize = true;
            this.label24.Location = new System.Drawing.Point(89, 135);
            this.label24.Name = "label24";
            this.label24.Size = new System.Drawing.Size(52, 13);
            this.label24.TabIndex = 19;
            this.label24.Text = "Pa (m t-1)";
            // 
            // label26
            // 
            this.label26.AutoSize = true;
            this.label26.Location = new System.Drawing.Point(89, 109);
            this.label26.Name = "label26";
            this.label26.Size = new System.Drawing.Size(40, 13);
            this.label26.TabIndex = 18;
            this.label26.Text = "k2 (t-1)";
            // 
            // label27
            // 
            this.label27.AutoSize = true;
            this.label27.Location = new System.Drawing.Point(89, 83);
            this.label27.Name = "label27";
            this.label27.Size = new System.Drawing.Size(40, 13);
            this.label27.TabIndex = 17;
            this.label27.Text = "k1 (t-1)";
            // 
            // label28
            // 
            this.label28.AutoSize = true;
            this.label28.Location = new System.Drawing.Point(89, 57);
            this.label28.Name = "label28";
            this.label28.Size = new System.Drawing.Size(52, 13);
            this.label28.TabIndex = 16;
            this.label28.Text = "P0 (m t-1)";
            // 
            // parameter_k2_textbox
            // 
            this.parameter_k2_textbox.Location = new System.Drawing.Point(14, 106);
            this.parameter_k2_textbox.Name = "parameter_k2_textbox";
            this.parameter_k2_textbox.Size = new System.Drawing.Size(53, 20);
            this.parameter_k2_textbox.TabIndex = 14;
            this.parameter_k2_textbox.Text = "6";
            // 
            // parameter_Pa_textbox
            // 
            this.parameter_Pa_textbox.Location = new System.Drawing.Point(14, 132);
            this.parameter_Pa_textbox.Name = "parameter_Pa_textbox";
            this.parameter_Pa_textbox.Size = new System.Drawing.Size(53, 20);
            this.parameter_Pa_textbox.TabIndex = 13;
            this.parameter_Pa_textbox.Text = "0.00002";
            // 
            // parameter_P0_textbox
            // 
            this.parameter_P0_textbox.Location = new System.Drawing.Point(14, 54);
            this.parameter_P0_textbox.Name = "parameter_P0_textbox";
            this.parameter_P0_textbox.Size = new System.Drawing.Size(53, 20);
            this.parameter_P0_textbox.TabIndex = 12;
            this.parameter_P0_textbox.Text = "0.000033";
            // 
            // label21
            // 
            this.label21.AutoSize = true;
            this.label21.Location = new System.Drawing.Point(11, 11);
            this.label21.Name = "label21";
            this.label21.Size = new System.Drawing.Size(0, 13);
            this.label21.TabIndex = 4;
            // 
            // Biological_weathering_checkbox
            // 
            this.Biological_weathering_checkbox.AutoSize = true;
            this.Biological_weathering_checkbox.Location = new System.Drawing.Point(14, 19);
            this.Biological_weathering_checkbox.Name = "Biological_weathering_checkbox";
            this.Biological_weathering_checkbox.Size = new System.Drawing.Size(124, 17);
            this.Biological_weathering_checkbox.TabIndex = 3;
            this.Biological_weathering_checkbox.Text = "Activate this process";
            this.Biological_weathering_checkbox.UseVisualStyleBackColor = true;
            // 
            // fill_sinks_during_checkbox
            // 
            this.fill_sinks_during_checkbox.AutoSize = true;
            this.fill_sinks_during_checkbox.Location = new System.Drawing.Point(11, 45);
            this.fill_sinks_during_checkbox.Name = "fill_sinks_during_checkbox";
            this.fill_sinks_during_checkbox.Size = new System.Drawing.Size(88, 17);
            this.fill_sinks_during_checkbox.TabIndex = 133;
            this.fill_sinks_during_checkbox.Text = "while running";
            this.toolTip1.SetToolTip(this.fill_sinks_during_checkbox, resources.GetString("fill_sinks_during_checkbox.ToolTip"));
            this.fill_sinks_during_checkbox.UseVisualStyleBackColor = true;
            // 
            // fill_sinks_before_checkbox
            // 
            this.fill_sinks_before_checkbox.AutoSize = true;
            this.fill_sinks_before_checkbox.Location = new System.Drawing.Point(11, 22);
            this.fill_sinks_before_checkbox.Name = "fill_sinks_before_checkbox";
            this.fill_sinks_before_checkbox.Size = new System.Drawing.Size(94, 17);
            this.fill_sinks_before_checkbox.TabIndex = 132;
            this.fill_sinks_before_checkbox.Text = "before running";
            this.toolTip1.SetToolTip(this.fill_sinks_before_checkbox, resources.GetString("fill_sinks_before_checkbox.ToolTip"));
            this.fill_sinks_before_checkbox.UseVisualStyleBackColor = true;
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.label_layer_thickness);
            this.groupBox3.Controls.Add(this.label_thickness_ratio);
            this.groupBox3.Controls.Add(this.textbox_layer_thickness);
            this.groupBox3.Controls.Add(this.textbox_layer_thickness_increase);
            this.groupBox3.Location = new System.Drawing.Point(504, 74);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(261, 70);
            this.groupBox3.TabIndex = 137;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "layering options:";
            this.toolTip1.SetToolTip(this.groupBox3, "LORICA");
            this.groupBox3.Enter += new System.EventHandler(this.groupBox3_Enter);
            // 
            // label_layer_thickness
            // 
            this.label_layer_thickness.AutoSize = true;
            this.label_layer_thickness.Location = new System.Drawing.Point(6, 19);
            this.label_layer_thickness.Name = "label_layer_thickness";
            this.label_layer_thickness.Size = new System.Drawing.Size(148, 13);
            this.label_layer_thickness.TabIndex = 163;
            this.label_layer_thickness.Text = "Thickness of surface layer [m]";
            this.label_layer_thickness.Click += new System.EventHandler(this.label_layer_thickness_Click);
            // 
            // label_thickness_ratio
            // 
            this.label_thickness_ratio.AutoSize = true;
            this.label_thickness_ratio.Location = new System.Drawing.Point(6, 36);
            this.label_thickness_ratio.Name = "label_thickness_ratio";
            this.label_thickness_ratio.Size = new System.Drawing.Size(184, 26);
            this.label_thickness_ratio.TabIndex = 165;
            this.label_thickness_ratio.Text = "Ratio of successive layer thicknesses\r\n(1 for constant thickness)";
            this.label_thickness_ratio.Click += new System.EventHandler(this.label_thickness_ratio_Click);
            // 
            // textbox_layer_thickness
            // 
            this.textbox_layer_thickness.Location = new System.Drawing.Point(202, 16);
            this.textbox_layer_thickness.Name = "textbox_layer_thickness";
            this.textbox_layer_thickness.Size = new System.Drawing.Size(38, 20);
            this.textbox_layer_thickness.TabIndex = 162;
            this.textbox_layer_thickness.Text = "0.10";
            // 
            // textbox_layer_thickness_increase
            // 
            this.textbox_layer_thickness_increase.Location = new System.Drawing.Point(202, 42);
            this.textbox_layer_thickness_increase.Name = "textbox_layer_thickness_increase";
            this.textbox_layer_thickness_increase.Size = new System.Drawing.Size(38, 20);
            this.textbox_layer_thickness_increase.TabIndex = 164;
            this.textbox_layer_thickness_increase.Text = "1.1";
            this.textbox_layer_thickness_increase.TextChanged += new System.EventHandler(this.textbox_layer_thickness_increase_TextChanged);
            // 
            // Proglacial_text
            // 
            this.Proglacial_text.Location = new System.Drawing.Point(13, 49);
            this.Proglacial_text.Name = "Proglacial_text";
            this.Proglacial_text.Size = new System.Drawing.Size(151, 24);
            this.Proglacial_text.TabIndex = 162;
            this.Proglacial_text.Text = "proglacial age raster [yr]";
            this.Proglacial_text.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.toolTip1.SetToolTip(this.Proglacial_text, "Hourly rainfall data - in an ascii format");
            // 
            // label37
            // 
            this.label37.Location = new System.Drawing.Point(285, 52);
            this.label37.Name = "label37";
            this.label37.Size = new System.Drawing.Size(401, 24);
            this.label37.TabIndex = 164;
            this.label37.Text = "Ascii map of the year in which the glacier retreated from each location";
            this.label37.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.toolTip1.SetToolTip(this.label37, "Hourly rainfall data - in an ascii format");
            this.label37.Click += new System.EventHandler(this.label37_Click);
            // 
            // label4
            // 
            this.label4.Location = new System.Drawing.Point(12, 25);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(151, 24);
            this.label4.TabIndex = 106;
            this.label4.Text = "Land Use (classes)";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.toolTip1.SetToolTip(this.label4, "Map of the landuse - in ascii format. Define properties by clicking on the button" +
        " \"determine effects..\"");
            // 
            // annual_output_checkbox
            // 
            this.annual_output_checkbox.AutoSize = true;
            this.annual_output_checkbox.Location = new System.Drawing.Point(5, 35);
            this.annual_output_checkbox.Name = "annual_output_checkbox";
            this.annual_output_checkbox.Size = new System.Drawing.Size(57, 17);
            this.annual_output_checkbox.TabIndex = 1;
            this.annual_output_checkbox.Text = "annual";
            this.toolTip1.SetToolTip(this.annual_output_checkbox, "Between current and previous timestep ");
            this.annual_output_checkbox.UseVisualStyleBackColor = true;
            // 
            // cumulative_output_checkbox
            // 
            this.cumulative_output_checkbox.AutoSize = true;
            this.cumulative_output_checkbox.Checked = true;
            this.cumulative_output_checkbox.Location = new System.Drawing.Point(5, 12);
            this.cumulative_output_checkbox.Name = "cumulative_output_checkbox";
            this.cumulative_output_checkbox.Size = new System.Drawing.Size(76, 17);
            this.cumulative_output_checkbox.TabIndex = 0;
            this.cumulative_output_checkbox.TabStop = true;
            this.cumulative_output_checkbox.Text = "cumulative";
            this.toolTip1.SetToolTip(this.cumulative_output_checkbox, "Between current timestep and timestep 0");
            this.cumulative_output_checkbox.UseVisualStyleBackColor = true;
            // 
            // label5
            // 
            this.label5.Location = new System.Drawing.Point(279, 12);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(51, 24);
            this.label5.TabIndex = 108;
            this.label5.Text = "filename";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // explain_input_button
            // 
            this.explain_input_button.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.explain_input_button.Location = new System.Drawing.Point(333, 14);
            this.explain_input_button.Name = "explain_input_button";
            this.explain_input_button.Size = new System.Drawing.Size(45, 22);
            this.explain_input_button.TabIndex = 147;
            this.explain_input_button.Text = "help";
            this.explain_input_button.UseVisualStyleBackColor = true;
            this.explain_input_button.Click += new System.EventHandler(this.explain_input_Click);
            // 
            // UTMzonebox
            // 
            this.UTMzonebox.Location = new System.Drawing.Point(0, 0);
            this.UTMzonebox.Name = "UTMzonebox";
            this.UTMzonebox.Size = new System.Drawing.Size(100, 20);
            this.UTMzonebox.TabIndex = 0;
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(19, 49);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(104, 24);
            this.label1.TabIndex = 56;
            this.label1.Text = "DEM (.asc format)";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // button6
            // 
            this.button6.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.button6.Location = new System.Drawing.Point(132, 235);
            this.button6.Name = "button6";
            this.button6.Size = new System.Drawing.Size(100, 24);
            this.button6.TabIndex = 7;
            this.button6.Text = "test data";
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(131, 121);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(120, 20);
            this.textBox1.TabIndex = 103;
            this.textBox1.Text = "null";
            // 
            // textBox2
            // 
            this.textBox2.Location = new System.Drawing.Point(131, 49);
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(120, 20);
            this.textBox2.TabIndex = 100;
            this.textBox2.Text = "whole9.dat";
            // 
            // Output
            // 
            this.Output.Controls.Add(this.timeseries_button);
            this.Output.Controls.Add(this.groupBox6);
            this.Output.Location = new System.Drawing.Point(4, 22);
            this.Output.Name = "Output";
            this.Output.Size = new System.Drawing.Size(803, 293);
            this.Output.TabIndex = 7;
            this.Output.Text = "Output";
            this.Output.UseVisualStyleBackColor = true;
            // 
            // timeseries_button
            // 
            this.timeseries_button.Location = new System.Drawing.Point(330, 16);
            this.timeseries_button.Name = "timeseries_button";
            this.timeseries_button.Size = new System.Drawing.Size(149, 25);
            this.timeseries_button.TabIndex = 223;
            this.timeseries_button.Text = "timeseries outputs";
            this.timeseries_button.UseVisualStyleBackColor = true;
            this.timeseries_button.Click += new System.EventHandler(this.timeseries_button_Click_1);
            // 
            // groupBox6
            // 
            this.groupBox6.Controls.Add(this.groupBox12);
            this.groupBox6.Controls.Add(this.groupBox11);
            this.groupBox6.Controls.Add(this.groupBox1);
            this.groupBox6.Controls.Add(this.checkedListBox1);
            this.groupBox6.Location = new System.Drawing.Point(8, 16);
            this.groupBox6.Name = "groupBox6";
            this.groupBox6.Size = new System.Drawing.Size(294, 258);
            this.groupBox6.TabIndex = 222;
            this.groupBox6.TabStop = false;
            this.groupBox6.Text = "Normal outputs (ascii grids)";
            // 
            // groupBox12
            // 
            this.groupBox12.Controls.Add(this.annual_output_checkbox);
            this.groupBox12.Controls.Add(this.cumulative_output_checkbox);
            this.groupBox12.Location = new System.Drawing.Point(181, 19);
            this.groupBox12.Name = "groupBox12";
            this.groupBox12.Size = new System.Drawing.Size(102, 63);
            this.groupBox12.TabIndex = 227;
            this.groupBox12.TabStop = false;
            // 
            // groupBox11
            // 
            this.groupBox11.Controls.Add(this.label8);
            this.groupBox11.Controls.Add(this.Regular_output_checkbox);
            this.groupBox11.Controls.Add(this.Final_output_checkbox);
            this.groupBox11.Controls.Add(this.Box_years_output);
            this.groupBox11.Location = new System.Drawing.Point(6, 19);
            this.groupBox11.Name = "groupBox11";
            this.groupBox11.Size = new System.Drawing.Size(166, 63);
            this.groupBox11.TabIndex = 226;
            this.groupBox11.TabStop = false;
            // 
            // Regular_output_checkbox
            // 
            this.Regular_output_checkbox.AutoSize = true;
            this.Regular_output_checkbox.Location = new System.Drawing.Point(6, 36);
            this.Regular_output_checkbox.Name = "Regular_output_checkbox";
            this.Regular_output_checkbox.Size = new System.Drawing.Size(55, 17);
            this.Regular_output_checkbox.TabIndex = 221;
            this.Regular_output_checkbox.Text = "every ";
            this.Regular_output_checkbox.UseVisualStyleBackColor = true;
            // 
            // Final_output_checkbox
            // 
            this.Final_output_checkbox.AutoSize = true;
            this.Final_output_checkbox.Checked = true;
            this.Final_output_checkbox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.Final_output_checkbox.Enabled = false;
            this.Final_output_checkbox.Location = new System.Drawing.Point(6, 13);
            this.Final_output_checkbox.Name = "Final_output_checkbox";
            this.Final_output_checkbox.Size = new System.Drawing.Size(133, 17);
            this.Final_output_checkbox.TabIndex = 220;
            this.Final_output_checkbox.Text = "at simulation end, plus:";
            this.Final_output_checkbox.UseVisualStyleBackColor = true;
            // 
            // Box_years_output
            // 
            this.Box_years_output.AcceptsTab = true;
            this.Box_years_output.Location = new System.Drawing.Point(67, 34);
            this.Box_years_output.Name = "Box_years_output";
            this.Box_years_output.Size = new System.Drawing.Size(44, 20);
            this.Box_years_output.TabIndex = 1;
            this.Box_years_output.Text = "3";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.water_output_checkbox);
            this.groupBox1.Controls.Add(this.all_process_output_checkbox);
            this.groupBox1.Controls.Add(this.Soildepth_output_checkbox);
            this.groupBox1.Controls.Add(this.Alt_change_output_checkbox);
            this.groupBox1.Controls.Add(this.Altitude_output_checkbox);
            this.groupBox1.Location = new System.Drawing.Point(6, 85);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(277, 167);
            this.groupBox1.TabIndex = 225;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Outputs:";
            // 
            // water_output_checkbox
            // 
            this.water_output_checkbox.AutoSize = true;
            this.water_output_checkbox.Checked = true;
            this.water_output_checkbox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.water_output_checkbox.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.water_output_checkbox.Location = new System.Drawing.Point(24, 118);
            this.water_output_checkbox.Name = "water_output_checkbox";
            this.water_output_checkbox.Size = new System.Drawing.Size(74, 17);
            this.water_output_checkbox.TabIndex = 227;
            this.water_output_checkbox.Text = "Waterflow";
            this.water_output_checkbox.UseVisualStyleBackColor = true;
            // 
            // all_process_output_checkbox
            // 
            this.all_process_output_checkbox.AutoSize = true;
            this.all_process_output_checkbox.Checked = true;
            this.all_process_output_checkbox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.all_process_output_checkbox.Location = new System.Drawing.Point(24, 95);
            this.all_process_output_checkbox.Name = "all_process_output_checkbox";
            this.all_process_output_checkbox.Size = new System.Drawing.Size(106, 17);
            this.all_process_output_checkbox.TabIndex = 225;
            this.all_process_output_checkbox.Text = "Process volumes";
            this.all_process_output_checkbox.UseVisualStyleBackColor = true;
            // 
            // Soildepth_output_checkbox
            // 
            this.Soildepth_output_checkbox.AutoSize = true;
            this.Soildepth_output_checkbox.Checked = true;
            this.Soildepth_output_checkbox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.Soildepth_output_checkbox.Location = new System.Drawing.Point(24, 72);
            this.Soildepth_output_checkbox.Name = "Soildepth_output_checkbox";
            this.Soildepth_output_checkbox.Size = new System.Drawing.Size(70, 17);
            this.Soildepth_output_checkbox.TabIndex = 224;
            this.Soildepth_output_checkbox.Text = "Soildepth";
            this.Soildepth_output_checkbox.UseVisualStyleBackColor = true;
            // 
            // Alt_change_output_checkbox
            // 
            this.Alt_change_output_checkbox.AutoSize = true;
            this.Alt_change_output_checkbox.Checked = true;
            this.Alt_change_output_checkbox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.Alt_change_output_checkbox.Location = new System.Drawing.Point(24, 49);
            this.Alt_change_output_checkbox.Name = "Alt_change_output_checkbox";
            this.Alt_change_output_checkbox.Size = new System.Drawing.Size(100, 17);
            this.Alt_change_output_checkbox.TabIndex = 223;
            this.Alt_change_output_checkbox.Text = "Altitude change";
            this.Alt_change_output_checkbox.UseVisualStyleBackColor = true;
            // 
            // Altitude_output_checkbox
            // 
            this.Altitude_output_checkbox.AutoSize = true;
            this.Altitude_output_checkbox.Checked = true;
            this.Altitude_output_checkbox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.Altitude_output_checkbox.Location = new System.Drawing.Point(24, 26);
            this.Altitude_output_checkbox.Name = "Altitude_output_checkbox";
            this.Altitude_output_checkbox.Size = new System.Drawing.Size(61, 17);
            this.Altitude_output_checkbox.TabIndex = 222;
            this.Altitude_output_checkbox.Text = "Altitude";
            this.Altitude_output_checkbox.UseVisualStyleBackColor = true;
            // 
            // checkedListBox1
            // 
            this.checkedListBox1.FormattingEnabled = true;
            this.checkedListBox1.Items.AddRange(new object[] {
            "altitude",
            "altitude change",
            "soildepth",
            "soildepth change",
            "redistribution all processes",
            "redistribution per process",
            "weathering all processes",
            "weathering per process"});
            this.checkedListBox1.Location = new System.Drawing.Point(119, 96);
            this.checkedListBox1.Name = "checkedListBox1";
            this.checkedListBox1.Size = new System.Drawing.Size(152, 4);
            this.checkedListBox1.TabIndex = 0;
            this.checkedListBox1.Visible = false;
            // 
            // textBox6
            // 
            this.textBox6.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBox6.Location = new System.Drawing.Point(6, 17);
            this.textBox6.Multiline = true;
            this.textBox6.Name = "textBox6";
            this.textBox6.ReadOnly = true;
            this.textBox6.Size = new System.Drawing.Size(87, 22);
            this.textBox6.TabIndex = 196;
            this.textBox6.Text = "UTM zone (1-60)";
            // 
            // UTMsouthcheck
            // 
            this.UTMsouthcheck.Location = new System.Drawing.Point(0, 0);
            this.UTMsouthcheck.Name = "UTMsouthcheck";
            this.UTMsouthcheck.Size = new System.Drawing.Size(104, 24);
            this.UTMsouthcheck.TabIndex = 0;
            // 
            // Run
            // 
            this.Run.Controls.Add(this.groupBox8);
            this.Run.Controls.Add(this.groupBox7);
            this.Run.Location = new System.Drawing.Point(4, 22);
            this.Run.Name = "Run";
            this.Run.Size = new System.Drawing.Size(803, 293);
            this.Run.TabIndex = 8;
            this.Run.Text = "Run";
            this.Run.UseVisualStyleBackColor = true;
            // 
            // groupBox8
            // 
            this.groupBox8.Controls.Add(this.fill_sinks_during_checkbox);
            this.groupBox8.Controls.Add(this.fill_sinks_before_checkbox);
            this.groupBox8.Location = new System.Drawing.Point(351, 39);
            this.groupBox8.Name = "groupBox8";
            this.groupBox8.Size = new System.Drawing.Size(158, 73);
            this.groupBox8.TabIndex = 135;
            this.groupBox8.TabStop = false;
            this.groupBox8.Text = "remove sinks and flats: ";
            // 
            // groupBox7
            // 
            this.groupBox7.Controls.Add(this.checkbox_t_intervene);
            this.groupBox7.Controls.Add(this.textbox_t_intervene);
            this.groupBox7.Controls.Add(this.runs_checkbox);
            this.groupBox7.Controls.Add(this.label16);
            this.groupBox7.Controls.Add(this.Number_runs_textbox);
            this.groupBox7.Location = new System.Drawing.Point(48, 32);
            this.groupBox7.Name = "groupBox7";
            this.groupBox7.Size = new System.Drawing.Size(277, 88);
            this.groupBox7.TabIndex = 3;
            this.groupBox7.TabStop = false;
            this.groupBox7.Text = "Please specify  the number of timesteps per run";
            // 
            // textbox_t_intervene
            // 
            this.textbox_t_intervene.Location = new System.Drawing.Point(190, 60);
            this.textbox_t_intervene.Name = "textbox_t_intervene";
            this.textbox_t_intervene.Size = new System.Drawing.Size(55, 20);
            this.textbox_t_intervene.TabIndex = 3;
            this.textbox_t_intervene.Text = "0";
            // 
            // runs_checkbox
            // 
            this.runs_checkbox.AutoSize = true;
            this.runs_checkbox.Checked = true;
            this.runs_checkbox.Location = new System.Drawing.Point(54, 33);
            this.runs_checkbox.Name = "runs_checkbox";
            this.runs_checkbox.Size = new System.Drawing.Size(79, 17);
            this.runs_checkbox.TabIndex = 2;
            this.runs_checkbox.TabStop = true;
            this.runs_checkbox.Text = "runs (years)";
            this.runs_checkbox.UseVisualStyleBackColor = true;
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Location = new System.Drawing.Point(73, 39);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(0, 13);
            this.label16.TabIndex = 1;
            // 
            // Number_runs_textbox
            // 
            this.Number_runs_textbox.Location = new System.Drawing.Point(190, 30);
            this.Number_runs_textbox.Name = "Number_runs_textbox";
            this.Number_runs_textbox.Size = new System.Drawing.Size(55, 20);
            this.Number_runs_textbox.TabIndex = 0;
            this.Number_runs_textbox.Text = "1";
            // 
            // Input
            // 
            this.Input.Controls.Add(this.groupBox17);
            this.Input.Controls.Add(this.groupBox13);
            this.Input.Controls.Add(this.explain_input_button);
            this.Input.Controls.Add(this.label29);
            this.Input.Controls.Add(this.constant_value_label);
            this.Input.Controls.Add(label6);
            this.Input.Controls.Add(this.label5);
            this.Input.Location = new System.Drawing.Point(4, 22);
            this.Input.Name = "Input";
            this.Input.Size = new System.Drawing.Size(803, 293);
            this.Input.TabIndex = 0;
            this.Input.Text = "Inputs";
            this.Input.UseVisualStyleBackColor = true;
            // 
            // groupBox17
            // 
            this.groupBox17.Controls.Add(this.groupBox3);
            this.groupBox17.Controls.Add(this.groupBox_landuse);
            this.groupBox17.Controls.Add(this.check_time_evap);
            this.groupBox17.Controls.Add(this.check_time_infil);
            this.groupBox17.Controls.Add(this.check_time_rain);
            this.groupBox17.Controls.Add(this.check_time_till_fields);
            this.groupBox17.Controls.Add(this.check_time_landuse);
            this.groupBox17.Controls.Add(this.tillfields_constant_textbox);
            this.groupBox17.Controls.Add(this.tillfields_input_filename_textbox);
            this.groupBox17.Controls.Add(this.evap_constant_value_box);
            this.groupBox17.Controls.Add(this.evap_input_filename_textbox);
            this.groupBox17.Controls.Add(this.infil_constant_value_box);
            this.groupBox17.Controls.Add(this.infil_input_filename_textbox);
            this.groupBox17.Controls.Add(this.rainfall_constant_value_box);
            this.groupBox17.Controls.Add(this.landuse_constant_value_box);
            this.groupBox17.Controls.Add(this.landuse_input_filename_textbox);
            this.groupBox17.Controls.Add(this.rain_input_filename_textbox);
            this.groupBox17.Controls.Add(this.check_space_evap);
            this.groupBox17.Controls.Add(this.check_space_infil);
            this.groupBox17.Controls.Add(this.check_space_rain);
            this.groupBox17.Controls.Add(this.check_space_till_fields);
            this.groupBox17.Controls.Add(this.check_space_landuse);
            this.groupBox17.Controls.Add(this.label17);
            this.groupBox17.Controls.Add(this.label15);
            this.groupBox17.Controls.Add(this.label14);
            this.groupBox17.Controls.Add(this.label4);
            this.groupBox17.Controls.Add(this.label25);
            this.groupBox17.Location = new System.Drawing.Point(7, 135);
            this.groupBox17.Name = "groupBox17";
            this.groupBox17.Size = new System.Drawing.Size(783, 155);
            this.groupBox17.TabIndex = 161;
            this.groupBox17.TabStop = false;
            this.groupBox17.Text = "depending on process (optional)";
            // 
            // check_time_evap
            // 
            this.check_time_evap.AutoSize = true;
            this.check_time_evap.Location = new System.Drawing.Point(213, 131);
            this.check_time_evap.Name = "check_time_evap";
            this.check_time_evap.Size = new System.Drawing.Size(15, 14);
            this.check_time_evap.TabIndex = 145;
            this.check_time_evap.UseVisualStyleBackColor = true;
            this.check_time_evap.CheckedChanged += new System.EventHandler(this.check_time_evap_CheckedChanged);
            // 
            // check_time_infil
            // 
            this.check_time_infil.AutoSize = true;
            this.check_time_infil.Location = new System.Drawing.Point(213, 107);
            this.check_time_infil.Name = "check_time_infil";
            this.check_time_infil.Size = new System.Drawing.Size(15, 14);
            this.check_time_infil.TabIndex = 144;
            this.check_time_infil.UseVisualStyleBackColor = true;
            this.check_time_infil.CheckedChanged += new System.EventHandler(this.check_time_infil_CheckedChanged);
            // 
            // check_time_rain
            // 
            this.check_time_rain.AutoSize = true;
            this.check_time_rain.Location = new System.Drawing.Point(213, 83);
            this.check_time_rain.Name = "check_time_rain";
            this.check_time_rain.Size = new System.Drawing.Size(15, 14);
            this.check_time_rain.TabIndex = 143;
            this.check_time_rain.UseVisualStyleBackColor = true;
            this.check_time_rain.CheckedChanged += new System.EventHandler(this.check_time_rain_CheckedChanged);
            // 
            // check_time_till_fields
            // 
            this.check_time_till_fields.AutoSize = true;
            this.check_time_till_fields.Enabled = false;
            this.check_time_till_fields.Location = new System.Drawing.Point(213, 59);
            this.check_time_till_fields.Name = "check_time_till_fields";
            this.check_time_till_fields.Size = new System.Drawing.Size(15, 14);
            this.check_time_till_fields.TabIndex = 142;
            this.check_time_till_fields.UseVisualStyleBackColor = true;
            this.check_time_till_fields.CheckedChanged += new System.EventHandler(this.check_time_tillage_CheckedChanged);
            // 
            // check_time_landuse
            // 
            this.check_time_landuse.AutoSize = true;
            this.check_time_landuse.Enabled = false;
            this.check_time_landuse.Location = new System.Drawing.Point(213, 32);
            this.check_time_landuse.Name = "check_time_landuse";
            this.check_time_landuse.Size = new System.Drawing.Size(15, 14);
            this.check_time_landuse.TabIndex = 141;
            this.check_time_landuse.UseVisualStyleBackColor = true;
            this.check_time_landuse.CheckedChanged += new System.EventHandler(this.check_time_landuse_CheckedChanged);
            // 
            // tillfields_constant_textbox
            // 
            this.tillfields_constant_textbox.BackColor = System.Drawing.SystemColors.Window;
            this.tillfields_constant_textbox.Location = new System.Drawing.Point(403, 54);
            this.tillfields_constant_textbox.Name = "tillfields_constant_textbox";
            this.tillfields_constant_textbox.Size = new System.Drawing.Size(80, 20);
            this.tillfields_constant_textbox.TabIndex = 123;
            this.tillfields_constant_textbox.Text = "0";
            // 
            // tillfields_input_filename_textbox
            // 
            this.tillfields_input_filename_textbox.Enabled = false;
            this.tillfields_input_filename_textbox.Location = new System.Drawing.Point(251, 54);
            this.tillfields_input_filename_textbox.Name = "tillfields_input_filename_textbox";
            this.tillfields_input_filename_textbox.Size = new System.Drawing.Size(120, 20);
            this.tillfields_input_filename_textbox.TabIndex = 122;
            this.tillfields_input_filename_textbox.Text = "..";
            this.tillfields_input_filename_textbox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.tillfields_input_filename_textbox.Click += new System.EventHandler(this.tillfields_input_filename_textbox_TextChanged);
            // 
            // evap_constant_value_box
            // 
            this.evap_constant_value_box.Location = new System.Drawing.Point(403, 128);
            this.evap_constant_value_box.Name = "evap_constant_value_box";
            this.evap_constant_value_box.Size = new System.Drawing.Size(80, 20);
            this.evap_constant_value_box.TabIndex = 120;
            this.evap_constant_value_box.Text = "0.35";
            // 
            // evap_input_filename_textbox
            // 
            this.evap_input_filename_textbox.Enabled = false;
            this.evap_input_filename_textbox.Location = new System.Drawing.Point(251, 128);
            this.evap_input_filename_textbox.Name = "evap_input_filename_textbox";
            this.evap_input_filename_textbox.Size = new System.Drawing.Size(120, 20);
            this.evap_input_filename_textbox.TabIndex = 119;
            this.evap_input_filename_textbox.Text = "..";
            this.evap_input_filename_textbox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.evap_input_filename_textbox.Click += new System.EventHandler(this.evap_input_filename_textbox_TextChanged);
            // 
            // infil_constant_value_box
            // 
            this.infil_constant_value_box.Location = new System.Drawing.Point(403, 102);
            this.infil_constant_value_box.Name = "infil_constant_value_box";
            this.infil_constant_value_box.Size = new System.Drawing.Size(80, 20);
            this.infil_constant_value_box.TabIndex = 117;
            this.infil_constant_value_box.Text = "0.150";
            // 
            // infil_input_filename_textbox
            // 
            this.infil_input_filename_textbox.Enabled = false;
            this.infil_input_filename_textbox.Location = new System.Drawing.Point(251, 102);
            this.infil_input_filename_textbox.Name = "infil_input_filename_textbox";
            this.infil_input_filename_textbox.Size = new System.Drawing.Size(120, 20);
            this.infil_input_filename_textbox.TabIndex = 116;
            this.infil_input_filename_textbox.Text = "..";
            this.infil_input_filename_textbox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.infil_input_filename_textbox.Click += new System.EventHandler(this.infil_input_filename_textbox_TextChanged);
            // 
            // rainfall_constant_value_box
            // 
            this.rainfall_constant_value_box.Location = new System.Drawing.Point(403, 78);
            this.rainfall_constant_value_box.Name = "rainfall_constant_value_box";
            this.rainfall_constant_value_box.Size = new System.Drawing.Size(80, 20);
            this.rainfall_constant_value_box.TabIndex = 114;
            this.rainfall_constant_value_box.Text = "0.700";
            // 
            // landuse_constant_value_box
            // 
            this.landuse_constant_value_box.Location = new System.Drawing.Point(403, 29);
            this.landuse_constant_value_box.Name = "landuse_constant_value_box";
            this.landuse_constant_value_box.Size = new System.Drawing.Size(80, 20);
            this.landuse_constant_value_box.TabIndex = 113;
            this.landuse_constant_value_box.Text = "1";
            // 
            // landuse_input_filename_textbox
            // 
            this.landuse_input_filename_textbox.Enabled = false;
            this.landuse_input_filename_textbox.Location = new System.Drawing.Point(251, 27);
            this.landuse_input_filename_textbox.Name = "landuse_input_filename_textbox";
            this.landuse_input_filename_textbox.Size = new System.Drawing.Size(120, 20);
            this.landuse_input_filename_textbox.TabIndex = 107;
            this.landuse_input_filename_textbox.Text = "..";
            this.landuse_input_filename_textbox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.landuse_input_filename_textbox.Click += new System.EventHandler(this.landuse_input_filename_textbox_TextChanged);
            // 
            // rain_input_filename_textbox
            // 
            this.rain_input_filename_textbox.Enabled = false;
            this.rain_input_filename_textbox.Location = new System.Drawing.Point(251, 78);
            this.rain_input_filename_textbox.Name = "rain_input_filename_textbox";
            this.rain_input_filename_textbox.Size = new System.Drawing.Size(120, 20);
            this.rain_input_filename_textbox.TabIndex = 103;
            this.rain_input_filename_textbox.Text = "..";
            this.rain_input_filename_textbox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.rain_input_filename_textbox.Click += new System.EventHandler(this.rain_input_filename_textbox_TextChanged);
            // 
            // check_space_evap
            // 
            this.check_space_evap.AutoSize = true;
            this.check_space_evap.Location = new System.Drawing.Point(181, 131);
            this.check_space_evap.Name = "check_space_evap";
            this.check_space_evap.Size = new System.Drawing.Size(15, 14);
            this.check_space_evap.TabIndex = 130;
            this.check_space_evap.UseVisualStyleBackColor = true;
            this.check_space_evap.CheckedChanged += new System.EventHandler(this.check_cnst_evap_CheckedChanged);
            // 
            // check_space_infil
            // 
            this.check_space_infil.AutoSize = true;
            this.check_space_infil.Location = new System.Drawing.Point(181, 107);
            this.check_space_infil.Name = "check_space_infil";
            this.check_space_infil.Size = new System.Drawing.Size(15, 14);
            this.check_space_infil.TabIndex = 129;
            this.check_space_infil.UseVisualStyleBackColor = true;
            this.check_space_infil.CheckedChanged += new System.EventHandler(this.check_cnst_infil_CheckedChanged);
            // 
            // check_space_rain
            // 
            this.check_space_rain.AutoSize = true;
            this.check_space_rain.Location = new System.Drawing.Point(181, 83);
            this.check_space_rain.Name = "check_space_rain";
            this.check_space_rain.Size = new System.Drawing.Size(15, 14);
            this.check_space_rain.TabIndex = 128;
            this.check_space_rain.UseVisualStyleBackColor = true;
            this.check_space_rain.CheckedChanged += new System.EventHandler(this.check_cnst_rain_CheckedChanged_1);
            // 
            // check_space_till_fields
            // 
            this.check_space_till_fields.AutoSize = true;
            this.check_space_till_fields.Location = new System.Drawing.Point(181, 59);
            this.check_space_till_fields.Name = "check_space_till_fields";
            this.check_space_till_fields.Size = new System.Drawing.Size(15, 14);
            this.check_space_till_fields.TabIndex = 127;
            this.check_space_till_fields.UseVisualStyleBackColor = true;
            this.check_space_till_fields.CheckedChanged += new System.EventHandler(this.check_cnst_till_fields_CheckedChanged);
            // 
            // check_space_landuse
            // 
            this.check_space_landuse.AutoSize = true;
            this.check_space_landuse.Location = new System.Drawing.Point(181, 32);
            this.check_space_landuse.Name = "check_space_landuse";
            this.check_space_landuse.Size = new System.Drawing.Size(15, 14);
            this.check_space_landuse.TabIndex = 126;
            this.check_space_landuse.UseVisualStyleBackColor = true;
            this.check_space_landuse.CheckedChanged += new System.EventHandler(this.check_cnst_landuse_CheckedChanged_1);
            // 
            // groupBox13
            // 
            this.groupBox13.Controls.Add(this.groupBox5);
            this.groupBox13.Controls.Add(this.check_space_DTM);
            this.groupBox13.Controls.Add(this.soildepth_constant_value_box);
            this.groupBox13.Controls.Add(this.soildepth_input_filename_textbox);
            this.groupBox13.Controls.Add(this.dtm_input_filename_textbox);
            this.groupBox13.Controls.Add(this.check_space_soildepth);
            this.groupBox13.Controls.Add(this.label3);
            this.groupBox13.Controls.Add(this.label23);
            this.groupBox13.Location = new System.Drawing.Point(6, 35);
            this.groupBox13.Name = "groupBox13";
            this.groupBox13.Size = new System.Drawing.Size(784, 83);
            this.groupBox13.TabIndex = 160;
            this.groupBox13.TabStop = false;
            this.groupBox13.Text = "required";
            // 
            // groupBox5
            // 
            this.groupBox5.Controls.Add(this.soil_specify_button);
            this.groupBox5.Controls.Add(this.label_max_soil_layers);
            this.groupBox5.Controls.Add(this.textbox_max_soil_layers);
            this.groupBox5.Location = new System.Drawing.Point(505, 19);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Size = new System.Drawing.Size(261, 54);
            this.groupBox5.TabIndex = 159;
            this.groupBox5.TabStop = false;
            this.groupBox5.Text = "for soils:";
            // 
            // soil_specify_button
            // 
            this.soil_specify_button.Location = new System.Drawing.Point(9, 20);
            this.soil_specify_button.Name = "soil_specify_button";
            this.soil_specify_button.Size = new System.Drawing.Size(97, 21);
            this.soil_specify_button.TabIndex = 149;
            this.soil_specify_button.Text = "set properties..";
            this.soil_specify_button.UseVisualStyleBackColor = true;
            this.soil_specify_button.Click += new System.EventHandler(this.soil_specify_button_Click);
            // 
            // label_max_soil_layers
            // 
            this.label_max_soil_layers.AutoSize = true;
            this.label_max_soil_layers.Location = new System.Drawing.Point(112, 24);
            this.label_max_soil_layers.Name = "label_max_soil_layers";
            this.label_max_soil_layers.Size = new System.Drawing.Size(86, 13);
            this.label_max_soil_layers.TabIndex = 156;
            this.label_max_soil_layers.Text = "Number of layers";
            // 
            // textbox_max_soil_layers
            // 
            this.textbox_max_soil_layers.Location = new System.Drawing.Point(199, 21);
            this.textbox_max_soil_layers.Name = "textbox_max_soil_layers";
            this.textbox_max_soil_layers.Size = new System.Drawing.Size(41, 20);
            this.textbox_max_soil_layers.TabIndex = 155;
            this.textbox_max_soil_layers.Text = "10";
            // 
            // check_space_DTM
            // 
            this.check_space_DTM.AutoSize = true;
            this.check_space_DTM.Checked = true;
            this.check_space_DTM.CheckState = System.Windows.Forms.CheckState.Checked;
            this.check_space_DTM.Enabled = false;
            this.check_space_DTM.Location = new System.Drawing.Point(182, 28);
            this.check_space_DTM.Name = "check_space_DTM";
            this.check_space_DTM.Size = new System.Drawing.Size(15, 14);
            this.check_space_DTM.TabIndex = 138;
            this.check_space_DTM.UseVisualStyleBackColor = true;
            // 
            // soildepth_constant_value_box
            // 
            this.soildepth_constant_value_box.Location = new System.Drawing.Point(404, 53);
            this.soildepth_constant_value_box.Name = "soildepth_constant_value_box";
            this.soildepth_constant_value_box.Size = new System.Drawing.Size(80, 20);
            this.soildepth_constant_value_box.TabIndex = 112;
            this.soildepth_constant_value_box.Text = "5";
            // 
            // soildepth_input_filename_textbox
            // 
            this.soildepth_input_filename_textbox.Enabled = false;
            this.soildepth_input_filename_textbox.Location = new System.Drawing.Point(252, 53);
            this.soildepth_input_filename_textbox.Name = "soildepth_input_filename_textbox";
            this.soildepth_input_filename_textbox.Size = new System.Drawing.Size(120, 20);
            this.soildepth_input_filename_textbox.TabIndex = 105;
            this.soildepth_input_filename_textbox.Text = "..";
            this.soildepth_input_filename_textbox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.soildepth_input_filename_textbox.Click += new System.EventHandler(this.soildepth_input_filename_textbox_TextChanged);
            // 
            // dtm_input_filename_textbox
            // 
            this.dtm_input_filename_textbox.Location = new System.Drawing.Point(252, 27);
            this.dtm_input_filename_textbox.Name = "dtm_input_filename_textbox";
            this.dtm_input_filename_textbox.Size = new System.Drawing.Size(120, 20);
            this.dtm_input_filename_textbox.TabIndex = 100;
            this.dtm_input_filename_textbox.Text = "..";
            this.dtm_input_filename_textbox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.dtm_input_filename_textbox.Click += new System.EventHandler(this.dtm_input_filename_textbox_Click);
            // 
            // check_space_soildepth
            // 
            this.check_space_soildepth.AutoSize = true;
            this.check_space_soildepth.Location = new System.Drawing.Point(182, 55);
            this.check_space_soildepth.Name = "check_space_soildepth";
            this.check_space_soildepth.Size = new System.Drawing.Size(15, 14);
            this.check_space_soildepth.TabIndex = 125;
            this.check_space_soildepth.UseVisualStyleBackColor = true;
            this.check_space_soildepth.CheckedChanged += new System.EventHandler(this.check_cnst_soildepth_CheckedChanged_1);
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(13, 48);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(104, 24);
            this.label3.TabIndex = 104;
            this.label3.Text = "Soil Depth [m]";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label23
            // 
            this.label23.Location = new System.Drawing.Point(13, 22);
            this.label23.Name = "label23";
            this.label23.Size = new System.Drawing.Size(134, 24);
            this.label23.TabIndex = 56;
            this.label23.Text = "Digital Elevation Model [m]";
            this.label23.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Geomorph
            // 
            this.Geomorph.Controls.Add(this.Process_tabs);
            this.Geomorph.Location = new System.Drawing.Point(4, 22);
            this.Geomorph.Name = "Geomorph";
            this.Geomorph.Size = new System.Drawing.Size(803, 293);
            this.Geomorph.TabIndex = 6;
            this.Geomorph.Text = "Geomorphic processes";
            this.Geomorph.UseVisualStyleBackColor = true;
            // 
            // Process_tabs
            // 
            this.Process_tabs.Controls.Add(this.Water);
            this.Process_tabs.Controls.Add(this.Tillage);
            this.Process_tabs.Controls.Add(this.Creeper);
            this.Process_tabs.Controls.Add(Landsliding);
            this.Process_tabs.Controls.Add(this.Tectonics);
            this.Process_tabs.Location = new System.Drawing.Point(8, 14);
            this.Process_tabs.MaximumSize = new System.Drawing.Size(740, 276);
            this.Process_tabs.MinimumSize = new System.Drawing.Size(740, 276);
            this.Process_tabs.Name = "Process_tabs";
            this.Process_tabs.SelectedIndex = 0;
            this.Process_tabs.Size = new System.Drawing.Size(740, 276);
            this.Process_tabs.TabIndex = 0;
            // 
            // Water
            // 
            this.Water.Controls.Add(this.label87);
            this.Water.Controls.Add(this.selectivity_constant_textbox);
            this.Water.Controls.Add(this.bio_protection_constant_textbox);
            this.Water.Controls.Add(this.erosion_threshold_textbox);
            this.Water.Controls.Add(this.rock_protection_constant_textbox);
            this.Water.Controls.Add(this.parameter_n_textbox);
            this.Water.Controls.Add(this.parameter_conv_textbox);
            this.Water.Controls.Add(this.parameter_K_textbox);
            this.Water.Controls.Add(this.parameter_m_textbox);
            this.Water.Controls.Add(this.label90);
            this.Water.Controls.Add(this.label91);
            this.Water.Controls.Add(this.label92);
            this.Water.Controls.Add(this.only_waterflow_checkbox);
            this.Water.Controls.Add(this.pictureBox1);
            this.Water.Controls.Add(this.label12);
            this.Water.Controls.Add(this.label11);
            this.Water.Controls.Add(this.label10);
            this.Water.Controls.Add(this.label9);
            this.Water.Controls.Add(this.Water_ero_checkbox);
            this.Water.Location = new System.Drawing.Point(4, 22);
            this.Water.Name = "Water";
            this.Water.Padding = new System.Windows.Forms.Padding(3);
            this.Water.Size = new System.Drawing.Size(732, 250);
            this.Water.TabIndex = 0;
            this.Water.Text = "Water erosion and deposition";
            this.Water.UseVisualStyleBackColor = true;
            // 
            // label87
            // 
            this.label87.AutoSize = true;
            this.label87.Location = new System.Drawing.Point(101, 228);
            this.label87.Name = "label87";
            this.label87.Size = new System.Drawing.Size(136, 13);
            this.label87.TabIndex = 28;
            this.label87.Text = "selectivity change constant";
            // 
            // selectivity_constant_textbox
            // 
            this.selectivity_constant_textbox.Location = new System.Drawing.Point(26, 225);
            this.selectivity_constant_textbox.Name = "selectivity_constant_textbox";
            this.selectivity_constant_textbox.Size = new System.Drawing.Size(53, 20);
            this.selectivity_constant_textbox.TabIndex = 27;
            this.selectivity_constant_textbox.Text = "0";
            // 
            // bio_protection_constant_textbox
            // 
            this.bio_protection_constant_textbox.Location = new System.Drawing.Point(26, 199);
            this.bio_protection_constant_textbox.Name = "bio_protection_constant_textbox";
            this.bio_protection_constant_textbox.Size = new System.Drawing.Size(53, 20);
            this.bio_protection_constant_textbox.TabIndex = 21;
            this.bio_protection_constant_textbox.Text = "1";
            // 
            // erosion_threshold_textbox
            // 
            this.erosion_threshold_textbox.Location = new System.Drawing.Point(26, 147);
            this.erosion_threshold_textbox.Name = "erosion_threshold_textbox";
            this.erosion_threshold_textbox.Size = new System.Drawing.Size(53, 20);
            this.erosion_threshold_textbox.TabIndex = 20;
            this.erosion_threshold_textbox.Text = "0.01";
            // 
            // rock_protection_constant_textbox
            // 
            this.rock_protection_constant_textbox.Location = new System.Drawing.Point(26, 173);
            this.rock_protection_constant_textbox.Name = "rock_protection_constant_textbox";
            this.rock_protection_constant_textbox.Size = new System.Drawing.Size(53, 20);
            this.rock_protection_constant_textbox.TabIndex = 17;
            this.rock_protection_constant_textbox.Text = "1";
            // 
            // parameter_n_textbox
            // 
            this.parameter_n_textbox.Location = new System.Drawing.Point(26, 96);
            this.parameter_n_textbox.Name = "parameter_n_textbox";
            this.parameter_n_textbox.Size = new System.Drawing.Size(53, 20);
            this.parameter_n_textbox.TabIndex = 7;
            this.parameter_n_textbox.Text = "1.3";
            // 
            // parameter_conv_textbox
            // 
            this.parameter_conv_textbox.Location = new System.Drawing.Point(26, 47);
            this.parameter_conv_textbox.Name = "parameter_conv_textbox";
            this.parameter_conv_textbox.Size = new System.Drawing.Size(53, 20);
            this.parameter_conv_textbox.TabIndex = 6;
            this.parameter_conv_textbox.Text = "2";
            // 
            // parameter_K_textbox
            // 
            this.parameter_K_textbox.Location = new System.Drawing.Point(26, 121);
            this.parameter_K_textbox.Name = "parameter_K_textbox";
            this.parameter_K_textbox.Size = new System.Drawing.Size(53, 20);
            this.parameter_K_textbox.TabIndex = 5;
            this.parameter_K_textbox.Text = "0.0003";
            // 
            // parameter_m_textbox
            // 
            this.parameter_m_textbox.Location = new System.Drawing.Point(26, 70);
            this.parameter_m_textbox.Name = "parameter_m_textbox";
            this.parameter_m_textbox.Size = new System.Drawing.Size(53, 20);
            this.parameter_m_textbox.TabIndex = 1;
            this.parameter_m_textbox.Text = "1.67";
            // 
            // label90
            // 
            this.label90.AutoSize = true;
            this.label90.Location = new System.Drawing.Point(101, 150);
            this.label90.Name = "label90";
            this.label90.Size = new System.Drawing.Size(87, 13);
            this.label90.TabIndex = 24;
            this.label90.Text = "erosion threshold";
            // 
            // label91
            // 
            this.label91.AutoSize = true;
            this.label91.Location = new System.Drawing.Point(101, 202);
            this.label91.Name = "label91";
            this.label91.Size = new System.Drawing.Size(115, 13);
            this.label91.TabIndex = 23;
            this.label91.Text = "bio protection constant";
            // 
            // label92
            // 
            this.label92.AutoSize = true;
            this.label92.Location = new System.Drawing.Point(101, 176);
            this.label92.Name = "label92";
            this.label92.Size = new System.Drawing.Size(122, 13);
            this.label92.TabIndex = 22;
            this.label92.Text = "rock protection constant";
            // 
            // only_waterflow_checkbox
            // 
            this.only_waterflow_checkbox.AutoSize = true;
            this.only_waterflow_checkbox.Location = new System.Drawing.Point(156, 16);
            this.only_waterflow_checkbox.Name = "only_waterflow_checkbox";
            this.only_waterflow_checkbox.Size = new System.Drawing.Size(219, 17);
            this.only_waterflow_checkbox.TabIndex = 14;
            this.only_waterflow_checkbox.Text = "Only calculate waterflow, no ero and dep";
            this.only_waterflow_checkbox.UseVisualStyleBackColor = true;
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.Location = new System.Drawing.Point(266, 53);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(180, 137);
            this.pictureBox1.TabIndex = 13;
            this.pictureBox1.TabStop = false;
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(101, 124);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(66, 13);
            this.label12.TabIndex = 11;
            this.label12.Text = "K (erodibility)";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(101, 50);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(109, 13);
            this.label11.TabIndex = 10;
            this.label11.Text = "p (multiple flow factor)";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(101, 99);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(106, 13);
            this.label10.TabIndex = 9;
            this.label10.Text = "n (exponent of slope)";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(101, 73);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(146, 13);
            this.label9.TabIndex = 8;
            this.label9.Text = "m (exponent of overland flow)";
            // 
            // Water_ero_checkbox
            // 
            this.Water_ero_checkbox.AutoSize = true;
            this.Water_ero_checkbox.Checked = true;
            this.Water_ero_checkbox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.Water_ero_checkbox.Location = new System.Drawing.Point(26, 16);
            this.Water_ero_checkbox.Name = "Water_ero_checkbox";
            this.Water_ero_checkbox.Size = new System.Drawing.Size(124, 17);
            this.Water_ero_checkbox.TabIndex = 0;
            this.Water_ero_checkbox.Text = "Activate this process";
            this.Water_ero_checkbox.UseVisualStyleBackColor = true;
            this.Water_ero_checkbox.CheckedChanged += new System.EventHandler(this.Water_ero_checkbox_CheckedChanged);
            // 
            // Tillage
            // 
            this.Tillage.Controls.Add(this.pictureBox2);
            this.Tillage.Controls.Add(this.label20);
            this.Tillage.Controls.Add(this.trte);
            this.Tillage.Controls.Add(this.parameter_tillage_constant_textbox);
            this.Tillage.Controls.Add(this.parameter_ploughing_depth_textbox);
            this.Tillage.Controls.Add(this.Tillage_checkbox);
            this.Tillage.Location = new System.Drawing.Point(4, 22);
            this.Tillage.Name = "Tillage";
            this.Tillage.Padding = new System.Windows.Forms.Padding(3);
            this.Tillage.Size = new System.Drawing.Size(732, 250);
            this.Tillage.TabIndex = 1;
            this.Tillage.Text = "Tillage";
            this.Tillage.UseVisualStyleBackColor = true;
            // 
            // pictureBox2
            // 
            this.pictureBox2.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox2.Image")));
            this.pictureBox2.Location = new System.Drawing.Point(276, 57);
            this.pictureBox2.Name = "pictureBox2";
            this.pictureBox2.Size = new System.Drawing.Size(180, 137);
            this.pictureBox2.TabIndex = 20;
            this.pictureBox2.TabStop = false;
            // 
            // label20
            // 
            this.label20.AutoSize = true;
            this.label20.Location = new System.Drawing.Point(128, 87);
            this.label20.Name = "label20";
            this.label20.Size = new System.Drawing.Size(78, 13);
            this.label20.TabIndex = 19;
            this.label20.Text = "tillage constant";
            // 
            // trte
            // 
            this.trte.AutoSize = true;
            this.trte.Location = new System.Drawing.Point(128, 61);
            this.trte.Name = "trte";
            this.trte.Size = new System.Drawing.Size(83, 13);
            this.trte.TabIndex = 18;
            this.trte.Text = "ploughing depth";
            // 
            // parameter_tillage_constant_textbox
            // 
            this.parameter_tillage_constant_textbox.Location = new System.Drawing.Point(53, 84);
            this.parameter_tillage_constant_textbox.Name = "parameter_tillage_constant_textbox";
            this.parameter_tillage_constant_textbox.Size = new System.Drawing.Size(53, 20);
            this.parameter_tillage_constant_textbox.TabIndex = 17;
            this.parameter_tillage_constant_textbox.Text = "1";
            // 
            // parameter_ploughing_depth_textbox
            // 
            this.parameter_ploughing_depth_textbox.AcceptsTab = true;
            this.parameter_ploughing_depth_textbox.Location = new System.Drawing.Point(53, 58);
            this.parameter_ploughing_depth_textbox.Name = "parameter_ploughing_depth_textbox";
            this.parameter_ploughing_depth_textbox.Size = new System.Drawing.Size(53, 20);
            this.parameter_ploughing_depth_textbox.TabIndex = 13;
            this.parameter_ploughing_depth_textbox.Text = "0.25";
            // 
            // Tillage_checkbox
            // 
            this.Tillage_checkbox.AutoSize = true;
            this.Tillage_checkbox.Location = new System.Drawing.Point(26, 16);
            this.Tillage_checkbox.Name = "Tillage_checkbox";
            this.Tillage_checkbox.Size = new System.Drawing.Size(124, 17);
            this.Tillage_checkbox.TabIndex = 1;
            this.Tillage_checkbox.Text = "Activate this process";
            this.Tillage_checkbox.UseVisualStyleBackColor = true;
            // 
            // Creeper
            // 
            this.Creeper.Controls.Add(this.creep_testing);
            this.Creeper.Controls.Add(this.pictureBox3);
            this.Creeper.Controls.Add(this.label19);
            this.Creeper.Controls.Add(this.parameter_diffusivity_textbox);
            this.Creeper.Controls.Add(this.creep_active_checkbox);
            this.Creeper.Location = new System.Drawing.Point(4, 22);
            this.Creeper.Name = "Creeper";
            this.Creeper.Size = new System.Drawing.Size(732, 250);
            this.Creeper.TabIndex = 6;
            this.Creeper.Text = "Creep";
            this.Creeper.UseVisualStyleBackColor = true;
            // 
            // creep_testing
            // 
            this.creep_testing.AutoSize = true;
            this.creep_testing.Location = new System.Drawing.Point(26, 108);
            this.creep_testing.Name = "creep_testing";
            this.creep_testing.Size = new System.Drawing.Size(88, 17);
            this.creep_testing.TabIndex = 26;
            this.creep_testing.Text = "Creep testing";
            this.creep_testing.UseVisualStyleBackColor = true;
            // 
            // pictureBox3
            // 
            this.pictureBox3.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox3.Image")));
            this.pictureBox3.Location = new System.Drawing.Point(276, 57);
            this.pictureBox3.Name = "pictureBox3";
            this.pictureBox3.Size = new System.Drawing.Size(180, 137);
            this.pictureBox3.TabIndex = 25;
            this.pictureBox3.TabStop = false;
            // 
            // label19
            // 
            this.label19.AutoSize = true;
            this.label19.Location = new System.Drawing.Point(128, 63);
            this.label19.Name = "label19";
            this.label19.Size = new System.Drawing.Size(127, 13);
            this.label19.TabIndex = 23;
            this.label19.Text = "potential creep [kg/m2/y]";
            // 
            // parameter_diffusivity_textbox
            // 
            this.parameter_diffusivity_textbox.AcceptsTab = true;
            this.parameter_diffusivity_textbox.Location = new System.Drawing.Point(53, 60);
            this.parameter_diffusivity_textbox.Name = "parameter_diffusivity_textbox";
            this.parameter_diffusivity_textbox.Size = new System.Drawing.Size(53, 20);
            this.parameter_diffusivity_textbox.TabIndex = 21;
            this.parameter_diffusivity_textbox.Text = "4.5";
            // 
            // creep_active_checkbox
            // 
            this.creep_active_checkbox.AutoSize = true;
            this.creep_active_checkbox.Location = new System.Drawing.Point(26, 18);
            this.creep_active_checkbox.Name = "creep_active_checkbox";
            this.creep_active_checkbox.Size = new System.Drawing.Size(124, 17);
            this.creep_active_checkbox.TabIndex = 20;
            this.creep_active_checkbox.Text = "Activate this process";
            this.creep_active_checkbox.UseVisualStyleBackColor = true;
            // 
            // Tectonics
            // 
            this.Tectonics.Controls.Add(this.groupBox14);
            this.Tectonics.Controls.Add(this.groupBox4);
            this.Tectonics.Location = new System.Drawing.Point(4, 22);
            this.Tectonics.Name = "Tectonics";
            this.Tectonics.Padding = new System.Windows.Forms.Padding(3);
            this.Tectonics.Size = new System.Drawing.Size(732, 250);
            this.Tectonics.TabIndex = 7;
            this.Tectonics.Text = "Tectonics";
            this.Tectonics.UseVisualStyleBackColor = true;
            // 
            // groupBox14
            // 
            this.groupBox14.Controls.Add(this.groupBox16);
            this.groupBox14.Controls.Add(this.Uplift_rate_textbox);
            this.groupBox14.Controls.Add(this.uplift_active_checkbox);
            this.groupBox14.Controls.Add(this.label39);
            this.groupBox14.Location = new System.Drawing.Point(176, 16);
            this.groupBox14.Name = "groupBox14";
            this.groupBox14.Size = new System.Drawing.Size(158, 209);
            this.groupBox14.TabIndex = 4;
            this.groupBox14.TabStop = false;
            this.groupBox14.Text = "Vertical uplift";
            // 
            // groupBox16
            // 
            this.groupBox16.Controls.Add(this.text_lift_col_less);
            this.groupBox16.Controls.Add(this.text_lift_col_more);
            this.groupBox16.Controls.Add(this.text_lift_row_less);
            this.groupBox16.Controls.Add(this.text_lift_row_more);
            this.groupBox16.Controls.Add(this.radio_lift_col_less_than);
            this.groupBox16.Controls.Add(this.radio_lift_row_more_than);
            this.groupBox16.Controls.Add(this.radio_lift_col_more_than);
            this.groupBox16.Controls.Add(this.radio_lift_row_less_than);
            this.groupBox16.Location = new System.Drawing.Point(13, 51);
            this.groupBox16.Name = "groupBox16";
            this.groupBox16.Size = new System.Drawing.Size(129, 105);
            this.groupBox16.TabIndex = 7;
            this.groupBox16.TabStop = false;
            this.groupBox16.Text = "For cells with:";
            // 
            // text_lift_col_less
            // 
            this.text_lift_col_less.Location = new System.Drawing.Point(63, 75);
            this.text_lift_col_less.Name = "text_lift_col_less";
            this.text_lift_col_less.Size = new System.Drawing.Size(54, 20);
            this.text_lift_col_less.TabIndex = 9;
            // 
            // text_lift_col_more
            // 
            this.text_lift_col_more.Location = new System.Drawing.Point(63, 56);
            this.text_lift_col_more.Name = "text_lift_col_more";
            this.text_lift_col_more.Size = new System.Drawing.Size(54, 20);
            this.text_lift_col_more.TabIndex = 8;
            // 
            // text_lift_row_less
            // 
            this.text_lift_row_less.Location = new System.Drawing.Point(63, 36);
            this.text_lift_row_less.Name = "text_lift_row_less";
            this.text_lift_row_less.Size = new System.Drawing.Size(54, 20);
            this.text_lift_row_less.TabIndex = 7;
            // 
            // text_lift_row_more
            // 
            this.text_lift_row_more.Location = new System.Drawing.Point(63, 16);
            this.text_lift_row_more.Name = "text_lift_row_more";
            this.text_lift_row_more.Size = new System.Drawing.Size(54, 20);
            this.text_lift_row_more.TabIndex = 6;
            // 
            // radio_lift_col_less_than
            // 
            this.radio_lift_col_less_than.AutoSize = true;
            this.radio_lift_col_less_than.Location = new System.Drawing.Point(6, 75);
            this.radio_lift_col_less_than.Name = "radio_lift_col_less_than";
            this.radio_lift_col_less_than.Size = new System.Drawing.Size(48, 17);
            this.radio_lift_col_less_than.TabIndex = 5;
            this.radio_lift_col_less_than.TabStop = true;
            this.radio_lift_col_less_than.Text = "col <";
            this.radio_lift_col_less_than.UseVisualStyleBackColor = true;
            // 
            // radio_lift_row_more_than
            // 
            this.radio_lift_row_more_than.AutoSize = true;
            this.radio_lift_row_more_than.Location = new System.Drawing.Point(6, 16);
            this.radio_lift_row_more_than.Name = "radio_lift_row_more_than";
            this.radio_lift_row_more_than.Size = new System.Drawing.Size(51, 17);
            this.radio_lift_row_more_than.TabIndex = 4;
            this.radio_lift_row_more_than.TabStop = true;
            this.radio_lift_row_more_than.Text = "row >";
            this.radio_lift_row_more_than.UseVisualStyleBackColor = true;
            // 
            // radio_lift_col_more_than
            // 
            this.radio_lift_col_more_than.AutoSize = true;
            this.radio_lift_col_more_than.Location = new System.Drawing.Point(6, 56);
            this.radio_lift_col_more_than.Name = "radio_lift_col_more_than";
            this.radio_lift_col_more_than.Size = new System.Drawing.Size(48, 17);
            this.radio_lift_col_more_than.TabIndex = 3;
            this.radio_lift_col_more_than.TabStop = true;
            this.radio_lift_col_more_than.Text = "col >";
            this.radio_lift_col_more_than.UseVisualStyleBackColor = true;
            // 
            // radio_lift_row_less_than
            // 
            this.radio_lift_row_less_than.AutoSize = true;
            this.radio_lift_row_less_than.Location = new System.Drawing.Point(6, 36);
            this.radio_lift_row_less_than.Name = "radio_lift_row_less_than";
            this.radio_lift_row_less_than.Size = new System.Drawing.Size(51, 17);
            this.radio_lift_row_less_than.TabIndex = 2;
            this.radio_lift_row_less_than.TabStop = true;
            this.radio_lift_row_less_than.Text = "row <";
            this.radio_lift_row_less_than.UseVisualStyleBackColor = true;
            // 
            // Uplift_rate_textbox
            // 
            this.Uplift_rate_textbox.Location = new System.Drawing.Point(13, 184);
            this.Uplift_rate_textbox.Name = "Uplift_rate_textbox";
            this.Uplift_rate_textbox.Size = new System.Drawing.Size(100, 20);
            this.Uplift_rate_textbox.TabIndex = 3;
            // 
            // uplift_active_checkbox
            // 
            this.uplift_active_checkbox.AutoSize = true;
            this.uplift_active_checkbox.Location = new System.Drawing.Point(13, 19);
            this.uplift_active_checkbox.Name = "uplift_active_checkbox";
            this.uplift_active_checkbox.Size = new System.Drawing.Size(65, 17);
            this.uplift_active_checkbox.TabIndex = 1;
            this.uplift_active_checkbox.Text = "Activate";
            this.uplift_active_checkbox.UseVisualStyleBackColor = true;
            // 
            // label39
            // 
            this.label39.AutoSize = true;
            this.label39.Location = new System.Drawing.Point(10, 168);
            this.label39.Name = "label39";
            this.label39.Size = new System.Drawing.Size(83, 13);
            this.label39.TabIndex = 2;
            this.label39.Text = "Uplift rate [m/a]:";
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.label38);
            this.groupBox4.Controls.Add(this.Tilting_rate_textbox);
            this.groupBox4.Controls.Add(this.groupBox15);
            this.groupBox4.Controls.Add(this.tilting_active_checkbox);
            this.groupBox4.Location = new System.Drawing.Point(13, 16);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(153, 210);
            this.groupBox4.TabIndex = 3;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "Tilting";
            // 
            // label38
            // 
            this.label38.AutoSize = true;
            this.label38.Location = new System.Drawing.Point(9, 168);
            this.label38.Name = "label38";
            this.label38.Size = new System.Drawing.Size(114, 13);
            this.label38.TabIndex = 8;
            this.label38.Text = "Max alt change [m/a]: ";
            // 
            // Tilting_rate_textbox
            // 
            this.Tilting_rate_textbox.Location = new System.Drawing.Point(6, 184);
            this.Tilting_rate_textbox.Name = "Tilting_rate_textbox";
            this.Tilting_rate_textbox.Size = new System.Drawing.Size(100, 20);
            this.Tilting_rate_textbox.TabIndex = 7;
            // 
            // groupBox15
            // 
            this.groupBox15.Controls.Add(this.radio_tilt_col_max);
            this.groupBox15.Controls.Add(this.radio_tilt_row_zero);
            this.groupBox15.Controls.Add(this.radio_tilt_col_zero);
            this.groupBox15.Controls.Add(this.radio_tilt_row_max);
            this.groupBox15.Location = new System.Drawing.Point(6, 51);
            this.groupBox15.Name = "groupBox15";
            this.groupBox15.Size = new System.Drawing.Size(113, 105);
            this.groupBox15.TabIndex = 6;
            this.groupBox15.TabStop = false;
            this.groupBox15.Text = "Stability along:";
            // 
            // radio_tilt_col_max
            // 
            this.radio_tilt_col_max.AutoSize = true;
            this.radio_tilt_col_max.Location = new System.Drawing.Point(6, 79);
            this.radio_tilt_col_max.Name = "radio_tilt_col_max";
            this.radio_tilt_col_max.Size = new System.Drawing.Size(87, 17);
            this.radio_tilt_col_max.TabIndex = 5;
            this.radio_tilt_col_max.TabStop = true;
            this.radio_tilt_col_max.Text = "col = max col";
            this.radio_tilt_col_max.UseVisualStyleBackColor = true;
            // 
            // radio_tilt_row_zero
            // 
            this.radio_tilt_row_zero.AutoSize = true;
            this.radio_tilt_row_zero.Location = new System.Drawing.Point(6, 16);
            this.radio_tilt_row_zero.Name = "radio_tilt_row_zero";
            this.radio_tilt_row_zero.Size = new System.Drawing.Size(60, 17);
            this.radio_tilt_row_zero.TabIndex = 4;
            this.radio_tilt_row_zero.TabStop = true;
            this.radio_tilt_row_zero.Text = "row = 0";
            this.radio_tilt_row_zero.UseVisualStyleBackColor = true;
            // 
            // radio_tilt_col_zero
            // 
            this.radio_tilt_col_zero.AutoSize = true;
            this.radio_tilt_col_zero.Location = new System.Drawing.Point(6, 56);
            this.radio_tilt_col_zero.Name = "radio_tilt_col_zero";
            this.radio_tilt_col_zero.Size = new System.Drawing.Size(57, 17);
            this.radio_tilt_col_zero.TabIndex = 3;
            this.radio_tilt_col_zero.TabStop = true;
            this.radio_tilt_col_zero.Text = "col = 0";
            this.radio_tilt_col_zero.UseVisualStyleBackColor = true;
            // 
            // radio_tilt_row_max
            // 
            this.radio_tilt_row_max.AutoSize = true;
            this.radio_tilt_row_max.Location = new System.Drawing.Point(6, 36);
            this.radio_tilt_row_max.Name = "radio_tilt_row_max";
            this.radio_tilt_row_max.Size = new System.Drawing.Size(93, 17);
            this.radio_tilt_row_max.TabIndex = 2;
            this.radio_tilt_row_max.TabStop = true;
            this.radio_tilt_row_max.Text = "row = max row";
            this.radio_tilt_row_max.UseVisualStyleBackColor = true;
            // 
            // tilting_active_checkbox
            // 
            this.tilting_active_checkbox.AutoSize = true;
            this.tilting_active_checkbox.Location = new System.Drawing.Point(6, 19);
            this.tilting_active_checkbox.Name = "tilting_active_checkbox";
            this.tilting_active_checkbox.Size = new System.Drawing.Size(65, 17);
            this.tilting_active_checkbox.TabIndex = 0;
            this.tilting_active_checkbox.Text = "Activate";
            this.tilting_active_checkbox.UseVisualStyleBackColor = true;
            // 
            // Creep_Checkbox
            // 
            this.Creep_Checkbox.AutoSize = true;
            this.Creep_Checkbox.Enabled = false;
            this.Creep_Checkbox.Location = new System.Drawing.Point(26, 16);
            this.Creep_Checkbox.Name = "Creep_Checkbox";
            this.Creep_Checkbox.Size = new System.Drawing.Size(124, 17);
            this.Creep_Checkbox.TabIndex = 1;
            this.Creep_Checkbox.Text = "Activate this process";
            this.Creep_Checkbox.UseVisualStyleBackColor = true;
            // 
            // tabControl1
            // 
            this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl1.Controls.Add(this.Geomorph);
            this.tabControl1.Controls.Add(this.Soil);
            this.tabControl1.Controls.Add(this.Geochron);
            this.tabControl1.Controls.Add(this.Input);
            this.tabControl1.Controls.Add(this.Run);
            this.tabControl1.Controls.Add(this.Output);
            this.tabControl1.Controls.Add(this.Experimental);
            this.tabControl1.Location = new System.Drawing.Point(4, 12);
            this.tabControl1.MaximumSize = new System.Drawing.Size(811, 319);
            this.tabControl1.MinimumSize = new System.Drawing.Size(811, 319);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(811, 319);
            this.tabControl1.TabIndex = 143;
            // 
            // Soil
            // 
            this.Soil.Controls.Add(this.tabControl2);
            this.Soil.Location = new System.Drawing.Point(4, 22);
            this.Soil.Name = "Soil";
            this.Soil.Size = new System.Drawing.Size(803, 293);
            this.Soil.TabIndex = 9;
            this.Soil.Text = "Soil forming processes";
            this.Soil.UseVisualStyleBackColor = true;
            // 
            // tabControl2
            // 
            this.tabControl2.Controls.Add(this.Rock_weathering);
            this.tabControl2.Controls.Add(this.physical);
            this.tabControl2.Controls.Add(this.chemical);
            this.tabControl2.Controls.Add(this.clay);
            this.tabControl2.Controls.Add(this.bioturbation);
            this.tabControl2.Controls.Add(this.carbon);
            this.tabControl2.Location = new System.Drawing.Point(16, 15);
            this.tabControl2.Name = "tabControl2";
            this.tabControl2.SelectedIndex = 0;
            this.tabControl2.Size = new System.Drawing.Size(759, 261);
            this.tabControl2.TabIndex = 0;
            // 
            // Rock_weathering
            // 
            this.Rock_weathering.Controls.Add(this.rockweath_method);
            this.Rock_weathering.Controls.Add(this.pictureBox6);
            this.Rock_weathering.Controls.Add(this.groupBox10);
            this.Rock_weathering.Controls.Add(this.groupBox9);
            this.Rock_weathering.Location = new System.Drawing.Point(4, 22);
            this.Rock_weathering.Name = "Rock_weathering";
            this.Rock_weathering.Size = new System.Drawing.Size(751, 235);
            this.Rock_weathering.TabIndex = 6;
            this.Rock_weathering.Text = "Soil production";
            this.Rock_weathering.UseVisualStyleBackColor = true;
            // 
            // rockweath_method
            // 
            this.rockweath_method.AllowDrop = true;
            this.rockweath_method.FormattingEnabled = true;
            this.rockweath_method.Items.AddRange(new object[] {
            "Humped",
            "Exponential (-P0 exp(-k1*dsoil))",
            "Function of infiltration (only with daily water flow)"});
            this.rockweath_method.Location = new System.Drawing.Point(26, 200);
            this.rockweath_method.Name = "rockweath_method";
            this.rockweath_method.Size = new System.Drawing.Size(121, 21);
            this.rockweath_method.TabIndex = 15;
            this.rockweath_method.Text = "Humped";
            // 
            // pictureBox6
            // 
            this.pictureBox6.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox6.Image")));
            this.pictureBox6.Location = new System.Drawing.Point(276, 57);
            this.pictureBox6.Name = "pictureBox6";
            this.pictureBox6.Size = new System.Drawing.Size(180, 137);
            this.pictureBox6.TabIndex = 14;
            this.pictureBox6.TabStop = false;
            // 
            // groupBox10
            // 
            this.groupBox10.Controls.Add(this.Frost_weathering_checkbox);
            this.groupBox10.Enabled = false;
            this.groupBox10.Location = new System.Drawing.Point(250, 14);
            this.groupBox10.Name = "groupBox10";
            this.groupBox10.Size = new System.Drawing.Size(222, 179);
            this.groupBox10.TabIndex = 6;
            this.groupBox10.TabStop = false;
            this.groupBox10.Text = "Frost weathering ";
            this.groupBox10.Visible = false;
            // 
            // Frost_weathering_checkbox
            // 
            this.Frost_weathering_checkbox.AutoSize = true;
            this.Frost_weathering_checkbox.Enabled = false;
            this.Frost_weathering_checkbox.Location = new System.Drawing.Point(14, 19);
            this.Frost_weathering_checkbox.Name = "Frost_weathering_checkbox";
            this.Frost_weathering_checkbox.Size = new System.Drawing.Size(124, 17);
            this.Frost_weathering_checkbox.TabIndex = 3;
            this.Frost_weathering_checkbox.Text = "Activate this process";
            this.Frost_weathering_checkbox.UseVisualStyleBackColor = true;
            // 
            // physical
            // 
            this.physical.Controls.Add(label43);
            this.physical.Controls.Add(label42);
            this.physical.Controls.Add(label41);
            this.physical.Controls.Add(this.physical_weath_constant2);
            this.physical.Controls.Add(this.physical_weath_constant1);
            this.physical.Controls.Add(this.Physical_weath_C1_textbox);
            this.physical.Controls.Add(this.soil_phys_weath_checkbox);
            this.physical.Location = new System.Drawing.Point(4, 22);
            this.physical.Name = "physical";
            this.physical.Padding = new System.Windows.Forms.Padding(3);
            this.physical.Size = new System.Drawing.Size(751, 235);
            this.physical.TabIndex = 0;
            this.physical.Text = "Physical weathering";
            this.physical.UseVisualStyleBackColor = true;
            // 
            // physical_weath_constant2
            // 
            this.physical_weath_constant2.Location = new System.Drawing.Point(35, 95);
            this.physical_weath_constant2.Name = "physical_weath_constant2";
            this.physical_weath_constant2.Size = new System.Drawing.Size(100, 20);
            this.physical_weath_constant2.TabIndex = 4;
            this.physical_weath_constant2.Text = "5";
            // 
            // physical_weath_constant1
            // 
            this.physical_weath_constant1.ImeMode = System.Windows.Forms.ImeMode.Off;
            this.physical_weath_constant1.Location = new System.Drawing.Point(35, 69);
            this.physical_weath_constant1.Name = "physical_weath_constant1";
            this.physical_weath_constant1.Size = new System.Drawing.Size(100, 20);
            this.physical_weath_constant1.TabIndex = 3;
            this.physical_weath_constant1.Text = "2";
            // 
            // Physical_weath_C1_textbox
            // 
            this.Physical_weath_C1_textbox.Location = new System.Drawing.Point(35, 43);
            this.Physical_weath_C1_textbox.Name = "Physical_weath_C1_textbox";
            this.Physical_weath_C1_textbox.Size = new System.Drawing.Size(100, 20);
            this.Physical_weath_C1_textbox.TabIndex = 2;
            this.Physical_weath_C1_textbox.Text = "0.000000004";
            // 
            // soil_phys_weath_checkbox
            // 
            this.soil_phys_weath_checkbox.AutoSize = true;
            this.soil_phys_weath_checkbox.Checked = true;
            this.soil_phys_weath_checkbox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.soil_phys_weath_checkbox.Location = new System.Drawing.Point(21, 6);
            this.soil_phys_weath_checkbox.Name = "soil_phys_weath_checkbox";
            this.soil_phys_weath_checkbox.Size = new System.Drawing.Size(124, 17);
            this.soil_phys_weath_checkbox.TabIndex = 1;
            this.soil_phys_weath_checkbox.Text = "Activate this process";
            this.soil_phys_weath_checkbox.UseVisualStyleBackColor = true;
            // 
            // chemical
            // 
            this.chemical.Controls.Add(this.chem_weath_specific_coefficient_textbox);
            this.chemical.Controls.Add(this.chem_weath_depth_constant_textbox);
            this.chemical.Controls.Add(this.chem_weath_rate_constant_textbox);
            this.chemical.Controls.Add(label53);
            this.chemical.Controls.Add(label50);
            this.chemical.Controls.Add(label51);
            this.chemical.Controls.Add(label52);
            this.chemical.Controls.Add(this.soil_chem_weath_checkbox);
            this.chemical.Location = new System.Drawing.Point(4, 22);
            this.chemical.Name = "chemical";
            this.chemical.Padding = new System.Windows.Forms.Padding(3);
            this.chemical.Size = new System.Drawing.Size(751, 235);
            this.chemical.TabIndex = 1;
            this.chemical.Text = "Chemical weathering";
            this.chemical.UseVisualStyleBackColor = true;
            // 
            // chem_weath_specific_coefficient_textbox
            // 
            this.chem_weath_specific_coefficient_textbox.Location = new System.Drawing.Point(29, 90);
            this.chem_weath_specific_coefficient_textbox.Name = "chem_weath_specific_coefficient_textbox";
            this.chem_weath_specific_coefficient_textbox.Size = new System.Drawing.Size(100, 20);
            this.chem_weath_specific_coefficient_textbox.TabIndex = 15;
            this.chem_weath_specific_coefficient_textbox.Text = "1";
            // 
            // chem_weath_depth_constant_textbox
            // 
            this.chem_weath_depth_constant_textbox.Location = new System.Drawing.Point(29, 64);
            this.chem_weath_depth_constant_textbox.Name = "chem_weath_depth_constant_textbox";
            this.chem_weath_depth_constant_textbox.Size = new System.Drawing.Size(100, 20);
            this.chem_weath_depth_constant_textbox.TabIndex = 14;
            this.chem_weath_depth_constant_textbox.Text = "0.4";
            // 
            // chem_weath_rate_constant_textbox
            // 
            this.chem_weath_rate_constant_textbox.Location = new System.Drawing.Point(29, 38);
            this.chem_weath_rate_constant_textbox.Name = "chem_weath_rate_constant_textbox";
            this.chem_weath_rate_constant_textbox.Size = new System.Drawing.Size(100, 20);
            this.chem_weath_rate_constant_textbox.TabIndex = 13;
            this.chem_weath_rate_constant_textbox.Text = "0.000000004";
            // 
            // soil_chem_weath_checkbox
            // 
            this.soil_chem_weath_checkbox.AutoSize = true;
            this.soil_chem_weath_checkbox.Location = new System.Drawing.Point(29, 6);
            this.soil_chem_weath_checkbox.Name = "soil_chem_weath_checkbox";
            this.soil_chem_weath_checkbox.Size = new System.Drawing.Size(124, 17);
            this.soil_chem_weath_checkbox.TabIndex = 1;
            this.soil_chem_weath_checkbox.Text = "Activate this process";
            this.soil_chem_weath_checkbox.UseVisualStyleBackColor = true;
            // 
            // clay
            // 
            this.clay.Controls.Add(this.ct_Jagercikova);
            this.clay.Controls.Add(this.label109);
            this.clay.Controls.Add(this.label108);
            this.clay.Controls.Add(this.ct_dd_Jagercikova);
            this.clay.Controls.Add(this.ct_v0_Jagercikova);
            this.clay.Controls.Add(this.ct_depth_decay);
            this.clay.Controls.Add(this.eluviation_coefficient_textbox);
            this.clay.Controls.Add(this.maximum_eluviation_textbox);
            this.clay.Controls.Add(this.clay_neoform_C2_textbox);
            this.clay.Controls.Add(this.clay_neoform_C1_textbox);
            this.clay.Controls.Add(this.clay_neoform_constant_textbox);
            this.clay.Controls.Add(label13);
            this.clay.Controls.Add(this.CT_depth_decay_checkbox);
            this.clay.Controls.Add(label69);
            this.clay.Controls.Add(label70);
            this.clay.Controls.Add(eluviation_rate_constant);
            this.clay.Controls.Add(label72);
            this.clay.Controls.Add(label64);
            this.clay.Controls.Add(label65);
            this.clay.Controls.Add(label66);
            this.clay.Controls.Add(label67);
            this.clay.Controls.Add(label60);
            this.clay.Controls.Add(this.soil_clay_transloc_checkbox);
            this.clay.Location = new System.Drawing.Point(4, 22);
            this.clay.Name = "clay";
            this.clay.Size = new System.Drawing.Size(751, 235);
            this.clay.TabIndex = 2;
            this.clay.Text = "Clay dynamics";
            this.clay.UseVisualStyleBackColor = true;
            // 
            // ct_Jagercikova
            // 
            this.ct_Jagercikova.AutoSize = true;
            this.ct_Jagercikova.Location = new System.Drawing.Point(540, 52);
            this.ct_Jagercikova.Name = "ct_Jagercikova";
            this.ct_Jagercikova.Size = new System.Drawing.Size(179, 17);
            this.ct_Jagercikova.TabIndex = 62;
            this.ct_Jagercikova.Text = "Advection equation Jagercikova";
            this.ct_Jagercikova.UseVisualStyleBackColor = true;
            // 
            // label109
            // 
            this.label109.AutoSize = true;
            this.label109.Location = new System.Drawing.Point(602, 108);
            this.label109.Name = "label109";
            this.label109.Size = new System.Drawing.Size(98, 13);
            this.label109.TabIndex = 61;
            this.label109.Text = "depth decay [cm-1]";
            // 
            // label108
            // 
            this.label108.AutoSize = true;
            this.label108.Location = new System.Drawing.Point(597, 78);
            this.label108.Name = "label108";
            this.label108.Size = new System.Drawing.Size(148, 13);
            this.label108.TabIndex = 60;
            this.label108.Text = "surface advection v0 [cm a-1]";
            // 
            // ct_dd_Jagercikova
            // 
            this.ct_dd_Jagercikova.Location = new System.Drawing.Point(540, 104);
            this.ct_dd_Jagercikova.Name = "ct_dd_Jagercikova";
            this.ct_dd_Jagercikova.Size = new System.Drawing.Size(51, 20);
            this.ct_dd_Jagercikova.TabIndex = 58;
            this.ct_dd_Jagercikova.Text = "0.09";
            // 
            // ct_v0_Jagercikova
            // 
            this.ct_v0_Jagercikova.Location = new System.Drawing.Point(540, 75);
            this.ct_v0_Jagercikova.Name = "ct_v0_Jagercikova";
            this.ct_v0_Jagercikova.Size = new System.Drawing.Size(51, 20);
            this.ct_v0_Jagercikova.TabIndex = 57;
            this.ct_v0_Jagercikova.Text = "0.18";
            // 
            // ct_depth_decay
            // 
            this.ct_depth_decay.Location = new System.Drawing.Point(303, 169);
            this.ct_depth_decay.Name = "ct_depth_decay";
            this.ct_depth_decay.Size = new System.Drawing.Size(100, 20);
            this.ct_depth_decay.TabIndex = 55;
            this.ct_depth_decay.Text = "2";
            // 
            // eluviation_coefficient_textbox
            // 
            this.eluviation_coefficient_textbox.Location = new System.Drawing.Point(304, 101);
            this.eluviation_coefficient_textbox.Name = "eluviation_coefficient_textbox";
            this.eluviation_coefficient_textbox.Size = new System.Drawing.Size(100, 20);
            this.eluviation_coefficient_textbox.TabIndex = 49;
            this.eluviation_coefficient_textbox.Text = "2";
            // 
            // maximum_eluviation_textbox
            // 
            this.maximum_eluviation_textbox.Location = new System.Drawing.Point(304, 75);
            this.maximum_eluviation_textbox.Name = "maximum_eluviation_textbox";
            this.maximum_eluviation_textbox.Size = new System.Drawing.Size(100, 20);
            this.maximum_eluviation_textbox.TabIndex = 48;
            this.maximum_eluviation_textbox.Text = "0.007";
            // 
            // clay_neoform_C2_textbox
            // 
            this.clay_neoform_C2_textbox.Location = new System.Drawing.Point(25, 127);
            this.clay_neoform_C2_textbox.Name = "clay_neoform_C2_textbox";
            this.clay_neoform_C2_textbox.Size = new System.Drawing.Size(100, 20);
            this.clay_neoform_C2_textbox.TabIndex = 42;
            this.clay_neoform_C2_textbox.Text = "20";
            // 
            // clay_neoform_C1_textbox
            // 
            this.clay_neoform_C1_textbox.Location = new System.Drawing.Point(25, 101);
            this.clay_neoform_C1_textbox.Name = "clay_neoform_C1_textbox";
            this.clay_neoform_C1_textbox.Size = new System.Drawing.Size(100, 20);
            this.clay_neoform_C1_textbox.TabIndex = 41;
            this.clay_neoform_C1_textbox.Text = "1";
            // 
            // clay_neoform_constant_textbox
            // 
            this.clay_neoform_constant_textbox.Location = new System.Drawing.Point(25, 75);
            this.clay_neoform_constant_textbox.Name = "clay_neoform_constant_textbox";
            this.clay_neoform_constant_textbox.Size = new System.Drawing.Size(100, 20);
            this.clay_neoform_constant_textbox.TabIndex = 40;
            this.clay_neoform_constant_textbox.Text = "0.5";
            // 
            // CT_depth_decay_checkbox
            // 
            this.CT_depth_decay_checkbox.AutoSize = true;
            this.CT_depth_decay_checkbox.Checked = true;
            this.CT_depth_decay_checkbox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.CT_depth_decay_checkbox.Location = new System.Drawing.Point(304, 146);
            this.CT_depth_decay_checkbox.Name = "CT_depth_decay_checkbox";
            this.CT_depth_decay_checkbox.Size = new System.Drawing.Size(137, 17);
            this.CT_depth_decay_checkbox.TabIndex = 54;
            this.CT_depth_decay_checkbox.Text = "Depth decay constant?";
            this.CT_depth_decay_checkbox.UseVisualStyleBackColor = true;
            // 
            // soil_clay_transloc_checkbox
            // 
            this.soil_clay_transloc_checkbox.AutoSize = true;
            this.soil_clay_transloc_checkbox.Location = new System.Drawing.Point(26, 12);
            this.soil_clay_transloc_checkbox.Name = "soil_clay_transloc_checkbox";
            this.soil_clay_transloc_checkbox.Size = new System.Drawing.Size(124, 17);
            this.soil_clay_transloc_checkbox.TabIndex = 1;
            this.soil_clay_transloc_checkbox.Text = "Activate this process";
            this.soil_clay_transloc_checkbox.UseVisualStyleBackColor = true;
            // 
            // bioturbation
            // 
            this.bioturbation.Controls.Add(label_bt_depthfunction);
            this.bioturbation.Controls.Add(this.bt_depthfunction_box);
            this.bioturbation.Controls.Add(label_bt_mounding_rate);
            this.bioturbation.Controls.Add(this.potential_bt_mounding_textbox);
            this.bioturbation.Controls.Add(label68);
            this.bioturbation.Controls.Add(label_bt_depthdecay);
            this.bioturbation.Controls.Add(label_bt_mixing_rate);
            this.bioturbation.Controls.Add(this.bt_depth_decay_textbox);
            this.bioturbation.Controls.Add(this.potential_bt_mixing_textbox);
            this.bioturbation.Controls.Add(this.soil_bioturb_checkbox);
            this.bioturbation.Location = new System.Drawing.Point(4, 22);
            this.bioturbation.Name = "bioturbation";
            this.bioturbation.Size = new System.Drawing.Size(751, 235);
            this.bioturbation.TabIndex = 3;
            this.bioturbation.Text = "Bioturbation";
            this.bioturbation.UseVisualStyleBackColor = true;
            // 
            // bt_depthfunction_box
            // 
            this.bt_depthfunction_box.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.bt_depthfunction_box.FormattingEnabled = true;
            this.bt_depthfunction_box.Items.AddRange(new object[] {
            "Exponential",
            "Linear",
            "Abrupt"});
            this.bt_depthfunction_box.Location = new System.Drawing.Point(27, 129);
            this.bt_depthfunction_box.Name = "bt_depthfunction_box";
            this.bt_depthfunction_box.Size = new System.Drawing.Size(99, 21);
            this.bt_depthfunction_box.TabIndex = 62;
            // 
            // potential_bt_mounding_textbox
            // 
            this.potential_bt_mounding_textbox.Location = new System.Drawing.Point(27, 74);
            this.potential_bt_mounding_textbox.Name = "potential_bt_mounding_textbox";
            this.potential_bt_mounding_textbox.Size = new System.Drawing.Size(100, 20);
            this.potential_bt_mounding_textbox.TabIndex = 60;
            this.potential_bt_mounding_textbox.Text = "2";
            // 
            // bt_depth_decay_textbox
            // 
            this.bt_depth_decay_textbox.Location = new System.Drawing.Point(26, 101);
            this.bt_depth_decay_textbox.Name = "bt_depth_decay_textbox";
            this.bt_depth_decay_textbox.Size = new System.Drawing.Size(100, 20);
            this.bt_depth_decay_textbox.TabIndex = 56;
            this.bt_depth_decay_textbox.Text = "0.4";
            // 
            // potential_bt_mixing_textbox
            // 
            this.potential_bt_mixing_textbox.Location = new System.Drawing.Point(26, 48);
            this.potential_bt_mixing_textbox.Name = "potential_bt_mixing_textbox";
            this.potential_bt_mixing_textbox.Size = new System.Drawing.Size(100, 20);
            this.potential_bt_mixing_textbox.TabIndex = 55;
            this.potential_bt_mixing_textbox.Text = "2";
            // 
            // soil_bioturb_checkbox
            // 
            this.soil_bioturb_checkbox.AutoSize = true;
            this.soil_bioturb_checkbox.Location = new System.Drawing.Point(26, 12);
            this.soil_bioturb_checkbox.Name = "soil_bioturb_checkbox";
            this.soil_bioturb_checkbox.Size = new System.Drawing.Size(124, 17);
            this.soil_bioturb_checkbox.TabIndex = 1;
            this.soil_bioturb_checkbox.Text = "Activate this process";
            this.soil_bioturb_checkbox.UseVisualStyleBackColor = true;
            // 
            // carbon
            // 
            this.carbon.Controls.Add(SOM_algorthm_textbox);
            this.carbon.Controls.Add(this.som_cycle_algorithm_box);
            this.carbon.Controls.Add(this.carbon_o_decomp_rate_textbox);
            this.carbon.Controls.Add(this.carbon_y_decomp_rate_textbox);
            this.carbon.Controls.Add(this.carbon_o_depth_decay_textbox);
            this.carbon.Controls.Add(this.carbon_y_depth_decay_textbox);
            this.carbon.Controls.Add(this.carbon_humification_fraction_textbox);
            this.carbon.Controls.Add(this.carbon_depth_decay_textbox);
            this.carbon.Controls.Add(this.carbon_input_textbox);
            this.carbon.Controls.Add(label86);
            this.carbon.Controls.Add(label84);
            this.carbon.Controls.Add(label83);
            this.carbon.Controls.Add(label82);
            this.carbon.Controls.Add(label80);
            this.carbon.Controls.Add(label77);
            this.carbon.Controls.Add(label81);
            this.carbon.Controls.Add(label74);
            this.carbon.Controls.Add(label75);
            this.carbon.Controls.Add(label76);
            this.carbon.Controls.Add(this.soil_carbon_cycle_checkbox);
            this.carbon.Location = new System.Drawing.Point(4, 22);
            this.carbon.Name = "carbon";
            this.carbon.Size = new System.Drawing.Size(751, 235);
            this.carbon.TabIndex = 4;
            this.carbon.Text = "Organic Matter";
            this.carbon.UseVisualStyleBackColor = true;
            // 
            // som_cycle_algorithm_box
            // 
            this.som_cycle_algorithm_box.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.som_cycle_algorithm_box.FormattingEnabled = true;
            this.som_cycle_algorithm_box.Items.AddRange(new object[] {
            "Yoo et al., 2006",
            "ICBM (Introductory Carbon Balance Model)"});
            this.som_cycle_algorithm_box.Location = new System.Drawing.Point(24, 69);
            this.som_cycle_algorithm_box.Name = "som_cycle_algorithm_box";
            this.som_cycle_algorithm_box.Size = new System.Drawing.Size(121, 21);
            this.som_cycle_algorithm_box.TabIndex = 82;
            // 
            // carbon_o_decomp_rate_textbox
            // 
            this.carbon_o_decomp_rate_textbox.Location = new System.Drawing.Point(453, 111);
            this.carbon_o_decomp_rate_textbox.Name = "carbon_o_decomp_rate_textbox";
            this.carbon_o_decomp_rate_textbox.Size = new System.Drawing.Size(100, 20);
            this.carbon_o_decomp_rate_textbox.TabIndex = 81;
            this.carbon_o_decomp_rate_textbox.Text = "0.005";
            // 
            // carbon_y_decomp_rate_textbox
            // 
            this.carbon_y_decomp_rate_textbox.Location = new System.Drawing.Point(347, 111);
            this.carbon_y_decomp_rate_textbox.Name = "carbon_y_decomp_rate_textbox";
            this.carbon_y_decomp_rate_textbox.Size = new System.Drawing.Size(100, 20);
            this.carbon_y_decomp_rate_textbox.TabIndex = 79;
            this.carbon_y_decomp_rate_textbox.Text = "0.01";
            // 
            // carbon_o_depth_decay_textbox
            // 
            this.carbon_o_depth_decay_textbox.Location = new System.Drawing.Point(453, 137);
            this.carbon_o_depth_decay_textbox.Name = "carbon_o_depth_decay_textbox";
            this.carbon_o_depth_decay_textbox.Size = new System.Drawing.Size(100, 20);
            this.carbon_o_depth_decay_textbox.TabIndex = 75;
            this.carbon_o_depth_decay_textbox.Text = "0.125";
            // 
            // carbon_y_depth_decay_textbox
            // 
            this.carbon_y_depth_decay_textbox.Location = new System.Drawing.Point(347, 137);
            this.carbon_y_depth_decay_textbox.Name = "carbon_y_depth_decay_textbox";
            this.carbon_y_depth_decay_textbox.Size = new System.Drawing.Size(100, 20);
            this.carbon_y_depth_decay_textbox.TabIndex = 73;
            this.carbon_y_depth_decay_textbox.Text = "0.125";
            // 
            // carbon_humification_fraction_textbox
            // 
            this.carbon_humification_fraction_textbox.Location = new System.Drawing.Point(23, 171);
            this.carbon_humification_fraction_textbox.Name = "carbon_humification_fraction_textbox";
            this.carbon_humification_fraction_textbox.Size = new System.Drawing.Size(100, 20);
            this.carbon_humification_fraction_textbox.TabIndex = 65;
            this.carbon_humification_fraction_textbox.Text = "0.8";
            // 
            // carbon_depth_decay_textbox
            // 
            this.carbon_depth_decay_textbox.Location = new System.Drawing.Point(23, 142);
            this.carbon_depth_decay_textbox.Name = "carbon_depth_decay_textbox";
            this.carbon_depth_decay_textbox.Size = new System.Drawing.Size(100, 20);
            this.carbon_depth_decay_textbox.TabIndex = 61;
            this.carbon_depth_decay_textbox.Text = "0.125";
            // 
            // carbon_input_textbox
            // 
            this.carbon_input_textbox.Location = new System.Drawing.Point(23, 116);
            this.carbon_input_textbox.Name = "carbon_input_textbox";
            this.carbon_input_textbox.Size = new System.Drawing.Size(100, 20);
            this.carbon_input_textbox.TabIndex = 60;
            this.carbon_input_textbox.Text = "1.5";
            // 
            // soil_carbon_cycle_checkbox
            // 
            this.soil_carbon_cycle_checkbox.AutoSize = true;
            this.soil_carbon_cycle_checkbox.Location = new System.Drawing.Point(25, 14);
            this.soil_carbon_cycle_checkbox.Name = "soil_carbon_cycle_checkbox";
            this.soil_carbon_cycle_checkbox.Size = new System.Drawing.Size(124, 17);
            this.soil_carbon_cycle_checkbox.TabIndex = 2;
            this.soil_carbon_cycle_checkbox.Text = "Activate this process";
            this.soil_carbon_cycle_checkbox.UseVisualStyleBackColor = true;
            // 
            // Geochron
            // 
            this.Geochron.Controls.Add(this.tabControl3);
            this.Geochron.Location = new System.Drawing.Point(4, 22);
            this.Geochron.Name = "Geochron";
            this.Geochron.Padding = new System.Windows.Forms.Padding(3);
            this.Geochron.Size = new System.Drawing.Size(803, 293);
            this.Geochron.TabIndex = 11;
            this.Geochron.Text = "Geochronological tracers";
            this.Geochron.UseVisualStyleBackColor = true;
            // 
            // tabControl3
            // 
            this.tabControl3.Controls.Add(this.tabPage4);
            this.tabControl3.Controls.Add(this.tabPage5);
            this.tabControl3.Location = new System.Drawing.Point(6, 6);
            this.tabControl3.Name = "tabControl3";
            this.tabControl3.SelectedIndex = 0;
            this.tabControl3.Size = new System.Drawing.Size(797, 278);
            this.tabControl3.TabIndex = 0;
            // 
            // tabPage4
            // 
            this.tabPage4.Controls.Add(this.label137);
            this.tabPage4.Controls.Add(this.OSL_inherited_textbox);
            this.tabPage4.Controls.Add(this.label122);
            this.tabPage4.Controls.Add(this.bleachingdepth_textbox);
            this.tabPage4.Controls.Add(this.label121);
            this.tabPage4.Controls.Add(this.ngrains_textbox);
            this.tabPage4.Controls.Add(this.OSL_checkbox);
            this.tabPage4.Location = new System.Drawing.Point(4, 22);
            this.tabPage4.Name = "tabPage4";
            this.tabPage4.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage4.Size = new System.Drawing.Size(789, 252);
            this.tabPage4.TabIndex = 0;
            this.tabPage4.Text = "Luminescence";
            this.tabPage4.UseVisualStyleBackColor = true;
            // 
            // label137
            // 
            this.label137.AutoSize = true;
            this.label137.Location = new System.Drawing.Point(64, 103);
            this.label137.Name = "label137";
            this.label137.Size = new System.Drawing.Size(84, 13);
            this.label137.TabIndex = 16;
            this.label137.Text = "Inherited age [a]";
            // 
            // OSL_inherited_textbox
            // 
            this.OSL_inherited_textbox.Location = new System.Drawing.Point(6, 97);
            this.OSL_inherited_textbox.Name = "OSL_inherited_textbox";
            this.OSL_inherited_textbox.Size = new System.Drawing.Size(45, 20);
            this.OSL_inherited_textbox.TabIndex = 15;
            this.OSL_inherited_textbox.Text = "0";
            // 
            // label122
            // 
            this.label122.AutoSize = true;
            this.label122.Location = new System.Drawing.Point(64, 70);
            this.label122.Name = "label122";
            this.label122.Size = new System.Drawing.Size(101, 13);
            this.label122.TabIndex = 14;
            this.label122.Text = "Bleaching depth [m]";
            // 
            // bleachingdepth_textbox
            // 
            this.bleachingdepth_textbox.Location = new System.Drawing.Point(6, 64);
            this.bleachingdepth_textbox.Name = "bleachingdepth_textbox";
            this.bleachingdepth_textbox.Size = new System.Drawing.Size(45, 20);
            this.bleachingdepth_textbox.TabIndex = 13;
            this.bleachingdepth_textbox.Text = "0.005";
            // 
            // label121
            // 
            this.label121.AutoSize = true;
            this.label121.Location = new System.Drawing.Point(60, 39);
            this.label121.Name = "label121";
            this.label121.Size = new System.Drawing.Size(190, 13);
            this.label121.TabIndex = 12;
            this.label121.Text = "Initial number of grains per kg/m2 sand";
            // 
            // ngrains_textbox
            // 
            this.ngrains_textbox.Location = new System.Drawing.Point(6, 33);
            this.ngrains_textbox.Name = "ngrains_textbox";
            this.ngrains_textbox.Size = new System.Drawing.Size(45, 20);
            this.ngrains_textbox.TabIndex = 10;
            this.ngrains_textbox.Text = "2";
            // 
            // OSL_checkbox
            // 
            this.OSL_checkbox.AutoSize = true;
            this.OSL_checkbox.Location = new System.Drawing.Point(9, 9);
            this.OSL_checkbox.Name = "OSL_checkbox";
            this.OSL_checkbox.Size = new System.Drawing.Size(124, 17);
            this.OSL_checkbox.TabIndex = 9;
            this.OSL_checkbox.Text = "Activate this process";
            this.OSL_checkbox.UseVisualStyleBackColor = true;
            // 
            // tabPage5
            // 
            this.tabPage5.Controls.Add(this.label780_cn);
            this.tabPage5.Controls.Add(this.label1310_cn);
            this.tabPage5.Controls.Add(this.label132_cn);
            this.tabPage5.Controls.Add(this.isC14_mu_input_textbox);
            this.tabPage5.Controls.Add(this.isBe10_mu_input_textbox);
            this.tabPage5.Controls.Add(this.label131_cn);
            this.tabPage5.Controls.Add(this.attenuationlength_mu_textbox);
            this.tabPage5.Controls.Add(this.label138_CN);
            this.tabPage5.Controls.Add(this.met_10Be_clayfrac);
            this.tabPage5.Controls.Add(this.label_met10Be_dd);
            this.tabPage5.Controls.Add(this.met10Be_dd);
            this.tabPage5.Controls.Add(this.label136_cn);
            this.tabPage5.Controls.Add(this.isC14_inherited_textbox);
            this.tabPage5.Controls.Add(this.isBe10_inherited_textbox);
            this.tabPage5.Controls.Add(this.label134);
            this.tabPage5.Controls.Add(this.metBe10_inherited_textbox);
            this.tabPage5.Controls.Add(this.label133_cn);
            this.tabPage5.Controls.Add(this.attenuationlength_sp_textbox);
            this.tabPage5.Controls.Add(this.label130);
            this.tabPage5.Controls.Add(this.label79_cn);
            this.tabPage5.Controls.Add(this.label78_cn);
            this.tabPage5.Controls.Add(this.C14_decay_textbox);
            this.tabPage5.Controls.Add(this.isC14_sp_input_textbox);
            this.tabPage5.Controls.Add(this.label33_cn);
            this.tabPage5.Controls.Add(this.label126);
            this.tabPage5.Controls.Add(this.label125);
            this.tabPage5.Controls.Add(this.isBe10_sp_input_textbox);
            this.tabPage5.Controls.Add(this.Be10_decay_textbox);
            this.tabPage5.Controls.Add(this.metBe10_input_textbox);
            this.tabPage5.Controls.Add(this.label124);
            this.tabPage5.Controls.Add(this.label123);
            this.tabPage5.Controls.Add(this.CN_checkbox);
            this.tabPage5.Location = new System.Drawing.Point(4, 22);
            this.tabPage5.Name = "tabPage5";
            this.tabPage5.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage5.Size = new System.Drawing.Size(789, 252);
            this.tabPage5.TabIndex = 1;
            this.tabPage5.Text = "Cosmogenic nuclides";
            this.tabPage5.UseVisualStyleBackColor = true;
            // 
            // label780_cn
            // 
            this.label780_cn.AutoSize = true;
            this.label780_cn.Location = new System.Drawing.Point(510, 117);
            this.label780_cn.Name = "label780_cn";
            this.label780_cn.Size = new System.Drawing.Size(108, 13);
            this.label780_cn.TabIndex = 47;
            this.label780_cn.Text = "[atoms g quartz-1 y-1]";
            // 
            // label1310_cn
            // 
            this.label1310_cn.AutoSize = true;
            this.label1310_cn.Location = new System.Drawing.Point(795, 183);
            this.label1310_cn.Name = "label1310_cn";
            this.label1310_cn.Size = new System.Drawing.Size(108, 13);
            this.label1310_cn.TabIndex = 46;
            this.label1310_cn.Text = "[atoms g quartz-1 y-1]";
            // 
            // label132_cn
            // 
            this.label132_cn.AutoSize = true;
            this.label132_cn.Location = new System.Drawing.Point(507, 101);
            this.label132_cn.Name = "label132_cn";
            this.label132_cn.Size = new System.Drawing.Size(176, 13);
            this.label132_cn.TabIndex = 45;
            this.label132_cn.Text = "Muon production rate at the surface";
            // 
            // isC14_mu_input_textbox
            // 
            this.isC14_mu_input_textbox.Location = new System.Drawing.Point(444, 105);
            this.isC14_mu_input_textbox.Name = "isC14_mu_input_textbox";
            this.isC14_mu_input_textbox.Size = new System.Drawing.Size(57, 20);
            this.isC14_mu_input_textbox.TabIndex = 44;
            this.isC14_mu_input_textbox.Text = "0";
            // 
            // isBe10_mu_input_textbox
            // 
            this.isBe10_mu_input_textbox.Location = new System.Drawing.Point(320, 105);
            this.isBe10_mu_input_textbox.Name = "isBe10_mu_input_textbox";
            this.isBe10_mu_input_textbox.Size = new System.Drawing.Size(57, 20);
            this.isBe10_mu_input_textbox.TabIndex = 43;
            this.isBe10_mu_input_textbox.Text = "0.084";
            // 
            // label131_cn
            // 
            this.label131_cn.AutoSize = true;
            this.label131_cn.Location = new System.Drawing.Point(379, 225);
            this.label131_cn.Name = "label131_cn";
            this.label131_cn.Size = new System.Drawing.Size(217, 13);
            this.label131_cn.TabIndex = 42;
            this.label131_cn.Text = "Attenuation length in-situ CNs Muon [kg m-2]";
            // 
            // attenuationlength_mu_textbox
            // 
            this.attenuationlength_mu_textbox.Location = new System.Drawing.Point(316, 222);
            this.attenuationlength_mu_textbox.Name = "attenuationlength_mu_textbox";
            this.attenuationlength_mu_textbox.Size = new System.Drawing.Size(57, 20);
            this.attenuationlength_mu_textbox.TabIndex = 41;
            this.attenuationlength_mu_textbox.Text = "25000";
            // 
            // label138_CN
            // 
            this.label138_CN.AutoSize = true;
            this.label138_CN.Location = new System.Drawing.Point(75, 176);
            this.label138_CN.Margin = new System.Windows.Forms.Padding(3, 0, 2, 0);
            this.label138_CN.Name = "label138_CN";
            this.label138_CN.Size = new System.Drawing.Size(133, 13);
            this.label138_CN.TabIndex = 40;
            this.label138_CN.Text = "Fraction associated to clay";
            // 
            // met_10Be_clayfrac
            // 
            this.met_10Be_clayfrac.Location = new System.Drawing.Point(12, 171);
            this.met_10Be_clayfrac.Name = "met_10Be_clayfrac";
            this.met_10Be_clayfrac.Size = new System.Drawing.Size(56, 20);
            this.met_10Be_clayfrac.TabIndex = 39;
            this.met_10Be_clayfrac.Text = "0.8";
            // 
            // label_met10Be_dd
            // 
            this.label_met10Be_dd.AutoSize = true;
            this.label_met10Be_dd.Cursor = System.Windows.Forms.Cursors.Arrow;
            this.label_met10Be_dd.Location = new System.Drawing.Point(74, 108);
            this.label_met10Be_dd.Name = "label_met10Be_dd";
            this.label_met10Be_dd.Size = new System.Drawing.Size(109, 13);
            this.label_met10Be_dd.TabIndex = 38;
            this.label_met10Be_dd.Text = "Adsorption coefficient";
            // 
            // met10Be_dd
            // 
            this.met10Be_dd.Location = new System.Drawing.Point(11, 103);
            this.met10Be_dd.Name = "met10Be_dd";
            this.met10Be_dd.Size = new System.Drawing.Size(56, 20);
            this.met10Be_dd.TabIndex = 37;
            this.met10Be_dd.Text = "4";
            // 
            // label136_cn
            // 
            this.label136_cn.AutoSize = true;
            this.label136_cn.Location = new System.Drawing.Point(508, 173);
            this.label136_cn.Name = "label136_cn";
            this.label136_cn.Size = new System.Drawing.Size(105, 13);
            this.label136_cn.TabIndex = 36;
            this.label136_cn.Text = "Inherited atoms cm-2";
            // 
            // isC14_inherited_textbox
            // 
            this.isC14_inherited_textbox.Location = new System.Drawing.Point(445, 168);
            this.isC14_inherited_textbox.Name = "isC14_inherited_textbox";
            this.isC14_inherited_textbox.Size = new System.Drawing.Size(56, 20);
            this.isC14_inherited_textbox.TabIndex = 35;
            this.isC14_inherited_textbox.Text = "0";
            // 
            // isBe10_inherited_textbox
            // 
            this.isBe10_inherited_textbox.Location = new System.Drawing.Point(317, 167);
            this.isBe10_inherited_textbox.Name = "isBe10_inherited_textbox";
            this.isBe10_inherited_textbox.Size = new System.Drawing.Size(56, 20);
            this.isBe10_inherited_textbox.TabIndex = 33;
            this.isBe10_inherited_textbox.Text = "0";
            // 
            // label134
            // 
            this.label134.AutoSize = true;
            this.label134.Location = new System.Drawing.Point(74, 206);
            this.label134.Name = "label134";
            this.label134.Size = new System.Drawing.Size(105, 13);
            this.label134.TabIndex = 32;
            this.label134.Text = "Inherited atoms cm-2";
            // 
            // metBe10_inherited_textbox
            // 
            this.metBe10_inherited_textbox.Location = new System.Drawing.Point(11, 201);
            this.metBe10_inherited_textbox.Name = "metBe10_inherited_textbox";
            this.metBe10_inherited_textbox.Size = new System.Drawing.Size(56, 20);
            this.metBe10_inherited_textbox.TabIndex = 31;
            this.metBe10_inherited_textbox.Text = "0";
            // 
            // label133_cn
            // 
            this.label133_cn.AutoSize = true;
            this.label133_cn.Location = new System.Drawing.Point(380, 199);
            this.label133_cn.Name = "label133_cn";
            this.label133_cn.Size = new System.Drawing.Size(236, 13);
            this.label133_cn.TabIndex = 30;
            this.label133_cn.Text = "Attenuation length in-situ CNs Spallation [kg m-2]";
            // 
            // attenuationlength_sp_textbox
            // 
            this.attenuationlength_sp_textbox.Location = new System.Drawing.Point(317, 196);
            this.attenuationlength_sp_textbox.Name = "attenuationlength_sp_textbox";
            this.attenuationlength_sp_textbox.Size = new System.Drawing.Size(57, 20);
            this.attenuationlength_sp_textbox.TabIndex = 29;
            this.attenuationlength_sp_textbox.Text = "1600";
            // 
            // label130
            // 
            this.label130.AutoSize = true;
            this.label130.Location = new System.Drawing.Point(508, 139);
            this.label130.Name = "label130";
            this.label130.Size = new System.Drawing.Size(61, 13);
            this.label130.TabIndex = 26;
            this.label130.Text = "Decay [y-1]";
            // 
            // label79_cn
            // 
            this.label79_cn.AutoSize = true;
            this.label79_cn.Location = new System.Drawing.Point(508, 81);
            this.label79_cn.Name = "label79_cn";
            this.label79_cn.Size = new System.Drawing.Size(108, 13);
            this.label79_cn.TabIndex = 25;
            this.label79_cn.Text = "[atoms g quartz-1 y-1]";
            // 
            // label78_cn
            // 
            this.label78_cn.AutoSize = true;
            this.label78_cn.Location = new System.Drawing.Point(508, 65);
            this.label78_cn.Name = "label78_cn";
            this.label78_cn.Size = new System.Drawing.Size(195, 13);
            this.label78_cn.TabIndex = 24;
            this.label78_cn.Text = "Spallation production rate at the surface";
            // 
            // C14_decay_textbox
            // 
            this.C14_decay_textbox.Location = new System.Drawing.Point(445, 136);
            this.C14_decay_textbox.Name = "C14_decay_textbox";
            this.C14_decay_textbox.Size = new System.Drawing.Size(57, 20);
            this.C14_decay_textbox.TabIndex = 22;
            this.C14_decay_textbox.Text = "1.21E-4";
            // 
            // isC14_sp_input_textbox
            // 
            this.isC14_sp_input_textbox.Location = new System.Drawing.Point(445, 69);
            this.isC14_sp_input_textbox.Name = "isC14_sp_input_textbox";
            this.isC14_sp_input_textbox.Size = new System.Drawing.Size(57, 20);
            this.isC14_sp_input_textbox.TabIndex = 21;
            this.isC14_sp_input_textbox.Text = "15.7";
            // 
            // label33_cn
            // 
            this.label33_cn.AutoSize = true;
            this.label33_cn.Location = new System.Drawing.Point(445, 49);
            this.label33_cn.Name = "label33_cn";
            this.label33_cn.Size = new System.Drawing.Size(87, 13);
            this.label33_cn.TabIndex = 20;
            this.label33_cn.Text = "In-situ Carbon-14";
            // 
            // label126
            // 
            this.label126.AutoSize = true;
            this.label126.Location = new System.Drawing.Point(74, 145);
            this.label126.Name = "label126";
            this.label126.Size = new System.Drawing.Size(61, 13);
            this.label126.TabIndex = 19;
            this.label126.Text = "Decay [y-1]";
            // 
            // label125
            // 
            this.label125.AutoSize = true;
            this.label125.Location = new System.Drawing.Point(74, 76);
            this.label125.Name = "label125";
            this.label125.Size = new System.Drawing.Size(111, 13);
            this.label125.TabIndex = 18;
            this.label125.Text = "Input [atoms cm-2 y-1]";
            // 
            // isBe10_sp_input_textbox
            // 
            this.isBe10_sp_input_textbox.Location = new System.Drawing.Point(321, 69);
            this.isBe10_sp_input_textbox.Name = "isBe10_sp_input_textbox";
            this.isBe10_sp_input_textbox.Size = new System.Drawing.Size(57, 20);
            this.isBe10_sp_input_textbox.TabIndex = 15;
            this.isBe10_sp_input_textbox.Text = "4.76";
            // 
            // Be10_decay_textbox
            // 
            this.Be10_decay_textbox.Location = new System.Drawing.Point(11, 140);
            this.Be10_decay_textbox.Name = "Be10_decay_textbox";
            this.Be10_decay_textbox.Size = new System.Drawing.Size(56, 20);
            this.Be10_decay_textbox.TabIndex = 14;
            this.Be10_decay_textbox.Text = "4.997E-7";
            // 
            // metBe10_input_textbox
            // 
            this.metBe10_input_textbox.Location = new System.Drawing.Point(12, 71);
            this.metBe10_input_textbox.Name = "metBe10_input_textbox";
            this.metBe10_input_textbox.Size = new System.Drawing.Size(56, 20);
            this.metBe10_input_textbox.TabIndex = 13;
            this.metBe10_input_textbox.Text = "1000000";
            // 
            // label124
            // 
            this.label124.AutoSize = true;
            this.label124.Location = new System.Drawing.Point(321, 48);
            this.label124.Name = "label124";
            this.label124.Size = new System.Drawing.Size(94, 13);
            this.label124.TabIndex = 12;
            this.label124.Text = "In-situ Beryllium-10";
            // 
            // label123
            // 
            this.label123.AutoSize = true;
            this.label123.Location = new System.Drawing.Point(12, 48);
            this.label123.Name = "label123";
            this.label123.Size = new System.Drawing.Size(107, 13);
            this.label123.TabIndex = 11;
            this.label123.Text = "Meteoric Beryllium-10";
            // 
            // CN_checkbox
            // 
            this.CN_checkbox.AutoSize = true;
            this.CN_checkbox.Location = new System.Drawing.Point(9, 9);
            this.CN_checkbox.Name = "CN_checkbox";
            this.CN_checkbox.Size = new System.Drawing.Size(124, 17);
            this.CN_checkbox.TabIndex = 10;
            this.CN_checkbox.Text = "Activate this process";
            this.CN_checkbox.UseVisualStyleBackColor = true;
            // 
            // Experimental
            // 
            this.Experimental.Controls.Add(this.tabControl4);
            this.Experimental.Location = new System.Drawing.Point(4, 22);
            this.Experimental.Name = "Experimental";
            this.Experimental.Padding = new System.Windows.Forms.Padding(3);
            this.Experimental.Size = new System.Drawing.Size(803, 293);
            this.Experimental.TabIndex = 12;
            this.Experimental.Text = "Experimental";
            this.Experimental.UseVisualStyleBackColor = true;
            // 
            // tabControl4
            // 
            this.tabControl4.Controls.Add(this.blocks);
            this.tabControl4.Controls.Add(this.Hydro);
            this.tabControl4.Controls.Add(this.treefall);
            this.tabControl4.Controls.Add(this.Options);
            this.tabControl4.Controls.Add(this.tabPage1);
            this.tabControl4.Location = new System.Drawing.Point(8, 18);
            this.tabControl4.Name = "tabControl4";
            this.tabControl4.SelectedIndex = 0;
            this.tabControl4.Size = new System.Drawing.Size(732, 269);
            this.tabControl4.TabIndex = 0;
            // 
            // blocks
            // 
            this.blocks.Controls.Add(this.label129);
            this.blocks.Controls.Add(this.label128);
            this.blocks.Controls.Add(this.label127);
            this.blocks.Controls.Add(this.blockweath_textbox);
            this.blocks.Controls.Add(this.blocksize_textbox);
            this.blocks.Controls.Add(this.hardlayerweath_textbox);
            this.blocks.Controls.Add(this.hardlayerdensity_textbox);
            this.blocks.Controls.Add(this.hardlayerelevation_textbox);
            this.blocks.Controls.Add(this.hardlayerthickness_textbox);
            this.blocks.Controls.Add(this.label63);
            this.blocks.Controls.Add(this.label62);
            this.blocks.Controls.Add(this.label61);
            this.blocks.Controls.Add(this.blocks_active_checkbox);
            this.blocks.Location = new System.Drawing.Point(4, 22);
            this.blocks.Name = "blocks";
            this.blocks.Padding = new System.Windows.Forms.Padding(3);
            this.blocks.Size = new System.Drawing.Size(724, 243);
            this.blocks.TabIndex = 10;
            this.blocks.Text = "Blocks";
            this.blocks.UseVisualStyleBackColor = true;
            // 
            // label129
            // 
            this.label129.AutoSize = true;
            this.label129.Location = new System.Drawing.Point(129, 162);
            this.label129.Name = "label129";
            this.label129.Size = new System.Drawing.Size(115, 13);
            this.label129.TabIndex = 21;
            this.label129.Text = "Minimum block size (m)";
            // 
            // label128
            // 
            this.label128.AutoSize = true;
            this.label128.Location = new System.Drawing.Point(129, 136);
            this.label128.Name = "label128";
            this.label128.Size = new System.Drawing.Size(110, 13);
            this.label128.TabIndex = 20;
            this.label128.Text = "Block weathering rate";
            // 
            // label127
            // 
            this.label127.AutoSize = true;
            this.label127.Location = new System.Drawing.Point(129, 110);
            this.label127.Name = "label127";
            this.label127.Size = new System.Drawing.Size(110, 13);
            this.label127.TabIndex = 19;
            this.label127.Text = "Hard layer weathering";
            // 
            // blockweath_textbox
            // 
            this.blockweath_textbox.Location = new System.Drawing.Point(6, 133);
            this.blockweath_textbox.Name = "blockweath_textbox";
            this.blockweath_textbox.Size = new System.Drawing.Size(100, 20);
            this.blockweath_textbox.TabIndex = 18;
            this.blockweath_textbox.Text = "0.01";
            // 
            // blocksize_textbox
            // 
            this.blocksize_textbox.Location = new System.Drawing.Point(6, 159);
            this.blocksize_textbox.Name = "blocksize_textbox";
            this.blocksize_textbox.Size = new System.Drawing.Size(100, 20);
            this.blocksize_textbox.TabIndex = 17;
            this.blocksize_textbox.Text = "0.15";
            // 
            // hardlayerweath_textbox
            // 
            this.hardlayerweath_textbox.Location = new System.Drawing.Point(6, 107);
            this.hardlayerweath_textbox.Name = "hardlayerweath_textbox";
            this.hardlayerweath_textbox.Size = new System.Drawing.Size(100, 20);
            this.hardlayerweath_textbox.TabIndex = 16;
            this.hardlayerweath_textbox.Text = "0.01";
            // 
            // hardlayerdensity_textbox
            // 
            this.hardlayerdensity_textbox.Location = new System.Drawing.Point(6, 81);
            this.hardlayerdensity_textbox.Name = "hardlayerdensity_textbox";
            this.hardlayerdensity_textbox.Size = new System.Drawing.Size(100, 20);
            this.hardlayerdensity_textbox.TabIndex = 13;
            this.hardlayerdensity_textbox.Text = "2500";
            // 
            // hardlayerelevation_textbox
            // 
            this.hardlayerelevation_textbox.Location = new System.Drawing.Point(6, 55);
            this.hardlayerelevation_textbox.Name = "hardlayerelevation_textbox";
            this.hardlayerelevation_textbox.Size = new System.Drawing.Size(100, 20);
            this.hardlayerelevation_textbox.TabIndex = 12;
            this.hardlayerelevation_textbox.Text = "1";
            // 
            // hardlayerthickness_textbox
            // 
            this.hardlayerthickness_textbox.Location = new System.Drawing.Point(6, 29);
            this.hardlayerthickness_textbox.Name = "hardlayerthickness_textbox";
            this.hardlayerthickness_textbox.Size = new System.Drawing.Size(100, 20);
            this.hardlayerthickness_textbox.TabIndex = 11;
            this.hardlayerthickness_textbox.Text = "1";
            // 
            // label63
            // 
            this.label63.AutoSize = true;
            this.label63.Location = new System.Drawing.Point(129, 84);
            this.label63.Name = "label63";
            this.label63.Size = new System.Drawing.Size(131, 13);
            this.label63.TabIndex = 15;
            this.label63.Text = "Hard layer density (kg/m3)";
            // 
            // label62
            // 
            this.label62.AutoSize = true;
            this.label62.Location = new System.Drawing.Point(129, 58);
            this.label62.Name = "label62";
            this.label62.Size = new System.Drawing.Size(118, 13);
            this.label62.TabIndex = 14;
            this.label62.Text = "Hard layer elevation (m)";
            // 
            // label61
            // 
            this.label61.AutoSize = true;
            this.label61.Location = new System.Drawing.Point(129, 32);
            this.label61.Name = "label61";
            this.label61.Size = new System.Drawing.Size(120, 13);
            this.label61.TabIndex = 10;
            this.label61.Text = "Hard layer thickness (m)";
            // 
            // blocks_active_checkbox
            // 
            this.blocks_active_checkbox.AutoSize = true;
            this.blocks_active_checkbox.Location = new System.Drawing.Point(6, 6);
            this.blocks_active_checkbox.Name = "blocks_active_checkbox";
            this.blocks_active_checkbox.Size = new System.Drawing.Size(124, 17);
            this.blocks_active_checkbox.TabIndex = 9;
            this.blocks_active_checkbox.Text = "Activate this process";
            this.blocks_active_checkbox.UseVisualStyleBackColor = true;
            // 
            // Hydro
            // 
            this.Hydro.Controls.Add(this.check_scaling_daily_weather);
            this.Hydro.Controls.Add(this.label106);
            this.Hydro.Controls.Add(this.snow_threshold_textbox);
            this.Hydro.Controls.Add(this.label105);
            this.Hydro.Controls.Add(this.snowmelt_factor_textbox);
            this.Hydro.Controls.Add(this.label104);
            this.Hydro.Controls.Add(this.latitude_min);
            this.Hydro.Controls.Add(this.label103);
            this.Hydro.Controls.Add(this.latitude_deg);
            this.Hydro.Controls.Add(this.label100);
            this.Hydro.Controls.Add(this.label101);
            this.Hydro.Controls.Add(this.label102);
            this.Hydro.Controls.Add(this.dailyT_min);
            this.Hydro.Controls.Add(this.dailyT_max);
            this.Hydro.Controls.Add(this.dailyT_avg);
            this.Hydro.Controls.Add(this.label97);
            this.Hydro.Controls.Add(this.daily_n);
            this.Hydro.Controls.Add(this.label96);
            this.Hydro.Controls.Add(this.label93);
            this.Hydro.Controls.Add(this.label89);
            this.Hydro.Controls.Add(this.label40);
            this.Hydro.Controls.Add(this.dailyET0);
            this.Hydro.Controls.Add(this.dailyD);
            this.Hydro.Controls.Add(this.dailyP);
            this.Hydro.Location = new System.Drawing.Point(4, 22);
            this.Hydro.Name = "Hydro";
            this.Hydro.Size = new System.Drawing.Size(724, 243);
            this.Hydro.TabIndex = 11;
            this.Hydro.Text = "Hydrological parameters";
            this.Hydro.UseVisualStyleBackColor = true;
            // 
            // check_scaling_daily_weather
            // 
            this.check_scaling_daily_weather.AutoSize = true;
            this.check_scaling_daily_weather.Location = new System.Drawing.Point(125, 227);
            this.check_scaling_daily_weather.Name = "check_scaling_daily_weather";
            this.check_scaling_daily_weather.Size = new System.Drawing.Size(230, 17);
            this.check_scaling_daily_weather.TabIndex = 71;
            this.check_scaling_daily_weather.Text = "Scale daily weather with annual timeseries?";
            this.check_scaling_daily_weather.UseVisualStyleBackColor = true;
            // 
            // label106
            // 
            this.label106.AutoSize = true;
            this.label106.Location = new System.Drawing.Point(394, 114);
            this.label106.Name = "label106";
            this.label106.Size = new System.Drawing.Size(236, 13);
            this.label106.TabIndex = 70;
            this.label106.Text = "Snowfall and snowmelt temperature threshold [C]";
            // 
            // snow_threshold_textbox
            // 
            this.snow_threshold_textbox.Enabled = false;
            this.snow_threshold_textbox.Location = new System.Drawing.Point(340, 111);
            this.snow_threshold_textbox.Name = "snow_threshold_textbox";
            this.snow_threshold_textbox.Size = new System.Drawing.Size(40, 20);
            this.snow_threshold_textbox.TabIndex = 69;
            this.snow_threshold_textbox.Text = "0";
            this.snow_threshold_textbox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label105
            // 
            this.label105.AutoSize = true;
            this.label105.Location = new System.Drawing.Point(394, 76);
            this.label105.Name = "label105";
            this.label105.Size = new System.Drawing.Size(174, 13);
            this.label105.TabIndex = 68;
            this.label105.Text = "Snowmelt factor [m degree-1 day-1]";
            // 
            // snowmelt_factor_textbox
            // 
            this.snowmelt_factor_textbox.Enabled = false;
            this.snowmelt_factor_textbox.Location = new System.Drawing.Point(340, 73);
            this.snowmelt_factor_textbox.Name = "snowmelt_factor_textbox";
            this.snowmelt_factor_textbox.Size = new System.Drawing.Size(40, 20);
            this.snowmelt_factor_textbox.TabIndex = 67;
            this.snowmelt_factor_textbox.Text = "0.004";
            this.snowmelt_factor_textbox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label104
            // 
            this.label104.AutoSize = true;
            this.label104.Location = new System.Drawing.Point(333, 15);
            this.label104.Name = "label104";
            this.label104.Size = new System.Drawing.Size(118, 13);
            this.label104.TabIndex = 66;
            this.label104.Text = "Properties of study area";
            // 
            // latitude_min
            // 
            this.latitude_min.Enabled = false;
            this.latitude_min.Location = new System.Drawing.Point(397, 35);
            this.latitude_min.Name = "latitude_min";
            this.latitude_min.Size = new System.Drawing.Size(44, 31);
            this.latitude_min.TabIndex = 65;
            this.latitude_min.Text = "22";
            // 
            // label103
            // 
            this.label103.AutoSize = true;
            this.label103.Location = new System.Drawing.Point(446, 38);
            this.label103.Name = "label103";
            this.label103.Size = new System.Drawing.Size(100, 13);
            this.label103.TabIndex = 64;
            this.label103.Text = "Latitude [deg], [min]";
            // 
            // latitude_deg
            // 
            this.latitude_deg.Enabled = false;
            this.latitude_deg.Location = new System.Drawing.Point(340, 35);
            this.latitude_deg.Name = "latitude_deg";
            this.latitude_deg.Size = new System.Drawing.Size(40, 20);
            this.latitude_deg.TabIndex = 63;
            this.latitude_deg.Text = "53";
            this.latitude_deg.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label100
            // 
            this.label100.AutoSize = true;
            this.label100.Location = new System.Drawing.Point(143, 148);
            this.label100.Name = "label100";
            this.label100.Size = new System.Drawing.Size(59, 13);
            this.label100.TabIndex = 62;
            this.label100.Text = "Daily T min";
            // 
            // label101
            // 
            this.label101.AutoSize = true;
            this.label101.Location = new System.Drawing.Point(143, 174);
            this.label101.Name = "label101";
            this.label101.Size = new System.Drawing.Size(62, 13);
            this.label101.TabIndex = 61;
            this.label101.Text = "Daily T max";
            // 
            // label102
            // 
            this.label102.AutoSize = true;
            this.label102.Location = new System.Drawing.Point(143, 117);
            this.label102.Name = "label102";
            this.label102.Size = new System.Drawing.Size(82, 13);
            this.label102.TabIndex = 60;
            this.label102.Text = "Daily T average";
            // 
            // dailyT_min
            // 
            this.dailyT_min.Enabled = false;
            this.dailyT_min.Location = new System.Drawing.Point(37, 145);
            this.dailyT_min.Name = "dailyT_min";
            this.dailyT_min.Size = new System.Drawing.Size(100, 20);
            this.dailyT_min.TabIndex = 59;
            this.dailyT_min.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // dailyT_max
            // 
            this.dailyT_max.Enabled = false;
            this.dailyT_max.Location = new System.Drawing.Point(37, 171);
            this.dailyT_max.Name = "dailyT_max";
            this.dailyT_max.Size = new System.Drawing.Size(100, 20);
            this.dailyT_max.TabIndex = 58;
            this.dailyT_max.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // dailyT_avg
            // 
            this.dailyT_avg.Enabled = false;
            this.dailyT_avg.Location = new System.Drawing.Point(37, 114);
            this.dailyT_avg.Name = "dailyT_avg";
            this.dailyT_avg.Size = new System.Drawing.Size(100, 20);
            this.dailyT_avg.TabIndex = 57;
            this.dailyT_avg.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label97
            // 
            this.label97.AutoSize = true;
            this.label97.Location = new System.Drawing.Point(142, 204);
            this.label97.Name = "label97";
            this.label97.Size = new System.Drawing.Size(83, 13);
            this.label97.TabIndex = 56;
            this.label97.Text = "Amount of years";
            // 
            // daily_n
            // 
            this.daily_n.Enabled = false;
            this.daily_n.Location = new System.Drawing.Point(36, 201);
            this.daily_n.Name = "daily_n";
            this.daily_n.Size = new System.Drawing.Size(100, 20);
            this.daily_n.TabIndex = 55;
            this.daily_n.Text = "6";
            this.daily_n.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label96
            // 
            this.label96.AutoSize = true;
            this.label96.Location = new System.Drawing.Point(20, 15);
            this.label96.Name = "label96";
            this.label96.Size = new System.Drawing.Size(229, 13);
            this.label96.TabIndex = 54;
            this.label96.Text = "Insert a series of yearly records of the following:";
            // 
            // label93
            // 
            this.label93.AutoSize = true;
            this.label93.Location = new System.Drawing.Point(142, 60);
            this.label93.Name = "label93";
            this.label93.Size = new System.Drawing.Size(53, 13);
            this.label93.TabIndex = 53;
            this.label93.Text = "Daily ET0";
            // 
            // label89
            // 
            this.label89.AutoSize = true;
            this.label89.Location = new System.Drawing.Point(142, 91);
            this.label89.Name = "label89";
            this.label89.Size = new System.Drawing.Size(71, 13);
            this.label89.TabIndex = 52;
            this.label89.Text = "Daily duration";
            // 
            // label40
            // 
            this.label40.AutoSize = true;
            this.label40.Location = new System.Drawing.Point(142, 34);
            this.label40.Name = "label40";
            this.label40.Size = new System.Drawing.Size(40, 13);
            this.label40.TabIndex = 51;
            this.label40.Text = "Daily P";
            // 
            // dailyET0
            // 
            this.dailyET0.Enabled = false;
            this.dailyET0.Location = new System.Drawing.Point(36, 57);
            this.dailyET0.Name = "dailyET0";
            this.dailyET0.Size = new System.Drawing.Size(100, 20);
            this.dailyET0.TabIndex = 50;
            this.dailyET0.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // dailyD
            // 
            this.dailyD.Enabled = false;
            this.dailyD.Location = new System.Drawing.Point(36, 88);
            this.dailyD.Name = "dailyD";
            this.dailyD.Size = new System.Drawing.Size(100, 20);
            this.dailyD.TabIndex = 49;
            this.dailyD.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // dailyP
            // 
            this.dailyP.Enabled = false;
            this.dailyP.Location = new System.Drawing.Point(36, 31);
            this.dailyP.Name = "dailyP";
            this.dailyP.Size = new System.Drawing.Size(100, 20);
            this.dailyP.TabIndex = 48;
            this.dailyP.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // treefall
            // 
            this.treefall.Controls.Add(this.tf_freq);
            this.treefall.Controls.Add(this.tf_age);
            this.treefall.Controls.Add(this.tf_growth);
            this.treefall.Controls.Add(this.tf_D);
            this.treefall.Controls.Add(this.tf_W);
            this.treefall.Controls.Add(this.label112);
            this.treefall.Controls.Add(this.label111);
            this.treefall.Controls.Add(this.label110);
            this.treefall.Controls.Add(this.label95);
            this.treefall.Controls.Add(this.label107);
            this.treefall.Controls.Add(this.treefall_checkbox);
            this.treefall.Location = new System.Drawing.Point(4, 22);
            this.treefall.Name = "treefall";
            this.treefall.Size = new System.Drawing.Size(724, 243);
            this.treefall.TabIndex = 13;
            this.treefall.Text = "Tree fall";
            this.treefall.UseVisualStyleBackColor = true;
            // 
            // tf_freq
            // 
            this.tf_freq.Location = new System.Drawing.Point(25, 162);
            this.tf_freq.Name = "tf_freq";
            this.tf_freq.Size = new System.Drawing.Size(53, 20);
            this.tf_freq.TabIndex = 30;
            this.tf_freq.Text = "0.00002";
            // 
            // tf_age
            // 
            this.tf_age.Location = new System.Drawing.Point(25, 131);
            this.tf_age.Name = "tf_age";
            this.tf_age.Size = new System.Drawing.Size(53, 20);
            this.tf_age.TabIndex = 28;
            this.tf_age.Text = "300";
            // 
            // tf_growth
            // 
            this.tf_growth.Location = new System.Drawing.Point(25, 103);
            this.tf_growth.Name = "tf_growth";
            this.tf_growth.Size = new System.Drawing.Size(53, 20);
            this.tf_growth.TabIndex = 26;
            this.tf_growth.Text = "150";
            // 
            // tf_D
            // 
            this.tf_D.Location = new System.Drawing.Point(25, 77);
            this.tf_D.Name = "tf_D";
            this.tf_D.Size = new System.Drawing.Size(53, 20);
            this.tf_D.TabIndex = 24;
            this.tf_D.Text = "0.7";
            // 
            // tf_W
            // 
            this.tf_W.Location = new System.Drawing.Point(25, 51);
            this.tf_W.Name = "tf_W";
            this.tf_W.Size = new System.Drawing.Size(53, 20);
            this.tf_W.TabIndex = 21;
            this.tf_W.Text = "4";
            // 
            // label112
            // 
            this.label112.AutoSize = true;
            this.label112.Location = new System.Drawing.Point(100, 165);
            this.label112.Name = "label112";
            this.label112.Size = new System.Drawing.Size(132, 13);
            this.label112.TabIndex = 29;
            this.label112.Text = "fall frequency [trees/m2/a]";
            // 
            // label111
            // 
            this.label111.AutoSize = true;
            this.label111.Location = new System.Drawing.Point(100, 134);
            this.label111.Name = "label111";
            this.label111.Size = new System.Drawing.Size(119, 13);
            this.label111.TabIndex = 27;
            this.label111.Text = "maximum age of tree [a]";
            // 
            // label110
            // 
            this.label110.AutoSize = true;
            this.label110.Location = new System.Drawing.Point(100, 106);
            this.label110.Name = "label110";
            this.label110.Size = new System.Drawing.Size(204, 13);
            this.label110.TabIndex = 25;
            this.label110.Text = "time it takes to reach these dimensions [a]";
            // 
            // label95
            // 
            this.label95.AutoSize = true;
            this.label95.Location = new System.Drawing.Point(100, 80);
            this.label95.Name = "label95";
            this.label95.Size = new System.Drawing.Size(145, 13);
            this.label95.TabIndex = 23;
            this.label95.Text = "maximum depth root mass [m]";
            // 
            // label107
            // 
            this.label107.AutoSize = true;
            this.label107.Location = new System.Drawing.Point(100, 54);
            this.label107.Name = "label107";
            this.label107.Size = new System.Drawing.Size(158, 13);
            this.label107.TabIndex = 22;
            this.label107.Text = "maximum diameter root mass [m]";
            // 
            // treefall_checkbox
            // 
            this.treefall_checkbox.AutoSize = true;
            this.treefall_checkbox.Location = new System.Drawing.Point(25, 16);
            this.treefall_checkbox.Name = "treefall_checkbox";
            this.treefall_checkbox.Size = new System.Drawing.Size(124, 17);
            this.treefall_checkbox.TabIndex = 0;
            this.treefall_checkbox.Text = "Activate this process";
            this.treefall_checkbox.UseVisualStyleBackColor = true;
            // 
            // Options
            // 
            this.Options.Controls.Add(this.version_bioturbation_Checkbox);
            this.Options.Controls.Add(this.CarboZALF_calib_stabilizationages_checkbox);
            this.Options.Controls.Add(this.daily_water);
            this.Options.Controls.Add(this.version_CarboZALF_checkbox);
            this.Options.Controls.Add(this.depressions_output_checkbox);
            this.Options.Controls.Add(this.luxlitter_checkbox);
            this.Options.Controls.Add(this.version_Konza_checkbox);
            this.Options.Controls.Add(this.button4);
            this.Options.Controls.Add(this.version_lux_checkbox);
            this.Options.Controls.Add(this.groupBox2);
            this.Options.Controls.Add(this.calibration);
            this.Options.Controls.Add(this.Spitsbergen_case_study);
            this.Options.Controls.Add(this.dtm_iterate_checkbox);
            this.Options.Location = new System.Drawing.Point(4, 22);
            this.Options.Name = "Options";
            this.Options.Size = new System.Drawing.Size(724, 243);
            this.Options.TabIndex = 12;
            this.Options.Text = "Options";
            this.Options.UseVisualStyleBackColor = true;
            // 
            // CarboZALF_calib_stabilizationages_checkbox
            // 
            this.CarboZALF_calib_stabilizationages_checkbox.AutoSize = true;
            this.CarboZALF_calib_stabilizationages_checkbox.Location = new System.Drawing.Point(137, 226);
            this.CarboZALF_calib_stabilizationages_checkbox.Name = "CarboZALF_calib_stabilizationages_checkbox";
            this.CarboZALF_calib_stabilizationages_checkbox.Size = new System.Drawing.Size(128, 17);
            this.CarboZALF_calib_stabilizationages_checkbox.TabIndex = 229;
            this.CarboZALF_calib_stabilizationages_checkbox.Text = "with stabilization ages";
            this.CarboZALF_calib_stabilizationages_checkbox.UseVisualStyleBackColor = true;
            // 
            // daily_water
            // 
            this.daily_water.AutoSize = true;
            this.daily_water.Location = new System.Drawing.Point(113, 115);
            this.daily_water.Name = "daily_water";
            this.daily_water.Size = new System.Drawing.Size(100, 17);
            this.daily_water.TabIndex = 228;
            this.daily_water.Text = "Daily water flow";
            this.daily_water.UseVisualStyleBackColor = true;
            // 
            // version_CarboZALF_checkbox
            // 
            this.version_CarboZALF_checkbox.AutoSize = true;
            this.version_CarboZALF_checkbox.Location = new System.Drawing.Point(7, 226);
            this.version_CarboZALF_checkbox.Name = "version_CarboZALF_checkbox";
            this.version_CarboZALF_checkbox.Size = new System.Drawing.Size(131, 17);
            this.version_CarboZALF_checkbox.TabIndex = 230;
            this.version_CarboZALF_checkbox.Text = "CarboZALF calibration";
            this.version_CarboZALF_checkbox.UseVisualStyleBackColor = true;
            // 
            // depressions_output_checkbox
            // 
            this.depressions_output_checkbox.AutoSize = true;
            this.depressions_output_checkbox.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.depressions_output_checkbox.Location = new System.Drawing.Point(13, 42);
            this.depressions_output_checkbox.Name = "depressions_output_checkbox";
            this.depressions_output_checkbox.Size = new System.Drawing.Size(143, 17);
            this.depressions_output_checkbox.TabIndex = 227;
            this.depressions_output_checkbox.Text = "Write depression outputs";
            this.depressions_output_checkbox.UseVisualStyleBackColor = true;
            // 
            // luxlitter_checkbox
            // 
            this.luxlitter_checkbox.AutoSize = true;
            this.luxlitter_checkbox.Location = new System.Drawing.Point(13, 115);
            this.luxlitter_checkbox.Name = "luxlitter_checkbox";
            this.luxlitter_checkbox.Size = new System.Drawing.Size(80, 17);
            this.luxlitter_checkbox.TabIndex = 167;
            this.luxlitter_checkbox.Text = "Lux litter on";
            this.luxlitter_checkbox.UseVisualStyleBackColor = true;
            // 
            // version_Konza_checkbox
            // 
            this.version_Konza_checkbox.AutoSize = true;
            this.version_Konza_checkbox.Location = new System.Drawing.Point(13, 136);
            this.version_Konza_checkbox.Name = "version_Konza_checkbox";
            this.version_Konza_checkbox.Size = new System.Drawing.Size(93, 17);
            this.version_Konza_checkbox.TabIndex = 166;
            this.version_Konza_checkbox.Text = "Konza version";
            this.version_Konza_checkbox.UseVisualStyleBackColor = true;
            // 
            // button4
            // 
            this.button4.Location = new System.Drawing.Point(13, 185);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(191, 41);
            this.button4.TabIndex = 165;
            this.button4.Text = "now purely calculate terrain derivatives";
            this.button4.UseVisualStyleBackColor = true;
            // 
            // version_lux_checkbox
            // 
            this.version_lux_checkbox.AutoSize = true;
            this.version_lux_checkbox.Location = new System.Drawing.Point(13, 92);
            this.version_lux_checkbox.Name = "version_lux_checkbox";
            this.version_lux_checkbox.Size = new System.Drawing.Size(115, 17);
            this.version_lux_checkbox.TabIndex = 164;
            this.version_lux_checkbox.Text = "Luxemburg version";
            this.version_lux_checkbox.UseVisualStyleBackColor = true;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.num_cal_paras_textbox);
            this.groupBox2.Controls.Add(this.label33);
            this.groupBox2.Controls.Add(this.obsfile_textbox);
            this.groupBox2.Controls.Add(this.calibration_ratio_reduction_parameter_textbox);
            this.groupBox2.Controls.Add(this.label119);
            this.groupBox2.Controls.Add(this.calibration_levels_textbox);
            this.groupBox2.Controls.Add(this.label116);
            this.groupBox2.Controls.Add(this.label118);
            this.groupBox2.Controls.Add(this.label117);
            this.groupBox2.Controls.Add(this.label115);
            this.groupBox2.Controls.Add(this.label114);
            this.groupBox2.Controls.Add(this.Calibration_button);
            this.groupBox2.Controls.Add(this.label113);
            this.groupBox2.Controls.Add(this.calibration_ratios_textbox);
            this.groupBox2.Location = new System.Drawing.Point(219, 8);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(430, 214);
            this.groupBox2.TabIndex = 161;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Calibration";
            // 
            // num_cal_paras_textbox
            // 
            this.num_cal_paras_textbox.Location = new System.Drawing.Point(338, 73);
            this.num_cal_paras_textbox.Name = "num_cal_paras_textbox";
            this.num_cal_paras_textbox.Size = new System.Drawing.Size(65, 20);
            this.num_cal_paras_textbox.TabIndex = 16;
            this.num_cal_paras_textbox.Text = "1";
            // 
            // label33
            // 
            this.label33.AutoSize = true;
            this.label33.Location = new System.Drawing.Point(39, 177);
            this.label33.Name = "label33";
            this.label33.Size = new System.Drawing.Size(116, 13);
            this.label33.TabIndex = 15;
            this.label33.Text = "observations (optional):";
            // 
            // obsfile_textbox
            // 
            this.obsfile_textbox.Location = new System.Drawing.Point(218, 174);
            this.obsfile_textbox.Name = "obsfile_textbox";
            this.obsfile_textbox.Size = new System.Drawing.Size(186, 20);
            this.obsfile_textbox.TabIndex = 14;
            this.obsfile_textbox.Text = "..";
            // 
            // calibration_ratio_reduction_parameter_textbox
            // 
            this.calibration_ratio_reduction_parameter_textbox.Location = new System.Drawing.Point(338, 148);
            this.calibration_ratio_reduction_parameter_textbox.Name = "calibration_ratio_reduction_parameter_textbox";
            this.calibration_ratio_reduction_parameter_textbox.Size = new System.Drawing.Size(66, 20);
            this.calibration_ratio_reduction_parameter_textbox.TabIndex = 12;
            this.calibration_ratio_reduction_parameter_textbox.Text = "1.5";
            // 
            // label119
            // 
            this.label119.AutoSize = true;
            this.label119.Location = new System.Drawing.Point(39, 151);
            this.label119.Name = "label119";
            this.label119.Size = new System.Drawing.Size(208, 13);
            this.label119.TabIndex = 11;
            this.label119.Text = "5. reduction of variations per level (if smart)";
            // 
            // calibration_levels_textbox
            // 
            this.calibration_levels_textbox.Location = new System.Drawing.Point(338, 124);
            this.calibration_levels_textbox.Name = "calibration_levels_textbox";
            this.calibration_levels_textbox.Size = new System.Drawing.Size(66, 20);
            this.calibration_levels_textbox.TabIndex = 10;
            this.calibration_levels_textbox.Text = "3";
            // 
            // label116
            // 
            this.label116.AutoSize = true;
            this.label116.Location = new System.Drawing.Point(194, 35);
            this.label116.Name = "label116";
            this.label116.Size = new System.Drawing.Size(210, 13);
            this.label116.TabIndex = 9;
            this.label116.Text = "The optimal set of parameters will be stored";
            // 
            // label118
            // 
            this.label118.AutoSize = true;
            this.label118.Location = new System.Drawing.Point(39, 127);
            this.label118.Name = "label118";
            this.label118.Size = new System.Drawing.Size(97, 13);
            this.label118.TabIndex = 8;
            this.label118.Text = "4. levels (iterations)";
            // 
            // label117
            // 
            this.label117.AutoSize = true;
            this.label117.Location = new System.Drawing.Point(39, 80);
            this.label117.Name = "label117";
            this.label117.Size = new System.Drawing.Size(176, 13);
            this.label117.TabIndex = 7;
            this.label117.Text = "2. number of parameters to calibrate";
            // 
            // label115
            // 
            this.label115.AutoSize = true;
            this.label115.Location = new System.Drawing.Point(39, 56);
            this.label115.Name = "label115";
            this.label115.Size = new System.Drawing.Size(350, 13);
            this.label115.TabIndex = 5;
            this.label115.Text = "1. define objective function in code, and describe parameters to calibrate";
            // 
            // label114
            // 
            this.label114.AutoSize = true;
            this.label114.Location = new System.Drawing.Point(39, 102);
            this.label114.Name = "label114";
            this.label114.Size = new System.Drawing.Size(132, 13);
            this.label114.TabIndex = 4;
            this.label114.Text = "3. variations per parameter";
            // 
            // Calibration_button
            // 
            this.Calibration_button.AutoSize = true;
            this.Calibration_button.Location = new System.Drawing.Point(22, 33);
            this.Calibration_button.Name = "Calibration_button";
            this.Calibration_button.Size = new System.Drawing.Size(142, 17);
            this.Calibration_button.TabIndex = 2;
            this.Calibration_button.Text = "Run calibration (iterative)";
            this.Calibration_button.UseVisualStyleBackColor = true;
            // 
            // label113
            // 
            this.label113.AutoSize = true;
            this.label113.Location = new System.Drawing.Point(73, 39);
            this.label113.Name = "label113";
            this.label113.Size = new System.Drawing.Size(0, 13);
            this.label113.TabIndex = 1;
            // 
            // calibration_ratios_textbox
            // 
            this.calibration_ratios_textbox.Location = new System.Drawing.Point(218, 99);
            this.calibration_ratios_textbox.Name = "calibration_ratios_textbox";
            this.calibration_ratios_textbox.Size = new System.Drawing.Size(186, 20);
            this.calibration_ratios_textbox.TabIndex = 0;
            this.calibration_ratios_textbox.Text = "0.25;0.5;1;2;4";
            // 
            // calibration
            // 
            this.calibration.AutoSize = true;
            this.calibration.Location = new System.Drawing.Point(13, 157);
            this.calibration.Name = "calibration";
            this.calibration.Size = new System.Drawing.Size(125, 17);
            this.calibration.TabIndex = 163;
            this.calibration.Text = "Lessivage calibration";
            this.calibration.UseVisualStyleBackColor = true;
            // 
            // Spitsbergen_case_study
            // 
            this.Spitsbergen_case_study.AutoSize = true;
            this.Spitsbergen_case_study.Location = new System.Drawing.Point(13, 71);
            this.Spitsbergen_case_study.Name = "Spitsbergen_case_study";
            this.Spitsbergen_case_study.Size = new System.Drawing.Size(119, 17);
            this.Spitsbergen_case_study.TabIndex = 162;
            this.Spitsbergen_case_study.Text = "Spitsbergen version";
            this.Spitsbergen_case_study.UseVisualStyleBackColor = true;
            // 
            // dtm_iterate_checkbox
            // 
            this.dtm_iterate_checkbox.AutoSize = true;
            this.dtm_iterate_checkbox.Location = new System.Drawing.Point(13, 17);
            this.dtm_iterate_checkbox.Name = "dtm_iterate_checkbox";
            this.dtm_iterate_checkbox.Size = new System.Drawing.Size(145, 17);
            this.dtm_iterate_checkbox.TabIndex = 160;
            this.dtm_iterate_checkbox.Text = "iterate comparable DEMs";
            this.dtm_iterate_checkbox.UseVisualStyleBackColor = true;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.label37);
            this.tabPage1.Controls.Add(this.proglacial_input_filename_textbox);
            this.tabPage1.Controls.Add(this.Proglacial_text);
            this.tabPage1.Controls.Add(this.Proglacial_checkbox);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(724, 243);
            this.tabPage1.TabIndex = 14;
            this.tabPage1.Text = "Proglacial";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // proglacial_input_filename_textbox
            // 
            this.proglacial_input_filename_textbox.Location = new System.Drawing.Point(147, 52);
            this.proglacial_input_filename_textbox.Name = "proglacial_input_filename_textbox";
            this.proglacial_input_filename_textbox.Size = new System.Drawing.Size(120, 20);
            this.proglacial_input_filename_textbox.TabIndex = 163;
            this.proglacial_input_filename_textbox.Text = "..";
            this.proglacial_input_filename_textbox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.proglacial_input_filename_textbox.Click += new System.EventHandler(this.proglacial_input_filename_textbox_TextChanged);
            // 
            // Proglacial_checkbox
            // 
            this.Proglacial_checkbox.AutoSize = true;
            this.Proglacial_checkbox.Location = new System.Drawing.Point(16, 15);
            this.Proglacial_checkbox.Name = "Proglacial_checkbox";
            this.Proglacial_checkbox.Size = new System.Drawing.Size(124, 17);
            this.Proglacial_checkbox.TabIndex = 10;
            this.Proglacial_checkbox.Text = "Activate this process";
            this.Proglacial_checkbox.UseVisualStyleBackColor = true;
            // 
            // version_bioturbation_Checkbox
            // 
            this.version_bioturbation_Checkbox.AutoSize = true;
            this.version_bioturbation_Checkbox.Location = new System.Drawing.Point(271, 226);
            this.version_bioturbation_Checkbox.Name = "version_bioturbation_Checkbox";
            this.version_bioturbation_Checkbox.Size = new System.Drawing.Size(133, 17);
            this.version_bioturbation_Checkbox.TabIndex = 231;
            this.version_bioturbation_Checkbox.Text = "Bioturbation calibration";
            this.version_bioturbation_Checkbox.UseVisualStyleBackColor = true;
            // 
            // Mother_form
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.AutoScroll = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(842, 491);
            this.Controls.Add(this.End_button);
            this.Controls.Add(this.start_button);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.statusBar1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximumSize = new System.Drawing.Size(1200, 700);
            this.Menu = this.mainMenu1;
            this.MinimumSize = new System.Drawing.Size(823, 530);
            this.Name = "Mother_form";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "LORICA - Soilscape Evolution Model ";
            this.Load += new System.EventHandler(this.Form1_Load);
            Landsliding.ResumeLayout(false);
            Landsliding.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox4)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.InfoStatusPanel)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TimeStatusPanel)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ScenarioStatusPanel)).EndInit();
            this.groupBox_landuse.ResumeLayout(false);
            this.groupBox9.ResumeLayout(false);
            this.groupBox9.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.Output.ResumeLayout(false);
            this.groupBox6.ResumeLayout(false);
            this.groupBox12.ResumeLayout(false);
            this.groupBox12.PerformLayout();
            this.groupBox11.ResumeLayout(false);
            this.groupBox11.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.Run.ResumeLayout(false);
            this.groupBox8.ResumeLayout(false);
            this.groupBox8.PerformLayout();
            this.groupBox7.ResumeLayout(false);
            this.groupBox7.PerformLayout();
            this.Input.ResumeLayout(false);
            this.groupBox17.ResumeLayout(false);
            this.groupBox17.PerformLayout();
            this.groupBox13.ResumeLayout(false);
            this.groupBox13.PerformLayout();
            this.groupBox5.ResumeLayout(false);
            this.groupBox5.PerformLayout();
            this.Geomorph.ResumeLayout(false);
            this.Process_tabs.ResumeLayout(false);
            this.Water.ResumeLayout(false);
            this.Water.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.Tillage.ResumeLayout(false);
            this.Tillage.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
            this.Creeper.ResumeLayout(false);
            this.Creeper.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox3)).EndInit();
            this.Tectonics.ResumeLayout(false);
            this.groupBox14.ResumeLayout(false);
            this.groupBox14.PerformLayout();
            this.groupBox16.ResumeLayout(false);
            this.groupBox16.PerformLayout();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.groupBox15.ResumeLayout(false);
            this.groupBox15.PerformLayout();
            this.tabControl1.ResumeLayout(false);
            this.Soil.ResumeLayout(false);
            this.tabControl2.ResumeLayout(false);
            this.Rock_weathering.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox6)).EndInit();
            this.groupBox10.ResumeLayout(false);
            this.groupBox10.PerformLayout();
            this.physical.ResumeLayout(false);
            this.physical.PerformLayout();
            this.chemical.ResumeLayout(false);
            this.chemical.PerformLayout();
            this.clay.ResumeLayout(false);
            this.clay.PerformLayout();
            this.bioturbation.ResumeLayout(false);
            this.bioturbation.PerformLayout();
            this.carbon.ResumeLayout(false);
            this.carbon.PerformLayout();
            this.Geochron.ResumeLayout(false);
            this.tabControl3.ResumeLayout(false);
            this.tabPage4.ResumeLayout(false);
            this.tabPage4.PerformLayout();
            this.tabPage5.ResumeLayout(false);
            this.tabPage5.PerformLayout();
            this.Experimental.ResumeLayout(false);
            this.tabControl4.ResumeLayout(false);
            this.blocks.ResumeLayout(false);
            this.blocks.PerformLayout();
            this.Hydro.ResumeLayout(false);
            this.Hydro.PerformLayout();
            this.treefall.ResumeLayout(false);
            this.treefall.PerformLayout();
            this.Options.ResumeLayout(false);
            this.Options.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.ResumeLayout(false);

        }
        #endregion

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.Run(new Mother_form());
        }   // creates the forms

        LORICA4.Output_timeseries timeseries = new LORICA4.Output_timeseries();
        LORICA4.Landuse_determinator landuse_determinator = new LORICA4.Landuse_determinator();
        public LORICA4.Soil_specifier soildata = new LORICA4.Soil_specifier();
        int clearmatrices_test()
        {
            // Reseting values of existing memory instead of Re-allocating (saves memory)
            // Re-allocating, while faster, still keeps the previous memory until Garbage Collection (GC) decides to clear it up

            //Another potentially faster way is to for-loop through each array position and reset to its default value.
            //While it does require so extra coding logic it would be good for larger arrays and can implement Parallel.For()

            if (this.Spitsbergen_case_study.Checked == true) { Array.Clear(original_dtm, 0, original_dtm.Length); }
            Array.Clear(dtm, 0, dtm.Length);
            if (merely_calculating_derivatives == false)
            {
                //Array.Clear(OSL_age, 0, OSL_age.Length);
                Array.Clear(soildepth_m, 0, soildepth_m.Length);
                Array.Clear(dtmchange_m, 0, dtmchange_m.Length);
                Array.Clear(dz_soil, 0, dz_soil.Length);
                // climate grids
                if (check_space_evap.Checked == true) { Array.Clear(evapotranspiration, 0, evapotranspiration.Length); }
                if (check_space_infil.Checked == true) { Array.Clear(infil, 0, infil.Length); }
                if (check_space_rain.Checked == true) { Array.Clear(rain_m, 0, rain_m.Length); }

                Array.Clear(veg, 0, veg.Length);
                //Array.Clear(veg_correction_factor, 0, veg.Length);
                // categorical grids
                if (check_space_landuse.Checked == true) { Array.Clear(landuse, 0, landuse.Length); }
                if (check_space_landuse.Checked == true) { Array.Clear(evapotranspiration, 0, evapotranspiration.Length); } 
                if (check_space_landuse.Checked == true) { Array.Clear(infil, 0, infil.Length); }
                if (check_space_landuse.Checked == true) { Array.Clear(root_cohesion_kPa_new, 0, root_cohesion_kPa_new.Length); }
                if (check_space_landuse.Checked == true) { Array.Clear(OM_LU, 0, OM_LU.Length); }
                if (check_space_landuse.Checked == true) { Array.Clear(Till_LU, 0, Till_LU.Length); }
                if (check_space_landuse.Checked == true) { Array.Clear(BiotRate_LU, 0, BiotRate_LU.Length); }
                if (check_space_landuse.Checked == true) { Array.Clear(K_fac, 0, K_fac.Length); }
                if (check_space_landuse.Checked == true) { Array.Clear(P_fac, 0, P_fac.Length); }
            }
            Array.Clear(status_map, 0, status_map.Length);
            //sorting arrays
            Array.Clear(index, 0, index.Length);
            Array.Clear(row_index, 0, row_index.Length);
            Array.Clear(col_index, 0, col_index.Length);
            Array.Clear(rowcol_index, 0, rowcol_index.Length);
            //others
            Array.Clear(depression, 0, depression.Length);
            Array.Clear(dtmfill_A, 0, dtmfill_A.Length);
            if (merely_calculating_derivatives == false)
            {
                if (1 == 1)
                {
                    Array.Clear(texture_kg, 0, texture_kg.Length);   //: mass in kg (per voxel = layer * thickness)
                    Array.Clear(layerthickness_m, 0, layerthickness_m.Length);      // : thickness in m 
                    Array.Clear(young_SOM_kg, 0, young_SOM_kg.Length);   // : OM mass in kg (per voxel = layer * thickness)
                    Array.Clear(old_SOM_kg, 0, old_SOM_kg.Length);
                    Array.Clear(bulkdensity, 0, bulkdensity.Length);           // : bulkdensity in kg/m3 (over the voxel = layer * thickness)
                }
                if (CN_checkbox.Checked)
                {
                    CN_atoms_cm2 = new double[nr, nc, max_soil_layers, n_cosmo];
                    //for (row = 0; row < nr; row++)
                    //{
                    //    for (col = 0; col < nc; col++)
                    //    {
                    //        for (int lay = 0; lay < max_soil_layers; lay++)
                    //        {
                    //            CN_atoms_cm2[row, col, lay, 0] = Convert.ToInt64(8E8);
                    //        }
                    //    }
                    //}
                    // dim[,,,0] = Meteoric 10-Be (dynamics linked to both clay fractions)
                    // dim[,,,1] = In-situ 10-Be (dynamics linked to sand fraction)
                    // dim[,,,2] = In-situ 14-C (dynamics linked to sand fraction)
                    // dim[,,,3] = 137-Cs
                    // Other nuclides can be included at a later stage (e.g., 14-C, 137-Cs, 210-Pb)
                }
                if (OSL_checkbox.Checked)
                {
                    int ngrains = System.Convert.ToInt32(ngrains_textbox.Text);
                    int start_age = 1000000;
                    // OSL_age = new int[nr * nc * max_soil_layers * ngrains, 5];
                    OSL_grainages = new int[nr, nc, max_soil_layers][];
                    OSL_depositionages = new int[nr, nc, max_soil_layers][];
                    OSL_surfacedcount = new int[nr, nc, max_soil_layers][];

                    int count = 0;
                    for (int row = 0; row < nr; row++)
                    {
                        for (int col = 0; col < nc; col++)
                        {
                            for (int lay = 0; lay < max_soil_layers; lay++)
                            {
                                OSL_grainages[row, col, lay] = new int[ngrains];
                                OSL_depositionages[row, col, lay] = new int[ngrains];
                                OSL_surfacedcount[row, col, lay] = new int[ngrains];
                            }
                        }
                    }
                }
                if (Water_ero_checkbox.Checked)
                {
                    //doubles
                    Array.Clear(waterflow_m3, 0, waterflow_m3.Length);
                    if (only_waterflow_checkbox.Checked == false)
                    {
                        Array.Clear(K_fac, 0, K_fac.Length);
                        Array.Clear(P_fac, 0, P_fac.Length);
                        Array.Clear(sediment_in_transport_kg, 0, sediment_in_transport_kg.Length);
                        Array.Clear(young_SOM_in_transport_kg, 0, young_SOM_in_transport_kg.Length);
                        Array.Clear(old_SOM_in_transport_kg, 0, old_SOM_in_transport_kg.Length);
                        Array.Clear(sum_water_erosion, 0, sum_water_erosion.Length);
                        Array.Clear(dz_ero_m, 0, dz_ero_m.Length);
                        Array.Clear(dz_sed_m, 0, dz_sed_m.Length);
                        Array.Clear(lake_sed_m, 0, lake_sed_m.Length);
                        Array.Clear(depressionsum_texture_kg, 0, depressionsum_texture_kg.Length);
                        if (CN_checkbox.Checked) { CN_in_transport = new double[nr, nc, n_cosmo]; }
                        if (OSL_checkbox.Checked)
                        {
                            OSL_grainages_in_transport = new int[nr, nc][];
                            OSL_depositionages_in_transport = new int[nr, nc][];
                            OSL_surfacedcount_in_transport = new int[nr, nc][];
                        }
                    }
                }

                if (Tillage_checkbox.Checked)
                {
                    Array.Clear(till_result, 0, till_result.Length);
                    Array.Clear(sum_tillage, 0, sum_tillage.Length);
                    Array.Clear(tillfields, 0, tillfields.Length);
                    Array.Clear(dz_till_bd, 0, dz_till_bd.Length);
                }

                if (treefall_checkbox.Checked)
                {
                    Array.Clear(treefall_count, 0, treefall_count.Length);
                    Array.Clear(dz_treefall, 0, dz_treefall.Length);
                }

                if (version_lux_checkbox.Checked)
                {
                    Array.Clear(tpi, 0, tpi.Length);
                    Array.Clear(hornbeam_cover_fraction, 0, hornbeam_cover_fraction.Length);
                    Array.Clear(litter_kg, 0, litter_kg.Length);
                }

                if (creep_active_checkbox.Checked)
                {
                    Array.Clear(creep, 0, creep.Length);
                    Array.Clear(sum_creep_grid, 0, sum_creep_grid.Length);
                }
                if (Landslide_checkbox.Checked)
                {
                    //doubles
                    Array.Clear(stslope_radians, 0, stslope_radians.Length);
                    Array.Clear(crrain_m_d, 0, crrain_m_d.Length);
                    Array.Clear(camf, 0, camf.Length);
                    Array.Clear(T_fac, 0, T_fac.Length);
                    Array.Clear(Cohesion_factor, 0, Cohesion_factor.Length);
                    Array.Clear(Cs_fac, 0, Cs_fac.Length);
                    Array.Clear(sat_bd_kg_m3, 0, sat_bd_kg_m3.Length);
                    Array.Clear(peak_friction_angle_radians, 0, peak_friction_angle_radians.Length);
                    Array.Clear(resid_friction_angle_radians, 0, resid_friction_angle_radians.Length);
                    Array.Clear(reserv, 0, reserv.Length);
                    Array.Clear(ero_slid_m, 0, ero_slid_m.Length);
                    Array.Clear(cel_dist, 0, cel_dist.Length);
                    Array.Clear(sed_slid_m, 0, sed_slid_m.Length);
                    Array.Clear(sed_bud_m, 0, sed_bud_m.Length);
                    Array.Clear(dh_slid, 0, dh_slid.Length);
                    Array.Clear(sum_landsliding, 0, sum_landsliding.Length);
                    Array.Clear(landslidesum_texture_kg, 0, landslidesum_texture_kg.Length);
                    Array.Clear(landslidesum_thickness_m, 0, landslidesum_thickness_m.Length);
                    Array.Clear(landslidesum_OM_kg, 0, landslidesum_OM_kg.Length);

                    //integers
                    Array.Clear(slidenr, 0, slidenr.Length);
                }
                if (Biological_weathering_checkbox.Checked)
                {
                    Array.Clear(bedrock_weathering_m, 0, bedrock_weathering_m.Length);
                    Array.Clear(sum_biological_weathering, 0, sum_biological_weathering.Length);
                }
                if (Frost_weathering_checkbox.Checked)
                {
                    Array.Clear(frost_weathering, 0, frost_weathering.Length);
                    Array.Clear(sum_frost_weathering, 0, sum_frost_weathering.Length);
                }
                if (tilting_active_checkbox.Checked)
                {
                    Array.Clear(sum_tilting, 0, sum_tilting.Length);
                }
                if (uplift_active_checkbox.Checked)
                {
                    Array.Clear(sum_uplift, 0, sum_uplift.Length);
                }
                if (blocks_active == 1)
                {
                    Array.Clear(hardlayeropenness_fraction, 0, hardlayeropenness_fraction.Length);
                }

                if (this.Proglacial_checkbox.Checked) { 
                    Array.Clear(age_rast_yr, 0, age_rast_yr.Length);
                    Array.Clear(og_dtm, 0, og_dtm.Length);
                }

                if (check_space_landuse.Checked == true) 
                {
                    K_fac = new double[nr, nc]; 
                    P_fac = new double[nr, nc]; 
                    infil = new double[nr, nc]; 
                    evapotranspiration = new double[nr, nc]; 
                    OM_LU = new double[nr, nc]; 
                    Till_LU = new double[nr, nc];
                    BiotRate_LU = new double[nr, nc]; 
                    root_cohesion_kPa_new = new double[nr, nc]; 
                }
            }
            Array.Clear(aspect, 0, aspect.Length);
            Array.Clear(slopeAnalysis, 0, slopeAnalysis.Length);
            Array.Clear(hillshade, 0, hillshade.Length);
            Array.Clear(Tau, 0, Tau.Length);
            // Debug.WriteLine("memory assigned succesfully");
            return 1;
        }
        int makematrices()
        {
            // Debug.WriteLine("assigning memory");
            // status grids
            if (this.Spitsbergen_case_study.Checked == true) { original_dtm = new double[nr, nc]; }
            dtm = new double[nr, nc];
            if (merely_calculating_derivatives == false)
            {
                soildepth_m = new double[nr, nc];
                dtmchange_m = new double[nr, nc];
                dz_soil = new double[nr, nc];
                // climate grids
                if (check_space_evap.Checked == true) { evapotranspiration = new double[nr, nc]; }
                if (check_space_infil.Checked == true) { infil = new double[nr, nc]; }
                if (check_space_rain.Checked == true) { rain_m = new double[nr, nc]; }
                veg = new double[nr, nc];
                // categorical grids
                if (check_space_landuse.Checked == true) { landuse = new int[nr, nc]; }
            }
            status_map = new int[nr, nc];
            //sorting arrays
            index = new double[nr * nc];
            row_index = new int[nr * nc];
            col_index = new int[nr * nc];
            rowcol_index = new string[nr * nc];
            //others
            depression = new int[nr, nc];
            dtmfill_A = new double[nr, nc];
            if (merely_calculating_derivatives == false)
            {
                if (1 == 1)
                {
                    texture_kg = new double[nr, nc, max_soil_layers, n_texture_classes];    //: mass in kg (per voxel = layer * thickness)
                    layerthickness_m = new double[nr, nc, max_soil_layers];        // : thickness in m 
                    young_SOM_kg = new double[nr, nc, max_soil_layers];         // : OM mass in kg (per voxel = layer * thickness)
                    old_SOM_kg = new double[nr, nc, max_soil_layers];
                    bulkdensity = new double[nr, nc, max_soil_layers];            // : bulkdensity in kg/m3 (over the voxel = layer * thickness)
                }

                if (CN_checkbox.Checked)
                {
                    CN_atoms_cm2 = new double[nr, nc, max_soil_layers, n_cosmo];
                    // dim[,,,0] = Meteoric 10-Be (dynamics linked to both clay fractions)
                    // dim[,,,1] = Meteoric 10-Be (dynamics linked to silt fraction)
                    // dim[,,,2] = In-situ 10-Be (dynamics linked to sand fraction)
                    // dim[,,,3] = In-situ 14-C (dynamics linked to sand fraction)
                    // dim[,,,4] = ...
                    // Other nuclides can be included at a later stage (e.g., 14-C, 137-Cs, 210-Pb)



                   
                }
                if (OSL_checkbox.Checked)
                {
                    OSL_grainages = new int[nr, nc, max_soil_layers][];
                    OSL_depositionages = new int[nr, nc, max_soil_layers][];
                    OSL_surfacedcount = new int[nr, nc, max_soil_layers][];
                }

                if (Water_ero_checkbox.Checked)
                {
                    //doubles
                    waterflow_m3 = new double[nr, nc];
                    if (only_waterflow_checkbox.Checked == false)
                    {
                        K_fac = new double[nr, nc];
                        P_fac = new double[nr, nc];
                        sediment_in_transport_kg = new double[nr, nc, n_texture_classes];
                        young_SOM_in_transport_kg = new double[nr, nc];
                        old_SOM_in_transport_kg = new double[nr, nc];
                        sum_water_erosion = new double[nr, nc];
                        dz_ero_m = new double[nr, nc];
                        dz_sed_m = new double[nr, nc];
                        lake_sed_m = new double[nr, nc];
                        depressionsum_texture_kg = new double[n_texture_classes];
                        if (CN_checkbox.Checked) { CN_in_transport = new double[nr, nc, n_cosmo]; }
                        if (OSL_checkbox.Checked) { OSL_grainages_in_transport = new int[nr, nc][]; OSL_depositionages_in_transport = new int[nr, nc][]; OSL_surfacedcount_in_transport = new int[nr, nc][]; }
                    }

                }
                if (Tillage_checkbox.Checked)
                {
                    till_result = new double[nr, nc];
                    sum_tillage = new double[nr, nc];
                    tillfields = new int[nr, nc];
                    dz_till_bd = new double[nr, nc];
                }

                if (treefall_checkbox.Checked)
                {
                    treefall_count = new int[nr, nc];
                    dz_treefall = new double[nr, nc];
                }

                if (version_lux_checkbox.Checked)
                {
                    tpi = new double[nr, nc];
                    hornbeam_cover_fraction = new double[nr, nc];
                    litter_kg = new double[nr, nc, 2];
                }

                if (creep_active_checkbox.Checked)
                {
                    creep = new double[nr, nc];
                    sum_creep_grid = new double[nr, nc];
                }
                if (Landslide_checkbox.Checked)
                {
                    //Mopstafa update with new variables and remove old variables
                    //doubles
                    stslope_radians = new double[nr, nc];
                    crrain_m_d = new double[nr, nc];
                    camf = new double[nr, nc];
                    T_fac = new double[nr, nc];
                    Cohesion_factor = new double[nr, nc];
                    Cs_fac = new double[nr, nc];
                    sat_bd_kg_m3 = new double[nr, nc];
                    peak_friction_angle_radians = new double[nr, nc];
                    resid_friction_angle_radians = new double[nr, nc];
                    reserv = new double[nr, nc];
                    ero_slid_m = new double[nr, nc];
                    remaining_vertical_size_m = new double[nr, nc];
                    sed_slid_m = new double[nr, nc];
                    if (sediment_in_transport_kg == null) { sediment_in_transport_kg = new double[nr, nc, n_texture_classes]; }
                    if (young_SOM_in_transport_kg == null) { young_SOM_in_transport_kg = new double[nr, nc]; }
                    if (old_SOM_in_transport_kg == null) { old_SOM_in_transport_kg = new double[nr, nc]; }
                    //shorts
                    slidestatus = new short[nr, nc];
                }
                if (Biological_weathering_checkbox.Checked)
                {
                    bedrock_weathering_m = new double[nr, nc];
                    sum_biological_weathering = new double[nr, nc];
                }
                if (Frost_weathering_checkbox.Checked)
                {
                    frost_weathering = new double[nr, nc];
                    sum_frost_weathering = new double[nr, nc];
                }
                if (tilting_active_checkbox.Checked)
                {
                    sum_tilting = new double[nr, nc];
                }
                if (uplift_active_checkbox.Checked)
                {
                    sum_uplift = new double[nr, nc];
                }
                if (blocks_active == 1)
                {
                    hardlayeropenness_fraction = new float[nr, nc];
                }
                if (check_space_landuse.Checked) //AleG
                {
                    root_cohesion_kPa_new = new double[nr, nc]; //AleG
                    infil = new double[nr, nc]; //AleG
                    evapotranspiration = new double[nr, nc]; //AleG
                    K_fac = new double[nr, nc]; //AleG
                    P_fac = new double[nr, nc]; //AleG
                }

                if (Proglacial_checkbox.Checked)
                {
                    age_rast_yr = new int[nr, nc];
                    og_dtm = new double[nr, nc];
                }
            }
            aspect = new double[nr, nc];
            slopeAnalysis = new double[nr, nc];
            hillshade = new double[nr, nc];
            Tau = new double[nr, nc];
            // Debug.WriteLine("memory assigned succesfully");
            return 1;
        }

        #region top level code
        private TaskScheduler guiThread; //Store reference of UI Thread for updating UI Text/labels later (slow, use sparingly)
        private void main_loop(object sender, System.EventArgs e)
        {
            //This allows GUI to be responsive so you can move it around
            guiThread = TaskScheduler.FromCurrentSynchronizationContext(); //have reference to main thread (UI thread)
            var options = TaskCreationOptions.LongRunning;
            var StartThread = Task.Factory.StartNew(() =>  //send simulation work on background thread
            {
                main_loop_code(); //start simulation work on background thread
            }, CancellationToken.None, options, TaskScheduler.Default);
            // this.start_button.Enabled = false;
        }
        //test commit 1
        private void main_loop_code()
        {
            //use this example for accessing the UI thread to update any GUI labels :
            Task.Factory.StartNew(() => { this.InfoStatusPanel.Text = "Entered main program"; }, CancellationToken.None, TaskCreationOptions.None, guiThread);

            stopwatch = Stopwatch.StartNew();

            if (!checkregionalformat())
            {
                MessageBox.Show("Unsupported regional format");
                input_data_error = true;
                return;
            }

            try
            {
                string dtmmotherfilename = dtm_input_filename_textbox.Text; int dtmrepeats = 1;
                string dtmfilename = dtmmotherfilename; string[] listdtmfiles = { dtm_input_filename_textbox.Text };
                if (dtm_iterate_checkbox.Checked == true)
                {
                    //    user has selected 1 dtm but wants to run all similar dtms that exist in that directory.
                    //    We will remove any numbers from that filename and count all files that match the remainder
                    Debug.WriteLine(" setting up multiple dtm runs ");
                    string[] separater = dtmmotherfilename.Split('\\');
                    string pattern = Regex.Replace(separater[separater.Length - 1], @"[\d-]", string.Empty);
                    string pattern1 = pattern.Split('.')[0];
                    string pattern2 = pattern.Split('.')[pattern.Split('.').Length - 1];
                    pattern = "*" + pattern1 + "*." + pattern2;
                    Debug.WriteLine(" selecting all dem versions that contain " + pattern);
                    Debug.WriteLine(" path is " + System.IO.Path.GetDirectoryName(dtmmotherfilename));
                    listdtmfiles = Directory.GetFiles(System.IO.Path.GetDirectoryName(dtmmotherfilename), pattern);
                    dtmrepeats = listdtmfiles.Length;
                    Debug.WriteLine(" there are " + dtmrepeats + " dem versions available. We will run LORICA for all of them");
                }
                for (int dtmrun = 0; dtmrun < dtmrepeats; dtmrun++)
                {
                    if (dtm_iterate_checkbox.Checked == true)
                    {
                        dtmfilename = listdtmfiles[dtmrun];
                    }
                    Debug.WriteLine("Entered LORICA main code with " + dtmfilename);
                    string[] separate = dtmfilename.Split('.');
                    workdir = separate[0];
                    Debug.WriteLine("storing results in " + workdir);
                    System.IO.Directory.CreateDirectory(workdir);
                    input_data_error = false;

                    try { end_time = int.Parse(Number_runs_textbox.Text); }
                    catch { input_data_error = true; MessageBox.Show("Invalid number of years"); }
                    try { max_soil_layers = int.Parse(textbox_max_soil_layers.Text); }
                    catch { input_data_error = true; MessageBox.Show("Invalid number of soil layers"); }
                    try { layer_z_surface = double.Parse(textbox_layer_thickness.Text); }
                    catch { input_data_error = true; MessageBox.Show("Invalid standard thickness of soil layers"); }
                    try { layer_z_increase = double.Parse(textbox_layer_thickness_increase.Text); }
                    catch { input_data_error = true; MessageBox.Show("Invalid factor for increasing layer thickness"); }
                    

                    try { ntr = System.Convert.ToInt32(end_time); }     // WVG initialise ntr: number of rows in timeseries matrix   
                    catch (OverflowException)
                    {
                        MessageBox.Show("number of timesteps is outside the range of the Int32 type.");
                    }
                    //WVG initialise ntr, nr of timesteps, can be changed to nr of output timesteps
                    numfile = 1;
                    if (Water_ero_checkbox.Checked)
                    {
                        water_ero_active = true;
                    }
                    if (Tillage_checkbox.Checked)
                    {
                        tillage_active = true;
                    }
                    if (blocks_active_checkbox.Checked)
                    {
                        blocks_active = 1;
                    }
                    if (Landslide_checkbox.Checked)
                    {
                        landslide_active = true;
                    }
                    if (creep_active_checkbox.Checked)
                    {
                        creep_active = true;
                    }
                    if (Biological_weathering_checkbox.Checked)
                    {
                        bedrock_weathering_active = true;
                    }
                    if (Frost_weathering_checkbox.Checked)
                    {
                        frost_weathering_active = true;
                    }
                    if (tilting_active_checkbox.Checked)
                    {
                        tilting_active = true;
                    }
                    if (uplift_active_checkbox.Checked)
                    {
                        uplift_active = true;
                    }
                    if (soil_phys_weath_checkbox.Checked)
                    {
                        soil_phys_weath_active = true;
                    }
                    if (soil_chem_weath_checkbox.Checked)
                    {
                        soil_chem_weath_active = true;
                    }
                    if (soil_bioturb_checkbox.Checked)
                    {
                        soil_bioturb_active = true;
                    }
                    if (soil_clay_transloc_checkbox.Checked)
                    {
                        soil_clay_transloc_active = true;
                    }
                    if (soil_carbon_cycle_checkbox.Checked) //:)
                    {
                        soil_carbon_active = true;
                    }

                    //INPUTS
                    //GENERAL INPUTS
                    //Entry point for consecutive runs for sensitivity analyses or calibration 
                    maxruns = 1;
                    int currentlevel = 0;



                    if (Calibration_button.Checked == true)
                    {
                        int runs_per_level = 0;
                        //CALIB_USER INPUT NEEDED NEXT LINE IN THE CODE :
                        try
                        {
                            user_specified_number_of_calibration_parameters = Convert.ToInt32(num_cal_paras_textbox.Text);
                        }
                        catch
                        {
                            Debug.WriteLine(" problem setting number of parameters for calibration ");
                        }
                        best_error = 99999999999; //or any other absurdly high number
                        best_parameters = new double[user_specified_number_of_calibration_parameters];
                        user_specified_number_of_ratios = calibration_ratios_textbox.Text.Split(';').Length;
                        runs_per_level = Convert.ToInt32(Math.Pow(user_specified_number_of_ratios, user_specified_number_of_calibration_parameters));
                        calib_ratios = new double[user_specified_number_of_calibration_parameters, user_specified_number_of_ratios];
                        original_ratios = new double[user_specified_number_of_ratios];
                        for (int rat = 0; rat < user_specified_number_of_ratios; rat++)
                        {
                            try
                            {
                                original_ratios[rat] = Convert.ToDouble(calibration_ratios_textbox.Text.Split(';')[rat]);
                                for (int par = 0; par < user_specified_number_of_calibration_parameters; par++)
                                {
                                    calib_ratios[par, rat] = Convert.ToDouble(calibration_ratios_textbox.Text.Split(';')[rat]);
                                }
                            }
                            catch { Debug.WriteLine(" problem setting original parameter ratios for calibration "); }
                        }
                        calib_calculate_maxruns(user_specified_number_of_calibration_parameters);
                        Debug.WriteLine(" starting " + maxruns + " calibration runs");
                        calib_prepare_report();
                    }

                    for (run_number = 0; run_number < maxruns; run_number++) //Maxruns Loop()
                    {



                        //WATER EROSION AND DEPOSITION PARAMETERS
                        if (water_ero_active)
                        {
                            try { m = double.Parse(parameter_m_textbox.Text); }
                            catch { input_data_error = true; MessageBox.Show("value for parameter m is not valid"); }                      // Kirkby's m and n factors for increasing
                            try { n = double.Parse(parameter_n_textbox.Text); }
                            catch { input_data_error = true; MessageBox.Show("value for parameter n is not valid"); }                   // sheet, wash, overland, gully to river flow
                            try { conv_fac = double.Parse(parameter_conv_textbox.Text); }
                            catch { input_data_error = true; MessageBox.Show("value for parameter p is not valid"); }
                            try { advection_erodibility = double.Parse(parameter_K_textbox.Text); }
                            catch { input_data_error = true; MessageBox.Show("value for parameter K is not valid"); }
                            try { bio_protection_constant = double.Parse(bio_protection_constant_textbox.Text); }
                            catch { input_data_error = true; MessageBox.Show("value for parameter P is not valid"); }
                            try { rock_protection_constant = double.Parse(rock_protection_constant_textbox.Text); }
                            catch { input_data_error = true; MessageBox.Show("value for parameter P is not valid"); }
                            try { constant_selective_transcap = double.Parse(selectivity_constant_textbox.Text); }
                            catch { input_data_error = true; MessageBox.Show("value for parameter P is not valid"); }
                            try { erosion_threshold_kg = double.Parse(erosion_threshold_textbox.Text); }
                            catch { input_data_error = true; MessageBox.Show("value for parameter P is not valid"); }
                        }

                        //TILLAGE PARAMETERS
                        if (tillage_active)
                        {
                            try { plough_depth = double.Parse(parameter_ploughing_depth_textbox.Text); }
                            catch { input_data_error = true; MessageBox.Show("value for parameter plough depth is not valid"); }
                            try { tilc = double.Parse(parameter_tillage_constant_textbox.Text); }
                            catch { input_data_error = true; MessageBox.Show("value for parameter tillage constant is not valid"); }
                        }

                        //BLOCK PARAMETERS
                        if (blocks_active == 1)
                        {
                            try { hardlayerelevation_m = Int32.Parse(hardlayerelevation_textbox.Text); }
                            catch { input_data_error = true; MessageBox.Show("value for parameter block size threshold is not valid"); }
                            try { hardlayerthickness_m = Int32.Parse(hardlayerthickness_textbox.Text); }
                            catch { input_data_error = true; MessageBox.Show("value for parameter block weathering fraction is not valid"); }
                            try { hardlayer_weath_contrast = Double.Parse(hardlayerweath_textbox.Text); }
                            catch { input_data_error = true; MessageBox.Show("value for parameter block size threshold is not valid"); }
                            try { hardlayerdensity_kg_m3 = Int32.Parse(hardlayerdensity_textbox.Text); }
                            catch { input_data_error = true; MessageBox.Show("value for parameter block weathering fraction is not valid"); }

                            try { blocksizethreshold_m = Single.Parse(blocksize_textbox.Text); }
                            catch { input_data_error = true; MessageBox.Show("value for parameter block size threshold is not valid"); }
                            try { blockweatheringratio = Single.Parse(blockweath_textbox.Text); }
                            catch { input_data_error = true; MessageBox.Show("value for parameter block weathering fraction is not valid"); }
                        }

                        //CREEP PARAMETER
                        if (creep_active)
                        {
                            try { potential_creep_kg_m2_y = double.Parse(parameter_diffusivity_textbox.Text); }
                            catch { input_data_error = true; MessageBox.Show("value for parameter potential_creep_kg_m2_y is not valid"); }
                            try { bioturbation_decay_depth_m = Convert.ToDouble(bt_depth_decay_textbox.Text); }
                            catch { input_data_error = true; MessageBox.Show("value for creep depth dependence (from BIOTURBATION) is not valid"); }
                        }

                        //LANDSLIDE PARAMETERS
                        if (landslide_active)
                        {
                            conv_fac = 4;        // multiple flow conversion factor
                            specific_area[0] = Convert.ToDouble(soildata.specific_area_coarse_textbox.Text);
                            specific_area[1] = Convert.ToDouble(soildata.specific_area_sand_textbox.Text);
                            specific_area[2] = Convert.ToDouble(soildata.specific_area_silt_textbox.Text);
                            specific_area[3] = Convert.ToDouble(soildata.specific_area_clay_textbox.Text);
                            specific_area[4] = Convert.ToDouble(soildata.specific_area_fine_clay_textbox.Text);
                        }

                        //Bio Weathering PARAMETERS
                        if (bedrock_weathering_active)
                        {
                            try { P0 = double.Parse(parameter_P0_textbox.Text); }
                            catch { input_data_error = true; MessageBox.Show("value for parameter P0 is not valid"); }
                            try { k1 = double.Parse(parameter_k1_textbox.Text); }
                            catch { input_data_error = true; MessageBox.Show("value for parameter k1 is not valid"); }
                            try { k2 = double.Parse(parameter_k2_textbox.Text); }
                            catch { input_data_error = true; MessageBox.Show("value for parameter k2 is not valid"); }
                            try { Pa = double.Parse(parameter_Pa_textbox.Text); }
                            catch { input_data_error = true; MessageBox.Show("value for parameter Pa is not valid"); }
                        }

                        //Tilting parameters
                        if (tilting_active)
                        {
                            if (radio_tilt_col_zero.Checked) { tilt_location = 0; }
                            if (radio_tilt_row_zero.Checked) { tilt_location = 1; }
                            if (radio_tilt_col_max.Checked) { tilt_location = 2; }
                            if (radio_tilt_row_max.Checked) { tilt_location = 3; }
                            try { tilt_intensity = double.Parse(Tilting_rate_textbox.Text); }
                            catch { input_data_error = true; MessageBox.Show("value for parameter tilting rate is not valid"); }
                        }

                        //Uplift parameters
                        if (uplift_active)
                        {
                            if (radio_lift_row_less_than.Checked) { lift_type = 0; }
                            if (radio_lift_row_more_than.Checked) { lift_type = 1; }
                            if (radio_lift_col_less_than.Checked) { lift_type = 2; }
                            if (radio_lift_row_more_than.Checked) { lift_type = 3; }
                            if (lift_type == 0)
                            {
                                try { lift_location = int.Parse(text_lift_row_less.Text); }
                                catch { input_data_error = true; MessageBox.Show("value for parameter tilting rate is not valid"); }
                            }
                            if (lift_type == 1)
                            {
                                try { lift_location = int.Parse(text_lift_row_more.Text); }
                                catch { input_data_error = true; MessageBox.Show("value for parameter tilting rate is not valid"); }
                            }
                            if (lift_type == 2)
                            {
                                try { lift_location = int.Parse(text_lift_col_less.Text); }
                                catch { input_data_error = true; MessageBox.Show("value for parameter tilting rate is not valid"); }
                            }
                            if (lift_type == 3)
                            {
                                try { lift_location = int.Parse(text_lift_col_more.Text); }
                                catch { input_data_error = true; MessageBox.Show("value for parameter tilting rate is not valid"); }
                            }
                            try { lift_intensity = double.Parse(Uplift_rate_textbox.Text); }
                            catch { input_data_error = true; MessageBox.Show("value for parameter tilting rate is not valid"); }
                        }

                        // TREE FALL PARAMETERS
                        if (treefall_checkbox.Checked)
                        {
                            W_m_max = System.Convert.ToDouble(tf_W.Text);
                            D_m_max = System.Convert.ToDouble(tf_D.Text);
                            growth_a_max = System.Convert.ToInt32(tf_growth.Text);
                            age_a_max = System.Convert.ToInt32(tf_age.Text);
                            tf_frequency = System.Convert.ToDouble(tf_freq.Text);
                        }

                        //SOIL PHYSICAL WEATHERING PARAMETERS
                        if (soil_phys_weath_active)
                        {
                            try
                            {
                                physical_weathering_constant = Convert.ToDouble(Physical_weath_C1_textbox.Text);
                                phys_weath_decay_depth_m = Convert.ToDouble(physical_weath_constant1.Text);
                                Ctwo = Convert.ToDouble(physical_weath_constant2.Text);
                                //the upper sizes of particle for the different fractions are declared in initialise_soil because they are always needed
                                // Debug.WriteLine("succesfully read parameters for pysical weathering");
                            }
                            catch
                            {
                                input_data_error = true; Debug.WriteLine("problem reading parameters for pysical weathering");
                            }
                        }

                        //SOIL CHEMICAL WEATHERING PARAMETERS
                        if (soil_chem_weath_active)
                        {
                            try
                            {
                                chemical_weathering_constant = Convert.ToDouble(chem_weath_rate_constant_textbox.Text);
                                chem_weath_decay_depth_m = Convert.ToDouble(chem_weath_depth_constant_textbox.Text);
                                Cfour = Convert.ToDouble(chem_weath_specific_coefficient_textbox.Text);
                                specific_area[0] = Convert.ToDouble(soildata.specific_area_coarse_textbox.Text);
                                specific_area[1] = Convert.ToDouble(soildata.specific_area_sand_textbox.Text);
                                specific_area[2] = Convert.ToDouble(soildata.specific_area_silt_textbox.Text);
                                specific_area[3] = Convert.ToDouble(soildata.specific_area_clay_textbox.Text);
                                specific_area[4] = Convert.ToDouble(soildata.specific_area_fine_clay_textbox.Text);
                                neoform_constant = Convert.ToDouble(clay_neoform_constant_textbox.Text);
                                Cfive = Convert.ToDouble(clay_neoform_C1_textbox.Text);
                                Csix = Convert.ToDouble(clay_neoform_C2_textbox.Text);
                                // Debug.WriteLine("succesfully read parameters for chemical weathering");
                            }
                            catch
                            {
                                input_data_error = true; Debug.WriteLine("problem reading parameters for chemical weathering");
                            }
                        }

                        //SOIL CLAY DYNAMICS PARAMETERS
                        if (soil_clay_transloc_active)
                        {
                            try
                            {
                                max_eluviation = Convert.ToDouble(maximum_eluviation_textbox.Text);
                                Cclay = Convert.ToDouble(eluviation_coefficient_textbox.Text);
                                // Debug.WriteLine("succesfully read parameters for  clay dynamics");
                            }
                            catch
                            {
                                input_data_error = true; Debug.WriteLine("problem reading parameters for clay dynamics");
                            }
                            if (CT_depth_decay_checkbox.Checked)
                            {
                                try
                                {
                                    ct_depthdec = Convert.ToDouble(ct_depth_decay.Text);
                                }
                                catch
                                {
                                    input_data_error = true; Debug.WriteLine("problem reading depth decay parameter for clay dynamics");
                                }
                            }
                        }

                        //BIOTURBATION PARAMETERS
                        if (soil_bioturb_active)
                        {
                            try
                            {
                                potential_bt_mixing_kg_m2_y = Convert.ToDouble(potential_bt_mixing_textbox.Text); // MvdM changed name to match parameter in BT process
                                potential_bt_mounding_kg_m2_y = Convert.ToDouble(potential_bt_mounding_textbox.Text);
                                bioturbation_decay_depth_m = Convert.ToDouble(bt_depth_decay_textbox.Text);
                                this.Invoke(new MethodInvoker(delegate () { bt_depth_function = bt_depthfunction_box.SelectedIndex; }));

                            }
                            catch
                            {
                                input_data_error = true; Debug.WriteLine("problem reading parameters for bioturbation");
                            }
                        }

                        //CARBON CYCLE PARAMETERS
                        if (soil_carbon_active)
                        {
                            try
                            {
                                potential_OM_input = Convert.ToDouble(carbon_input_textbox.Text);

                                this.Invoke(new MethodInvoker(delegate () { som_cycle_algorithm = som_cycle_algorithm_box.SelectedIndex; }));
                                OM_input_decay_depth_m = Convert.ToDouble(carbon_depth_decay_textbox.Text);
                                humification_fraction = Convert.ToDouble(carbon_humification_fraction_textbox.Text);
                                potential_young_decomp_rate = Convert.ToDouble(carbon_y_decomp_rate_textbox.Text);
                                potential_old_decomp_rate = Convert.ToDouble(carbon_o_decomp_rate_textbox.Text);
                                young_OM_decomp_char_decay_depth_m = Convert.ToDouble(carbon_y_depth_decay_textbox.Text);
                                old_OM_decomp_char_decay_depth_m = Convert.ToDouble(carbon_o_depth_decay_textbox.Text);
                            }
                            catch
                            {
                                input_data_error = true; Debug.WriteLine("problem reading parameters for carbon cycle");
                            }
                        }

                        // COSMOGENIC NUCLIDE PARAMETERS
                        if(CN_checkbox.Checked)
                        {
                            try
                            {
                                met_10Be_input = System.Convert.ToDouble(metBe10_input_textbox.Text);// atoms/cm2/y. MvdM should be determined as function of latitude. 
                                met_10Be_adsorptioncoefficient = Convert.ToDouble(met10Be_dd.Text); // adsorption coefficient of met 10 beadsorption, equivalent to a depth decay parameter
                                met_10Be_clayfraction = Convert.ToDouble(met_10Be_clayfrac.Text);
                                P0_10Be_is_sp = System.Convert.ToDouble(isBe10_sp_input_textbox.Text); // Spallation. atoms / g quartz /y. 4.76; http://dx.doi.org/10.1029/2010GC003084
                                P0_10Be_is_mu = System.Convert.ToDouble(isBe10_mu_input_textbox.Text); // Muon. atoms / g quartz /y
                                P0_14C_is_sp = System.Convert.ToDouble(isC14_sp_input_textbox.Text);// Spallation. atoms / g quartz /y. 15.7; https://doi.org/10.1016/S0016-7037(01)00566-X
                                P0_14C_is_mu = System.Convert.ToDouble(isC14_mu_input_textbox.Text);// Muon. atoms / g quartz /y
                                attenuation_length_sp = System.Convert.ToDouble(attenuationlength_sp_textbox.Text);// kg m-2
                                attenuation_length_mu = System.Convert.ToDouble(attenuationlength_mu_textbox.Text);// kg m-2
                                // Decay parameters
                                decay_Be10 = System.Convert.ToDouble(Be10_decay_textbox.Text);
                                decay_C14 = System.Convert.ToDouble(C14_decay_textbox.Text);
                                // Inherited concentrations
                                met10Be_inherited = System.Convert.ToDouble(metBe10_inherited_textbox.Text);
                                is10Be_inherited = System.Convert.ToDouble(isBe10_inherited_textbox.Text);
                                isC14_inherited = System.Convert.ToDouble(isC14_inherited_textbox.Text);
                            }
                            catch
                            {
                                input_data_error = true; Debug.WriteLine("problem reading parameters for cosmogenic nuclides");

                            }
                        }

                        // LUMINESCENCE PARAMETERS
                        if(OSL_checkbox.Checked)
                        {
                            try
                            {
                                ngrains_kgsand_m2 = Convert.ToInt32(ngrains_textbox.Text);
                                bleaching_depth_m = Convert.ToDouble(bleachingdepth_textbox.Text);
                                start_age = Convert.ToInt32(OSL_inherited_textbox.Text);
                            }
                            catch
                            {
                                input_data_error = true; Debug.WriteLine("problem reading parameters for luminescence tracing");

                            }
                        }

                        if (input_data_error == false)
                        {
                            try
                            {
                                //Debug.WriteLine("reading general values");
                                if (check_space_soildepth.Checked != true)
                                {
                                    try { soildepth_value = double.Parse(soildepth_constant_value_box.Text); }
                                    catch { MessageBox.Show("value for parameter soildepth is not valid"); }
                                }
                                if (check_space_landuse.Checked != true && check_time_landuse.Checked != true)
                                {
                                    try { landuse_value = int.Parse(landuse_constant_value_box.Text); }
                                    catch { MessageBox.Show("value for parameter landuse is not valid"); }
                                }
                                if (check_space_evap.Checked != true && check_time_evap.Checked != true)
                                {
                                    try { evap_value_m = double.Parse(evap_constant_value_box.Text); }
                                    catch { MessageBox.Show("value for parameter evapotranspiration is not valid"); }
                                }
                                if (check_space_infil.Checked != true && check_time_infil.Checked != true)
                                {

                                    try { infil_value_m = double.Parse(infil_constant_value_box.Text); }
                                    catch { MessageBox.Show("value for parameter infiltration is not valid"); }
                                }
                                if (check_space_rain.Checked != true && check_time_rain.Checked != true)
                                {

                                    try { rain_value_m = double.Parse(rainfall_constant_value_box.Text); }

                                    catch { MessageBox.Show("value for parameter rainfall is not valid"); }
                                }
                            }
                            catch { MessageBox.Show("there was a problem reading input values"); input_data_error = true; }


                            try
                            {
                                filename = dtmfilename;             //for directory input
                                dtm_file(filename);                 // from dtm_file(), almost all memory for the model is claimed

                            }
                            catch { Debug.WriteLine(" failed to initialise dtm matrices"); }
                            try { initialise_once(); } // reading input files
                            catch { MessageBox.Show("there was a problem reading input files "); input_data_error = true; }

                            Task.Factory.StartNew(() =>
                            {
                                this.ScenarioStatusPanel.Text = "scen " + (run_number + 1) + "/" + maxruns;
                            }, CancellationToken.None, TaskCreationOptions.None, guiThread);

                            if (input_data_error == false)
                            {

                                try { dtm_file_test(dtmfilename); }              // from dtm_file(), almost all memory for the model is claimed

                                catch { Debug.WriteLine(" failed to reset dtm "); }


                                try { initialize_once_testing(); }  // Reset Memory values instead of Allocating new memory
                                catch { MessageBox.Show("there was a problem reading input files and resetting values "); input_data_error = true; }



                                //CALIB_USER: multiply parameter values with current ratio
                                //Note the correspondence between the formulas. Change only 1 value for additional parameters!



                                if (Calibration_button.Checked == true)
                                {
                                    if (version_lux_checkbox.Checked)
                                    {
                                        int rat_number = Convert.ToInt32(Math.Floor(run_number / Math.Pow(user_specified_number_of_ratios, 0)) % user_specified_number_of_ratios);
                                        advection_erodibility *= calib_ratios[0, rat_number];
                                        Debug.WriteLine("First ratio number: " + rat_number + " adv_ero now " + advection_erodibility + " ratio " + calib_ratios[0, rat_number]);
                                    }
                                    if (version_Konza_checkbox.Checked)
                                    {
                                        int rat_number = Convert.ToInt32(Math.Floor(run_number / Math.Pow(user_specified_number_of_ratios, 0)) % user_specified_number_of_ratios);
                                        advection_erodibility *= calib_ratios[0, rat_number];
                                        rat_number = Convert.ToInt32(Math.Floor(run_number / Math.Pow(user_specified_number_of_ratios, 1)) % user_specified_number_of_ratios);
                                        potential_creep_kg_m2_y *= calib_ratios[1, rat_number];
                                        rat_number = Convert.ToInt32(Math.Floor(run_number / Math.Pow(user_specified_number_of_ratios, 2)) % user_specified_number_of_ratios);
                                        P0 *= calib_ratios[2, rat_number];
                                        rat_number = Convert.ToInt32(Math.Floor(run_number / Math.Pow(user_specified_number_of_ratios, 3)) % user_specified_number_of_ratios);
                                        k1 *= calib_ratios[3, rat_number];
                                        rat_number = Convert.ToInt32(Math.Floor(run_number / Math.Pow(user_specified_number_of_ratios, 4)) % user_specified_number_of_ratios);
                                        k2 *= calib_ratios[4, rat_number];
                                    }
                                }

                                timeseries_matrix = new double[System.Convert.ToInt32(end_time), number_of_outputs];
                                Debug.WriteLine("Created timeseries matrix with " + System.Convert.ToInt32(end_time) + " rows and " + number_of_outputs + " columns");
                                if (input_data_error == false)
                                {
                                    if (input_data_error == false)
                                    {
                                        int count_intervene = 0;
                                        if (checkbox_t_intervene.Checked)
                                        {
                                            t_intervene = int.Parse(textbox_t_intervene.Text);
                                        }

                                        //Proglacial_Error();
                                        if (Proglacial_checkbox.Checked)
                                        {
                                            if (t == 0)
                                            {
                                                age_rast_min = min_value_int(age_rast_yr, nr, nc);
                                                age_rast_max = max_value_int(age_rast_yr, nr, nc);
                                                age_rast_value = age_rast_min - 1;

                                                if (checkbox_t_intervene.Checked)
                                                {
                                                    age_rast_min = age_rast_min + (int)t_intervene;
                                                }


                                                for (row = 0; row < nr; row++)
                                                {
                                                    for (col = 0; col < nc; col++)
                                                    {
                                                        og_dtm[row, col] = dtm[row, col];

                                                        if (og_dtm[row, col] != -9999)

                                                        {
                                                            if (age_rast_yr[row, col] != -9999)

                                                            {
                                                                if (age_rast_yr[row, col] >= age_rast_value)
                                                                {
                                                                    dtm[row, col] = -9999;
                                                                }

                                                                else
                                                                {

                                                                    dtm[row, col] = dtm[row, col];
                                                                }
                                                            }
                                                            else
                                                            {
                                                                dtm[row, col] = dtm[row, col];
                                                            }

                                                        }

                                                    }

                                                }

                                            }

                                        }

                                        if (t_intervene > 0) { read_soil_elevation_distance_from_output(t_intervene, workdir); }

                                        for (t = t_intervene; t < end_time; t++)
                                        {
                                            //Proglacial mode 
                                            if (Proglacial_checkbox.Checked)
                                            {

                                                if (t <= (age_rast_max - age_rast_min) + 1)

                                                {
                                                    age_rast_value = age_rast_value + 1;


                                                    for (row = 0; row < nr; row++)
                                                    {
                                                        for (col = 0; col < nc; col++)
                                                        {

                                                            if (age_rast_yr[row, col] == age_rast_value)

                                                            {
                                                                new_glacier = true;

                                                                if (new_glacier == true)
                                                                {
                                                                    break;
                                                                }

                                                            }

                                                            if (new_glacier == true)
                                                            {
                                                                break;
                                                            }

                                                        }

                                                        if (new_glacier == true)
                                                        {
                                                            break;
                                                        }

                                                    }

                                                    if (new_glacier == true)
                                                    {
                                                        for (row = 0; row < nr; ++row)
                                                        {
                                                            for (col = 0; col < nc; col++)
                                                            {
                                                                if (og_dtm[row, col] != -9999)

                                                                {
                                                                    if (dtm[row, col] == -9999)
                                                                    {
                                                                        dtm[row, col] = og_dtm[row, col];
                                                                    }


                                                                    if (age_rast_yr[row, col] != -9999)

                                                                    {
                                                                        if (age_rast_yr[row, col] >= age_rast_value)
                                                                        {
                                                                            dtm[row, col] = -9999;
                                                                        }

                                                                        else
                                                                        {

                                                                            dtm[row, col] = dtm[row, col];
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        dtm[row, col] = dtm[row, col];
                                                                    }


                                                                }
                                                                else
                                                                {
                                                                    dtm[row, col] = og_dtm[row, col];
                                                                }
                                                            }
                                                        }
                                                        new_glacier = false;

                                                    }

                                                }

                                            }


                                            try
                                            {
                                                every_timestep();
                                            }
                                            catch
                                            {
                                                Debug.WriteLine("failed to run in timestep " + t);
                                                MessageBox.Show("failed to run in timestep + " + t);
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                            if (input_data_error == true)
                            {
                                MessageBox.Show("input data error - program can not yet run");
                                tabControl1.Visible = true;
                            }

                            if (Calibration_button.Checked == true)
                            {
                                //calculate how good this run was:                          
                                double current_error = -1;
                                if (version_lux_checkbox.Checked)
                                {
                                    current_error = calib_objective_function_Lux();
                                }
                                if (version_Konza_checkbox.Checked)
                                {
                                    current_error = calib_objective_function_Konza();
                                }
                                if (version_CarboZALF_checkbox.Checked)
                                {
                                    current_error = calib_objective_function_CarboZALF();
                                }
                                if (version_bioturbation_Checkbox.Checked)
                                {
                                    current_error = calib_objective_function_bioturbation();
                                }
                                if (current_error == -1)
                                {
                                    Debug.WriteLine(" no error calculated during calibration ");
                                }

                                //store that information along with the parameter values used to achieve it:
                                calib_update_report(current_error);
                                if (current_error < best_error) { best_error = current_error; calib_update_best_paras(); best_run = run_number; }
                                //and check whether one 'level' of calibration has finished. If so, we have to change parameter values
                                Debug.WriteLine("run " + run_number + " number paras " + user_specified_number_of_calibration_parameters + " number ratios " + calibration_ratios_textbox.Text.Split(';').Length);
                                if ((run_number + 1) % Convert.ToInt32(Math.Pow(calibration_ratios_textbox.Text.Split(';').Length, user_specified_number_of_calibration_parameters)) == 0)
                                {
                                    //a level of calibration has finished
                                    //If it was the last level, we are now done
                                    currentlevel++;
                                    Debug.WriteLine(" successfully finished a level of calibration runs");
                                    if (run_number == maxruns - 1)
                                    {
                                        Debug.WriteLine(" successfully finished last level of calibration runs");
                                        calib_finish_report();
                                    }
                                    else
                                    {
                                        Debug.WriteLine(" setting new ratios ");
                                        //CALIB_USER INPUT NEEDED HERE IN CODE
                                        //check whether the best run was on the edge of parameter space or inside, shift to that place and zoom out or in
                                        if (version_lux_checkbox.Checked)
                                        {
                                            calib_shift_and_zoom(0, double.Parse(calibration_ratio_reduction_parameter_textbox.Text), double.Parse(parameter_K_textbox.Text));
                                        }
                                        if (version_Konza_checkbox.Checked)
                                        {
                                            calib_shift_and_zoom(0, double.Parse(calibration_ratio_reduction_parameter_textbox.Text), double.Parse(parameter_K_textbox.Text));
                                            calib_shift_and_zoom(1, double.Parse(calibration_ratio_reduction_parameter_textbox.Text), double.Parse(parameter_diffusivity_textbox.Text));
                                            calib_shift_and_zoom(2, double.Parse(calibration_ratio_reduction_parameter_textbox.Text), double.Parse(parameter_P0_textbox.Text));
                                            calib_shift_and_zoom(3, double.Parse(calibration_ratio_reduction_parameter_textbox.Text), double.Parse(parameter_k1_textbox.Text));
                                            calib_shift_and_zoom(4, double.Parse(calibration_ratio_reduction_parameter_textbox.Text), double.Parse(parameter_k2_textbox.Text));
                                        }
                                    }
                                }
                                else
                                {
                                    //nothing. Parameter values are adapted with the corresponding ratios to continue calibration above.
                                }
                            }

                        } // exit point for consecutive runs of the same dtm
                    } // end try
                } // exit point for consecutive runs of different dtms
            }
            catch
            {
                Debug.WriteLine("Error in accessing file " + this.dtm_input_filename_textbox.Text);
            }

        }  //end main

        private void calculate_overwater_landscape_Spitsbergen()
        {
            //to account for a landscape that is isostaically rebounding from below sealevel to above sealevel. 
            //height above sealevel itself is not important, just that the landscape grows over time
            //therefore, Marijn's solution: if (elevation < threshold(t)) , then elevation = nodata
            double minimum_overwater_elevation = (10263 - t) / 218;
            for (row = 0; row < nr; row++)
            {
                for (col = 0; col < nc; col++)
                {

                    if (original_dtm[row, col] != nodata_value)
                    {

                        if (original_dtm[row, col] > minimum_overwater_elevation && dtm[row, col] == nodata_value)
                        {
                            // these cases were not over water, but now will be.
                            dtm[row, col] = original_dtm[row, col];
                            // all cases that were already overwater, will stay overwater - no changes there.
                        }
                    }
                    else
                    {
                        // nothing happens because these cells are simply not part of the study area
                    }
                }
            }
        }

        private void every_timestep()    //performs actions in every timestep
        {
            if (Proglacial_checkbox.Checked)
            {
                initialise_every();
                comb_sort();
                findsinks();
                searchdepressions();
                define_fillheight_new();
                calculate_water_ero_sed();
                soil_update_split_and_combine_layers();
            }

            // If a cell should remain at the same fixed elevation (e.g. fixed elevation boundary condition), here the cell can be selected
            if (OSL_checkbox.Checked)
            {
                update_and_bleach_OSL_ages();
            }

            if (CN_checkbox.Checked)
            {
                update_cosmogenic_nuclides();
            }

            if (daily_water.Checked)
            //if (nr > 50 & nc > 100)
            {
                //if (t == t_intervene) { dtm00 = dtm[50, 100]; }

                //// force no-change boundary op de outlet CLORPT
                //dtm[50, 100] = dtm00;

            }

            DateTime geo_start, pedo_start, hydro_start;

            //if (t == 0 | t == 1) { displaysoil(50, 0); }
            int i = 0;
            Task.Factory.StartNew(() =>
            {
                this.TimeStatusPanel.Text = "time " + (t + 1) + "/" + end_time;
            }, CancellationToken.None, TaskCreationOptions.None, guiThread);
            // Debug.WriteLine("starting calculations - TIME " + t);

            if (Spitsbergen_case_study.Checked)
            { calculate_overwater_landscape_Spitsbergen(); }

            #region hydrological processes
            hydro_start = DateTime.Now;
            if (daily_water.Checked)
            {
                water_balance();

            }
            hydro_t += DateTime.Now - hydro_start;
            #endregion

            #region Vegetation
            // Debug.WriteLine("before vegetation");
            if (daily_water.Checked)
            {
                determine_vegetation_type();
                change_vegetation_parameters();
            }
            if (version_lux_checkbox.Checked & luxlitter_checkbox.Checked)
            {
                soil_litter_cycle();
            }
            #endregion

            #region Geomorphic processes
            geo_start = DateTime.Now;

            //Debug.WriteLine("before WE");
            //displaysoil(0, 0);
            if (water_ero_active)
            {
                initialise_every();
                comb_sort();

                if (daily_water.Checked)
                {
                    calculate_water_ero_sed_daily();
                    soil_update_split_and_combine_layers();
                }
                else
                {
                    Debug.WriteLine("before annual WE2");
                    findsinks();
                    searchdepressions();
                    define_fillheight_new();
                    if (NA_anywhere_in_soil() == true) { Debug.WriteLine("NA found before erosed"); }
                    //Debug.WriteLine("before annual calc WE2");
                    calculate_water_ero_sed();
                    soil_update_split_and_combine_layers();

                    if (NA_anywhere_in_soil() == true) { Debug.WriteLine("NA found after erosed"); }
                    if (crashed) { Debug.WriteLine("crashed while calculating water erosion"); }

                }
            }

            //Debug.WriteLine("before TF");
            if (treefall_checkbox.Checked)
            {
                if (t <= (end_time - 300)) // if there is no tillage
                {
                    calculate_tree_fall();
                }
            }

            if (bedrock_weathering_active)
            {
                calculate_bedrock_weathering();
                soil_update_split_and_combine_layers();
            }

            if (creep_active)
            {
                try
                { //Debug.WriteLine("calculating creep");
                    comb_sort();

                    calculate_creep();

                    soil_update_split_and_combine_layers();
                }
                catch { Debug.WriteLine(" failed during creep calculations"); }
            }

            if (blocks_active == 1)
            {
                try
                {
                    hardlayer_breaking();
                    Debug.WriteLine(" broke hard layer");
                    block_weathering();
                    //Debug.WriteLine(" weathered blocks");
                    block_movement();
                    //Debug.WriteLine(" moved blocks");
                    if (t % 1 == 0 | t > 3995)
                    {
                        string outblocks = (workdir + "\\" + "outblocks" + t + ".txt");
                        out_blocks(outblocks);
                        string outopenness = (workdir + "\\" + "openness" + t + ".asc");
                        out_float(outopenness, hardlayeropenness_fraction);
                        if (blocks_active == 1)
                        {
                            Debug.WriteLine("topo control on rolling: " + topoconttoroll + " creep control " + creepconttoroll + " creep control ratio: " + (creepconttoroll / (creepconttoroll + topoconttoroll)));
                            Debug.WriteLine("blocks produced: " + blocksproduced + " rolls: " + blocksrolled + " rolls per block: " + (blocksrolled / blocksproduced));
                        }
                    }
                }
                catch { Debug.WriteLine(" failed during block calculations"); }
            }

            // Debug.WriteLine("before TI");
            //displaysoil(0, 0);
            if (tillage_active)
            {
                comb_sort();
                int tilltime = 0;
                if (check_time_till_fields.Checked) { tilltime = till_record[t]; }
                else { tilltime = 1; }

                //if (t > (end_time - 300)) { tilltime = 1; } //MvdM Code to simulate tillage only in the last 500 years of the run. Used in SOIL paper SOIl2: to 200 a 
                //if (t > (end_time - 100)) { tilltime = 0; } //MvdM Code to simulate tillage only in the last 500 years of the run. Used in SOIL paper SOIl2: to 200 a 

                if (tilltime == 1)
                {

                    initialise_every_till();
                    calculate_tillage();
                    soil_update_split_and_combine_layers();
                }

            }

            //Debug.WriteLine("after TI");
            //displaysoil(0, 0);
            if (landslide_active)
            {
                //Debug.WriteLine("calculating landsliding");
                soil_update_split_and_combine_layers();
                comb_sort();
                ini_slope();
                calculate_critical_rain();
                calculate_slide_new();
                soil_update_split_and_combine_layers();
            }

            geo_t += DateTime.Now - geo_start;

            #endregion

            #region Pedogenic processes

            pedo_start = DateTime.Now;

            // Debug.WriteLine("before PW");
            //displaysoil(0, 0);
            if (soil_phys_weath_active)
            {
                // Debug.WriteLine("calculating soil physical weathering");
                if (Spitsbergen_case_study.Checked == false) { soil_physical_weathering(); }
                else
                {
                    SPITS_soil_physical_weathering();
                    SPITS_aeolian_deposition();
                }
                soil_update_split_and_combine_layers();

            }
            // Debug.WriteLine("before CW");
            //displaysoil(0, 0);
            if (soil_chem_weath_active)
            {
                //Debug.WriteLine("calculating soil chemical weathering");
                soil_chemical_weathering();
                soil_update_split_and_combine_layers();
                if (timeseries.total_average_soilthickness_checkbox.Checked)
                {
                    timeseries_matrix[t, timeseries_order[29]] = total_average_soilthickness_m;
                }
                if (timeseries.timeseries_number_soil_thicker_checkbox.Checked)
                {
                    timeseries_matrix[t, timeseries_order[30]] = number_soil_thicker_than;
                }
                if (timeseries.timeseries_coarser_checkbox.Checked)
                {
                    timeseries_matrix[t, timeseries_order[31]] = number_soil_coarser_than;
                }
                if (timeseries.timeseries_soil_depth_checkbox.Checked)
                {
                    timeseries_matrix[t, timeseries_order[32]] = local_soil_depth_m;
                }
                if (timeseries.timeseries_soil_mass_checkbox.Checked)
                {
                    timeseries_matrix[t, timeseries_order[33]] = local_soil_mass_kg;
                }
            }

            // Debug.WriteLine("before CT");
            //displaysoil(0, 0);
            if (soil_clay_transloc_active)
            {
                // Debug.WriteLine("calculating soil clay dynamics ");

                if (Spitsbergen_case_study.Checked == true)
                {
                    soil_silt_translocation(); // Spitsbergen case study
                }
                else
                {
                    if (ct_Jagercikova.Checked == true)
                    {
                        soil_clay_translocation_Jagercikova();
                    }
                    else
                    {
                        soil_clay_translocation();
                    }
                }
                soil_update_split_and_combine_layers();
                if (NA_in_map(dtm) > 0 | NA_in_map(soildepth_m) > 0)
                {
                    Debug.WriteLine("err_ets1");
                }

            }
            if (NA_anywhere_in_soil() == true) { Debug.WriteLine("NA found before soil carbon"); }
            //displaysoil(0, 0);
            if (soil_carbon_active)
            {
                // Debug.WriteLine("calculating carbon dynamics ");

                soil_carbon_cycle();
                soil_update_split_and_combine_layers();

            }
            if (NA_anywhere_in_soil() == true) { Debug.WriteLine("NA found after soil carbon"); }

            if (soil_bioturb_active)
            {
                for (int row = 0; row < nr; row++)
                {
                    for (int col = 0; col < nc; col++)
                    {
                        update_all_layer_thicknesses(row, col);
                    }
                }
                // Debug.WriteLine("calculating bioturbation");
                soil_bioturbation_mixing();
                soil_bioturbation_mounding();
                // if (findnegativetexture()) { Debugger.Break(); }

                soil_update_split_and_combine_layers();
                // if (findnegativetexture()) { Debugger.Break(); }

            }
            if (NA_anywhere_in_soil() == true) { Debug.WriteLine("NA found after soil bioturb"); }

            pedo_t += DateTime.Now - pedo_start;

            #endregion

            #region write output

            // Debug.WriteLine("before output");
            numfile++;

            int t_out = t + 1;
            if ((Final_output_checkbox.Checked && t_out == end_time) || (Regular_output_checkbox.Checked && ((t_out) % (int.Parse(Box_years_output.Text)) == 0)))
            {
                if (t == end_time - 1)
                {

                    Debug.WriteLine("Time balance. Geomorphic processes: {0} min, pedogenic processes: {1} min, hydrologic processes: {2} min, ponding {3} min", geo_t, pedo_t, hydro_t, ponding_t);
                    // Debug.WriteLine("Time balance OSL methods. Long matrix: {0} min. Jagged array: {1} min.", OSL_matrix_t, OSL_JA_t);
                }
                //Debug.WriteLine("Attempting to write outputs");

                // displaysoil(31, 12);
                // Debug.WriteLine("Total catchment mass = " + total_catchment_mass_decimal());
                try
                {
                    //Debug.WriteLine("writing all soils");
                    writeallsoils(workdir + "\\" + run_number + "_" + t_out + "_out_allsoils.csv");
                }
                catch
                {
                    Debug.WriteLine("Failed during writing of soils");
                }

                if (Altitude_output_checkbox.Checked)
                {

                    try { out_double(workdir + "\\" + run_number + "_" + t_out + "_out_dtm.asc", dtm); }
                    catch { MessageBox.Show("dtm has not been written"); }

                    try { out_double(workdir + "\\" + run_number + "_" + t_out + "_out_dz_soil.asc", dz_soil); }
                    catch { MessageBox.Show("dz_soil has not been written"); }

                    //try { out_double(workdir + "\\" + run_number + "_" + t + "_out_dzero.asc", dz_ero_m); }
                    //catch { MessageBox.Show("dzero has not been written"); }
                    //try { out_double(workdir + "\\" + run_number + "_" + t + "_out_dzsed.asc", dz_sed_m); }
                    //catch { MessageBox.Show("dzsed has not been written"); }
                }
                if (treefall_checkbox.Checked)
                {
                    try
                    {
                        out_double(workdir + "\\" + run_number + "_" + t_out + "_out_dz_treefall.asc", dz_treefall);
                        out_integer(workdir + "\\" + run_number + "_" + t_out + "_out_treefallcount.asc", treefall_count);

                    }
                    catch { MessageBox.Show("treefall has not been written"); }
                }
                if (Soildepth_output_checkbox.Checked)
                {
                    try { out_double(workdir + "\\" + run_number + "_" + t_out + "_out_soildepth.asc", soildepth_m); }
                    catch { MessageBox.Show("soildepth has not been written"); }
                }
                if (Alt_change_output_checkbox.Checked)
                {
                    try { out_double(workdir + "\\" + run_number + "_" + t_out + "_out_change.asc", dtmchange_m); }
                    catch { MessageBox.Show("change has not been written"); }
                }

                if (water_output_checkbox.Checked & Water_ero_checkbox.Checked)
                {
                    // Debug.WriteLine("before writing water flow");

                    try
                    {
                        if (daily_water.Checked)
                        {
                            for (int roww = 0; roww < nr; roww++)
                            {
                                for (int colw = 0; colw < nc; colw++)
                                {
                                    waterflow_m3[roww, colw] = OFy_m[roww, colw, 0];
                                }
                            }
                        }
                        out_double(workdir + "\\" + run_number + "_" + t_out + "_out_water.asc", waterflow_m3);
                    }
                    catch { MessageBox.Show("water has not been written"); }
                }
                if (depressions_output_checkbox.Checked)
                {
                    try { out_integer(workdir + "\\" + run_number + "_" + t_out + "_out_depress.asc", depression); }
                    catch { MessageBox.Show("depressions have not been written"); }
                    try { out_double(workdir + "\\" + run_number + "_" + t_out + "_out_dtmfillA.asc", dtmfill_A); }
                    catch { MessageBox.Show("dfmfill has not been written"); }
                }
                if (Water_ero_checkbox.Checked)
                {
                    // Debug.WriteLine("before writing water erosion");

                    if (all_process_output_checkbox.Checked)
                    {
                        try { out_double(workdir + "\\" + run_number + "_" + t_out + "_out_water_erosion.asc", sum_water_erosion); }
                        catch { MessageBox.Show("water erosion has not been written"); }
                    }
                }
                if (creep_active_checkbox.Checked)
                {
                    // Debug.WriteLine("before writing creep");

                    try { out_double(workdir + "\\" + run_number + "_" + t_out + "_out_creep.asc", creep); }
                    catch { MessageBox.Show("creep has not been written"); }

                }

                if (Tillage_checkbox.Checked)
                {
                    // Debug.WriteLine("before writing tillage erosion");

                    if (all_process_output_checkbox.Checked)
                    {
                        try { out_double(workdir + "\\" + run_number + "_" + t_out + "_out_tillage.asc", sum_tillage); }
                        catch { MessageBox.Show("tillage has not been written"); }
                    }
                }

                if (OSL_checkbox.Checked)
                {
                    try
                    {
                        //writeOSLages();
                        writeOSLages_jaggedArray();
                    }
                    catch
                    {
                        Debug.WriteLine("OSl ages have not been written");
                    }
                }
                if (Landslide_checkbox.Checked)
                {
                    try { out_double(workdir + "\\" + run_number + "_" + t_out + "_crrain.asc", crrain_m_d); }
                    catch { MessageBox.Show("crrain has not been written"); }
                    try { out_double(workdir + "\\" + run_number + "_" + t_out + "_ca.asc", camf); }
                    catch { MessageBox.Show("ca has not been written"); }
                }
                //Debug.WriteLine("after outputs");

                if (daily_water.Checked)
                {
                    Debug.WriteLine("writing daily water");

                    //try { out_double(workdir + "\\" + run_number + "_" + t_out + "_out_aridity.asc", aridity_vegetation); }
                    //catch { MessageBox.Show("vegetation has not been written"); }

                    try { out_double(workdir + "\\" + run_number + "_" + t_out + "_out_infiltration_m.asc", Iy); }
                    catch { MessageBox.Show("infiltration has not been written"); }

                    try { out_double(workdir + "\\" + run_number + "_" + t_out + "_out_actual_evapotranspiration_m.asc", ETay); }
                    catch { MessageBox.Show("ETa has not been written"); }

                    try
                    {
                        out_integer(workdir + "\\" + run_number + "_" + t_out + "_out_vegetationtype.asc", vegetation_type);
                        for (int row = 0; row < nr; row++)
                        {
                            for (int col = 0; col < nc; col++)
                            {
                                vegetation_type[row, col] = 0; // reset vegetation_type, to give the output per output period
                            }
                        }
                    }
                    catch { MessageBox.Show("vegetation type has not been written"); }
                }

                if (version_lux_checkbox.Checked == true)
                {
                    try
                    {
                        // outputs for case study Luxembourg. Focus on different litter types
                        // young labile OM is hornbeam, old stable OM is beech
                        // Outputs:
                        // SOM stocks entire profile: total, young, old (kg/m2)
                        // top layer: total, old, young (-) 

                        string[] litter_types = { "hornbeam", "beech", "total" };
                        string[] litter_outputs = { "stocks_kgm2", "toplayer_frac" };

                        foreach (string type in litter_types) // loop over different SOM types
                        {
                            // determine which SOM fraction should be considered
                            bool h_bool = false; bool b_bool = false;
                            if (type == "hornbeam") { h_bool = true; }
                            if (type == "beech") { b_bool = true; }
                            if (type == "total") { h_bool = true; b_bool = true; }

                            foreach (string output in litter_outputs)
                            {
                                // determine which layers to consider and what to calculate
                                int numberoflayers = 0;
                                if (output == "stocks_kgm2") { numberoflayers = max_soil_layers; }
                                if (output == "toplayer_frac") { numberoflayers = 1; }

                                double[,] output_litter_map = new double[nr, nc];
                                for (int row = 0; row < nr; row++)
                                {
                                    for (int col = 0; col < nc; col++)
                                    {
                                        double litterstock_kg = 0;
                                        double mineralsoil_toplayer_kg = 0;
                                        if (h_bool) { litterstock_kg += litter_kg[row, col, 0]; }
                                        if (b_bool) { litterstock_kg += litter_kg[row, col, 1]; }

                                        if (output == "toplayer_frac")
                                        {
                                            for (int tex = 0; tex < 5; tex++)
                                            {
                                                mineralsoil_toplayer_kg += texture_kg[row, col, 0, tex];
                                            }
                                        }

                                        if (output == "toplayer_frac") { litterstock_kg /= (mineralsoil_toplayer_kg + litterstock_kg); } // calculate to fraction
                                        if (output == "stocks_kgm2") { litterstock_kg /= (dx * dx); } // calculate to kg/m2
                                        output_litter_map[row, col] = litterstock_kg;
                                    }
                                }
                                try { out_double(workdir + "\\" + run_number + "_" + t_out + "_out_litter_" + type + "_" + output + ".asc", output_litter_map); }
                                catch { MessageBox.Show("litter output has not been written"); }
                            }
                        }
                        try { out_double(workdir + "\\" + run_number + "_" + t_out + "_out_TPI.asc", tpi); }
                        catch { MessageBox.Show("TPI output has not been written"); }

                        /* CODE BLOCK BELOW WRITES OUT DIFFERENT ORGANIC MATTER MAPS. THIS IS NOT NECESSARY ANYMORE NOW LITTER IS STORED IN ITS OWN MATRIX
                         * 
                        // outputs for case study Luxembourg. Focus on different organic matter types
                        // young labile OM is hornbeam, old stable OM is beech
                        // Outputs:
                        // SOM stocks entire profile: total, young, old (kg/m2)
                        // top layer: total, old, young (-) 

                        string[] SOM_types = { "young", "old", "total" };
                        string[] SOM_outputs = { "stocks_kgm2", "toplayer_frac" };

                        foreach (string type in SOM_types) // loop over different SOM types
                        {
                            // determine which SOM fraction should be considered
                            bool y_bool = false; bool o_bool = false;
                            if (type == "young") { y_bool = true; }
                            if (type == "old") { o_bool = true; }
                            if (type == "total") { y_bool = true; o_bool = true; }

                            foreach (string output in SOM_outputs)
                            {
                                // determine which layers to consider and what to calculate
                                int numberoflayers = 0;
                                if (output == "stocks_kgm2") { numberoflayers = max_soil_layers; }
                                if (output == "toplayer_frac") { numberoflayers = 1; }

                                double[,] output_SOM_map = new double[nr, nc] ;
                                for (int row = 0; row < nr; row++)
                                {
                                    for (int col = 0; col < nc; col++)
                                    {
                                        double SOMstock_kg = 0;
                                        double mineralsoil_kg = 0;
                                        for (int lay = 0; lay < numberoflayers; lay++)
                                        {
                                            if (y_bool) { SOMstock_kg += young_SOM_kg[row, col, lay]; }
                                            if (o_bool) { SOMstock_kg += old_SOM_kg[row, col, lay]; }

                                            if (output == "toplayer_frac")
                                            {
                                                for (int tex = 0; tex < 5; tex++)
                                                {
                                                    mineralsoil_kg += texture_kg[row, col, lay, tex];
                                                }
                                            }
                                        }
                                        if (output == "toplayer_frac") { SOMstock_kg /= (mineralsoil_kg + SOMstock_kg); } // calculate to fraction
                                        if (output == "stocks_kgm2") { SOMstock_kg /= (dx * dx); } // calculate to kg/m2
                                        output_SOM_map[row, col] = SOMstock_kg;
                                    }
                                }
                                try { out_double(workdir + "\\" + run_number + "_" + t_out + "_out_SOM_" + type + "_" + output + ".asc", output_SOM_map); }
                                catch { MessageBox.Show("SOM output has not been written"); }
                            }
                        }
                        try { out_double(workdir + "\\" + run_number + "_" + t_out + "_out_TPI.asc", tpi); }
                        catch { MessageBox.Show("TPI output has not been written"); }

                        */
                    }
                    catch
                    {
                        Debug.WriteLine("Error in writing litterwater_ outputs for Luxembourg case study");
                    }
                }

               
            }

            if (t == end_time - 1)
            {
                Task.Factory.StartNew(() =>
                {
                    this.InfoStatusPanel.Text = " --scenario finished--";
                }, CancellationToken.None, TaskCreationOptions.None, guiThread);
                stopwatch.Stop();
                Debug.WriteLine("Most recent scenario: " + stopwatch.Elapsed);
                //Timeseries output
                if (number_of_outputs > 0) { timeseries_output(); }
            }
            #endregion

        }
        private void comb_sort()      //sorts the data cells in a dtm in order of increasing altitude
        {
            // comb sorting by Wlodek Dobosiewicz in 1980
            // http://en.wikipedia.org/wiki/Comb_sort
            // LORICA adaptation by Arnaud Temme june 2009
            //Debug.WriteLine("sorting. nr " + nr + " nc " + nc + " t " + t);
            /*
            Task.Factory.StartNew(() =>
            {
                this.InfoStatusPanel.Text = "sorting";
            }, CancellationToken.None, TaskCreationOptions.None, guiThread);
            */
            int i = 0;
            double dtm_temp = 0;
            int row_temp = 0, col_temp = 0;
            string rowcol_temp;
            //Debug.WriteLine("sorting. nr " + nr + " nc " + nc + " t " + t);
            if (Proglacial_checkbox.Checked) //Proglacial
            {
                number_of_data_cells = 0;
                for (int row = 0; row < nr; row++)
                {
                    for (int col = 0; col < nc; col++)
                    {
                        if (dtm[row, col] != -9999)
                        {
                            index[i] = dtm[row, col]; row_index[i] = row; col_index[i] = col; rowcol_index[i] = row.ToString() + "." + col.ToString();
                            i++;
                        }
                    }
                }
                number_of_data_cells = i;
            }

            else
            {
                if (t == t_intervene)  // only in the first timestep;
                {
                    //Debug.WriteLine("normal sorting. nr " + nr + " nc " + nc + " t " + t);
                    number_of_data_cells = 0;
                    for (int row = 0; row < nr; row++)  // why not do this only in the first timestep? And use the existing one as input in subsequent timesteps?
                    {
                        for (int col = 0; col < nc; col++)
                        {
                            if (dtm[row, col] != -9999)
                            {
                                index[i] = dtm[row, col]; row_index[i] = row; col_index[i] = col; rowcol_index[i] = row.ToString() + "." + col.ToString();
                                i++;
                            }
                        }
                    }
                    number_of_data_cells = i;
                }
                else
                {
                    //Debug.WriteLine("alternative sorting. nr " + nr + " nc " + nc + " t " + t);
                    for (i = 0; i < number_of_data_cells; i++)
                    {
                        index[i] = dtm[row_index[i], col_index[i]];     //merely update the existing index with the adapted altitudes and then sort     
                    }
                }
            }
            //displayonscreen(0, 0);
            //Task.Factory.StartNew(() =>
            //{
            //    this.InfoStatusPanel.Text = "data cells: " + number_of_data_cells;
            //}, CancellationToken.None, TaskCreationOptions.None, guiThread);
            //Debug.WriteLine("\n--sorting overview--");
            //Debug.WriteLine("Sorting " + number_of_data_cells + " cells");
            long gap = number_of_data_cells;
            bool swaps;
            long total_swaps = 0;
            //while (gap > 1 && swaps == true)  // in freak? situations, swaps may be false for gap = x, but true for subsequent values of gap
            while (gap > 1)
            {
                if (gap > 1)
                {
                    if (gap == 2) { gap = 1; }
                    gap = Convert.ToInt64(gap / 1.2);
                }
                i = 0;
                swaps = false;
                //this.InfoStatusPanel.Text = "i " + i + " gap " + gap + " tot swaps " + total_swaps;
                //Debug.WriteLine("i " + i + " gap " + gap + " tot swaps " + total_swaps);
                while (i + gap < number_of_data_cells)
                {
                    //if (gap == Convert.ToInt64(number_of_data_cells / 1.2) && i < 10) {Debug.WriteLine("    i " + i + " gap " + gap + " tot swaps " + total_swaps + " alt1 " + index[i] + " (" + row_index[i] + "," + col_index[i] + ") alt2 " + index[i+gap] + " (" + row_index[i+gap] + "," + col_index[i+gap] + ")"); }
                    if (index[i] > index[i + gap])
                    {
                        dtm_temp = index[i]; index[i] = index[i + gap]; index[i + gap] = dtm_temp;
                        row_temp = row_index[i]; row_index[i] = row_index[i + gap]; row_index[i + gap] = row_temp;
                        col_temp = col_index[i]; col_index[i] = col_index[i + gap]; col_index[i + gap] = col_temp;
                        rowcol_temp = rowcol_index[i]; rowcol_index[i] = rowcol_index[i + gap]; rowcol_index[i + gap] = rowcol_temp;
                        swaps = true;
                        total_swaps++;
                    } // end if
                    i++;
                }  // end while
                   //if (gap < 4) { Debug.WriteLine("i " + i + " gap " + gap + " tot swaps " + total_swaps); }
            } //end while
            int sorting_error = 0;
            for (i = 0; i < number_of_data_cells - 1; i++)
            {
                if (index[i] > index[i + 1]) { sorting_error = 1; }
            }
            if (sorting_error == 1)
            {
                Debug.WriteLine(" Sorting error in comb_sort ");
            }
            else
            {
                //Debug.WriteLine(" Sorting successful ");
            }
        }
        #endregion
    }

}

