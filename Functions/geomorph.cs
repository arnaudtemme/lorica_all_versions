using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LORICA4
{
    public partial class Mother_form
    {
        #region Geomorphic processes code

        int NA_in_map(double[,] map)
        {
            int NA_count = 0;

            try
            {
                for (int rowNA = 0; rowNA < nr; rowNA++)
                {
                    for (int colNA = 0; colNA < nc; colNA++)
                    {
                        if (Double.IsNaN(map[rowNA, colNA]) | Double.IsInfinity(map[rowNA, colNA]))
                        {
                            NA_count++;
                            Debug.WriteLine("NA or inf at row {0}, col {1}", rowNA, colNA);
                            if (landslide_active)
                            {
                                Debug.WriteLine(" ls ero " + ero_slid_m[rowNA, colNA] + " ls dep " + sed_slid_m[rowNA, colNA]);
                                Debug.WriteLine(" ");
                            }
                        }
                    }
                }

            }
            catch
            {
                Debug.WriteLine("err_NAmap1");

            }
            return (NA_count);
        }

        bool find_gravel(int row_g, int col_g, int lay_g)
        {
            bool bool_out = false;
            if (texture_kg[row_g, col_g, lay_g, 0] > 0) { bool_out = true; }
            return (bool_out);
        }

        bool NA_in_soil(int rowNA, int colNA)
        {
            bool boolNA = false;
            for (int layNA = 0; layNA < max_soil_layers; layNA++)
            {
                for (int texNA = 0; texNA < 5; texNA++)
                {
                    if (Double.IsNaN(texture_kg[rowNA, colNA, layNA, texNA]) | Double.IsInfinity(texture_kg[rowNA, colNA, layNA, texNA]))
                    {
                        boolNA = true;
                        Debug.WriteLine("NA in row {0}, col {1}, lay {2}, tex {3}", rowNA, colNA, layNA, texNA);
                    }

                }
                if (Double.IsNaN(young_SOM_kg[rowNA, colNA, layNA]) | Double.IsInfinity(young_SOM_kg[rowNA, colNA, layNA]))
                {
                    boolNA = true;
                    Debug.WriteLine("NA in row {0}, col {1}, lay {2}, young OM, val {3}", rowNA, colNA, layNA, young_SOM_kg[rowNA, colNA, layNA]);
                }

                if (Double.IsNaN(old_SOM_kg[rowNA, colNA, layNA]) | Double.IsInfinity(old_SOM_kg[rowNA, colNA, layNA]))
                {
                    boolNA = true;
                    Debug.WriteLine("NA in row {0}, col {1}, lay {2}, old OM, val {3}", rowNA, colNA, layNA, old_SOM_kg[rowNA, colNA, layNA]);
                }
            }

            return (boolNA);
        }

        bool NA_anywhere_in_soil()
        {

            bool boolNA = false;
            try
            {
                for (int rowNA = 0; rowNA < nr; rowNA++)
                {
                    for (int colNA = 0; colNA < nc; colNA++)
                    {
                        if (NA_in_soil(rowNA, colNA) == true) { boolNA = true; }
                        //if (landslide_active) { Debug.WriteLine(" ls ero " + ero_slid_m[rowNA, colNA] + " ls dep " + sed_slid_m[rowNA, colNA]); }
                    }
                }

            }
            catch
            {
                Debug.WriteLine("err_NAmap1");

            }
            return (boolNA);
        }

        int NA_in_location(double[,] map, int rowNA, int colNA)
        {
            int NA_count = 0;
            try
            {
                if (Double.IsNaN(map[rowNA, colNA]) | Double.IsInfinity(map[rowNA, colNA]))
                {
                    NA_count++;
                    Debug.WriteLine("NA at row {0}, col {1}", rowNA, colNA);
                    if (landslide_active) { Debug.WriteLine(" ls ero " + ero_slid_m[rowNA, colNA] + " ls dep " + sed_slid_m[rowNA, colNA]); }
                }
            }
            catch
            {
                Debug.WriteLine("err_nal1");

            }
            return (NA_count);
        }

        void calculate_sediment_dynamics(int row_sd, int col_sd, int i_sd, int j_sd, double waterflow_m3_per_m, double flowfraction, double sum_of_fractions)
        {
            int size;
            double total_sediment_in_transport_kg, organic_in_transport, mass_to_be_eroded, rock_fraction, bio_fraction, vegetation_cover_fraction, selectivity_fraction, potential_transported_amount_kg, organic_selectivity_fraction, frac_eroded, frac_deposited;
            double[] total_mass_eroded, total_mass_deposited_kg;
            total_mass_eroded = new double[7] { 0, 0, 0, 0, 0, 0, 0 };
            total_mass_deposited_kg = new double[7] { 0, 0, 0, 0, 0, 0, 0 };
            transport_capacity_kg = advection_erodibility * (bulkdensity[row_sd, col_sd, 0] * dx * dx) * (Math.Pow(waterflow_m3_per_m, m) * Math.Pow(dh, n)); // in a departure from literature, the erosion threshold is only evaluated if erosion actually occurs
            if (transport_capacity_kg < 0)
            {
                transport_capacity_kg = 0;
                Debug.WriteLine(" Warning: negative transport capacity at" + row_sd + " " + col_sd);
            }  // this should never happen
               // We now compare transport_capacity with the total amount of sediment in transport, to determine whether we will have erosion or deposition or nothing
            total_sediment_in_transport_kg = 0;

            for (size = 0; size < n_texture_classes; size++)
            {
                total_sediment_in_transport_kg += flowfraction * sediment_in_transport_kg[row_sd, col_sd, size];                     //all in kg
            }

            // Transport
            if (transport_capacity_kg == total_sediment_in_transport_kg)
            {
                // neither erosion nor deposition, simply transport
                for (size = 0; size < n_texture_classes; size++)
                {
                    sediment_in_transport_kg[row_sd + i_sd, col_sd + j_sd, size] += flowfraction * sediment_in_transport_kg[row_sd, col_sd, size];  //all in kg 
                }
                old_SOM_in_transport_kg[row_sd + i_sd, col_sd + j_sd] += flowfraction * old_SOM_in_transport_kg[row_sd, col_sd];  //all in kg
                young_SOM_in_transport_kg[row_sd + i_sd, col_sd + j_sd] += flowfraction * young_SOM_in_transport_kg[row_sd, col_sd];  //all in kg

                if (CN_checkbox.Checked)
                {
                    // add fraction of all CNs to the receiving cell
                    for (int i_cn = 0; i_cn < n_cosmo; i_cn++)
                    {
                        CN_in_transport[row_sd + i_sd, col_sd + j_sd, i_cn] += CN_in_transport[row_sd, col_sd, i_cn] * flowfraction;
                    }
                }
                if (OSL_checkbox.Checked)
                {
                    transport_ero_sed_OSL_by_WE(row_sd, col_sd, i_sd, j_sd, sum_of_fractions, flowfraction, 0, 0, 0);
                }
            }

            // Erosion
            if (transport_capacity_kg > total_sediment_in_transport_kg)
            {
                //in case of desired erosion, we first evaluate whether we exceed the erosion threshold
                if ((transport_capacity_kg - total_sediment_in_transport_kg) > erosion_threshold_kg)
                {
                    //first, calculate how much we are going to erode. Not as much as we want to if the soil is protected by rocks or plants
                    rock_fraction = texture_kg[row_sd, col_sd, 0, 0] / (texture_kg[row_sd, col_sd, 0, 0] + texture_kg[row_sd, col_sd, 0, 1] + texture_kg[row_sd, col_sd, 0, 2] + texture_kg[row_sd, col_sd, 0, 3] + texture_kg[row_sd, col_sd, 0, 4]);
                    if (version_lux_checkbox.Checked == false)
                    {
                        mass_to_be_eroded = (transport_capacity_kg - total_sediment_in_transport_kg)
                        * Math.Exp(-rock_protection_constant * rock_fraction)
                        * Math.Exp(-bio_protection_constant);
                    }
                    else
                    {  //for Luxemburg version, here we additially protect soil from erosion by its cover of 'bad' organic matter as litter (i.e. in top layer)

                        // MvdM litter fraction is determined by the total amount of litter as fraction of the mineral soil in the top layer. This might be changed, because mineral content is variable and indepent of litter quantity
                        //XIA change this number to 0.25 as well. For creep and no creep
                        double litter_characteristic_protection_mass_kg_m2 = 0.9; // based on the maximum litter content in the model results
                        double litter_characteristic_protection_mass_kg = litter_characteristic_protection_mass_kg_m2 * dx * dx;
                        double litter_protection_fraction = (litter_kg[row_sd, col_sd, 0] + litter_kg[row_sd, col_sd, 1]) / litter_characteristic_protection_mass_kg;
                        // double litter_fraction = (litter_kg[row, col, 0] + litter_kg[row, col, 0]) / (litter_kg[row, col, 0] + litter_kg[row, col, 0] + total_layer_mass(row, col, 0));

                        //double litter_fraction = (old_SOM_kg[row, col, 0] + young_SOM_kg[row, col, 0]) / total_layer_mass(row, col, 0);
                        //LUX Xia you have to set this parameter here in the code. Value between 0-1.
                        // double litter_protection_constant = 0.5;

                        mass_to_be_eroded = (transport_capacity_kg - total_sediment_in_transport_kg)
                        * Math.Exp(-rock_protection_constant * rock_fraction)
                        * Math.Exp(-bio_protection_constant)
                        * Math.Exp(-litter_protection_fraction);
                    }
                    if (daily_water.Checked)
                    {
                        if (aridity_vegetation[row, col] >= 1) { vegetation_cover_fraction = 1; }
                        else { vegetation_cover_fraction = aridity_vegetation[row, col]; }
                        mass_to_be_eroded = (transport_capacity_kg - total_sediment_in_transport_kg)
                            * Math.Exp(-rock_protection_constant * rock_fraction)
                            * Math.Exp(-bio_protection_constant
                            * vegetation_cover_fraction);
                    }

                    // second, calculate how the mass to be eroded is taken from the different size fractions: selectivity
                    // if total transport capacity is small, only the finer fractions will be eroded (selectivity with diameter to power 0.5). For larger transport capacities, selectivity decreases (diameter to power 0 = equal between fractions)

                    double constant_b1 = 0.5 * Math.Exp(constant_selective_transcap * transport_capacity_kg);
                    double sum_diameter_power = 0;
                    for (size = 0; size < 5; size++)
                    {
                        sum_diameter_power += 1 / Math.Pow(upper_particle_size[size], constant_b1);
                    }
                    double clayeroded_0_kg = 0, claypresent_0_kg = 0, clayeroded_1_kg = 0, claypresent_1_kg = 0;
                    double silteroded_0_kg = 0, siltpresent_0_kg = 0, silteroded_1_kg = 0, siltpresent_1_kg = 0;
                    double sanderoded_0_kg = 0, sandpresent_0_kg = 0, sanderoded_1_kg = 0, sandpresent_1_kg = 0;
                    for (size = 0; size < 5; size++)
                    {
                        selectivity_fraction = (1 / Math.Pow(upper_particle_size[size], constant_b1)) / sum_diameter_power;    // unit [-]
                        if (texture_kg[row_sd, col_sd, 0, size] >= selectivity_fraction * mass_to_be_eroded)
                        {    // typical situation
                            if (size == 1)
                            {
                                sanderoded_0_kg = selectivity_fraction * mass_to_be_eroded;
                                sandpresent_0_kg = texture_kg[row_sd, col_sd, 0, size];
                            }
                            if (size > 2)
                            {
                                clayeroded_0_kg += selectivity_fraction * mass_to_be_eroded;
                                claypresent_0_kg += texture_kg[row_sd, col_sd, 0, size];
                            }
                            total_mass_eroded[size] += selectivity_fraction * mass_to_be_eroded;
                            texture_kg[row_sd, col_sd, 0, size] -= selectivity_fraction * mass_to_be_eroded;   // unit [kg]
                            sediment_in_transport_kg[row_sd + i_sd, col_sd + j_sd, size] += selectivity_fraction * mass_to_be_eroded;  // unit [kg
                        }
                        else
                        {    // exceptional. If we want to erode more than present in the layer, we will take it from one layer down.
                             //this is to avoid exceptionally thin rocky layers blocking all erosion
                             //we will then first erode everything from the top layer (layer "0") and then erode from the second layer  (i.e. layer "1").

                            // Layer 0
                            if (size == 1)
                            {
                                sanderoded_0_kg = texture_kg[row_sd, col_sd, 0, size];
                                sandpresent_0_kg = texture_kg[row_sd, col_sd, 0, size];
                            }
                            if (size == 2)
                            {
                                silteroded_0_kg = texture_kg[row_sd, col_sd, 0, size];
                                siltpresent_0_kg = texture_kg[row_sd, col_sd, 0, size];
                            }
                            if (size > 2)
                            {
                                clayeroded_0_kg += texture_kg[row_sd, col_sd, 0, size];
                                claypresent_0_kg += texture_kg[row_sd, col_sd, 0, size];
                            }
                            total_mass_eroded[size] += texture_kg[row_sd, col_sd, 0, size];
                            double left = (selectivity_fraction * mass_to_be_eroded) - texture_kg[row_sd, col_sd, 0, size]; // unit [kg]
                            sediment_in_transport_kg[row_sd + i_sd, col_sd + j_sd, size] += texture_kg[row_sd, col_sd, 0, size];
                            texture_kg[row_sd, col_sd, 0, size] = 0;

                            // Layer 1
                            if (max_soil_layers > 1)
                            {
                                if (texture_kg[row_sd, col_sd, 1, size] >= left)
                                {   // typical
                                    if (size == 1)
                                    {
                                        sanderoded_1_kg = left;
                                        sandpresent_1_kg = texture_kg[row_sd, col_sd, 1, size];
                                    }
                                    if (size == 2)
                                    {
                                        silteroded_1_kg = left;
                                        siltpresent_1_kg = texture_kg[row_sd, col_sd, 1, size];
                                    }
                                    if (size > 2)
                                    {
                                        clayeroded_1_kg += left;
                                        claypresent_1_kg += texture_kg[row_sd, col_sd, 1, size];
                                    }
                                    total_mass_eroded[size] += left;
                                    sediment_in_transport_kg[row_sd + i_sd, col_sd + j_sd, size] += left;   // unit [kg]
                                    texture_kg[row_sd, col_sd, 1, size] -= left;  // unit [kg]
                                }
                                else
                                {
                                    total_mass_eroded[size] += texture_kg[row_sd, col_sd, 1, size];
                                    if (size == 1)
                                    {
                                        sanderoded_1_kg = texture_kg[row_sd, col_sd, 1, size];
                                        sandpresent_1_kg = texture_kg[row_sd, col_sd, 1, size];
                                    }
                                    if (size == 2)
                                    {
                                        silteroded_1_kg = texture_kg[row_sd, col_sd, 1, size];
                                        siltpresent_1_kg = texture_kg[row_sd, col_sd, 1, size];
                                    }
                                    if (size > 2)
                                    {
                                        clayeroded_1_kg += texture_kg[row_sd, col_sd, 1, size];
                                        claypresent_1_kg += texture_kg[row_sd, col_sd, 1, size];
                                    }
                                    sediment_in_transport_kg[row_sd + i_sd, col_sd + j_sd, size] += texture_kg[row_sd, col_sd, 1, size];// unit [kg]
                                    texture_kg[row_sd, col_sd, 1, size] = 0;
                                }
                            }
                        }
                    }

                    //organic matter is eroded as a fraction of total OM. That fraction equals the fraction of clay eroded from the layer
                    //the assumption underlying this is that clay and humus are bound in aggregates
                    //this does not cover: LMW SOM, peat or large woody debris
                    double clayerodedfraction_0 = clayeroded_0_kg / claypresent_0_kg;
                    double clayerodedfraction_1 = clayeroded_1_kg / claypresent_1_kg;
                    double silterodedfraction_0 = silteroded_0_kg / siltpresent_0_kg;
                    double silterodedfraction_1 = silteroded_1_kg / siltpresent_1_kg;
                    double sanderodedfraction_0 = sanderoded_0_kg / sandpresent_0_kg;
                    double sanderodedfraction_1 = sanderoded_1_kg / sandpresent_1_kg;
                    if (Double.IsNaN(clayerodedfraction_0))
                    {
                        clayerodedfraction_0 = 0;
                        //Debug.WriteLine(" this should not have happened - no OM erosion possible");
                    }
                    if (Double.IsNaN(clayerodedfraction_1)) { clayerodedfraction_1 = 0; }
                    if (Double.IsNaN(sanderodedfraction_0)) { sanderodedfraction_0 = 0; }
                    if (Double.IsNaN(sanderodedfraction_1)) { sanderodedfraction_1 = 0; }
                    //if (row == 62 && col == 78) { Debug.WriteLine(clayerodedfraction_0 + "  " + clayerodedfraction_1); displaysoil(row, col); }
                    old_SOM_in_transport_kg[row_sd, col_sd] += old_SOM_kg[row_sd, col_sd, 0] * clayerodedfraction_0 + old_SOM_kg[row_sd, col_sd, 1] * clayerodedfraction_1;
                    young_SOM_in_transport_kg[row_sd, col_sd] += young_SOM_kg[row_sd, col_sd, 0] * clayerodedfraction_0 + young_SOM_kg[row_sd, col_sd, 1] * clayerodedfraction_1;
                    total_mass_eroded[5] += old_SOM_kg[row_sd, col_sd, 0] * clayerodedfraction_0 + old_SOM_kg[row_sd, col_sd, 1] * clayerodedfraction_1;
                    total_mass_eroded[6] += young_SOM_kg[row_sd, col_sd, 0] * clayerodedfraction_0 + young_SOM_kg[row_sd, col_sd, 1] * clayerodedfraction_1;
                    old_SOM_kg[row_sd, col_sd, 0] *= 1 - clayerodedfraction_0;
                    young_SOM_kg[row_sd, col_sd, 0] *= 1 - clayerodedfraction_0;
                    old_SOM_kg[row_sd, col_sd, 1] *= 1 - clayerodedfraction_1;
                    young_SOM_kg[row_sd, col_sd, 1] *= 1 - clayerodedfraction_1;

                    if (OSL_checkbox.Checked)
                    {
                        transport_ero_sed_OSL_by_WE(row_sd, col_sd, i_sd, j_sd, sum_of_fractions, flowfraction, sanderodedfraction_0, sanderodedfraction_1, 0);
                    }

                    if (CN_checkbox.Checked)
                    {
                        for (int i_cn = 0; i_cn < n_cosmo; i_cn++)
                        {
                            frac_eroded = 0;
                            // Erosion layer 0
                            if (i_cn == 0) { frac_eroded = clayerodedfraction_0; }
                            if (i_cn == 1) { frac_eroded = silterodedfraction_0; }
                            if (i_cn == 2 | i_cn == 3) { frac_eroded = sanderodedfraction_0; }
                            double CN_erosion = CN_atoms_cm2[row_sd, col_sd, 0, i_cn] * frac_eroded;
                            CN_in_transport[row_sd + i_sd, col_sd + j_sd, i_cn] += CN_erosion; // add CNs associated with eroded fraction to transport
                            CN_atoms_cm2[row_sd, col_sd, 0, i_cn] -= CN_erosion; // remove from source location

                            // Erosion layer 1
                            if (i_cn == 0 | i_cn == 4) { frac_eroded = clayerodedfraction_1; } else { frac_eroded = sanderodedfraction_1; }
                            CN_erosion = CN_atoms_cm2[row_sd, col_sd, 1, i_cn] * frac_eroded;
                            CN_in_transport[row_sd + i_sd, col_sd + j_sd, i_cn] += CN_erosion; // add CNs associated with eroded fraction
                            CN_atoms_cm2[row_sd, col_sd, 1, i_cn] -= CN_erosion; // remove from source location

                            // CNs already in transport
                            CN_in_transport[row_sd + i_sd, col_sd + j_sd, i_cn] += CN_in_transport[row_sd, col_sd, i_cn] * flowfraction;
                        }
                    }
                }

                // We still need to transport the sediments that were already in transport:
                for (size = 0; size < n_texture_classes; size++)
                {
                    sediment_in_transport_kg[row_sd + i_sd, col_sd + j_sd, size] += flowfraction * sediment_in_transport_kg[row_sd, col_sd, size];  //all in kg 
                }
                old_SOM_in_transport_kg[row_sd + i_sd, col_sd + j_sd] += flowfraction * old_SOM_in_transport_kg[row_sd, col_sd];  //all in kg
                young_SOM_in_transport_kg[row_sd + i_sd, col_sd + j_sd] += flowfraction * young_SOM_in_transport_kg[row_sd, col_sd];  //all in kg

                if (OSL_checkbox.Checked)
                {
                    transport_ero_sed_OSL_by_WE(row_sd, col_sd, i_sd, j_sd, sum_of_fractions, flowfraction, 0, 0, 0);
                }

                if (CN_checkbox.Checked)
                {
                    for (int i_cn = 0; i_cn < n_cosmo; i_cn++)
                    {
                        // CNs already in transport
                        CN_in_transport[row_sd + i_sd, col_sd + j_sd, i_cn] += CN_in_transport[row_sd, col_sd, i_cn] * flowfraction;
                    }
                }
            }

            // Deposition
            if (transport_capacity_kg < total_sediment_in_transport_kg)
            {
                //first, calculate how much we are going to keep in transport. This is the way that selectivity works now. 
                double sum_diameter_power = 0, clay_deposited = 0, clay_transported = 0, clay_in_transport = 0, sand_deposited = 0, sand_in_transport = 0;
                for (size = 0; size < 5; size++)
                {
                    sum_diameter_power += 1 / Math.Pow(upper_particle_size[size], 0.5);
                }
                for (size = 0; size < 5; size++)
                {
                    selectivity_fraction = (1 / Math.Pow(upper_particle_size[size], 0.5)) / sum_diameter_power;    // unit [-]
                    potential_transported_amount_kg = selectivity_fraction * transport_capacity_kg;                      // unit [kg]
                    if (potential_transported_amount_kg < sediment_in_transport_kg[row_sd, col_sd, size] * flowfraction)
                    {
                        total_mass_deposited_kg[size] += sediment_in_transport_kg[row_sd, col_sd, size] * flowfraction - potential_transported_amount_kg;
                        texture_kg[row_sd, col_sd, 0, size] += sediment_in_transport_kg[row_sd, col_sd, size] * flowfraction - potential_transported_amount_kg;        // unit [kg]
                        sediment_in_transport_kg[row_sd + i_sd, col_sd + j_sd, size] = potential_transported_amount_kg;                                    // unit [kg]  
                        if (size == 1)
                        {
                            sand_deposited = sediment_in_transport_kg[row_sd, col_sd, size] * flowfraction - potential_transported_amount_kg;
                            sand_in_transport = sediment_in_transport_kg[row_sd, col_sd, size] * flowfraction;
                        }
                        if (size > 2)
                        {
                            clay_deposited += sediment_in_transport_kg[row_sd, col_sd, size] * flowfraction - potential_transported_amount_kg;
                            clay_transported += potential_transported_amount_kg;
                            clay_in_transport += sediment_in_transport_kg[row_sd, col_sd, size] * flowfraction;
                        }
                    }
                    else
                    {
                        //do nothing. We keep the sediment in transport, and do not deposit anything. Only transport the sediments to the next cell
                        sediment_in_transport_kg[row_sd + i_sd, col_sd + j_sd, size] += sediment_in_transport_kg[row_sd, col_sd, size] * flowfraction;
                    }
                }
                // now organic matter
                double sand_deposited_fraction = sand_deposited / sand_in_transport; if (Double.IsNaN(sand_deposited_fraction)) { sand_deposited_fraction = 0; }
                double clay_deposited_fraction = clay_deposited / clay_in_transport; ; if (Double.IsNaN(clay_deposited_fraction)) { clay_deposited_fraction = 0; }

                total_mass_deposited_kg[5] += flowfraction * young_SOM_in_transport_kg[row_sd, col_sd] * clay_deposited_fraction;
                total_mass_deposited_kg[6] += flowfraction * old_SOM_in_transport_kg[row_sd, col_sd] * clay_deposited_fraction;
                young_SOM_kg[row_sd, col_sd, 0] += flowfraction * young_SOM_in_transport_kg[row_sd, col_sd] * clay_deposited_fraction;
                old_SOM_kg[row_sd, col_sd, 0] += flowfraction * old_SOM_in_transport_kg[row_sd, col_sd] * clay_deposited_fraction;

                young_SOM_in_transport_kg[row_sd + i_sd, col_sd + j_sd] += flowfraction * young_SOM_in_transport_kg[row_sd, col_sd] * (1 - clay_deposited_fraction);
                old_SOM_in_transport_kg[row_sd + i_sd, col_sd + j_sd] += flowfraction * old_SOM_in_transport_kg[row_sd, col_sd] * (1 - clay_deposited_fraction);

                // Now geochronology
                if (OSL_checkbox.Checked)
                {
                    transport_ero_sed_OSL_by_WE(row_sd, col_sd, i_sd, j_sd, sum_of_fractions, flowfraction, 0, 0, sand_deposited_fraction); // layer 0
                }

                if (CN_checkbox.Checked)
                {
                    for (int i_cn = 0; i_cn < n_cosmo; i_cn++)
                    {
                        // Deposition layer 0
                        if (i_cn == 0 | i_cn == 4) { frac_deposited = clay_deposited_fraction; } else { frac_deposited = sand_deposited_fraction; }

                        double CN_deposition = CN_in_transport[row_sd, col_sd, i_cn] * flowfraction * frac_deposited;
                        CN_atoms_cm2[row_sd, col_sd, 0, i_cn] += CN_deposition; // deposit CNs
                        CN_in_transport[row_sd + i_sd, col_sd + j_sd, i_cn] += CN_in_transport[row_sd, col_sd, i_cn] * flowfraction - CN_deposition; // transport the rest
                    }
                }
            } // end deposition

        }

        void calculate_water_ero_sed_daily()
        {
            if (NA_in_map(dtm) > 0 | NA_in_map(soildepth_m) > 0)
            {
                Debug.WriteLine("we1");
            }

            for (sbyte tcls = 0; tcls < 5; tcls++)
            {
                domain_sed_export_kg[tcls] = 0;
            }
            domain_OOM_export_kg = 0;
            domain_YOM_export_kg = 0;

            Decimal mass_before = total_catchment_mass_decimal(), mass_after, mass_export = 0;
            //Debug.WriteLine("WE1");
            int size, dir;
            double water_out, flow_between_cells_m3_per_m, total_sediment_in_transport_kg, rock_fraction, total_ero = 0, total_dep = 0, potential_transported_amount_kg, vegetation_cover_fraction, mass_to_be_eroded, selectivity_fraction;

            // 1: set all water and sediment flow to 0
            for (int row = 0; row < nr; row++)
            {
                for (int col = 0; col < nc; col++)
                {
                    {
                        if (only_waterflow_checkbox.Checked == false)
                        {

                            for (size = 0; size < n_texture_classes; size++)
                            {
                                sediment_in_transport_kg[row, col, size] = 0;
                            }
                            old_SOM_in_transport_kg[row, col] = 0;
                            young_SOM_in_transport_kg[row, col] = 0;
                            dz_ero_m[row, col] = 0;
                            dz_sed_m[row, col] = 0;
                            lake_sed_m[row, col] = 0;

                            if (OSL_checkbox.Checked)
                            {
                                OSL_grainages_in_transport[row, col] = new int[] { };
                                OSL_depositionages_in_transport[row, col] = new int[] { };
                                OSL_surfacedcount_in_transport[row, col] = new int[] { };
                            }
                            if (CN_checkbox.Checked)
                            {
                                for (int i_cn = 0; i_cn < n_cosmo; i_cn++)
                                {
                                    CN_in_transport[row, col, i_cn] = 0;
                                }

                            }

                        }
                    }
                }  // end for col
            }  //end for row
               //Debug.WriteLine("WE2");

            // 2: Iterate through rows and columns
            int runner = 0;
            for (runner = number_of_data_cells - 1; runner >= 0; runner--)
            {

                if (index[runner] != nodata_value)
                {
                    //if (row == 40 & col == 31) { displaysoil(40, 31); Debugger.Break(); }

                    row = row_index[runner]; col = col_index[runner];

                    //Debug.WriteLine("WE3");

                    // 3: Determine fraction of water flowing to lower neighbour
                    water_out = 0;
                    for (int dir2 = 1; dir2 < 9; dir2++)
                    {
                        water_out += OFy_m[row, col, dir2];
                        //if (col == 12 && dir2 == 1) { Debugger.Break(); }
                        // if (t == 1) { Debugger.Break(); }
                    }
                    double fracsum = 0;

                    if (water_out > 0)
                    {
                        // Debug.WriteLine("Overland flow in col {0}", col);
                        dir = 0;
                        for (i = (-1); i <= 1; i++)
                        {
                            for (j = (-1); j <= 1; j++)
                            {
                                if (!((i == 0) && (j == 0))) { dir++; }

                                dh = 0; fraction = 0; transport_capacity_kg = 0;

                                if (((row + i) >= 0) && ((row + i) < nr) && ((col + j) >= 0) && ((col + j) < nc) && !((i == 0) && (j == 0)))
                                {
                                    if (dtm[row + i, col + j] != nodata_value)
                                    {
                                        // Debug.WriteLine("row = {0}, col = {1}, dir = {2}, i = {3}, j = {4}",row,col,dir,i,j);
                                        dh = dtm[row, col] - dtm[row + i, col + j];
                                        //Debug.WriteLine("Source: {0}, sink: {1}", dtm[row, col], dtm[row + i, col + j]);
                                        d_x = dx;
                                        //if (col + j == 0) { Debugger.Break(); }

                                        if (dh > 0)
                                        {
                                            if ((row != row + i) && (col != col + j)) { d_x = dx * Math.Sqrt(2); } else { d_x = dx; }
                                            dh /= d_x;

                                            fraction = OFy_m[row, col, dir] / water_out;

                                            flow_between_cells_m3_per_m = OFy_m[row, col, dir] * dx * dx / dx; // 

                                            //Debug.WriteLine("WE4");
                                            calculate_sediment_dynamics(row, col, i, j, flow_between_cells_m3_per_m, fraction, fracsum);
                                            fracsum += fraction;
                                        } // end dh > 0
                                    } // end dtm!=nodata_value
                                }
                            } // end j
                        } // end i
                        if (fracsum < 0.9999 & !search_nodataneighbour(row, col))
                        {
                            Debug.WriteLine("fracsum = " + fracsum);
                            for (int otp = 0; otp < 10; otp++)
                            {
                                Debug.WriteLine("dir {0}, {1}", otp, OFy_m[row, col, otp]);
                            }
                            //minimaps(row, col);
                            Debug.WriteLine("err_we3");

                        }
                    } // outflow water
                    else
                    { // no outflow of water. 
                      // -> Cell located in depression. Deposit all sediments
                      // -> Cell at border of landscape, outflow of all sediments
                      // double mass_temp = total_catchment_mass_decimal();
                        bool bool_outflow = false;
                        for (i = (-1); i <= 1; i++)
                        {
                            for (j = (-1); j <= 1; j++)
                            {
                                if (i != 0 & j != 0)
                                {
                                    if ((row + i) >= nr | (row + i) <= 0 | (col + j) >= nc | (col + j) <= 0) // Does the cell fall outside of the area?
                                    {
                                        bool_outflow = true;
                                    }
                                    else if (dtm[row + i, col + j] == nodata_value)
                                    {
                                        bool_outflow = true;
                                    }
                                }
                            }
                        }
                        if (bool_outflow) // if there is outflow, export of sediments. 
                        {
                            for (size = 0; size < n_texture_classes; size++)
                            {
                                mass_export += Convert.ToDecimal(sediment_in_transport_kg[row, col, size]);
                            }
                            mass_export += Convert.ToDecimal(old_SOM_in_transport_kg[row, col]);  //all in kg
                            mass_export += Convert.ToDecimal(young_SOM_in_transport_kg[row, col]);  //all in kg
                        }
                        else // if there is no outflow, deposition of sediments in cell. No delta formation (yet) develop MvdM
                        {
                            for (size = 0; size < n_texture_classes; size++)
                            {
                                texture_kg[row, col, 0, size] += sediment_in_transport_kg[row, col, size];  //all in kg 
                                total_dep += sediment_in_transport_kg[row, col, size];

                                old_SOM_kg[row, col, 0] += old_SOM_in_transport_kg[row, col];  //all in kg
                                young_SOM_kg[row, col, 0] += young_SOM_in_transport_kg[row, col];  //all in kg

                                // now geochronology
                                if (CN_checkbox.Checked)
                                {
                                    for (int cn_i = 0; cn_i < n_cosmo; cn_i++)
                                    {
                                        CN_atoms_cm2[row, col, 0, cn_i] += CN_in_transport[row, col, cn_i];
                                    }
                                }
                                if (OSL_checkbox.Checked)
                                {
                                    transport_ero_sed_OSL_by_WE(row, col, 0, 0, 0, 1, 0, 0, 1); // Deposit all grains in transport at this location
                                }
                            }
                        }
                    }
                }
                mass_after = total_catchment_mass_decimal();
                if (mass_before - (mass_after + mass_export) > Convert.ToDecimal(0.001))
                {
                    Debug.WriteLine("err_we4");
                }
                // all cells have now been considered in order of (original) altitude. We must still recalculate their thicknesses and recalculate altitude. While doing that, we should count how much erosion and deposition there has been. 
                double old_total_elevation = total_catchment_elevation();
                volume_eroded_m = 0; sediment_exported_m = 0; volume_deposited_m = 0;
                total_average_altitude_m = 0; total_altitude_m = 0;
                total_rain_m = 0; total_evap_m = 0; total_infil_m = 0;
                total_rain_m3 = 0; total_evap_m3 = 0; total_infil_m3 = 0; total_outflow_m3 = 0;
                wet_cells = 0; eroded_cells = 0; deposited_cells = 0;
                for (int row = 0; row < nr; row++)
                {
                    for (int col = 0; col < nc; col++)
                    {
                        if (dtm[row, col] != nodata_value)
                        {
                            if (only_waterflow_checkbox.Checked == false)
                            {
                                double old_thickness = soildepth_m[row, col];
                                update_all_layer_thicknesses(row, col);
                                double new_thickness = total_soil_thickness(row, col);
                                dtm[row, col] += new_thickness - old_thickness;
                                soildepth_m[row, col] = new_thickness;
                                dtmchange_m[row, col] += new_thickness - old_thickness;
                                sum_water_erosion[row, col] += new_thickness - old_thickness;
                                if ((new_thickness - old_thickness) < 0)
                                { dz_ero_m[row, col] += new_thickness - old_thickness; }
                                else { dz_sed_m[row, col] += new_thickness - old_thickness; }
                                if (-dz_ero_m[row, col] > timeseries.timeseries_erosion_threshold) { eroded_cells++; }
                                if (dz_sed_m[row, col] + lake_sed_m[row, col] > timeseries.timeseries_deposition_threshold) { deposited_cells++; }
                            }
                            // 7: Update timeseries
                            if (check_space_rain.Checked == true) { total_rain_m += rain_m[row, col]; }
                            total_rain_m += rain_value_m;
                            if (check_space_evap.Checked == true) { total_evap_m += evapotranspiration[row, col]; }
                            total_evap_m += evap_value_m;
                            if (check_space_infil.Checked == true) { total_infil_m += infil[row, col]; }
                            total_infil_m += infil_value_m;
                            if (waterflow_m3[row, col] * dx * dx > timeseries.timeseries_waterflow_threshold) { wet_cells++; }
                        } // end for nodata
                    }   // end for col
                } // end for row

                // out_double(workdir + "\\" + run_number + "_" + t + "_mass_difference.asc", mass_difference_input_output);
                total_rain_m3 = total_rain_m * dx * dx;   // m3
                total_evap_m3 = total_evap_m * dx * dx;   // m3
                total_infil_m3 = total_infil_m * dx * dx;  // m3
                total_outflow_m3 = total_rain_m3 - total_evap_m3 - total_infil_m3;
                //Debug.WriteLine("\n--erosion and deposition overview--");
                //Debug.WriteLine("rain " + total_rain + " evap " + total_evap + " total_infil " + total_infil);
                /*Task.Factory.StartNew(() =>
                {
                    this.InfoStatusPanel.Text = "calc movement has been finished";
                    this.out_sed_statuspanel.Text = string.Format("sed_exp {0:F0} * 1000 m3", total_sed_export * dx * dx / 1000);
                }, CancellationToken.None, TaskCreationOptions.None, guiThread); */

                if (NA_in_map(dtm) > 0 | NA_in_map(soildepth_m) > 0)
                {
                    Debug.WriteLine("err_we5");
                }

            }
        }

        void calculate_water_ero_sed()    //where the water starts flowing, eroding and transporting
        {
            Debug.WriteLine("calculating erosion");
            //this.InfoStatusPanel.Text = "water erosion calculation";
            dhmax_errors = 0;
            //set all start q values effective precipitation at time t
            nb_ok = 0;  // nb_ok is 1 als er uberhaupt buren zijn, dus 0 als er alleen maar NODATA is
            nb_check = 0; all_grids = 0;
            dz_bal = 0; sediment_exported_m = 0; erocnt = 0; sedcnt = 0;
            sedbal = 0; erobal = 0; maximum_allowed_deposition = large_negative_number; dh_tol = 0.00025;
            sedbal2 = 0; erobal2 = 0;
            tel1 = 0; tel2 = 0; tel3 = 0; tel4 = 0;
            depressions_filled = 0; depressions_delta = 0; depressions_alone = 0; sediment_delta_m = 0; sediment_filled_m = 0; depressionvolume_filled_m = 0; crashed = false;
            for (sbyte tcls = 0; tcls < 5; tcls++)
            {
                domain_sed_export_kg[tcls] = 0;
            }
            domain_OOM_export_kg = 0;
            domain_YOM_export_kg = 0;


            double powered_slope_sum, flow_between_cells_m3_per_m;
            int size;
            for (alpha = 1; alpha <= maxdepressionnumber; alpha++)  // zeroing all waterflow at outlets of depressions
            {
                depressionconsidered[alpha] = 0;
                for (int outletcounter = 0; outletcounter < 5; outletcounter++)
                {
                    if (drainingoutlet_row[alpha, outletcounter] != -1)
                    {
                        waterflow_m3[drainingoutlet_row[alpha, outletcounter], drainingoutlet_col[alpha, outletcounter]] = 0;
                    }
                }
            }
            if (NA_anywhere_in_soil() == true) { Debug.WriteLine("NA found before row col loop in water erosed"); }
            for (row = 0; row < nr; row++)
            {
                for (col = 0; col < nc; col++)
                {
                    // if(row==50 & col == 99) { Debugger.Break(); }
                    if (dtm[row, col] != nodata_value)
                    {
                        // First, we apply rainwater to our landscape (in a two step approach - first normal cells and lake outlets)
                        if (depression[row, col] == 0 ||
                            (drainingoutlet_row[depression[row, col], 0] == row && drainingoutlet_col[depression[row, col], 0] == col) ||
                            (drainingoutlet_row[depression[row, col], 1] == row && drainingoutlet_col[depression[row, col], 1] == col) ||
                            (drainingoutlet_row[depression[row, col], 2] == row && drainingoutlet_col[depression[row, col], 2] == col) ||
                            (drainingoutlet_row[depression[row, col], 3] == row && drainingoutlet_col[depression[row, col], 3] == col) ||
                            (drainingoutlet_row[depression[row, col], 4] == row && drainingoutlet_col[depression[row, col], 4] == col))
                        {
                            if (check_space_evap.Checked == true) { evap_value_m = evapotranspiration[row, col]; }
                            if (check_space_rain.Checked == true) { rain_value_m = rain_m[row, col]; }
                            if (check_space_infil.Checked == true) { infil_value_m = infil[row, col]; }
                            //ArT // development required to account for f(t) situations
                            waterflow_m3[row, col] += (rain_value_m - infil_value_m - evap_value_m) * dx * dx;
                            if (waterflow_m3[row, col] < 0) { waterflow_m3[row, col] = 0; }
                            if (waterflow_m3[row, col] < -0.001) { Debug.WriteLine(" Negative waterflow at " + row + " " + col + ": " + waterflow_m3[row, col] + ". rain " + rain_value_m + " infil " + infil_value_m + " evap " + evap_value_m + " use " + landuse[row, col]); }
                        }
                        else  // and then, second step, for other lake cells
                        { // for other lakecells, we send the rainwater directly (equally distributed) to that lake's outlet(s) (infiltration is not zero in the lake at the moment)
                          //Debug.WriteLine(" B at " + row + " col " + col + " alt " + dtm[row, col] + " dep " + depression[row, col]);
                            int outletcounter = 0; ;
                            while (drainingoutlet_col[depression[row, col], outletcounter] != -1)
                            {
                                outletcounter++;
                                if (outletcounter == 5) { break; }
                            }
                            for (i = 0; i < outletcounter; i++)
                            {

                                if (check_space_evap.Checked == true) { evap_value_m = evapotranspiration[row, col]; }
                                if (check_space_rain.Checked == true) { rain_value_m = rain_m[row, col]; }
                                if (check_space_infil.Checked == true) { infil_value_m = infil[row, col]; }
                                //ArT // development required to account for f(t) situations
                                //ArT remember to check for negative lake outflow once it happens
                                waterflow_m3[drainingoutlet_row[depression[row, col], i], drainingoutlet_col[depression[row, col], i]] += dx * dx * (rain_value_m - infil_value_m - evap_value_m) / outletcounter;
                            }
                        }
                        if (only_waterflow_checkbox.Checked == false)
                        {
                            for (size = 0; size < n_texture_classes; size++)
                            {
                                sediment_in_transport_kg[row, col, size] = 0;
                            }
                            old_SOM_in_transport_kg[row, col] = 0;
                            young_SOM_in_transport_kg[row, col] = 0;
                            dz_ero_m[row, col] = 0;
                            dz_sed_m[row, col] = 0;
                            lake_sed_m[row, col] = 0;

                            if (OSL_checkbox.Checked)
                            {
                                OSL_grainages_in_transport[row, col] = new int[] { };
                                OSL_depositionages_in_transport[row, col] = new int[] { };
                                OSL_surfacedcount_in_transport[row, col] = new int[] { };
                            }
                            if (CN_checkbox.Checked)
                            {
                                for (int i_cn = 0; i_cn < n_cosmo; i_cn++)
                                {
                                    CN_in_transport[row, col, i_cn] = 0;
                                }
                            }

                        }
                    }
                }  // end for col
            }  //end for row
               //Debug.WriteLine(" prepared water. Ready to route for erosion and deposition");
            all_grids = (nr) * (nc);
            memberdepressionnotconsidered = 0;
            int runner = 0;
            if (NA_anywhere_in_soil() == true) { Debug.WriteLine("NA found before sorted row col loop in water erosed"); }
            for (runner = number_of_data_cells - 1; runner >= 0; runner--)
            {     // the index is sorted from low to high values, but flow goes from high to low
                if (index[runner] != nodata_value)
                {

                    int row = row_index[runner]; int col = col_index[runner];
                    // if(row==50 & col == 99) { Debugger.Break(); }
                    //Debug.WriteLine(runner + " " + row + "  " + col + " nr " + nr + " nc " + nc + " nr*nc " + nr * nc + " data cells " + number_of_data_cells);
                    if (t == 1 && row == 24 && col == 81) { diagnostic_mode = 1; }

                    else { diagnostic_mode = 0; }

                    powered_slope_sum = 0; max_allowed_erosion = 0; dz_min = large_negative_number;
                    direction = 20; dz_max = -10; dhtemp = -8888; maximum_allowed_deposition = large_negative_number;
                    if (depression[row, col] < 0) { depression[row, col] = 0; }
                    if ((drainingoutlet_row[depression[row, col], 0] == row && drainingoutlet_col[depression[row, col], 0] == col) ||
                        (drainingoutlet_row[depression[row, col], 1] == row && drainingoutlet_col[depression[row, col], 1] == col) ||
                        (drainingoutlet_row[depression[row, col], 2] == row && drainingoutlet_col[depression[row, col], 2] == col) ||
                        (drainingoutlet_row[depression[row, col], 3] == row && drainingoutlet_col[depression[row, col], 3] == col) ||
                        (drainingoutlet_row[depression[row, col], 4] == row && drainingoutlet_col[depression[row, col], 4] == col))
                    {
                        if (depressionconsidered[depression[row, col]] == 0)
                        {

                            depressionnumber = depression[row, col];
                            depressionconsidered[depressionnumber] = 1;

                            if (diagnostic_mode == 1) { Debug.WriteLine(" now considering dep " + depressionnumber + " index " + runner); }
                            update_depression(depressionnumber);
                            if (depressionsum_sediment_m == 0)
                            {
                                leave_depression_alone(depressionnumber); depressions_alone++;
                            }
                            else
                            {
                                if (depressionsum_sediment_m >= needed_to_fill_depression_m) { fill_depression(depressionnumber, needed_to_fill_depression_m / depressionsum_sediment_m); depressions_filled++; }
                                else { delta_depression(depressionnumber); depressions_delta++; }
                            }
                        }
                        //all cells of this lake have now been considered, except the outlets
                    }
                    if (depression[row, col] < 0) { Debug.WriteLine(" error: negative depression value " + depression[row, col] + " at " + row + " " + col); minimaps(row, col); }
                    // this check indicates a problem with the resetting of cells involved in a delta
                    if (depression[row, col] == 0 ||
                                                (drainingoutlet_row[depression[row, col], 0] == row && drainingoutlet_col[depression[row, col], 0] == col) ||
                                                (drainingoutlet_row[depression[row, col], 1] == row && drainingoutlet_col[depression[row, col], 1] == col) ||
                                                (drainingoutlet_row[depression[row, col], 2] == row && drainingoutlet_col[depression[row, col], 2] == col) ||
                                                (drainingoutlet_row[depression[row, col], 3] == row && drainingoutlet_col[depression[row, col], 3] == col) ||
                                                (drainingoutlet_row[depression[row, col], 4] == row && drainingoutlet_col[depression[row, col], 4] == col))
                    { //for all cells outside a depression and for outlets, we use the stream power equations based on a multiple flow (D8) template
                      //if (row == 24 && col == 81) { Debug.WriteLine(" looking around cell " + row + " " + col); minimaps(row, col); }
                        for (sbyte i = (-1); i <= 1; i++)
                        {
                            for (sbyte j = (-1); j <= 1; j++)
                            {
                                dh = 0; dhtemp = -8888; d_x = dx;
                                if (((row + i) >= 0) && ((row + i) < nr) && ((col + j) >= 0) && ((col + j) < nc) && !((i == 0) && (j == 0)))  //to stay within the grid and avoid the row col cell itself
                                {
                                    // below, we calculate slope_sum for all cells either not in a depression, or being a outlet
                                    // slope_sum is needed to calculate flow in a multiple flow environment until someone thinks of something better
                                    // if (diagnostic_mode == 1) { Debug.WriteLine("checking " + (row + i) + " " + (col + j) + " from cell " + row + " " + col); }
                                    if (depression[row, col] == 0)
                                    {    // if the cell is not in a depression (it could be in a depression as an outlet)
                                        if (dtm[row + i, col + j] != nodata_value)
                                        {  //if the cell has no NODATA
                                            if (only_waterflow_checkbox.Checked)
                                            {
                                                dh = dtm[row, col] - dtm[row + i, col + j]; // in the case that we are not interested in erosion and deposition, then there is no ero and sed to query                                            }
                                            }
                                            else
                                            {
                                                dh = (dtm[row, col] + dz_ero_m[row, col] + dz_sed_m[row, col]) - (dtm[row + i, col + j] + dz_ero_m[row + i, col + j] + dz_sed_m[row + i, col + j]);    // diff @ this moment 
                                            }
                                            if (dh < 0)  // we are looking at a higher neighbour
                                            {
                                                if (dh > maximum_allowed_deposition) { maximum_allowed_deposition = dh; }   // we keep track of the minimum difference in altitude between this cell and its lowest higher neighbour - we will not raise it more, even if we would like to when the Courant criterion is violated
                                            } // end if dh
                                            if ((row != row + i) && (col != col + j)) { d_x = dx * Math.Sqrt(2); } else { d_x = dx; }   // for non-cardinal neighbours, we use the adapted length
                                            if (dh > 0)
                                            {  // i j is a lower neighbour
                                                if (dh > max_allowed_erosion - dh_tol) { max_allowed_erosion = (dh - dh_tol); }  // we keep track of the minimum difference in current altitude between this cell and its highest lower neighbour - we will not erode it more, even if we would like to

                                                dh = dh / d_x;
                                                dh = Math.Pow(dh, conv_fac);
                                                powered_slope_sum = powered_slope_sum + dh;
                                            }//end if dh  
                                        }//end if novalues
                                    }  // end if not in depression
                                    if ((drainingoutlet_row[depression[row, col], 0] == row && drainingoutlet_col[depression[row, col], 0] == col)
                                        || (drainingoutlet_row[depression[row, col], 1] == row && drainingoutlet_col[depression[row, col], 1] == col)
                                        || (drainingoutlet_row[depression[row, col], 2] == row && drainingoutlet_col[depression[row, col], 2] == col)
                                        || (drainingoutlet_row[depression[row, col], 3] == row && drainingoutlet_col[depression[row, col], 3] == col)
                                        || (drainingoutlet_row[depression[row, col], 4] == row && drainingoutlet_col[depression[row, col], 4] == col))
                                    {    // this cell is one of the draining outlets and is only allowed to drain to cells not in the lake																											
                                         // if the lake has been filled at this time, then all its (by now non-lake) cells have an altitude > outlet, and will not be considered for that reason
                                        if (depression[row + i, col + j] != depression[row, col])
                                        {
                                            if ((row != row + i) && (col != col + j)) { d_x = dx * Math.Sqrt(2); } else { d_x = dx; }
                                            dh = dtm[row, col] - dtm[row + i, col + j];
                                            if (dh > 0)
                                            {// i j is a lower neighbour
                                                dh = dh / d_x;                      // dh is now equal to slope
                                                dh = Math.Pow(dh, conv_fac);            // dh is nu de helling tot de macht conv fac
                                                powered_slope_sum = powered_slope_sum + dh;
                                            } // end if lower nb
                                        } //end if nb not within depression
                                    } // end if drainingoutlet
                                }// end if boundaries
                            }//end for j
                        }//end for i, we now know slope sum for this cell. We have included cells that are in a lake in this calculation. //ArT should we replace their altitude with depressionlevel?
                         // (row == 24 && col == 81) { Debug.WriteLine("passed"); }
                        if (maximum_allowed_deposition == large_negative_number) { maximum_allowed_deposition = 0; } else { maximum_allowed_deposition = -maximum_allowed_deposition; }
                        if (max_allowed_erosion < 0) { max_allowed_erosion = -dh_tol; } else { max_allowed_erosion = -max_allowed_erosion; }
                        //if (diagnostic_mode == 1) { Debug.WriteLine(" slopesum = " + slope_sum + " maximum deposition " + maximum_allowed_deposition + " maximum erosion " + max_allowed_erosion); }

                        //if slope_sum is zero, then we are in a non-lake cell or a lake outlet that has no nbs in the dtm -> we have reached an outflow point.
                        //no action is  needed, but we do count the number of outlets and the total amount of sed in trans that leaves the catchment from these places
                        if (powered_slope_sum == 0)
                        {
                            number_of_outflow_cells++;
                            for (sbyte tcls = 0; tcls < 5; tcls++)
                            {
                                domain_sed_export_kg[tcls] += sediment_in_transport_kg[row, col, tcls];
                            }
                            domain_OOM_export_kg += old_SOM_in_transport_kg[row, col];
                            domain_YOM_export_kg += young_SOM_in_transport_kg[row, col];
                        }
                        else  //apparently, there is at least 1 lower nb in the DEM. Let's do business with it
                        {

                            // we are now prepared to actually calculate erosion and deposition: we can calculate how much water and sediment is redistributed using slope_sum
                            if (NA_in_soil(row, col) == true) { Debug.WriteLine("NA found before eroding " + row + " " + col); }
                            double sum_frac_OSL = 0;
                            for (sbyte i = (-1); i <= 1; i++)
                            {
                                for (sbyte j = (-1); j <= 1; j++)
                                {
                                    dh = 0; fraction = 0; transport_capacity_kg = 0;
                                    sediment_transported = 0; detachment_rate = 0;
                                    d_x = dx;
                                    if (((row + i) >= 0) && ((row + i) < nr) && ((col + j) >= 0) && ((col + j) < nc) && !((i == 0) && (j == 0)))
                                    {  //boundaries
                                       //if (row == 24 && col == 81) { Debug.WriteLine("entered" + i + j); }
                                        if (dtm[row + i, col + j] != nodata_value)
                                        {
                                            if (only_waterflow_checkbox.Checked)
                                            {
                                                dh = dtm[row, col] - dtm[row + i, col + j];
                                            }
                                            else
                                            {
                                                dh = (dtm[row, col] + dz_ero_m[row, col] + dz_sed_m[row, col]) - (dtm[row + i, col + j] + dz_ero_m[row + i, col + j] + dz_sed_m[row + i, col + j]);
                                            }
                                            if (dh > 0)

                                            {
                                                //we have found one of the lower nbs
                                                //if (row == 24 && col == 81) { Debug.WriteLine("this is a lower nb " + i + j + "dh" + dh + " " + waterflow_m3[row, col]); }
                                                if ((row != row + i) && (col != col + j)) { d_x = dx * Math.Sqrt(2); } else { d_x = dx; }
                                                if ((depression[row, col] != 0 && depression[row + i, col + j] != depression[row, col]) || (depression[row, col] == 0))
                                                {   //if cell == outlet of current lake and nb not member of that lake OR if not a lake member

                                                    // Now, we first calculate the fraction of water and sediment that goes from row, col to row+i to col+j , always using current altitudes
                                                    // Then, we calculate the actual amounts of water and sediment, and with that, using the stream power equation, the transport capacity
                                                    // In future, the Hjülstrom diagram can be used to give texture-dependent erosion thresholds (or selectivity)

                                                    dh /= d_x;  //dh is now slope
                                                    fraction = Math.Pow(dh, conv_fac) / powered_slope_sum;
                                                    if (waterflow_m3[row, col] < 0) { waterflow_m3[row, col] = 0; }    // this can have happened if water enters a drier zone in the landscape
                                                    flow_between_cells_m3_per_m = fraction * waterflow_m3[row, col] / dx;
                                                    if (depression[row + i, col + j] == 0)
                                                    {  // if receiving cell is not in a depression, its waterflow is increased 
                                                        waterflow_m3[row + i, col + j] += flow_between_cells_m3_per_m * dx;
                                                    }
                                                    if (depression[row + i, col + j] != 0)
                                                    {  // if receiving cell is in a depression, its outlets' waterflow is increased 
                                                        currentdepression = Math.Abs(depression[row + i, col + j]); // this Abs stuff should not be necessary and is included for stability!
                                                        int outletcounter = 0;
                                                        while (drainingoutlet_col[currentdepression, outletcounter] != -1)
                                                        {
                                                            outletcounter++;
                                                            if (outletcounter == 5) { break; }
                                                        }
                                                        for (int iter = 0; iter < outletcounter; iter++) // for all outlets of this depression, divide that amount of water over them
                                                        {
                                                            waterflow_m3[drainingoutlet_row[currentdepression, iter], drainingoutlet_col[currentdepression, iter]] += dx * flow_between_cells_m3_per_m / outletcounter;
                                                        }
                                                    }

                                                    if (only_waterflow_checkbox.Checked == false)
                                                    {
                                                        calculate_sediment_dynamics(row, col, i, j, flow_between_cells_m3_per_m, fraction, sum_frac_OSL);

                                                    } // end if else : also erosion and deposition considered
                                                    sum_frac_OSL += fraction;
                                                }

                                                // 4. Indien oververzadigd: depositie. Berekenen van de doorgaande massa van iedere textuurklasse, op basis van 1/d0.5 (zie Excel). 
                                                // 4b. Vergelijken van doorgaande massa met massa aanwezig in transport per textuurfractie. Indien teveel aanwezig, afwerpen. 
                                                // 4c. Organische stof afwerpen propoertioneel met de afzettingsfractie van de beide kleifracties. (Dus als er 30% van de klei in transport blijft, dan ook 30% van de OM).
                                                // Dit leidt bij de kleifractie slechts zelden tot afzetting. 

                                                // Depressies: volledige afzetting van materiaal dat in transport is. 
                                                // Instabiliteit: geen garantie dat dit niet gebeurt. Smearing kan er bij gezet worden. 
                                                // Gravelafzettingen: volgens pdf een rho van 2.7. Afgeronde gravel afzettingen van rivieren kunnen die heel laag hebben. 

                                            } //end`dH > 000
                                        }//end if novalues
                                    }//end if boundaries
                                }//end for j
                            }//end for i
                        }  // end else slope_sum ==0                      
                        if (NA_in_soil(row, col) == true) { Debug.WriteLine("NA found after eroding " + row + " " + col); }
                        //if (row == 24 && col == 81) { Debug.WriteLine("passed"); }
                    } // end if not in a lake or a lake outlet (all other lake cells have been considered before
                } //end if nodata
            }//end for index
             // all cells have now been considered in order of (original) altitude. We must still recalculate their thicknesses and recalculate altitude. While doing that, we should count how much erosion and deposition there has been. 
            volume_eroded_m = 0; sediment_exported_m = 0; volume_deposited_m = 0;
            total_average_altitude_m = 0; total_altitude_m = 0;
            total_rain_m = 0; total_evap_m = 0; total_infil_m = 0;
            total_rain_m3 = 0; total_evap_m3 = 0; total_infil_m3 = 0; total_outflow_m3 = 0;
            wet_cells = 0; eroded_cells = 0; deposited_cells = 0;
            for (int row = 0; row < nr; row++)
            {
                for (int col = 0; col < nc; col++)
                {
                    if (dtm[row, col] != nodata_value)
                    {
                        if (only_waterflow_checkbox.Checked == false)
                        {
                            //erosion and deposition affect only the top two layers of soil. All others: unaffected.
                            //So, we calculate the difference between the original and final thicknesses of these two layers to calculate dz_ero and dz_sed. 
                            //We already knew how much mass was involved in ero and sed, but we need the volumes to update the dtm.

                            for (sbyte i = 0; i < 2 & i < max_soil_layers; i++)
                            {
                                double pastlayer = layerthickness_m[row, col, i];
                                layerthickness_m[row, col, i] = thickness_calc(row, col, i);
                                if (pastlayer < layerthickness_m[row, col, i])  //if there is deposition in volume terms
                                {
                                    dz_sed_m[row, col] += layerthickness_m[row, col, i] - pastlayer;  // leading to positive values for dz_sed_m, which is what we want
                                }
                                else
                                {
                                    dz_ero_m[row, col] += layerthickness_m[row, col, i] - pastlayer;  //leading to negative values for dz_ero_m, which is what we want
                                }
                            }
                            //now dz_ero_m and dz_sed_m hold the changed altitudes. 

                            volume_eroded_m += dz_ero_m[row, col];
                            volume_deposited_m += dz_sed_m[row, col];
                            dtmchange_m[row, col] += dz_ero_m[row, col] + dz_sed_m[row, col];  //attention: LAKE_sed and dz_sed_m are treated differently. 
                            dtm[row, col] += dz_ero_m[row, col] + dz_sed_m[row, col];                           //No need to add lake_sed to dtm in the next line
                            soildepth_m[row, col] += dz_ero_m[row, col] + dz_sed_m[row, col]; // update soil depth
                            sum_water_erosion[row, col] += dz_ero_m[row, col] + dz_sed_m[row, col] + lake_sed_m[row, col];

                            if (-dz_ero_m[row, col] > timeseries.timeseries_erosion_threshold) { eroded_cells++; }
                            if (dz_sed_m[row, col] + lake_sed_m[row, col] > timeseries.timeseries_deposition_threshold) { deposited_cells++; }
                        }
                        if (check_space_rain.Checked == true) { total_rain_m += rain_m[row, col]; }
                        total_rain_m += rain_value_m;
                        if (check_space_evap.Checked == true) { total_evap_m += evapotranspiration[row, col]; }
                        total_evap_m += evap_value_m;
                        if (check_space_infil.Checked == true) { total_infil_m += infil[row, col]; }
                        total_infil_m += infil_value_m;
                        if (waterflow_m3[row, col] * dx * dx > timeseries.timeseries_waterflow_threshold) { wet_cells++; }
                    } // end for nodata
                }   // end for col
            } // end for row
            total_rain_m3 = total_rain_m * dx * dx;   // m3
            total_evap_m3 = total_evap_m * dx * dx;   // m3
            total_infil_m3 = total_infil_m * dx * dx;  // m3
            total_outflow_m3 = total_rain_m3 - total_evap_m3 - total_infil_m3;
            //Debug.WriteLine("\n--erosion and deposition overview--");
            //Debug.WriteLine("rain " + total_rain + " evap " + total_evap + " total_infil " + total_infil);
            if (only_waterflow_checkbox.Checked == false)
            {

                //Debug.WriteLine(" number of dhmax erosion errors: " + + "\n" ,dhmax_errors); 
                //Debug.WriteLine(" filled " + + " of " + + " depressions, %.3f sediment used for %.3f depressionvolume\n",depressions_filled,totaldepressions,sediment_filled,depressionvolume_filled); 
                //Debug.WriteLine(" sedimented into " + + " of " + + " depressions, %.3f sediment used\n",depressions_delta,totaldepressions,sediment_delta);
                //Debug.WriteLine(" left alone " + + " of " + + " depressions",depressions_alone,totaldepressions); 
                //Debug.WriteLine(" total %6.0f cubic metres of sediment (of max %6.0f) deposited ",(sediment_deposited+sediment_delta+sediment_filled)*dx*dx,(-sediment_produced*dx*dx)); 
                /* Debug.WriteLine(" MASS BASED [kg]:");
                 Debug.WriteLine(" SDR_all " + (total_kg_eroded - total_kg_deposited) / (total_kg_eroded));
                 if (total_mass_eroded[0] != 0) { Debug.WriteLine(" SDR_coarse " + (total_mass_eroded[0] - total_mass_deposited[0]) / (total_mass_eroded[0]) + " ero " + total_mass_eroded[0] + "kg sed " + total_mass_deposited[0] + "kg"); } else { Debug.WriteLine("no coarse transport"); }
                 if (total_mass_eroded[1] != 0) { Debug.WriteLine(" SDR_sand " + (total_mass_eroded[1] - total_mass_deposited[1]) / (total_mass_eroded[1]) + " ero " + total_mass_eroded[1] + "kg sed " + total_mass_deposited[1] + "kg"); } else { Debug.WriteLine("no sand transport"); }
                 if (total_mass_eroded[2] != 0) { Debug.WriteLine(" SDR_silt " + (total_mass_eroded[2] - total_mass_deposited[2]) / (total_mass_eroded[2]) + " ero " + total_mass_eroded[2] + "kg sed " + total_mass_deposited[2] + "kg"); } else { Debug.WriteLine("no silt transport"); }
                 if (total_mass_eroded[3] != 0) { Debug.WriteLine(" SDR_clay " + (total_mass_eroded[3] - total_mass_deposited[3]) / (total_mass_eroded[3]) + " ero " + total_mass_eroded[3] + "kg sed " + total_mass_deposited[3] + "kg"); } else { Debug.WriteLine("no clay transport"); }
                 if (total_mass_eroded[4] != 0) { Debug.WriteLine(" SDR_fine_clay " + (total_mass_eroded[4] - total_mass_deposited[4]) / (total_mass_eroded[4]) + " ero " + total_mass_eroded[4] + "kg sed " + total_mass_deposited[4] + "kg"); } else { Debug.WriteLine("no fine clay transport"); }

                 Debug.WriteLine(" VOLUME BASED [m3]:");
                 Debug.WriteLine(" SDR " + (volume_eroded + volume_deposited + sediment_delta + sediment_filled) / (volume_eroded));
                 //Debug.WriteLine(" as sink : %.3f ",((-sediment_delta-sediment_filled)/sediment_produced)); 
                 //Debug.WriteLine(" as sediment : %.3f ",((-sediment_deposited)/sediment_produced)); 
                 Debug.Write(" ERO " + (volume_eroded * dx * dx) + " \n");
                 Debug.Write(" SED " + (volume_deposited * dx * dx) + " \n");
                 Debug.Write(" DEL " + (sediment_delta * dx * dx) + " \n");
                 Debug.WriteLine(" FIL " + (sediment_filled * dx * dx) + " \n");
                 */
                /*if ((volume_eroded + volume_deposited + sediment_delta + sediment_filled) / volume_eroded != 0)
                {
                    Debug.WriteLine(" ALTITUDE BASED:");
                    Debug.WriteLine(" t = " + t + " number of dhmax erosion errors: " + dhmax_errors);
                    Debug.WriteLine(" on m-basis: filled " + depressions_filled + " of " + totaldepressions + " depressions, " + sediment_filled + " sediment used for " + depressionvolume_filled + " depressionvolume");
                    Debug.WriteLine(" on m-basis: sedimented into " + depressions_delta + " of " + totaldepressions + " depressions, " + sediment_delta + "  sediment used");
                } */
            }
            /*
            Task.Factory.StartNew(() =>
            {
                this.InfoStatusPanel.Text = "calc movement has been finished";
                this.out_sed_statuspanel.Text = string.Format("sed_exp {0:F0} * 1000 m3", total_sed_export * dx * dx / 1000);
            }, CancellationToken.None, TaskCreationOptions.None, guiThread);
            */
            //save timeseries_outputs
            if (timeseries.timeseries_cell_waterflow_check.Checked)
            {
                timeseries_matrix[t, timeseries_order[1]] = waterflow_m3[System.Convert.ToInt32(timeseries.timeseries_textbox_cell_row.Text), System.Convert.ToInt32(timeseries.timeseries_textbox_cell_col.Text)];
            }
            if (timeseries.timeseries_cell_altitude_check.Checked)
            {
                timeseries_matrix[t, timeseries_order[2]] = dtm[System.Convert.ToInt32(timeseries.timeseries_textbox_cell_row.Text), System.Convert.ToInt32(timeseries.timeseries_textbox_cell_col.Text)];
            }
            if (timeseries.timeseries_net_ero_check.Checked)
            {
                timeseries_matrix[t, timeseries_order[3]] = volume_eroded_m + volume_deposited_m + sediment_delta_m + sediment_filled_m;
            }
            if (timeseries.timeseries_number_dep_check.Checked)
            {
                timeseries_matrix[t, timeseries_order[4]] = deposited_cells;
            }
            if (timeseries.timeseries_number_erosion_check.Checked)
            {
                timeseries_matrix[t, timeseries_order[5]] = eroded_cells;
            }
            if (timeseries.timeseries_number_waterflow_check.Checked)
            {
                timeseries_matrix[t, timeseries_order[6]] = wet_cells;
            }
            if (timeseries.timeseries_SDR_check.Checked)
            {
                timeseries_matrix[t, timeseries_order[7]] = (volume_eroded_m + volume_deposited_m + sediment_delta_m + sediment_filled_m) / volume_eroded_m;
            }
            if (timeseries.timeseries_total_average_alt_check.Checked)
            {
                timeseries_matrix[t, timeseries_order[8]] = total_average_altitude_m;
            }
            if (timeseries.timeseries_total_dep_check.Checked)
            {
                timeseries_matrix[t, timeseries_order[9]] = volume_deposited_m + sediment_delta_m + sediment_filled_m;
            }
            if (timeseries.timeseries_total_ero_check.Checked)
            {
                timeseries_matrix[t, timeseries_order[10]] = -volume_eroded_m;
            }
            if (timeseries.timeseries_total_evap_check.Checked)
            {
                timeseries_matrix[t, timeseries_order[11]] = total_evap_m3;
            }
            if (timeseries.timeseries_total_infil_check.Checked)
            {
                timeseries_matrix[t, timeseries_order[12]] = total_infil_m3;
            }
            if (timeseries.timeseries_total_outflow_check.Checked)
            {
                timeseries_matrix[t, timeseries_order[13]] = total_outflow_m3;
            }
            if (timeseries.timeseries_total_rain_check.Checked)
            {
                timeseries_matrix[t, timeseries_order[14]] = total_rain_m3;
            }
            if (timeseries.timeseries_outflow_cells_checkbox.Checked)
            {
                timeseries_matrix[t, timeseries_order[15]] = number_of_outflow_cells;
            }
            if (timeseries.timeseries_sedexport_checkbox.Checked)
            {
                timeseries_matrix[t, timeseries_order[16]] = domain_sed_export_kg[0];
            }
            if (timeseries.timeseries_sedexport_checkbox.Checked)
            {
                timeseries_matrix[t, timeseries_order[17]] = domain_sed_export_kg[1];
            }
            if (timeseries.timeseries_sedexport_checkbox.Checked)
            {
                timeseries_matrix[t, timeseries_order[18]] = domain_sed_export_kg[2];
            }
            if (timeseries.timeseries_sedexport_checkbox.Checked)
            {
                timeseries_matrix[t, timeseries_order[19]] = domain_sed_export_kg[3];
            }
            if (timeseries.timeseries_sedexport_checkbox.Checked)
            {
                timeseries_matrix[t, timeseries_order[20]] = domain_sed_export_kg[4];
            }
            if (timeseries.timeseries_sedexport_checkbox.Checked)
            {
                timeseries_matrix[t, timeseries_order[21]] = domain_YOM_export_kg;
            }
            if (timeseries.timeseries_sedexport_checkbox.Checked)
            {
                timeseries_matrix[t, timeseries_order[22]] = domain_OOM_export_kg;
            }
        }

        void ini_slope()   //Initialise LS parameters   
        {
            // the soil physical / hydrological / slope stability parameters:
            //	 Transmissivity, Bulk Density,              
            //   Combined Cohesion and Internal friction.

            rain_intensity_m_d = Convert.ToDouble(text_ls_rel_rain_intens.Text) * rain_value_m;

            for (row = 0; row < nr; row++)
            {
                for (col = 0; col < nc; col++)
                {
                    //currently spatially uniform
                    T_fac[row, col] = nodata_value;
                    Cohesion_factor[row, col] = nodata_value;
                    sat_bd_kg_m3[row, col] = nodata_value;
                    peak_friction_angle_radians[row, col] = nodata_value;
                    resid_friction_angle_radians[row, col] = nodata_value;
                } //for
            } //for
            for (int ls = 0; ls < 100000; ls++)
            {
                landslidesum_thickness_m[ls] = 0;
            }
            //Debug.WriteLine(" initialized old landslide parameters from interface");
        }

        void calc_friction_angles()
        {
            //residual soil friction angle equals is in units of degrees, from : https://agupubs.onlinelibrary.wiley.com/doi/10.1029/2021GL095311
            //but we must move into radians for the other calculations
            //for ease, we currently take only topsoil into account
            double specific_surface_area_m2_g;
            double min_frict_angle, max_frict_angle, reference_SSA_m2_g, parameter_n;
            double n_exponent = 0;
            double fcoarse, fsand, fsilt, fclay, ffineclay;
            double peak_friction_angle = 0, resid_friction_angle = 0;

            for (int row = 0; row < nr; row++)
            {
                for (int col = 0; col < nc; col++)
                {
                    if (dtm[row, col] != nodata_value)
                    {//note that specific surface areas from the interface are in m2/kg, we need m2/g:
                        fcoarse = texture_kg[row, col, 0, 0] / total_layer_mass_kg(row, col, 0);
                        fsand = texture_kg[row, col, 0, 1] / total_layer_mass_kg(row, col, 0);
                        fsilt = texture_kg[row, col, 0, 2] / total_layer_mass_kg(row, col, 0);
                        fclay = texture_kg[row, col, 0, 3] / total_layer_mass_kg(row, col, 0);
                        ffineclay = texture_kg[row, col, 0, 4] / total_layer_mass_kg(row, col, 0);
                        specific_surface_area_m2_g = (fcoarse * specific_area[0] + fsand * specific_area[1] + fsilt * specific_area[2] + fclay * specific_area[3] + ffineclay * specific_area[4]) * 0.001;

                        //for peak:
                        min_frict_angle = 8; max_frict_angle = 32; reference_SSA_m2_g = 25; parameter_n = 2.7;
                        n_exponent = n_exponent = -(1 - (1 / parameter_n));
                        peak_friction_angle = min_frict_angle + (max_frict_angle - min_frict_angle) * Math.Pow(1 + Math.Pow(specific_surface_area_m2_g / reference_SSA_m2_g, parameter_n), n_exponent);
                        peak_friction_angle_radians[row, col] = peak_friction_angle / 180 * Math.PI;
                        //for residual:
                        min_frict_angle = 3.7; max_frict_angle = 30; reference_SSA_m2_g = 25; parameter_n = 3.0;
                        n_exponent = n_exponent = -(1 - (1 / parameter_n));
                        resid_friction_angle = min_frict_angle + (max_frict_angle - min_frict_angle) * Math.Pow(1 + Math.Pow(specific_surface_area_m2_g / reference_SSA_m2_g, parameter_n), n_exponent);
                        resid_friction_angle_radians[row, col] = resid_friction_angle / 180 * Math.PI;
                    }
                    else
                    {
                        peak_friction_angle_radians[row, col] = nodata_value;
                        resid_friction_angle_radians[row, col] = nodata_value;
                    }
                }
            }
        }

        void calc_soil_cohesion_factor()
        {
            //dimensionless soil cohesion equals root cohesion plus soil cohesion (both in kPa) , divided by soil thickness, sat soil bulk density and gravitational constant
            //for ease, we take only topsoil cohesion into account
            double g_constant = 9.81;
            double root_cohesion_kPa = 1;
            double soil_cohesion_kPa = 0;
            double clay_perc = 0;

            for (int row = 0; row < nr; row++)
            {
                for (int col = 0; col < nc; col++)
                {
                    if (dtm[row, col] != nodata_value)
                    {
                        clay_perc = 100 * (texture_kg[row, col, 0, 3] + texture_kg[row, col, 0, 4]) / (texture_kg[row, col, 0, 0] + texture_kg[row, col, 0, 1] + texture_kg[row, col, 0, 2] + texture_kg[row, col, 0, 3] + texture_kg[row, col, 0, 4]); // of the entire soil
                        soil_cohesion_kPa = 1.33 + 0.33 * clay_perc; // Khaboushan et al 2018, Soil Tillage Research (R2 0.75 - P < 0.01)
                        Cohesion_factor[row, col] = (root_cohesion_kPa + soil_cohesion_kPa) / (soildepth_m[row, col] * (sat_bd_kg_m3[row, col] / 1000) * g_constant);
                    }
                    else
                    {
                        Cohesion_factor[row, col] = nodata_value;
                    }
                }
            }
        }

        void calc_transmissivity()
        {
            double transmissivity_m2_hr;
            double fsilt = 0, fclay = 0, fOM = 0;
            double currentdepth_m;
            double slope_rad = 0;
            double Ks_m_hr;
            double BD_kg_m3 = 0;
            for (int row = 0; row < nr; row++)
            {
                for (int col = 0; col < nc; col++)
                {
                    if (dtm[row, col] != nodata_value)
                    {
                        transmissivity_m2_hr = 0;
                        currentdepth_m = 0;
                        for (int layer = 0; layer < max_soil_layers; layer++)
                        {
                            //we calculate transmissivity using the thickness of the soil in slope-perpendicular fashion, i.e. including the cosine of thicknesses of layers
                            try
                            {
                                currentdepth_m += layerthickness_m[row, col, layer];
                                fsilt = 100 * texture_kg[row, col, layer, 2] / (texture_kg[row, col, layer, 1] + texture_kg[row, col, layer, 2] + texture_kg[row, col, layer, 3] + texture_kg[row, col, layer, 4] + old_SOM_kg[row, col, layer] + young_SOM_kg[row, col, layer]); // only fine fraction
                                fclay = 100 * (texture_kg[row, col, layer, 3] + texture_kg[row, col, layer, 4]) / (texture_kg[row, col, layer, 1] + texture_kg[row, col, layer, 2] + texture_kg[row, col, layer, 3] + texture_kg[row, col, layer, 4] + old_SOM_kg[row, col, layer] + young_SOM_kg[row, col, layer]); // only fine fraction
                                fOM = 100 * (old_SOM_kg[row, col, layer] + young_SOM_kg[row, col, layer]) / (texture_kg[row, col, layer, 1] + texture_kg[row, col, layer, 2] + texture_kg[row, col, layer, 3] + texture_kg[row, col, layer, 4] + old_SOM_kg[row, col, layer] + young_SOM_kg[row, col, layer]); // only fine fraction
                                BD_kg_m3 = bulk_density_calc_kg_m3(texture_kg[row, col, layer, 0], texture_kg[row, col, layer, 1], texture_kg[row, col, layer, 2], texture_kg[row, col, layer, 3], texture_kg[row, col, layer, 4], old_SOM_kg[row, col, layer], young_SOM_kg[row, col, layer], currentdepth_m);
                                //this BD does not yet account for a stone fraction (it would become higher).
                                slope_rad = calc_slope_stdesc(row, col);
                                Ks_m_hr = (Ks_wosten(fsilt, fclay, fOM, BD_kg_m3 / 1000, 1) / 24);
                                transmissivity_m2_hr += Ks_m_hr * (layerthickness_m[row, col, layer] * Math.Cos(slope_rad));
                                if (Double.IsInfinity(transmissivity_m2_hr))
                                {
                                    Debug.WriteLine("transmis is " + transmissivity_m2_hr + " m2/hr");
                                }
                            }
                            catch
                            {
                                Debug.WriteLine("error in calculating transmissivity " + row + " " + col);
                            }
                        }
                        T_fac[row, col] = transmissivity_m2_hr;
                    }
                    else
                    {
                        T_fac[row, col] = nodata_value;
                    }

                }
            }

        }

        void calc_saturated_density(double particle_density_kg_m3)
        {
            //saturated density (a double 2D array will be calculated in two steps: 
            //first porosity, from soil bulk density and particle density
            //then filling the pores with water to calculate bulk density
            //we'll do this for the entire soildepth
            double localdepth_m, localmass_kg, profile_dry_bd_kg_m3, porosity_fraction;

            localmass_kg = 0;
            for (int row = 0; row < nr; row++)
            {
                for (int col = 0; col < nc; col++)
                {
                    if (dtm[row, col] != nodata_value)
                    {
                        localdepth_m = soildepth_m[row, col];
                        localmass_kg = 0;
                        for (int layer = 0; layer < max_soil_layers; layer++)
                        {
                            localmass_kg += texture_kg[row, col, layer, 0] + texture_kg[row, col, layer, 1] + texture_kg[row, col, layer, 2] + texture_kg[row, col, layer, 3] + texture_kg[row, col, layer, 4] + young_SOM_kg[row, col, layer] + old_SOM_kg[row, col, layer];
                            if (local_soil_mass_kg < 0)
                            {
                                Debug.WriteLine("err_negative_soil_mass");
                            }
                        }
                        profile_dry_bd_kg_m3 = localmass_kg / (dx * dx * localdepth_m);
                        porosity_fraction = 1 - (profile_dry_bd_kg_m3 / particle_density_kg_m3);
                        //we know that with saturated bulk density, all this prosity is filled with water at 1000 kg m3, so:
                        sat_bd_kg_m3[row, col] = porosity_fraction * 1000 + (1 - porosity_fraction) * profile_dry_bd_kg_m3;
                        /*if(Double.IsNaN(sat_bd_kg_m3[row, col])){
                            Debug.WriteLine(" Saturated BD is NaN at " + row + " " + col);            
                        }*/
                    }
                    else { sat_bd_kg_m3[row, col] = nodata_value; }
                }
            }
        }

        double store_slid_mass_new(double extra_slid_thickness_m, int donorrow, int donorcol)
        {
            //knowing the depth eroded from a certain donor cell, takes that amount in kg, stores it in the sediment in transport matrix and restores soildata at the donor cell

            //three tasks:
            //A walk through layers from top to bottom until ero_slid_m is reached
            //B at each layer take all or part of the mass present and store it in sediment_in_transport_kg
            //C fix soil data when done
            //Debug.WriteLine(" entered store_slid_mass for row " + donorrow + " col" + donorcol);
            double eroded_mass_kg = 0;

            if (Double.IsInfinity(extra_slid_thickness_m))
            {
                Debug.WriteLine("stop");
            }
            try
            {
                double remaining_depth_m = extra_slid_thickness_m;
                //double new_ls_mass_kg = 0;
                int layer_slide = 0;
                double slidfraction = 0;
                while (remaining_depth_m > 0.00001)
                {
                    if (remaining_depth_m >= layerthickness_m[donorrow, donorcol, layer_slide])
                    {
                        slidfraction = 1;
                    }
                    else
                    {
                        slidfraction = remaining_depth_m / layerthickness_m[donorrow, donorcol, layer_slide];
                    }
                    //Debug.WriteLine(" remaining depth " + remaining_depth_m + " to be subtracted " + layerthickness_m[donorrow, donorcol, layer_slide] + " x " + slidfraction);
                    if (slidfraction < 0 || slidfraction > 1)
                    {
                        Debug.WriteLine("impossible value for slidfraction");
                    }
                    for (int ti = 0; ti < n_texture_classes; ti++)
                    {
                        sediment_in_transport_kg[donorrow, donorcol, ti] += texture_kg[donorrow, donorcol, layer_slide, ti] * slidfraction;
                        eroded_mass_kg += texture_kg[donorrow, donorcol, layer_slide, ti] * slidfraction;
                        //Debug.WriteLine("adding " + texture_kg[donorrow, donorcol, layer_slide, ti] * slidfraction + " to sed in trans, now " + sediment_in_transport_kg[donorrow, donorcol, ti]);
                        texture_kg[donorrow, donorcol, layer_slide, ti] -= texture_kg[donorrow, donorcol, layer_slide, ti] * slidfraction;

                    }
                    young_SOM_in_transport_kg[donorrow, donorcol] += young_SOM_kg[donorrow, donorcol, layer_slide] * slidfraction;
                    old_SOM_in_transport_kg[donorrow, donorcol] += old_SOM_kg[donorrow, donorcol, layer_slide] * slidfraction;
                    eroded_mass_kg += young_SOM_kg[donorrow, donorcol, layer_slide] * slidfraction;
                    eroded_mass_kg += old_SOM_kg[donorrow, donorcol, layer_slide] * slidfraction;
                    old_SOM_kg[donorrow, donorcol, layer_slide] -= old_SOM_kg[donorrow, donorcol, layer_slide] * slidfraction;
                    young_SOM_kg[donorrow, donorcol, layer_slide] -= young_SOM_kg[donorrow, donorcol, layer_slide] * slidfraction;
                    remaining_depth_m -= layerthickness_m[donorrow, donorcol, layer_slide] * slidfraction;
                    //Debug.WriteLine(" remaining depth " + remaining_depth_m + " subtracted " + layerthickness_m[donorrow, donorcol, layer_slide] * slidfraction);
                    layer_slide++;
                }
                //C now reset layerthickness and the entire soil profile, and the elevation rasters
                try { remove_empty_layers(donorrow, donorcol); } catch { Debug.WriteLine("failure in update all layer thicknesses"); }
                try { update_all_layer_thicknesses(donorrow, donorcol); } catch { Debug.WriteLine("failure in update all layer thicknesses"); }
                double old_thickness = soildepth_m[donorrow, donorcol];
                double new_thickness = total_soil_thickness(donorrow, donorcol);
                if (new_thickness == 0)
                {
                    //Debug.WriteLine(" soil completely removed at " + donorrow + " " + donorcol);
                }
                else if (new_thickness < 0)
                {
                    Debug.WriteLine(" negative soil thickness at " + donorrow + " " + donorcol);
                }
                if (Double.IsInfinity(extra_slid_thickness_m))
                {
                    Debug.WriteLine("stop");
                }
                //Debug.WriteLine(" finished for cell " + donorrow + " " + donorcol);
            }
            catch
            {
                Debug.WriteLine("failure in store_slid_mass");
            }
            soildepth_m[donorrow, donorcol] = total_soil_thickness(donorrow, donorcol);
            return eroded_mass_kg;
        }

        double deposit_slid_mass_new(double depofraction, int deprow, int depcol)
        {
            double locally_deposited_mass_kg = 0;
            try
            {
                //we know how much of the seven materials is available here (via ediment in transport),
                //and how much of it we deposit (via depofraction)
                //now let's add that mass to the first layer of the soil here:
                //Debug.WriteLine("depositing landslide material on " + deprow + " " + depcol + " slide " + slidenumber);
                //Debug.WriteLine("fraction " + fraction_of_mass);
                if (depofraction > 1) { Debug.WriteLine(" ERROR: fraction of mass above 1 "); }
                if (depofraction < 0) { Debug.WriteLine(" ERROR: fraction of mass below 0 "); }
                else
                {
                    for (int ti = 0; ti < n_texture_classes; ti++)
                    {
                        texture_kg[deprow, depcol, 0, ti] += sediment_in_transport_kg[deprow, depcol, ti] * depofraction;
                        locally_deposited_mass_kg += sediment_in_transport_kg[deprow, depcol, ti] * depofraction;
                        sediment_in_transport_kg[deprow, depcol, ti] -= sediment_in_transport_kg[deprow, depcol, ti] * depofraction;
                        //Debug.WriteLine("taking " + sediment_in_transport_kg[deprow, depcol, ti] * depofraction + " from sed in trans, now " + sediment_in_transport_kg[deprow, depcol, ti]);
                    }
                    young_SOM_kg[deprow, depcol, 0] += young_SOM_in_transport_kg[deprow, depcol] * depofraction;
                    old_SOM_kg[deprow, depcol, 0] += old_SOM_in_transport_kg[deprow, depcol] * depofraction;
                    locally_deposited_mass_kg += young_SOM_in_transport_kg[deprow, depcol] * depofraction;
                    locally_deposited_mass_kg += old_SOM_in_transport_kg[deprow, depcol] * depofraction;
                    young_SOM_in_transport_kg[deprow, depcol] -= young_SOM_in_transport_kg[deprow, depcol] * depofraction;
                    old_SOM_in_transport_kg[deprow, depcol] -= old_SOM_in_transport_kg[deprow, depcol] * depofraction;
                    //so, that's done. Now reestablish soil layers and accounting at this location
                    update_all_layer_thicknesses(deprow, depcol);
                    double old_thickness = soildepth_m[deprow, depcol];
                    double new_thickness_m = total_soil_thickness(deprow, depcol);
                    if (new_thickness_m > 10)
                    {
                        Debug.WriteLine("soil changed from " + old_thickness + "m thick to " + new_thickness_m + "m thick");
                    }
                }
            }
            catch
            {
                Debug.WriteLine("failure in deposit_slid_mass");
            }
            if (locally_deposited_mass_kg == 0)
            {
                Debug.WriteLine(" error: almost no mass used ");
            }
            return locally_deposited_mass_kg;
        }

        void calculate_critical_rain()    //Calculates Critical Steady State Rainfall for Landsliding    
        {
            //Debug.WriteLine(" calculating critical rainfall amounts per cell");
            double particle_density_kg_m3 = 2700;
            //first update all matrix values that we get from LORICA's state variables, using PTFs from literature
            calc_friction_angles();
            calc_transmissivity();
            calc_saturated_density(particle_density_kg_m3);
            calc_soil_cohesion_factor();

            double beta;
            nb_ok = 0; nb_check = 0; all_grids = 0;
            maximum_allowed_deposition = large_negative_number; dh_tol = 0.00025;
            for (int row = 0; row < nr; row++)
            {
                for (int col = 0; col < nc; col++)
                {
                    camf[row, col] = 1;    // contributing area multiple flow matrix = 1
                    stslope_radians[row, col] = 0;
                    crrain_m_d[row, col] = nodata_value;
                }
            }
            int runner;
            for (runner = number_of_data_cells - 1; runner >= 0; runner--)
            {           // the index is sorted from low to high values, but flow goes from high to low
                row = row_index[runner]; col = col_index[runner];
                // into loop for surounding grids of certain grid
                // Start first the slope_sum loop for all lower neighbour grids
                powered_slope_sum = 0; max_allowed_erosion = 0; dz_min = large_negative_number;
                direction = 20; dz_max = -1; dhtemp = -8888; maximum_allowed_deposition = (large_negative_number);

                // Repeat the loop to determine flow if all draining neighbours are known
                // but do this only once
                for (i = (-1); i <= 1; i++)
                {
                    for (j = (-1); j <= 1; j++)
                    {
                        dh = 000000; dh1 = 000; dhtemp = large_negative_number; d_x = dx;
                        if (((row + i) >= 0) && ((row + i) < nr) &&   // boundaries
                        ((col + j) >= 0) && ((col + j) < nc) &&
                       !((i == 0) && (j == 0)))
                        {
                            dh = (dtm[row, col] - dtm[row + i, col + j]);
                            if ((row != row + i) && (col != col + j)) { d_x = dx * Math.Sqrt(2); } else { d_x = dx; }
                            if (dh < 000000)
                            {// i j is a higher neighbour
                                if (dh > dz_min) { dz_min = dh; }
                                if ((dh < 000000))
                                {// i j is a higher neighbour
                                    if (dh1 > maximum_allowed_deposition) { maximum_allowed_deposition = (dh1); }
                                }
                            }
                            if (dh > 000000)
                            {// i j is a lower neighbour
                                if ((dh > 000000))
                                {
                                    if (dh1 > max_allowed_erosion - dh_tol) { max_allowed_erosion = (dh1 - dh_tol); }
                                }
                                dh = dh / d_x;
                                if (dh > dz_max) { dz_max = dh; direction = (i * 3 + 5 + j); }
                                dh = Math.Pow(dh, conv_fac);
                                powered_slope_sum = powered_slope_sum + dh;
                            }//end if
                        }//end if
                    }//end for
                }//end for
                if (maximum_allowed_deposition == large_negative_number) { maximum_allowed_deposition = 0; } else { maximum_allowed_deposition = -maximum_allowed_deposition; }
                if (max_allowed_erosion == 0.0) { max_allowed_erosion = -dh_tol; } else { max_allowed_erosion = -max_allowed_erosion; }
                for (i = (-1); i <= 1; i++)
                {
                    for (j = (-1); j <= 1; j++)
                    {
                        dh = 000000; fraction = 0;
                        frac_dis = 0;
                        d_x = dx;
                        if (((row + i) >= 0) && ((row + i) < nr) && ((col + j) >= 0) && ((col + j) < nc) && !((i == 0) && (j == 0)))
                        {
                            dh = (dtm[row, col] - dtm[row + i, col + j]);
                            // Multiple Flow: If there are lower neighbours start evaluating
                            if (dh > 000000)
                            { // multiple flow
                              // fraction of discharge into a neighbour grid
                                if ((row != row + i) && (col != col + j)) { d_x = dx * Math.Sqrt(2); } else { d_x = dx; }
                                slope_tan = dh / d_x;
                                dh = dh / d_x;
                                dh = Math.Pow(dh, conv_fac);
                                fraction = (dh / powered_slope_sum); // multiple flow
                                frac_dis = (camf[row, col] * fraction);
                                camf[row + i, col + j] += frac_dis;
                            }//end if
                        }//end if boarders
                    }//end for j
                }//end for i
            }   // end for runner - now we know contributing area everywhere

            // Calculation of steepest descent local slope, trying all 8 options, and then critical rainfall
            for (row = 0; row < nr; row++)
            {
                for (col = 0; col < nc; col++)
                {
                    if (dtm[row, col] != nodata_value)
                    {
                        direction = 20; dz_max = -1;
                        for (i = (-1); i <= 1; i++)
                        {
                            for (j = (-1); j <= 1; j++)
                            {
                                dh = 000000;
                                if (((row + i) >= 0) && ((row + i) < nr) && ((col + j) >= 0) && ((col + j) < nc) && !((i == 0) && (j == 0)))
                                {
                                    dh = (dtm[row, col] - dtm[row + i, col + j]);
                                    if ((row != row + i) && (col != col + j)) { d_x = dx * Math.Sqrt(2); } else { d_x = dx; }
                                    if (dh > 000000)
                                    {// i j is a lower neighbour
                                        dh = dh / d_x;
                                        if (dh > dz_max) { dz_max = dh; direction = (i * 3 + 5 + j); }
                                    }//end if
                                }//end if
                            }//end for
                        }//end for
                         //we now know the steepest local slope, and we have saved the direction that that is in, in the variable 'direct' 
                         //now let's calculate critical rainfall
                        for (i = (-1); i <= 1; i++)
                        {
                            for (j = (-1); j <= 1; j++)
                            {

                                dh = 000000;
                                if (((row + i) >= 0) && ((row + i) < nr) && ((col + j) >= 0) && ((col + j) < nc) && !((i == 0) && (j == 0)))
                                {
                                    if (dtm[row + i, col + j] != nodata_value)
                                    {
                                        dh = (dtm[row, col] - dtm[row + i, col + j]);
                                        if ((row != row + i) && (col != col + j)) { d_x = dx * Math.Sqrt(2); } else { d_x = dx; }
                                        //the following line is a trick to ensure that only steepest descent directions are taken. "direction" was stored earlier as a calculation from i and j
                                        if ((i * 3 + 5 + j) == direction)
                                        { // steepest descent
                                            stslope_radians[row, col] = Math.Atan(dh / d_x);
                                            // Calculation of CRITICAL RAINFALL value = relative landslide hazard, along steepest descent local slope
                                            // we must assume a depth of landsliding to calculate all the soil properties over.
                                            // We now assume that the entire soildepth determines these properties.
                                            beta = (T_fac[row, col] * (Math.Sin(stslope_radians[row, col]))
                                                * (dx / (camf[row, col] * dx * dx)) * sat_bd_kg_m3[row, col]
                                                * (1 - ((Math.Sin(stslope_radians[row, col]) - Cohesion_factor[row, col]) / ((Math.Tan(peak_friction_angle_radians[row, col]) * Math.Cos(stslope_radians[row, col])))))); // 'valid' critical rainfall value
                                            /*if (beta < 0)
                                            {
                                                Debug.WriteLine("problem");
                                            }*/
                                            //if (((sat_bd_kg_m3[row, col] * Math.Sin(stslope_radians[row, col])) + ((1 - sat_bd_kg_m3[row, col]) * Math.Cos(stslope_radians[row, col]) * Math.Tan(peak_friction_angle_radians[row, col]))) <= ((sat_bd_kg_m3[row, col]) * (Cohesion_factor[row, col])))
                                            if (Math.Tan(stslope_radians[row, col]) <= (Cohesion_factor[row, col] / Math.Cos(stslope_radians[row, col]) + (1 - (1000 / sat_bd_kg_m3[row, col])) * Math.Tan(peak_friction_angle_radians[row, col])))
                                            {
                                                beta = -9999; // unconditionally stable
                                            }
                                            if (Math.Tan(stslope_radians[row, col]) > (Math.Tan(peak_friction_angle_radians[row, col]) + (Cohesion_factor[row, col] / Math.Cos(stslope_radians[row, col]))))
                                            {
                                                beta = 0;  //unconditionally unstable
                                            }
                                            //places that are both always unstable and always stable (should not be the case):
                                            if (Math.Tan(peak_friction_angle_radians[row, col]) < (1 - (1000 / sat_bd_kg_m3[row, col])) * Math.Tan(peak_friction_angle_radians[row, col]))
                                            {
                                                beta = 11;  //both at the same time!
                                            }
                                            crrain_m_d[row, col] = (beta);
                                            //Debug.WriteLine( "critical rain for " + row + " " + col + " " + crrain[row,col] + " T_fac " + T_fac[row, col] + " stslope_sin " + Math.Sin(stslope[row, col]) + " upstream " + camf[row,col] + "\n bulkd " + bulkd[row, col] + " C_fac " + C_fac[row, col] + " intfr " + Math.Tan(intfr[row, col]) + " stslope_cos " + Math.Cos(stslope[row, col]) );
                                        }
                                    }
                                }//end if
                            }//end for
                        }//end for
                    } // end no data loop
                } // end for
            } // end for 
            if (t % 20 == 0)
            {
                out_double(workdir + "\\" + run_number + "_" + t + "_critrain_m_d.asc", crrain_m_d);
                out_double(workdir + "\\" + run_number + "_" + t + "_peakfrictangle_radians.asc", peak_friction_angle_radians);
                out_double(workdir + "\\" + run_number + "_" + t + "_cohesion_factor.asc", Cohesion_factor);
                out_double(workdir + "\\" + run_number + "_" + t + "_residfrictangle_radians.asc", resid_friction_angle_radians);
                out_double(workdir + "\\" + run_number + "_" + t + "_transmissivity_m2_d.asc", T_fac);
            }
        }

        void steepdesc(int rowst, int colst)
        {
            try
            {
                int trow;
                int tcol;
                double sloper, sloper_max = 0;
                trow = rowst;
                tcol = colst;
                xrow = 0; xcol = 0;
                powered_slope_sum = 0;
                //minimaps(rowst, colst);
                for (i = (-1); i <= 1; i++)
                {
                    for (j = (-1); j <= 1; j++)
                    {
                        dh = 0; sloper = 0; dh1 = 000; dhtemp = large_negative_number; d_x = dx;
                        if (((trow + i) >= 0) && ((trow + i) < nr) &&   // boundaries
                            ((tcol + j) >= 0) && ((tcol + j) < nc) &&
                            !((i == 0) && (j == 0)))
                        {
                            if (dtm[trow + i, tcol + j] != nodata_value)
                            {
                                dh = (dtm[trow, tcol] - dtm[trow + i, tcol + j]);
                                if ((trow != trow + i) && (tcol != tcol + j)) { d_x = dx * Math.Sqrt(2); } else { d_x = dx; }
                                if (dh < 000000)
                                {// i j is a higher neighbour
                                    if (dh > dz_min) { dz_min = dh; }
                                    if ((dh < 000000))
                                    {// i j is a higher neighbour
                                        if (dh1 > maximum_allowed_deposition) { maximum_allowed_deposition = (dh1); }
                                    }
                                }
                                if (dh > 000000)
                                {// i j is a lower neighbour
                                    if (dh1 > max_allowed_erosion - dh_tol) { max_allowed_erosion = (dh1 - dh_tol); }
                                    sloper = dh / d_x;
                                    if (sloper > sloper_max)
                                    {
                                        sloper_max = sloper;
                                        direction = (i * 3 + 5 + j);
                                        xrow = trow + i;
                                        xcol = tcol + j;
                                    }
                                }//end if
                            }
                        }//end if
                    }//end for
                }//end for
                if (maximum_allowed_deposition == large_negative_number) { maximum_allowed_deposition = 0; } else { maximum_allowed_deposition = (maximum_allowed_deposition * (-1)); }
                if (max_allowed_erosion == 0) { max_allowed_erosion = dh_tol * -1; } else { max_allowed_erosion = (max_allowed_erosion * (-1)); }
            }
            catch { Debug.WriteLine("failed during search for steepest descent neighbour"); }
        }
        void calculate_slide_new()
        {

            Debug.WriteLine("started new land sliding at time " + t);
            //Mostafa, these values should be set in the interface by the user:
            double minimum_slope_for_movement_tan = 0.7;
            double runout_ratio = 0.35; //in horizontal meters covered by the deposit PER vertical meters of the eroding part of a landslide. Ratio is static, hordist can grow and shrink repeatedly with topography
            double LS_conv_fac = 1.75; // determines how much eroding material is distributed sideways

            double initiation_volume_m3 = 0, continuation_volume_m3 = 0, deposition_volume_m3 = 0, requested_deposition_volume_m3 = 0;
            double average_rainfall_intensity_m_d = 0, sum_rainfall_intensity_m_d = 0;
            decimal initiation_mass_kg = 0, continuation_mass_kg = 0, deposition_mass_kg = 0;
            try
            {
                Task.Factory.StartNew(() =>
                {
                    this.InfoStatusPanel.Text = "new landslide calculation";
                }, CancellationToken.None, TaskCreationOptions.None, guiThread);
                nb_ok = 0; nb_check = 0; all_grids = 0.0;
                maximum_allowed_deposition = large_negative_number; dh_tol = 0.00025; erotot_m = 0.0;
                for (row = 0; row < nr; row++)
                {
                    for (col = 0; col < nc; col++)
                    {
                        if (dtm[row, col] != nodata_value)
                        {
                            ero_slid_m[row, col] = 0;
                            sed_slid_m[row, col] = 0;
                            slidestatus[row, col] = 0;
                            remaining_vertical_size_m[row, col] = 0;
                            for (int material = 0; material < 5; material++)
                            {
                                sediment_in_transport_kg[row, col, material] = 0;
                            }
                            young_SOM_in_transport_kg[row, col] = 0;
                            old_SOM_in_transport_kg[row, col] = 0;
                        }
                    }
                }
                //Debug.WriteLine("prepared");
                // we will now go past all cells, and first decide whether one of three things is true:
                // A: a landslide can initiate here (if crrain_mm_d < actual rainfall)
                // B: a landslide can continue (if there are already landslide deposits here, and the slope is steep enough )
                // C: a landslide can deposit here ( if there are already ls deposits here, but the slope is not steep enough )
                int runner;
                for (runner = number_of_data_cells - 1; runner >= 0; runner--)
                {           // the list of cells (index) is sorted from low to high values, but flow goes from high to low
                            //so, we will now walk from highest cell to next lower cell, etc, not necessarily to a direct neighbour.
                    row = row_index[runner]; col = col_index[runner];
                    powered_slope_sum = 0.0; max_allowed_erosion = 0.0; dz_min = large_negative_number; d_x = dx;
                    dz_max = -1.0; dhtemp = large_negative_number; maximum_allowed_deposition = large_negative_number;
                    bool situation_A = false, situation_B = false, situation_C = false, situation_D = false;

                    //first, calculating steepest local slope:
                    steepdesc(row, col); // this changes the value of global variables xrow,xcol so that those reference the steepest lower neighbour
                    if (xrow == row | xcol == col) { d_x = dx; } else { d_x = dx * Math.Sqrt(2); }
                    double steepestslope_tan = (dtm[row, col] - dtm[xrow, xcol]) / d_x;
                    //calculating current local rain intensity:
                    if (check_space_rain.Checked == true) { rain_value_m = rain_m[row, col]; }
                    rain_intensity_m_d = Convert.ToDouble(text_ls_rel_rain_intens.Text) * rain_value_m;
                    //make sure we don't always hit that rain intensity in the same way:
                    Random random = new Random();
                    double n = random.NextDouble() / 2 + 0.6;
                    rain_intensity_m_d = rain_intensity_m_d * n;
                    sum_rainfall_intensity_m_d += rain_intensity_m_d;
                    //Debug.WriteLine("this year's rain factor is " + n);


                    //now let's calculate which situation applies to this cell:

                    if (crrain_m_d[row, col] > 0.0 && rain_intensity_m_d > crrain_m_d[row, col])
                    {
                        situation_A = true;// A: a landslide will initiate here (because crrain_m_d < actual rainfall) -
                                           // even if there was already a landslide above us, moving down to here
                    }
                    else
                    {
                        //so, we don't initiate a landslide here. Do we continue one? Do we deposit?
                        if (remaining_vertical_size_m[row, col] > 0)
                        {
                            //ok, we have a landslide here, and there should be sediment from it. Do we continue by eroding, or depositing?
                            if (steepestslope_tan > minimum_slope_for_movement_tan)
                            {
                                situation_B = true; //B: an existing landslide will MAYBE continue here (we have stuff AND slope)
                            }
                            else
                            {
                                situation_C = true; //C: an existing landslide will deposit here (we have stuff WITHOUT slope}
                            }
                        }
                        else
                        {
                            situation_D = true; //do nothing: there is no landslide to continue or deposit from
                        }
                    }

                    //at this point, we know what should happen at this cell. Now let's do it. It's mostly similar for situations A and B:
                    if (situation_A == true || situation_B == true)
                    {
                        //initiation and continuation:
                        //erode, route downhill, and administer:
                        //per https://doi.org/10.1016/j.geomorph.2006.06.039 and previous pubs:
                        double desired_erosion_m = (sat_bd_kg_m3[row, col] / 1000) * Math.Cos(Math.Atan(steepestslope_tan)) * (steepestslope_tan - minimum_slope_for_movement_tan) / Cohesion_factor[row, col];
                        double actual_erosion_m = Math.Min(desired_erosion_m, soildepth_m[row, col]);
                        //actual_erosion_m must be a zer or  a positive number. We are either eroding the entire soil, or however much we wanted to erode
                        //record this erosion in ero_slid_m, but don't change the dtm until we have considered all cells:
                        //if actual_erosion_m is zero: we don't do anything, but we do transport any possible sed_in_trans further down to the next cells
                        if (actual_erosion_m > 0)
                        {
                            ero_slid_m[row, col] = -actual_erosion_m;
                            if (situation_A == true)
                            {
                                initiation_volume_m3 += actual_erosion_m * dx * dx;
                                initiation_mass_kg += Convert.ToDecimal(store_slid_mass_new(actual_erosion_m, row, col));
                            }
                            else
                            {
                                continuation_volume_m3 += actual_erosion_m * dx * dx;
                                continuation_mass_kg += Convert.ToDecimal(store_slid_mass_new(actual_erosion_m, row, col));
                            }
                        }
                        else
                        { //so we didn't erode anything, usually because there was no soil. That's OK.

                        }
                        //now, take away from the existing soil and add to material in transport
                        //then route material in transport to downhill cells, so first calculate powered slope sum for this cell:
                        powered_slope_sum = 0;
                        double DANGER_extra_factor_ArT_GSA2024 = 0.09;
                        for (int i = -1; i <= 1; i++)
                        {
                            for (int j = -1; j <= 1; j++)
                            {
                                if (((row + i) >= 0) && ((row + i) < nr) && ((col + j) >= 0) && ((col + j) < nc) && !((i == 0) && (j == 0)))
                                {
                                    dh = 0;
                                    if (dtm[row + i, col + j] != nodata_value)
                                    {
                                        dh = dtm[row, col] - dtm[row + i, col + j];
                                        if (dh > 0)
                                        {// i j is a lower neighbour
                                            if ((row != row + i) && (col != col + j)) { d_x = dx * Math.Sqrt(2); } else { d_x = dx; }
                                            slope_tan = dh / d_x;
                                            if (slope_tan > minimum_slope_for_movement_tan + DANGER_extra_factor_ArT_GSA2024) // only cells that are steeply enough under this cell, can be recipients
                                            {
                                                double powered_slope = Math.Pow(slope_tan, LS_conv_fac);
                                                powered_slope_sum += powered_slope;
                                            }
                                        }//end if
                                    }
                                }//end if
                            }//end for j
                        }//end for i
                         //we now know powered slope sum 
                         //now clculate the fraction for this cell:
                        for (int i = -1; i <= 1; i++)
                        {
                            for (int j = -1; j <= 1; j++)
                            {
                                dh = 0;
                                if (((row + i) >= 0) && ((row + i) < nr) && ((col + j) >= 0) && ((col + j) < nc) && !((i == 0) && (j == 0)))
                                {
                                    if (dtm[row + i, col + j] != nodata_value)
                                    {
                                        dh = dtm[row, col] - dtm[row + i, col + j];
                                        if (dh > 0)
                                        {// i j is a lower neighbour
                                            if ((row != row + i) && (col != col + j)) { d_x = dx * Math.Sqrt(2); } else { d_x = dx; }
                                            slope_tan = dh / d_x;
                                            if (slope_tan > minimum_slope_for_movement_tan + DANGER_extra_factor_ArT_GSA2024)
                                            {
                                                double powered_slope = Math.Pow(slope_tan, LS_conv_fac);
                                                fraction = powered_slope / powered_slope_sum;
                                                for (int tex = 0; tex < n_texture_classes; tex++)
                                                {
                                                    sediment_in_transport_kg[row + i, col + j, tex] += fraction * sediment_in_transport_kg[row, col, tex];
                                                    //Debug.WriteLine("routed " + texture_kg[donorrow, donorcol, layer_slide, ti] * slidfraction + " to sed in trans, now " + sediment_in_transport_kg[donorrow, donorcol, ti]);
                                                }
                                                young_SOM_in_transport_kg[row + i, col + j] += fraction * young_SOM_in_transport_kg[row, col];
                                                old_SOM_in_transport_kg[row + i, col + j] += fraction * old_SOM_in_transport_kg[row, col];
                                                remaining_vertical_size_m[row + i, col + j] = Math.Max(remaining_vertical_size_m[row + i, col + j], (dtm[row, col] - dtm[row + i, col + j]) + remaining_vertical_size_m[row, col]);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        //so, all cells under this cell have received the sediment in transport and OM in transport that they need.
                        //We will work on those cells when it's their turn but not now.
                        //for now, we are done with this cell, and it does no longer have sediment in transport:
                        for (int tex = 0; tex < n_texture_classes; tex++)
                        {
                            sediment_in_transport_kg[row, col, tex] = 0;
                        }
                        young_SOM_in_transport_kg[row, col] = 0;
                        old_SOM_in_transport_kg[row, col] = 0;
                        //final administration:
                        if (situation_A == true)
                        {
                            //initiation
                            slidestatus[row, col] = 1;
                            landslide_initiation_cells += 1;
                        }
                        else if (situation_B == true)
                        {
                            //continuation
                            slidestatus[row, col] = 2;
                            landslide_continuation_cells += 1;
                        }
                    }

                    if (situation_C == true) // deposition
                    {
                        //deposit, route downhill, and administer
                        //we would like to deposit everything we have in transport, but we can only deposit a fraction of that because of the momentum.
                        //we use a literature-based runout-ratio  -> remaining_vertical_size_m * runout ratio is how far this slide can still travel in m
                        double remaining_runout_distance_cells = remaining_vertical_size_m[row, col] / dx * runout_ratio;
                        if (remaining_runout_distance_cells > 1000)
                        {
                            Debug.WriteLine("hold it right there ");
                        }
                        double[] available_mass_kg = { 0, 0, 0, 0, 0 };
                        //let's calculate how much landslide mass and thickness we have available:
                        for (int size = 0; size < n_texture_classes; size++)
                        {
                            available_mass_kg[size] = sediment_in_transport_kg[row, col, size];
                        }
                        double available_landslide_material_m = calc_thickness_from_mass(available_mass_kg, young_SOM_in_transport_kg[row, col], old_SOM_in_transport_kg[row, col]);

                        //Debug.WriteLine(" approximate bd of material to be deposited: " + ( available_mass_kg.Sum() / (available_landslide_material_m * dx * dx)) + " kg/m3");
                        //OK, we know what we have available, and what its remaining momentum=runout distance is                        
                        double requested_deposition_m = available_landslide_material_m;
                        if (remaining_runout_distance_cells > 1)
                        {
                            // limit deposition here to a fraction determined by the remaining runout:
                            requested_deposition_m /= remaining_runout_distance_cells;
                        }
                        //now, let's check how much we can deposit without creating a peak: a bit less than the highest higher nb cell, or nothing if there is no cell like that.
                        //so, lets look around at all possible higher cells to find that out
                        double maximum_allowed_deposition_m = 0;
                        powered_slope_sum = 0;
                        for (int i = -1; i <= 1; i++)
                        {
                            for (int j = -1; j <= 1; j++)
                            {
                                dh = 0; dh1 = 0;
                                if (((row + i) >= 0) && ((row + i) < nr) && ((col + j) >= 0) && ((col + j) < nc) && !((i == 0) && (j == 0)))
                                {
                                    if (dtm[row + i, col + j] != nodata_value)
                                    {
                                        dh = dtm[row, col] - dtm[row + i, col + j];
                                        if (dh <= 0)
                                        {
                                            // i j was originally a higher neighbour, but it may have eroded by now from sliding
                                            //let's see how much/whether it is currently higher than the previous lowest higher neighbour, taking into account erosion of the overlying cell and deposition on the overlying cell.
                                            dh1 = (dtm[row + i, col + j] + ero_slid_m[row + i, col + j] + sed_slid_m[row + i, col + j]) - dtm[row, col];
                                            if (dh1 > maximum_allowed_deposition_m) { maximum_allowed_deposition_m = dh1; }
                                        }
                                    }
                                }//end if
                            }//end for j
                        }//end for i
                         //we now know how much we CAN deposit here. It's equal to maximum_allowed_deposition_m. 
                         //we pick the largest possible amount - either what topography allows (maximum_allowed), or what we insist on (requested). 
                         //maximum_allowed_deposition_m = Math.Max(maximum_allowed_deposition_m, requested_deposition_m);
                         //alternatively, we pick the minimum: we deposit what we would like, unless that amount is too much, in which case we take less:
                        maximum_allowed_deposition_m = Math.Min(maximum_allowed_deposition_m, requested_deposition_m);
                        //But we do of course limit ourselves to the entire amount that we want to deposit:
                        maximum_allowed_deposition_m = Math.Min(maximum_allowed_deposition_m, available_landslide_material_m);
                        double deposited_landslide_fraction = maximum_allowed_deposition_m / available_landslide_material_m;
                        sed_slid_m[row, col] += maximum_allowed_deposition_m;
                        deposition_volume_m3 += maximum_allowed_deposition_m * dx * dx;
                        decimal previous_dep_mass_kg = deposition_mass_kg;
                        deposition_mass_kg += Convert.ToDecimal(deposit_slid_mass_new(deposited_landslide_fraction, row, col));
                        try
                        {
                            if (maximum_allowed_deposition_m > 0)
                            {
                                decimal dep_bd_kg_m3 = (deposition_mass_kg - previous_dep_mass_kg) / Convert.ToDecimal(maximum_allowed_deposition_m * dx * dx);
                                if (Convert.ToBoolean(Convert.ToDouble(dep_bd_kg_m3) < 1))
                                {
                                    Debug.WriteLine("approximate bd of material just deposited: " + dep_bd_kg_m3 + " kg/m3 at " + row + " " + col + " cell dist " + remaining_runout_distance_cells);
                                }
                            }
                        }
                        catch { }
                        //move downstream with everything else
                        //we don't need to know how many meters of  material are left - it's the material in transport (kg) that we need to distribute
                        double undeposited_landslide_fraction = 1 - deposited_landslide_fraction;
                        if (undeposited_landslide_fraction > 0)
                        {
                            for (int i = -1; i <= 1; i++)
                            {
                                for (int j = -1; j <= 1; j++)
                                {
                                    dh = 0; dh1 = 0;
                                    if (((row + i) >= 0) && ((row + i) < nr) && ((col + j) >= 0) && ((col + j) < nc) && !((i == 0) && (j == 0)))
                                    {
                                        if (dtm[row + i, col + j] != nodata_value)
                                        {
                                            dh = dtm[row, col] + sed_slid_m[row, col] - dtm[row + i, col + j];
                                            if (dh > 0)
                                            {// i j is a lower neighbour (where by definition there has not yet been erosion)
                                                if ((row != row + i) && (col != col + j)) { d_x = dx * Math.Sqrt(2); } else { d_x = dx; }
                                                slope_tan = dh / d_x;
                                                double powered_slope = Math.Pow(slope_tan, LS_conv_fac);
                                                powered_slope_sum += powered_slope;
                                            }//end if
                                        }
                                    }//end if
                                }//end for j
                            }//end for i
                            if (powered_slope_sum == 0) { Debug.WriteLine("there was vertical space to deposit, but no lower space to send sediment to. Sediment left in transport at " + row + " " + col); }
                            else
                            {
                                for (int i = -1; i <= 1; i++)
                                {
                                    for (int j = -1; j <= 1; j++)
                                    {
                                        if (((row + i) >= 0) && ((row + i) < nr) && ((col + j) >= 0) && ((col + j) < nc) && !((i == 0) && (j == 0)))
                                        {
                                            if (dtm[row + i, col + j] != nodata_value)
                                            {
                                                dh = dtm[row, col] + sed_slid_m[row, col] - dtm[row + i, col + j];
                                                if (dh > 0)
                                                {// i j is a lower neighbour (where by definition there has not yet been erosion)
                                                    if ((row != row + i) && (col != col + j)) { d_x = dx * Math.Sqrt(2); } else { d_x = dx; }
                                                    slope_tan = dh / d_x;
                                                    double powered_slope = Math.Pow(slope_tan, LS_conv_fac);
                                                    double distribution_fraction = powered_slope / powered_slope_sum;
                                                    for (int tex = 0; tex < n_texture_classes; tex++)
                                                    {
                                                        sediment_in_transport_kg[row + i, col + j, tex] += distribution_fraction * undeposited_landslide_fraction * sediment_in_transport_kg[row, col, tex];
                                                    }
                                                    young_SOM_in_transport_kg[row + i, col + j] += distribution_fraction * undeposited_landslide_fraction * young_SOM_in_transport_kg[row, col];
                                                    old_SOM_in_transport_kg[row + i, col + j] += distribution_fraction * undeposited_landslide_fraction * old_SOM_in_transport_kg[row, col];
                                                    remaining_vertical_size_m[row + i, col + j] = Math.Max(remaining_vertical_size_m[row + i, col + j], (remaining_runout_distance_cells - (d_x / dx)) * dx / runout_ratio);
                                                }//end if
                                            } // end if
                                        }//end if
                                    }//end for j
                                }//end for i
                            } // end else
                        }
                        //clean up after we are done here:
                        for (int tex = 0; tex < n_texture_classes; tex++)
                        {
                            sediment_in_transport_kg[row, col, tex] = 0;
                        }
                        young_SOM_in_transport_kg[row, col] = 0;
                        old_SOM_in_transport_kg[row, col] = 0;
                        //do administration:
                        if (maximum_allowed_deposition_m == 0)
                        {
                            slidestatus[row, col] = 4; //means: deposition was needed, but not allowed topographically
                        }
                        else
                        {
                            slidestatus[row, col] = 3; // means: deposition was needed, and happened
                        }
                        landslide_deposition_cells += 1;
                    } // end if situation C = deposition
                    if (situation_D == true)
                    {
                        slidestatus[row, col] = 5; // means: this is outside of landslide initiation, continuation, deposition area - beyond the runout distance. 
                        for (int tex = 0; tex < n_texture_classes; tex++)
                        {
                            if (sediment_in_transport_kg[row, col, tex] > 0)
                            {
                                //Debug.WriteLine(" there is sediment left but no place to put it. Now what? " + row + " " + col + " " + sediment_in_transport_kg[row, col, tex] + " kg");
                            }
                        }
                    }
                } //for runner
                //now update the DTM:
                for (row = 0; row < nr; row++)
                {
                    for (col = 0; col < nc; col++)
                    {
                        if (dtm[row, col] != nodata_value)
                        {
                            dtm[row, col] += ero_slid_m[row, col] + sed_slid_m[row, col];
                            dtmchange_m[row, col] += ero_slid_m[row, col] + sed_slid_m[row, col];
                            soildepth_m[row, col] = total_soil_thickness(row, col);
                        }
                    }
                }

                if (t > 498)
                {
                    out_double(workdir + "\\" + run_number + "_" + string.Format("{0:0000.}", t) + "_slide_erosion_m.asc", ero_slid_m);
                    out_double(workdir + "\\" + run_number + "_" + string.Format("{0:0000.}", t) + "_slide_deposit_m.asc", sed_slid_m);
                    out_short(workdir + "\\" + run_number + "_" + string.Format("{0:0000.}", t) + "_slide_status.asc", slidestatus);
                    out_double(workdir + "\\" + run_number + "_" + string.Format("{0:0000.}", t) + "_dtm_m.asc", dtm);
                    //out_double(workdir + "\\" + run_number + "_" + string.Format("{0:0000.}", t) + "_verticalsize_m.asc", remaining_vertical_size_m);
                    //out_sed_in_tran_kg(workdir + "\\" + run_number + "_" + string.Format("{0:0000.}", t) + "_sed_in_trans_kg.asc", sediment_in_transport_kg);
                }

                Debug.WriteLine(" initiated on " + landslide_initiation_cells + " cells (" + (100 * landslide_initiation_cells / number_of_data_cells) + "%)");
                Debug.WriteLine(" continued through " + landslide_continuation_cells + " cells(" + (100 * landslide_continuation_cells / number_of_data_cells) + "%)");
                Debug.WriteLine(" deposited on " + landslide_deposition_cells + " cells(" + (100 * landslide_deposition_cells / number_of_data_cells) + "%)");
                /*
                Debug.WriteLine(" initiation caused " + string.Format("{0:0.0000}", (initiation_volume_m3 / 1000000)) + " million m3 erosion");
                Debug.WriteLine(" continuation caused " + string.Format("{0:0.0000}", (continuation_volume_m3 / 1000000)) + " million m3 erosion");
                Debug.WriteLine(" deposition caused " + string.Format("{0:0.0000}", (deposition_volume_m3 / 1000000)) + " million m3 deposition");
                Debug.WriteLine(" material dropped into the river network: " + string.Format("{0:0.0000}", ((initiation_volume_m3 + continuation_volume_m3 - deposition_volume_m3) / 1000000)) + " million m3 ");

                Debug.WriteLine(" initiation caused " + string.Format("{0:0.0000}", (initiation_mass_kg / 1000000)) + " million kg erosion");
                Debug.WriteLine(" continuation caused " + string.Format("{0:0.0000}", (continuation_mass_kg / 1000000)) + " million kg erosion");
                Debug.WriteLine(" deposition caused " + string.Format("{0:0.0000}", (deposition_mass_kg / 1000000)) + " million kg deposition");
                Debug.WriteLine(" material disappeared into thin air: " + string.Format("{0:0.0000}", ((initiation_mass_kg + continuation_mass_kg - deposition_mass_kg) / 1000000)) + " million kg ");

                //Check overall bulk densities:
                if (deposition_volume_m3 > 0) { Debug.WriteLine(" density of material from initiation: " + string.Format("{0:0.0}", ((initiation_mass_kg / Convert.ToDecimal(initiation_volume_m3)))) + " kg/m3 "); }
                if (continuation_volume_m3 > 0) { Debug.WriteLine(" density of material from continuation: " + string.Format("{0:0.0}", ((continuation_mass_kg / Convert.ToDecimal(continuation_volume_m3)))) + " kg/m3 "); }
                if (deposition_volume_m3 > 0) {Debug.WriteLine(" density of material from deposition: " + string.Format("{0:0.0}", ((deposition_mass_kg / Convert.ToDecimal(deposition_volume_m3)))) + " kg/m3 "); }
                if (initiation_volume_m3 + continuation_volume_m3 - deposition_volume_m3 > 0) { Debug.WriteLine(" density of material lost into thin air: " + string.Format("{0:0.0}", (((initiation_mass_kg + continuation_mass_kg - deposition_mass_kg) / Convert.ToDecimal((initiation_volume_m3 + continuation_volume_m3 - deposition_volume_m3))))) + " kg/m3 ");   }
                */

                average_rainfall_intensity_m_d = sum_rainfall_intensity_m_d / number_of_data_cells;
                if (timeseries.timeseries_slide_checkbox.Checked)
                {
                    timeseries_matrix[t, timeseries_order[34]] = Convert.ToDouble(initiation_mass_kg);
                    timeseries_matrix[t, timeseries_order[35]] = Convert.ToDouble(continuation_mass_kg);
                    timeseries_matrix[t, timeseries_order[36]] = Convert.ToDouble(deposition_mass_kg);
                    timeseries_matrix[t, timeseries_order[37]] = Convert.ToDouble((initiation_mass_kg + continuation_mass_kg - deposition_mass_kg));
                    timeseries_matrix[t, timeseries_order[38]] = average_rainfall_intensity_m_d;
                }
                Debug.WriteLine(" finished landsliding process");
            } // try
            catch (Exception e) { }
        }

        private void calculate_tillage()
        {
            try
            {
                Decimal mass_before = total_catchment_mass_decimal();
                /*Task.Factory.StartNew(() =>
                {
                    this.InfoStatusPanel.Text = "tillage calculation";
                }, CancellationToken.None, TaskCreationOptions.None, guiThread); */
                int row, col, i, j;
                double slope_sum, dz_min, d_x, dz_max, dh, fraction, temptill, tempdep, slope;

                nb_ok = 0; nb_check = 0;
                for (row = 0; row < nr; row++)
                {
                    for (col = 0; col < nc; col++)
                    {
                        till_result[row, col] = 0;
                    }
                }

                int runner = 0;
                for (runner = number_of_data_cells - 1; runner >= 0; runner--)
                {           // the index is sorted from low to high values, but flow goes from high to low
                    row = row_index[runner]; col = col_index[runner];
                    //  Debug.WriteLine("till1");

                    if (tillfields[row, col] == 1)
                    {
                        if (check_negative_weight(row, col) == true) { MessageBox.Show("negative weight in t " + t + ", row " + row + ", col " + col + ", step 1"); }

                        // 1. Mixing of the topsoil. We use the code for upheaval for this
                        soil_bioturbation_upheaval(1, plough_depth);



                        // Debug.WriteLine("till5");
                        // 2. Calculate redistribution of material
                        // 2.a First calculate slope_sum for multiple flow, and remember how much lower the !currently! lowest lower neighbour is
                        slope_sum = 0; d_x = dx; dhtemp = large_negative_number; nb_ok = 1; dz_max = 0; dz_min = large_negative_number;
                        for (i = (-1); i <= 1; i++)
                        {
                            for (j = (-1); j <= 1; j++)
                            {
                                dh = 0; dhtemp = large_negative_number; d_x = dx;
                                if (((row + i) >= 0) && ((row + i) < nr) && ((col + j) >= 0) && ((col + j) < nc) && !((i == 0) && (j == 0)))
                                {    // boundaries
                                    if (dtm[row + i, col + j] != nodata_value)
                                    {
                                        dh = ((dtm[row, col] + till_result[row, col]) - (dtm[row + i, col + j] + till_result[row + i, col + j]));
                                        if ((row != row + i) && (col != col + j)) { d_x = dx * Math.Sqrt(2); } else { d_x = dx; }
                                        if (dh > 0)
                                        {           // i j is a lower neighbour
                                            if (dh > dz_max) { dz_max = dh; }
                                            dh = dh / d_x;
                                            // dh = Math.Pow(dh, conv_fac); MvdM tillage always with normal divergence, not accentuated by convergence factor
                                            slope_sum = slope_sum + dh;
                                        }//end if
                                    }//end if novalues
                                }// end if boundaries
                            }//end for
                        }//end for

                        // 2.b knowing slope_sum, we can now calculate which fraction of the tilled amount goes where, and how much that is. 
                        // knowing the lowest lower neighbour of row,col lets us limit the tillage-erosion to avoid row,col becoming lower 
                        // than its lowest lower neighbour (avoiding sinks).
                        // we are also going to limit the tilled amount to avoid row+i, col+j becoming higher than its own lowest higher nb.
                        // that avoids sinks as well.
                        // Debug.WriteLine("till6");
                        for (i = (-1); i <= 1; i++)
                        {
                            for (j = (-1); j <= 1; j++)
                            {
                                dh = 0; fraction = 0.0;
                                frac_dis = 0.0;
                                d_x = dx;
                                if (((row + i) >= 0) && ((row + i) < nr) && ((col + j) >= 0) && ((col + j) < nc) && !((i == 0) && (j == 0))) // boundaries
                                {
                                    if ((dtm)[row + i, col + j] != (nodata_value))
                                    {
                                        dh = ((dtm[row, col] + till_result[row, col]) - (dtm[row + i, col + j] + till_result[row + i, col + j]));

                                        if (col == 108 & t == 0)
                                        {
                                            // Debugger.Break();
                                        }
                                        if (dh > 0.000000) // i j is a lower neighbour to which we would like to till a certain amount.
                                        {
                                            // Calculate fraction of discharge into this cell
                                            if ((row != row + i) && (col != col + j)) { d_x = dx * Math.Sqrt(2); } else { d_x = dx; }
                                            slope = dh / d_x;
                                            dh = dh / d_x;
                                            // dh = Math.Pow(dh, conv_fac); MvdM tillage always with normal divergence, not accentuated by convergence factor
                                            fraction = (dh / slope_sum);
                                            // Tillage erosion calculation
                                            temptill = fraction * (tilc * slope * plough_depth) * dt;    // temptill is what we would like to till from r,c to r+i,c+j
                                                                                                         // Tillage erosion correction through calculating maximum tillage: tempdep
                                           if(temptill > 0.5)
                                            {
                                                // Debugger.Break();
                                            }
                                            tempdep = soildepth_m[row, col];
                                            //if there is more soil than the difference between the donor cell and its lowest lower nb, limit tillage to that difference.
                                            if (tempdep > dz_max) { tempdep = dz_max; }
                                            //if there is not enough space in the receiver cell because its currently lowest higher neighbour is not high enough, 
                                            //then limit tillage to that amount. First, calculate the current altitude difference with the lowest higher neighbour of r+i,c+j 
                                            dz_min = 9999;
                                            for (alpha = (-1); alpha <= 1; alpha++)
                                            {
                                                for (beta = (-1); beta <= 1; beta++)
                                                {
                                                    if (((row + i + alpha) >= 0) && ((row + i + alpha) < nr) && ((col + j + beta) >= 0) && ((col + j + beta) < nc) && !((alpha == 0) && (beta == 0))) // boundaries
                                                    {
                                                        if (dtm[row + i + alpha, col + j + beta] != nodata_value)
                                                        {
                                                            if ((dtm[row + i + alpha, col + j + beta] + till_result[row + i + alpha, col + j + beta]) > (dtm[row + i, col + j] + till_result[row + i, col + j]))
                                                            { // we are looking at a higher neighbour of the receiver cell
                                                                if (((dtm[row + i + alpha, col + j + beta] + till_result[row + i + alpha, col + j + beta]) - (dtm[row + i, col + j] + till_result[row + i, col + j])) < dz_min)
                                                                {
                                                                    dz_min = (dtm[row + i + alpha, col + j + beta] + till_result[row + i + alpha, col + j + beta]) - (dtm[row + i, col + j] + till_result[row + i, col + j]);
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                            // knowing the maximum tillage that the receiver cell can receive without blocking its own higher nbs, we limit the maximum tillage to that amount
                                            if (tempdep > dz_min) { tempdep = dz_min; }
                                            if (dz_min == 9999) { tempdep = 0.0; }     // if the receiver cell does not have higher nbs, we cannot till at all.
                                            if (tempdep < 0.0) { tempdep = 0.0; }
                                            if (tempdep > epsilon) { tempdep -= epsilon; }  // finally, always till just a bit less than the max allowed to prevent flat areas
                                            if (temptill > tempdep) { temptill = tempdep; } // if we want to till more than the maximum possible, we only till the maximum possible.

                                            till_result[row, col] -= temptill;
                                            till_result[row + i, col + j] += temptill;
                                           
                                            if (check_negative_weight(row, col) == true) { MessageBox.Show("negative weight in t " + t + ", row " + row + ", col " + col + ", step 2"); }

                                            //double dz_till_m = temptill;
                                            // Debug.WriteLine("till7");

                                            // 2.c update soil properties which are tilled
                                            // top layers are mixed, so it doesn't matter where eroded material comes from.
                                            // problems can arise when eroded depth is larger than plough depth. 
                                            // DEVELOP MvdM: development needed for layers with varying bulk density, in the case this occurs in an Ap horizon
                                            double mass_partial_layer, frac_eroded, total_mass_start, total_mass_end;

                                            //total_mass_start = total_soil_mass(row, col);
                                            int layero = 0;
                                            double temptill0 = temptill;

                                            while (layero < max_soil_layers & temptill >= layerthickness_m[row, col, layero]) // hele laag wordt verwijderd, al het materiaal naar de volgende cel
                                            {
                                                // These layers are completely eroded. All material is transported to the top layer in the next cell
                                                temptill -= layerthickness_m[row, col, layero];

                                                transfer_material_between_layers(row, col, layero, row + i, col + j, 0, 1);
                                                

                                                //layerthickness_m[row, col, layero] = 0;
                                                //layerthickness_m[row + i, col + j, 0] = thickness_calc(row + i, col + j, 0);

                                                layero++;
                                                // escape when last soil layer is reached
                                                if (layero == (max_soil_layers - 1)) ;
                                                {
                                                    break;
                                                }
                                            }
                                            // Debug.WriteLine("till8");

                                            // The remaining layer is partly eroded, based on the volume fraction, or is the last soil layer that gets completely eroded
                                            frac_eroded = temptill / layerthickness_m[row, col, layero];
                                            if (frac_eroded > 1) { frac_eroded = 1; }
                                            transfer_material_between_layers(row, col, layero, row + i, col + j, 0, frac_eroded);

                                            layerthickness_m[row, col, layero] = thickness_calc(row, col, layero);
                                            layerthickness_m[row + i, col + j, 0] = thickness_calc(row + i, col + j, 0);

                                            //if necessary, i.e. an entire layer removed, shift cells up
                                            if (layero > 0)
                                            {
                                                try { remove_empty_layers(row, col); update_all_layer_thicknesses(row, col); }
                                                catch { Debug.WriteLine("Error in removing empty layers after tillage"); }
                                            }

                                        }//end if
                                    }//end if novalues
                                }//end if borders
                            }//end for j
                        }//end for i
                    } //end if tillfields
                }   // end  for 
                    // Debug.WriteLine("till9");
                    // 3. Update elevation changes
                for (row = 0; row < nr; row++)
                {
                    for (col = 0; col < nc; col++)
                    {
                        if (dtm[row, col] != nodata_value)
                        {
                            double old_soil_thickness = soildepth_m[row, col];
                            update_all_layer_thicknesses(row, col);
                            double new_soil_thickness = total_soil_thickness(row, col);

                            dtm[row, col] += new_soil_thickness - old_soil_thickness;
                            dtmchange_m[row, col] += new_soil_thickness - old_soil_thickness;
                            sum_tillage[row, col] += new_soil_thickness - old_soil_thickness;
                            soildepth_m[row, col] = new_soil_thickness;
                            if (till_result[row, col] > 0) { total_sum_tillage += new_soil_thickness - old_soil_thickness; }
                        }
                    }
                }

                // Debug.WriteLine("\n--tillage overview--");
                // Debug.WriteLine(" tilled a total of " + total_sum_tillage * dx * dx / 1000 + " * 1000 m3");
                Decimal mass_after = total_catchment_mass_decimal();
                if (Math.Abs(mass_before - mass_after) > Convert.ToDecimal(0.1))
                {
                    Debug.WriteLine("err_ti3");
                }

            }
            catch
            {
                Debug.WriteLine("err_ti4");
            }
        }
        void display_grain_distribution(int row2, int col2)
        {
            Debug.Write("Grains row " + row2 + ", col " + col2 + ", t" + t + ": ");
            for (int lay2 = 0; lay2 < max_soil_layers; lay2++)
            {
                Debug.Write(OSL_grainages[row2, col2, lay2].Length + ", ");
            }
            Debug.Write("\r\n");
        }

        private void calculate_creep()
        {
            //Debug.WriteLine("start of creep with diffusivity at " + potential_creep_kg_m2_y);
            try
            {
                if (NA_in_map(dtm) > 0 | NA_in_map(soildepth_m) > 0)
                {
                    Debug.WriteLine("err_cr1");
                    Debugger.Break();
                }
                Task.Factory.StartNew(() =>
                {
                    this.InfoStatusPanel.Text = "creep calculation";
                }, CancellationToken.None, TaskCreationOptions.None, guiThread);
                int row, col,
                            i, j,
                            nb_ok,
                            NA_dem;
                double
                            dhmin, dhe_tol, dhs_tol,
                            slope_sum, dhmax, dz_min, d_x, dz_max, dh1, dh,
                            fraction,
                            temp, tempcreep_kg, tempdep,
                            slope,
                            local_creep_kg = 0;

                nb_ok = 0; nb_check = 0; all_grids = 0;
                dhmin = large_negative_number; dhe_tol = 0.00000; dhs_tol = 0.00000;

                //NA_dem = NA_in_DEM();
                //if (NA_dem != NA_in_DEM()) { Debugger.Break(); }

                int runner = 0;

                for (runner = number_of_data_cells - 1; runner >= 0; runner--)
                {           // the index is sorted from low to high values, but flow goes from high to low
                    row = row_index[runner]; col = col_index[runner];
                    if (dtm[row, col] != nodata_value)
                    {
                        //Debug.WriteLine("cr1");
                        // into loop for surrounding grids of certain grid
                        // Start first the slope_sum loop for all lower neighbour grids
                        slope_sum = 0; dhmax = 0; dz_min = large_negative_number; d_x = dx;
                        dz_max = -1; dhtemp = large_negative_number; dhmin = (large_negative_number);
                        // if(row == 31 && col == 12) { Debug.WriteLine("creep1"); displaysoil(row, col); }
                        if (thickness_calc(row, col, 0) < 0)
                        {
                            displaysoil(row, col); Debug.WriteLine("err_cr2");
                        }

                        for (i = (-1); i <= 1; i++)
                        {
                            for (j = (-1); j <= 1; j++)
                            {
                                dh = 000000; dh1 = 000; dhtemp = large_negative_number; d_x = dx;
                                if (((row + i) >= 0) && ((row + i) < nr) && ((col + j) >= 0) && ((col + j) < nc) && !((i == 0) && (j == 0)))
                                {    // boundaries
                                    if (dtm[row + i, col + j] != nodata_value);
                                    {
                                        dh = ((dtm)[row, col] - (dtm)[row + i, col + j]);
                                        if ((row != row + i) && (col != col + j)) { d_x = dx * Math.Sqrt(2); } else { d_x = dx; }
                                        if (dh < 000000)
                                        {           // i j is a higher neighbour
                                            if (dh > dz_min) { dz_min = dh; }
                                            if (dh1 > dhmin + dhs_tol) { dhmin = (dh1 + dhs_tol); }
                                        }
                                        if (dh > 000000)
                                        {           // i j is a lower neighbour
                                            if (dh > dhmax - dhe_tol) { dhmax = (dh - dhe_tol); }
                                            dh = dh / d_x;
                                            if (dh > dz_max) { dz_max = dh; }
                                            dh = Math.Pow(dh, conv_fac);
                                            slope_sum = slope_sum + dh;
                                        }//end if
                                    }//end if novalues
                                }// end if boundaries
                            }//end for j
                        }//end for i
                         //if (NA_dem != NA_in_DEM()) { Debugger.Break(); }
                        if (thickness_calc(row, col, 0) < 0)
                        {
                            displaysoil(row, col); Debug.WriteLine("err_cr3");
                        }
                        if (dhmax < 0)
                        {
                            Debug.WriteLine("err_cr4");
                        }
                        //Debug.WriteLine("cr2"); 

                        // calculate potential creep in kg
                        double maxslope = Math.Atan(dz_max); // max slope in radians

                        if (daily_water.Checked)
                        {
                            if (aridity_vegetation[row, col] < 1) { potential_creep_kg_m2_y = 4 + 0.3; } // grassland
                            else { potential_creep_kg_m2_y = 4 + 1.3; } // forest
                                                                        // standard potential creep of 4 kg. 0.3 or 1.3 is added, based on vegetation type. Rates are derived from Wilkinson 2009: breaking ground and Gabet
                        }

                        // local_creep_kg = potential_creep_kg * Math.Sin(maxslope) * Math.Cos(maxslope) * dx * dx * dt; //Equation from gabet et al., 2003 https://doi.org/10.1146/annurev.earth.31.100901.141314 

                        double total_soil_thickness_m = 0;
                        for (int layer = 0; layer < max_soil_layers; layer++)
                        {
                            total_soil_thickness_m += layerthickness_m[row, col, layer];
                        }
                        local_creep_kg = potential_creep_kg_m2_y * (1 - Math.Exp(-bioturbation_decay_depth_m * total_soil_thickness_m)) * dx * dx * dt;
                        //Debug.WriteLine("cr3");

                        if (local_creep_kg > 0)
                        {

                            if (dhmin == large_negative_number) { dhmin = 0; } else { dhmin = -dhmin; }
                            if (dhmax <= 0.0) { dhmax = 0.0; } else { dhmax = -dhmax; }
                            for (i = (-1); i <= 1; i++)
                            {
                                for (j = (-1); j <= 1; j++)
                                {
                                    dh = 0.000000; fraction = 0.0;
                                    frac_dis = 0.0;
                                    d_x = dx;
                                    //if (col == 1 | col == (nc - 1))
                                    //{ Debugger.Break(); }
                                    if (((row + i) >= 0) && ((row + i) < nr) && ((col + j) >= 0) && ((col + j) < nc) && !((i == 0) && (j == 0))) // boundaries
                                    {
                                        //if (NA_dem != NA_in_DEM()) { Debugger.Break(); }
                                        if (thickness_calc(row, col, 0) < 0)
                                        {
                                            displaysoil(row, col); Debug.WriteLine("err_cr5");
                                        }

                                        if (dtm[row + i, col + j] != nodata_value)
                                        {
                                            dh = (dtm[row, col] - dtm[row + i, col + j]);
                                            temp = dtm[row + i, col + j];
                                            // if (NA_dem != NA_in_DEM()) { Debugger.Break(); }
                                            // Multiple Flow: If there are lower neighbours start evaluating
                                            if (dh > 0.000000)
                                            {
                                                // if (row == 31 && col == 12) { Debug.WriteLine("creep3"); displaysoil(row, col); }
                                                //Debug.WriteLine("Cr1, dtm = {0}", dtm[row, col]);
                                                // fraction of discharge into a neighbour grid
                                                if ((row != row + i) && (col != col + j)) { d_x = dx * Math.Sqrt(2); } else { d_x = dx; }
                                                slope = dh / d_x;
                                                dh = dh / d_x;
                                                dh = Math.Pow(dh, conv_fac);
                                                fraction = (dh / slope_sum);
                                                tempcreep_kg = fraction * local_creep_kg * slope * 100; //MM develop. Original function was fraction*slope*diffusivity. Do I need to add slope in calculations?

                                                //// oldsoildepths
                                                double dsoil_source = total_soil_thickness(row, col);
                                                double dsoil_sink = total_soil_thickness(row + i, col + j);
                                                //displaysoil(row + i, col + j);
                                                decimal oldmass = total_soil_mass_kg_decimal(row, col) + total_soil_mass_kg_decimal(row + i, col + j);

                                                decimal oldmass_source = total_soil_mass_kg_decimal(row, col);
                                                decimal oldmass_sink = total_soil_mass_kg_decimal(row + i, col + j);

                                                calc_creep_layers(row, col, i, j, tempcreep_kg);

                                                // update soil depths                                               
                                                update_all_layer_thicknesses(row, col);
                                                update_all_layer_thicknesses(row + i, col + j);

                                                //if (NA_dem != NA_in_DEM()) { Debugger.Break(); }
                                                if (thickness_calc(row, col, 0) < 0)
                                                {
                                                    displaysoil(row, col);
                                                    Debug.WriteLine("err_cr6");

                                                    Debug.WriteLine(thickness_calc(row, col, 0));
                                                    ;
                                                }

                                                double dsoil_source_new = total_soil_thickness(row, col);
                                                double dsoil_sink_new = total_soil_thickness(row + i, col + j);

                                                //displaysoil(row + i, col + j);

                                                double dz_source = dsoil_source_new - dsoil_source; // change in soil depth
                                                double dz_sink = dsoil_sink_new - dsoil_sink; // change in soil depth
                                                                                              //at this stage, we should connect to any blocks that are in the source and sink cells,
                                                                                              //and add dz_source and sink to their accum creep counters
                                                                                              //first, see if there are any blocks in here
                                                if (blocks_active == 1)
                                                {
                                                    // need to precalculate total block surface area per cell in a function (not from here) 
                                                    // to allow correction for more blocks having more stopping power and higher buildup behind them
                                                    foreach (var Block in Blocklist)
                                                    {
                                                        int blrow = Convert.ToInt32(Math.Floor(Block.Y_row));
                                                        int blcol = Convert.ToInt32(Math.Floor(Block.X_col));
                                                        if (row == blrow && col == blcol)
                                                        {
                                                            //then we have blocks that are giving in this direction, so should subtract from accum_creep
                                                            //i, j will tell us where this creep went to, so which of our 8 values we shoudl subrtact from
                                                            switch (10 * j + i)
                                                            {
                                                                case -1:  //To N
                                                                    Block.Accumulated_creep_m_0 += Convert.ToSingle(dz_source); break;
                                                                case 9: // To NE
                                                                    Block.Accumulated_creep_m_1 += Convert.ToSingle(dz_source); break;
                                                                case 10: //To E
                                                                    Block.Accumulated_creep_m_2 += Convert.ToSingle(dz_source); break;
                                                                case 11: //To SE
                                                                    Block.Accumulated_creep_m_3 += Convert.ToSingle(dz_source); break;
                                                                case 1: //To S
                                                                    Block.Accumulated_creep_m_4 += Convert.ToSingle(dz_source); break;
                                                                case -9: //To SW
                                                                    Block.Accumulated_creep_m_5 += Convert.ToSingle(dz_source); break;
                                                                case -10: //To W
                                                                    Block.Accumulated_creep_m_6 += Convert.ToSingle(dz_source); break;
                                                                case -11: //To NW
                                                                    Block.Accumulated_creep_m_7 += Convert.ToSingle(dz_source); break;
                                                                default:
                                                                    break;
                                                            }
                                                        }
                                                        if ((row + i) == blrow && (col + j) == blcol)
                                                        {
                                                            //then we have blocks that are receiving FROM this direction, so should add to accum_creep
                                                            //note the changed SWITCH VALUES
                                                            switch (10 * j + i)
                                                            {
                                                                case -1:  //From N, To S, etc
                                                                    Block.Accumulated_creep_m_4 += Convert.ToSingle(dz_sink); break;
                                                                case 9: // From NE
                                                                    Block.Accumulated_creep_m_5 += Convert.ToSingle(dz_sink); break;
                                                                case 10: //From E
                                                                    Block.Accumulated_creep_m_6 += Convert.ToSingle(dz_sink); break;
                                                                case 11: //From SE
                                                                    Block.Accumulated_creep_m_7 += Convert.ToSingle(dz_sink); break;
                                                                case 1: //From S
                                                                    Block.Accumulated_creep_m_0 += Convert.ToSingle(dz_sink); break;
                                                                case -9: //From SW
                                                                    Block.Accumulated_creep_m_1 += Convert.ToSingle(dz_sink); break;
                                                                case -10: //From W
                                                                    Block.Accumulated_creep_m_2 += Convert.ToSingle(dz_sink); break;
                                                                case -11: //From NW
                                                                    Block.Accumulated_creep_m_3 += Convert.ToSingle(dz_sink); break;
                                                                default:
                                                                    break;
                                                            }
                                                        }
                                                    }
                                                }

                                                decimal newmass = total_soil_mass_kg_decimal(row, col) + total_soil_mass_kg_decimal(row + i, col + j);
                                                creep[row, col] += dz_source;
                                                creep[row + i, col + j] += dz_sink;

                                                soildepth_m[row, col] += dz_source;
                                                soildepth_m[row + i, col + j] += dz_sink;
                                                // if (soildepth_m[row, col] > 5) { Debugger.Break(); }
                                                if (soildepth_m[row, col] < 0) { soildepth_m[row, col] = 0; }
                                                if (soildepth_m[row + i, col + j] < 0) { soildepth_m[row + i, col + j] = 0; }

                                                dtm[row, col] += dz_source;
                                                dtm[row + i, col + j] += dz_sink;

                                                dtmchange_m[row, col] += dz_source; //MMS
                                                dtmchange_m[row + i, col + j] += dz_sink; //MMS

                                                //Debug.WriteLine("Cr5, dtm = {0}", dtm[row, col]);
                                                //displaysoil(row, col);

                                                if (Math.Abs(oldmass - newmass) > Convert.ToDecimal(0.00001))
                                                {
                                                    Debug.WriteLine("err_cr7");

                                                } //MM Qua hoogte lijkt het hieronder nog mis te gaan. De gewichtscheck hierboven gaat wel goed. 
                                                  // Op DTM zijn de verschillen niet te zien, op creep[,] wel
                                                if (Math.Abs(dz_source + dz_sink) > 0.01 & t > 1)
                                                {
                                                    // Can occur with a thick lower soil layer, due to small changes in depth->bulk density->thickness. Can be 4 cm for a total soil thickness of 100  m.
                                                    Debug.WriteLine("Creep: Thickness erosion and deposition are not approximately equal");
                                                    //displaysoil(row, col); 
                                                    //displaysoil(row + i, col + j); 
                                                    // Debugger.Break();
                                                }
                                                //if (NA_dem != NA_in_DEM()) { Debugger.Break(); }
                                                if (thickness_calc(row, col, 0) < 0)
                                                {
                                                    displaysoil(row, col); Debug.WriteLine("err_cr8");
                                                }

                                                // if (row == 31 && col == 12) { Debug.WriteLine("creep7"); displaysoil(row, col); }

                                            }//end if
                                        }//end if novalues
                                    }//end if borders
                                }//end for j
                            }//end for i
                        } // end potential_creep_kg>0
                    }       // end for sorted 
                } // end runner
                if (NA_in_map(dtm) > 0 | NA_in_map(soildepth_m) > 0)
                {
                    Debug.WriteLine("err_cr9");
                }

            }
            catch
            {
                Debug.WriteLine("err_cr10");

            }
            //Debug.WriteLine("end of creep");
        }

        private void calc_creep_layers(int row1, int col1, int iiii, int jjjj, double mass_export_soil_kg)
        {
            // tempcreep_kg in kg
            try
            {
                //Debug.WriteLine("Cr3.1, dtm = {0}", dtm[row1, col1]);
                //displaysoil(row1, col1);
                int layerreceiver = 0;
                double creep_decay_depth_m = Convert.ToDouble(bt_depth_decay_textbox.Text);

                double frac_overlap_lay, upperdepthdonor = 0, lowerdepthdonor = 0, upperdepthreceiver = 0, lowerdepthreceiver = 0, dsoil = 0, upp_z_lay = 0, int_curve_total, int_curve_lay, mass_export_lay_kg;
                bool C_done = false, lastlayer = false;

                dsoil = total_soil_thickness(row1, col1);

                //expanding soil thickness to if blocks are active to account of openness of (possibly) underlying hardlayer:
                if (blocks_active == 1)
                {
                    //this condition may be too strict or too lax 
                    if ((dtm[row1, col1] - dsoil) <= (hardlayerelevation_m + 0.001) && (dtm[row1, col1] - dsoil) > hardlayerelevation_m - 0.001)
                    {
                        //Debug.WriteLine(" r " + row1 + " c " + col1 + "  increasing total creeping soildepth from " + dsoil + " to " + (dtm[row1, col1] - (hardlayerelevation_m - (hardlayerthickness_m * hardlayeropenness_fraction[row1, col1]))));
                        dsoil = Math.Max(dsoil, (dtm[row1, col1] - (hardlayerelevation_m - (hardlayerthickness_m * hardlayeropenness_fraction[row1, col1]))));
                        //Debug.WriteLine(" soildepth now " + dsoil );
                    }
                }

                //int_curve_total = 1 / (-creep_depth_decay_constant) * Math.Exp(-creep_depth_decay_constant * 0) - 1 / (-creep_depth_decay_constant) * Math.Exp(-creep_depth_decay_constant * dsoil); // integral over depth decay function, from depth 0 to total soil depth
                upperdepthdonor = 0; //  dtm[row1, col1]; using 0 leads to a continuous landscapes, instead of a step-wise pattern
                upperdepthreceiver = 0; // dtm[row1 + iiii, col1 + jjjj];
                lowerdepthreceiver = upperdepthreceiver - layerthickness_m[row1 + iiii, col1 + jjjj, layerreceiver];

                for (int lay = 0; lay < max_soil_layers; lay++) // loop over receiving layers
                {

                    double laythick_m = layerthickness_m[row1, col1, lay];
                    if (laythick_m > 0)
                    {
                        if (blocks_active == 1)
                        {
                            if ((dtm[row1, col1] - (upp_z_lay + laythick_m)) <= (hardlayerelevation_m + 0.001) && (dtm[row1, col1] - (upp_z_lay + laythick_m)) > hardlayerelevation_m - 0.001)
                            {
                                //even though we change layer thickness here, we are not adapting how much mass is in it. 
                                //also for layers that currently end slightly IN hardlayer (for reasons unknown), and openness really small, we may actually end up reducing their thickness
                                //and if those layers are already very thin, we may end up with negative layerthicknes..
                                if (((dtm[row1, col1] - upp_z_lay) - (hardlayerelevation_m - (hardlayerthickness_m * hardlayeropenness_fraction[row1, col1])) - laythick_m) < -0.001)
                                {
                                    Debug.WriteLine(" last layer thick_m " + laythick_m + " will be adding " + ((dtm[row1, col1] - upp_z_lay) - (hardlayerelevation_m - (hardlayerthickness_m * hardlayeropenness_fraction[row1, col1])) - laythick_m));
                                }
                                laythick_m = (dtm[row1, col1] - upp_z_lay) - (hardlayerelevation_m - (hardlayerthickness_m * hardlayeropenness_fraction[row1, col1]));
                                //Debug.WriteLine("adapted thickness of last layer to " + laythick_m );
                                if (laythick_m < 0)
                                {
                                    Debug.WriteLine(" yikes " + laythick_m + " laythick - less than zero ");
                                    Debug.WriteLine("adapted thickness of last layer to " + laythick_m);
                                    laythick_m = 0;
                                }
                            }
                        }
                        // int_curve_lay = 1 / (-creep_depth_decay_constant) * Math.Exp(-creep_depth_decay_constant * upp_z_lay) - 1 / (-creep_depth_decay_constant) * Math.Exp(-creep_depth_decay_constant * (upp_z_lay + laythick_m));//integral over depth decay function, from top of layer to bottom of layer                        
                        //lowerdepthdonor = upperdepthdonor - laythick_m; // elevation range donor layer
                        double curve_fraction = activity_fraction(creep_decay_depth_m, dsoil, upp_z_lay, upp_z_lay + laythick_m);
                        mass_export_lay_kg = mass_export_soil_kg * (curve_fraction); // mass to be removed from layer in kg 
                        upp_z_lay += laythick_m;

                        if (mass_export_lay_kg < 0)
                        {
                            Debug.Write(" YIKES" + mass_export_lay_kg + " will be exported ");
                            mass_export_lay_kg = 0;
                        }
                        string exchangetype = "N"; //this is the sentinel value, meaning "None of the options below"
                        frac_overlap_lay = 0; // this fraction will be used to correct for partally overlapping layers 

                        //five options: 
                        //              A donor layer is located completely above receiving layer, exchange with air above receiving layer 0,
                        //              B donor layer partially sticks above upper receiving layer, exchange with air above receiving layer 0,
                        // option A and B will not be used, as we consider the transitio between cells as a continuous curve, i.e. not step-wise pattern
                        // -----------------------------------------------------------
                        //              C (partial) overlap with receiving layer higher than donor layer, 
                        //              D receiving layer fully overlapped by (thicker) donor layer,
                        //              E (partial) overlap with receiving layer lower than donor layer,
                        //              F donor layer fully overlapped by (thicker) receiving layer,
                        //              (G exact overlap (which is like B or E, therefore not explicitly modeled)

                        // Options A and B are about donor layers above the surface of the receiving cell.
                        // Options C-F are about subsurface overlaps of layers, working from higher to lower receiving layers.
                        // This enables update of the receiving layer, when the overlap no longer exists
                        // Exchange of mass is done immediately. After this loop layer thicknesses and DTM are updated for the next calculation

                        // OPTION A: donor layer lies completely above receiving layer
                        // Not possible with the curret setup, where upper depth of donor and receiver both are zero, as is the case in continuous landscapes
                        if (lowerdepthdonor >= upperdepthreceiver && layerreceiver == 0)
                        {
                            exchangetype = "A";
                            frac_overlap_lay = 1;
                            creep_transport(row1, col1, lay, row1 + iiii, col1 + jjjj, layerreceiver, mass_export_lay_kg, frac_overlap_lay, exchangetype);
                            // if(row1==0&&col1==0){ Debug.WriteLine("A, layer " +lay+": " + frac_dz_lay * frac_overlap_lay); }
                            // no need to update receiving layer number
                        }

                        // OPTION B. donor layer partly rises above surface source layer. exchange with air above receiving layer 0
                        // Not possible with the curret setup, where upper depth of donor and receiver both are zero, as is the case in continuous landscapes

                        if (upperdepthdonor > upperdepthreceiver && lowerdepthdonor < upperdepthreceiver && layerreceiver == 0)
                        {
                            exchangetype = "B";
                            frac_overlap_lay = (upperdepthdonor - upperdepthreceiver) / layerthickness_m[row1, col1, lay];
                            creep_transport(row1, col1, lay, row1 + iiii, col1 + jjjj, layerreceiver, mass_export_lay_kg, frac_overlap_lay, exchangetype);
                            // no need to update receiving layer number, because we only look at air exchange. subsurface exchange will be treated later
                            // if (row1 == 0 && col1 == 0) { Debug.WriteLine("B, layer " + lay + ": " + frac_dz_lay * frac_overlap_lay); }

                        }

                        // OPTION C: (partial or complete) overlap with receiving layer located (partially) higher than donor layer
                        if (upperdepthdonor <= upperdepthreceiver && lowerdepthdonor <= lowerdepthreceiver && upperdepthdonor > lowerdepthreceiver)
                        {
                            exchangetype = "C";
                            frac_overlap_lay = (upperdepthdonor - lowerdepthreceiver) / layerthickness_m[row1, col1, lay];
                            if (frac_overlap_lay > 1 && frac_overlap_lay < 1.00001)
                            {
                                //for some reason, the calculation results sometimes in frac_overlap_lay values that are extremely near 1, but just a bit higher
                                frac_overlap_lay = 1;
                            }
                            creep_transport(row1, col1, lay, row1 + iiii, col1 + jjjj, layerreceiver, mass_export_lay_kg, frac_overlap_lay, exchangetype);

                            C_done = true;

                            if (lowerdepthdonor <= lowerdepthreceiver && layerreceiver < (max_soil_layers - 1)) // update receiving layer to a lower one. only occurs when lowerdepthdonor == lowerdepthreceiver
                            {
                                layerreceiver += 1;
                                upperdepthreceiver = lowerdepthreceiver;
                                lowerdepthreceiver -= layerthickness_m[row1 + iiii, col1 + jjjj, layerreceiver];
                            }
                            // if (row1 == 0 && col1 == 0) { Debug.WriteLine("C, layer " + lay + ": " + frac_dz_lay * frac_overlap_lay); }

                        }

                        // OPTION D: receiving layer completely overlapped by (thicker) donor layer
                        while (upperdepthdonor > upperdepthreceiver && lowerdepthdonor < lowerdepthreceiver && lastlayer == false) // while loop, this can occur several times, when the donor layer completely overlaps receiving layers
                        {
                            exchangetype = "D";
                            frac_overlap_lay = (upperdepthreceiver - lowerdepthreceiver) / layerthickness_m[row1, col1, lay];
                            creep_transport(row1, col1, lay, row1 + iiii, col1 + jjjj, layerreceiver, mass_export_lay_kg, frac_overlap_lay, exchangetype);
                            // update receiving layer. the next layer can also be overlapped completely by donor layer
                            if (layerreceiver < (max_soil_layers - 1))
                            {
                                layerreceiver += 1;
                                upperdepthreceiver = lowerdepthreceiver;
                                lowerdepthreceiver -= layerthickness_m[row1 + iiii, col1 + jjjj, layerreceiver];
                            }
                            else
                            {
                                lastlayer = true;
                            }
                            // if (row1 == 0 && col1 == 0) { Debug.WriteLine("D, layer " + lay + ": " + frac_dz_lay * frac_overlap_lay); }

                        }
                        //OPTION E: overlap with receiving layer lower than donor layer  (take care that this does not evaluate to TRUE when C is also TRUE)
                        if (upperdepthdonor >= upperdepthreceiver && lowerdepthdonor >= lowerdepthreceiver && lowerdepthdonor < upperdepthreceiver && C_done == false)
                        {
                            exchangetype = "E";
                            frac_overlap_lay = (upperdepthreceiver - lowerdepthdonor) / layerthickness_m[row1, col1, lay];
                            creep_transport(row1, col1, lay, row1 + iiii, col1 + jjjj, layerreceiver, mass_export_lay_kg, frac_overlap_lay, exchangetype);

                            if (lowerdepthdonor <= lowerdepthreceiver && layerreceiver < (max_soil_layers - 1)) // update receiving layer to a lower one
                            {
                                layerreceiver += 1;
                                upperdepthreceiver = lowerdepthreceiver;
                                lowerdepthreceiver -= layerthickness_m[row1 + iiii, col1 + jjjj, layerreceiver];
                            }
                            //if (row1 == 0 && col1 == 0)
                            //{
                            //    Debug.WriteLine("E, layer " + lay + ": " + frac_dz_lay * frac_overlap_lay);
                            //    Debug.WriteLine("Dupper = {0}, Dlower = {1}, Rupper = {2}, Rlower = {3}", upperdepthdonor, lowerdepthdonor, upperdepthreceiver, lowerdepthreceiver);
                            //}
                        }

                        //OPTION F, donor layer completely overlapped by (thicker) receiver layer
                        if (upperdepthdonor < upperdepthreceiver && lowerdepthdonor > lowerdepthreceiver)
                        {
                            exchangetype = "F";
                            frac_overlap_lay = 1;
                            creep_transport(row1, col1, lay, row1 + iiii, col1 + jjjj, layerreceiver, mass_export_lay_kg, frac_overlap_lay, exchangetype);
                            // no update of receiving layer required
                            // if (row1 == 0 && col1 == 0) { Debug.WriteLine("F, layer " + lay + ": " + frac_dz_lay * frac_overlap_lay); }
                        }

                        //OPTION G, receiver soil might be absent. Material moves to upper layer of receiving layer, if elevation allows
                        if (total_soil_thickness(row1 + iiii, col1 + jjjj) == 0) // if receiving cell is bare rock 
                        {
                            exchangetype = "G";
                            bool partial_overlap = true;
                            if ((dtm[row1, col1] + upperdepthdonor) < dtm[row1 + iiii, col1 + jjjj]) { frac_overlap_lay = 0; partial_overlap = false; } // donor layer lies completely below surface of receiving cell
                            if ((dtm[row1, col1] + lowerdepthdonor) > dtm[row1 + iiii, col1 + jjjj]) { frac_overlap_lay = 1; partial_overlap = false; } // donor layer lies completely above surface of receiving cell

                            if (partial_overlap == true)
                            {
                                frac_overlap_lay = ((dtm[row1, col1] + upperdepthdonor) - dtm[row1 + iiii, col1 + jjjj]) / (upperdepthdonor - lowerdepthdonor);
                            } // donor layer lies partially above surface of receiving cell
                            if (frac_overlap_lay > 1) { frac_overlap_lay = 1; } // fraction can be a bit higher due to rounding errors

                            if (Double.IsInfinity(frac_overlap_lay) | Double.IsNaN(frac_overlap_lay) | frac_overlap_lay > 1)
                            {
                                Debug.WriteLine("err_cr11");
                            } // something went wrong in calculating overlapping fraction. Either divided by zero, a non-real answer or a fraction larger than 1

                            if (frac_overlap_lay > 0) // If the donor layer is (partially) above the bare bedrock of the receiving cell, everything can move to next cell:
                            {
                                frac_overlap_lay = 1;
                                creep_transport(row1, col1, lay, row1 + iiii, col1 + jjjj, 0, mass_export_lay_kg, frac_overlap_lay, exchangetype);
                            }
                        }

                        upperdepthdonor = lowerdepthdonor;
                        C_done = false;
                    } // end layerthickness > 0
                } // end layers
            } // end of try
            catch
            {
                Debug.WriteLine("Error in time {0}, row {1}, col{2}, receiving row {4}, col {5}", t, row1, col1, row1 + iiii, col1 + jjjj);
                Debug.WriteLine("err_cr12");

            }
        } // end calc_creep_layers

        private void creep_transport(int fromrow, int fromcol, int fromlay, int torow, int tocol, int tolay, double mass_export, double fraction_overlap, string exchangetype)
        {
            double CN_before = 0, CN_after = 0;
            //if (CN_checkbox.Checked) { CN_before = total_CNs(); }

            try
            {
                //Debug.WriteLine("Cr3.1.1, dtm = {0}", dtm[fromrow, fromcol]);
                //displaysoil(fromrow, fromcol);

                // double fraction_transport = fraction_dz * fraction_overlap;
                double fraction_transport = mass_export / total_layer_mass_kg(fromrow, fromcol, fromlay); // fraction of mass to be exported
                if (fraction_transport > 1) { fraction_transport = 1; Debug.WriteLine("type " + exchangetype + " err_cr13a - tried to transport more material via creep than present in donor layer"); }
                if (fraction_transport < 0)
                {
                    fraction_transport = 0; Debug.WriteLine("type " + exchangetype + " err_cr13b - tried to transport negative amount of material - transporting none now");
                }
                if (fraction_overlap > 1)
                {
                    Debug.WriteLine("fraction overlap = " + fraction_overlap.ToString("F40"));
                    fraction_overlap = 1;
                    Debug.WriteLine("type " + exchangetype + " err_cr13c - tried to transport more material via creep than present in donor layer");
                }

                transfer_material_between_layers(fromrow, fromcol, fromlay, torow, tocol, tolay, fraction_transport);
                //Debug.WriteLine("Cr3.1.2, dtm = {0}", dtm[fromrow, fromcol]);
                //displaysoil(fromrow, fromcol);
                //if (CN_checkbox.Checked) { 
                //    CN_after = total_CNs(); 
                //    if (((CN_before - CN_after) / (CN_after+1)) > 1E-6) { Debugger.Break(); } }

            }
            catch
            {
                Debug.WriteLine("crashed during creep transport calculations");
                Debug.WriteLine("type " + exchangetype + " err_cr14");

            }

        }

        private void calculate_tree_fall()
        {
            Decimal tf_mass_before = total_catchment_mass_decimal();
            double CN_before = 0, CN_after = 0;
            //if (CN_checkbox.Checked) { CN_before = total_CNs(); }

            try
            {
                Task.Factory.StartNew(() =>
                {
                    this.InfoStatusPanel.Text = "tree fall calculation";
                }, CancellationToken.None, TaskCreationOptions.None, guiThread);
                bool fallen = false;
                int i_tf = 0, j_tf = 0;
                // double mass_before_tf = total_catchment_mass_decimal();
                double exported_mass_tf = 0, old_soil_depth_m = 0, tree_fall_frac_sum, tf_frac_dx;

                double[] tree_fall_mass, tree_fall_om;
                double[,] tree_fall_frac;
                Random rand = new Random(t); // t as a random seed
                Random falldirection = new Random(t);
                Random age_of_trees = new Random(t);
                int P_fall = Convert.ToInt32(Math.Round((1 / tf_frequency) / (dx * dx)));
                // int P_fall = Convert.ToInt32(Math.Round(1730 / dx / dx)); // 1/P_fall is the chance of tree fall, per m2, that's why we correct for cell size 
                // Debug.WriteLine("elevation of row 57 and col 40 at t {0} is {1}", t, dtm[57, 40]);
                int rowsource = 0, colsource = 0, rowsink = 0, colsink = 0;
                for (row = 0; row < nr; row++)
                {
                    for (col = 0; col < nc; col++)
                    {
                        if (dtm[row, col] != nodata_value & aridity_vegetation[row, col] > 1) // if cell exists, and if there is no grass growing
                        {
                            int chance = rand.Next(0, P_fall);
                            // Debug.WriteLine("tf2");
                            //if (row == 31 & col == 31 & t == 226) { displaysoil(31, 31); Debugger.Break(); }
                            if (chance == 1) // if a tree falls
                            {
                                rowsource = row;
                                colsource = col;
                                fallen = true;
                                treefall_count[row, col] += 1;
                                // if (row == 0 & col == 5) { Debug.WriteLine("tf on t {0}", t); }
                                int falldir = falldirection.Next(1, 9); // for now a random fall direction. This can be changed as a function of e.g. slope, aspect and dominant wind direction
                                                                        // It appears that these factors don't have a dominant effect:https://doi.org/10.3159/10-RA-011.1 
                                                                        // trees can now fall in 8 directions, to all neighbouring cells. Depending on the distance to these cells, sediments will be redistributed.
                                                                        // neighbours:
                                                                        // 1  2  3
                                                                        // 4  X  5
                                                                        // 6  7  8
                                                                        // DEVELOP change surface roughness as function of tree fall, to promote more infiltration?
                                                                        //determine row direction i_tf
                                if (falldir < 4) { i_tf = -1; }
                                else if (falldir < 6) { i_tf = 0; }
                                else if (falldir < 9) { i_tf = 1; }
                                else { MessageBox.Show("error in tree fall. Fall direction Y not known"); }
                                // Debug.WriteLine("tf3");

                                // determine col direction j_tf
                                if (falldir == 1 | falldir == 4 | falldir == 6) { j_tf = -1; }
                                else if (falldir == 2 | falldir == 7) { j_tf = 0; }
                                else if (falldir == 3 | falldir == 5 | falldir == 8) { j_tf = 1; }
                                else { MessageBox.Show("error in tree fall. Fall direction X not known"); }

                                d_x = dx; if (falldir == 1 | falldir == 3 | falldir == 6 | falldir == 8) { d_x = dx * Math.Sqrt(2); } // determine lateral fall distance

                                if ((row + i_tf) >= 0 & (row + i_tf) < nr & (col + j_tf) >= 0 & (col + j_tf) < nc)
                                {
                                    if (dtm[row + i_tf, col + j_tf] != nodata_value)
                                    {
                                        dh = (dtm[row, col] - dtm[row + i_tf, col + j_tf]) / d_x; // if receiving cell is in catchment, calculate slope
                                    }
                                    else
                                    {
                                        dh = 0;
                                    }
                                }
                                else
                                {
                                    dh = 0; // receiving cell outside catchment, so we assume a slope of 0 percent in order to do the calculations below
                                }

                                double dh_deg = Math.Atan(dh);

                                // growth of tree root system. A spherical growth model is assumed. 
                                // Maximum root system width W_m = 4 m in a circle. This will later be converted to a square surface with the same area
                                // Maximum root system depth D_m = 0.7 m. // From paper Finke on tree fall
                                // These depths are reached after 150 years (typical life span)
                                if (thickness_calc(row, col, 0) < 0)
                                {
                                    Debug.WriteLine("err_tf1");
                                }
                                double W_m, D_m;
                                int tree_age = age_of_trees.Next(0, age_a_max); // selects age between 0 and maximum age of tree, for growth model
                                if (tree_age <= growth_a_max)
                                {
                                    W_m = W_m_max * (3 / 2 * tree_age / growth_a_max - 0.5 * Math.Pow((tree_age / growth_a_max), 3));
                                    D_m = D_m_max * (3 / 2 * tree_age / growth_a_max - 0.5 * Math.Pow((tree_age / growth_a_max), 3));
                                }
                                else
                                {
                                    W_m = W_m_max;
                                    D_m = D_m_max;
                                } // growth formula of trees, giving variable root sizes

                                // Convert spherical surface area to square surface area, to facilitate calculations
                                W_m = Math.Sqrt(Math.PI * Math.Pow(W_m / 2, 2));
                                // Debug.WriteLine("tf4");

                                // Calculation of transported mass over distance
                                int n_affected_cells = 1; // number of affected cells (in a square, so n_cells * n_cells
                                while (W_m > n_affected_cells * dx)
                                {
                                    n_affected_cells += 2;
                                }
                                tree_fall_mass = new double[5];
                                tree_fall_om = new double[2];
                                double[] tree_fall_CN = new double[n_cosmo];

                                tree_fall_frac = new double[n_affected_cells, n_affected_cells]; // keep track of fractions that are removed from all cells, so that those fractions can be redistributed in teh same pattern one or a few cells outward. 
                                tree_fall_frac_sum = 0;

                                // Debug.WriteLine("N affected cells = {0}", n_affected_cells);

                                // minimaps(row, col); // Base situation

                                for (int ii = (n_affected_cells - 1) / -2; ii <= (n_affected_cells - 1) / 2; ii++)
                                {
                                    for (int jj = (n_affected_cells - 1) / -2; jj <= (n_affected_cells - 1) / 2; jj++)
                                    {
                                        // Debug.WriteLine("tf10");

                                        if (Math.Abs(ii) == (n_affected_cells - 1) / 2 | Math.Abs(jj) == (n_affected_cells - 1) / 2) // cell on the side
                                        {
                                            if (Math.Abs(ii) == (n_affected_cells - 1) / 2 & Math.Abs(jj) == (n_affected_cells - 1) / 2) // all corner cells. Fraction eroded = overlap^2 / dx^2
                                            {
                                                tf_frac_dx = Math.Pow(dx - ((n_affected_cells * dx - W_m) / 2), 2) / Math.Pow(dx, 2);
                                            }
                                            else // all other border cells: fraction = overlap * dx / dx^2
                                            {
                                                tf_frac_dx = ((dx - ((n_affected_cells * dx - W_m) / 2)) * dx) / Math.Pow(dx, 2);
                                            }
                                        }
                                        else
                                        {
                                            tf_frac_dx = 1;
                                        }
                                        if (tf_frac_dx > 1) { MessageBox.Show("df_frac_dx > 1"); }
                                        tree_fall_frac[(n_affected_cells - 1) / 2 + ii, (n_affected_cells - 1) / 2 + jj] = tf_frac_dx;
                                        tree_fall_frac_sum += tf_frac_dx;

                                        if (((row + ii) >= 0) && ((row + ii) < nr) && ((col + jj) >= 0) && ((col + jj) < nc) && dtm[row + ii, col + jj] != nodata_value)
                                        {
                                            // Debug.WriteLine("tf10b");

                                            old_soil_depth_m = soildepth_m[row + ii, col + jj];
                                            // if (soildepth_m[row + ii, col + jj] < 50) { Debug.WriteLine("tf1 d = {0}, at t {1}, r {2}, c {3}", soildepth_m[row + ii, col + jj], t, row + ii, col + jj); Debugger.Break(); }

                                            // Debug.WriteLine("tf5");

                                            double depth = 0, tf_frac_dz;
                                            int lay = 0;
                                            while (depth < D_m & lay < max_soil_layers)
                                            {
                                                // fraction of lowest layer which is incorporated
                                                if (depth + layerthickness_m[row + ii, col + jj, lay] <= D_m) { tf_frac_dz = 1; } // fraction of layer taken up by roots, in z direction
                                                else { tf_frac_dz = (D_m - depth) / layerthickness_m[row + ii, col + jj, lay]; }

                                                if (tf_frac_dz > 1) { MessageBox.Show("df_frac_dz > 1"); }

                                                // uptake of sediments
                                                for (int tex = 0; tex < 5; tex++)
                                                {
                                                    tree_fall_mass[tex] += texture_kg[row + ii, col + jj, lay, tex] * tf_frac_dz * tf_frac_dx;
                                                    texture_kg[row + ii, col + jj, lay, tex] -= texture_kg[row + ii, col + jj, lay, tex] * tf_frac_dz * tf_frac_dx;
                                                }
                                                tree_fall_om[0] += old_SOM_kg[row + ii, col + jj, lay] * tf_frac_dz * tf_frac_dx;
                                                old_SOM_kg[row + ii, col + jj, lay] -= old_SOM_kg[row + ii, col + jj, lay] * tf_frac_dz * tf_frac_dx;
                                                tree_fall_om[1] += young_SOM_kg[row + ii, col + jj, lay] * tf_frac_dz * tf_frac_dx;
                                                young_SOM_kg[row + ii, col + jj, lay] -= young_SOM_kg[row + ii, col + jj, lay] * tf_frac_dz * tf_frac_dx;

                                                if (CN_checkbox.Checked)
                                                {
                                                    for (int cosmo = 0; cosmo < n_cosmo; cosmo++)
                                                    {
                                                        double transport = CN_atoms_cm2[row + ii, col + jj, lay, cosmo] * tf_frac_dz * tf_frac_dx;
                                                        tree_fall_CN[cosmo] += transport;
                                                        CN_atoms_cm2[row + ii, col + jj, lay, cosmo] -= transport;
                                                    }
                                                }

                                                if (OSL_checkbox.Checked)
                                                {
                                                    // MvdM develop
                                                }
                                                // Debug.WriteLine("tf10c");

                                                // if (thickness_calc(row, col, 0) < 0) { Debugger.Break(); }

                                                // verder
                                                // check if all fractions are calculated correctly
                                                // what if W_m == dx, than the fraction is 0; correct by doing 1-fraction? is the eroded fraction still calculate correctly?
                                                // calculations of fraction have to be corrected
                                                // redistribution to a next cell, with the right distance etc 
                                                // solve mass loss tree fall

                                                // update depth and reference layer
                                                depth += layerthickness_m[row + ii, col + jj, lay];
                                                lay += 1;
                                            } // end while depth  < Dm
                                              //Debug.WriteLine("Total soil mass: {0}", total_soil_mass(row + ii, col + jj));
                                              //displaysoil(row + ii, col + jj);
                                            remove_empty_layers(row + ii, col + jj);
                                            //Debug.WriteLine("Total soil mass: {0}", total_soil_mass(row + ii, col + jj));
                                            //displaysoil(row + ii, col + jj);

                                            // Debug.WriteLine("tf11");
                                            update_all_layer_thicknesses(row + ii, col + jj);
                                            update_all_layer_thicknesses(row + ii, col + jj); // meij twice, because bulk density depends on depth. This way, the thickness of the empty layers is set to 0 in the first calculation, and used for bulk density in the second calculation

                                            // Elevation change by erosion (removal of material). 
                                            soildepth_m[row + ii, col + jj] = total_soil_thickness(row + ii, col + jj);
                                            //if (soildepth_m[row + ii, col + jj] < 50) { Debug.WriteLine("tf2 d = {0}, at t {1}, r {2}, c {3}", soildepth_m[row + ii, col + jj], t, row + ii, col + jj); Debugger.Break(); }
                                            if (thickness_calc(row, col, 0) < 0)
                                            {
                                                Debug.WriteLine("err_tf2");
                                            }
                                            // Debug.WriteLine("tf12");

                                            if (old_soil_depth_m - soildepth_m[row + ii, col + jj] > 1)
                                            {
                                                Debug.WriteLine("err_tf3");
                                            }

                                            dtm[row + ii, col + jj] += soildepth_m[row + ii, col + jj] - old_soil_depth_m;
                                            dz_treefall[row + ii, col + jj] += soildepth_m[row + ii, col + jj] - old_soil_depth_m;
                                            dtmchange_m[row + ii, col + jj] += soildepth_m[row + ii, col + jj] - old_soil_depth_m;
                                            // Debug.WriteLine("erosion by tree fall = {0}", soildepth_m[row + ii, col + jj] - old_soil_depth_m);
                                            // Debug.WriteLine("tf13");

                                        } // end ii or jj in the area
                                    } // end jj
                                } // end ii
                                  //if (n_affected_cells > 1) { Debugger.Break(); }
                                  // minimaps(row, col); // After erosion

                                // Redistribution of material, deposition
                                // Debug.WriteLine("tf14");
                                double falldist;
                                double dh_rad = dh_deg * (Math.PI / 180);
                                if (dh < 0) // negative slope, tree falls upslope
                                {
                                    falldist = W_m / 2 * (Math.Cos(dh_rad) - Math.Sin(dh_rad)) - D_m / 2 * (Math.Cos(dh_rad) + Math.Sin(dh_rad));
                                }
                                else // positive or zero slope, tree falls downslope
                                {
                                    falldist = W_m / 2 * (Math.Cos(dh_rad) + Math.Sin(dh_rad)) + D_m / 2 * (Math.Sin(dh_rad) - Math.Cos(dh_rad));
                                }

                                // falldist is the distance where the centerpoint of the soil-root mass ends up. The distribution of the material follows the same pattern as the uptake, only shifted a few cells. The whole clump is moved a few steps

                                // Debug.WriteLine("tf15");
                                int ndist_cells = 0;
                                while (falldist > (ndist_cells + 0.5) * dx)
                                {
                                    ndist_cells += 1;
                                }
                                rowsink = row + ndist_cells * i_tf;
                                colsink = col + ndist_cells * j_tf;
                                for (int ii = (n_affected_cells - 1) / -2; ii <= (n_affected_cells - 1) / 2; ii++)
                                {
                                    for (int jj = (n_affected_cells - 1) / -2; jj <= (n_affected_cells - 1) / 2; jj++)
                                    {
                                        // Debug.WriteLine("tf16");

                                        tf_frac_dx = tree_fall_frac[(n_affected_cells - 1) / 2 + ii, (n_affected_cells - 1) / 2 + jj] / tree_fall_frac_sum;
                                        if (((rowsink + ii) >= 0) && ((rowsink + ii) < nr) && ((colsink + jj) >= 0) && ((colsink + jj) < nc) && dtm[rowsink + ii, colsink + jj] != nodata_value)
                                        {
                                            // Debug.WriteLine("tf17");

                                            for (int tex = 0; tex < 5; tex++)
                                            {
                                                texture_kg[rowsink + ii, colsink + jj, 0, tex] += tree_fall_mass[tex] * tf_frac_dx;
                                            }
                                            old_SOM_kg[rowsink + ii, colsink + jj, 0] += tree_fall_om[0] * tf_frac_dx;
                                            old_SOM_kg[rowsink + ii, colsink + jj, 0] += tree_fall_om[1] * tf_frac_dx;

                                            if (CN_checkbox.Checked)
                                            {
                                                for (int cosmo = 0; cosmo < n_cosmo; cosmo++)
                                                {
                                                    CN_atoms_cm2[rowsink + ii, colsink + jj, 0, cosmo] += tree_fall_CN[cosmo] * tf_frac_dx;
                                                }
                                            }

                                            if (OSL_checkbox.Checked)
                                            {

                                            }

                                            // elevation change by deposition
                                            old_soil_depth_m = soildepth_m[rowsink + ii, colsink + jj];
                                            double ds_1 = soildepth_m[rowsink + ii, colsink + jj];
                                            update_all_layer_thicknesses(rowsink + ii, colsink + jj);
                                            double ds_2 = soildepth_m[rowsink + ii, colsink + jj];
                                            update_all_layer_thicknesses(rowsink + ii, colsink + jj); // update twice, to approach real BD value, which is depth dependent
                                            double ds_3 = soildepth_m[rowsink + ii, colsink + jj];
                                            //if (soildepth_m[rowsink + ii, colsink + jj] < 50)
                                            //{
                                            //    Debug.WriteLine("tf3 d = {0}, at t {1}, r {2}, c {3}", soildepth_m[rowsink + ii, colsink + jj], t, rowsink + ii, colsink + jj);
                                            //    Debugger.Break();
                                            //}

                                            soildepth_m[rowsink + ii, colsink + jj] = total_soil_thickness(rowsink + ii, colsink + jj);
                                            dtm[rowsink + ii, colsink + jj] += soildepth_m[rowsink + ii, colsink + jj] - old_soil_depth_m;
                                            dz_treefall[rowsink + ii, colsink + jj] += soildepth_m[rowsink + ii, colsink + jj] - old_soil_depth_m;
                                            dtmchange_m[rowsink + ii, colsink + jj] += soildepth_m[rowsink + ii, colsink + jj] - old_soil_depth_m;
                                            //Debug.WriteLine("deposition by tree fall = {0}", soildepth_m[row + ndist_cells * i, col + ndist_cells * j] - old_soil_depth_m);

                                        } // end_time if dtm[,] = nodata_value
                                        else
                                        {
                                            for (int tex = 0; tex < 5; tex++)
                                            {
                                                exported_mass_tf += tree_fall_mass[tex] * tf_frac_dx;
                                            }
                                            exported_mass_tf += tree_fall_om[0] * tf_frac_dx;
                                            exported_mass_tf += tree_fall_om[1] * tf_frac_dx;
                                        }
                                    } // end jj
                                } // end ii
                                  // minimaps(row, col); // After deposition

                            } // end chance ==1 (tree is falling)
                        } // end dtm != nodata_value
                    } // end col
                } // end row
                  // double mass_after_tf = total_catchment_mass_decimal() + exported_mass_tf;
                  //if (mass_before_tf != mass_after_tf) { MessageBox.Show("Tree fall mass not equal. difference = "+ (mass_before_tf - mass_after_tf)); }
                if (fallen == true)
                {
                    // Debug.WriteLine("sink cell 1");
                    //displaysoil(rowsink, colsink);

                    //if (t == 4)
                    //{
                    //    Debug.WriteLine("Total soil mass: {0}", total_soil_mass(rowsource,colsource));
                    //    displaysoil(rowsource, colsource);
                    //    Debug.WriteLine("Total soil mass: {0}", total_soil_mass(rowsink,colsink));
                    //    displaysoil(rowsink,colsink);
                    //}
                    // Debug.WriteLine("tf1a");
                    for (int r_tf = 0; r_tf < nr; r_tf++)
                    {
                        for (int c_tf = 0; c_tf < nc; c_tf++)
                        {
                            remove_empty_layers(r_tf, c_tf);
                            remove_empty_layers(r_tf, c_tf);
                            if (total_soil_mass_kg_decimal(r_tf, c_tf) <= 0) { Debugger.Break(); }
                            update_all_layer_thicknesses(r_tf, c_tf);
                        }
                    }
                    soil_update_split_and_combine_layers();
                    // Debug.WriteLine("tf1b");

                    //if (t == 4)
                    //{
                    //    Debug.WriteLine("Total soil mass: {0}", total_soil_mass(rowsource, colsource));
                    //    displaysoil(rowsource, colsource);
                    //    Debug.WriteLine("Total soil mass: {0}", total_soil_mass(rowsink, colsink));
                    //    displaysoil(rowsink, colsink);
                    //    Debugger.Break();
                    //}
                    if (NA_in_map(dtm) > 0)
                    {
                        Debug.WriteLine("err_tf5");
                    }
                    if (NA_in_map(soildepth_m) > 0)
                    {
                        Debug.WriteLine("err_tf6");
                    }

                }
                Decimal tf_mass_after = total_catchment_mass_decimal() + Convert.ToDecimal(exported_mass_tf);
                //if (CN_checkbox.Checked) { CN_after = total_CNs(); if (((CN_before - CN_after) / (CN_after+1)) > 1E-6) { Debugger.Break(); } }
                //if (Math.Abs(tf_mass_before - tf_mass_after)>0.001) { Debugger.Break(); }
            }
            catch
            {
                Debug.WriteLine("err_tf7");

            }

        }

        private void calculate_bedrock_weathering()
        {
            // as function of infiltration?
            //Debug.WriteLine("Entered bedrock weathering");												
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
            int soil_layer, lowest_soil_layer;
            for (row = 0; row < nr; row++)
            {
                for (col = 0; col < nc; col++)
                {
                    if (dtm[row, col] != nodata_value)
                    {
                        double weatheringdepth = 0;
                        //Debug.WriteLine(" bedrock weathering at r " + row + " c " + col);
                        //if the first occurrence of bedrock is the hardlayer, then no weathering should occur.
                        //if more weathering is calculated than needed to get to the hardlayer, then it should be thus limited. 

                        weatheringdepth = soildepth_m[row, col];

                        // humped
                        if (rockweath_method.SelectedIndex == 0)
                        {
                            bedrock_weathering_m[row, col] = P0 * (Math.Exp(-k1 * weatheringdepth) - Math.Exp(-k2 * weatheringdepth)) + Pa;

                        }
                        if (rockweath_method.SelectedIndex == 1)
                        {
                            // exponential (Heimsath, Chappell et al., 2000)
                            bedrock_weathering_m[row, col] = P0 * (Math.Exp(-k1 * weatheringdepth));
                        }

                        if (rockweath_method.SelectedIndex == 2)
                        {
                            if (daily_water.Checked)
                            {
                                bedrock_weathering_m[row, col] = P0 * -k1 * (Iy[row, col] - Imin) / (Imax - Imin);
                            }
                        }
                        //we now know how deep we would weather into normal bedrock
                        if (blocks_active == 1)
                        {
                            double newlowestelevsoil = dtm[row, col] - soildepth_m[row, col] - bedrock_weathering_m[row, col];
                            double oldlowestelevsoil = dtm[row, col] - soildepth_m[row, col];
                            if (newlowestelevsoil < hardlayerelevation_m && oldlowestelevsoil >= hardlayerelevation_m)
                            {
                                //we limit bedrock weathering to the part of the bedrock above hardlayer:
                                bedrock_weathering_m[row, col] = (dtm[row, col] - soildepth_m[row, col]) - hardlayerelevation_m;
                                Debug.WriteLine(" limited bedrock weathering to stop at hardlayer r " + row + " c " + col + " dtm " + dtm[row, col]);
                                //and apply the rest of the weathering to increasing openness of the hardlayer:
                                hardlayeropenness_fraction[row, col] += Convert.ToSingle((hardlayerelevation_m - newlowestelevsoil) * hardlayer_weath_contrast);
                                Debug.WriteLine(" increased openness of hardlayer to " + hardlayeropenness_fraction[row, col]);
                                if (hardlayeropenness_fraction[row, col] > 0.5) { hardlayeropenness_fraction[row, col] = 0.5f; }
                            }
                        }

                        soildepth_m[row, col] += bedrock_weathering_m[row, col]; // this will really be updated at the end of this timestep, but this is a good approximation for the moment

                        //we also add this amount of coarse material to the lowest layer of our soil
                        soil_layer = 0; lowest_soil_layer = 0;
                        while (layerthickness_m[row, col, soil_layer] > 0 & soil_layer < max_soil_layers) // MvdM added second conditional for when all layers are already filled
                        {
                            lowest_soil_layer = soil_layer;
                            soil_layer++;
                            //Debug.WriteLine(" lowest soil layer now " + soil_layer);
                            if (lowest_soil_layer == max_soil_layers - 1) { break; }
                        }
                        texture_kg[row, col, lowest_soil_layer, 0] += bedrock_weathering_m[row, col] * 2700 * dx * dx;   // to go from m (=m3/m2) to kg, we multiply by m2 and by kg/m3
                    }

                }
            }

        }

        private void calculate_tilting()
        {
            Task.Factory.StartNew(() =>
            {
                this.InfoStatusPanel.Text = "tilting calculation";
            }, CancellationToken.None, TaskCreationOptions.None, guiThread);
            //this.InfoStatusPanel.Text = "tilting calculation";								  
            int row, col;

            for (row = 0; row < nr; row++)
            {
                for (col = 0; col < nc; col++)
                {
                    if (tilt_location == 0) { dtm[row, col] += tilt_intensity * (col / nc); }
                    if (tilt_location == 1) { dtm[row, col] += tilt_intensity * (row / nr); }
                    if (tilt_location == 2) { dtm[row, col] += tilt_intensity * ((nc - col) / nc); }
                    if (tilt_location == 3) { dtm[row, col] += tilt_intensity * ((nr - row) / nr); }
                }
            }
        }

        private void calculate_uplift()
        {
            Task.Factory.StartNew(() =>
            {
                this.InfoStatusPanel.Text = "uplift calculation";
            }, CancellationToken.None, TaskCreationOptions.None, guiThread);
            //this.InfoStatusPanel.Text = "uplift calculation";	  
            int row, col;

            for (row = 0; row < nr; row++)
            {
                for (col = 0; col < nc; col++)
                {
                    if (lift_location == 0 && row > lift_location) { dtm[row, col] += lift_intensity; }
                    if (lift_location == 1 && row > lift_location) { dtm[row, col] += lift_intensity; }
                    if (lift_location == 2 && row > lift_location) { dtm[row, col] += lift_intensity; }
                    if (lift_location == 3 && row > lift_location) { dtm[row, col] += lift_intensity; }
                }
            }
        }

        #endregion
    }
}
