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

        int[] P_all, ET0_all, Tavg_all, Tmin_all, Tmax_all, D_all;
        double[] Py = new double[365], ET0_m = new double[12];
        int[] Tavgy = new int[365], Tminy = new int[365], Tmaxy = new int[365], Dy = new int[365];
        // int[] D_all = new int[123], Dy = new int[365];
        double[,,] OFy_m, Ks_md, water_balance_m, Ra_rcm;
        double[,] Iy, ROy, Ks_topsoil_m_h, pond_d, pond_y, outflow_y, stagdepth, waterfactor, total_outflow_y, ETay, ET0y;
        int[] month = new int[12] { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
        int[] monthcum = new int[12] { 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334, 365 };
        int[] midmonthdays = new int[] { 16, 46, 75, 106, 136, 167, 197, 228, 259, 289, 320, 350 };
        double Ks_min_mh, Ks_max_mh, snow_m, snow_start_m, snowfall_m, snowmelt_factor_mTd, snow_threshold_C, Pduringsnowmelt; // snow thickness is now constant in space. develop: spatially varying effects of snowfall. snowmelt factor in m T-1 d-1
                                                                                                                               // snowmelt modeling in Hock, 2003, Eq. 2: https://www.sciencedirect.com/science/article/pii/S0022169403002579#BIB50
                                                                                                                               // read data into P_all  etc
        DateTime ponding_start;

        double local_solar_radiation(double slope_rad, double aspect_rad, int month)
        {
            //Debug.WriteLine("lsr1");
            // SOURCE: Swift 1976: Algorithm for solar radiation on mountain slopes
            // https://doi.org/10.1029/WR012i001p00108
            double lat_rad, L1_Ra, L2_Ra, D1_Ra, D_Ra, E_Ra, R0_Ra, R1_Ra, R4_Ra, T_Ra, T7_Ra, T6_Ra, T3_Ra, T2_Ra, T1_Ra, T0_Ra, acos_in;

            R0_Ra = 1.95 * 0.041868; // Convert from cal/cm2/min to MJ/m2/min
            lat_rad = Math.PI / 180 * (System.Convert.ToDouble(latitude_deg.Text) + System.Convert.ToDouble(latitude_min.Text) / 60); // latitude [rad]
            L1_Ra = Math.Asin(Math.Cos(slope_rad) * Math.Sin(lat_rad) + Math.Sin(slope_rad) * Math.Cos(lat_rad) * Math.Cos(aspect_rad)); // equivalent latitude [rad]
            D1_Ra = Math.Cos(slope_rad) * Math.Cos(lat_rad) - Math.Sin(slope_rad) * Math.Sin(lat_rad) * Math.Cos(aspect_rad);
            if (D1_Ra == 0) { D1_Ra = 1E-10; }
            L2_Ra = Math.Atan(Math.Sin(slope_rad) * Math.Sin(aspect_rad) / D1_Ra);
            if (D1_Ra < 0) { L2_Ra += Math.PI; }

            int day_Ra = midmonthdays[month];

            D_Ra = 0.4 * Math.PI / 180 - 23.3 * Math.PI / 180 * Math.Cos((day_Ra + 10) * Math.PI / 180 * 0.986);
            E_Ra = 1 - 0.0167 * Math.Cos((day_Ra - 3) * Math.PI / 180 * 0.986);
            R1_Ra = 60 * R0_Ra / (E_Ra * E_Ra);

            acos_in = -Math.Tan(L1_Ra) * Math.Tan(D_Ra);
            if (acos_in > 1) { acos_in = 1; }
            T_Ra = Math.Acos(acos_in);
            T7_Ra = T_Ra - L2_Ra;
            T6_Ra = -T_Ra - L2_Ra;

            acos_in = -Math.Tan(lat_rad) * Math.Tan(D_Ra);
            if (acos_in > 1) { acos_in = 1; }
            T_Ra = Math.Acos(acos_in);
            T1_Ra = T_Ra;
            T0_Ra = -T_Ra;
            if (T7_Ra < T1_Ra) { T3_Ra = T7_Ra; } else { T3_Ra = T1_Ra; }
            if (T6_Ra > T0_Ra) { T2_Ra = T6_Ra; } else { T2_Ra = T0_Ra; }

            R4_Ra = R1_Ra * (Math.Sin(D_Ra) * Math.Sin(L1_Ra) * (T3_Ra - T2_Ra) * 12 / Math.PI + Math.Cos(D_Ra) * Math.Cos(L1_Ra) * (Math.Sin(T3_Ra + L2_Ra) - Math.Sin(T2_Ra + L2_Ra)) * 12 / Math.PI);
            //Debug.WriteLine("lsr2");
            if (R4_Ra < 0)
            {
                Debug.WriteLine("err_lsr1");
            }
            return (R4_Ra * 0.408 / 1000); // convert to m/d
        }

        void update_solar_radiation()
        {
            if (t % 100 == 0)
            {
                update_slope_and_aspect(); // updates slopemap slopeAnalysis [rad] and aspect [rad]
            }

            for (int hrow = 0; hrow < nr; hrow++)
            {
                for (int hcol = 0; hcol < nc; hcol++)
                {
                    if (dtm[hrow, hcol] != -9999)
                    {
                        for (int mo = 0; mo < 12; mo++)
                        {
                            //Debug.WriteLine("sr1");
                            // Fill 3D matrix with local monthly ET
                            Ra_rcm[hrow, hcol, mo] = local_solar_radiation(slopeAnalysis[hrow, hcol], aspect[hrow, hcol], mo);
                            //Debug.WriteLine("sr2");
                        }

                    }
                }
            }
        }

        double total_snow_melt, total_water_flow;
        void water_balance()
        {
            // Debug.WriteLine("wb1");
            total_water_flow = 0;
            if (t % 100 == 0)
            {
                update_solar_radiation();
            }
            Pduringsnowmelt = 0;

            total_snow_melt = 0;
            snow_start_m = snow_m;
            snowfall_m = 0;
            create_daily_weather(); // Calculate daily weather variables

            // Create yearly matrices for infiltration and overland flow
            // Debug.WriteLine("wb2");
            for (int row = 0; row < nr; row++)
            {
                for (int col = 0; col < nc; col++)
                {
                    pond_y[row, col] = 0;
                    outflow_y[row, col] = 0;
                    waterfactor[row, col] = 1;
                    total_outflow_y[row, col] = 0;
                    ETay[row, col] = 0;
                    ET0y[row, col] = 0;
                    Iy[row, col] = 0;
                    for (int dir = 0; dir < 10; dir++)
                    {
                        OFy_m[row, col, dir] = 0;
                    }
                }
            }
            comb_sort();

            double P_OF, snowmelt_m = 0;
            // Debug.WriteLine("wb3");
            if (t % 10 == 0) { update_Ks(); }

            // Debug.WriteLine("wb4");
            int daycount = 0;
            for (int mo = 0; mo < 12; mo++)
            {
                // create monthly timeseries
                double[] Pm = new double[month[mo]];
                int[] Dm = new int[month[mo]], Tavgm = new int[month[mo]];
                P_OF = 0;
                Array.Copy(Py, daycount, Pm, 0, month[mo]);
                Array.Copy(Dy, daycount, Dm, 0, month[mo]);
                Array.Copy(Tavgy, daycount, Tavgm, 0, month[mo]);
                ;
                // daily overland flow
                snow_threshold_C = 0;
                for (int day = 0; day < month[mo]; day++)
                {
                    if (Tavgm[day] <= snow_threshold_C) // temperature below 0, all rain falls as snow
                    {
                        if (Pm[day] > 0)
                        {
                            // Debug.WriteLine("snowfall");
                            snowfall_m += Pm[day];
                            snow_m += Pm[day];
                            P_OF += Pm[day];
                        }
                    }
                    else // T above 0, snow can melt and is all added to runoff. 
                    {
                        if (snow_m > 0) // Snow present
                        {
                            Pduringsnowmelt += Pm[day];
                            snowmelt_m = snowmelt_factor_mTd * (Tavgm[day] - snow_threshold_C);
                            if (snowmelt_m > snow_m) { snowmelt_m = snow_m; }
                            snow_m -= snowmelt_m;
                            total_snow_melt += snowmelt_m;
                            // Debug.WriteLine("t {0}, m {1} d {2} snowmelt {3} m", t,mo, day,snowmelt_m);
                            dailyflow(Pm[day], Dm[day], day, mo, snowmelt_m); // all snowmelt (+extra rain) becomes overland flow
                            P_OF += Pm[day];

                        }
                        else // no snow cover, rainfall intensity is used as threshold
                        {
                            if (Pm[day] > (Ks_min_mh * Dm[day]) && Dm[day] != 0) // develop. If rainfall is also spatially variable, this has to be adjusted
                            {
                                // Debug.WriteLine("wb4a");
                                //Debug.WriteLine("Overland flow initiated at date {0}/{1}/{2}", day, mo, t);
                                dailyflow(Pm[day], Dm[day], day, mo, 0);
                                P_OF += Pm[day];
                            }
                        }

                        if (snow_m < 0)
                        {
                            Debug.WriteLine("err_sno1");
                        }
                    }
                }

                //if (Pm.Sum() < P_OF) { MessageBox.Show("Pd > P"); }
                // Monthly water balance
                for (int row = 0; row < nr; row++)
                {
                    for (int col = 0; col < nc; col++)
                    {
                        if (dtm[row, col] != -9999)
                        {
                            double ET0m_act = ET0_m[mo] * Ra_rcm[row, col, mo] * veg_correction_factor[row, col];
                            // if (row == 0 & col == 0) { Debug.WriteLine(ET0m_act); }
                            ET0y[row, col] += ET0m_act;
                            double ETam = Pm.Sum() / Math.Pow(1 + Math.Pow(Pm.Sum() / (ET0m_act), 1.5), (1 / 1.5));

                            ETay[row, col] += ETam;

                            Iy[row, col] += (Pm.Sum() - P_OF) - ETam; // overland flow has been dealt with earlier (dailyflow), just as ponding
                            if (double.IsNaN(Iy[row, col]))
                            {
                                Debug.WriteLine("err wb1");
                            }
                        }
                    }
                }
                // Debugger.Break();

                daycount += month[mo];
            } // end months

            // yearly update infiltration

            bool Ineg = false;
            for (row = 0; row < nr; row++)
            {
                for (col = 0; col < nc; col++)
                {
                    if (Iy[row, col] < 0)
                    {
                        Ineg = true;
                    }
                }
            }
            if (Ineg)
            {
                ;
            }
            //if (t % 10 == 0) { Debugger.Break(); }
            // Debug.WriteLine("wb6");
        }

        void print_water_balance()
        {
            double P_wb = 0, ETa_wb = 0, I_wb = 0, OutF_wb = 0, snow_wb = 0, snow_left = 0, snow_start = 0, OFy_0 = 0, OFy_9 = 0;
            for (int rowb = 0; rowb < nr; rowb++)
            {
                for (int colb = 0; colb < nc; colb++)
                {
                    if (dtm[rowb, colb] != -9999)
                    {
                        P_wb += Py.Sum();
                        ETa_wb += ETay[rowb, colb];
                        I_wb += Iy[rowb, colb];
                        OutF_wb += total_outflow_y[rowb, colb];
                        snow_wb += snowfall_m;
                        snow_start += snow_start_m;
                        snow_left += snow_m;
                        OFy_0 += OFy_m[row, col, 0];
                        OFy_9 += OFy_m[row, col, 9];
                    }

                }
            }
            double balance = P_wb - ETa_wb - I_wb - OutF_wb;
            Debug.WriteLine("Snow start: {0}, snow end: {1}, P during snow: {2}", snow_start, snow_left, Pduringsnowmelt * nr * nc);
            Debug.WriteLine("Annual water balance");
            Debug.WriteLine("P: {0}, of which snowfall: {1}.  ETa: {2}. I: {3}. Outflow: {4}. Balance: {5}. OFy_0: {6}, OFy_9: {7}", P_wb, snow_wb, ETa_wb, I_wb, OutF_wb, balance, OFy_0, OFy_9);
            if (double.IsNaN(I_wb))
            {
                Debug.WriteLine("err_pwb1");
            }
            if (Math.Abs(balance + snow_start - snow_left) > 0.00000001)
            {
                Debug.WriteLine("err_pwb2");
            }
        }

        void print_spatial_water_balance()
        {
            for (int rowb = 0; rowb < nr; rowb++)
            {
                for (int colb = 0; colb < nc; colb++)
                {
                    Debug.WriteLine("{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11} {12}", rowb, colb, t, Py.Sum(), ETay[rowb, colb], OFy_m[rowb, colb, 0] - OFy_m[rowb, colb, 9], Iy[rowb, colb], (Py.Sum() - ETay[rowb, colb] - Iy[rowb, colb]), snow_m, snow_start_m, snowfall_m, total_outflow_y[rowb, colb], Pduringsnowmelt);
                    // snowfall_m is part of Py.Sum(), therefore, this should not be accounted for in the balance. Important terms are P, ETa, I and outflow. They close the balance. 
                }
            }
        }

        void print_P_ET0()
        {
            double ET_out = 0;
            double count = 0;
            for (row = 0; row < nr; row++)
            {
                for (col = 0; col < nc; col++)
                {
                    ET_out += ET0y[row, col];
                    count += 1;

                }
            }
            Debug.WriteLine("P {0} ET0 {1}", Py.Sum(), ET_out / count);
        }

        void update_potential_ET()
        {
            for (int hrow = 0; hrow < nr; hrow++)
            {
                for (int hcol = 0; hcol < nc; hcol++)
                {

                }
            }
        }

        void update_Ks() // both Ks matrix as Ks for topsoil
        {
            double[] tex_topsoil;
            double depth, fsilt, fclay, fOM, BD_t_kg_m3, slope_rad;
            int lay;
            Ks_min_mh = 1000; Ks_max_mh = 0;
            List<double> BD_topsoil;

            // Debug.WriteLine("uks1");
            for (int row = 0; row < nr; row++)
            {
                for (int col = 0; col < nc; col++)
                {
                    BD_topsoil = new List<double>();
                    depth = 0;
                    tex_topsoil = new double[7];
                    lay = 0;
                    if (total_soil_mass_kg_decimal(row, col) > 0)
                    {
                        while (depth <= 0.5 & lay < max_soil_layers)
                        {
                            // if (lay == max_soil_layers) { Debugger.Break(); }
                            if (total_layer_mass_kg(row, col, lay) > 0)
                            {
                                depth += layerthickness_m[row, col, lay] / 2;

                                for (int text = 0; text < 5; text++)
                                {
                                    tex_topsoil[text] += texture_kg[row, col, lay, text];
                                }
                                tex_topsoil[5] += young_SOM_kg[row, col, lay];
                                tex_topsoil[5] += old_SOM_kg[row, col, lay];

                                BD_topsoil.Add(bulk_density_calc_kg_m3(texture_kg[row, col, lay, 0], texture_kg[row, col, lay, 1], texture_kg[row, col, lay, 2], texture_kg[row, col, lay, 3], texture_kg[row, col, lay, 4], old_SOM_kg[row, col, lay], young_SOM_kg[row, col, lay], depth));
                                depth += layerthickness_m[row, col, lay] / 2;

                            }
                            lay += 1;
                        }

                        fsilt = 100 * tex_topsoil[2] / (tex_topsoil[1] + tex_topsoil[2] + tex_topsoil[3] + tex_topsoil[4]); // only fine fraction
                        fclay = 100 * (tex_topsoil[3] + tex_topsoil[4]) / (tex_topsoil[1] + tex_topsoil[2] + tex_topsoil[3] + tex_topsoil[4]); // only fine fraction
                        fOM = 100 * tex_topsoil[5] / (tex_topsoil[1] + tex_topsoil[2] + tex_topsoil[3] + tex_topsoil[4] + tex_topsoil[5]); // only fine fraction
                        BD_t_kg_m3 = BD_topsoil.Average();
                        slope_rad = calc_slope_stdesc(row, col);
                        double slope_test = Math.Cos(slope_rad);

                        // Debug.WriteLine("uks3a");
                        Ks_topsoil_m_h[row, col] = (Ks_wosten(fsilt, fclay, fOM, BD_t_kg_m3 / 1000, 1) / 24) * Math.Cos(slope_rad);
                    }
                    else
                    {
                        Ks_topsoil_m_h[row, col] = 0;
                        Debug.WriteLine("Empty soil at row {0}, col {1}, t {2}", row, col, t);
                    }

                    if (double.IsNaN(Ks_topsoil_m_h[row, col]))
                    {
                        Debug.WriteLine("err_kst1");
                    }
                    // Debug.WriteLine("uks4");

                    //  Ks_topsoil_mh[row, col] *= veg_lag_factor;
                    // Ks_update
                    if (Ks_min_mh > Ks_topsoil_m_h[row, col]) { Ks_min_mh = Ks_topsoil_m_h[row, col]; }
                    if (Ks_max_mh < Ks_topsoil_m_h[row, col]) { Ks_max_mh = Ks_topsoil_m_h[row, col]; }
                    // Debug.WriteLine("uks5");
                }
            }
        }

        void create_daily_weather()
        {
            //Debug.WriteLine("dw.start");
            // 1. Select yearly timeseries and the corrected temperatures

            double P_ann = rain_value_m;
            int T_ann = temp_value_C;

            Random year_w = new Random(t); // t as random seed to get deterministic results
            int n_timeseries = year_w.Next(0, System.Convert.ToInt32(daily_n.Text));
            // n_timeseries = 5;

            //Debug.WriteLine("dw.1c");
            Array.Copy(Tmin_all, 365 * (n_timeseries), Tminy, 0, 365); // read new Tmin timeseries
            Array.Copy(Tmax_all, 365 * (n_timeseries), Tmaxy, 0, 365); // read new Tmin timeseries
            Array.Copy(Tavg_all, 365 * (n_timeseries), Tavgy, 0, 365); // read new Tmin timeseries

            Array.Copy(P_all, 365 * (n_timeseries), Py, 0, 365); // read new P timeseries
            Array.Copy(D_all, 365 * (n_timeseries), Dy, 0, 365); // read new D timeseries
            for (int pi = 0; pi < 365; pi++)
            {
                Py[pi] /= 1000; // convert to meters
                                // if (Py[pi] > 0.036) { Debugger.Break(); }
            }

            // 2. Rescale rainfall and temperature
            //Debug.WriteLine("dw.1a");

            if (check_scaling_daily_weather.Checked) // scaling with yearly values, for global change scenarios
            {
                double Py_sum = Py.Sum();
                for (int pi = 0; pi < Py.Count(); pi++) { Py[pi] = Py[pi] / Py_sum * P_ann; } // Scale with yearly P in meters
                double total_P = Py.Sum();

                int d_T = T_ann - Convert.ToInt32(Tavgy.Average());
                for (int pi = 0; pi < Tavgy.Count(); pi++)
                {
                    Tminy[pi] += d_T;
                    Tmaxy[pi] += d_T;
                    Tavgy[pi] += d_T;
                }
            }

            // 3. Calculate PET according to Hargreaves https://www.repository.utl.pt/bitstream/10400.5/4250/1/REP-J.L.Teixeira-InTech-Hargreaves_and_other_reduced_set_methods_for_calculating_evapotranspiration.pdf
            // Use monthly T values, gives a result very similar to daily values
            // multiplication with extraterrestrial radiation occurs in a later step, when ET0_m is actually necessary (water balance). Here we capture the monthly variation. At the later step, the spatiotemporal differences in solar radiation are captured
            int[] Tminm, Tmaxm, Tavgm;
            int daycount = 0;
            for (int pi = 0; pi < 12; pi++)
            {
                Tminm = new int[month[pi]];
                Tmaxm = new int[month[pi]];
                Tavgm = new int[month[pi]];

                Array.Copy(Tminy, daycount, Tminm, 0, month[pi]);
                Array.Copy(Tmaxy, daycount, Tmaxm, 0, month[pi]);
                Array.Copy(Tavgy, daycount, Tavgm, 0, month[pi]);

                daycount += month[pi];
                //get temperature info
                ET0_m[pi] = 0.0023 * (Tavgm.Average() + 17.78) * Math.Sqrt(Tmaxm.Average() - Tminm.Average()) * month[pi]; // multiply value by number of days in the month, to get the monthly total
                if (ET0_m[pi] < 0) { ET0_m[pi] = 0; }

            }

            //Debug.WriteLine("dw.end"); 
        }

        void dailyflow(double P_total, double D_total, int qday, int qmonth, double snowmelt)
        {
            // Debug.WriteLine("df1");
            /*
            Check:
            -is all water flow reset after first iteration?
            -do dtm and ponding values correspond well?
            -is the water balance closing?
            -do all cells refer to OFd[r,c,0] as flow component? rainfall and overland flow
            -link infiltration to differences in Ks (negative Ks) !!!
             */

            pond_d = new double[nr, nc];
            double[,] currentflow = new double[nr, nc];
            // Debug.WriteLine("df1");
            //every cell, inflow and outflow;
            double powered_slope_sum, OF_tot1 = 0, OF_tot2 = 0, OF_tot3 = 0; ;
            double[,,] OFd = new double[nr, nc, 10];

            //0: total flow
            //1-8: flow to neighbours
            //
            // 1 2 3 
            // 4   5
            // 6 7 8
            //
            //9: temporary flow, for the thresholds

            for (int row = 0; row < nr; row++)
            {
                for (int col = 0; col < nc; col++)
                {
                    pond_d[row, col] = 0;
                    for (int it = 0; it <= 9; it++)
                    {
                        OFd[row, col, it] = 0;
                    } // reset all values
                }
            }

            int runner = 0;
            // Debug.WriteLine("df2");
            double totalwater = 0, totalwater2 = 0;
            // create overland flow in current flow map. This will be reset after flowing out, to consider a new flux of water after saddle overflow, without counting the first flux twice. 
            for (int row = 0; row < nr; row++)
            {
                for (int col = 0; col < nc; col++)
                {
                    // Add rainfall excess to every cell. If negative, it can absorb incoming water from upstream.Overland flow will only be calculated when outflow is larger than zero. 
                    // Infiltration is dealt with at the end of the day. Water can still flow into the cell from higher up
                    //If there is snowmelt, all water (including rainfall), becomes overland flow
                    if (snowmelt > 0)
                    {
                        currentflow[row, col] = P_total + snowmelt;
                        total_water_flow += P_total + snowmelt;
                    }
                    else
                    {
                        currentflow[row, col] = P_total - Ks_topsoil_m_h[row, col] * D_total; // infiltration excess becomes overland flow
                        total_water_flow += P_total - Ks_topsoil_m_h[row, col] * D_total;
                    }
                    totalwater += currentflow[row, col];
                }
            }

            List<double> dh_list = new List<double>();
            List<string> dh_list_loc = new List<string>();
            // Debug.WriteLine("df3");
            // Debug.WriteLine("df2");
            // route to neighbours
            for (runner = number_of_data_cells - 1; runner >= 0; runner--)
            {
                //Debug.WriteLine("runner start of run: " + runner);
                if (index[runner] != -9999)
                {
                    row = row_index[runner]; col = col_index[runner];

                    // if (row == 186 & col == 499 & t == 5) { minimaps(186, 499); }

                    powered_slope_sum = 0;

                    // dh_list = new List<double>();

                    if (currentflow[row, col] > 0) // if there is currently water flowing out of the cell
                    {
                        for (i = (-1); i <= 1; i++)
                        {
                            for (j = (-1); j <= 1; j++)
                            {
                                dh = 0; dhtemp = -99999.99; d_x = dx;
                                if (((row + i) >= 0) && ((row + i) < nr) && ((col + j) >= 0) && ((col + j) < nc) && !((i == 0) && (j == 0)))
                                {
                                    if (dtm[row + i, col + j] != -9999)
                                    {  //if the cell has no NODATA

                                        dh = dtm[row, col] - (dtm[row + i, col + j] + pond_d[row + i, col + j]);
                                        //Debug.WriteLine("dh = {0}", dh);
                                        if (dh > 0)
                                        {
                                            if ((row != row + i) && (col != col + j)) { d_x = dx * Math.Sqrt(2); } else { d_x = dx; }   // for non-cardinal neighbours, we use the adapted length

                                            dh = dh / d_x;
                                            dh = Math.Pow(dh, conv_fac);
                                            dh_list.Add(dh);
                                            dh_list_loc.Add(Convert.ToString(col) + "." + Convert.ToString(j));
                                            powered_slope_sum = powered_slope_sum + dh;

                                            // no correction for possible sedimentation, like in normal overland flow
                                        }
                                    }
                                }
                            }
                        }
                        // Debug.WriteLine("df3");
                        if (powered_slope_sum > 0) // not in a depression
                        {
                            try
                            {

                                int direction = 0;
                                for (i = (-1); i <= 1; i++)
                                {
                                    for (j = (-1); j <= 1; j++)
                                    {
                                        if (!((i == 0) && (j == 0))) { direction++; }
                                        if (((row + i) >= 0) && ((row + i) < nr) && ((col + j) >= 0) && ((col + j) < nc) && !((i == 0) && (j == 0)))
                                        {
                                            if (dtm[row + i, col + j] != -9999)
                                            {  //if the cell has no NODATA

                                                dh = dtm[row, col] - (dtm[row + i, col + j] + pond_d[row + i, col + j]);
                                                if (dh > 0)
                                                {
                                                    if ((row != row + i) && (col != col + j)) { d_x = dx * Math.Sqrt(2); } else { d_x = dx; }   // for non-cardinal neighbours, we use the adapted length

                                                    dh = dh / d_x;
                                                    dh = Math.Pow(dh, conv_fac);
                                                    int t2 = t;
                                                    // if (col == 12) { Debugger.Break(); }
                                                    // flow to ij = dh / powered_slope_sum
                                                    OFd[row, col, direction] += dh / powered_slope_sum * currentflow[row, col]; // update outflow to each cell
                                                    currentflow[row + i, col + j] += dh / powered_slope_sum * currentflow[row, col];// update inflow of receiving cell. Here, a negative currentflow can become less negative or positive. 
                                                    OFd[row + i, col + j, 9] += dh / powered_slope_sum * currentflow[row, col]; // to track total inflow for the water balance

                                                }
                                            }
                                        }

                                    } // end j
                                } // end i
                                OFd[row, col, 0] += currentflow[row, col]; // total outflow
                                currentflow[row, col] = 0; // reset currentflow for possible later new flux after saddle position overflow
                            }
                            catch
                            {
                                MessageBox.Show("Error in water redistribution");
                            }

                        } // end powered_slope_sum > 0

                        else // no outflow, so at the edge of the catchment, or in a sink or depression
                        {
                            bool nodataneighbour = search_nodataneighbour(row, col);

                            if (nodataneighbour == true) // outflow
                            {
                                OFd[row, col, 0] += currentflow[row, col];
                                total_outflow_y[row, col] += currentflow[row, col];
                                currentflow[row, col] = 0;
                            }
                            else // depression
                            {
                                // Debug.WriteLine("df3a");
                                List<double> saddle_OF = new List<double>();
                                saddle_OF = ponding(row, col, currentflow[row, col], qday, qmonth);
                                OFd[row, col, 0] += currentflow[row, col];
                                currentflow[row, col] = 0;
                                // Debug.WriteLine("df3b");
                                // Debug.WriteLine("col =" + col);
                                if (saddle_OF.Count() > 0) // if there is saddle overflow
                                {
                                    int OF_row = Convert.ToInt32(saddle_OF[1]);
                                    int OF_col = Convert.ToInt32(saddle_OF[2]);
                                    currentflow[OF_row, OF_col] += saddle_OF[0];
                                    // Debug.WriteLine("runner old: " + runner + ", " + saddle_OF[0] + " " + saddle_OF[1] + " " + saddle_OF[2]);
                                    string rowcol = OF_row.ToString() + "." + OF_col.ToString();
                                    runner = Array.IndexOf(rowcol_index, rowcol) + 1; // index of selected row and col + 1, because in the next iteration of the for loop, 1 is subtracted from runner
                                                                                      // Debug.WriteLine("runner new: " + runner);
                                } // end of saddle overflow
                            }
                        }

                        // Debug.WriteLine("df4");
                    } // end inflow > 0
                    else
                    {

                        // two options:
                        // currentflow is still negative, indicating that only infiltration occurs. 
                        // currentflow has been reset earlier, so nothing happens
                        // if (snowmelt > 0) { Debugger.Break(); }
                        OFd[row, col, 0] += currentflow[row, col]; //Creates negative flow, because this is used for calculating infiltration. At that stage, this parameters will be set to zero
                        currentflow[row, col] = 0;
                    }

                } // end runner !=-9999
            } // end runner
              // Debug.WriteLine("df4");

            for (int row = 0; row < nr; row++)
            {
                for (int col = 0; col < nc; col++)
                {
                    Iy[row, col] += pond_d[row, col];
                    if (double.IsNaN(Iy[row, col]))
                    {
                        Debug.WriteLine("err_df1");
                    }

                    // Calculate infiltration from rainfall and incoming flow
                    if (snowmelt == 0) // no snowmelt
                    {
                        if (OFd[row, col, 0] > 0) // if runoff occurred out of this cell, infiltration is maximum possible infiltration
                        {
                            Iy[row, col] += Ks_topsoil_m_h[row, col] * D_total;
                            if (double.IsNaN(Iy[row, col]))
                            {
                                Debug.WriteLine("err_df2");
                            }

                        }
                        else
                        { // cell not saturated, infiltration is maximum infiltration minus the deficit at row, col, which is recorded in OFd[,,0]
                            Iy[row, col] += Ks_topsoil_m_h[row, col] * D_total + OFd[row, col, 0]; //+, because OFd[row, col is negative
                                                                                                   //if (double.IsNaN(Iy[row, col])) { Debugger.Break(); }

                            OFd[row, col, 0] = 0;
                        }
                    }
                    else
                    {
                        // snow melt, no infiltration, because the soil is frozen. The rest leaves the catchment. this water only infiltrates when ponding. For the rest, it influences erosion
                    }

                    // Write daily flow to yearly flow
                    for (int it = 0; it <= 9; it++)
                    {
                        OFy_m[row, col, it] += OFd[row, col, it];
                    }

                    // check if total ponding equals total overland flow
                    pond_y[row, col] += pond_d[row, col];
                }
            }
            // Debug.WriteLine("df6");
        } // end dailyflow

        double Ks_wosten(double silt, double clay, double OM, double BD_g_cm3, int topsoil)
        {
            //https://www.sciencedirect.com/science/article/abs/pii/S0016706198001323
            // Debug.WriteLine("KsW1");
            //Debug.WriteLine("calculating Ks_Wosten with " + silt + " " + clay + " " + OM + " " + BD_g_cm3 + " " + topsoil);            
            if (OM < 0.5) { OM = 0.5; } // half percent of OM for soils where it is absent, otherwise the PTFs will crash
            double KsW = Math.Exp(7.755 + 0.03252 * silt + 0.93 * topsoil - 0.967 * BD_g_cm3 * BD_g_cm3 - 0.000484 * clay * clay - 0.000322 * silt *
                silt + 0.001 / silt - 0.0748 / OM - 0.643 * Math.Log(silt) - 0.01398 * BD_g_cm3 * clay - 0.1673 * BD_g_cm3 * OM + 0.02986 * topsoil * clay - 0.03305 * topsoil * silt) / 100;
            // Debug.WriteLine("KsW2");
            if (Double.IsNaN(KsW)) { KsW = 0; }
            // Debug.WriteLine("KsW3");
            return (KsW); // m day-1

        }

        List<double> ponding(int row1, int col1, double inflow_m, int pday, int pmonth)
        {
            ponding_start = DateTime.Now;
            pond_d[row1, col1] += inflow_m;
            bool flatwater = false;
            int rowp, colp, rowp1, colp1, pondedcells, ri_of, ci_of;
            double minponding = inflow_m, dz_water;
            List<double> elev_p = new List<double>();
            List<double> output = new List<double>();
            elev_p.Add(dtm[row1, col1]);

            // 1. initiate list of ponded rows and cols
            List<int> pondingrows = new List<int>();
            List<int> pondingcols = new List<int>();
            pondingrows.Add(row1);
            pondingcols.Add(col1);
            List<double> dh_nb;
            // Debug.WriteLine("po1");

            // 2. loop over neighbours of ponded sink, to look for lowest neighbour to share water with
            // start with lowest neighbour of ponded cells, and work the way up
            while (flatwater == false)
            {
                dh_nb = new List<double>(); // store the dhs in a list, because we don't know how big the pond gets and therefore how many neighbours there are

                for (int pondcell = 0; pondcell < pondingrows.Count(); pondcell++)
                {

                    rowp = pondingrows[pondcell];
                    colp = pondingcols[pondcell];
                    // if (minponding > pond_d[row, colp]) {minponding = pond_d[row, colp]; }
                    for (int i = -1; i <= 1; i++)
                    { // find lowest neighbour of all ponded cells.
                        for (int j = -1; j <= 1; j++)
                        {
                            if (((rowp + i) >= 0) && ((rowp + i) < nr) && ((colp + j) >= 0) && ((colp + j) < nc) && !((i == 0) && (j == 0)))
                            {
                                if (dtm[rowp + i, colp + j] != -9999)
                                {
                                    dh = (dtm[rowp, colp] + pond_d[rowp, colp]) - (dtm[rowp + i, colp + j] + pond_d[rowp + i, colp + j]);
                                    dh_nb.Add(dh);
                                } // end dtm != -9999
                            } // end if
                        } // end j
                    }  // end i
                }// end rowp colp

                //  Debug.WriteLine("po2");
                double maxdiff = dh_nb.Max();

                // 3. If there is a lower neighbour, share the water with him. Threshold a bit above 0, to avoid rounding errors
                if (maxdiff > 0.000000000001)
                {
                    // Debug.WriteLine("po3a");
                    for (int pondcell = 0; pondcell < pondingrows.Count(); pondcell++)
                    {
                        rowp = pondingrows[pondcell];
                        colp = pondingcols[pondcell];
                        double h_c = dtm[rowp, colp];
                        for (int i = -1; i <= 1; i++)
                        { // 4. relocate lowest neighbour of all ponded cells.
                            for (int j = -1; j <= 1; j++)
                            {
                                if (((rowp + i) >= 0) && ((rowp + i) < nr) && ((colp + j) >= 0) && ((colp + j) < nc) && !((i == 0) && (j == 0)))
                                {
                                    if (dtm[rowp + i, colp + j] != -9999)
                                    {
                                        // Debug.WriteLine("po3b");
                                        dh = (dtm[rowp, colp] + pond_d[rowp, colp]) - (dtm[rowp + i, colp + j] + pond_d[rowp + i, colp + j]);
                                        if (dh == dh_nb.Max()) // if the selected neighbour is the lowest neighbour
                                        {

                                            pondedcells = pondingrows.Count();
                                            dz_water = pondedcells * dh / (pondedcells + 1); // amount of water added to the lowest (waterless) neighbour

                                            // if change in ponded cells exceeds available water in shallowest ponding cell (e.g. saddle positions), water drop is limited to this depth, preventing negative ponding
                                            double h_n = dtm[rowp + i, colp + j] + pond_d[rowp + i, colp + j];
                                            if (h_n < h_c) // if we cross a saddle position, stop water flow for now

                                            {
                                                double overflow = pond_d[rowp, colp] * pondedcells; // excess water is ponding level at the saddle position, times the amount of ponding cells

                                                for (int i_of = 0; i_of < pondedcells; i_of++)
                                                {
                                                    ri_of = pondingrows[i_of];
                                                    ci_of = pondingcols[i_of];
                                                    pond_d[ri_of, ci_of] -= overflow / pondedcells;
                                                }
                                                output.Add(overflow); // create list for output, consisting of saddle r and c, and amount of overflow
                                                output.Add(rowp);
                                                output.Add(colp);
                                                //OFd[rowp, colp, 0] += overflow; // add overflow to the saddle cell, not the lower neighbour. from the saddle cell it will flow onward
                                                dz_water = 0; flatwater = true;
                                            }

                                            else
                                            {
                                                // Debug.WriteLine("po3c");
                                                for (int pondcell1 = 0; pondcell1 < pondingrows.Count(); pondcell1++)
                                                { // subtract redistributed water from original ponds
                                                    rowp1 = pondingrows[pondcell1];
                                                    colp1 = pondingcols[pondcell1];
                                                    pond_d[rowp1, colp1] -= dz_water / pondedcells;
                                                }
                                                pond_d[rowp + i, colp + j] += dz_water; // add water to the new ponding cell
                                                                                        //  Debug.WriteLine("po3d");
                                                pondingrows.Add(rowp + i);
                                                pondingcols.Add(colp + j);
                                                if (minponding > pond_d[rowp + i, colp + j]) { minponding = pond_d[rowp + i, colp + j]; }
                                                elev_p.Add(dtm[rowp + i, colp + j]);
                                            }

                                            //for(int iii = 0;iii<pondingrows.Count();iii++)
                                            //{
                                            //    Debug.Write(dtm[pondingrows[iii], pondingcols[iii]] + pond_d[pondingrows[iii], pondingcols[iii]]+" ");
                                            //}
                                            //Debug.Write("\n");
                                        }
                                    } // end dtm != -9999
                                } // end if
                            } // end j
                        }  // end i

                    } // end rowp colp
                } // end if dh_nb > 
                else
                { // no lower neighbour, end of ponding
                    flatwater = true;
                }
                // Debug.WriteLine("po3");
            } // end of water redistribution

            bool negativeponding = false;
            for (int r = 0; r < nr; r++)
            {
                for (int c = 0; c < nc; c++)
                {
                    //pond_y[r, c] += pond_d[r, c];
                    if (pond_d[r, c] < -0.00000001) { Debug.WriteLine("negative ponding on row {0} and col {1}. Amount = {2} ", r, c, pond_d[r, c]); }
                }
            }
            // if(negativeponding == true) { out_double(workdir + "\\debug\\" + run_number + "_" + t + "_ "+ pmonth+"_ " + pday + "_out_ponding.asc", pond_d); }
            ponding_t += DateTime.Now - ponding_start;

            return (output);

        }

        bool stagnation(double I_d)
        {
            // DEVELOP MM account for different moisture conditions throughout the year
            bool stag;
            bool stag_total = false;
            int lay;
            double depth;
            for (int row = 0; row < nr; row++)
            {
                for (int col = 0; col < nc; col++)
                {
                    if (dtm[row, col] != -9999)
                    {
                        stag = false;
                        lay = 0;
                        depth = 0;
                        if (I_d > Ks_md[row, col, lay]) { stag = true; stag_total = true; }
                        while (stag == false)
                        {
                            depth += layerthickness_m[row, col, lay];
                            if (I_d > Ks_md[row, col, lay]) { stag = true; stag_total = true; }
                        }
                        if (stag == true) { stagdepth[row, col] = depth; }
                    }
                }
            }
            return (stag_total);
        }

        void lateral_flow()
        {

            for (int row = 0; row < nr; row++)
            {
                for (int col = 0; col < nc; col++)
                {
                    for (int i = (-1); i <= 1; i++)
                    {
                        for (int j = (-1); j <= 1; j++)
                        {
                            if (((row + i) >= 0) && ((row + i) < nr) && ((col + j) >= 0) && ((col + j) < nc) && !((i == 0) && (j == 0)))
                            {
                                if (dtm[row + i, col + j] != -9999)
                                {  //if the cell has no NODATA

                                }
                            }
                        }
                    }
                }
            }
        }

        void update_slope_and_aspect()
        {
            double slopemax, slope, slopetot;
            for (row = 0; row < nr; row++)
            {
                for (col = 0; col < nc; col++)
                {
                    if (dtm[row, col] != -9999)
                    {
                        slopemax = 0;
                        slope = 0;
                        slopetot = 0;

                        // Do slope analysis and Aspect Calculation first
                        if ((row - 1) >= 0)
                        {
                            if (dtm[row, col] > dtm[row - 1, col] && dtm[row - 1, col] != -9999) // North 0
                            {
                                slope = (dtm[row, col] - dtm[row - 1, col]) / dx;
                                if (slope > slopemax)
                                {
                                    slopemax = slope;
                                    slopetot++;
                                    aspect[row, col] = 0 * (3.141592654 / 180);
                                }
                            }
                        }

                        if ((row - 1) >= 0 & (col + 1) < nc)
                        {
                            if (dtm[row, col] > dtm[row - 1, col + 1] && dtm[row - 1, col + 1] != -9999) // Northeast 45
                            {
                                slope = (dtm[row, col] - dtm[row - 1, col + 1]) / (dx * Math.Sqrt(2));
                                if (slope > slopemax)
                                {
                                    slopemax = slope;
                                    slopetot++;
                                    aspect[row, col] = 45 * (3.141592654 / 180);
                                }
                            }
                        }

                        if ((col + 1) < nc)
                        {
                            if (dtm[row, col] > dtm[row, col + 1] && dtm[row, col + 1] != -9999) // East 90
                            {
                                slope = (dtm[row, col] - dtm[row, col + 1]) / dx;
                                if (slope > slopemax)
                                {
                                    slopemax = slope;
                                    slopetot++;
                                    aspect[row, col] = 90 * (3.141592654 / 180);
                                }
                            }
                        }
                        if ((row + 1) < nr & (col + 1) < nc)
                        {
                            if (dtm[row, col] > dtm[row + 1, col + 1] && dtm[row + 1, col + 1] != -9999) // SouthEast 135
                            {
                                slope = (dtm[row, col] - dtm[row + 1, col + 1]) / (dx * Math.Sqrt(2));
                                if (slope > slopemax)
                                {
                                    slopemax = slope;
                                    slopetot++;
                                    aspect[row, col] = 135 * (3.141592654 / 180);
                                }

                            }
                        }

                        if ((row + 1) < nr)
                        {
                            if (dtm[row, col] > dtm[row + 1, col] && dtm[row + 1, col] != -9999) // South 180
                            {
                                slope = (dtm[row, col] - dtm[row + 1, col]) / dx;
                                if (slope > slopemax)
                                {
                                    slopemax = slope;
                                    slopetot++;
                                    aspect[row, col] = 180 * (3.141592654 / 180);
                                }
                            }
                        }
                        if ((row + 1) < nr & (col - 1) >= 0)
                        {
                            if (dtm[row, col] > dtm[row + 1, col - 1] && dtm[row + 1, col - 1] != -9999) // SouthWest 225
                            {
                                slope = (dtm[row, col] - dtm[row + 1, col - 1]) / (dx * Math.Sqrt(2));
                                if (slope > slopemax)
                                {
                                    slopemax = slope;
                                    slopetot++;
                                    aspect[row, col] = 225 * (3.141592654 / 180);
                                }
                            }
                        }

                        if ((col - 1) >= 0)
                        {
                            if (dtm[row, col] > dtm[row, col - 1] && dtm[row, col - 1] != -9999) // West 270
                            {
                                slope = (dtm[row, col] - dtm[row, col - 1]) / dx;
                                if (slope > slopemax)
                                {
                                    slopemax = slope;
                                    slopetot++;
                                    aspect[row, col] = 270;
                                }
                            }
                        }

                        if ((row - 1) >= 0 & (col - 1) >= 0)
                        {
                            if (dtm[row, col] > dtm[row - 1, col - 1] && dtm[row - 1, col - 1] != -9999) // Northwest 315
                            {
                                slope = (dtm[row, col] - dtm[row - 1, col - 1]) / (dx * Math.Sqrt(2));
                                if (slope > slopemax)
                                {
                                    slopemax = slope;
                                    slopetot++;
                                    aspect[row, col] = 315 * (3.141592654 / 180);
                                }
                            }
                        }

                        if (slope > 0) slopeAnalysis[row, col] = slopemax;// Tom's: (slope/slopetot); ?
                        else { slopeAnalysis[row, col] = 0; }

                        // Convert slope to radians
                        slopeAnalysis[row, col] = System.Math.Atan(slopeAnalysis[row, col]);

                        //// test
                        //slopeAnalysis[row, col] = 0 * Math.PI / 180;
                        //aspect[row, col] = 0 * Math.PI / 180;

                    }
                }
            }
        }
        double calc_slope_stdesc(int row_s, int col_s)
        {
            double slope_desc = 0, slope_temp = 0;
            if (dtm[row_s, col_s] != -9999)
            {
                for (i = (-1); i <= 1; i++)
                {
                    for (j = (-1); j <= 1; j++)
                    {
                        if (((row_s + i) >= 0) && ((row_s + i) < nr) && ((col_s + j) >= 0) && ((col_s + j) < nc) && !((i == 0) && (j == 0)))  //to stay within the grid and avoid the row col cell itself
                        {
                            if (dtm[row_s + i, col_s + j] != -9999) // if neighbour exists
                            {
                                if ((row_s != row_s + i) && (col_s != col_s + j)) { d_x = dx * Math.Sqrt(2); } else { d_x = dx; }
                                slope_temp = (dtm[row_s, col_s] - dtm[row_s + i, col_s + j]) / d_x;
                                if (slope_desc < slope_temp) { slope_desc = slope_temp; }
                            }
                        }
                    }
                }
            }

            slope_desc = Math.Atan(slope_desc); // slope in radians
            return (slope_desc);
        }
    }
}
