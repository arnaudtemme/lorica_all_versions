using MathNet.Numerics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
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

            Debug.WriteLine("phys weath of soil started");

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
            Debug.WriteLine("chem weath of soil started");
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

        // -------------------------
        // Support types (add inside your class)
        // -------------------------
        private struct Target
        {
            public int Idx;
            public double W;
            public Target(int idx, double w) { Idx = idx; W = w; }
        }

        private sealed class BtScratch
        {
            public List<Target> Targets;
            public double[] Mixture;            // length 7 (1..4 textures, 5 young SOM, 6 old SOM)
            public double[,] TextureOutTotal;   // used only when OSL is enabled

            // Per-cell precomputations
            public bool[] Exists;
            public double[] Thick, ZTop, ZBot, ZCtr, FineMass;

            public const double EpsLocal = 1e-16;
            public const double EpsPair = 1e-12;

            public BtScratch(int maxLayers, int nTextureClasses, bool preallocOSL)
            {
                Targets = new List<Target>(64);
                Mixture = new double[7];
                Exists = new bool[maxLayers];
                Thick = new double[maxLayers];
                ZTop = new double[maxLayers];
                ZBot = new double[maxLayers];
                ZCtr = new double[maxLayers];
                FineMass = new double[maxLayers];
                if (preallocOSL)
                    TextureOutTotal = new double[nTextureClasses, 4];
            }
        }

        // -------------------------
        // Optional tiny sequential fallback (small grids)
        // -------------------------
        private void RunBioturbationSequential()
        {
            bool proglacial = Proglacial_checkbox.Checked;
            bool doCN = CN_checkbox.Checked;
            bool doOSL = OSL_checkbox.Checked;

            total_mass_bioturbed_kg = 0.0;

            for (int r = 0; r < nr; r++)
            {
                for (int c = 0; c < nc; c++)
                {
                    if (proglacial && glacier_cell[r, c] == 1) continue;
                    double local = 0.0;
                    var scratch = new BtScratch(max_soil_layers, n_texture_classes, doOSL);
                    ProcessCellParallel(r, c, doCN, doOSL, scratch, ref local);
                    total_mass_bioturbed_kg += local;
                }
            }

            if (timeseries.total_mass_bioturbed_checkbox.Checked)
                timeseries_matrix[t, timeseries_order[27]] = total_mass_bioturbed_kg;
        }

        // -------------------------
        // Per‑cell worker (Fix 1 + optimizations)
        // -------------------------
        private void ProcessCellParallel(int row, int col, bool doCN, bool doOSL, BtScratch s, ref double localTotalBtKg)
        {
            if (dtm[row, col] == nodata_value || soildepth_m[row, col] <= 0) return;

            remove_empty_layers(row, col);
            update_all_layer_thicknesses(row, col);

            // Precompute per-cell layer properties
            double total_soil_thickness_m = 0.0;
            for (int k = 0; k < max_soil_layers; k++)
            {
                double mLayer = total_layer_mass_kg(row, col, k);
                s.Exists[k] = mLayer > 0.0;
                double th = layerthickness_m[row, col, k];
                s.Thick[k] = th;

                if (s.Exists[k])
                {
                    double fm =
                        texture_kg[row, col, k, 1] + texture_kg[row, col, k, 2] +
                        texture_kg[row, col, k, 3] + texture_kg[row, col, k, 4] +
                        young_SOM_kg[row, col, k] + old_SOM_kg[row, col, k];
                    s.FineMass[k] = fm;
                    total_soil_thickness_m += th;
                }
                else s.FineMass[k] = 0.0;
            }

            // Build depths
            double z = 0.0;
            for (int k = 0; k < max_soil_layers; k++)
            {
                s.ZTop[k] = z;
                s.ZBot[k] = z + s.Thick[k];
                s.ZCtr[k] = z + 0.5 * s.Thick[k];
                z = s.ZBot[k];
            }

            // Snapshot to temp buffers
            double[,] temp = new double[max_soil_layers, 7];
            for (int k = 0; k < max_soil_layers; k++)
            {
                if (!s.Exists[k]) continue;
                for (int p = 0; p < 5; p++) temp[k, p] = texture_kg[row, col, k, p];
                temp[k, 5] = young_SOM_kg[row, col, k];
                temp[k, 6] = old_SOM_kg[row, col, k];
            }

            // Conservation baselines
            decimal mass_before = total_soil_mass_kg_decimal(row, col);
            double top_before = total_layer_mass_kg(row, col, 0);

            // Total bioturbation mass for this cell
            double local_bt =
                potential_bt_mixing_kg_m2_y *
                (1 - Math.Exp(-bioturbation_decay_depth_m * total_soil_thickness_m)) *
                dx * dx * dt;

            if (local_bt <= BtScratch.EpsLocal) goto Commit;

            localTotalBtKg += local_bt;

            // Secondary decay
            double dd_bt_m = bioturbation_decay_depth_m * 2.0;
            if (doOSL && s.TextureOutTotal == null)
                s.TextureOutTotal = new double[n_texture_classes, 4];

            // Source layer loop
            for (int layer = 0; layer < max_soil_layers; layer++)
            {
                if (!s.Exists[layer] || s.FineMass[layer] <= 0.0) continue;

                double layer_bt = activity_fraction(
                    bioturbation_decay_depth_m, total_soil_thickness_m, s.ZTop[layer], s.ZBot[layer]) * local_bt;

                if (layer_bt <= BtScratch.EpsLocal) continue;

                double zc = s.ZCtr[layer];

                // Global index (as in your original code)
                double total_index =
                    -1.0 / dd_bt_m * (Math.Exp(-dd_bt_m * (zc - 0.0)) - 1.0) +
                    -1.0 / dd_bt_m * (Math.Exp(-dd_bt_m * (total_soil_thickness_m - zc)) - 1.0);

                // Build weights for other layers (exclude self), normalize by total_index
                s.Targets.Clear();
                double wsum = 0.0;

                for (int other = 0; other < max_soil_layers; other++)
                {
                    if (other == layer || !s.Exists[other] || s.FineMass[other] <= 0.0) continue;

                    double w;
                    if (other < layer)
                        w = -1.0 / dd_bt_m * (Math.Exp(-(zc - s.ZTop[other]) / dd_bt_m) - Math.Exp(-(zc - s.ZBot[other]) / dd_bt_m));
                    else
                        w = -1.0 / dd_bt_m * (Math.Exp(-(s.ZBot[other] - zc) / dd_bt_m) - Math.Exp(-(s.ZTop[other] - zc) / dd_bt_m));

                    if (w <= 0.0 || total_index <= 0.0) continue;

                    double wn = w / total_index;
                    if (wn <= 0.0) continue;

                    s.Targets.Add(new Target(other, wn));
                    wsum += wn;
                }

                if (wsum <= 0.0) continue;

                // Fix 1: renormalize over actual targets so weights sum to 1
                for (int i = 0; i < s.Targets.Count; i++)
                {
                    var t = s.Targets[i];
                    t.W = t.W / wsum;
                    s.Targets[i] = t;
                }

                // Pairwise exchanges
                for (int ti = 0; ti < s.Targets.Count; ti++)
                {
                    int otherlayer = s.Targets[ti].Idx;
                    double pij = s.Targets[ti].W;

                    double inter_bt = layer_bt * pij;

                    // Cap by available fine mass in each donor
                    double from_l = Math.Min(s.FineMass[layer], inter_bt * 0.5);
                    double from_ol = Math.Min(s.FineMass[otherlayer], inter_bt * 0.5);

                    if ((from_l + from_ol) <= BtScratch.EpsPair) continue;

                    // Reset mixture
                    for (int p = 0; p < 7; p++) s.Mixture[p] = 0.0;
                    if (doOSL)
                        for (int p = 0; p < n_texture_classes; p++)
                            for (int c = 0; c < 4; c++)
                                s.TextureOutTotal[p, c] = 0.0;

                    double mass_l = 0.0, mass_ol = 0.0;

                    // Texture 1..4
                    for (int prop = 1; prop < 5; prop++)
                    {
                        double d_l = (from_l / Math.Max(s.FineMass[layer], 1e-30)) * texture_kg[row, col, layer, prop];
                        double d_ol = (from_ol / Math.Max(s.FineMass[otherlayer], 1e-30)) * texture_kg[row, col, otherlayer, prop];

                        d_l = Math.Min(d_l, temp[layer, prop]);
                        d_ol = Math.Min(d_ol, temp[otherlayer, prop]);

                        s.Mixture[prop] += (d_l + d_ol);
                        mass_l += d_l;
                        mass_ol += d_ol;

                        temp[layer, prop] -= d_l;
                        temp[otherlayer, prop] -= d_ol;

                        if (doOSL)
                        {
                            s.TextureOutTotal[prop, 0] = d_l;
                            s.TextureOutTotal[prop, 1] = texture_kg[row, col, layer, prop];
                            s.TextureOutTotal[prop, 2] = d_ol;
                            s.TextureOutTotal[prop, 3] = texture_kg[row, col, otherlayer, prop];
                        }
                    }

                    // Young SOM (5)
                    {
                        double d_l = (from_l / Math.Max(s.FineMass[layer], 1e-30)) * young_SOM_kg[row, col, layer];
                        double d_ol = (from_ol / Math.Max(s.FineMass[otherlayer], 1e-30)) * young_SOM_kg[row, col, otherlayer];

                        d_l = Math.Min(d_l, temp[layer, 5]);
                        d_ol = Math.Min(d_ol, temp[otherlayer, 5]);

                        s.Mixture[5] += (d_l + d_ol);
                        mass_l += d_l;
                        mass_ol += d_ol;

                        temp[layer, 5] -= d_l;
                        temp[otherlayer, 5] -= d_ol;
                    }

                    // Old SOM (6)
                    {
                        double d_l = (from_l / Math.Max(s.FineMass[layer], 1e-30)) * old_SOM_kg[row, col, layer];
                        double d_ol = (from_ol / Math.Max(s.FineMass[otherlayer], 1e-30)) * old_SOM_kg[row, col, otherlayer];

                        d_l = Math.Min(d_l, temp[layer, 6]);
                        d_ol = Math.Min(d_ol, temp[otherlayer, 6]);

                        s.Mixture[6] += (d_l + d_ol);
                        mass_l += d_l;
                        mass_ol += d_ol;

                        temp[layer, 6] -= d_l;
                        temp[otherlayer, 6] -= d_ol;
                    }

                    // Return mixture proportional to donations
                    double pool = mass_l + mass_ol;
                    if (pool > 0.0)
                    {
                        double w_to_l = mass_l / pool;
                        double w_to_ol = mass_ol / pool;

                        for (int prop = 1; prop < 7; prop++)
                        {
                            temp[layer, prop] += s.Mixture[prop] * w_to_l;
                            temp[otherlayer, prop] += s.Mixture[prop] * w_to_ol;
                        }

                        if (doCN)
                        {
                            for (int cn = 0; cn < n_cosmo; cn++)
                            {
                                double d_CN_l = (mass_l / Math.Max(s.FineMass[layer], 1e-30)) * CN_atoms_cm2[row, col, layer, cn];
                                double d_CN_ol = (mass_ol / Math.Max(s.FineMass[otherlayer], 1e-30)) * CN_atoms_cm2[row, col, otherlayer, cn];
                                double cn_pool = d_CN_l + d_CN_ol;

                                CN_atoms_cm2[row, col, layer, cn] += (-d_CN_l + cn_pool * w_to_l);
                                CN_atoms_cm2[row, col, otherlayer, cn] += (-d_CN_ol + cn_pool * w_to_ol);
                            }
                        }

                        if (doOSL)
                        {
                            double prob_layer = s.TextureOutTotal[1, 0] / Math.Max(s.TextureOutTotal[1, 1], 1e-30);
                            double prob_otherlayer = s.TextureOutTotal[1, 2] / Math.Max(s.TextureOutTotal[1, 3], 1e-30);
                            transfer_OSL_grains(row, col, layer, row, col, otherlayer, prob_layer, prob_otherlayer);
                        }
                    }
                }
            }

        Commit:
            // Write back
            for (int k = 0; k < max_soil_layers; k++)
            {
                if (!s.Exists[k]) continue;
                for (int p = 1; p < 5; p++)
                    texture_kg[row, col, k, p] = temp[k, p];
                young_SOM_kg[row, col, k] = temp[k, 5];
                old_SOM_kg[row, col, k] = temp[k, 6];
            }

            // Conservation check
            decimal mass_after = total_soil_mass_kg_decimal(row, col);
            double top_after = total_layer_mass_kg(row, col, 0);

            double dTop = Math.Abs(top_before - top_after);
            double dTot = Math.Abs((double)(mass_before - mass_after));
            double tolT = Math.Max(1e-9 * Math.Max(top_before, 1.0), 1e-8);
            double tolM = Math.Max(1e-9 * Math.Max((double)mass_before, 1.0), 1e-8);

            if (dTop > tolT || dTot > tolM)
                Debug.WriteLine("Mass loss during bioturbation");
        }

        // -------------------------
        // Public entry point with partitioner (Fix 1 + speedups)
        // -------------------------
        public void soil_bioturbation_mixing()
        {
            try
            {
                bool proglacial = Proglacial_checkbox.Checked;
                bool doCN = CN_checkbox.Checked;
                bool doOSL = OSL_checkbox.Checked;
                Debug.WriteLine("mixing bioturbation started");
                total_mass_bioturbed_kg = 0.0;

                int cores = Environment.ProcessorCount;
                var po = new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, cores - 1) };

                // Choose chunkRows and clamp
                int chunkRows = Math.Max(8, Math.Min(128, nr / (cores * 8)));
                chunkRows = Math.Max(1, Math.Min(chunkRows, nr));

                // Tiny grids: sequential is faster
                if (nr * nc < 10000)
                {
                    RunBioturbationSequential();
                    return;
                }

                var rangePartitioner = System.Collections.Concurrent.Partitioner.Create(0, nr, chunkRows);
                var threadTotals = new System.Threading.ThreadLocal<double>(() => 0.0, true);

                Parallel.ForEach<System.Tuple<int, int>, BtScratch>(
                    rangePartitioner,
                    po,
                    // localInit
                    () => new BtScratch(max_soil_layers, n_texture_classes, doOSL),
                    // body
                    (range, state, scratch) =>
                    {
                        double localTotal = 0.0;

                        for (int r = range.Item1; r < range.Item2; r++)
                        {
                            for (int c = 0; c < nc; c++)
                            {
                                if (proglacial && glacier_cell[r, c] == 1) continue;
                                ProcessCellParallel(r, c, doCN, doOSL, scratch, ref localTotal);
                            }
                        }

                        threadTotals.Value += localTotal;
                        return scratch;
                    },
                    // localFinally
                    scratch => { /* no-op */ }
                );

                // Combine thread locals
                double sum = 0.0;
                foreach (double v in threadTotals.Values) sum += v;
                total_mass_bioturbed_kg = sum;

                // Timeseries writeback
                if (timeseries.total_mass_bioturbed_checkbox.Checked)
                    timeseries_matrix[t, timeseries_order[27]] = total_mass_bioturbed_kg;

                if (NA_in_map(dtm) > 0 | NA_in_map(soildepth_m) > 0)
                    Debug.WriteLine("err_sbt20");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error in bioturbation calculations (parallel): " + ex.Message);
            }
        }
        
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
                                    local_bioturbation_kg = potential_bt_mounding_kg_m2_y * (1 - Math.Exp(-bioturbation_decay_depth_m * total_soil_thickness_m)) * dx * dx * dt;
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
                                local_bioturbation_kg = potential_bt_mounding_kg_m2_y * (1 - Math.Exp(-bioturbation_decay_depth_m * total_soil_thickness_m)) * dx * dx * dt;
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
                    layer_bio_activity_index = Math.Exp(-bioturbation_decay_depth_m * depth) - (Math.Exp(-bioturbation_decay_depth_m * (depth + layerthickness_m[row, col, layer])));
                    total_bio_activity_index = 1 - (Math.Exp(-bioturbation_decay_depth_m * total_soil_thickness_m));
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
                                        young_decomposition_rate = potential_young_decomp_rate * Math.Exp(-young_OM_decomp_char_decay_depth_m * (depth + young_midpoint_m));
                                        old_decomposition_rate = potential_old_decomp_rate * Math.Exp(-old_OM_decomp_char_decay_depth_m * (depth + old_midpoint_m));


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
                                    young_decomposition_rate = potential_young_decomp_rate * Math.Exp(-young_OM_decomp_char_decay_depth_m * (depth + young_midpoint_m));
                                    old_decomposition_rate = potential_old_decomp_rate * Math.Exp(-old_OM_decomp_char_decay_depth_m * (depth + old_midpoint_m));


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
