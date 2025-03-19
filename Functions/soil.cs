using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LORICA4
{
    public partial class Mother_form 
    {

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

                int n_shifts = 0;
                Decimal mass_before = total_soil_mass_kg_decimal(row2, col2);
                //Debug.WriteLine("rel2");
                for (int lay2 = 0; lay2 < max_soil_layers; lay2++)
                {
                    bool full_layer_shift = false;
                    double layer_mass = total_layer_mass_kg(row2, col2, lay2);
                    if (layer_mass < 0.000000000001) // empty layer
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
                            for (i = 0; i < 5; i++)
                            {
                                texture_kg[row2, col2, layert, i] = texture_kg[row2, col2, layert + 1, i];
                            }
                            old_SOM_kg[row2, col2, layert] = old_SOM_kg[row2, col2, layert + 1];
                            young_SOM_kg[row2, col2, layert] = young_SOM_kg[row2, col2, layert + 1];

                            if (CN_checkbox.Checked)
                            {
                                for (int cosmo = 0; cosmo < n_cosmo; cosmo++)
                                {
                                    CN_atoms_cm2[row2, col2, layert, cosmo] = CN_atoms_cm2[row2, col2, layert + 1, cosmo];
                                }
                            }

                            if (OSL_checkbox.Checked)
                            {
                                transfer_OSL_grains(row2, col2, layert + 1, row2, col2, layert, 1, 0); // transfer all grains from layert + 1 to layers
                            }

                        }
                        for (i = 0; i < 5; i++)
                        {
                            texture_kg[row2, col2, max_soil_layers - 1, i] = 0;
                        }
                        old_SOM_kg[row2, col2, max_soil_layers - 1] = 0;
                        young_SOM_kg[row2, col2, max_soil_layers - 1] = 0;
                        layerthickness_m[row2, col2, max_soil_layers - 1] = 0;

                        if (CN_checkbox.Checked)
                        {
                            for (int cosmo = 0; cosmo < n_cosmo; cosmo++)
                            {
                                CN_atoms_cm2[row2, col2, max_soil_layers - 1, cosmo] = 0;
                            }
                        }

                        if (OSL_checkbox.Checked)
                        {

                            //// OSL matrix
                            //for (int osl_i = 0; osl_i < OSL_age.GetLength(0); osl_i++) // loop over all rows
                            //{
                            //    if (OSL_age[osl_i, 0] == row_to & OSL_age[osl_i, 1] == col_to)
                            //    {
                            //        if (OSL_age[osl_i, 2] == lay_to) { Debugger.Break(); } // should not happen. This layer has been eroded completely
                            //        if (OSL_age[osl_i, 2] > lay_to)
                            //        {
                            //            OSL_age[osl_i, 2] -= 1; // MvdM no need to account here for multiple empty layer removal, because this is included by the repeated loop over layer numbers 
                            //        } // reduce layer number by 1
                            //    }
                            //}
                        }

                        if (full_layer_shift == true) { lay2--; }
                        //Debug.WriteLine("-");
                        //displaysoil(row_to, col_to);
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

        void soil_update_split_and_combine_layers_original_Lorica()
        {
            //where at the end of soil development, splitting and combining of soil layers is performed.

            //per layer: if too thin: combine with one of the two neighbours (the closest one in properties). 
            // too thick: split
            // if total too much - combine the most similar two layers although the product conforms to most restrictive rule about thicknesses
            //maat voor verschil is som (absolute differences in de vijf text classes and two organic matter classes)

            // 0-50 cm    min 2.5   insteek 5    maximum 10 cm       n=10    bovenste laag geen minimum (sediment HOEFT niet meteen weggemiddeld te worden - pas als nodig)
            // 50-200 cm  min 10    insteek 15    maximum 50 cm      n=8
            // daarna     min 40    insteek 50  geen max            n=4

            //for combining : only when matrix size exceeded, then those two layers that are most equal are combined
            //do not combine two layers that together are too thick for their position in the profile. If needed, make lowest layer thicker. Therefore, approach from above

            //MISSING: HOW DO WE GO FROM MAX TO LESS LAYERS? ART
            //  Debug.WriteLine("suscl1 ");

            //displaysoil(0,0);
            Decimal mass_before = total_catchment_mass_decimal();
            total_average_soilthickness_m = 0;
            number_soil_thicker_than = 0;
            number_soil_coarser_than = 0;
            local_soil_depth_m = 0;
            local_soil_mass_kg = 0;

            int layer;
            int numberoflayers = 0;
            double depth_m;  // keep better track of this, currently not OK yet
            try
            {
                //Debug.WriteLine("suscl2");
                //displaysoil(0, 0);
                for (int row = 0; row < nr; row++)
                {
                    for (int col = 0; col < nc; col++)
                    {
                        if (dtm[row, col] != nodata_value)
                        {
                            update_all_layer_thicknesses(row, col);
                            update_all_layer_thicknesses(row, col);

                            depth_m = 0;
                            for (layer = 0; layer < max_soil_layers; layer++)
                            {
                                ////update the layers' thickness now that textures and organic matter amounts have changed (if there is anything in the layer at all).
                                //if (!(texture_kg[row, col, layer, 0] == 0 && texture_kg[row, col, layer, 1] == 0 && texture_kg[row, col, layer, 2] == 0 && texture_kg[row, col, layer, 3] == 0 && texture_kg[row, col, layer, 4] == 0 && young_SOM_kg[row, col, layer] == 0 && old_SOM_kg[row, col, layer] == 0))
                                //{
                                //layerthickness_m[row, col, layer] = thickness_calc(row, col, layer);
                                //layerthickness_m[row, col, layer] = thickness_calc(row, col, layer);
                                //if (layerthickness_m[row,col,layer] < 0) { Debugger.Break(); } //MMS
                                //find_negative_texture_rcl(row, col, layer); //MMS
                                //}
                                if (timeseries.timeseries_soil_mass_checkbox.Checked && System.Convert.ToInt32(timeseries.timeseries_soil_cell_row.Text) == row && System.Convert.ToInt32(timeseries.timeseries_soil_cell_col.Text) == col)
                                {
                                    local_soil_mass_kg += texture_kg[row, col, layer, 0] + texture_kg[row, col, layer, 1] + texture_kg[row, col, layer, 2] + texture_kg[row, col, layer, 3] + texture_kg[row, col, layer, 4] + young_SOM_kg[row, col, layer] + old_SOM_kg[row, col, layer];
                                    if (local_soil_mass_kg < 0)
                                    {
                                        Debug.WriteLine("err_uscl1");
                                    } //MMS
                                }
                                if (timeseries.timeseries_soil_mass_checkbox.Checked && layer == 0 && texture_kg[row, col, layer, 0] / (texture_kg[row, col, layer, 0] + texture_kg[row, col, layer, 1] + texture_kg[row, col, layer, 2] + texture_kg[row, col, layer, 3] + texture_kg[row, col, layer, 4] + young_SOM_kg[row, col, layer] + old_SOM_kg[row, col, layer]) > System.Convert.ToDouble(timeseries.timeseries_soil_coarser_fraction_textbox.Text))
                                {
                                    number_soil_coarser_than++;
                                }
                            }

                        }
                    }
                }

                //displaysoil(0, 0);
                for (int row = 0; row < nr; row++)
                {
                    for (int col = 0; col < nc; col++)
                    {
                        if (dtm[row, col] != nodata_value)
                        {
                            decimal old_soil_mass = total_soil_mass_kg_decimal(row, col), new_soil_mass;
                            // Debug.WriteLine("suscl0" + row + ", " + col + ", " + t + " " + total_soil_mass(row, col));
                            //Debug.WriteLine("soil before splitting");
                            // if (row == 0 & col == 0) { displaysoil(row, col); }
                            depth_m = 0; numberoflayers = 0;
                            bool boolsplit = false;
                            bool boolcombine = false;
                            for (layer = 0; layer < max_soil_layers - 1; layer++)
                            {
                                if (layerthickness_m[row, col, layer] > 0)
                                {

                                    //Debug.WriteLine("depth is now " + depth + " for lyr " +  layer);
                                    numberoflayers++;

                                    // 0-50 cm    min 2.5   insteek 5    maximum 10 cm       n=10    bovenste laag geen minimum (sediment HOEFT niet meteen weggemiddeld te worden - pas als nodig)
                                    // 50-250 cm  min 10    insteek 25    maximum 50 cm      n=8
                                    // daarna     min 50    insteek 100  geen max            n=4

                                    if (depth_m <= 0.5)
                                    {
                                        if (layer == 0 & layerthickness_m[row, col, layer] < 0.001) // smaller than one mm -> merge with layer below
                                        {
                                            combine_layers(row, col, layer, layer + 1);
                                            update_all_layer_thicknesses(row, col);
                                            boolcombine = true;
                                            if (Math.Round(old_soil_mass, 6) != Math.Round(total_soil_mass_kg_decimal(row, col), 6)) { Debug.WriteLine("err_uscl2"); }
                                        }

                                        if (layerthickness_m[row, col, layer] < 0.025 && layer != 0)
                                        { //combine layers: select the one most like this one
                                            if (layer_difference(row, col, layer, layer - 1) > layer_difference(row, col, layer, layer + 1))
                                            {
                                                combine_layers(row, col, layer, layer + 1);
                                                update_all_layer_thicknesses(row, col);
                                                boolcombine = true;
                                                if (Math.Round(old_soil_mass, 6) != Math.Round(total_soil_mass_kg_decimal(row, col), 6))
                                                {
                                                    Debug.WriteLine("err_uscl3");
                                                }

                                            }
                                            else
                                            {
                                                combine_layers(row, col, layer - 1, layer);
                                                layer--;  //because we combined with the previous one, the current one has been replaced with one that has not yet been considered
                                                update_all_layer_thicknesses(row, col);
                                                boolcombine = true;
                                                // if (Math.Round(old_soil_mass, 6) != Math.Round(total_soil_mass(row, col), 6)) { Debugger.Break(); }

                                            }
                                            // we will now check whether layers have become too thick and if needed cut them in half
                                        }

                                        while (layerthickness_m[row, col, layer] > 0.1)
                                        { //split 
                                          // Debug.WriteLine("splitting after combining 1");
                                            split_layer_original_Lorica(row, col, layer, depth_m);
                                            update_all_layer_thicknesses(row, col);
                                            boolsplit = true;
                                            // if (Math.Abs(old_soil_mass - total_soil_mass(row, col)) > 0.00000001) { Debugger.Break(); }

                                        }

                                        // 0-50 cm    min 2.5   insteek 5    maximum 10 cm       n=10    bovenste laag geen minimum (sediment HOEFT niet meteen weggemiddeld te worden - pas als nodig)
                                        // 50-250 cm  min 10    insteek 25    maximum 50 cm      n=8
                                        // daarna     min 50    insteek 100  geen max            n=4

                                    }

                                    depth_m += layerthickness_m[row, col, layer]; // MM moved this down one if-function, to be able to split big clumps of earth by tree fall. If I put it at the end, it will give problems with splitting the one-before-last layer

                                    if (depth_m > 0.5 && depth_m <= 2)
                                    {
                                        if (layerthickness_m[row, col, layer] < 0.1 && layer != 0)
                                        { //combine 
                                            if (layer_difference(row, col, layer, layer - 1) > layer_difference(row, col, layer, layer + 1))
                                            {
                                                combine_layers(row, col, layer, layer + 1);
                                                update_all_layer_thicknesses(row, col);
                                                boolcombine = true;
                                                if (Math.Abs(old_soil_mass - total_soil_mass_kg_decimal(row, col)) > Convert.ToDecimal(0.00000001))
                                                {
                                                    Debug.WriteLine("err_uscl4");
                                                }
                                            }
                                            else
                                            {
                                                combine_layers(row, col, layer - 1, layer);
                                                layer--;  //because we combined with the previous one, the current one has been replaced with one that has not yet been considered
                                                update_all_layer_thicknesses(row, col);
                                                boolcombine = true;
                                                // if (Math.Abs(old_soil_mass - total_soil_mass(row, col)) > 0.00000001) { Debugger.Break(); }

                                            }
                                        }
                                        while (layerthickness_m[row, col, layer] > 0.5)
                                        { //split 
                                          // Debug.WriteLine("splitting after combining 2. layer = {0}, layerthickness = {1}", layer, layerthickness_m[row, col, layer]);
                                            split_layer_original_Lorica(row, col, layer, depth_m);
                                            update_all_layer_thicknesses(row, col);
                                            boolsplit = true;
                                            new_soil_mass = total_soil_mass_kg_decimal(row, col);
                                            if (Math.Abs(old_soil_mass - new_soil_mass) > Convert.ToDecimal(0.00000001))
                                            {
                                                Debug.WriteLine("err_uscl5");
                                            }

                                        }
                                    }

                                    if (depth_m > 2)
                                    {
                                        if (layerthickness_m[row, col, layer] < 0.4 && layer != 0)
                                        { //combine 
                                          //displaysoil(row, col);
                                            if (layer_difference(row, col, layer, layer - 1) > layer_difference(row, col, layer, layer + 1))
                                            {
                                                combine_layers(row, col, layer, layer + 1);
                                                update_all_layer_thicknesses(row, col);
                                                boolcombine = true;
                                                if (Math.Abs(old_soil_mass - total_soil_mass_kg_decimal(row, col)) > Convert.ToDecimal(0.00000001))
                                                {
                                                    Debug.WriteLine("err_uscl6");
                                                }

                                            }
                                            else
                                            {
                                                combine_layers(row, col, layer - 1, layer);
                                                layer--;  //because we combined with the previous one, the current one has been replaced with one that has not yet been considered
                                                numberoflayers--;
                                                update_all_layer_thicknesses(row, col);
                                                boolcombine = true;
                                                // if (Math.Abs(old_soil_mass - total_soil_mass(row, col)) > 0.00000001) { Debugger.Break(); }

                                            }

                                        }
                                        if (layerthickness_m[row, col, layer] > 0.5)
                                        { //split 
                                          // no splitting, no maximum thickness
                                        }
                                    }

                                    //Debug.WriteLine("depth is now " + depth + " and number of layers is  " + numberoflayers);
                                }
                            }
                            //Debug.WriteLine("suscl1" + row + ", " + col + ", " + t);

                            for (int layupdate = 0; layupdate < max_soil_layers; layupdate++)
                            {
                                layerthickness_m[row, col, layupdate] = thickness_calc(row, col, layupdate);
                            }
                            //Debug.WriteLine("Soil after splitting");
                            //if (row == 0 & col == 0) { displaysoil(row, col); }
                            //Debug.WriteLine("suscl4");
                            //displaysoil(0, 0);

                            new_soil_mass = total_soil_mass_kg_decimal(row, col);
                            // if (numberoflayers > max_soil_layers)
                            if (Math.Abs(old_soil_mass - new_soil_mass) > Convert.ToDecimal(0.00000001))
                            {
                                // this should never happen, because the data of the lowest layers have then been lost.
                                Debug.WriteLine(" Warning - loss of soil data ");
                                //displaysoil(row, col);
                            }
                            if (timeseries.timeseries_number_soil_thicker_checkbox.Checked && System.Convert.ToDouble(timeseries.timeseries_soil_thicker_textbox.Text) < depth_m) { number_soil_thicker_than++; }
                            if (timeseries.total_average_soilthickness_checkbox.Checked) { total_average_soilthickness_m += depth_m; }
                            if (timeseries.timeseries_soil_depth_checkbox.Checked && System.Convert.ToInt32(timeseries.timeseries_soil_cell_row.Text) == row && System.Convert.ToInt32(timeseries.timeseries_soil_cell_col.Text) == col)
                            {
                                local_soil_depth_m = depth_m;
                            }

                            //Debug.WriteLine("suscl0" + row + ", " + col + ", " + t + " " + total_soil_mass(row, col));
                            // update dtm and soil thickness map //MMS
                            double old_thickness = soildepth_m[row, col];
                            double new_thickness = total_soil_thickness(row, col);
                            dtm[row, col] += new_thickness - old_thickness;
                            soildepth_m[row, col] = new_thickness;
                            dtmchange_m[row, col] += new_thickness - old_thickness;
                            dz_soil[row, col] += new_thickness - old_thickness;
                        }// end dtm!=nodata_value

                    } // end col
                } // end row

                if (timeseries.total_average_soilthickness_checkbox.Checked) { total_average_soilthickness_m /= number_of_data_cells; }
            }
            catch { }
            Decimal mass_after = total_catchment_mass_decimal();
            // if (Math.Round(mass_before, 3) != Math.Round(mass_after, 3)) { Debugger.Break(); }
        } //always keep this code


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

                for (row = 0; row<nr; row++)
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

                                if(OSL_checkbox.Checked & layer == 0)
                                {
                                    z_layer_ref_m = bleaching_depth_m;
                                }

                                // update surface layer
                                if(layer == 0)
                                {
                                    if (layerthickness_m[row,col,layer] < 0.001)
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
        void soil_update_split_and_combine_layers_constant_thickness()
        {
            //per layer: if too thin: combine with one of the two neighbours (the closest one in properties). 
            // too thick: split
            // if total too much - combine the most similar two layers although the product conforms to most restrictive rule about thicknesses
            //maat voor verschil is som (absolute differences in de vijf text classes and two organic matter classes)

            // 0-50 cm    min 2.5   insteek 5    maximum 10 cm       n=10    bovenste laag geen minimum (sediment HOEFT niet meteen weggemiddeld te worden - pas als nodig)
            // 50-200 cm  min 10    insteek 15    maximum 50 cm      n=8
            // daarna     min 40    insteek 50  geen max            n=4

            //for combining : only when matrix size exceeded, then those two layers that are most equal are combined
            //do not combine two layers that together are too thick for their position in the profile. If needed, make lowest layer thicker. Therefore, approach from above

            //MISSING: HOW DO WE GO FROM MAX TO LESS LAYERS? ART
            // Debug.WriteLine("suscl1 ");
            if (NA_in_map(dtm) > 0 | NA_in_map(soildepth_m) > 0)
            {
                Debug.WriteLine("err_uscl7");
            }

            //displaysoil(0,0);
            Decimal mass_before = total_catchment_mass_decimal();
            total_average_soilthickness_m = 0;
            number_soil_thicker_than = 0;
            number_soil_coarser_than = 0;
            local_soil_depth_m = 0;
            local_soil_mass_kg = 0;

            int layer;
            int numberoflayers = 0;
            double depth_m;  // keep better track of this, currently not OK yet
            try
            {
                /*
                //displaysoil(0, 0);
                for (row = 0; row < nr; row++)
                {
                    for (col = 0; col < nc; col++)
                    {
                        if (dtm[row, col] != nodata_value)
                        {
                            update_all_layer_thicknesses(row, col);
                            update_all_layer_thicknesses(row, col);
                            // Debug.WriteLine("suscl2");
                            if (NA_in_soil(row, col)) { Debugger.Break(); }

                            depth_m = 0;
                            for (layer = 0; layer < max_soil_layers; layer++)
                            {
                                ////update the layers' thickness now that textures and organic matter amounts have changed (if there is anything in the layer at all).
                                //if (!(texture_kg[row, col, layer, 0] == 0 && texture_kg[row, col, layer, 1] == 0 && texture_kg[row, col, layer, 2] == 0 && texture_kg[row, col, layer, 3] == 0 && texture_kg[row, col, layer, 4] == 0 && young_SOM_kg[row, col, layer] == 0 && old_SOM_kg[row, col, layer] == 0))
                                //{
                                //layerthickness_m[row, col, layer] = thickness_calc(row, col, layer);
                                //layerthickness_m[row, col, layer] = thickness_calc(row, col, layer);
                                //if (layerthickness_m[row,col,layer] < 0) { Debugger.Break(); } //MMS
                                //find_negative_texture_rcl(row, col, layer); //MMS
                                //}
                                if (timeseries.timeseries_soil_mass_checkbox.Checked && System.Convert.ToInt32(timeseries.timeseries_soil_cell_row.Text) == row && System.Convert.ToInt32(timeseries.timeseries_soil_cell_col.Text) == col)
                                {
                                    local_soil_mass_kg += texture_kg[row, col, layer, 0] + texture_kg[row, col, layer, 1] + texture_kg[row, col, layer, 2] + texture_kg[row, col, layer, 3] + texture_kg[row, col, layer, 4] + young_SOM_kg[row, col, layer] + old_SOM_kg[row, col, layer];
                                    if (local_soil_mass_kg < 0) { Debugger.Break(); } //MMS
                                }
                                if (timeseries.timeseries_soil_mass_checkbox.Checked && layer == 0 && texture_kg[row, col, layer, 0] / (texture_kg[row, col, layer, 0] + texture_kg[row, col, layer, 1] + texture_kg[row, col, layer, 2] + texture_kg[row, col, layer, 3] + texture_kg[row, col, layer, 4] + young_SOM_kg[row, col, layer] + old_SOM_kg[row, col, layer]) > System.Convert.ToDouble(timeseries.timeseries_soil_coarser_fraction_textbox.Text))
                                {
                                    number_soil_coarser_than++;
                                }
                            }

                        }
                    }
                } */

                //displaysoil(0, 0);
                depth_m = 0;
                for (row = 0; row < nr; row++)
                {
                    for (col = 0; col < nc; col++)
                    {
                        if (dtm[row, col] != nodata_value)
                        {
                            remove_empty_layers(row, col);
                            remove_empty_layers(row, col);
                            update_all_layer_thicknesses(row, col);
                            decimal old_soil_mass_kg = total_soil_mass_kg_decimal(row, col), new_soil_mass_kg;
                            // Debug.WriteLine("suscl0" + row + ", " + col + ", " + t + " " + total_soil_mass(row, col));
                            // Debug.WriteLine("soil before splitting");
                            // if (row == 0 & col == 0) { displaysoil(row, col); }
                            depth_m = 0; numberoflayers = 0;
                            bool boolsplit = false;
                            bool boolcombine = false;
                            
                            

                            for (layer = 0; layer < (max_soil_layers - 1); layer++)
                            {
                                if (total_layer_mass_kg(row, col, layer) > 0)  //where is the else? If layers get totally removed, this should have an else ArT
                                {
                                    //Debug.WriteLine("depth is now " + depth + " for lyr " +  layer);
                                    // Debug.WriteLine("Start update split combine");
                                    numberoflayers++;
                                    if (layer == 0)
                                    {
                                        if (layerthickness_m[row, col, layer] < 0.001 | total_soil_mass_kg_decimal(row, col) < Convert.ToDecimal(0.001)) // smaller than one mm, lighter than 1 gram -> merge with layer below, to avoid numerical problems when always a fraction leaves the profile (e.g. with creep)
                                        {
                                            combine_layers(row, col, layer, layer + 1);
                                            // Debug.WriteLine("suscl2");
                                            update_all_layer_thicknesses(row, col);
                                            // Debug.WriteLine("suscl3");
                                            boolcombine = true;
                                            if (Math.Round(old_soil_mass_kg, 6) != Math.Round(total_soil_mass_kg_decimal(row, col), 6))
                                            {
                                                Debug.WriteLine("err_uscl9");
                                            }
                                            // Debug.WriteLine("suscl4");
                                        }
                                        while (layerthickness_m[row, col, layer] > layer_z_surface * (1 + tolerance)) //Higher end, split
                                        { //split 
                                            split_layer(row, col, layer, layer_z_surface);
                                            // Debug.WriteLine("splitting after combining 2");
                                            // Debug.WriteLine("d_layer {0}", layerthickness_m[row, col, layer]);
                                            update_all_layer_thicknesses(row, col);
                                            // Debug.WriteLine("splitting after combining 3");
                                            // Debug.WriteLine("d_layer {0}", layerthickness_m[row, col, layer] );
                                            boolsplit = true;
                                        }
                                    }
                                    if (layer != 0)
                                    {
                                        if (layerthickness_m[row, col, layer] < (layer_z_surface * (1 - tolerance))) // Lower end, combine
                                        {
                                            if (layer_difference(row, col, layer, layer - 1) > layer_difference(row, col, layer, layer + 1))
                                            {
                                                if (Math.Abs(old_soil_mass_kg - total_soil_mass_kg_decimal(row, col)) > Convert.ToDecimal(0.00001))
                                                {
                                                    Debug.WriteLine("err_uscl10");
                                                }
                                                combine_layers(row, col, layer, layer + 1);
                                                update_all_layer_thicknesses(row, col);
                                                boolcombine = true;
                                                if (Math.Round(old_soil_mass_kg, 6) != Math.Round(total_soil_mass_kg_decimal(row, col), 6))
                                                {
                                                    Debug.WriteLine("err_uscl11");
                                                }
                                            }
                                            else
                                            {
                                                if (Math.Abs(old_soil_mass_kg - total_soil_mass_kg_decimal(row, col)) > Convert.ToDecimal(0.00001))
                                                {
                                                    Debug.WriteLine("err_uscl12");
                                                }
                                                combine_layers(row, col, layer - 1, layer);
                                                layer--;  //because we combined with the previous one, the current one has been replaced with one that has not yet been considered
                                                update_all_layer_thicknesses(row, col);
                                                boolcombine = true;
                                                // if (Math.Round(old_soil_mass, 6) != Math.Round(total_soil_mass(row, col), 6)) { Debugger.Break(); }
                                                if (Math.Abs(old_soil_mass_kg - total_soil_mass_kg_decimal(row, col)) > Convert.ToDecimal(0.00001))
                                                {
                                                    Debug.WriteLine("err_uscl13");
                                                }

                                            }
                                        }
                                        if (Math.Abs(old_soil_mass_kg - total_soil_mass_kg_decimal(row, col)) > Convert.ToDecimal(0.00001))
                                        {
                                            Debug.WriteLine("err_uscl14");
                                        }
                                        if (NA_in_soil(row, col))
                                        {
                                            Debug.WriteLine("err_uscl15");
                                        }
                                        // MvdM changed the 'while' into an 'if' below, to prevent an infinite loop
                                        if (layerthickness_m[row, col, layer] > layer_z_surface * (1 + tolerance)) //Higher end, split
                                        { //split 
                                            // Debug.WriteLine("splitting after combining 1");
                                            split_layer(row, col, layer, layer_z_surface);
                                            update_all_layer_thicknesses(row, col);
                                            boolsplit = true;
                                            // if (Math.Abs(old_soil_mass-total_soil_mass(row, col))>0.00000001) { Debugger.Break(); }
                                        }

                                        if (Math.Abs(old_soil_mass_kg - total_soil_mass_kg_decimal(row, col)) > Convert.ToDecimal(0.00001))
                                        {
                                            Debug.WriteLine("err_uscl16");
                                        }

                                        //Debug.WriteLine("depth is now " + depth + " and number of layers is  " + numberoflayers);
                                    }
                                    if (Math.Abs(old_soil_mass_kg - total_soil_mass_kg_decimal(row, col)) > Convert.ToDecimal(0.00001))
                                    {
                                        Debug.WriteLine("err_uscl17");
                                    }
                                }
                            } // end layer

                            
                              //Debug.WriteLine("suscl1" + row + ", " + col + ", " + t);
                            if (NA_in_soil(row, col))
                            {
                                Debug.WriteLine("err_uscl18");
                            }

                            for (int layupdate = 0; layupdate < max_soil_layers; layupdate++)
                            {
                                layerthickness_m[row, col, layupdate] = thickness_calc(row, col, layupdate);
                            }
                            //Debug.WriteLine("Soil after splitting");
                            //if (row == 0 & col == 0) { displaysoil(row, col); }

                            // Debug.WriteLine("suscl4");
                            //displaysoil(0, 0);
                            if (Math.Abs(old_soil_mass_kg - total_soil_mass_kg_decimal(row, col)) > Convert.ToDecimal(0.00001))
                            {
                                Debug.WriteLine("err_uscl19");
                            }
                            new_soil_mass_kg = total_soil_mass_kg_decimal(row, col);
                            // if (numberoflayers > max_soil_layers)
                            if (Math.Abs(old_soil_mass_kg - new_soil_mass_kg) > Convert.ToDecimal(0.00001))
                            {
                                // this should never happen, because the data of the lowest layers have then been lost.
                                // Debug.WriteLine("{0}", t);
                                Debug.WriteLine(" Warning - loss of soil data ");
                                //displaysoil(row, col);
                                Debug.WriteLine("err_uscl20");

                            }
                            if (timeseries.timeseries_number_soil_thicker_checkbox.Checked && System.Convert.ToDouble(timeseries.timeseries_soil_thicker_textbox.Text) < depth_m) { number_soil_thicker_than++; }
                            if (timeseries.total_average_soilthickness_checkbox.Checked) { total_average_soilthickness_m += depth_m; }
                            if (timeseries.timeseries_soil_depth_checkbox.Checked && System.Convert.ToInt32(timeseries.timeseries_soil_cell_row.Text) == row && System.Convert.ToInt32(timeseries.timeseries_soil_cell_col.Text) == col)
                            {
                                local_soil_depth_m = depth_m;
                            }

                            //Debug.WriteLine("suscl0" + row + ", " + col + ", " + t + " " + total_soil_mass(row, col));
                            // update dtm and soil thickness map //MMS
                            update_all_layer_thicknesses(row, col);
                            double old_thickness = soildepth_m[row, col];
                            double new_thickness = total_soil_thickness(row, col);
                            dtm[row, col] += new_thickness - old_thickness;
                            soildepth_m[row, col] = new_thickness;
                            dtmchange_m[row, col] += new_thickness - old_thickness;
                            dz_soil[row, col] += new_thickness - old_thickness;
                        } // end dtm!=nodata_value
                    } // end col
                } // end row

                if (timeseries.total_average_soilthickness_checkbox.Checked) { total_average_soilthickness_m /= number_of_data_cells; }
            }
            catch
            {
                Debug.WriteLine("err_uscl21");
            }
            Decimal mass_after = total_catchment_mass_decimal();

        } // aangepast voor constante diktes
        
        void transfer_material_between_layers(int row_from, int col_from, int lay_from, int row_to, int col_to, int lay_to, double fraction_transport, bool transport_coarse = true)
        {
            double transport_betw_layers;
            // determine whether coarse material gets transported as well. Standard is yes, but some cases it doesn't happen, such as bioturbation by soil fauna
            int tex_start = 0;
            if(!transport_coarse) { tex_start = 1; }
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

        private void find_negative_texture()
        {
            for (int rr = 0; rr < nr; rr++)
            {
                for (int cc = 0; cc < nc; cc++)
                {
                    for (int ll = 0; ll < max_soil_layers; ll++)
                    {
                        for (int tt = 0; tt < 5; tt++)
                        {
                            if (texture_kg[rr, cc, ll, tt] < 0)
                            {
                                Debug.WriteLine("err_nt1");
                            }
                        }
                    }
                }
            }
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

        /*double total_soil_mass_kg_decimal(int rowmass, int colmass)
        {
            double tot_mass = 0;
            for (int lay = 0; lay < max_soil_layers; lay++)
            {
                for (int ii = 0; ii < n_texture_classes; ii++)
                {
                    tot_mass += texture_kg[rowmass, colmass, lay, ii];
                }
                tot_mass += old_SOM_kg[rowmass, colmass, lay];
                tot_mass += young_SOM_kg[rowmass, colmass, lay];
            }
            return (tot_mass);
        } */

        decimal total_soil_mass_kg_decimal(int rowmass, int colmass)
        {
            decimal tot_mass = 0;
            for (int lay = 0; lay < max_soil_layers; lay++)
            {
                for (int ii = 0; ii < n_texture_classes; ii++)
                {
                    tot_mass += Convert.ToDecimal(texture_kg[rowmass, colmass, lay, ii]);
                }
                tot_mass += Convert.ToDecimal(old_SOM_kg[rowmass, colmass, lay]);
                tot_mass += Convert.ToDecimal(young_SOM_kg[rowmass, colmass, lay]);
            }
            return (tot_mass);
        }

        double total_layer_mass_kg(int rowmass, int colmass, int laymass)
        {
            double tot_mass = 0;

            for (int ii = 0; ii < 5; ii++)
            {
                tot_mass += texture_kg[rowmass, colmass, laymass, ii];
            }
            tot_mass += old_SOM_kg[rowmass, colmass, laymass];
            tot_mass += young_SOM_kg[rowmass, colmass, laymass];

            return (tot_mass);
        }

        double total_layer_fine_earth_mass_kg(int rowmass, int colmass, int laymass)
        {
            double tot_mass = 0;

            for (int ii = 1; ii < 5; ii++)
            {
                tot_mass += texture_kg[rowmass, colmass, laymass, ii];
            }
            tot_mass += old_SOM_kg[rowmass, colmass, laymass];
            tot_mass += young_SOM_kg[rowmass, colmass, laymass];

            return (tot_mass);
        }

        double total_layer_mineral_earth_mass_kg(int rowmass, int colmass, int laymass) //MMS
        {
            double tot_mass = 0;

            for (int ii = 0; ii < 5; ii++)
            {
                tot_mass += texture_kg[rowmass, colmass, laymass, ii];
            }
            //tot_mass += old_SOM_kg[rowmass, colmass, laymass];
            //tot_mass += young_SOM_kg[rowmass, colmass, laymass];

            return (tot_mass);
        }

        double total_layer_fine_earth_om_mass_kg(int rowmass, int colmass, int laymass) //MMS
        {
            double tot_mass = 0;

            for (int ii = 1; ii < 5; ii++)
            {
                tot_mass += texture_kg[rowmass, colmass, laymass, ii];
            }
            tot_mass += old_SOM_kg[rowmass, colmass, laymass];
            tot_mass += young_SOM_kg[rowmass, colmass, laymass];

            return (tot_mass);
        }

        bool findnegativetexture()
        {
            bool neg = false;

            try
            {
                for (int row = 0; row < nr; row++)
                {
                    for (int col = 0; col < nc; col++)
                    {
                        for (int lay = 0; lay < max_soil_layers; lay++)
                        {
                            for (int tex = 0; tex < n_texture_classes; tex++)
                            {
                                if (texture_kg[row, col, lay, tex] < 0) { neg = true; }
                            }
                        }
                    }
                }
            }
            catch
            {
                Debug.WriteLine("err_nt2");

            }

            return neg;
        }

        double total_mass_in_transport()
        {
            double tot_mass = 0;
            for (int rowmass = 0; rowmass < nr; rowmass++)
            {
                for (int colmass = 0; colmass < nc; colmass++)
                {
                    for (ii = 0; ii < 5; ii++)
                    {
                        tot_mass += sediment_in_transport_kg[rowmass, colmass, ii];
                    }

                    tot_mass += old_SOM_in_transport_kg[rowmass, colmass];
                    tot_mass += young_SOM_in_transport_kg[rowmass, colmass];
                }
            }
            return (tot_mass);
        }

        double mass_in_transport_row_col(int row1, int col1)
        {
            double tot_mass = 0;
            for (ii = 0; ii < 5; ii++)
            {
                tot_mass += sediment_in_transport_kg[row1, col1, ii];
            }

            tot_mass += old_SOM_in_transport_kg[row1, col1];
            tot_mass += young_SOM_in_transport_kg[row1, col1];

            return (tot_mass);
        }

        decimal total_catchment_mass_decimal()
        {
            decimal tot_mass = 0;
            for (int rowmass = 0; rowmass < nr; rowmass++)
            {
                for (int colmass = 0; colmass < nc; colmass++)
                {
                    for (int lay = 0; lay < max_soil_layers; lay++)
                    {
                        for (ii = 0; ii < 5; ii++)
                        {
                            tot_mass += Convert.ToDecimal(texture_kg[rowmass, colmass, lay, ii]);
                        }
                        tot_mass += Convert.ToDecimal(old_SOM_kg[rowmass, colmass, lay]);
                        tot_mass += Convert.ToDecimal(young_SOM_kg[rowmass, colmass, lay]);
                    }
                }
            }

            return (tot_mass);
        }

     
        double total_catchment_elevation()
        {
            double tot_elev = 0;
            for (int rowmass = 0; rowmass < nr; rowmass++)
            {
                for (int colmass = 0; colmass < nc; colmass++)
                {
                    if (dtm[rowmass, colmass] != nodata_value)
                    {
                        tot_elev += dtm[rowmass, colmass];
                    }

                }
            }

            return (tot_elev);
        }

        double total_soil_thickness(int rowthick, int colthick)
        {
            double tot_thick = 0;
            for (int lay = 0; lay < max_soil_layers; lay++)
            {
                tot_thick += layerthickness_m[rowthick, colthick, lay];
            }
            return (tot_thick);
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
                div = 1 / (Math.Pow(layer_z_increase, 0) + Math.Pow(layer_z_increase, 1));

                if ((lay_split) == (max_soil_layers - 2)) // if we are splitting the second last layer, use reference thickness to calculate splitting ratio
                {
                    div = z_lay_ref_m / (layerthickness_m[row, col, lay_split]); // aim to split the layer at reference thickness and give the rest to the bottom layer
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


        void split_layer_original_Lorica(int rowwer, int coller, int lay1, double currentdepth) // splits layers 
        {

            try
            {
                // if(t == 7 & row == 0 & col == 41) { Debugger.Break(); }
                int totalgrains_start = 0;
                int grains_splitlayer = 0;
                int[] grains_before = new int[max_soil_layers];
                int[] grains_after = new int[max_soil_layers];
                if (OSL_checkbox.Checked)
                {
                    for (int lay_OSL = 0; lay_OSL < max_soil_layers; lay_OSL++)
                    {
                        totalgrains_start += OSL_grainages[rowwer, coller, lay_OSL].Length;
                        grains_before[lay_OSL] = OSL_grainages[rowwer, coller, lay_OSL].Length;
                    }
                    grains_splitlayer = OSL_grainages[rowwer, coller, lay1].Length;
                }

                double max_layer_difference, current_difference, maximum_allowed_thickness;
                Decimal old_mass_soil, new_mass_soil;
                old_mass_soil = total_soil_mass_kg_decimal(rowwer, coller);
                //splitting will increase the number of layers. If this splits beyond the max number of layers, then combine the two most similar ones 
                int laynum, combininglayer = -1;
                // Debug.WriteLine("sl0");
                if (diagnostic_mode == 1) { Debug.WriteLine("total soil mass = " + total_soil_mass_kg_decimal(rowwer, coller)); }
                double mass_lowest_layer = total_layer_mass_kg(rowwer, coller, max_soil_layers - 1);
                if (diagnostic_mode == 1) { Debug.WriteLine("total mass last layer = {0}", mass_lowest_layer); }
                if ((total_layer_mass_kg(rowwer, coller, max_soil_layers - 1) > 0))  // so, if we are using the lowest possible layer already:
                {
                    //this breaks now because the lowest layer can be empty due to its encountering a hard layer
                    //if they are already all in use, then the split will create one too many. We start by looking for the two most similar layers that would not create a too-thick product (do we need to do that last part?)
                    max_layer_difference = 100; //100 is a huge difference
                    for (laynum = 0; laynum < max_soil_layers - 1; laynum++)
                    {
                        current_difference = layer_difference(rowwer, coller, laynum, laynum + 1);
                        maximum_allowed_thickness = 9999;   // 9999 is a sentinel value and means infinitely thick 
                        if (currentdepth < 0.5) { maximum_allowed_thickness = 0.1; }
                        else { if (currentdepth < 2.5) { maximum_allowed_thickness = 0.5; } }

                        if (layerthickness_m[rowwer, coller, laynum] + layerthickness_m[rowwer, coller, laynum + 1] < maximum_allowed_thickness)  //if it potentially is possible to combine them 
                        {
                            if (current_difference <= max_layer_difference)   // the equal to condition means that we prefer to combine layers lower in the profile (if equally different from each other)
                            {
                                max_layer_difference = current_difference;
                                combininglayer = laynum;
                            }
                        }
                    }
                    // Debug.WriteLine("sl1");
                    //Debug.WriteLine("total soil mass = " + total_soil_mass(rowwer, coller));

                    //combine the two most-similar layers, or the lowest two if nothing else possible
                    if (max_layer_difference == 100) { combininglayer = max_soil_layers - 2; }
                    // Debug.WriteLine("sl1a");
                    if (diagnostic_mode == 1)

                        Debug.WriteLine(rowwer + "," + coller + "," + combininglayer + "," + (combininglayer + 1) + "," + t);

                    try { combine_layers(rowwer, coller, combininglayer, combininglayer + 1); }
                    catch { Debug.WriteLine(" failed to combine layers to prepare for splitting "); }
                    // Debug.WriteLine("sl1b");
                    //make sure to change lay_from if needed (because something overlying has been combined into 1 for instance).
                    if (combininglayer == lay1 || combininglayer == lay1 - 1)
                    {
                        if (diagnostic_mode == 1) { Debug.WriteLine("the layer that needed to be split has now been combined: layer {0} at t {1}", combininglayer, t); }
                        // displaysoil(rowwer, coller);
                        // Debugger.Break();
                    }
                    if (combininglayer < lay1) { lay1++; } // mvdm Should this be changed from ++ to --? because if layers above are combined, the target layer has moved up one spot
                                                           // Debug.WriteLine("sl1c");
                }

                if (Math.Abs(old_mass_soil - total_soil_mass_kg_decimal(rowwer, coller)) > Convert.ToDecimal(0.0001))
                {
                    Debug.WriteLine("err_spl_1 {0}", total_soil_mass_kg_decimal(rowwer, coller));
                    //Debugger.Break();
                }
                // Debug.WriteLine("sl2");
                //Debug.WriteLine("total soil mass = " + total_soil_mass(rowwer, coller));

                // now we can move all layers down below the one we want to split
                for (laynum = max_soil_layers - 1; laynum >= lay1 + 2; laynum--)  // we want to clear layer lay_from+1 (so we run through move-receiving layers from below up to lay_from+2). 
                //This means that layer laynum+1, into which we want to split, will be evacuated and will give its values to laynum+2;
                {
                    //transfer_material_between_layers(rowwer, coller, laynum - 1, rowwer, coller, laynum, 1);

                    for (int i = 0; i < 5; i++)
                    {
                        texture_kg[rowwer, coller, laynum, i] = texture_kg[rowwer, coller, laynum - 1, i];
                        texture_kg[rowwer, coller, laynum - 1, i] = 0;
                    }
                    // Debug.WriteLine("sl2b, ");
                    old_SOM_kg[rowwer, coller, laynum] = old_SOM_kg[rowwer, coller, laynum - 1]; old_SOM_kg[rowwer, coller, laynum - 1] = 0;
                    young_SOM_kg[rowwer, coller, laynum] = young_SOM_kg[rowwer, coller, laynum - 1]; young_SOM_kg[rowwer, coller, laynum - 1] = 0;

                    if (CN_checkbox.Checked)
                    {
                        for (int cosmo = 0; cosmo < n_cosmo; cosmo++)
                        {
                            CN_atoms_cm2[rowwer, coller, laynum, cosmo] = CN_atoms_cm2[rowwer, coller, laynum - 1, cosmo]; CN_atoms_cm2[rowwer, coller, laynum - 1, cosmo] = 0;
                        }
                    }
                    if (OSL_checkbox.Checked)
                    {
                        transfer_OSL_grains(rowwer, coller, laynum - 1, rowwer, coller, laynum, 1, 0); // move all grains from laynum - 1 to laynum
                    }
                }
                if (Math.Abs(old_mass_soil - total_soil_mass_kg_decimal(rowwer, coller)) > Convert.ToDecimal(0.00001))
                {
                    Debug.WriteLine("err_spl_2 {0}", total_soil_mass_kg_decimal(rowwer, coller));
                    // Debugger.Break();
                    // old_mass_soil = total_soil_mass(rowwer, coller);
                }

                // MvdM cleaned up the code below. I removed the if-else statement for even splitting or uneven splitting and use div to split the layers. If even splitting, div = 0.5, if not, div gets recalculated
                double div = 0.5;
                if ((lay1 + 1) == (max_soil_layers - 1)) // if one of the splitting layers is the last layer, //MvdM develop. Changed this to max_soil_layers - 1, to indicate that the deeper layer is the last layer. Therefore, a different divide 
                {
                    div = layer_z_surface / (layerthickness_m[row, col, lay1]); // aim to have the split layer at standard thickness
                }
                if (div > 1) { div = 1; }
                for (i = 0; i < 5; i++)
                {
                    texture_kg[rowwer, coller, lay1 + 1, i] = texture_kg[rowwer, coller, lay1, i] * (1 - div);
                    texture_kg[rowwer, coller, lay1, i] *= div;
                    if (double.IsNaN(texture_kg[rowwer, coller, lay1, i]))
                    {
                        Debug.WriteLine("err_spl1");
                    }
                    if (double.IsNaN(texture_kg[rowwer, coller, lay1 + 1, i]))
                    {
                        Debug.WriteLine("err_spl2");
                    }
                }
                old_SOM_kg[rowwer, coller, lay1 + 1] += old_SOM_kg[rowwer, coller, lay1] * (1 - div);
                old_SOM_kg[rowwer, coller, lay1] *= div;
                young_SOM_kg[rowwer, coller, lay1 + 1] += young_SOM_kg[rowwer, coller, lay1] * (1 - div);
                young_SOM_kg[rowwer, coller, lay1] *= div;

                if (CN_checkbox.Checked)
                {
                    for (int cosmo = 0; cosmo < n_cosmo; cosmo++)
                    {
                        double transport = CN_atoms_cm2[rowwer, coller, lay1, cosmo] * (1 - div);
                        CN_atoms_cm2[rowwer, coller, lay1 + 1, cosmo] += transport;
                        CN_atoms_cm2[rowwer, coller, lay1, cosmo] -= transport;

                    }
                }
                //Debug.WriteLine(" successfully split layer ");
                // Debug.WriteLine("sl4");
                //Debug.WriteLine("total soil mass = " + total_soil_mass(rowwer, coller));
                if (OSL_checkbox.Checked)
                {
                    // jagged array
                    transfer_OSL_grains(rowwer, coller, lay1, rowwer, coller, lay1 + 1, (1 - div), 0);
                }
                decimal new_soil_mass = total_soil_mass_kg_decimal(rowwer, coller);
                if (Math.Abs(old_mass_soil - new_soil_mass) > Convert.ToDecimal(0.00001))
                {
                    Debug.WriteLine("err_spl_3 {0}", total_soil_mass_kg_decimal(rowwer, coller));
                    //Debugger.Break();
                }

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
                    splitlayers_end = OSL_grainages[rowwer, coller, lay1].Length + OSL_grainages[rowwer, coller, lay1 + 1].Length;
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

        bool search_nodataneighbour(int row, int col)
        {
            bool ndn = false;
            for (i = (-1); i <= 1; i++)
            {
                for (j = (-1); j <= 1; j++)
                {
                    if ((((row + i) < 0) | ((row + i) >= nr)) | (((col + j) < 0) | ((col + j) >= nc)))
                    {
                        ndn = true;
                    }
                }
            }
            return (ndn);
        }

        void soil_physical_weathering()  //calculate physical weathering
        {
            decimal old_mass_kg = 0;
            old_mass_kg = total_catchment_mass_decimal();
            int cells = nr * nc;
            int layer, tex_class;
            double depth;
            try
            {
                for (int row = 0; row < nr; row++)
                {
                    for (int col = 0; col < nc; col++)
                    {
                        int tempcol = col;
                        depth = 0;
                        for (layer = 0; layer < max_soil_layers; layer++)
                        {
                            if (layerthickness_m[row, tempcol, layer] > 0)
                            {
                                int templayer = layer;
                                depth += layerthickness_m[row, tempcol, templayer] / 2;
                                for (tex_class = 0; tex_class <= 2; tex_class++)   //we only physically weather the coarse, sand and silt fractions.
                                {
                                    int tempclass = tex_class;
                                    // calculate the mass involved in physical weathering
                                    weathered_mass_kg = texture_kg[row, tempcol, templayer, tempclass] * physical_weathering_constant * Math.Exp(-Cone * depth) * -Ctwo / Math.Log10(upper_particle_size[tempclass]) * dt;
                                    total_phys_weathered_mass_kg += weathered_mass_kg;
                                    //Debug.WriteLine(" weathered mass is " + weathered_mass + " for class " + tempclass );
                                    // calculate the products involved
                                    if (tex_class == 0) //coarse fraction , boulders
                                    {
                                        texture_kg[row, tempcol, templayer, tempclass + 1] += 0.975 * weathered_mass_kg;
                                        texture_kg[row, tempcol, templayer, tempclass + 2] += 0.025 * weathered_mass_kg;
                                    }
                                    if (tex_class == 1)
                                    {
                                        texture_kg[row, tempcol, templayer, tempclass + 1] += 0.96 * weathered_mass_kg;
                                        texture_kg[row, tempcol, templayer, tempclass + 2] += 0.04 * weathered_mass_kg;
                                    }
                                    if (tex_class == 2)
                                    {
                                        texture_kg[row, tempcol, templayer, tempclass + 1] += weathered_mass_kg;
                                    }
                                    texture_kg[row, tempcol, templayer, tempclass] -= weathered_mass_kg;
                                }
                                depth += layerthickness_m[row, tempcol, templayer] / 2;
                            }
                        } //else error handling ArT
                    }  //);
                } // end for cells
                  //timeseries
                if (timeseries.timeseries_cell_waterflow_check.Checked)
                {
                    timeseries_matrix[t, timeseries_order[23]] = total_phys_weathered_mass_kg;
                }
            }
            catch { Debug.WriteLine(" Soil physical weathering calculation threw an exception"); }

            decimal new_mass_kg = total_catchment_mass_decimal();
            if (Math.Abs(old_mass_kg - new_mass_kg) > Convert.ToDecimal(0.0001))
            {
                Debug.WriteLine("err_spw1");
            }
        }

        void SPITS_soil_physical_weathering()  //calculate sedimentary rock (siltstone, limestone) physical weathering
        {
            // in this variant, coarse material (siltstone, limestone) weathers only into a silt fraction. 
            // Nothing else weathers. 
            // Not all of the coarse material weathers into silt, a certain fraction is lost to dissolution (90%).

            int cells = nr * nc;
            int layer, tex_class;
            double depth;
            try
            {
                for (row = 0; row < nr; row++)
                {
                    for (col = 0; col < nc; col++)

                    //Parallel.For(0, nc-1, col =>                    //we should paralellize over cols. Problem so far seems to be that the nc-1 or layer limit is exceeded
                    {
                        int tempcol = col;
                        //Code here is executed in parallel as much as possible for different soils in different places. 
                        //Main assumption: soils affect each other only through their surface interactions and not e.g. through throughflow
                        depth = 0;
                        for (layer = 0; layer < max_soil_layers; layer++)
                        {
                            if (layerthickness_m[row, tempcol, layer] > 0)
                            {
                                int templayer = layer;
                                depth += layerthickness_m[row, tempcol, templayer] / 2;
                                tex_class = 0;
                                int tempclass = tex_class;
                                // calculate the mass involved in physical weathering
                                weathered_mass_kg = texture_kg[row, tempcol, templayer, tempclass] * physical_weathering_constant * Math.Exp(-Cone * depth) * -Ctwo / Math.Log10(upper_particle_size[tempclass]) * dt;
                                //Debug.WriteLine(" weathered mass is " + weathered_mass + " for class " + tempclass );
                                // calculate the products involved
                                texture_kg[row, tempcol, templayer, tempclass + 2] += 0.1 * weathered_mass_kg;
                                texture_kg[row, tempcol, templayer, tempclass] -= weathered_mass_kg;
                            }
                        }
                    }  //);
                }
            }
            catch { Debug.WriteLine(" Soil physical weathering calculation threw an exception"); }

        }

        void SPITS_aeolian_deposition()
        {
            //tricks the deposition process by playing with tillage fields. Tillage shoudl be ON - but with zero par values.
            try
            {
                for (row = 0; row < nr; row++)
                {
                    for (col = 0; col < nc; col++)
                    {
                        if (tillfields[row, col] == 1)
                        {
                            //deposition in kg/m2/y is 0.063 
                            texture_kg[row, col, 0, 1] += 0.063 * dx * dx;
                        }
                    }
                }

            }
            catch { }
        }

        void soil_chemical_weathering()
        {
            int cells = nr * nc;
            int layer, tex_class;
            double depth, weathered_mass_kg, total_weath_mass, fraction_neoform;
            total_chem_weathered_mass_kg = 0;
            total_fine_neoformed_mass_kg = 0;
            for (row = 0; row < nr; row++)
            {
                for (col = 0; col < nc; col++)
                {
                    //Main assumption: soils affect each other only through their surface interactions and not e.g. through throughflow
                    depth = 0; total_weath_mass = 0;
                    for (layer = 0; layer < max_soil_layers; layer++)
                    {
                        if (layerthickness_m[row, col, layer] > 0)
                        {
                            depth += layerthickness_m[row, col, layer] / 2;
                            for (tex_class = 1; tex_class <= 4; tex_class++) // only sand, silt, clay and fine clay are chemically weathered
                            {
                                weathered_mass_kg = texture_kg[row, col, layer, tex_class] * chemical_weathering_constant / 10 * Math.Exp(-Cthree * depth) * Cfour * specific_area[tex_class] * dt;

                                if (daily_water.Checked) { weathered_mass_kg *= waterfactor[row, col]; }

                                //Debug.WriteLine(" weath mass for layer " + layer + " class " + tex_class + " is " + weathered_mass_kg + " " + Math.Exp(-Cthree * depth));
                                // note that the chem_weath constant is in kg/m2 mineral surface / y (in contrast to the original value from Salvador Blanes (mol/m2 mineral/s)
                                if (weathered_mass_kg > texture_kg[row, col, layer, tex_class]) { weathered_mass_kg = texture_kg[row, col, layer, tex_class]; }
                                total_chem_weathered_mass_kg += weathered_mass_kg;
                                texture_kg[row, col, layer, tex_class] -= weathered_mass_kg;

                                //the following code accounts for the change in average size of the weathered class, 
                                //and the fact that a fraction of it therefore falls into a finer class as well

                                if (tex_class == 1)
                                {
                                    if (texture_kg[row, col, layer, tex_class] > 0.0000156252 * weathered_mass_kg)
                                    {

                                        texture_kg[row, col, layer, tex_class] -= 0.0000156252 * weathered_mass_kg;
                                        texture_kg[row, col, layer, tex_class + 1] += 0.0000156252 * weathered_mass_kg;
                                    }
                                    else
                                    {
                                        texture_kg[row, col, layer, tex_class + 1] += texture_kg[row, col, layer, tex_class];
                                        texture_kg[row, col, layer, tex_class] = 0;
                                    }
                                }
                                if (tex_class == 2)
                                {
                                    if (texture_kg[row, col, layer, tex_class] > 0.0000640041 * weathered_mass_kg)
                                    {
                                        texture_kg[row, col, layer, tex_class] -= 0.0000640041 * weathered_mass_kg;
                                        texture_kg[row, col, layer, tex_class + 1] += 0.0000640041 * weathered_mass_kg;
                                    }
                                    else
                                    {
                                        texture_kg[row, col, layer, tex_class + 1] += texture_kg[row, col, layer, tex_class];
                                        texture_kg[row, col, layer, tex_class] = 0;
                                    }
                                }
                                if (tex_class == 3)
                                {
                                    if (texture_kg[row, col, layer, tex_class] > 0.000125 * weathered_mass_kg)
                                    {
                                        texture_kg[row, col, layer, tex_class] -= 0.000125 * weathered_mass_kg;
                                        texture_kg[row, col, layer, tex_class + 1] += 0.000125 * weathered_mass_kg;
                                    }
                                    else
                                    {
                                        texture_kg[row, col, layer, tex_class + 1] += texture_kg[row, col, layer, tex_class];
                                        texture_kg[row, col, layer, tex_class] = 0;
                                    }
                                }
                                total_weath_mass += weathered_mass_kg;  //leached amount
                            }

                            // clay neoformation
                            fraction_neoform = neoform_constant * (Math.Exp(-Cfive * depth) - Math.Exp(-Csix * depth));
                            if (fraction_neoform >= 1)
                            {
                                Debug.WriteLine(" Warning: more than 100% of leached mass wants to become fine clay. This may indicate an error. Capping at 100%");
                                fraction_neoform = 1;
                            }
                            if (daily_water.Checked) { fraction_neoform *= waterfactor[row, col]; }
                            texture_kg[row, col, layer, 4] += total_weath_mass * fraction_neoform;
                            total_fine_neoformed_mass_kg += total_weath_mass * fraction_neoform;
                            total_weath_mass -= total_weath_mass * fraction_neoform;
                            depth += layerthickness_m[row, col, layer] / 2;
                        }
                    }
                }
            }  //);
               //timeseries
            if (timeseries.total_chem_weath_checkbox.Checked)
            {
                timeseries_matrix[t, timeseries_order[24]] = total_chem_weathered_mass_kg;
            }
            if (timeseries.total_fine_formed_checkbox.Checked)
            {
                timeseries_matrix[t, timeseries_order[25]] = total_fine_neoformed_mass_kg;
            }

        }

        void soil_bioturbation()
        {
            try
            {
                //for bioturbation, we first calculate how much bioturbation (kg) this cell will experience, given its thickness
                //shallower soils do not experience the same amount as deeper soils
                //then we look at individual layers. Thicker layers, and layers closer to the surface, will experience more bioturbation kg
                //then, per layer, we will exchange the required bioturbation kg with the surrounding layers. 
                //Layers that are closer will exchange more than those that are further (regardless of whether they are deeper or closer to the surface)

                double local_bioturbation_kg, layer_bioturbation_kg, interlayer_bioturbation_kg;
                double layer_bio_activity_index, total_bio_activity_index, mass_distance_sum, mass_distance_layer;
                int layer, otherlayer;
                double fine_otherlayer_mass, fine_layer_mass;
                double total_soil_thickness_m;
                double depth, otherdepth, distance;
                total_mass_bioturbed_kg = 0;
                double[,] temp_tex_som_kg = new double[max_soil_layers, 7]; // this will hold temporary changed values of texture until all bioturbation is done
                double[] layer_0 = new double[7], layer_0_after = new double[7];
                double mass_top_before = 0, mass_top_after = 0;
                decimal mass_soil_before = 0, mass_soil_after = 0;
                // if (findnegativetexture()) { Debugger.Break(); }
                double lux_hornbeam_OM_litter_fraction = 0;
                double total_BT_transport_kgm = 0;
                double total_young_som_kg, total_old_som_kg;

                double CN_before = 0, CN_after = 0;
                //if (CN_checkbox.Checked) { CN_before = total_CNs(); }
                for (row = 0; row < nr; row++)
                {
                    for (col = 0; col < nc; col++)
                    {

                        if (dtm[row, col] != nodata_value & soildepth_m[row, col] > 0)
                        {
                            remove_empty_layers(row, col);
                            update_all_layer_thicknesses(row, col);
                            total_young_som_kg = 0; total_old_som_kg = 0;

                            mass_soil_before = total_soil_mass_kg_decimal(row, col);
                            mass_top_before = total_layer_mass_kg(row, col, 0);
                            total_soil_thickness_m = 0;
                            for (layer = 0; layer < max_soil_layers; layer++)
                            {
                                if (layer == 0)
                                {
                                    for (int tex = 0; tex < 5; tex++)
                                    {
                                        layer_0[tex] = texture_kg[row, col, 0, tex];
                                    }
                                    layer_0[5] = young_SOM_kg[row, col, 0];
                                    layer_0[6] = old_SOM_kg[row, col, 0];
                                }

                                if (total_layer_mass_kg(row, col, layer) > 0)  //this says: if the layer actually exists
                                {
                                    for (int prop = 0; prop < 5; prop++) { temp_tex_som_kg[layer, prop] = texture_kg[row, col, layer, prop]; }
                                    temp_tex_som_kg[layer, 5] = young_SOM_kg[row, col, layer];
                                    temp_tex_som_kg[layer, 6] = old_SOM_kg[row, col, layer];
                                    total_soil_thickness_m += layerthickness_m[row, col, layer];

                                    total_young_som_kg += young_SOM_kg[row, col, layer];
                                    total_old_som_kg += old_SOM_kg[row, col, layer];
                                }
                            }
                           
                            //here we calculate the first quantity: how much bioturbation kg needs to happen in this location
                            local_bioturbation_kg = potential_bt_mixing_kg_m2_y * (1 - Math.Exp(-bioturbation_depth_decay_constant * total_soil_thickness_m)) * dx * dx * dt;
                            if (local_bioturbation_kg < 0) // local_bt == 0 happens when soil is absent
                            {
                                Debug.WriteLine(" error in local_bioturbation calculation : zero mass");
                                Debug.WriteLine(" total soil thickness :" + total_soil_thickness_m + " at rc " + row + " " + col);
                                Debug.WriteLine("err_sbt1");

                            }

                            total_mass_bioturbed_kg += local_bioturbation_kg;

                            depth = 0;
                            for (layer = 0; layer < max_soil_layers; layer++)
                            {

                                if (total_layer_fine_earth_mass_kg(row, col, layer) > 0)  //this says: if the layer actually exists

                                {
                                    double dd_bt = bioturbation_depth_decay_constant * 2; // possible adjustments to second depth decay for bioturbation are possible here


                                    //double total_BT_depth_decay_index = 
                                    //    -1/dd_bt*(Math.Exp(-dd_bt*(z_toplayer - 0)) - Math.Exp(-dd_bt*(z_toplayer - z_toplayer))) +
                                    //    -1/dd_bt*(Math.Exp(-dd_bt*(total_soil_thickness_m - z_bottomlayer)) - Math.Exp(-dd_bt*(z_bottomlayer - z_bottomlayer)));                                    


                                    double check_BT_dd = 0;

                                    //integration over the exponential decay function in JGR 2006 for the entire profile, and for the current layer.
                                    //then calculate the fraction of bioturbation that will happen in this layer, and multiply with total bioturbation in this cell
                                    fine_layer_mass = total_layer_fine_earth_mass_kg(row, col, layer);

                                    layer_bioturbation_kg = soil_bioturbation_layer_activity(row, col, layer, depth, total_soil_thickness_m) * local_bioturbation_kg;
                                    mass_distance_sum = 0;

                                    depth += layerthickness_m[row, col, layer] / 2;

                                    double total_BT_depth_decay_index =
                                        -1 / dd_bt * (Math.Exp(-dd_bt * (depth - 0)) - 1) + // upper part of the curve
                                        -1 / dd_bt * (Math.Exp(-dd_bt * (total_soil_thickness_m - depth)) - 1); // lower part of the curve


                                    otherdepth = 0; distance = 0;

                                    if (layerthickness_m[row, col, layer] <= 0) { Debug.WriteLine(" error: layer thickness is 0 at t " + t + " r " + row + " c " + col); }

                                    //now that we know how much bioturbation originates in this layer,
                                    //now look at all other layers and decide which one of them exchanges how much of that good stuff.
                                    //Here we include the source layer as wel, to prevent errors and inconsistencies with different layer thicknesses
                                    var mass_distances = new List<double>();
                                    var depths = new List<double>();
                                    var depthdecays = new List<double>();
                                    var P_fromto_list = new List<double>();
                                    var P_tofrom_list = new List<double>();


                                    double check_mass_distance = 0;

                                    otherdepth = 0; distance = 0;
                                    double BT_fraction = 0;
                                    double layer_BT_depth_decay_index = 0;
                                    for (otherlayer = 0; otherlayer < max_soil_layers; otherlayer++)
                                    {
                                        double z_topotherlayer = otherdepth;
                                        double z_bottomotherlayer = otherdepth + layerthickness_m[row, col, otherlayer];

                                        if (otherlayer < layer) // above the bioturbated layer
                                        {
                                            layer_BT_depth_decay_index = -1 / dd_bt * (Math.Exp(-dd_bt * (depth - z_topotherlayer)) - Math.Exp(-dd_bt * (depth - z_bottomotherlayer)));
                                        }
                                        if (otherlayer == layer) // if layer is the same layer. Can it be excluded here and in the calculations? It should be included in calculating the depth profiles
                                        {
                                            layer_BT_depth_decay_index = -1 / dd_bt * (Math.Exp(-dd_bt * (depth - z_topotherlayer)) - 1) +
                                                -1 / dd_bt * (Math.Exp(-dd_bt * (z_bottomotherlayer - depth)) - 1);
                                        }
                                        if (otherlayer > layer) // below the bioturbated layer
                                        {
                                            layer_BT_depth_decay_index = -1 / dd_bt * (Math.Exp(-dd_bt * (z_bottomotherlayer - depth)) - Math.Exp(-dd_bt * (z_topotherlayer - depth)));
                                        }

                                        otherdepth += layerthickness_m[row, col, otherlayer] / 2; // MM moved out of if-function, otherwise distance is not calculated correctly
                                        if (total_layer_fine_earth_mass_kg(row, col, otherlayer) > 0 & layer != otherlayer)  //this says: if the other layer actually exists
                                        {
                                            depthdecays.Add(layer_BT_depth_decay_index / total_BT_depth_decay_index);
                                            depths.Add(otherdepth);

                                            check_BT_dd += layer_BT_depth_decay_index / total_BT_depth_decay_index;
                                            distance = Math.Abs(otherdepth - depth);


                                            //here we calculate the amount of material bioturbated between the current layer and the current otherlayer

                                            // interlayer_bioturbation_kg = layer_bioturbation_kg * (mass_distance_layer / mass_distance_sum);
                                            interlayer_bioturbation_kg = layer_bioturbation_kg * (layer_BT_depth_decay_index / total_BT_depth_decay_index);
                                            // check_mass_distance += mass_distance_layer / mass_distance_sum;
                                            // BT_fraction += mass_distance_layer / mass_distance_sum;

                                            fine_otherlayer_mass = texture_kg[row, col, otherlayer, 1] + texture_kg[row, col, otherlayer, 2] + texture_kg[row, col, otherlayer, 3] + texture_kg[row, col, otherlayer, 4] + young_SOM_kg[row, col, otherlayer] + old_SOM_kg[row, col, otherlayer];


                                            //weathered_mass_kg may be more than present in the other layer, the current layer, or both - in that case one or both of the layers will become mixtures of the original two layers
                                            double fromlayertomixture_kg = 0, fromotherlayertomixture_kg = 0, totalmixturemass_kg = 0, massfromlayer = 0, massfromotherlayer = 0, dmass_l, dmass_ol, cn_bt_l, cn_bt_ol;
                                            double[] mixture_kg = new double[7];
                                            fromlayertomixture_kg = Math.Min(fine_layer_mass, (interlayer_bioturbation_kg / 2));
                                            fromotherlayertomixture_kg = Math.Min(fine_otherlayer_mass, (interlayer_bioturbation_kg / 2));
                                            // totalmixturemass_kg = fromlayertomixture_kg + fromotherlayertomixture_kg;

                                            if ((fromlayertomixture_kg + fromotherlayertomixture_kg) > 1E-6)  // if there is actual exchange (which is not the case when all fine material is removed)
                                            {
                                                //now add to mixture, and take from donors
                                                double massin_l = 0, massin_ol = 0, prob_layer = 0, prob_otherlayer = 0, d_CN_l, d_CN_ol, cn_pool;
                                                double[,] texture_out_total = new double[n_texture_classes, 4];
                                                // texture
                                                for (int prop = 1; prop < 5; prop++)
                                                {
                                                    //determine how much mass can be exchanged,. Do not take more than is present in the temporary layer to prevent negative textures in the end
                                                    //Should not happen, mass of top layer should stay constant, but happens anyway
                                                    dmass_l = (fromlayertomixture_kg / fine_layer_mass) * texture_kg[row, col, layer, prop];
                                                    dmass_ol = (fromotherlayertomixture_kg / fine_otherlayer_mass) * texture_kg[row, col, otherlayer, prop];

                                                    if (dmass_l > temp_tex_som_kg[layer, prop]) { dmass_l = temp_tex_som_kg[layer, prop]; }
                                                    if (dmass_ol > temp_tex_som_kg[otherlayer, prop]) { dmass_ol = temp_tex_som_kg[otherlayer, prop]; }

                                                    total_BT_transport_kgm += (dmass_l * distance + dmass_ol * distance);
                                                    //take mass from donors to mix
                                                    mixture_kg[prop] += (dmass_l + dmass_ol);
                                                    massfromlayer += dmass_l;
                                                    massfromotherlayer += dmass_ol;

                                                    temp_tex_som_kg[layer, prop] -= dmass_l;
                                                    temp_tex_som_kg[otherlayer, prop] -= dmass_ol;

                                                    // Fill the matrix of exported and total soil material
                                                    texture_out_total[prop, 0] = dmass_l;
                                                    texture_out_total[prop, 1] = texture_kg[row, col, layer, prop];
                                                    texture_out_total[prop, 2] = dmass_ol;
                                                    texture_out_total[prop, 3] = texture_kg[row, col, otherlayer, prop];


                                                }
                                                //young OM
                                                dmass_l = (fromlayertomixture_kg / fine_layer_mass) * (young_SOM_kg[row, col, layer]);
                                                dmass_ol = (fromotherlayertomixture_kg / fine_otherlayer_mass) * (young_SOM_kg[row, col, otherlayer]);

                                                if (dmass_l > temp_tex_som_kg[layer, 5]) { dmass_l = temp_tex_som_kg[layer, 5]; }
                                                if (dmass_ol > temp_tex_som_kg[otherlayer, 5]) { dmass_ol = temp_tex_som_kg[otherlayer, 5]; }

                                                //take mass from donors to mix
                                                mixture_kg[5] += (dmass_l + dmass_ol);
                                                massfromlayer += dmass_l;
                                                massfromotherlayer += dmass_ol;

                                                temp_tex_som_kg[layer, 5] -= dmass_l;
                                                temp_tex_som_kg[otherlayer, 5] -= dmass_ol;

                                                //old OM
                                                // if (layer == 0) { Debugger.Break(); }
                                                dmass_l = (fromlayertomixture_kg / fine_layer_mass) * (old_SOM_kg[row, col, layer]);
                                                dmass_ol = (fromotherlayertomixture_kg / fine_otherlayer_mass) * (old_SOM_kg[row, col, otherlayer]);

                                                if (dmass_l > temp_tex_som_kg[layer, 6]) { dmass_l = temp_tex_som_kg[layer, 6]; }
                                                if (dmass_ol > temp_tex_som_kg[otherlayer, 6]) { dmass_ol = temp_tex_som_kg[otherlayer, 6]; }

                                                //take mass from donors to mix
                                                mixture_kg[6] += (dmass_l + dmass_ol);
                                                massfromlayer += dmass_l;
                                                massfromotherlayer += dmass_ol;

                                                temp_tex_som_kg[layer, 6] -= dmass_l;
                                                temp_tex_som_kg[otherlayer, 6] -= dmass_ol;

                                                //now give from mixture to receivers
                                                totalmixturemass_kg = massfromlayer + massfromotherlayer;

                                                // if (findnegativetexture()) { Debugger.Break(); }

                                                for (int prop = 1; prop < 7; prop++)
                                                {
                                                    temp_tex_som_kg[otherlayer, prop] += mixture_kg[prop] * (massfromotherlayer / totalmixturemass_kg);
                                                    massin_ol += mixture_kg[prop] * (massfromotherlayer / totalmixturemass_kg);
                                                    temp_tex_som_kg[layer, prop] += mixture_kg[prop] * (massfromlayer / totalmixturemass_kg);
                                                    massin_l += mixture_kg[prop] * (massfromlayer / totalmixturemass_kg);

                                                    //mixture_kg[prop] = 0;  // that's not perse needed, but feels clean

                                                }

                                                if (CN_checkbox.Checked)
                                                {
                                                    // MvdM develop: change probabilities for sand and clay fractions. Should be the same as the current code, but will look nicer
                                                    for (int cn = 0; cn < n_cosmo; cn++) // For all CNs. Mixing is independent of grain size, so all fractions get mixed evenly
                                                    {
                                                        d_CN_l = (massfromlayer / fine_layer_mass) * CN_atoms_cm2[row, col, layer, cn];
                                                        d_CN_ol = (massfromotherlayer / fine_otherlayer_mass) * CN_atoms_cm2[row, col, otherlayer, cn];
                                                        cn_pool = d_CN_l + d_CN_ol;

                                                        CN_atoms_cm2[row, col, layer, cn] += (-d_CN_l + cn_pool * massfromlayer / totalmixturemass_kg);
                                                        CN_atoms_cm2[row, col, otherlayer, cn] += (-d_CN_ol + cn_pool * massfromotherlayer / totalmixturemass_kg);
                                                        //double test = 
                                                    }

                                                }
                                                if (OSL_checkbox.Checked)
                                                {
                                                    prob_layer = texture_out_total[1, 0] / texture_out_total[1, 1]; // sand fraction leaving the layer
                                                    prob_otherlayer = texture_out_total[1, 2] / texture_out_total[1, 3]; // sand fraction leaving the other layer
                                                    transfer_OSL_grains(row, col, layer, row, col, otherlayer, prob_layer, prob_otherlayer);

                                                    P_fromto_list.Add(prob_layer);
                                                    P_tofrom_list.Add(prob_otherlayer);
                                                    if (layer == 0 & otherlayer == (max_soil_layers - 1))
                                                    {
                                                        // Debugger.Break();
                                                    }
                                                }
                                            }

                                            //all sorts of checks - we should never have values under zero, or NotANumber NaN
                                            if (temp_tex_som_kg[otherlayer, 1] < 0)
                                            {
                                                Debug.WriteLine(" texture 1 null " + t + " rc " + row + "  " + col + " otherlayers " + layer + " (" + total_layer_mass_kg(row, col, layer) + "kg) " + otherlayer + " (" + total_layer_mass_kg(row, col, otherlayer) + "kg) ");
                                            }
                                            if (temp_tex_som_kg[otherlayer, 2] < 0) { Debug.WriteLine(" texture 2 null " + t + " rc " + row + "  " + col + " otherlayers " + layer + " " + otherlayer); }
                                            if (temp_tex_som_kg[otherlayer, 3] < 0) { Debug.WriteLine(" texture 3 null " + t + " rc " + row + "  " + col + " otherlayers " + layer + " " + otherlayer); }
                                            if (temp_tex_som_kg[otherlayer, 4] < 0) { Debug.WriteLine(" texture 4 null " + t + " rc " + row + "  " + col + " otherlayers " + layer + " " + otherlayer); }
                                            if (temp_tex_som_kg[otherlayer, 5] < 0) { Debug.WriteLine(" young som null " + t + " rc " + row + "  " + col + " otherlayers " + layer + " " + otherlayer); }
                                            if (temp_tex_som_kg[otherlayer, 6] < 0) { Debug.WriteLine(" old som null " + t + " rc " + row + "  " + col + " otherlayers " + layer + " " + otherlayer); }

                                            if (temp_tex_som_kg[layer, 1] < 0) { Debug.WriteLine(" texture 1 null " + t + " rc " + row + "  " + col + " layer " + layer + " " + otherlayer); }
                                            if (temp_tex_som_kg[layer, 2] < 0) { Debug.WriteLine(" texture 2 null " + t + " rc " + row + "  " + col + " layer " + layer + " " + otherlayer); }
                                            if (temp_tex_som_kg[layer, 3] < 0) { Debug.WriteLine(" texture 3 null " + t + " rc " + row + "  " + col + " layer " + layer + " " + otherlayer); }
                                            if (temp_tex_som_kg[layer, 4] < 0) { Debug.WriteLine(" texture 4 null " + t + " rc " + row + "  " + col + " layer " + layer + " " + otherlayer); }
                                            if (temp_tex_som_kg[layer, 5] < 0) { Debug.WriteLine(" young som null " + t + " rc " + row + "  " + col + " layer " + layer + " " + otherlayer); }
                                            if (temp_tex_som_kg[layer, 6] < 0) { Debug.WriteLine(" old som null " + t + " rc " + row + "  " + col + " layer " + layer + " " + otherlayer); }

                                            if (double.IsNaN(temp_tex_som_kg[otherlayer, 1])) { Debug.WriteLine(" texture 1 NaN " + t + " rc " + row + "  " + col + " otherlayers " + layer + " " + otherlayer); }

                                            if (double.IsNaN(temp_tex_som_kg[otherlayer, 2])) { Debug.WriteLine(" texture 2 NaN " + t + " rc " + row + "  " + col + " otherlayers " + layer + " " + otherlayer); }
                                            if (double.IsNaN(temp_tex_som_kg[otherlayer, 3])) { Debug.WriteLine(" texture 3 NaN " + t + " rc " + row + "  " + col + " otherlayers " + layer + " " + otherlayer); }
                                            if (double.IsNaN(temp_tex_som_kg[otherlayer, 4])) { Debug.WriteLine(" texture 4 NaN " + t + " rc " + row + "  " + col + " otherlayers " + layer + " " + otherlayer); }
                                            if (double.IsNaN(temp_tex_som_kg[otherlayer, 5])) { Debug.WriteLine(" young som NaN " + t + " rc " + row + "  " + col + " otherlayers " + layer + " " + otherlayer); }
                                            if (double.IsNaN(temp_tex_som_kg[otherlayer, 6])) { Debug.WriteLine(" old som NaN " + t + " rc " + row + "  " + col + " otherlayers " + layer + " " + otherlayer); }

                                            if (double.IsNaN(temp_tex_som_kg[layer, 1])) { Debug.WriteLine(" texture 1 NaN " + t + " rc " + row + "  " + col + " layer " + layer + " " + otherlayer); }
                                            if (double.IsNaN(temp_tex_som_kg[layer, 2])) { Debug.WriteLine(" texture 2 NaN " + t + " rc " + row + "  " + col + " layer " + layer + " " + otherlayer); }
                                            if (double.IsNaN(temp_tex_som_kg[layer, 3])) { Debug.WriteLine(" texture 3 NaN " + t + " rc " + row + "  " + col + " layer " + layer + " " + otherlayer); }
                                            if (double.IsNaN(temp_tex_som_kg[layer, 4])) { Debug.WriteLine(" texture 4 NaN " + t + " rc " + row + "  " + col + " layer " + layer + " " + otherlayer); }
                                            if (double.IsNaN(temp_tex_som_kg[layer, 5])) { Debug.WriteLine(" young som NaN " + t + " rc " + row + "  " + col + " layer " + layer + " " + otherlayer); }
                                            if (double.IsNaN(temp_tex_som_kg[layer, 6])) { Debug.WriteLine(" old som NaN " + t + " rc " + row + "  " + col + " layer " + layer + " " + otherlayer); }
                                        }
                                        otherdepth += layerthickness_m[row, col, otherlayer] / 2; // MM added, because only half other otherdepth was added in this version
                                    }
                                    //if (Math.Round(check_mass_distance,4) != 1) { Debugger.Break(); }
                                    //if (findnegativetexture()) { Debugger.Break(); }

                                    // if (Math.Round(BT_fraction, 6) != 1) { Debugger.Break(); }
                                    depth += layerthickness_m[row, col, layer] / 2;
                                }
                            } // end for layer
                            total_BT_transport_kgm += 0;
                            // now we know the new, bioturbated amounts in every layer in this row col, let's store them in the main texture_kg variables
                            for (layer = 0; layer < max_soil_layers; layer++)
                            {
                                // if (layer == 0 & temp_tex_som_kg[0, 2] == 0)
                                // {
                                //     Debug.WriteLine("err_sbt_16a. empty top layer after BT 0: {0}, {1}, {2}, {3}, {4}, {5}, {6}. t {7}, row {8}, col {9}, dlayer {10}", layer_0[0], layer_0[1], layer_0[2], layer_0[3], layer_0[4], layer_0[5], layer_0[6], t, row, col, layerthickness_m[row, col, 0]);
                                // 
                                // }
                                for (int prop = 1; prop < 5; prop++)
                                {
                                    if (temp_tex_som_kg[layer, prop] < 0)
                                    {
                                        Debug.WriteLine("err_sbt17");
                                    }
                                    texture_kg[row, col, layer, prop] = temp_tex_som_kg[layer, prop];
                                    layer_0_after[prop] = temp_tex_som_kg[layer, prop];
                                    temp_tex_som_kg[layer, prop] = 0;
                                }
                                young_SOM_kg[row, col, layer] = temp_tex_som_kg[layer, 5];
                                old_SOM_kg[row, col, layer] = temp_tex_som_kg[layer, 6];
                                layer_0_after[5] = temp_tex_som_kg[layer, 5];
                                layer_0_after[6] = temp_tex_som_kg[layer, 6];
                                temp_tex_som_kg[layer, 5] = 0;
                                temp_tex_som_kg[layer, 6] = 0;
                            } //end for layer
                              // if (findnegativetexture()) { Debugger.Break(); }

                            mass_soil_after = total_soil_mass_kg_decimal(row, col);
                            mass_top_after = total_layer_mass_kg(row, col, 0);

                            if (Math.Abs(mass_soil_before - mass_soil_after) > Convert.ToDecimal(1E-8) | Math.Abs(mass_top_before - mass_top_after) > 1E-8)
                            {
                                Debug.WriteLine("Mass loss during bioturbation");
                                // Debugger.Break(); 
                            }

                        } // end dtm!=nodata_value
                    }// for col
                } // end for row
                  // if (findnegativetexture()) { Debugger.Break(); }

                if (timeseries.total_mass_bioturbed_checkbox.Checked)
                {
                    timeseries_matrix[t, timeseries_order[27]] = total_mass_bioturbed_kg;
                }
                if (NA_in_map(dtm) > 0 | NA_in_map(soildepth_m) > 0)
                {
                    Debug.WriteLine("err_sbt20");
                }

            }
            catch { Debug.WriteLine(" Error in bioturbation calculations in timestep {)}", t); }

        } // nieuwe code van Arnaud



        void soil_bioturbation_mounding()
        {
            try
            {
                double local_bioturbation_kg, layer_bioturbation_kg, interlayer_bioturbation_kg;
                double layer_bio_activity_index, total_bio_activity_index, mass_distance_sum, mass_distance_layer;
                int layer, otherlayer;
                double fine_otherlayer_mass, fine_layer_mass;
                double total_soil_thickness_m;
                double depth, otherdepth, distance;
                total_mass_bioturbed_kg = 0;
                double[,] temp_tex_som_kg = new double[max_soil_layers, 7]; // this will hold temporary changed values of texture until all bioturbation is done
                double[] layer_0 = new double[7], layer_0_after = new double[7];
                double mass_top_before = 0, mass_top_after = 0;
                decimal mass_soil_before = 0, mass_soil_after = 0;
                // if (findnegativetexture()) { Debugger.Break(); }
                double lux_hornbeam_OM_litter_fraction = 0;
                double total_BT_transport_kgm = 0;
                double total_young_som_kg, total_old_som_kg, bioturbated_fraction;

                double CN_before = 0, CN_after = 0;
                //if (CN_checkbox.Checked) { CN_before = total_CNs(); }
                for (row = 0; row < nr; row++)
                {
                    for (col = 0; col < nc; col++)
                    {

                        if (dtm[row, col] != nodata_value & soildepth_m[row, col] > 0)
                        {
                            remove_empty_layers(row, col);
                            update_all_layer_thicknesses(row, col);
                            total_young_som_kg = 0; total_old_som_kg = 0;

                            mass_soil_before = total_soil_mass_kg_decimal(row, col);
                            mass_top_before = total_layer_mass_kg(row, col, 0);
                            total_soil_thickness_m = total_soil_thickness(row, col);

                            //here we calculate the first quantity: how much bioturbation kg needs to happen in this location
                            local_bioturbation_kg = potential_bt_mounding_kg_m2_y * (1 - Math.Exp(-bioturbation_depth_decay_constant * total_soil_thickness_m)) * dx * dx * dt;
                            if (local_bioturbation_kg < 0) // local_bt == 0 happens when soil is absent
                            {
                                Debug.WriteLine(" error in local_bioturbation calculation : zero mass");
                                Debug.WriteLine(" total soil thickness :" + total_soil_thickness_m + " at rc " + row + " " + col);
                                Debug.WriteLine("err_sbt1");
                            }

                            total_mass_bioturbed_kg += local_bioturbation_kg;

                            depth = layerthickness_m[row, col, 0]; // start at layer 1
                            for (layer = 1; layer < max_soil_layers; layer++)
                            {

                                if (total_layer_fine_earth_mass_kg(row, col, layer) > 0)  //this says: if the layer actually exists
                                {
                                    double dd_bt = bioturbation_depth_decay_constant; // possible adjustments to second depth decay for bioturbation are possible here

                                    // determine fraction bioturbation from this layer using the function soil_bioturbation_layer_activity. Then calculate bioturbated fraction of source layer
                                    fine_layer_mass = total_layer_fine_earth_mass_kg(row, col, layer);
                                    layer_bioturbation_kg = soil_bioturbation_layer_activity(row, col, layer, depth, total_soil_thickness_m) * local_bioturbation_kg;
                                    if (layer_bioturbation_kg > fine_layer_mass) { layer_bioturbation_kg = fine_layer_mass; }

                                    if (layer_bioturbation_kg > 0)
                                    {
                                        bioturbated_fraction = layer_bioturbation_kg / fine_layer_mass;
                                        transfer_material_between_layers(row, col, layer, row, col, 0, bioturbated_fraction, false);
                                    }
                                    depth += layerthickness_m[row, col, layer];
                                }
                            } // end for layer
                            total_BT_transport_kgm += 0;
                            // now we know the new, bioturbated amounts in every layer in this row col, let's store them in the main texture_kg variables

                            mass_soil_after = total_soil_mass_kg_decimal(row, col);
                            mass_top_after = total_layer_mass_kg(row, col, 0);

                            if (Math.Abs(mass_soil_before - mass_soil_after) > Convert.ToDecimal(1E-8))
                            {
                                Debug.WriteLine("Mass loss during bioturbation");
                                // Debugger.Break(); 
                            }

                        } // end dtm!=nodata_value
                    }// for col
                } // end for row
                  // if (findnegativetexture()) { Debugger.Break(); }

                if (timeseries.total_mass_bioturbed_checkbox.Checked)
                {
                    timeseries_matrix[t, timeseries_order[19]] = total_mass_bioturbed_kg;
                }
                if (NA_in_map(dtm) > 0 | NA_in_map(soildepth_m) > 0)
                {
                    Debug.WriteLine("err_sbt20");
                }

            }
            catch { Debug.WriteLine(" Error in bioturbation calculations in timestep {0}", t); }

        } // only upward movement of particles, like ants bringing up soil material

        void soil_bioturbation_upheaval(double uph_freq, double uph_depth)
        {
            // Code from mixing by tillage. Homogenizes the soil over the mixing depth. Is used in tillage and bioturbation 

            if (t % uph_freq == 0) // Does an upheaval event occur in this simulation year?
            {
                for (row = 0; row < nr; row++)
                {
                    for (col = 0; col < nc; col++)
                    {
                        if (dtm[row, col] != -9999 & soildepth_m[row, col] > 0)
                        {
                            remove_empty_layers(row, col);
                            update_all_layer_thicknesses(row, col);

                            double mixeddepth = 0, completelayerdepth = 0, newdepth = 0;
                            int completelayers = -1;

                            while (mixeddepth <= uph_depth & completelayers < (max_soil_layers - 1))
                            {
                                completelayers++;
                                mixeddepth += layerthickness_m[row, col, completelayers];
                                // OSL_age[row, col, completelayers] = 0;

                            }// this will lead to incorporation of the (partial) layer below tillage horizon in completelayers parameter. So the highest number indicates the partial layer 
                             // Debug.WriteLine("till2");
                            double[] upheaved_text = new double[5]; // contains soil 
                            double[] upheaved_om = new double[2]; // contains OM
                            double[] alldepths = new double[completelayers + 1]; // contains thicknesses of all layers
                            double[] upheaved_mass = new double[completelayers + 1];
                            double[] fraction_mixed = new double[completelayers + 1];
                            double[] upheaved_cosmo_nuclides = new double[n_cosmo]; // contains cosmogenic nuclides

                            // take material from layers to mix
                            decimal mass_soil_before = total_soil_mass_kg_decimal(row, col);
                            double fraction_mixed_layer;
                            for (int lay = 0; lay <= completelayers; lay++) // Includes partial layer, will be selective taken up
                            {
                                if ((completelayerdepth + layerthickness_m[row, col, lay]) < uph_depth)
                                {
                                    fraction_mixed_layer = 1;
                                    fraction_mixed[lay] = 1;
                                    alldepths[lay] = layerthickness_m[row, col, lay];
                                }
                                else
                                {
                                    fraction_mixed_layer = (uph_depth - completelayerdepth) / layerthickness_m[row, col, lay];
                                    fraction_mixed[lay] = fraction_mixed_layer; // fraction of layer that is mixed
                                    alldepths[lay] = layerthickness_m[row, col, lay] * fraction_mixed_layer; // part of layer [m] that is considered
                                }
                                completelayerdepth += layerthickness_m[row, col, lay];
                                for (int tex = 0; tex < 5; tex++)
                                {
                                    upheaved_text[tex] += texture_kg[row, col, lay, tex] * fraction_mixed_layer;
                                    upheaved_mass[lay] += texture_kg[row, col, lay, tex] * fraction_mixed_layer;
                                    texture_kg[row, col, lay, tex] *= (1 - fraction_mixed_layer);

                                }
                                upheaved_om[0] += old_SOM_kg[row, col, lay] * fraction_mixed_layer;
                                upheaved_om[1] += young_SOM_kg[row, col, lay] * fraction_mixed_layer;
                                upheaved_mass[lay] += (old_SOM_kg[row, col, lay] * fraction_mixed_layer + young_SOM_kg[row, col, lay] * fraction_mixed_layer);
                                old_SOM_kg[row, col, lay] *= (1 - fraction_mixed_layer);
                                young_SOM_kg[row, col, lay] *= (1 - fraction_mixed_layer);
                                if (CN_checkbox.Checked)
                                {
                                    for (int cosmo = 0; cosmo < n_cosmo; cosmo++)
                                    {
                                        double transport = CN_atoms_cm2[row, col, lay, cosmo] * fraction_mixed_layer;
                                        upheaved_cosmo_nuclides[cosmo] += transport;
                                        CN_atoms_cm2[row, col, lay, cosmo] -= transport;
                                    }
                                }
                            }
                            // Give material back to layers, based on their given mass to mixture and total mixed mass
                            for (int lay = 0; lay <= completelayers; lay++)
                            {
                                for (int tex = 0; tex < 5; tex++)
                                {
                                    texture_kg[row, col, lay, tex] += upheaved_text[tex] * upheaved_mass[lay] / upheaved_mass.Sum();

                                }
                                old_SOM_kg[row, col, lay] += upheaved_om[0] * upheaved_mass[lay] / upheaved_mass.Sum();
                                young_SOM_kg[row, col, lay] += upheaved_om[1] * upheaved_mass[lay] / upheaved_mass.Sum();

                                if (CN_checkbox.Checked)
                                {
                                    for (int cosmo = 0; cosmo < n_cosmo; cosmo++)
                                    {
                                        CN_atoms_cm2[row, col, lay, cosmo] += upheaved_cosmo_nuclides[cosmo] * upheaved_mass[lay] / upheaved_mass.Sum();
                                    }
                                }

                                layerthickness_m[row, col, lay] = thickness_calc(row, col, lay);
                                layerthickness_m[row, col, lay] = thickness_calc(row, col, lay);
                                newdepth += layerthickness_m[row, col, lay];
                            }
                            if (OSL_checkbox.Checked) // Mix grains from the top layers
                            {
                                int totalgrains_start = 0;
                                for (int lay = 0; lay < max_soil_layers; lay++) { totalgrains_start += OSL_grainages[row, col, lay].Length; }
                                int[] grains_from_layer = new int[alldepths.Length]; // number of donor grains from each layer
                                var mixedgrains = new List<Int32>();
                                var mixedgrains_da = new List<Int32>(); // for deposition ages
                                var mixedgrains_su = new List<Int32>(); // for surfaced count

                                // add grains from complete and partial layers
                                for (int lay = 0; lay <= completelayers; lay++)
                                {
                                    var grains_staying_behind = new List<Int32>();
                                    var grains_staying_behind_da = new List<Int32>();
                                    var grains_staying_behind_su = new List<Int32>();
                                    int P_mixing = Convert.ToInt32(Math.Round(10000 * fraction_mixed[lay]));
                                    if (OSL_grainages[row, col, lay].Length > 0)
                                    {
                                        for (int ind = 0; ind < OSL_grainages[row, col, lay].Length; ind++)
                                        {
                                            if ((randOslLayerMixing.Next(0, 10000) < P_mixing ? 1 : 0) == 1)
                                            {
                                                mixedgrains.Add(OSL_grainages[row, col, lay][ind]);
                                                mixedgrains_da.Add(OSL_depositionages[row, col, lay][ind]);
                                                mixedgrains_su.Add(OSL_surfacedcount[row, col, lay][ind]);
                                                grains_from_layer[lay] += 1;
                                            }
                                            else
                                            {
                                                grains_staying_behind.Add(OSL_grainages[row, col, lay][ind]);
                                                grains_staying_behind_da.Add(OSL_depositionages[row, col, lay][ind]);
                                                grains_staying_behind_su.Add(OSL_surfacedcount[row, col, lay][ind]);
                                            }
                                        }
                                    }
                                    OSL_grainages[row, col, lay] = grains_staying_behind.ToArray(); // Preserve the grains that stay behind
                                    OSL_depositionages[row, col, lay] = grains_staying_behind_da.ToArray(); // 
                                    OSL_surfacedcount[row, col, lay] = grains_staying_behind_su.ToArray(); // 
                                }

                                // Shuffle the array
                                // make indices based on list lengths
                                int[] indices = new int[mixedgrains.ToArray().Length];
                                for (int ii = 0; ii < indices.Length; ii++) { indices[ii] = ii; }
                                indices = indices.OrderBy(x => randOslLayerMixing.Next()).ToArray();
                                int[] indices_da = new int[indices.Length];
                                int[] indices_su = new int[indices.Length];
                                for (int ii = 0; ii < indices.Length; ii++) { indices_da[ii] = indices[ii]; indices_su[ii] = indices[ii]; }

                                int[] ages_array = mixedgrains.ToArray();
                                Array.Sort(indices, ages_array);
                                mixedgrains = ages_array.ToList();

                                ages_array = mixedgrains_da.ToArray();
                                Array.Sort(indices_da, ages_array);
                                mixedgrains_da = ages_array.ToList();

                                ages_array = mixedgrains_su.ToArray();
                                Array.Sort(indices_su, ages_array);
                                mixedgrains_su = ages_array.ToList();

                                // add back random grains from grain pool
                                int count = 0;
                                for (int lay = 0; lay <= completelayers; lay++)// add grains to complete layers
                                {
                                    var newgrains = new List<Int32>();
                                    newgrains = mixedgrains.GetRange(count, grains_from_layer[lay]);
                                    newgrains.AddRange(OSL_grainages[row, col, lay]);
                                    OSL_grainages[row, col, lay] = newgrains.ToArray();
                                    newgrains = mixedgrains_da.GetRange(count, grains_from_layer[lay]);
                                    newgrains.AddRange(OSL_depositionages[row, col, lay]);
                                    OSL_depositionages[row, col, lay] = newgrains.ToArray();

                                    newgrains = mixedgrains_su.GetRange(count, grains_from_layer[lay]);
                                    newgrains.AddRange(OSL_surfacedcount[row, col, lay]);
                                    OSL_surfacedcount[row, col, lay] = newgrains.ToArray();
                                    count += grains_from_layer[lay];
                                }
                                int totalgrains_end = 0;
                                for (int lay = 0; lay < max_soil_layers; lay++) { totalgrains_end += OSL_grainages[row, col, lay].Length; }
                                if (totalgrains_start != totalgrains_end) { Debugger.Break(); }
                            }

                            decimal mass_soil_after = total_soil_mass_kg_decimal(row, col);
                            if (Math.Abs(mass_soil_before - mass_soil_after) > Convert.ToDecimal(0.0001))
                            {
                                Debug.WriteLine("err_ti2");
                            }
                        }
                    }
                }
            }
        }

        double soil_bioturbation_layer_activity(int row, int col, int layer, double depth, double total_soil_thickness_m)
        {
            double depth_upp, depth_low, layer_bio_activity_index = 0, total_bio_activity_index = 1, bio_layer_index = 0;
            try
            {
                if (bt_depth_function == 0) 
                {
                    // Exponential
                    // bioturbation_depth_decay_constant = 6;
                    layer_bio_activity_index = Math.Exp(-bioturbation_depth_decay_constant * depth) - (Math.Exp(-bioturbation_depth_decay_constant * (depth + layerthickness_m[row, col, layer])));
                    total_bio_activity_index = 1 - (Math.Exp(-bioturbation_depth_decay_constant * total_soil_thickness_m));
                }

                if (bt_depth_function == 1) 
                {
                    // Gradational
                    // bioturbation_depth_decay_constant = 1;
                    depth_upp = depth;
                    depth_low = depth + layerthickness_m[row, col, layer];
                    if (depth_upp < 1 / bioturbation_depth_decay_constant) // Is the upper part of the layer still under the mixing limit?
                    {
                        if (depth_low > (1 / bioturbation_depth_decay_constant))
                        {
                            depth_low = 1 / bioturbation_depth_decay_constant; // if the lower boundary is below the mixing limit, reset it to the mixing limit
                        }
                        layer_bio_activity_index = -bioturbation_depth_decay_constant / 2 * (Math.Pow(depth_low, 2) - Math.Pow(depth_upp, 2)) + ((depth_low) - depth_upp);
                        total_bio_activity_index = -bioturbation_depth_decay_constant / 2 * Math.Pow(1 / bioturbation_depth_decay_constant, 2) + 1 / bioturbation_depth_decay_constant;
                    }
                    else
                    { // if top of the layer is below the mixing limit, there is no BT
                        layer_bio_activity_index = 0;
                        total_bio_activity_index = 1;
                    }
                }
                
                if (bt_depth_function == 2)
                {
                    // Abrupt
                    //bioturbation_depth_decay_constant = 1;
                    depth_upp = depth;
                    depth_low = depth + layerthickness_m[row, col, layer];
                    if (depth_upp < bioturbation_depth_decay_constant) // Is the upper part of the layer still under the mixing limit?
                    {
                        if (depth_low > bioturbation_depth_decay_constant)
                        {
                            depth_low = bioturbation_depth_decay_constant; // if the lower boundary is below the mixing limit, reset it to the mixing limit
                        }
                        layer_bio_activity_index = depth_low - depth_upp;
                        total_bio_activity_index = bioturbation_depth_decay_constant;
                    }
                    else
                    { // if top of the layer is below the mixing limit, there is no BT
                        layer_bio_activity_index = 0;
                        total_bio_activity_index = 1;
                    }
                }

                bio_layer_index = (layer_bio_activity_index / total_bio_activity_index);
            }
            catch
            {
                Debug.WriteLine(" Error in  calculating bioturbation layer index in timestep {0}", t);
                Debugger.Break();
            }
            return (bio_layer_index);
        }

        void soil_litter_cycle()
        {
            // uses parameters from Carbon Cycle for now
            try
            {
                double litter_input_kg;

                //this line keeps young (hornbeam) OM completely gone from the surface every second year (reflecting that,
                //in reality, part of the year is unprotected). MvdM I added the else to reset the decomposition rate
                // if (t % 2 == 0) { potential_young_decomp_rate = 1; } else { potential_young_decomp_rate = Convert.ToDouble(carbon_y_decomp_rate_textbox.Text); }

                calculate_TPI(7);
                double a = -0.33;
                double b = 28.33;
                for (row = 0; row < nr; row++)
                {
                    for (col = 0; col < nc; col++)
                    {
                        //calculating hornbeam fraction
                        hornbeam_cover_fraction[row, col] = 1 - Math.Exp(a + b * tpi[row, col]) / (1 + Math.Exp(a + b * tpi[row, col]));

                        // Decomposition
                        litter_kg[row, col, 0] *= (1 - .97); // Hornbeam
                        litter_kg[row, col, 1] *= (1 - .45); // Beech

                        // litter_input_kg = potential_OM_input; // MvdM no changes in litter input due to soil thickness. 

                        // All litter to litter layer matrix
                        litter_kg[row, col, 0] += .230 * (hornbeam_cover_fraction[row, col]); // Hornbeam
                        litter_kg[row, col, 1] += .403 * (1 - hornbeam_cover_fraction[row, col]); // Beech

                    }
                }
            }
            catch { Debug.WriteLine(" Crash in litter cycle "); }
        }

        void soil_SOM_cycle()
        {
            try
            {
                double local_OM_input_kg, layer_OM_input_kg;
                double young_decomposition_rate, old_decomposition_rate;
                //Debug.WriteLine("succesfully read parameters for soil SOM");
                double depth;
                double total_soil_thickness;
                double layer_OM_input_index, total_OM_input_index;
                int layer;
                total_OM_input_kg = 0;
               

                if (NA_in_map(dtm) > 0 | NA_in_map(soildepth_m) > 0)
                {
                    Debug.WriteLine("err_cc1");
                }
                for (row = 0; row < nr; row++)
                {
                    //Parallel.For(0, nc, i =>                    //we parallelize over cols
                    for (col = 0; col < nc; col++)
                    {
                        // update soil thickni
                        total_soil_thickness = 0;
                        for (layer = 0; layer < max_soil_layers; layer++)
                        {
                            if (layerthickness_m[row, col, layer] > 0)
                            {
                                total_soil_thickness += layerthickness_m[row, col, layer];
                            }
                        }
                        local_OM_input_kg = potential_OM_input * (1 - Math.Exp(-OM_input_depth_decay_constant * total_soil_thickness)) * dx * dx * dt;
                        total_OM_input_kg += local_OM_input_kg;
                        depth = 0;

                        for (layer = 0; layer < max_soil_layers; layer++)
                        {
                            if (layerthickness_m[row, col, layer] > 0)
                            {
                                // if (layer == 0) { Debugger.Break(); }
                                layer_OM_input_index = -1 / OM_input_depth_decay_constant * (Math.Exp(-OM_input_depth_decay_constant * (depth + layerthickness_m[row, col, layer])) - Math.Exp(-OM_input_depth_decay_constant * depth));
                                total_OM_input_index = -1 / OM_input_depth_decay_constant * (Math.Exp(-OM_input_depth_decay_constant * (total_soil_thickness)) - 1);
                                layer_OM_input_kg = (layer_OM_input_index / total_OM_input_index) * local_OM_input_kg;

                                //decomposition gets lost as CO2 to the air (and soil water)
                                depth += layerthickness_m[row, col, layer] / 2;

                                if (som_cycle_algorithm == 0) 
                                {
                                    // standard implementation
                                    // Yoo et al. 2006: https://doi.org/10.1016/j.geoderma.2005.01.008
                                    // Minansy et al. 2008: https://doi.org/10.1016/j.geoderma.2007.12.013
                                    young_SOM_kg[row, col, layer] += layer_OM_input_kg * (1 - humification_fraction);
                                    old_SOM_kg[row, col, layer] += layer_OM_input_kg * (humification_fraction);
                                }
                                if (som_cycle_algorithm == 1)
                                {
                                    // ICBM model
                                    // Andrén & Kätterer 1997: https://doi.org/10.2307/2641210
                                    young_SOM_kg[row, col, layer] += layer_OM_input_kg;
                                    old_SOM_kg[row, col, layer] += humification_fraction * (young_SOM_kg[row, col, layer] * potential_young_decomp_rate * 1 * Math.Exp(-young_depth_decay_constant * depth));
                                }
                                if (double.IsNaN(young_SOM_kg[row, col, layer]))
                                {
                                    Debug.WriteLine("err_cc2");
                                }

                                //young_decomposition_rate = potential_young_decomp_rate * Math.Exp(-young_CTI_decay_constant * dynamic_TWI) * Math.Exp(-young_depth_decay_constant * depth);
                                //old_decomposition_rate = potential_old_decomp_rate * Math.Exp(-old_CTI_decay_constant * dynamic_TWI) * Math.Exp(-old_depth_decay_constant * depth);
                                young_decomposition_rate = potential_young_decomp_rate * 1 * Math.Exp(-young_depth_decay_constant * depth);
                                old_decomposition_rate = potential_old_decomp_rate * 1 * Math.Exp(-old_depth_decay_constant * depth);
                                young_SOM_kg[row, col, layer] *= (1 - young_decomposition_rate);
                                old_SOM_kg[row, col, layer] *= (1 - old_decomposition_rate);
                                //Debug.WriteLine(" cell  " + row + " " + col + " has layer_OM_input of " + layer_OM_input_kg);
                                depth += layerthickness_m[row, col, layer] / 2;
                                if (young_SOM_kg[row, col, layer] < 0 | old_SOM_kg[row, col, layer] < 0)
                                {
                                    Debug.WriteLine("err_cc3");
                                }
                            }

                        }
                    }

                }
                if (timeseries.total_OM_input_checkbox.Checked)
                {
                    timeseries_matrix[t, timeseries_order[28]] = total_OM_input_kg;
                }
                if (NA_in_map(dtm) > 0 | NA_in_map(soildepth_m) > 0)
                {
                    Debug.WriteLine("err_cc4");
                }

            }
            catch { Debug.WriteLine(" Crash in soil SOM cycle "); }

        }

        void soil_clay_translocation()
        {
            //possibly a function of local wetness / infiltration, but for now not/.
            double Iavg = 0, Imin = 10000000, Imax = 0;

            if (daily_water.Checked)
            {
                int Icount = 0;
                for (row = 0; row < nr; row++)
                {
                    for (col = 0; col < nc; col++)
                    {
                        if (dtm[row, col] != nodata_value)
                        {
                            if (Imin > Iy[row, col]) { Imin = Iy[row, col]; }
                            if (Imax < Iy[row, col]) { Imax = Iy[row, col]; }
                            Iavg += Iy[row, col];
                            Icount++;
                        }
                    }
                }
                Iavg /= Icount;
            }

            int layer;
            double eluviated_kg, depth, CN_transport;
            total_fine_eluviated_mass_kg = 0;
            try
            {
                for (row = 0; row < nr; row++)
                {
                    for (col = 0; col < nc; col++)
                    {

                        depth = 0;
                        for (layer = 0; layer < max_soil_layers - 1; layer++)   // we loop through all layers except the lower one - clay translocation there has no lower recipient
                        {
                            if (layerthickness_m[row, col, layer] > 0 && layerthickness_m[row, col, layer + 1] > 0)  // both source and sink layers have to exist.
                            {
                                if (texture_kg[row, col, layer, 4] > 0)
                                {
                                    depth += layerthickness_m[row, col, layer] / 2;
                                    double totalweight = texture_kg[row, col, layer, 0] + texture_kg[row, col, layer, 1] + texture_kg[row, col, layer, 2] + texture_kg[row, col, layer, 3] + texture_kg[row, col, layer, 4] + young_SOM_kg[row, col, layer] + old_SOM_kg[row, col, layer];
                                    //calculate the mass of eluviation
                                    if (CT_depth_decay_checkbox.Checked) { eluviated_kg = max_eluviation * (1 - Math.Exp(-Cclay * texture_kg[row, col, layer, 4] / totalweight)) * Math.Exp(-ct_depthdec * depth) * dt * dx * dx; }
                                    else { eluviated_kg = max_eluviation * (1 - Math.Exp(-Cclay * texture_kg[row, col, layer, 4] / totalweight)) * dt * dx * dx; }
                                    //
                                    if (daily_water.Checked)
                                    {
                                        eluviated_kg *= waterfactor[row, col];

                                    }

                                    if (eluviated_kg > texture_kg[row, col, layer, 4]) { eluviated_kg = texture_kg[row, col, layer, 4]; }

                                    total_fine_eluviated_mass_kg += eluviated_kg;
                                    texture_kg[row, col, layer, 4] -= eluviated_kg;
                                    texture_kg[row, col, layer + 1, 4] += eluviated_kg;

                                    if (CN_checkbox.Checked) // transport of meteoric Be-10 with clay fraction
                                    {
                                        CN_transport = CN_atoms_cm2[row, col, layer, 0] * (eluviated_kg / (texture_kg[row, col, layer, 3] + texture_kg[row, col, layer, 4]));
                                        CN_atoms_cm2[row, col, layer, 0] -= CN_transport;
                                        if ((layer + 1) < max_soil_layers)
                                        {
                                            if (total_layer_mass_kg(row, col, layer + 1) > 0) // if there is soil material present in the lower layer
                                            {
                                                CN_atoms_cm2[row, col, layer + 1, 0] += CN_transport;
                                            }
                                        }

                                    }

                                    //improve for lowers - where does the fine clay go?
                                    //count the amount of clay and leached chem exiting catchment
                                    // SOIL possibly improve with coarse clay fraction
                                    depth += layerthickness_m[row, col, layer] / 2;
                                }
                            }
                        }
                    }
                }
                if (timeseries.total_fine_eluviated_checkbox.Checked)
                {
                    timeseries_matrix[t, timeseries_order[25]] = total_fine_eluviated_mass_kg;
                }
            }
            catch { Debug.WriteLine(" Problem occurred in translocation calculation"); }
        }

        void soil_clay_translocation_Jagercikova()
        {
            double ct_adv0, ct_adv0_all, ct_dd, ct_dd_all;
            ct_adv0_all = Convert.ToDouble(ct_v0_Jagercikova.Text);
            ct_dd_all = Convert.ToDouble(ct_dd_Jagercikova.Text);
            ct_adv0 = ct_adv0_all;
            ct_dd = ct_dd_all;

            try
            {
                // based on the advection-diffusion equation of Jagercikova et al., 2017 https://doi.org/10.1007/s11368-016-1560-9
                // We only added the advection part, because the diffusion represents bioturbation and that is already modeled elsewhere
                double local_I;

                double depth, f_clay, f_oc, d_depth, ct_advi, eluviated_kg, CEC_ct, CCEC_ct, wdclay, CN_transport;
                for (row = 0; row < nr; row++)
                {
                    for (col = 0; col < nc; col++)
                    {
                        if (dtm[row, col] != nodata_value)
                        {
                            if (NA_in_soil(row, col))
                            {
                                Debug.WriteLine("ctj1");
                            }

                            if (daily_water.Checked)
                            {
                                local_I = Math.Max(Iy[row, col], 0);
                                ct_adv0 = ct_adv0_all * (1 - Math.Exp(-local_I / (2.0 * (1.0 / 3)))); // Exponential function to determine v0, based on infiltration. The function approaches a v0 of 1. I of 0.5~v0 of 0.5. the 2 indicates the range of the variogram. 
                                ct_dd = ct_dd_all - (1 - Math.Exp(-local_I / (2.0 * (1.0 / 3)))); // adjust depth decay, by subtracting 
                            }

                            depth = 0;
                            for (int layer = 0; layer < max_soil_layers; layer++) // we loop through all layers. Lowest layer has no recipient, so there we have free drainage of clay
                            {
                                if (layerthickness_m[row, col, layer] > 0)  // source layer has to exist. Adjusted for free drainage, receiving layer doesn't have to be present
                                {

                                    if (texture_kg[row, col, layer, 3] > 0)
                                    {
                                        depth += layerthickness_m[row, col, layer] / 2;

                                        f_clay = texture_kg[row, col, layer, 3] / (texture_kg[row, col, layer, 1] + texture_kg[row, col, layer, 2] + texture_kg[row, col, layer, 3]); // fine earth fraction of clay. No fine clay
                                        f_oc = (young_SOM_kg[row, col, layer] + old_SOM_kg[row, col, layer]) / (young_SOM_kg[row, col, layer] + old_SOM_kg[row, col, layer] + texture_kg[row, col, layer, 1] + texture_kg[row, col, layer, 2] + texture_kg[row, col, layer, 3]); // fine earth fraction of clay. No fine clay
                                        f_oc /= 1.72; // calculate from SOM to SOC: https://www.researchgate.net/post/How_can_I_convert_percent_soil_organic_matter_into_soil_C

                                        if ((layer + 1) < max_soil_layers)
                                        {
                                            d_depth = (layerthickness_m[row, col, layer] + layerthickness_m[row, col, layer + 1]) / 2; // distance from mid-point to mid-point of source and sink cell
                                        }
                                        else // eluviation from lowest layer
                                        {
                                            d_depth = (layerthickness_m[row, col, layer] + layerthickness_m[row, col, layer - 1]) / 2; // use distance to higher cell as reference
                                        }

                                        // Eluviation limited by association with OM and CEC (equations from Model 2 of Brubaker et al, 1992: estimating the water-dispersible clay content of soils)
                                        // CEC estimated with PTF from Foth and Ellis 1996, as used in Finke 2012
                                        CEC_ct = (32 + 3670 * f_oc + 196 * f_clay) / 10; // cmol+/kg
                                        CCEC_ct = CEC_ct - 300 * f_oc; // carbon corrected CEC
                                        if (f_clay == 0) { f_clay = 0.000001; } // prevent dividing by 0. clay percentage of 1% always has absent dispersible clay
                                        wdclay = (0.369 * (f_clay * 100) - 8.96 * (CCEC_ct / (f_clay * 100)) + 4.48) / 100; // fraction of clay that can be dispersed
                                        if (wdclay < 0) { wdclay = 0; } // prevent negative water-dispersible clay

                                        // if (t == 3000) { Debugger.Break(); }
                                        // advection
                                        ct_advi = ct_adv0 * Math.Exp(-ct_dd * depth);
                                        eluviated_kg = ct_advi / 100 * bulkdensity[row, col, layer] * f_clay * dx * dx;
                                        // eluviated_kg = 1 / d_depth * (ct_advi * f_clay) * 1000 / bulkdensity[row, col, layer];

                                        if (eluviated_kg > (texture_kg[row, col, layer, 3] * wdclay))
                                        {
                                            eluviated_kg = texture_kg[row, col, layer, 3] * wdclay;
                                        }

                                        if (eluviated_kg > texture_kg[row, col, layer, 3]) { eluviated_kg = texture_kg[row, col, layer, 3]; } // correct for too muchy clay eluviating, not necessary anymore due to limitation water-dispersible clay

                                        total_fine_eluviated_mass_kg += eluviated_kg;
                                        texture_kg[row, col, layer, 3] -= eluviated_kg;
                                        if ((layer + 1) < max_soil_layers) // in case there is a lower receiving layer
                                        {
                                            if (total_layer_mass_kg(row, col, layer + 1) > 0) // if there is soil material present in the lower layer
                                            {
                                                texture_kg[row, col, layer + 1, 3] += eluviated_kg;
                                            }
                                        }

                                        if (CN_checkbox.Checked) // transport of meteoric Be-10 (index 0) and Cs-137 (index 3) with clay fraction
                                        {

                                            CN_transport = CN_atoms_cm2[row, col, layer, 0] * (eluviated_kg / texture_kg[row, col, layer, 3]);
                                            CN_atoms_cm2[row, col, layer, 0] -= CN_transport;
                                            if ((layer + 1) < max_soil_layers)
                                            {
                                                if (total_layer_mass_kg(row, col, layer + 1) > 0) // if there is soil material present in the lower layer
                                                {
                                                    CN_atoms_cm2[row, col, layer + 1, 0] += CN_transport;
                                                }
                                            }
                                        }
                                        depth += layerthickness_m[row, col, layer] / 2;

                                    }

                                    if (NA_in_soil(row, col))
                                    {
                                        Debug.WriteLine("err_ctj2");
                                    }

                                }
                            }
                        }
                    }
                    if (timeseries.total_fine_eluviated_checkbox.Checked)
                    {
                        timeseries_matrix[t, timeseries_order[26]] = total_fine_eluviated_mass_kg;
                    }
                }
                if (NA_in_map(dtm) > 0 | NA_in_map(soildepth_m) > 0)
                {
                    Debug.WriteLine("err_ctj3");
                }

            }
            catch { Debug.WriteLine(" Problem occurred in translocation calculation"); }
        }

        void soil_silt_translocation()
        {
            //in Spitsbergen, it is mostly silt (with attendant clay) that gets translocated in the profile. Clay is not modelled in itself

            int layer;
            double eluviated_kg;
            try
            {
                for (row = 0; row < nr; row++)
                {
                    for (col = 0; col < nc; col++)
                    {
                        for (layer = 0; layer < max_soil_layers - 1; layer++)   // we loop through all layers except the lower one - clay translocation there has no lower recipient
                        {
                            if (layerthickness_m[row, col, layer] > 0 && layerthickness_m[row, col, layer + 1] > 0)  // both source and sink layers have to exist.
                            {
                                if (texture_kg[row, col, layer, 2] > 0)
                                {
                                    //calculate the mass of eluviation
                                    eluviated_kg = max_eluviation * (1 - Math.Exp(-Cclay * texture_kg[row, col, layer, 2])) * dt * dx * dx;
                                    texture_kg[row, col, layer, 2] -= eluviated_kg;
                                    if (texture_kg[row, col, layer, 2] < 0) { Debug.WriteLine("error: too much clay eluviation "); }
                                    texture_kg[row, col, layer + 1, 2] += eluviated_kg;
                                    //improve for lowers
                                    //count the amount of clay and leached chem exiting catchment
                                    // SOIL possibly improve with coarse clay fraction
                                }
                            }
                        }
                    }
                }
            }
            catch { Debug.WriteLine(" Problem occurred in translocation calculation"); }
        }

        void soil_decalcification()
        {
            // develop: erosion of carbonates, link to clay fraction? Or transport CO3_kg with the rest of the sediments?

            // Decalcification depends on the amount of percolation, according to Egli and Fitze (2001). The function below is a linear regression between the data in their paper. This function should work as a simple test. more complicated functions, with equilibria and secondary carbonates are possible
            try
            {
                double CO3_loss;
                // Carbonate losses [mol m-2 y-1] = 205.58 * percolation [m] - 12.392
                // Infiltration / percolation is modeled in m, so adjustments have to be made for cell size. In every step? 
                for (row = 0; row < nr; row++)
                {
                    for (col = 0; col < nc; col++)
                    {
                        if (dtm[row, col] != nodata_value)
                        {
                            CO3_loss = (205.58 * Iy[row, col] - 12.392) * (dx * dx) * 60.01; // Corrected the equation for cell size (dx*dx) and molar mass (60.01 g mol-1)
                            if (CO3_loss < 0) { CO3_loss = 0; }

                            int layer = 0;
                            while (CO3_loss > 0)
                            {
                                //Debug.WriteLine("dec1");
                                if (CO3_kg[row, col, layer] > 0)
                                {
                                    //Debug.WriteLine("dec1a");
                                    if (CO3_kg[row, col, layer] >= CO3_loss)
                                    {
                                        //Debug.WriteLine("dec2");
                                        CO3_kg[row, col, layer] -= CO3_loss;
                                        CO3_loss = 0;
                                    }
                                    else
                                    {
                                        //Debug.WriteLine("dec3");
                                        CO3_loss -= CO3_kg[row, col, layer];
                                        CO3_kg[row, col, layer] = 0;
                                        if (layer < (max_soil_layers - 1)) { layer++; }
                                        else { CO3_loss = 0; }// all CO3 is removed from the catchment
                                    }
                                    //Debug.WriteLine("dec4");
                                }
                                else
                                {
                                    //Debug.WriteLine("dec5");
                                    if (layer < (max_soil_layers - 1)) { layer++; }
                                    else { CO3_loss = 0; }// all CO3 is removed from the catchment
                                    ;
                                }
                            }
                            //Debug.WriteLine("layer decalc: {0}", layer);
                        }
                    }
                }
            }
            catch
            {
                MessageBox.Show("error in decalcification");
            }
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
    }
}
