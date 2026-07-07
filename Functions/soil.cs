using MathNet.Numerics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

namespace LORICA4
{
    public partial class Mother_form
    {

        public double activity_fraction(double characteristic_depth_m, double soildepth_m, double layertop_m, double layerbottom_m)
        {
            double c = characteristic_depth_m;
            if (soildepth_m < 1e-5)  // If soil depth is too small, use a default safe value //AleG
            {
                return 0.0;  // Return a reasonable default for small soil depths
            }
            double activity_fraction = (Math.Exp(-layertop_m / c) - Math.Exp(-layerbottom_m / c)) / (Math.Exp(-0 / c) - Math.Exp(-soildepth_m / c));
            return activity_fraction;
        }

        public double layer_midpoint_m(double characteristic_depth_m, double layertop_m, double layerbottom_m)
        {
            double c = characteristic_depth_m;
            double z_star = -c * Math.Log((Math.Exp(-layertop_m / c) + Math.Exp(-layerbottom_m / c)) / 2);
            return z_star;
        }
        void soil_physical_weathering()  // calculate physical weathering
        {
            // Constants and small thresholds
            const double EPS = 1e-12;      // small number for numeric guards
            const double NEG_TOL = -1e-12; // tolerance for negative checks

            // Reset the accumulator for this timestep/run (assuming it's a field)
            total_phys_weathered_mass_kg = 0.0;

            decimal old_mass_kg = total_catchment_mass_decimal();

            Debug.WriteLine("phys weath started");

            // -----------------------------------------------------------
            // Precompute per-class constants to avoid repeated Log10 calls
            // -----------------------------------------------------------
            // We only weather classes 0..2 here. Assumes texture_kg has at least 4 classes (0..3).
            double[] classFactor = new double[3]; // dt * physical_weathering_constant * (-Ctwo) / log10(up), clamped to >= 0
            bool[] classUsable = new bool[3];     // whether we can safely use the factor (valid up)
                                                  // Optional: split primary fractions for classes 0 and 1
            double[] splitPrimary = new double[3] { 0.975, 0.96, 1.0 }; // class 2 uses full delta -> class 3

            for (int k = 0; k <= 2; k++)
            {
                classUsable[k] = false;
                double up = upper_particle_size[k];

                // Guard: upper_particle_size must be positive and != 1
                if (!(up > 0.0) || Math.Abs(up - 1.0) < EPS)
                {
                    continue;
                }

                double denom = Math.Log10(up);
                if (double.IsNaN(denom) || double.IsInfinity(denom) || Math.Abs(denom) < EPS)
                {
                    continue;
                }

                // Base factor (without layer_fraction, which varies per layer) and with dt applied
                double baseFactor = dt * physical_weathering_constant * (-Ctwo) / denom;

                // Enforce non-negative weathering rate (coarse -> finer). Remove clamp if signed behavior is intended.
                if (baseFactor < 0.0)
                {
                    baseFactor = 0.0;
                }

                classFactor[k] = baseFactor;
                classUsable[k] = (baseFactor > 0.0);
            }

            try
            {
                for (int row = 0; row < nr; row++)
                {
                    for (int col = 0; col < nc; col++)
                    {
                        // If proglacial mode is on, skip glacier cells
                        if (Proglacial_checkbox.Checked && glacier_cell[row, col] != 0)
                        {
                            continue;
                        }

                        double depth = 0.0;

                        for (int layer = 0; layer < max_soil_layers; layer++)
                        {
                            double thick = layerthickness_m[row, col, layer];
                            if (thick <= 0.0)
                            {
                                continue;
                            }

                            // Compute activity fraction for this layer slice
                            double layer_fraction = activity_fraction(
                                phys_weath_decay_depth_m,
                                soildepth_m[row, col],
                                depth,
                                depth + thick
                            );

                            // If layer_fraction is 0 or invalid, skip class loop quickly
                            if (double.IsNaN(layer_fraction) || double.IsInfinity(layer_fraction) || layer_fraction <= 0.0)
                            {
                                depth += thick;
                                continue;
                            }

                            // Process texture classes 0..2 (coarse, sand, silt)
                            for (int tex_class = 0; tex_class <= 2; tex_class++)
                            {
                                if (!classUsable[tex_class])
                                {
                                    // Skip classes with invalid or zero factor (avoids Log10 issues, zero rate, etc.)
                                    continue;
                                }

                                // Effective factor for this layer (adds layer_fraction)
                                double effFactor = classFactor[tex_class] * layer_fraction;
                                if (effFactor <= 0.0 || double.IsNaN(effFactor) || double.IsInfinity(effFactor))
                                {
                                    continue;
                                }

                                // Available mass in this texture class
                                double available = texture_kg[row, col, layer, tex_class];
                                if (double.IsNaN(available) || double.IsInfinity(available) || available <= 0.0)
                                {
                                    continue;
                                }

                                // Weathered mass (delta). Clamp to available to avoid overdrawing.
                                double delta = available * effFactor;
                                if (double.IsNaN(delta) || double.IsInfinity(delta) || delta <= 0.0)
                                {
                                    continue;
                                }
                                if (delta > available)
                                {
                                    delta = available;
                                }

                                // Mass transfer: subtract from source first
                                texture_kg[row, col, layer, tex_class] -= delta;

                                // Distribute to finer classes with exact conservation via remainder
                                if (tex_class == 0) // coarse -> sand and silt
                                {
                                    double p = splitPrimary[0]; // 0.975
                                    double add1 = p * delta;             // to class 1 (sand)
                                    double add2 = delta - add1;          // exact remainder to class 2 (silt)
                                    texture_kg[row, col, layer, 1] += add1;
                                    texture_kg[row, col, layer, 2] += add2;
                                }
                                else if (tex_class == 1) // sand -> silt and clay
                                {
                                    double p = splitPrimary[1]; // 0.96
                                    double add1 = p * delta;             // to class 2 (silt)
                                    double add2 = delta - add1;          // exact remainder to class 3 (clay)
                                    texture_kg[row, col, layer, 2] += add1;
                                    texture_kg[row, col, layer, 3] += add2;
                                }
                                else if (tex_class == 2) // silt -> clay
                                {
                                    // All goes to clay
                                    texture_kg[row, col, layer, 3] += delta;
                                }

                                total_phys_weathered_mass_kg += delta;

                                // Sanity checks and clamping for negatives/tiny values
                                double src = texture_kg[row, col, layer, tex_class];
                                if (src < NEG_TOL)
                                {
                                    Debug.WriteLine("Warning: negative texture mass (src) at row=" + row + " col=" + col + " layer=" + layer + " class=" + tex_class + " val=" + src);
                                }
                                if (src < 0.0)
                                {
                                    texture_kg[row, col, layer, tex_class] = 0.0;
                                }

                                // Optional: destination checks
                                // dst1
                                if (tex_class <= 2)
                                {
                                    int c1 = tex_class + 1;
                                    double dst1 = texture_kg[row, col, layer, c1];
                                    if (dst1 < NEG_TOL)
                                    {
                                        Debug.WriteLine("Warning: negative texture mass (dst1) at row=" + row + " col=" + col + " layer=" + layer + " class=" + c1 + " val=" + dst1);
                                    }
                                    if (dst1 < 0.0) texture_kg[row, col, layer, c1] = 0.0;
                                }
                                // dst2
                                if (tex_class <= 1)
                                {
                                    int c2 = tex_class + 2;
                                    double dst2 = texture_kg[row, col, layer, c2];
                                    if (dst2 < NEG_TOL)
                                    {
                                        Debug.WriteLine("Warning: negative texture mass (dst2) at row=" + row + " col=" + col + " layer=" + layer + " class=" + c2 + " val=" + dst2);
                                    }
                                    if (dst2 < 0.0) texture_kg[row, col, layer, c2] = 0.0;
                                }
                            }

                            // Advance depth by this layer thickness
                            depth += thick;
                        }
                    }
                }

                // timeseries
                if (timeseries.timeseries_cell_waterflow_check.Checked)
                {
                    timeseries_matrix[t, timeseries_order[23]] = total_phys_weathered_mass_kg;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Soil physical weathering calculation threw an exception: " + ex.Message);
            }

            // Mass conservation check with absolute + relative tolerance
            decimal new_mass_kg = total_catchment_mass_decimal();
            decimal diff = Math.Abs(old_mass_kg - new_mass_kg);
            decimal relTol = old_mass_kg != 0m ? Math.Abs(old_mass_kg) * 1e-12m : 0m;
            decimal absTol = 0.0001m;
            decimal tol = (relTol > absTol) ? relTol : absTol;

            if (diff > tol)
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
                                tex_class = 0;
                                int tempclass = tex_class;
                                // calculate the mass involved in physical weathering
                                double layer_fraction = activity_fraction(phys_weath_decay_depth_m, soildepth_m[row, col], depth, depth + layerthickness_m[row, col, layer]);
                                weathered_mass_kg = texture_kg[row, tempcol, templayer, tempclass] * physical_weathering_constant * layer_fraction * -Ctwo / Math.Log10(upper_particle_size[tempclass]) * dt;
                                //Debug.WriteLine(" weathered mass is " + weathered_mass + " for class " + tempclass );
                                // calculate the products involved
                                texture_kg[row, tempcol, templayer, tempclass + 2] += 0.1 * weathered_mass_kg;
                                texture_kg[row, tempcol, templayer, tempclass] -= weathered_mass_kg;
                                depth += layerthickness_m[row, tempcol, templayer];
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
                    if (Proglacial_checkbox.Checked)
                    {
                        if (glacier_cell[row, col] != 1)
                        {
                            for (layer = 0; layer < max_soil_layers; layer++)
                            {
                                if (layerthickness_m[row, col, layer] > 0)
                                {
                                    double layer_fraction = activity_fraction(chem_weath_decay_depth_m, soildepth_m[row, col], depth, depth + layerthickness_m[row, col, layer]);
                                    for (tex_class = 1; tex_class <= 4; tex_class++) // only sand, silt, clay and fine clay are chemically weathered
                                    {

                                        weathered_mass_kg = texture_kg[row, col, layer, tex_class] * chemical_weathering_constant / 10 * layer_fraction * Cfour * specific_area[tex_class] * dt;

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
                                    depth += layerthickness_m[row, col, layer];
                                }
                            }
                        }
                    }
                    else
                    {
                        for (layer = 0; layer < max_soil_layers; layer++)
                        {
                            if (layerthickness_m[row, col, layer] > 0)
                            {
                                double layer_fraction = activity_fraction(chem_weath_decay_depth_m, soildepth_m[row, col], depth, depth + layerthickness_m[row, col, layer]);
                                for (tex_class = 1; tex_class <= 4; tex_class++) // only sand, silt, clay and fine clay are chemically weathered
                                {

                                    weathered_mass_kg = texture_kg[row, col, layer, tex_class] * chemical_weathering_constant / 10 * layer_fraction * Cfour * specific_area[tex_class] * dt;

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
                                depth += layerthickness_m[row, col, layer];
                            }
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

        void soil_bioturbation_mixing()
        {
            try
            {
                //for bioturbation, we first calculate how much bioturbation (kg) this cell will experience, given its thickness
                //shallower soils do not experience the same amount as deeper soils
                //then we look at individual layers. Thicker layers, and layers closer to the surface, will experience more bioturbation kg
                //then, per layer, we will exchange the required bioturbation kg with the surrounding layers. 
                //Layers that are closer will exchange more than those that are further (regardless of whether they are deeper or closer to the surface)

                double local_bioturbation_kg, layer_bioturbation_kg, interlayer_bioturbation_kg;
                int layer, otherlayer;
                double fine_otherlayer_mass, fine_layer_mass;
                double total_soil_thickness_m;
                double depth, otherdepth, distance;
                total_mass_bioturbed_kg = 0;
                double[,] temp_tex_som_kg = new double[max_soil_layers, 7]; // this will hold temporary changed values of texture until all bioturbation is done
                double[] layer_0 = new double[7], layer_0_after = new double[7];
                double mass_top_before = 0, mass_top_after = 0;
                decimal mass_soil_before = 0, mass_soil_after = 0;
                double total_BT_transport_kgm = 0;
                double total_young_som_kg, total_old_som_kg;
                //if (CN_checkbox.Checked) { CN_before = total_CNs(); }
                for (row = 0; row < nr; row++)
                {
                    for (col = 0; col < nc; col++)
                    {
                        if (Proglacial_checkbox.Checked)
                        {
                            if (glacier_cell[row, col] != 1)
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
                                    local_bioturbation_kg = potential_bt_mixing_kg_m2_y * (1 - Math.Exp(- total_soil_thickness_m/ bioturbation_decay_depth_m)) * dx * dx * dt; //AleG_june26
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
                                            double dd_bt_m = bioturbation_decay_depth_m * 2; // possible adjustments to second depth decay for bioturbation are possible here


                                            //double total_BT_depth_decay_index = 
                                            //    -1/dd_bt*(Math.Exp(-dd_bt*(z_toplayer - 0)) - Math.Exp(-dd_bt*(z_toplayer - z_toplayer))) +
                                            //    -1/dd_bt*(Math.Exp(-dd_bt*(total_soil_thickness_m - z_bottomlayer)) - Math.Exp(-dd_bt*(z_bottomlayer - z_bottomlayer)));                                    


                                            double check_BT_dd = 0;

                                            //integration over the exponential decay function in JGR 2006 for the entire profile, and for the current layer.
                                            //then calculate the fraction of bioturbation that will happen in this layer, and multiply with total bioturbation in this cell
                                            fine_layer_mass = total_layer_fine_earth_mass_kg(row, col, layer);

                                            layer_bioturbation_kg = activity_fraction(bioturbation_decay_depth_m, total_soil_thickness_m, depth, depth + layerthickness_m[row, col, layer]) * local_bioturbation_kg;
                                            depth += layerthickness_m[row, col, layer] / 2;  ///ArT development needed

                                            double total_BT_depth_decay_index =
                                                -1 / dd_bt_m * (Math.Exp(- (depth - 0)/ dd_bt_m) - 1) +  //AleG_june26 // upper part of the curve //Should this also be changed since dd_bt_m is now m instead of 1/m? AleG
                                                -1 / dd_bt_m * (Math.Exp(-(total_soil_thickness_m - depth)/ dd_bt_m) - 1);  //AleG_june26 // lower part of the curve //Should this also be changed since dd_bt_m is now m instead of 1/m? AleG


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

                                            otherdepth = 0; distance = 0;
                                            double layer_BT_depth_decay_index = 0;
                                            for (otherlayer = 0; otherlayer < max_soil_layers; otherlayer++)
                                            {
                                                double z_topotherlayer = otherdepth;
                                                double z_bottomotherlayer = otherdepth + layerthickness_m[row, col, otherlayer];

                                                if (otherlayer < layer) // above the bioturbated layer
                                                {
                                                    layer_BT_depth_decay_index = -1 / dd_bt_m * (Math.Exp(-(depth - z_topotherlayer) / dd_bt_m) - Math.Exp(-(depth - z_bottomotherlayer) / dd_bt_m));

                                                }
                                                if (otherlayer == layer) // if layer is the same layer. Can it be excluded here and in the calculations? It should be included in calculating the depth profiles
                                                {
                                                    layer_BT_depth_decay_index = -1 / dd_bt_m * (Math.Exp(-(depth - z_topotherlayer) / dd_bt_m) - 1) +
                                                    -1 / (Math.Exp(-dd_bt_m * (z_bottomotherlayer - depth) / dd_bt_m) - 1);

                                                }
                                                if (otherlayer > layer) // below the bioturbated layer
                                                {
                                                    layer_BT_depth_decay_index = -1 / dd_bt_m * (Math.Exp(-(z_bottomotherlayer - depth) / dd_bt_m) - Math.Exp(-(z_topotherlayer - depth) / dd_bt_m));
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
                                                    double fromlayertomixture_kg = 0, fromotherlayertomixture_kg = 0, totalmixturemass_kg = 0, massfromlayer = 0, massfromotherlayer = 0, dmass_l, dmass_ol;
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
                            }
                        }
                        else
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
                                local_bioturbation_kg = potential_bt_mixing_kg_m2_y * (1 - Math.Exp(-total_soil_thickness_m / bioturbation_decay_depth_m)) * dx * dx * dt; //AleG_june26
                                if(local_bioturbation_kg < 0) // local_bt == 0 happens when soil is absent
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
                                        double dd_bt_m = bioturbation_decay_depth_m * 2; // possible adjustments to second depth decay for bioturbation are possible here


                                        //double total_BT_depth_decay_index = 
                                        //    -1/dd_bt*(Math.Exp(-dd_bt*(z_toplayer - 0)) - Math.Exp(-dd_bt*(z_toplayer - z_toplayer))) +
                                        //    -1/dd_bt*(Math.Exp(-dd_bt*(total_soil_thickness_m - z_bottomlayer)) - Math.Exp(-dd_bt*(z_bottomlayer - z_bottomlayer)));                                    


                                        double check_BT_dd = 0;

                                        //integration over the exponential decay function in JGR 2006 for the entire profile, and for the current layer.
                                        //then calculate the fraction of bioturbation that will happen in this layer, and multiply with total bioturbation in this cell
                                        fine_layer_mass = total_layer_fine_earth_mass_kg(row, col, layer);

                                        layer_bioturbation_kg = activity_fraction(bioturbation_decay_depth_m, total_soil_thickness_m, depth, depth + layerthickness_m[row, col, layer]) * local_bioturbation_kg;
                                        depth += layerthickness_m[row, col, layer] / 2;  ///ArT development needed

                                        double total_BT_depth_decay_index =
                                            -1 / dd_bt_m * (Math.Exp(-dd_bt_m * (depth - 0)) - 1) + // upper part of the curve //Should this also be changed since dd_bt_m is now m instead of 1/m? AleG
                                            -1 / dd_bt_m * (Math.Exp(-dd_bt_m * (total_soil_thickness_m - depth)) - 1); // lower part of the curve //Should this also be changed since dd_bt_m is now m instead of 1/m? AleG


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

                                        otherdepth = 0; distance = 0;
                                        double layer_BT_depth_decay_index = 0;
                                        for (otherlayer = 0; otherlayer < max_soil_layers; otherlayer++)
                                        {
                                            double z_topotherlayer = otherdepth;
                                            double z_bottomotherlayer = otherdepth + layerthickness_m[row, col, otherlayer];

                                            if (otherlayer < layer) // above the bioturbated layer
                                            {
                                                layer_BT_depth_decay_index = -1 / dd_bt_m * (Math.Exp(-(depth - z_topotherlayer) / dd_bt_m) - Math.Exp(-(depth - z_bottomotherlayer) / dd_bt_m));

                                            }
                                            if (otherlayer == layer) // if layer is the same layer. Can it be excluded here and in the calculations? It should be included in calculating the depth profiles
                                            {
                                                layer_BT_depth_decay_index = -1 / dd_bt_m * (Math.Exp(-(depth - z_topotherlayer) / dd_bt_m) - 1) +
                                                -1 / (Math.Exp(-dd_bt_m * (z_bottomotherlayer - depth) / dd_bt_m) - 1);

                                            }
                                            if (otherlayer > layer) // below the bioturbated layer
                                            {
                                                layer_BT_depth_decay_index = -1 / dd_bt_m * (Math.Exp(-(z_bottomotherlayer - depth) / dd_bt_m) - Math.Exp(-(z_topotherlayer - depth) / dd_bt_m));
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
                                                double fromlayertomixture_kg = 0, fromotherlayertomixture_kg = 0, totalmixturemass_kg = 0, massfromlayer = 0, massfromotherlayer = 0, dmass_l, dmass_ol;
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
                        }

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
                double local_bioturbation_kg, layer_bioturbation_kg;
                int layer;
                double fine_layer_mass;
                double total_soil_thickness_m;
                double depth;
                total_mass_bioturbed_kg = 0;
                double[,] temp_tex_som_kg = new double[max_soil_layers, 7]; // this will hold temporary changed values of texture until all bioturbation is done
                double[] layer_0 = new double[7], layer_0_after = new double[7];
                double mass_top_before = 0, mass_top_after = 0;
                decimal mass_soil_before = 0, mass_soil_after = 0;
                double total_BT_transport_kgm = 0;
                double bioturbated_fraction;
                //if (CN_checkbox.Checked) { CN_before = total_CNs(); }
                for (row = 0; row < nr; row++)
                {
                    for (col = 0; col < nc; col++)
                    {

                        if (Proglacial_checkbox.Checked)
                        {
                            if (glacier_cell[row, col] != 1)
                            {
                                if (dtm[row, col] != nodata_value & soildepth_m[row, col] > 0)
                                {
                                    remove_empty_layers(row, col);
                                    update_all_layer_thicknesses(row, col);
                                    mass_soil_before = total_soil_mass_kg_decimal(row, col);
                                    mass_top_before = total_layer_mass_kg(row, col, 0);
                                    total_soil_thickness_m = total_soil_thickness(row, col);

                                    //here we calculate the first quantity: how much bioturbation kg needs to happen in this location
                                    local_bioturbation_kg = potential_bt_mixing_kg_m2_y * (1 - Math.Exp(-total_soil_thickness_m / bioturbation_decay_depth_m)) * dx * dx * dt; //AleG_june26
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
                                            double dd_bt = bioturbation_decay_depth_m; // possible adjustments to second depth decay for bioturbation are possible here

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
                            }
                        }
                        else
                        {
                            if (dtm[row, col] != nodata_value & soildepth_m[row, col] > 0)
                            {
                                remove_empty_layers(row, col);
                                update_all_layer_thicknesses(row, col);
                                mass_soil_before = total_soil_mass_kg_decimal(row, col);
                                mass_top_before = total_layer_mass_kg(row, col, 0);
                                total_soil_thickness_m = total_soil_thickness(row, col);

                                //here we calculate the first quantity: how much bioturbation kg needs to happen in this location
                                local_bioturbation_kg = potential_bt_mounding_kg_m2_y * (1 - Math.Exp(-total_soil_thickness_m / bioturbation_decay_depth_m)) * dx * dx * dt; //AleG_june26
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
                                        double dd_bt = bioturbation_decay_depth_m; // possible adjustments to second depth decay for bioturbation are possible here

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
                        }
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

        void soil_bioturbation_upheaval(double uph_freq, double uph_depth, int uphrow, int uphcol)
        {
            // Code from mixing by tillage. Homogenizes the soil over the mixing depth. Is used in tillage and bioturbation 
            try
            {
                if (t % uph_freq == 0) // Does an upheaval event occur in this simulation year?
                {

                    //if (uphrow == 617 && uphcol == 560) { Debug.WriteLine(" I will now mix material from top layers for tillage for row " + row + " " + col); }
                    if (Proglacial_checkbox.Checked)
                    {
                        if (glacier_cell[uphrow, uphcol] != 1)
                        {
                            if (dtm[uphrow, uphcol] != nodata_value & soildepth_m[uphrow, uphcol] > 0)
                            {
                                update_all_layer_thicknesses(uphrow, uphcol);

                                double mixeddepth = 0, completelayerdepth = 0, newdepth = 0;
                                int completelayers = -1;

                                // limit upheaval to available soil
                                local_soil_depth_m = total_soil_thickness(uphrow, uphcol);
                                uph_depth = Math.Min(uph_depth, local_soil_depth_m);

                                while (mixeddepth <= uph_depth & completelayers < (max_soil_layers - 1))
                                {
                                    completelayers++;
                                    mixeddepth += layerthickness_m[uphrow, uphcol, completelayers];
                                    // OSL_age[uphrow, uphcol, completelayers] = 0;

                                }// this will lead to incorporation of the (partial) layer below tillage horizon in completelayers parameter. So the highest number indicates the partial layer 
                                 // Debug.WriteLine("till2");
                                double[] upheaved_text = new double[5]; // contains soil 
                                double[] upheaved_om = new double[2]; // contains OM
                                double[] alldepths = new double[completelayers + 1]; // contains thicknesses of all layers
                                double[] upheaved_mass = new double[completelayers + 1];
                                double[] fraction_mixed = new double[completelayers + 1];
                                double[] upheaved_cosmo_nuclides = new double[n_cosmo]; // contains cosmogenic nuclides

                                // take material from layers to mix
                                decimal mass_soil_before = total_soil_mass_kg_decimal(uphrow, uphcol);
                                double fraction_mixed_layer;
                                for (int lay = 0; lay <= completelayers; lay++) // Includes partial layer, will be selective taken up
                                {
                                    if ((completelayerdepth + layerthickness_m[uphrow, uphcol, lay]) < uph_depth)
                                    {
                                        fraction_mixed_layer = 1;
                                        fraction_mixed[lay] = 1;
                                        alldepths[lay] = layerthickness_m[uphrow, uphcol, lay];
                                    }
                                    else
                                    {
                                        fraction_mixed_layer = (uph_depth - completelayerdepth) / layerthickness_m[uphrow, uphcol, lay];
                                        fraction_mixed[lay] = fraction_mixed_layer; // fraction of layer that is mixed
                                        alldepths[lay] = layerthickness_m[uphrow, uphcol, lay] * fraction_mixed_layer; // part of layer [m] that is considered
                                    }
                                    completelayerdepth += layerthickness_m[uphrow, uphcol, lay];
                                    for (int tex = 0; tex < 5; tex++)
                                    {
                                        upheaved_text[tex] += texture_kg[uphrow, uphcol, lay, tex] * fraction_mixed_layer;
                                        upheaved_mass[lay] += texture_kg[uphrow, uphcol, lay, tex] * fraction_mixed_layer;
                                        texture_kg[uphrow, uphcol, lay, tex] *= (1 - fraction_mixed_layer);

                                    }
                                    upheaved_om[0] += old_SOM_kg[uphrow, uphcol, lay] * fraction_mixed_layer;
                                    upheaved_om[1] += young_SOM_kg[uphrow, uphcol, lay] * fraction_mixed_layer;
                                    upheaved_mass[lay] += (old_SOM_kg[uphrow, uphcol, lay] * fraction_mixed_layer + young_SOM_kg[uphrow, uphcol, lay] * fraction_mixed_layer);
                                    old_SOM_kg[uphrow, uphcol, lay] *= (1 - fraction_mixed_layer);
                                    young_SOM_kg[uphrow, uphcol, lay] *= (1 - fraction_mixed_layer);
                                    if (CN_checkbox.Checked)
                                    {
                                        for (int cosmo = 0; cosmo < n_cosmo; cosmo++)
                                        {
                                            double transport = CN_atoms_cm2[uphrow, uphcol, lay, cosmo] * fraction_mixed_layer;
                                            upheaved_cosmo_nuclides[cosmo] += transport;
                                            CN_atoms_cm2[uphrow, uphcol, lay, cosmo] -= transport;
                                        }
                                    }
                                }
                                // Give material back to layers, based on their given mass to mixture and total mixed mass
                                for (int lay = 0; lay <= completelayers; lay++)
                                {
                                    for (int tex = 0; tex < 5; tex++)
                                    {
                                        texture_kg[uphrow, uphcol, lay, tex] += upheaved_text[tex] * upheaved_mass[lay] / upheaved_mass.Sum();

                                    }
                                    old_SOM_kg[uphrow, uphcol, lay] += upheaved_om[0] * upheaved_mass[lay] / upheaved_mass.Sum();
                                    young_SOM_kg[uphrow, uphcol, lay] += upheaved_om[1] * upheaved_mass[lay] / upheaved_mass.Sum();

                                    if (CN_checkbox.Checked)
                                    {
                                        for (int cosmo = 0; cosmo < n_cosmo; cosmo++)
                                        {
                                            CN_atoms_cm2[uphrow, uphcol, lay, cosmo] += upheaved_cosmo_nuclides[cosmo] * upheaved_mass[lay] / upheaved_mass.Sum();
                                        }
                                    }

                                    layerthickness_m[uphrow, uphcol, lay] = thickness_calc(uphrow, uphcol, lay);
                                    layerthickness_m[uphrow, uphcol, lay] = thickness_calc(uphrow, uphcol, lay);
                                    newdepth += layerthickness_m[uphrow, uphcol, lay];
                                }
                                if (OSL_checkbox.Checked) // Mix grains from the top layers
                                {
                                    int totalgrains_start = 0;
                                    for (int lay = 0; lay < max_soil_layers; lay++) { totalgrains_start += OSL_grainages[uphrow, uphcol, lay].Length; }
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
                                        if (OSL_grainages[uphrow, uphcol, lay].Length > 0)
                                        {
                                            for (int ind = 0; ind < OSL_grainages[uphrow, uphcol, lay].Length; ind++)
                                            {
                                                if ((randOslLayerMixing.Next(0, 10000) < P_mixing ? 1 : 0) == 1)
                                                {
                                                    mixedgrains.Add(OSL_grainages[uphrow, uphcol, lay][ind]);
                                                    mixedgrains_da.Add(OSL_depositionages[uphrow, uphcol, lay][ind]);
                                                    mixedgrains_su.Add(OSL_surfacedcount[uphrow, uphcol, lay][ind]);
                                                    grains_from_layer[lay] += 1;
                                                }
                                                else
                                                {
                                                    grains_staying_behind.Add(OSL_grainages[uphrow, uphcol, lay][ind]);
                                                    grains_staying_behind_da.Add(OSL_depositionages[uphrow, uphcol, lay][ind]);
                                                    grains_staying_behind_su.Add(OSL_surfacedcount[uphrow, uphcol, lay][ind]);
                                                }
                                            }
                                        }
                                        OSL_grainages[uphrow, uphcol, lay] = grains_staying_behind.ToArray(); // Preserve the grains that stay behind
                                        OSL_depositionages[uphrow, uphcol, lay] = grains_staying_behind_da.ToArray(); // 
                                        OSL_surfacedcount[uphrow, uphcol, lay] = grains_staying_behind_su.ToArray(); // 
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
                                        newgrains.AddRange(OSL_grainages[uphrow, uphcol, lay]);
                                        OSL_grainages[uphrow, uphcol, lay] = newgrains.ToArray();
                                        newgrains = mixedgrains_da.GetRange(count, grains_from_layer[lay]);
                                        newgrains.AddRange(OSL_depositionages[uphrow, uphcol, lay]);
                                        OSL_depositionages[uphrow, uphcol, lay] = newgrains.ToArray();

                                        newgrains = mixedgrains_su.GetRange(count, grains_from_layer[lay]);
                                        newgrains.AddRange(OSL_surfacedcount[uphrow, uphcol, lay]);
                                        OSL_surfacedcount[uphrow, uphcol, lay] = newgrains.ToArray();
                                        count += grains_from_layer[lay];
                                    }
                                    int totalgrains_end = 0;
                                    for (int lay = 0; lay < max_soil_layers; lay++) { totalgrains_end += OSL_grainages[uphrow, uphcol, lay].Length; }
                                    if (totalgrains_start != totalgrains_end) { Debugger.Break(); }
                                }

                                decimal mass_soil_after = total_soil_mass_kg_decimal(uphrow, uphcol);
                                if (Math.Abs(mass_soil_before - mass_soil_after) > Convert.ToDecimal(0.0001))
                                {
                                    Debug.WriteLine("err_ti2");
                                }
                            }
                        }
                    }
                    else
                    {
                        if (dtm[uphrow, uphcol] != nodata_value & soildepth_m[uphrow, uphcol] > 0)
                        {
                            if (uphrow == 617 && uphcol == 560) { Debug.WriteLine(" updating layer thicknesses"); }
                            update_all_layer_thicknesses(uphrow, uphcol);

                            double mixeddepth = 0, completelayerdepth = 0, newdepth = 0;
                            int completelayers = -1;

                            // limit upheaval to available soil
                            local_soil_depth_m = total_soil_thickness(uphrow, uphcol);
                            uph_depth = Math.Min(uph_depth, local_soil_depth_m);
                            //if (uphrow == 617 && uphcol == 560) { Debug.WriteLine(" total depth now " + local_soil_depth_m); }
                            while (mixeddepth <= uph_depth & completelayers < (max_soil_layers - 1))
                            {
                                completelayers++;
                                mixeddepth += layerthickness_m[uphrow, uphcol, completelayers];
                                //if (uphrow == 617 && uphcol == 560) { Debug.WriteLine(" mixed depth now " + mixeddepth); }
                                // OSL_age[uphrow, uphcol, completelayers] = 0;

                            }// this will lead to incorporation of the (partial) layer below tillage horizon in completelayers parameter. So the highest number indicates the partial layer 
                             // Debug.WriteLine("till2");
                            double[] upheaved_text = new double[5]; // contains soil 
                            double[] upheaved_om = new double[2]; // contains OM
                            double[] alldepths = new double[completelayers + 1]; // contains thicknesses of all layers
                            double[] upheaved_mass = new double[completelayers + 1];
                            double[] fraction_mixed = new double[completelayers + 1];
                            double[] upheaved_cosmo_nuclides = new double[n_cosmo]; // contains cosmogenic nuclides

                            // take material from layers to mix
                            decimal mass_soil_before = total_soil_mass_kg_decimal(uphrow, uphcol);
                            double fraction_mixed_layer;
                            //if (uphrow == 617 && uphcol == 560) { Debug.WriteLine(" taking material from layers"); }
                            for (int lay = 0; lay <= completelayers; lay++) // Includes partial layer, will be selective taken up
                            {
                                if (layerthickness_m[uphrow, uphcol, lay] > 0)
                                {
                                    if ((completelayerdepth + layerthickness_m[uphrow, uphcol, lay]) < uph_depth)
                                    {
                                        fraction_mixed_layer = 1;
                                        fraction_mixed[lay] = 1;
                                        alldepths[lay] = layerthickness_m[uphrow, uphcol, lay];
                                    }
                                    else
                                    {
                                        fraction_mixed_layer = (uph_depth - completelayerdepth) / layerthickness_m[uphrow, uphcol, lay];
                                        fraction_mixed[lay] = fraction_mixed_layer; // fraction of layer that is mixed
                                        alldepths[lay] = layerthickness_m[uphrow, uphcol, lay] * fraction_mixed_layer; // part of layer [m] that is considered
                                    }
                                    //if (uphrow == 617 && uphcol == 560 && lay == 2) { Debug.WriteLine(" layer 2 has thickness of " + layerthickness_m[uphrow, uphcol, lay] + " using fraction of " + fraction_mixed[lay]); }

                                    completelayerdepth += layerthickness_m[uphrow, uphcol, lay];
                                    for (int tex = 0; tex < 5; tex++)
                                    {
                                        upheaved_text[tex] += texture_kg[uphrow, uphcol, lay, tex] * fraction_mixed_layer;
                                        upheaved_mass[lay] += texture_kg[uphrow, uphcol, lay, tex] * fraction_mixed_layer;
                                        texture_kg[uphrow, uphcol, lay, tex] *= (1 - fraction_mixed_layer);
                                        //if (uphrow == 617 && uphcol == 560) { Debug.WriteLine(" gave material to upheaved mass sum is " + upheaved_mass.Sum()); }
                                    }
                                    upheaved_om[0] += old_SOM_kg[uphrow, uphcol, lay] * fraction_mixed_layer;
                                    upheaved_om[1] += young_SOM_kg[uphrow, uphcol, lay] * fraction_mixed_layer;
                                    upheaved_mass[lay] += (old_SOM_kg[uphrow, uphcol, lay] * fraction_mixed_layer + young_SOM_kg[uphrow, uphcol, lay] * fraction_mixed_layer);
                                    old_SOM_kg[uphrow, uphcol, lay] *= (1 - fraction_mixed_layer);
                                    young_SOM_kg[uphrow, uphcol, lay] *= (1 - fraction_mixed_layer);
                                    //if (uphrow == 617 && uphcol == 560) { Debug.WriteLine(" gave OM to upheaved mass sum is " + upheaved_mass.Sum()); }
                                    if (CN_checkbox.Checked)
                                    {
                                        for (int cosmo = 0; cosmo < n_cosmo; cosmo++)
                                        {
                                            double transport = CN_atoms_cm2[uphrow, uphcol, lay, cosmo] * fraction_mixed_layer;
                                            upheaved_cosmo_nuclides[cosmo] += transport;
                                            CN_atoms_cm2[uphrow, uphcol, lay, cosmo] -= transport;
                                        }
                                    }

                                }
                            }
                            // Give material back to layers, based on their given mass to mixture and total mixed mass
                            //if (uphrow == 617 && uphcol == 560) { Debug.WriteLine(" giving material to layers, upheaved mass sum is " + upheaved_mass.Sum()); }
                            for (int lay = 0; lay <= completelayers; lay++)
                            {
                                for (int tex = 0; tex < 5; tex++)
                                {
                                    texture_kg[uphrow, uphcol, lay, tex] += upheaved_text[tex] * upheaved_mass[lay] / upheaved_mass.Sum();

                                }
                                old_SOM_kg[uphrow, uphcol, lay] += upheaved_om[0] * upheaved_mass[lay] / upheaved_mass.Sum();
                                young_SOM_kg[uphrow, uphcol, lay] += upheaved_om[1] * upheaved_mass[lay] / upheaved_mass.Sum();

                                if (CN_checkbox.Checked)
                                {
                                    for (int cosmo = 0; cosmo < n_cosmo; cosmo++)
                                    {
                                        CN_atoms_cm2[uphrow, uphcol, lay, cosmo] += upheaved_cosmo_nuclides[cosmo] * upheaved_mass[lay] / upheaved_mass.Sum();
                                    }
                                }

                                //if (uphrow == 617 && uphcol == 560) { Debug.WriteLine(" calculating layer thickness"); }
                                layerthickness_m[uphrow, uphcol, lay] = thickness_calc(uphrow, uphcol, lay);
                                layerthickness_m[uphrow, uphcol, lay] = thickness_calc(uphrow, uphcol, lay);
                                newdepth += layerthickness_m[uphrow, uphcol, lay];
                            }
                            if (OSL_checkbox.Checked) // Mix grains from the top layers
                            {
                                int totalgrains_start = 0;
                                for (int lay = 0; lay < max_soil_layers; lay++) { totalgrains_start += OSL_grainages[uphrow, uphcol, lay].Length; }
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
                                    if (OSL_grainages[uphrow, uphcol, lay].Length > 0)
                                    {
                                        for (int ind = 0; ind < OSL_grainages[uphrow, uphcol, lay].Length; ind++)
                                        {
                                            if ((randOslLayerMixing.Next(0, 10000) < P_mixing ? 1 : 0) == 1)
                                            {
                                                mixedgrains.Add(OSL_grainages[uphrow, uphcol, lay][ind]);
                                                mixedgrains_da.Add(OSL_depositionages[uphrow, uphcol, lay][ind]);
                                                mixedgrains_su.Add(OSL_surfacedcount[uphrow, uphcol, lay][ind]);
                                                grains_from_layer[lay] += 1;
                                            }
                                            else
                                            {
                                                grains_staying_behind.Add(OSL_grainages[uphrow, uphcol, lay][ind]);
                                                grains_staying_behind_da.Add(OSL_depositionages[uphrow, uphcol, lay][ind]);
                                                grains_staying_behind_su.Add(OSL_surfacedcount[uphrow, uphcol, lay][ind]);
                                            }
                                        }
                                    }
                                    OSL_grainages[uphrow, uphcol, lay] = grains_staying_behind.ToArray(); // Preserve the grains that stay behind
                                    OSL_depositionages[uphrow, uphcol, lay] = grains_staying_behind_da.ToArray(); // 
                                    OSL_surfacedcount[uphrow, uphcol, lay] = grains_staying_behind_su.ToArray(); // 
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
                                    newgrains.AddRange(OSL_grainages[uphrow, uphcol, lay]);
                                    OSL_grainages[uphrow, uphcol, lay] = newgrains.ToArray();
                                    newgrains = mixedgrains_da.GetRange(count, grains_from_layer[lay]);
                                    newgrains.AddRange(OSL_depositionages[uphrow, uphcol, lay]);
                                    OSL_depositionages[uphrow, uphcol, lay] = newgrains.ToArray();

                                    newgrains = mixedgrains_su.GetRange(count, grains_from_layer[lay]);
                                    newgrains.AddRange(OSL_surfacedcount[uphrow, uphcol, lay]);
                                    OSL_surfacedcount[uphrow, uphcol, lay] = newgrains.ToArray();
                                    count += grains_from_layer[lay];
                                }
                                int totalgrains_end = 0;
                                for (int lay = 0; lay < max_soil_layers; lay++) { totalgrains_end += OSL_grainages[uphrow, uphcol, lay].Length; }
                                if (totalgrains_start != totalgrains_end) { Debugger.Break(); }
                            }

                            decimal mass_soil_after = total_soil_mass_kg_decimal(uphrow, uphcol);
                            if (Math.Abs(mass_soil_before - mass_soil_after) > Convert.ToDecimal(0.0001))
                            {
                                Debug.WriteLine("err_ti2");
                            }
                        }
                    }
                }
            }
            catch
            {
                Debug.WriteLine(" Error in tillage upheaval calculations in timestep {0}", t);
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
                    layer_bio_activity_index = Math.Exp(-depth/bioturbation_decay_depth_m) - (Math.Exp(-(depth + layerthickness_m[row, col, layer])/ bioturbation_decay_depth_m)); //AleG_june26
                    total_bio_activity_index = 1 - (Math.Exp(- total_soil_thickness_m/ bioturbation_decay_depth_m)); //AleG_june26
                }

                if (bt_depth_function == 1)
                {
                    // Gradational
                    // bioturbation_depth_decay_constant = 1;
                    depth_upp = depth;
                    depth_low = depth + layerthickness_m[row, col, layer];
                    if (depth_upp < 1 / bioturbation_decay_depth_m) // Is the upper part of the layer still under the mixing limit?
                    {
                        if (depth_low > (1 / bioturbation_decay_depth_m))
                        {
                            depth_low = 1 / bioturbation_decay_depth_m; // if the lower boundary is below the mixing limit, reset it to the mixing limit
                        }
                        layer_bio_activity_index = -bioturbation_decay_depth_m / 2 * (Math.Pow(depth_low, 2) - Math.Pow(depth_upp, 2)) + ((depth_low) - depth_upp);
                        total_bio_activity_index = -bioturbation_decay_depth_m / 2 * Math.Pow(1 / bioturbation_decay_depth_m, 2) + 1 / bioturbation_decay_depth_m;
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
                    if (depth_upp < bioturbation_decay_depth_m) // Is the upper part of the layer still under the mixing limit?
                    {
                        if (depth_low > bioturbation_decay_depth_m)
                        {
                            depth_low = bioturbation_decay_depth_m; // if the lower boundary is below the mixing limit, reset it to the mixing limit
                        }
                        layer_bio_activity_index = depth_low - depth_upp;
                        total_bio_activity_index = bioturbation_decay_depth_m;
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

        void soil_carbon_cycle()
        {
            try
            {
                double local_OM_input_kg, layer_OM_input_kg;
                double young_decomposition_rate, old_decomposition_rate;
                double young_midpoint_m, old_midpoint_m;
                //Debug.WriteLine("succesfully read parameters for soil SOM");
                double depth;
                double total_soil_thickness;
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
                    {// update soil thickni
                        total_soil_thickness = 0;
                        if (Proglacial_checkbox.Checked)
                        {
                            if (glacier_cell[row, col] != 1)
                            {
                                for (layer = 0; layer < max_soil_layers; layer++)
                                {
                                    if (layerthickness_m[row, col, layer] > 0)
                                    {
                                        total_soil_thickness += layerthickness_m[row, col, layer];
                                    }
                                }
                                local_OM_input_kg = potential_OM_input * (1 - Math.Exp(- total_soil_thickness / OM_input_decay_depth_m)) * dx * dx * dt;
                                total_OM_input_kg += local_OM_input_kg;
                                depth = 0;

                                for (layer = 0; layer < max_soil_layers; layer++)
                                {
                                    if (layerthickness_m[row, col, layer] > 0)
                                    {
                                        // if (layer == 0) { Debugger.Break(); }
                                        layer_OM_input_kg = activity_fraction(OM_input_decay_depth_m, total_soil_thickness, depth, depth + layerthickness_m[row, col, layer]) * local_OM_input_kg;

                                        //decomposition gets lost as CO2 to the air (and soil water)

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
                                            //old_SOM_kg[row, col, layer] += humification_fraction * (young_SOM_kg[row, col, layer] * potential_young_decomp_rate * 1 * Math.Exp(depth / young_OM_decomp_char_decay_depth_m)); //AleG_0825 
                                            old_SOM_kg[row, col, layer] += humification_fraction * (young_SOM_kg[row, col, layer] * potential_young_decomp_rate * 1 * Math.Exp(depth / young_OM_decomp_char_decay_depth_m)); //AleG_0825 quiii
                                        }
                                        if (double.IsNaN(young_SOM_kg[row, col, layer]))
                                        {
                                            Debug.WriteLine("err_cc2");
                                        }

                                        young_midpoint_m = layer_midpoint_m(young_OM_decomp_char_decay_depth_m, depth, depth + layerthickness_m[row, col, layer]);
                                        old_midpoint_m = layer_midpoint_m(old_OM_decomp_char_decay_depth_m, depth, depth + layerthickness_m[row, col, layer]);
                                        young_decomposition_rate = potential_young_decomp_rate * Math.Exp(-(depth + young_midpoint_m) / young_OM_decomp_char_decay_depth_m); //AleG_june26
                                        old_decomposition_rate = potential_old_decomp_rate * Math.Exp(-(depth + old_midpoint_m) / old_OM_decomp_char_decay_depth_m); //AleG_june26


                                        young_SOM_kg[row, col, layer] *= (1 - young_decomposition_rate);
                                        old_SOM_kg[row, col, layer] *= (1 - old_decomposition_rate);
                                        //Debug.WriteLine(" cell  " + row + " " + col + " has layer_OM_input of " + layer_OM_input_kg);
                                        depth += layerthickness_m[row, col, layer];
                                        if (young_SOM_kg[row, col, layer] < 0 | old_SOM_kg[row, col, layer] < 0)
                                        {
                                            Debug.WriteLine("err_cc3");
                                        }
                                    }

                                }
                            }
                        }

                        else
                        {
                            for (layer = 0; layer < max_soil_layers; layer++)
                            {
                                if (layerthickness_m[row, col, layer] > 0)
                                {
                                    total_soil_thickness += layerthickness_m[row, col, layer];
                                }
                            }
                            local_OM_input_kg = potential_OM_input * (1 - Math.Exp(-(total_soil_thickness/ OM_input_decay_depth_m))) * dx * dx * dt; //AleG_Apr26
                            total_OM_input_kg += local_OM_input_kg;
                            depth = 0;

                            for (layer = 0; layer < max_soil_layers; layer++)
                            {
                                if (layerthickness_m[row, col, layer] > 0)
                                {
                                    // if (layer == 0) { Debugger.Break(); }
                                    layer_OM_input_kg = activity_fraction(OM_input_decay_depth_m, total_soil_thickness, depth, depth + layerthickness_m[row, col, layer]) * local_OM_input_kg;

                                    //decomposition gets lost as CO2 to the air (and soil water)

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
                                        //old_SOM_kg[row, col, layer] += humification_fraction * (young_SOM_kg[row, col, layer] * potential_young_decomp_rate * 1 * Math.Exp(depth / young_OM_decomp_char_decay_depth_m)); //AleG_0825 
                                        old_SOM_kg[row, col, layer] += humification_fraction * (young_SOM_kg[row, col, layer] * potential_young_decomp_rate * 1 * Math.Exp(depth / young_OM_decomp_char_decay_depth_m)); //AleG_0825 quiii
                                    }
                                    if (double.IsNaN(young_SOM_kg[row, col, layer]))
                                    {
                                        Debug.WriteLine("err_cc2");
                                    }

                                    young_midpoint_m = layer_midpoint_m(young_OM_decomp_char_decay_depth_m, depth, depth + layerthickness_m[row, col, layer]);
                                    old_midpoint_m = layer_midpoint_m(old_OM_decomp_char_decay_depth_m, depth, depth + layerthickness_m[row, col, layer]);
                                    young_decomposition_rate = potential_young_decomp_rate * Math.Exp(-(depth + young_midpoint_m)/ young_OM_decomp_char_decay_depth_m); //AleG_june26
                                    old_decomposition_rate = potential_old_decomp_rate * Math.Exp(-(depth + old_midpoint_m)/ old_OM_decomp_char_decay_depth_m ); //AleG_june26


                                    young_SOM_kg[row, col, layer] *= (1 - young_decomposition_rate);
                                    old_SOM_kg[row, col, layer] *= (1 - old_decomposition_rate);
                                    //Debug.WriteLine(" cell  " + row + " " + col + " has layer_OM_input of " + layer_OM_input_kg);
                                    depth += layerthickness_m[row, col, layer];
                                    if (young_SOM_kg[row, col, layer] < 0 | old_SOM_kg[row, col, layer] < 0)
                                    {
                                        Debug.WriteLine("err_cc3");
                                    }
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

                        if (Proglacial_checkbox.Checked)
                        {
                            if (glacier_cell[row, col] != 1)
                            {
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
                        else
                        {
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
                        if (Proglacial_checkbox.Checked)
                        {
                            if (glacier_cell[row, col] != 1)
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
                        else
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
                        if (Proglacial_checkbox.Checked)
                        {
                            if (glacier_cell[row, col] != 1)
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
                        else
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
            }
            catch
            {
                MessageBox.Show("error in decalcification");
            }
        }

    }
}
