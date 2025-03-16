using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static LORICA4.Mother_form;

namespace LORICA4
{
    public partial class Mother_form
    {

        #region Hardlayer and block code

        void hardlayer_breaking()
        {
            //layers break off blocks when dz across the layer is larger than layer thickness
            //by a certain margin > 1 (I imagine)
            //Debug.WriteLine(" started breaking hard layer");
            //Debug.WriteLine(" Current number of blocks is " + Blocklist.Count);
            Random location_gen = new Random(t); // t as random seed to get deterministic results
            for (int hrd_lyr = 0; hrd_lyr < nhardlayers; hrd_lyr++)
            {
                for (int row = 0; row < nr; row++)
                {
                    for (int col = 0; col < nc; col++)
                    {
                        if (dtm[row, col] != nodata_value)
                        {
                            if (hardlayer_near_surface(row, col) == true)
                            {
                                //Debug.WriteLine(" hard layer is near surface for " + row + " " + col + " dtm " + dtm[row,col] + " layer " + hardlayerelevation_m);
                                int n_to_s = 5;  //impossible values
                                int e_to_w = 5;
                                //Debug.WriteLine("hardlayerthick " + hardlayerthickness_m + "max_dz_across_layer" + max_dz_across_hardlayer_m(row, col, out n_to_s, out e_to_w));
                                if (max_dz_across_hardlayer_m(row, col, out n_to_s, out e_to_w) > hardlayerthickness_m * 1.25)
                                {
                                    //Debug.WriteLine(" hardlayer breaking at row " + row + " " + col );
                                    dtm[row, col] -= hardlayerthickness_m;
                                    if (n_to_s == 5 | e_to_w == 5) { Debug.WriteLine("invalid direction returned from max_dz_across layer at r" + row + " c " + col); }
                                    //break off blocks and drop  them in direction of max_dz
                                    //floor ensures that blocks are not filling that cell up entirely  
                                    int ndropblocks = Convert.ToInt32(Math.Floor((dx * dx) * (1 - hardlayeropenness_fraction[row, col]) / hardlayerthickness_m));
                                    //Debug.WriteLine(" adding " + ndropblocks + " with size " + hardlayerthickness_m);
                                    for (int blck = 0; blck < ndropblocks; blck++)
                                    {
                                        //Debug.Write(" adding block ");
                                        Blocklist.Add(new Block(Convert.ToSingle(row - n_to_s + location_gen.NextDouble()), Convert.ToSingle(col + e_to_w + location_gen.NextDouble()), hardlayerthickness_m, 0, 0, 0, 0, 0, 0, 0, 0));
                                    }
                                    blocksproduced += ndropblocks;
                                }
                            }
                        }
                    }
                }
            }
            //Debug.WriteLine(" New number of blocks is " + Blocklist.Count);
        }

        bool hardlayer_near_surface(int rowt, int colt)
        {
            if (dtm[rowt, colt] <= (hardlayerelevation_m + 0.1) && dtm[rowt, colt] > (hardlayerelevation_m - 0.001))
            { return true; }
            else { return false; }
        }

        float max_dz_across_hardlayer_m(int nowrow, int nowcol, out int n_to_s, out int e_to_w)
        {
            //Debug.WriteLine(" calculating max dz across hardlayer for " + nowrow + " " + nowcol);
            float max_dz_m = 0;
            n_to_s = 5;
            e_to_w = 5;
            //development possible to account for presence of blocks in downslope cells
            //Debug.WriteLineIf(nowrow == 0 && nowcol == 88,"x");
            //Debug.WriteLineIf(nowrow == 0 && nowcol == 88,  "dtm" + dtm[nowrow, nowcol]);
            if (nowrow > 0 && nowcol > 0) { if (dtm[nowrow - 1, nowcol - 1] != nodata_value) { if ((dtm[nowrow, nowcol] - dtm[nowrow - 1, nowcol - 1]) > max_dz_m) { max_dz_m = Convert.ToSingle(dtm[nowrow, nowcol] - dtm[nowrow - 1, nowcol - 1]); n_to_s = 1; e_to_w = -1; } } }
            if (nowrow > 0) { if (dtm[nowrow - 1, nowcol] != nodata_value) { if ((dtm[nowrow, nowcol] - dtm[nowrow - 1, nowcol]) > max_dz_m) { max_dz_m = Convert.ToSingle(dtm[nowrow, nowcol] - dtm[nowrow - 1, nowcol]); n_to_s = 1; e_to_w = 0; } } }
            if (nowrow > 0 && (nowcol + 1) < nc) { if (dtm[nowrow - 1, nowcol + 1] != nodata_value) { if ((dtm[nowrow, nowcol] - dtm[nowrow - 1, nowcol + 1]) > max_dz_m) { max_dz_m = Convert.ToSingle(dtm[nowrow, nowcol] - dtm[nowrow - 1, nowcol + 1]); n_to_s = 1; e_to_w = 1; } } }
            if (nowcol > 0) { if (dtm[nowrow, nowcol - 1] != nodata_value) { if ((dtm[nowrow, nowcol] - dtm[nowrow, nowcol - 1]) > max_dz_m) { max_dz_m = Convert.ToSingle(dtm[nowrow, nowcol] - dtm[nowrow, nowcol - 1]); n_to_s = 0; e_to_w = -1; } } }
            if ((nowcol + 1) < nc) { if (dtm[nowrow, nowcol + 1] != nodata_value) { if ((dtm[nowrow, nowcol] - dtm[nowrow, nowcol + 1]) > max_dz_m) { max_dz_m = Convert.ToSingle(dtm[nowrow, nowcol] - dtm[nowrow, nowcol + 1]); n_to_s = 0; e_to_w = 1; } } }
            if ((nowrow + 1) < nr && nowcol > 0) { if (dtm[nowrow + 1, nowcol - 1] != nodata_value) { if ((dtm[nowrow, nowcol] - dtm[nowrow + 1, nowcol - 1]) > max_dz_m) { max_dz_m = Convert.ToSingle(dtm[nowrow, nowcol] - dtm[nowrow + 1, nowcol - 1]); n_to_s = -1; e_to_w = -1; } } }
            if ((nowrow + 1) < nr) { if (dtm[nowrow + 1, nowcol] != nodata_value) { if ((dtm[nowrow, nowcol] - dtm[nowrow + 1, nowcol]) > max_dz_m) { max_dz_m = Convert.ToSingle(dtm[nowrow, nowcol] - dtm[nowrow + 1, nowcol]); n_to_s = -1; e_to_w = 0; } } }
            if ((nowrow + 1) < nr && (nowcol + 1) < nc) { if (dtm[nowrow + 1, nowcol + 1] != nodata_value) { if ((dtm[nowrow, nowcol] - dtm[nowrow + 1, nowcol + 1]) > max_dz_m) { max_dz_m = Convert.ToSingle(dtm[nowrow, nowcol] - dtm[nowrow + 1, nowcol + 1]); n_to_s = -1; e_to_w = 1; } } }
            return max_dz_m;
        }

        void block_weathering()
        {
            //blocks are cubic and weather smaller as a function of surface area (size squared)
            if (diagnostic_mode == 1) { Debug.WriteLine(" starting block weathering"); }
            Random location_gen = new Random(t); // t as random seed to get deterministic results
            if (Blocklist.Count > 0)
            {
                int index = 0;
                while (index < Blocklist.Count)
                {
                    //Debug.WriteLine(Blocklist[0].Size_m);
                    //Debug.WriteLine(" Block " + index + " will be weathered. Now size is " + Blocklist[index].Size_m + " total blocks " + Blocklist.Count);
                    //Blocklist[index].Size_m *= blockweatheringratio * Convert.ToSingle(location_gen.NextDouble()+0.5);
                    Blocklist[index].Size_m *= blockweatheringratio;
                    //Debug.WriteLine(" Block " + index + " was weathered. Now size is " + Blocklist[index].Size_m + " total blocks " + Blocklist.Count);
                    if (Blocklist[index].Size_m < blocksizethreshold_m)
                    {
                        texture_kg[Convert.ToInt32(Math.Floor(Blocklist[index].Y_row)), Convert.ToInt32(Math.Floor(Blocklist[index].X_col)), 0, 0] += Math.Pow(Blocklist[index].Size_m, 3) * hardlayerdensity_kg_m3;
                        Blocklist.RemoveAt(index);
                        //this will also update Blocklist.Count, so we don't count too far. 

                    }
                    index++;
                }
            }
            else
            {
                Debug.WriteLine(" currently no blocks to weather ");
            }

        }

        void block_movement()
        {
            //blocks either roll or creep along
            // function of size, but not shape for now
            //accum_creep_alt (8 directions) += creep_alt_timestep
            //creep_alt_timestep (8 directions) = creep volume / (cell surface area - cell block area)
            //two options      :
            //EITHER
            //block rolls if slope in cell + (net_accum_creep_alt in any direction/size block) > 1 (45 degrees)
            //if block rolls, it rolls in first direction where the condition is met
            //by exactly one block size
            //accum creep alt in that direction is reset, others are kept

            //OR
            //block moves with creeping soil (which should always be less ?))
            //distance in any direction is volume of creep from that direction/cell size/soildepth in cell
            //average all distances and average their direction
            //pythagoras
            //does not reset accum_creep_alt

            // this way of thinking means that all blocks will roll, some more than others. Not nice
            // Also, many blocks in a cell or few blocks in a cell makes no difference.
            // In reality, it probably should because more blocks, less space, more bunching up of regolith, more roll
            // That can later be solved by taking dz_source or dz_sink DIV (1-blockcover) in creep calculations.
            if (diagnostic_mode == 1) { Debug.WriteLine(" starting block movement"); }
            Random location_gen = new Random(t);
            //Debug.WriteLine(" starting block movement");
            List<Block> removelist = new List<Block>();
            if (Blocklist.Count > 0)
            {
                //Debug.WriteLine(" blockcount now " + Blocklist.Count);
                foreach (var Block in Blocklist)
                {
                    int row = Convert.ToInt32(Math.Floor(Block.Y_row));
                    int col = Convert.ToInt32(Math.Floor(Block.X_col));
                    //Debug.WriteLine(" block in cell " + row + " " + col);
                    if (row == 0 || row == (nr - 1) || col == 0 || col == (nc - 1))
                    {
                        removelist.Add(Block);
                        //Debug.WriteLine(" added to remove list ");
                    }
                    else if (dtm[row, col] != nodata_value)
                    {
                        // we calculate for four possible directions whether the block should roll:
                        bool block_has_rolled = false;
                        //from north to south
                        double averageslope = (dtm[row - 1, col] - dtm[row + 1, col]) / (2 * dx); // averaged over two cells
                        double additionalslope = (Block.Accumulated_creep_m_0 - Block.Accumulated_creep_m_4) / Block.Size_m;
                        double totalslope = averageslope + additionalslope;
                        if (totalslope > 1)
                        {
                            //Debug.WriteLine(" block rolling. dtm1 " + dtm[row - 1, col] + " dtm2 " + dtm[row + 1, col] + " avslope " + averageslope + " addslope " + additionalslope + " totslope " + totalslope + "rownow " + Block.Y_row + " sizenow " + Block.Size_m);
                            Block.Y_row -= Block.Size_m;
                            block_has_rolled = true;
                            Block.Accumulated_creep_m_0 = 0;
                            Block.Accumulated_creep_m_4 = 0;
                            topoconttoroll += Math.Abs(averageslope);
                            creepconttoroll += Math.Abs(additionalslope);
                            //Debug.WriteLine(" block rolled n to s");
                            if (Math.Floor(Block.Y_row) == nr) { Blocklist.Remove(Block); }
                        } // rolls to south
                        if (totalslope < -1)
                        {
                            //Debug.WriteLine(" block rolling. dtm1 " + dtm[row - 1, col] + " dtm2 " + dtm[row + 1, col] + " avslope " + averageslope + " addslope " + additionalslope + " totslope " + totalslope + "rownow " + Block.Y_row + " sizenow " + Block.Size_m);
                            Block.Y_row += Block.Size_m;
                            block_has_rolled = true;
                            Block.Accumulated_creep_m_0 = 0;
                            Block.Accumulated_creep_m_4 = 0;
                            topoconttoroll += Math.Abs(averageslope);
                            creepconttoroll += Math.Abs(additionalslope);
                            //Debug.WriteLine(" block rolled s to n, lowered row");
                            if (Math.Floor(Block.Y_row) == -1) { Blocklist.Remove(Block); }
                        } // rolls to north

                        if (block_has_rolled == false)
                        {
                            //from NE to SW (note different slope calculation)
                            averageslope = (dtm[row - 1, col + 1] - dtm[row + 1, col - 1]) / (2 * dx * Math.Sqrt(2)); // averaged over two cells, diagonally
                            additionalslope = (Block.Accumulated_creep_m_1 - Block.Accumulated_creep_m_5) / Block.Size_m;
                            totalslope = averageslope + additionalslope;
                            if (totalslope > 1)
                            {   //higher on the NE, rolls to the SW
                                block_has_rolled = true;
                                Block.Y_row -= Convert.ToSingle(1.4142135 * Block.Size_m);
                                Block.X_col += Convert.ToSingle(1.4142135 * Block.Size_m);
                                Block.Accumulated_creep_m_1 = 0;
                                Block.Accumulated_creep_m_5 = 0;
                                //Debug.WriteLine(" block rolled");
                                topoconttoroll += Math.Abs(averageslope);
                                creepconttoroll += Math.Abs(additionalslope);
                                if (Math.Floor(Block.Y_row) == nr | Math.Floor(Block.X_col) == -1) { Blocklist.Remove(Block); }
                            }
                            if (totalslope < -1)
                            {
                                block_has_rolled = true;
                                Block.Y_row += Convert.ToSingle(1.4142135 * Block.Size_m);
                                Block.X_col -= Convert.ToSingle(1.4142135 * Block.Size_m);
                                Block.Accumulated_creep_m_1 = 0;
                                Block.Accumulated_creep_m_5 = 0;
                                //Debug.WriteLine(" block rolled");
                                topoconttoroll += Math.Abs(averageslope);
                                creepconttoroll += Math.Abs(additionalslope);
                                if (Math.Floor(Block.Y_row) == -1 | Math.Floor(Block.X_col) == nc) { Blocklist.Remove(Block); }
                            }
                        }
                        if (block_has_rolled == false)
                        {
                            //from E to W
                            averageslope = (dtm[row, col + 1] - dtm[row, col - 1]) / (2 * dx); // averaged over two cells 
                            additionalslope = (Block.Accumulated_creep_m_2 - Block.Accumulated_creep_m_6) / Block.Size_m;
                            totalslope = averageslope + additionalslope;
                            if (totalslope > 1)
                            {
                                Block.X_col += Block.Size_m;
                                block_has_rolled = true;
                                Block.Accumulated_creep_m_2 = 0;
                                Block.Accumulated_creep_m_6 = 0;
                                //Debug.WriteLine(" block rolled");
                                topoconttoroll += Math.Abs(averageslope);
                                creepconttoroll += Math.Abs(additionalslope);
                                if (Math.Floor(Block.X_col) == -1) { Blocklist.Remove(Block); }
                            }
                            if (totalslope < -1)
                            {
                                Block.X_col -= Block.Size_m;
                                block_has_rolled = true;
                                Block.Accumulated_creep_m_2 = 0;
                                Block.Accumulated_creep_m_6 = 0;
                                //Debug.WriteLine(" block rolled");
                                topoconttoroll += Math.Abs(averageslope);
                                creepconttoroll += Math.Abs(additionalslope);
                                if (Math.Floor(Block.X_col) == nr) { Blocklist.Remove(Block); }
                            }
                        }
                        if (block_has_rolled == false)
                        {
                            //from SE to NW (note different slope calculation)
                            averageslope = (dtm[row + 1, col + 1] - dtm[row - 1, col - 1]) / (2 * dx * Math.Sqrt(2)); // averaged over two cells, diagonally
                            additionalslope = (Block.Accumulated_creep_m_3 - Block.Accumulated_creep_m_7) / Block.Size_m;
                            totalslope = averageslope + additionalslope;
                            if (totalslope > 1)
                            {
                                block_has_rolled = true;
                                Block.Y_row += Convert.ToSingle(1.4142135 * Block.Size_m);
                                Block.X_col += Convert.ToSingle(1.4142135 * Block.Size_m);
                                Block.Accumulated_creep_m_3 = 0;
                                Block.Accumulated_creep_m_7 = 0;
                                //Debug.WriteLine(" block rolled");
                                topoconttoroll += Math.Abs(averageslope);
                                creepconttoroll += Math.Abs(additionalslope);
                                if (Math.Floor(Block.Y_row) == -1 | Math.Floor(Block.X_col) == -1) { Blocklist.Remove(Block); }
                            }
                            if (totalslope < -1)
                            {
                                block_has_rolled = true;
                                Block.Y_row -= Convert.ToSingle(1.4142135 * Block.Size_m);
                                Block.X_col -= Convert.ToSingle(1.4142135 * Block.Size_m);
                                Block.Accumulated_creep_m_3 = 0;
                                Block.Accumulated_creep_m_7 = 0;
                                //Debug.WriteLine(" block rolled");
                                topoconttoroll += Math.Abs(averageslope);
                                creepconttoroll += Math.Abs(additionalslope);
                                if (Math.Floor(Block.Y_row) == nr | Math.Floor(Block.X_col) == nr) { Blocklist.Remove(Block); }
                            }
                        }
                        if (block_has_rolled == false)
                        {
                            //find steepest lower nb, calculate creep to it, calculate distance from kg -> m3 -> 
                        }
                        if (block_has_rolled == true) { blocksrolled++; }
                    }
                }
            }
            else
            {
                //Debug.WriteLine(" currently no blocks to move ");
            }
            Blocklist.RemoveAll(x => removelist.Contains(x));
            //Debug.WriteLine(" removed " + removelist.Count + " blocks from list, leaving blockcount now " + Blocklist.Count);
        }

        #endregion

    }
}
