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
        void minimaps(int row, int col)
        {
            int lowerrow, upperrow, lowercol, uppercol, disrow, discol;
            lowerrow = row - 4; if (lowerrow < 0) { lowerrow = 0; }
            upperrow = row + 4; if (upperrow > nr - 1) { upperrow = nr - 1; }
            lowercol = col - 4; if (lowercol < 0) { lowercol = 0; }
            uppercol = col + 4; if (uppercol > nc - 1) { uppercol = nc - 1; }
            string mess;

            // dtm
            Debug.Write(" \n"); Debug.Write("      DEM");
            for (discol = lowercol; discol < (uppercol + 1); discol++)
            {
                //Qs = {0:F8}", tomsedi * dx * dx
                mess = String.Format("  {0:D10}", discol); Debug.Write(mess);
            }
            Debug.Write(" \n"); mess = String.Format(" {0:D8}", lowerrow); Debug.Write(mess);
            for (disrow = lowerrow; disrow < (upperrow + 1); disrow++)
            {
                for (discol = lowercol; discol < (uppercol + 1); discol++)
                {
                    mess = String.Format("  {0:F6}", dtm[disrow, discol]); Debug.Write(mess);
                }
                Debug.Write(" \n"); if ((disrow + 1) <= upperrow) { mess = String.Format(" {0:D8}", (disrow + 1)); Debug.Write(mess); }
            }
            /*
            //water flow
            Debug.Write(" \n"); Debug.Write("      Q");
            for (discol = lowercol; discol < (uppercol + 1); discol++)
            {
                //Qs = {0:F8}", tomsedi * dx * dx
                mess = String.Format("  {0:D10}", discol); Debug.Write(mess);
            }
            Debug.Write(" \n"); mess = String.Format(" {0:D8}", lowerrow); Debug.Write(mess);
            for (disrow = lowerrow; disrow < (upperrow + 1); disrow++)
            {
                for (discol = lowercol; discol < (uppercol + 1); discol++)
                {
                    mess = String.Format("  {0:F6}", OFy_m[disrow, discol,0]); Debug.Write(mess);
                }
                Debug.Write(" \n"); if ((disrow + 1) <= upperrow) { mess = String.Format(" {0:D8}", (disrow + 1)); Debug.Write(mess); }
            }

            Debug.Write(" \n"); Debug.Write("SedI_TRA_kg");
            for (discol = lowercol; discol < (uppercol + 1); discol++)
            {
                mess = String.Format("  {0:D10}", discol); Debug.Write(mess);
            }
            Debug.Write(" \n"); mess = String.Format(" {0:D10}", lowerrow); Debug.Write(mess);
            for (disrow = lowerrow; disrow < (upperrow + 1); disrow++)
            {
                for (discol = lowercol; discol < (uppercol + 1); discol++)
                {
                    mess = String.Format("  {0:F8}", sediment_in_transport_kg[disrow, discol, 0]); Debug.Write(mess);
                }
                Debug.Write(" \n"); if ((disrow + 1) <= upperrow) { mess = String.Format(" {0:D10}", (disrow + 1)); Debug.Write(mess); }
            } */
            /*
            Debug.Write(" \n"); Debug.Write("fillheightA_m    ");
            for (discol = lowercol; discol < (uppercol + 1); discol++)
            {
                mess = String.Format("  {0:D10}", discol); Debug.Write(mess);
            }
            Debug.Write(" \n"); mess = String.Format(" {0:D10}", lowerrow); Debug.Write(mess);
            for (disrow = lowerrow; disrow < (upperrow + 1); disrow++)
            {
                for (discol = lowercol; discol < (uppercol + 1); discol++)
                {
                    mess = String.Format("  {0:F8}", dtmfill_A[disrow, discol]); Debug.Write(mess);
                }
                Debug.Write(" \n"); if ((disrow + 1) <= upperrow) { mess = String.Format(" {0:D10}", (disrow + 1)); Debug.Write(mess); }
            }

            Debug.Write(" \n"); Debug.Write("dz_ero_m    ");
            for (discol = lowercol; discol < (uppercol + 1); discol++)
            {
                mess = String.Format("  {0:D10}", discol); Debug.Write(mess);
            }
            Debug.Write(" \n"); mess = String.Format(" {0:D10}", lowerrow); Debug.Write(mess);
            for (disrow = lowerrow; disrow < (upperrow + 1); disrow++)
            {
                for (discol = lowercol; discol < (uppercol + 1); discol++)
                {
                    mess = String.Format("  {0:F8}", dz_ero_m[disrow, discol]); Debug.Write(mess);
                }
                Debug.Write(" \n"); if ((disrow + 1) <= upperrow) { mess = String.Format(" {0:D10}", (disrow + 1)); Debug.Write(mess); }
            }

            Debug.Write(" \n"); Debug.Write(" DEPRESSION");
            for (discol = lowercol; discol < (uppercol + 1); discol++)
            {
                mess = String.Format("  {0:D10}", discol); Debug.Write(mess);
            }
            Debug.Write(" \n"); mess = String.Format(" {0:D10}", lowerrow); Debug.Write(mess);
            for (disrow = lowerrow; disrow < (upperrow + 1); disrow++)
            {
                for (discol = lowercol; discol < (uppercol + 1); discol++)
                {
                    mess = String.Format("  {0:D10}", depression[disrow, discol]); Debug.Write(mess);
                }
                Debug.Write(" \n"); if ((disrow + 1) <= upperrow) { mess = String.Format(" {0:D10}", (disrow + 1)); Debug.Write(mess); }
            }

            Debug.Write(" \n"); Debug.Write("    status");
            for (discol = lowercol; discol < (uppercol + 1); discol++)
            {
                mess = String.Format("  {0:D10}", discol); Debug.Write(mess);
            }
            Debug.Write(" \n"); mess = String.Format(" {0:D10}", lowerrow); Debug.Write(mess);
            for (disrow = lowerrow; disrow < (upperrow + 1); disrow++)
            {
                for (discol = lowercol; discol < (uppercol + 1); discol++)
                {
                    mess = String.Format("  {0:D10}", status_map[disrow, discol]); Debug.Write(mess);
                }
                Debug.Write(" \n"); if ((disrow + 1) <= upperrow) { mess = String.Format(" {0:D10}", (disrow + 1)); Debug.Write(mess); }
            }
            */

        }

        private void displaysoil(int row, int col)
        {

            int layer; double cumthick = 0; double depth = 0, z_layer = dtm[row, col];
            //  if (t == 0) { Debug.WriteLine("digitally augering and analysing at row " + row + " col " + col); }//header
            Debug.WriteLine("row col t nlayer cumth(m)  thick(m)  depth(m) z(m) coarse(kg) sand(kg)   silt(kg)   clay(kg)   fine(kg)   YOM(kg)    OOM(kg)   YOM/OOM   w%coarse   w%sand   w%silt   w%clay   w%fineclay BD");

            for (layer = 0; layer < max_soil_layers; layer++) // only the top layer
            {
                //if (layerthickness_m[row, col, layer] > 0)
                //{
                cumthick += layerthickness_m[row, col, layer];
                depth -= layerthickness_m[row, col, layer] / 2;
                double totalweight = texture_kg[row, col, layer, 0] + texture_kg[row, col, layer, 1] + texture_kg[row, col, layer, 2] + texture_kg[row, col, layer, 3] + texture_kg[row, col, layer, 4] + young_SOM_kg[row, col, layer] + old_SOM_kg[row, col, layer];  
                try { Debug.WriteLine(row + " " + col + " " + t + " " + layer + " " + cumthick + " " + layerthickness_m[row, col, layer] + " " + depth + " " + z_layer + " " + texture_kg[row, col, layer, 0] + " " + texture_kg[row, col, layer, 1] + " " + texture_kg[row, col, layer, 2] + " " + texture_kg[row, col, layer, 3] + " " + texture_kg[row, col, layer, 4] + " " + young_SOM_kg[row, col, layer] + " " + old_SOM_kg[row, col, layer] + " " + young_SOM_kg[row, col, layer] / old_SOM_kg[row, col, layer] + " " + texture_kg[row, col, layer, 0] / totalweight + " " + texture_kg[row, col, layer, 1] / totalweight + " " + texture_kg[row, col, layer, 2] / totalweight + " " + texture_kg[row, col, layer, 3] / totalweight + " " + texture_kg[row, col, layer, 4] / totalweight + " " + bulkdensity[row, col, layer]); }
                catch { Debug.WriteLine("Cannot write soilprofile"); }
                depth -= layerthickness_m[row, col, layer] / 2;
                z_layer -= layerthickness_m[row, col, layer];
                //}
            }

            /*if (t < end_time )
            {

                for (layer = 0; layer < max_soil_layers; layer++) // all layers
                {
                    if (layerthickness_m[row, col, layer] > 0)
                    {

                        cumthick += layerthickness_m[row, col, layer];
                        double totalweight = texture_kg[row, col, layer, 0] + texture_kg[row, col, layer, 1] + texture_kg[row, col, layer, 2] + texture_kg[row, col, layer, 3] + texture_kg[row, col, layer, 4] + young_SOM_kg[row, col, layer] + old_SOM_kg[row, col, layer];
                        try { Debug.WriteLine(t + " " + cumthick + " " + layerthickness_m[row, col, layer] + " " + texture_kg[row, col, layer, 0] + " " + texture_kg[row, col, layer, 1] + " " + texture_kg[row, col, layer, 2] + " " + texture_kg[row, col, layer, 3] + " " + texture_kg[row, col, layer, 4] + " " + young_SOM_kg[row, col, layer] + " " + old_SOM_kg[row, col, layer] + " " + young_SOM_kg[row, col, layer] / old_SOM_kg[row, col, layer] + " " + (texture_kg[row, col, layer, 3] + texture_kg[row, col, layer, 4]) / totalweight + " " + texture_kg[row, col, layer, 2] / totalweight + " " + texture_kg[row, col, layer, 1] / totalweight); }
                        catch { Debug.WriteLine("Cannot write soilprofile"); }
                    }
                }
            }*/
            //Debug.WriteLine("");
        }

        bool check_negative_weight(int row, int col)
        {
            bool check = false;
            for (int layer1 = 0; layer1 < max_soil_layers; layer1++)
            {
                for (int tex = 0; tex < 5; tex++)
                {
                    if (texture_kg[row, col, layer1, tex] < 0)
                    {
                        check = true;
                    }
                }
            }
            return (check);
        }

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
            update_all_layer_thicknesses(rowthick, colthick);
            double tot_thick = 0;
            for (int lay = 0; lay < max_soil_layers; lay++)
            {
                tot_thick += layerthickness_m[rowthick, colthick, lay];
            }
            return (tot_thick);
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
    }
}
