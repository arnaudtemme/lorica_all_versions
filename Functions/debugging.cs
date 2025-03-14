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

    }
}
