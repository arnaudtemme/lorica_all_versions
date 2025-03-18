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

        #region Geochronology code
        Random randOslRandomLayer = new Random(123);
        Random randOslLayerMixing = new Random(123);

        int returnRandomLayer(double[] probabilities)
        {
            for (int i = 1; i < probabilities.Length; i++) { probabilities[i] = probabilities[i] + probabilities[i - 1]; } // cumulate and
            for (int i = 0; i < probabilities.Length; i++) { probabilities[i] /= probabilities[probabilities.Length - 1]; } // normalize probabilities

            double prob = randOslRandomLayer.Next(0, 1000) / 1000.0; // determine probability

            for (int i = 0; i < probabilities.Length; i++)
            {
                if (prob <= probabilities[i])
                {
                    return (i);
                }
            }
            return (9999); // this cannot happen. Probability is always <= 1, which is the last value in [probabilities]
        }

        void update_and_bleach_OSL_ages()
        {
            int P_bleaching_int;
            double sep_fraction = 0;
            for (row = 0; row < nr; row++)
            {
                for (col = 0; col < nc; col++)
                {
                    update_all_layer_thicknesses(row, col);
                    //Debug.WriteLine("uboa1");
                    double depth = 0;
                    double bleached_depth_m = bleaching_depth_m;

                    // split or merge top layer, so that it gets the thickness of the bleaching depth. this is to avoid deep-reaching of bleached grains, when the layer is thicker
                    if (layerthickness_m[row, col, 0] > bleaching_depth_m) // give part to the layer below
                    {
                        sep_fraction = (layerthickness_m[row, col, 0] - bleaching_depth_m) / layerthickness_m[row, col, 0];
                        transfer_material_between_layers(row, col, 0, row, col, 1, sep_fraction);
                    }
                    if (layerthickness_m[row, col, 0] < bleaching_depth_m) // get part of the layer below
                    {
                        sep_fraction = (bleaching_depth_m - layerthickness_m[row, col, 0]) / layerthickness_m[row, col, 1];
                        transfer_material_between_layers(row, col, 1, row, col, 0, sep_fraction);
                    }

                    for (int layer = 0; layer < max_soil_layers; layer++)
                    {
                        //Debug.WriteLine("uboa2");
                        double laythick = layerthickness_m[row, col, layer];
                        double P_bleaching = 0;
                        if (laythick > 0 & bleached_depth_m > 0) { P_bleaching = bleached_depth_m / laythick; } // determine part of layer that is within bleaching range
                        if (P_bleaching > 1)
                        {
                            P_bleaching = 1;
                        } // set to 1 if bleaching range is larger than layer thickness
                        P_bleaching_int = Convert.ToInt32(Math.Round(100000000 * P_bleaching));
                        //Debug.WriteLine("uboa3");
                        if (OSL_grainages[row, col, layer].Length > 0)
                        {
                            for (int ind = 0; ind < OSL_grainages[row, col, layer].Length; ind++)
                            {
                                // add a year to all grains
                                OSL_grainages[row, col, layer][ind] += 1;
                                OSL_depositionages[row, col, layer][ind] += 1;

                                //Debug.WriteLine("uboa4");
                                if (bleached_depth_m > 0)
                                {
                                    // Chance of bleaching                          
                                    if ((randOslLayerMixing.Next(0, 100000000) < P_bleaching_int ? 1 : 0) == 1) // if grain is exposed to daylight
                                    {
                                        OSL_grainages[row, col, layer][ind] = 0; // bleach grain
                                        OSL_surfacedcount[row, col, layer][ind] += 1; // add 1 to the number of times the grain has surfaced and has been bleached

                                    }
                                }
                                //Debug.WriteLine("uboa5");
                            }
                        }
                        bleached_depth_m -= laythick; // subtract layer thickness from bleached depth
                        depth = depth + layerthickness_m[row, col, layer];
                        //Debug.WriteLine("uboa6");
                    }
                }
            }
        }

        void transfer_OSL_grains(int fromrow, int fromcol, int fromlay, int torow, int tocol, int tolay, double P_fromto, double P_tofrom = 0, bool bleaching_by_transport = false)
        {
            // if (tocol == 41 & tolay == 0) { Debugger.Break(); }
            OSL_JA_start = DateTime.Now;
            int[] ages_from = OSL_grainages[fromrow, fromcol, fromlay];
            int[] ages_to = OSL_grainages[torow, tocol, tolay];
            int[] ages_from_da = OSL_depositionages[fromrow, fromcol, fromlay];
            int[] ages_to_da = OSL_depositionages[torow, tocol, tolay];
            int[] ages_from_su = OSL_surfacedcount[fromrow, fromcol, fromlay];
            int[] ages_to_su = OSL_surfacedcount[torow, tocol, tolay];

            int P_transfer;
            var ages_from_transfer = new List<Int32>();
            var ages_from_keep = new List<Int32>();
            var ages_to_transfer = new List<Int32>();
            var ages_to_keep = new List<Int32>();

            var ages_from_transfer_da = new List<Int32>();
            var ages_from_keep_da = new List<Int32>();
            var ages_to_transfer_da = new List<Int32>();
            var ages_to_keep_da = new List<Int32>();

            var ages_from_transfer_su = new List<Int32>();
            var ages_from_keep_su = new List<Int32>();
            var ages_to_transfer_su = new List<Int32>();
            var ages_to_keep_su = new List<Int32>();

            int ages_before = ages_from.Sum() + ages_to.Sum();

            if (P_fromto < 0.000000001) { P_fromto = 0; }
            if (P_tofrom < 0.000000001) { P_tofrom = 0; }

            // calculate probability of moving
            P_transfer = 0;
            if (P_fromto > 0)
            {
                P_transfer = Convert.ToInt32(Math.Round(1000000000 * P_fromto)); // With a large number (1E9), there is a chance that even the smallest transports are modelled.

                for (int osl_i = 0; osl_i < ages_from.Length; osl_i++) // check for every possibly outgoing grain if it moves
                {
                    int chance_next = randOslLayerMixing.Next(0, 1000000000);
                    if ((chance_next < P_transfer ? 1 : 0) == 1) // if grain gets transported
                    {
                        ages_from_transfer.Add(ages_from[osl_i]);
                        ages_from_transfer_su.Add(ages_from_su[osl_i]);
                        if (bleaching_by_transport == true & (fromrow != torow | fromcol != tocol)) // if the grain is transported laterally and possibly bleached, reset the deposition age
                        {
                            ages_from_transfer_da.Add(0);
                        }
                        else // do not reset the deposition age
                        {
                            ages_from_transfer_da.Add(ages_from_da[osl_i]);
                        }
                    }
                    else
                    {
                        ages_from_keep.Add(ages_from[osl_i]);
                        ages_from_keep_da.Add(ages_from_da[osl_i]);
                        ages_from_keep_su.Add(ages_from_su[osl_i]);
                    }
                }
            }
            else
            {
                ages_from_keep.AddRange(ages_from); // if there is no transport, all grains remain
                ages_from_keep_da.AddRange(ages_from_da);
                ages_from_keep_su.AddRange(ages_from_su);
            }

            if (P_tofrom > 0)
            {
                P_transfer = Convert.ToInt32(Math.Round(1 / P_tofrom));
                for (int osl_i = 0; osl_i < ages_to.Length; osl_i++) // check for every possibly outgoing grain if it moves
                {

                    if ((randOslLayerMixing.Next(0, P_transfer) < 1 ? 1 : 0) == 1) // if grain gets transported
                    {
                        ages_to_transfer.Add(ages_to[osl_i]);
                        ages_to_transfer_su.Add(ages_to_su[osl_i]);

                        if (bleaching_by_transport == true & (fromrow != torow | fromcol != tocol)) // if the grain is transported laterally and possibly bleached, reset the deposition age
                        {
                            ages_to_transfer_da.Add(0);
                        }
                        else // do not reset the deposition age
                        {
                            ages_to_transfer_da.Add(ages_to_da[osl_i]);
                        }
                    }
                    else
                    {
                        ages_to_keep.Add(ages_to[osl_i]);
                        ages_to_keep_da.Add(ages_to_da[osl_i]);
                        ages_to_keep_su.Add(ages_to_su[osl_i]);
                    }
                }
            }
            else
            {
                ages_to_keep.AddRange(ages_to); // if there is no transport, all grains remain
                ages_to_keep_da.AddRange(ages_to_da); // if there is no transport, all grains remain
                ages_to_keep_su.AddRange(ages_to_su); // if there is no transport, all grains remain
            }

            ages_from_keep.AddRange(ages_to_transfer);
            OSL_grainages[fromrow, fromcol, fromlay] = ages_from_keep.ToArray();
            ages_from_keep_da.AddRange(ages_to_transfer_da);
            OSL_depositionages[fromrow, fromcol, fromlay] = ages_from_keep_da.ToArray();
            ages_from_keep_su.AddRange(ages_to_transfer_su);
            OSL_surfacedcount[fromrow, fromcol, fromlay] = ages_from_keep_su.ToArray();

            ages_to_keep.AddRange(ages_from_transfer);
            OSL_grainages[torow, tocol, tolay] = ages_to_keep.ToArray();
            ages_to_keep_da.AddRange(ages_from_transfer_da);
            OSL_depositionages[torow, tocol, tolay] = ages_to_keep_da.ToArray();
            ages_to_keep_su.AddRange(ages_from_transfer_su);
            OSL_surfacedcount[torow, tocol, tolay] = ages_to_keep_su.ToArray();

            int ages_after = OSL_grainages[fromrow, fromcol, fromlay].Sum() + OSL_grainages[torow, tocol, tolay].Sum();
            if (ages_before != ages_after) { Debugger.Break(); }

            OSL_JA_t += DateTime.Now - OSL_JA_start;
        }

        void update_cosmogenic_nuclides()
        {
            double local_met10Be_uptake, layer_met10Be_index, total_met10Be_index, total_soil_thickness_m, depth, cum_BD_kg_cm2, sandmass_g, layer_input_atoms_sp, layer_input_atoms_mu;
            ;
            try
            {
                for (row = 0; row < nr; row++)
                {
                    for (col = 0; col < nc; col++)
                    {
                        // Meteoric Beryllium-10
                        total_soil_thickness_m = total_soil_thickness(row, col);

                        local_met10Be_uptake = met_10Be_input * (1 - Math.Exp(-met_10Be_adsorptioncoefficient * total_soil_thickness_m)) * dt; // # atoms cm-2
                        total_met10Be_index = 1 - (Math.Exp(-met_10Be_adsorptioncoefficient * total_soil_thickness_m));

                        depth = 0;
                        cum_BD_kg_cm2 = 0;

                        for (int lay = 0; lay < max_soil_layers; lay++)
                        {
                            // Meteoric 10-Be
                            // Uptake
                            layer_met10Be_index = Math.Exp(-met_10Be_adsorptioncoefficient * depth) - (Math.Exp(-met_10Be_adsorptioncoefficient * (depth + layerthickness_m[row, col, lay])));
                            CN_atoms_cm2[row, col, lay, 0] += layer_met10Be_index / total_met10Be_index * local_met10Be_uptake * met_10Be_clayfraction;
                            CN_atoms_cm2[row, col, lay, 1] += layer_met10Be_index / total_met10Be_index * local_met10Be_uptake * (1 - met_10Be_clayfraction);
                            depth += layerthickness_m[row, col, lay];
                            // decay
                            CN_atoms_cm2[row, col, lay, 0] *= (1 - decay_Be10);
                            CN_atoms_cm2[row, col, lay, 1] *= (1 - decay_Be10);

                            // In-situ cosmogenic nuclides
                            // Assumed that all sand is quartz in the model
                            sandmass_g = texture_kg[row, col, lay, 1] * 1000;

                            // In-situ Be-10
                            layer_input_atoms_sp = P0_10Be_is_sp * sandmass_g / (dx * 100 * dx * 100) * dt; // # atoms cm-2
                            layer_input_atoms_mu = P0_10Be_is_mu * sandmass_g / (dx * 100 * dx * 100) * dt; // # atoms cm-2
                            CN_atoms_cm2[row, col, lay, 2] += layer_input_atoms_sp * Math.Exp(-cum_BD_kg_cm2 / attenuation_length_sp); // Spallation production
                            CN_atoms_cm2[row, col, lay, 2] += layer_input_atoms_mu * Math.Exp(-cum_BD_kg_cm2 / attenuation_length_mu); // Muonic production
                            CN_atoms_cm2[row, col, lay, 2] *= (1 - decay_Be10); // Decay

                            // In-situ C-14
                            layer_input_atoms_sp = P0_14C_is_sp * sandmass_g / (dx * 100 * dx * 100) * dt; // # atoms cm-2
                            layer_input_atoms_mu = P0_14C_is_mu * sandmass_g / (dx * 100 * dx * 100) * dt; // # atoms cm-2
                            CN_atoms_cm2[row, col, lay, 3] += layer_input_atoms_sp * Math.Exp(-cum_BD_kg_cm2 / attenuation_length_sp); // Spallation production
                            CN_atoms_cm2[row, col, lay, 3] += layer_input_atoms_mu * Math.Exp(-cum_BD_kg_cm2 / attenuation_length_mu); // Muonic production
                            CN_atoms_cm2[row, col, lay, 3] *= (1 - decay_C14); // Decay

                            // Update cumulative bulk density
                            cum_BD_kg_cm2 += bulkdensity[row, col, lay] * layerthickness_m[row, col, lay];
                        }
                    }
                }
            }
            catch
            {
                Debug.WriteLine("Error in updating cosmogenic nuclides");
            }

        }

        void transport_ero_sed_OSL_by_WE(int row_OSL, int col_OSL, int i_OSL, int j_OSL, double sum_fractions, double new_fraction, double P_ero_0, double P_ero_1, double P_sed)
        {
            var grains_to_next_cell = new List<Int32>();
            var grains_to_next_cell_da = new List<Int32>();
            var grains_to_next_cell_su = new List<Int32>();
            var grains_at_source_location = new List<Int32>();
            var grains_at_source_location_da = new List<Int32>();
            var grains_at_source_location_su = new List<Int32>();
            int P_transfer;
            int[] ages_array;
            double P_ero;

            if (sum_fractions == 0) // If it is the first time this cell is considered, shuffle the present grains to prevent earlier eroded grains to be deposited the first
            {
                int[] indices = new int[OSL_grainages_in_transport[row_OSL, col_OSL].Length]; // create array of indices
                if (indices.Length > 0)
                {
                    for (int ii = 0; ii < indices.Length; ii++) { indices[ii] = ii; } // fill them with numbers
                    indices = indices.OrderBy(x => randOslLayerMixing.Next()).ToArray(); // shuffle the array
                    int[] indices_da = new int[indices.Length]; // create new indice arrays for the other age properties
                    int[] indices_su = new int[indices.Length];
                    for (int ii = 0; ii < indices.Length; ii++) { indices_da[ii] = indices[ii]; indices_su[ii] = indices[ii]; } // copy the indices array

                    ages_array = OSL_grainages_in_transport[row_OSL, col_OSL];
                    Array.Sort(indices, ages_array);
                    OSL_grainages_in_transport[row_OSL, col_OSL] = ages_array;

                    ages_array = OSL_depositionages_in_transport[row_OSL, col_OSL];
                    Array.Sort(indices_da, ages_array);
                    OSL_depositionages_in_transport[row_OSL, col_OSL] = ages_array;

                    ages_array = OSL_surfacedcount_in_transport[row_OSL, col_OSL];
                    Array.Sort(indices_su, ages_array);
                    OSL_surfacedcount_in_transport[row_OSL, col_OSL] = ages_array;
                }
            }

            // select grains that go in the direction of row+i and col+j, based on the fractions of water flow
            int[] grains_considered = OSL_grainages_in_transport[row_OSL, col_OSL];
            int ind_start = Convert.ToInt32(Math.Round(sum_fractions * grains_considered.Length));
            int ind_end = Convert.ToInt32(Math.Round((sum_fractions + new_fraction) * grains_considered.Length));
            int ind_count = ind_end - ind_start;
            if (ind_start < 0 | ind_end > grains_considered.Length | ind_count < 0) { Debugger.Break(); }

            // Select grains that are in transport to next cell
            grains_to_next_cell.AddRange(OSL_grainages_in_transport[row_OSL, col_OSL].Skip(ind_start).Take(ind_count));
            grains_to_next_cell_da.AddRange(OSL_depositionages_in_transport[row_OSL, col_OSL].Skip(ind_start).Take(ind_count));
            grains_to_next_cell_su.AddRange(OSL_surfacedcount_in_transport[row_OSL, col_OSL].Skip(ind_start).Take(ind_count));

            // Erosion/uptake of grains
            double[] P_ero_both_layers = new double[] { P_ero_0, P_ero_1 };
            for (int lay_OSL = 0; lay_OSL < 2; lay_OSL++)
            {
                P_ero = P_ero_both_layers[lay_OSL];

                if (P_ero > 0)
                {
                    // Select grains that are at the source location, in the eroding layer
                    grains_at_source_location.AddRange(OSL_grainages[row_OSL, col_OSL, lay_OSL]);
                    grains_at_source_location_da.AddRange(OSL_depositionages[row_OSL, col_OSL, lay_OSL]);
                    grains_at_source_location_su.AddRange(OSL_surfacedcount[row_OSL, col_OSL, lay_OSL]);

                    // Loop over the grains, to see if they erode
                    P_transfer = Convert.ToInt32(Math.Round(10000 * P_ero));
                    var indices_to_be_removed = new List<Int32>();
                    for (int osl_i = 0; osl_i < grains_at_source_location.Count; osl_i++)
                    {
                        if ((randOslLayerMixing.Next(0, 10000) < P_transfer ? 1 : 0) == 1) // if grain gets eroded
                        {
                            // add to the transfer grains
                            grains_to_next_cell.Add(grains_at_source_location[osl_i]);
                            grains_to_next_cell_da.Add(grains_at_source_location_da[osl_i]);
                            grains_to_next_cell_su.Add(grains_at_source_location_su[osl_i]);
                            indices_to_be_removed.Add(osl_i);
                        }
                    }
                    // Go through indices to be removed in descending order, to remove grains from source location
                    for (int osl_i = (indices_to_be_removed.Count - 1); osl_i >= 0; osl_i--) // 
                    {
                        grains_at_source_location.RemoveAt(indices_to_be_removed[osl_i]);
                        grains_at_source_location_da.RemoveAt(indices_to_be_removed[osl_i]);
                        grains_at_source_location_su.RemoveAt(indices_to_be_removed[osl_i]);
                    }
                    // Reassign the grains that already were at the source location, minus the eroded grains
                    OSL_grainages[row_OSL, col_OSL, 0] = grains_at_source_location.ToArray();
                    OSL_depositionages[row_OSL, col_OSL, 0] = grains_at_source_location_da.ToArray();
                    OSL_surfacedcount[row_OSL, col_OSL, 0] = grains_at_source_location_su.ToArray();
                }
            }

            // Deposition of grains
            if (P_sed > 0)
            {
                // Select grains that are in the surface layer at the source location
                grains_at_source_location.AddRange(OSL_grainages[row_OSL, col_OSL, 0]);
                grains_at_source_location_da.AddRange(OSL_depositionages[row_OSL, col_OSL, 0]);
                grains_at_source_location_su.AddRange(OSL_surfacedcount[row_OSL, col_OSL, 0]);

                P_transfer = Convert.ToInt32(Math.Round(10000 * P_sed));
                var indices_to_be_removed = new List<Int32>();
                for (int osl_i = 0; osl_i < grains_to_next_cell.Count; osl_i++)
                {
                    if ((randOslLayerMixing.Next(0, 10000) < P_transfer ? 1 : 0) == 1) // if grain gets deposited
                    {
                        grains_at_source_location.Add(grains_to_next_cell[osl_i]);
                        grains_at_source_location_da.Add(0); // Add zero instead of age, because deposition age is reset after deposition
                        grains_at_source_location_su.Add(grains_to_next_cell_su[osl_i]);
                        indices_to_be_removed.Add(osl_i);
                    }
                }
                // Loop through indices to be removed in descending order, to remove grains from transport
                for (int osl_i = (indices_to_be_removed.Count - 1); osl_i >= 0; osl_i--) // Does this work?
                {
                    grains_to_next_cell.RemoveAt(indices_to_be_removed[osl_i]);
                    grains_to_next_cell_da.RemoveAt(indices_to_be_removed[osl_i]);
                    grains_to_next_cell_su.RemoveAt(indices_to_be_removed[osl_i]);
                }
                // Add the grains that already were at the  source location, with possible deposited grains, to the source location
                OSL_grainages[row_OSL, col_OSL, 0] = grains_at_source_location.ToArray();
                OSL_depositionages[row_OSL, col_OSL, 0] = grains_at_source_location_da.ToArray();
                OSL_surfacedcount[row_OSL, col_OSL, 0] = grains_at_source_location_su.ToArray();
            }

            // Transfer remaining grains
            // The grains at the source location have already been updated, either through erosion or deposition, or they haven't been affected
            // Now we have to transfer the grains that are still in transport to the next cell. Keep in mind that there might be already grains in transport present, which should be added
            grains_to_next_cell.AddRange(OSL_grainages_in_transport[row_OSL + i_OSL, col_OSL + j_OSL]);
            OSL_grainages_in_transport[row_OSL + i_OSL, col_OSL + j_OSL] = grains_to_next_cell.ToArray();
            grains_to_next_cell_da.AddRange(OSL_depositionages_in_transport[row_OSL + i_OSL, col_OSL + j_OSL]);
            OSL_depositionages_in_transport[row_OSL + i_OSL, col_OSL + j_OSL] = grains_to_next_cell_da.ToArray();
            grains_to_next_cell_su.AddRange(OSL_surfacedcount_in_transport[row_OSL + i_OSL, col_OSL + j_OSL]);
            OSL_surfacedcount_in_transport[row_OSL + i_OSL, col_OSL + j_OSL] = grains_to_next_cell_su.ToArray();
        }

        double total_CNs()
        {
            double total_CN = 0;
            for (int cn_row = 0; cn_row < nr; cn_row++)
            {
                for (int cn_col = 0; cn_col < nc; cn_col++)
                {
                    for (int cn_lay = 0; cn_lay < max_soil_layers; cn_lay++)
                    {
                        for (int cosmo = 0; cosmo < n_cosmo; cosmo++)
                        {
                            total_CN += CN_atoms_cm2[cn_row, cn_col, cn_lay, cosmo];
                        }
                    }
                }
            }
            return (total_CN);
        }

        #endregion

    }
}
