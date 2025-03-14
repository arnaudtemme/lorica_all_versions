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

        double[,] aridity_vegetation;

        void determine_vegetation_type()
        {
            aridity_vegetation = new double[nr, nc];
            double outflow = 0, aridity, outflowcells = 0;

            for (int vrow = 0; vrow < nr; vrow++)
            {
                for (int vcol = 0; vcol < nc; vcol++)
                {

                    if (dtm[vrow, vcol] != -9999)
                    {
                        // adjusted Budyko
                        // aridity (water stress) = P/PET. If PET>P, water stress, aridity < 1.
                        // P is replaced by (I+ETa), Incoming water that infiltrates in the cell is captured in I

                        outflow = OFy_m[vrow, vcol, 0] - OFy_m[vrow, vcol, 9];
                        // aridity = (Iy[vrow, vcol] + ETay[vrow, vcol] - outflow) / ET0y[vrow, vcol];
                        aridity = (Iy[vrow, vcol] + ETay[vrow, vcol]) / ET0y[vrow, vcol];

                        // First, we had (I+ET)*(P/(P+O) / PET. But I think the scaling is not necessary. 
                        if (aridity < 0)
                        {
                            Debug.WriteLine("err_vg1");
                        }
                        aridity_vegetation[vrow, vcol] = aridity;

                        if (aridity < 1)
                        {
                            vegetation_type[vrow, vcol] += 1; // arid / grass
                        }
                        else
                        {
                            vegetation_type[vrow, vcol] += 1000; // humid / forest
                        }
                    }
                }
            }
        }

        void change_vegetation_parameters()
        {
            // vegetation coefficients for ET
            for (int vrow = 0; vrow < nr; vrow++)
            {
                for (int vcol = 0; vcol < nc; vcol++)
                {
                    if (dtm[vrow, vcol] != -9999)
                    {
                        if (aridity_vegetation[vrow, vcol] < 1) { veg_correction_factor[vrow, vcol] = .75; } // all year long, according to FAO report 56
                        else { veg_correction_factor[vrow, vcol] = .85; } // I took the mid-season coefficient (95) of most deciduous crops and decreased it to 85 to account for less vegetation in other times of the year
                        if (t >= (end_time - 300)) { veg_correction_factor[vrow, vcol] = .45; }  // if there is agriculture
                    }
                }
            }
        }

        void calculate_TPI(int windowsize)
        {
            try
            {
                //Debug.WriteLine("Started calculating TPI");
                // check if window size is an uneven number, so the window has a center cell
                if (windowsize % 2 == 0) { MessageBox.Show("window size for TPI calculations should be an uneven number"); }

                int windowrange = (windowsize - 1) / 2;
                for (int row = 0; row < nr; row++)
                {
                    for (int col = 0; col < nc; col++)
                    {
                        if (dtm[row, col] != -9999)
                        {
                            double tpisum = 0;
                            double tpicount = 0;

                            // calculate moving window average
                            for (int rr = windowrange * -1; rr <= windowrange; rr++)
                            {
                                for (int cc = windowrange * -1; cc <= windowrange; cc++)
                                {
                                    if (row + rr >= 0 & row + rr < nr & col + cc >= 0 & col + cc < nc) // if cell exists in the DEM, 
                                    {
                                        if (dtm[row + rr, col + cc] != -9999 & (rr != 0 | cc != 0)) // if cell contains a value and cell isn't the target cell, it's considered in the TPI
                                        {
                                            tpisum += dtm[row + rr, col + cc];
                                            tpicount += 1;
                                        }
                                    }
                                }
                            }
                            tpi[row, col] = dtm[row, col] - (tpisum / tpicount);
                        }

                    }
                }
                //Debug.WriteLine("Finished calculating TPI");
            }
            catch
            {
                Debug.WriteLine("Error in calculating TPI");
            }
        }

    }
}
