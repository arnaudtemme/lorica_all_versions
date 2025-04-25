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

        #region depression code

        void findsinks()
        {
            /*Task.Factory.StartNew(() =>
            {
                this.InfoStatusPanel.Text = "findtrouble has been entered";
            }, CancellationToken.None, TaskCreationOptions.None, guiThread); */
            int number, twoequals = 0, threeequals = 0, moreequals = 0;
            int[] intoutlet = new int[9];
            int x;
            numsinks = 0;
            int row, col;

            if (Proglacial_checkbox.Checked)  //AleG
            {

                if (t == 0) 
                {
                    dtm_WE = og_dtm;
                }
                else
                {
                    dtm_WE = filled_dtm;
                }
            }
            else
            {
                dtm_WE = dtm;
            }

            for (row = 0; row < nr; row++)
            {        //visit all cells in the DEM and  ...
                for (col = 0; col < nc; col++)
                {
                    if (dtm[row, col] != nodata_value)
                    {
                        dh = 0.0; high = 0; low = 0; equal = 0; status_map[row, col] = 0; number = 0;
                        for (x = 0; x < 9; x++) { intoutlet[x] = 99; }
                        for (i = (-1); i <= 1; i++)
                        {       //make a circle around every cell  ...
                            for (j = (-1); j <= 1; j++)
                            {
                                if (((row + i) >= 0) && ((row + i) < nr) && ((col + j) >= 0) && ((col + j) < nc) && !((i == 0) && (j == 0)) && dtm[row + i, col + j] != nodata_value)
                                { //boundaries of grid
                                    number++;
                                    dh = dtm[row, col] - dtm[row + i, col + j];
                                    if (dh < 0) { high++; intoutlet[number - 1] = -1; }     // add one to the higher-nbour-counter and add 'h' to the outlet-string
                                    if (dh > 0) { low++; intoutlet[number - 1] = 1; }       // add one to the lower-nbour-counter and add 'l' to the outlet-string
                                    if (dh == 0) { equal++; intoutlet[number - 1] = 0; }      // add one to the equal-nbour-counter and add 'e' to the outlet-string
                                                                                              //Debug.WriteLine("cell %d %d, alt=%.2f nb %d %d, alt=%.2f dh %.3f low %d, equal %d, high %d\n",row,col,dtm[row,col],row+i,col+j,dtm[row+i,col+j],dh,low,equal,high); 
                                }  //end if within boundaries
                            }  // end for j
                        } // end for i, we have considered the circle around the cell and counted higher, equal and, therefore, lower neighbours

                        if (low == 0 && intoutlet[7] != 99) { status_map[row, col] = 1; numsinks++; }       // if 0 lower cells cells are present and 8 cells in total, we have a sink
                        if (low == 8) { status_map[row, col] = -1; }       // if 8 lower cells are present, we have a top
                        if (equal == 1) { twoequals++; }        // this case is rare in real DEMs
                        if (equal == 2) { threeequals++; }      // this case is very rare in real DEMs
                        if (equal > 2) { moreequals++; }        // this case is extremely rare in real DEMs
                    } //end for nodata
                    else
                    {
                        status_map[row, col] = 3;
                    }
                }   // end for col
            }  // end for row

            /*
            last but not least we give status_map a 3 for all cells on the edge of the DEM, so we can end formation of depressions there
            */

            for (col = 0; col < nc; col++)
            {
                status_map[0, col] = 3; status_map[nr - 1, col] = 3;
            }
            for (row = 0; row < nr; row++)
            {
                status_map[row, 0] = 3; status_map[row, nc - 1] = 3;
            }

            //reports

            //this.InfoStatusPanel.Text = "found " + numsinks + " true sinks in " + nr * nc + "  cells";
            //Debug.WriteLine("\n\n--sinks overview at t = " + t + "--");

            //if (numsinks / (nr * nc) > 0.0075) { Debug.WriteLine("this DEM contains " + numsinks + " true sinks in " + nr * nc + "  cells\n That's a lot!"); }
            //else { Debug.WriteLine("t" + t + " this DEM contains " + numsinks + " true sinks in " + nr * nc + "  cells"); }
            //Debug.WriteLine(" equals: " + twoequals / 2 + " double, " + threeequals / 3 + " triple and about " + moreequals + " larger\n");

        }

        void searchdepressions()
        {
            int z;
            //this.InfoStatusPanel.Text = "searchdepressions has been entered";
            for (int row = 0; row < nr; row++)
            {        //visit all cells in the DEM and  ...
                for (int col = 0; col < nc; col++)
                {
                    depression[row, col] = 0;     // set depression to zero
                }
            }

            for (z = 0; z < numberofsinks; z++)
            {         // the maximum number of depressions is the number of sinks
                drainingoutlet_row[z, 0] = -1;
                drainingoutlet_col[z, 0] = -1;
                drainingoutlet_row[z, 1] = -1;
                drainingoutlet_col[z, 1] = -1;
                drainingoutlet_row[z, 2] = -1;
                drainingoutlet_col[z, 2] = -1;
                drainingoutlet_row[z, 3] = -1;
                drainingoutlet_col[z, 3] = -1;
                drainingoutlet_row[z, 4] = -1;
                drainingoutlet_col[z, 4] = -1;
                depressionlevel[z] = 0;
                depressionsize[z] = 0;
                depressionvolume_m[z] = 0;
                iloedge[z] = 0;
                jloedge[z] = 0;
                iupedge[z] = 0;
                jupedge[z] = 0;
            }

            if (Proglacial_checkbox.Checked)  
            {
                if (t == 0)
                {
                    dtm_WE = og_dtm;
                }
                else
                {
                    dtm_WE = filled_dtm;
                }
            }
            else
            {
                dtm_WE = dtm;
            }

            totaldepressions = 0; totaldepressionsize = 0; maxsize = 0; totaldepressionvolume = 0; largestdepression = -1;
            depressionnumber = 0;
            for (int row = 0; row < nr; row++)
            {        //visit all cells in the DEM and  ...
                for (int col = 0; col < nc; col++)
                {
                    if (status_map[row, col] == 1 && depression[row, col] == 0)
                    {   // sink  -NODATA cells are never sinks, no need to exclude them explicitly here
                        numberoflowestneighbours = 0;
                        for (lowestneighbourcounter = 0; lowestneighbourcounter < maxlowestnbs; lowestneighbourcounter++)
                        {
                            rowlowestnb[lowestneighbourcounter] = -1;
                            collowestnb[lowestneighbourcounter] = -1;
                        }
                        depressionnumber++;                 // so depressionnumber 0 is not used, neither is depression[r,c] = 0
                        if (depressionnumber == 1153000) { diagnostic_mode = 1; } else { diagnostic_mode = 0; }
                        //Debug.WriteLine(" depressionvolume of depression " + depressionnumber + " is initially " + depressionvolume[depressionnumber]); 
                        totaldepressions++;
                        depressionlevel[depressionnumber] = dtm[row, col];
                        iloedge[depressionnumber] = row - 1;
                        iupedge[depressionnumber] = row + 1;
                        jloedge[depressionnumber] = col - 1;
                        jupedge[depressionnumber] = col + 1;
                        if (diagnostic_mode == 1)
                        {
                            Debug.WriteLine(" Sink " + depressionnumber + " located: " + row + "," + col + " alt " + dtm[row, col]);
                            Debug.WriteLine(" edges : " + iloedge[depressionnumber] + ", " + iupedge[depressionnumber] + ", " + jloedge[depressionnumber] + ", " + jupedge[depressionnumber]);
                        }
                        depressionsize[depressionnumber] = 1;
                        depression[row, col] = depressionnumber;
                        if (depression[row, col] < 0)
                        {
                            MessageBox.Show("Depression error at row " + row + " col " + col + " dep " + depression[row, col]);
                        }
                        iupradius = 1; jupradius = 1; iloradius = 1; jloradius = 1;
                        depressionready = 0; depressiondrainsout = 0;
                        while (depressionready != 1)
                        {
                            if (depressionnumber == 1153000) { diagnostic_mode = 1; }
                            minaltidiff = large_negative_number; int already_lower_than_lakelevel = 0;
                            for (i = (-1 * iloradius); i <= iupradius; i++)
                            {       //make a circle around the current cell that is so large that it covers all neighbours of all cells currently in the depression
                                for (j = (-1 * jloradius); j <= jupradius; j++)
                                {
                                    if (((row + i) >= 0) && ((row + i) < nr) && ((col + j) >= 0) && ((col + j) < nc) && !((i == 0) && (j == 0))) // in this case, we DO want to find cells with NODATA, because these are valid end-points of lakes
                                    {
                                        //if (diagnostic_mode == 1) { Debug.WriteLine("now at " + (row + i) + ", " + (col + j)); }
                                        nbismemberofdepression = 0;
                                        for (alpha = (-1); alpha <= 1; alpha++)
                                        {       // check in circle around the current cell if one of its neighbours is member of the depression
                                            for (beta = (-1); beta <= 1; beta++)
                                            {
                                                if (((row + i + alpha) >= 0) && ((row + i + alpha) < nr) && ((col + j + beta) >= 0) && ((col + j + beta) < nc) && !((alpha == 0) && (beta == 0)))
                                                {
                                                    if (depression[row + i + alpha, col + j + beta] == depressionnumber)
                                                    {   //and only if the cell is an actual neighbour of a cell that is member of the depression
                                                        nbismemberofdepression = 1;
                                                    } // end if nb = member of depression
                                                } // end if boundary
                                            }
                                        } // end for first alpha-beta circle to see if any nb = member of depression. It could have been no neighbour of the current depression but still within ilorarius, iupradius etc.
                                        if (nbismemberofdepression == 1 && depression[row + i, col + j] != depressionnumber)
                                        {   // only in case cell row+i, col+j  has a nb that is already member, we are interested in its altitude diff with depressionlevel
                                            altidiff = depressionlevel[depressionnumber] - dtm[row + i, col + j];
                                            if (diagnostic_mode == 1)
                                            {
                                                Debug.WriteLine((row + i) + ", " + (col + j) + " is neighbour of depression , altidifference " + altidiff);
                                            }
                                            if (altidiff == minaltidiff)
                                            {   //if lowest higher nb = equally high as previous lowest higher nb
                                                lowestneighbourcounter++; numberoflowestneighbours++;
                                                if (numberoflowestneighbours == maxlowestnbs) { Debug.WriteLine(" WARNING: the setting for maximum number of lowest neighbours is " + numberoflowestneighbours + " lowest nbs" + maxlowestnbs); }
                                                rowlowestnb[lowestneighbourcounter] = (row + i);
                                                collowestnb[lowestneighbourcounter] = (col + j);
                                                // in this way, we can add all equally high lowest higher neighbours to the current depression (maximum = maxlowestnbs)
                                            } //end if higher neighbour, equal as before
                                            if (altidiff > minaltidiff || dtm[row + i, col + j] < depressionlevel[depressionnumber])  // this INCLUDES nodata cells bordering the lake!!
                                            {   //het hoogteverschil met deze buur telt alleen als minder hoog dan vorige buren OF lager dan meerniveau 
                                                minaltidiff = altidiff;
                                                if (dtm[row + i, col + j] < depressionlevel[depressionnumber] && already_lower_than_lakelevel == 1)
                                                {  // the new lowest neighbour is lower than lakelevel!! 
                                                   // We want to know all lowest nbs that are lower than lakelevel, so we do not zero the rowlowestnb 
                                                   // and colllowestnb arrays
                                                    lowestneighbourcounter++; numberoflowestneighbours++;
                                                    rowlowestnb[lowestneighbourcounter] = (row + i);
                                                    collowestnb[lowestneighbourcounter] = (col + j);
                                                    if (diagnostic_mode == 1) { Debug.WriteLine(" found another neighbour that is lower than lakelevel "); }
                                                    if (diagnostic_mode == 1) { Debug.WriteLine(" lower neighbour that is lower than lakelevel: " + rowlowestnb[lowestneighbourcounter] + ", " + collowestnb[lowestneighbourcounter] + " , " + dtm[rowlowestnb[lowestneighbourcounter], collowestnb[lowestneighbourcounter]]); }
                                                }
                                                else  // the new lowest neighbour is higher than lakelevel or is the first lowest nb that is lower than lakelevel
                                                {
                                                    if (dtm[row + i, col + j] < depressionlevel[depressionnumber]) { already_lower_than_lakelevel = 1; }
                                                    for (lowestneighbourcounter = 0; lowestneighbourcounter < maxlowestnbs; lowestneighbourcounter++)
                                                    {
                                                        rowlowestnb[lowestneighbourcounter] = -1; collowestnb[lowestneighbourcounter] = -1;
                                                    } //end:  for all lowestneighbours that we had before, the rowlowestnb and collowestnb arrays have been zeroed
                                                    lowestneighbourcounter = 0; numberoflowestneighbours = 1;
                                                    rowlowestnb[0] = (row + i); collowestnb[0] = (col + j);
                                                    if (diagnostic_mode == 1) { Debug.WriteLine(" higher neighbour that is lower than previous higher nbs: " + rowlowestnb[0] + ", " + collowestnb[0] + " , " + dtm[rowlowestnb[0], collowestnb[0]]); }
                                                }
                                            } //end if higher neighbour but lower than before
                                        } //end if nbismemberofdepression

                                    } // end if boundary
                                }
                            } // double end for circle with possibly extended radius around the sink, alpha - beta circle . We now know what is/are the lowest higher neighbours

                            if (diagnostic_mode == 1)
                            {
                                for (lowestneighbourcounter = 0; lowestneighbourcounter < numberoflowestneighbours; lowestneighbourcounter++)
                                {
                                    Debug.WriteLine(rowlowestnb[lowestneighbourcounter] + " " + collowestnb[lowestneighbourcounter] + " is one of the " + numberoflowestneighbours + " lowest neighbours of depression " + depressionnumber + ". Its altitude is " + dtm[rowlowestnb[lowestneighbourcounter], collowestnb[lowestneighbourcounter]]);
                                }
                            }

                            int outletfound = 0; int numberofoutlets = 0; int outletnumber = 0;

                            for (lowestneighbourcounter = 0; lowestneighbourcounter < numberoflowestneighbours; lowestneighbourcounter++)
                            {
                                if (rowlowestnb[lowestneighbourcounter] >= iupedge[depressionnumber]) { iupedge[depressionnumber] = rowlowestnb[lowestneighbourcounter] + 1; }
                                if (rowlowestnb[lowestneighbourcounter] <= iloedge[depressionnumber]) { iloedge[depressionnumber] = rowlowestnb[lowestneighbourcounter] - 1; }
                                if (collowestnb[lowestneighbourcounter] >= jupedge[depressionnumber]) { jupedge[depressionnumber] = collowestnb[lowestneighbourcounter] + 1; }
                                if (collowestnb[lowestneighbourcounter] <= jloedge[depressionnumber]) { jloedge[depressionnumber] = collowestnb[lowestneighbourcounter] - 1; }
                                if (diagnostic_mode == 1) { Debug.WriteLine(" minaltidiff = " + minaltidiff + "; depression " + depressionnumber + " row " + rowlowestnb[lowestneighbourcounter] + " col " + collowestnb[lowestneighbourcounter] + " alt " + dtm[rowlowestnb[lowestneighbourcounter], collowestnb[lowestneighbourcounter]] + " is lower than depressionlevel " + depressionlevel[depressionnumber]); }

                                if (minaltidiff <= 0.0)
                                { // if the cell is higher than depressionlevel
                                  // it can either be a cell of another depression

                                    if (diagnostic_mode == 1) { Debug.WriteLine(rowlowestnb[lowestneighbourcounter] + "," + collowestnb[lowestneighbourcounter] + " is member of depression " + depression[rowlowestnb[lowestneighbourcounter], collowestnb[lowestneighbourcounter]]); }
                                    if (depression[rowlowestnb[lowestneighbourcounter], collowestnb[lowestneighbourcounter]] != 0 && depression[rowlowestnb[lowestneighbourcounter], collowestnb[lowestneighbourcounter]] != depressionnumber)
                                    {  // then we have touched upon a depression that was previously analysed
                                       //status_map[rowlowestnb[lowestneighbourcounter],collowestnb[lowestneighbourcounter]] = 0;
                                        otherdepressionsize = 0;
                                        otherdepression = depression[rowlowestnb[lowestneighbourcounter], collowestnb[lowestneighbourcounter]];
                                        if (t > 1000000) { diagnostic_mode = 1; }
                                        totaldepressions--;
                                        totaldepressionvolume -= depressionvolume_m[otherdepression];
                                        for (int outletcounter = 0; outletcounter < 5; outletcounter++)
                                        {
                                            drainingoutlet_row[otherdepression, outletcounter] = -1;
                                            drainingoutlet_col[otherdepression, outletcounter] = -1;
                                        }
                                        depressionvolume_m[depressionnumber] += depressionvolume_m[otherdepression];
                                        //if (diagnostic_mode == 1) { Debug.WriteLine(" depressionvolume of depression " + depressionnumber + " was increased to " + depressionvolume[depressionnumber] + " with " + depressionvolume[otherdepression] + " of depression " + otherdepression); }
                                        depressionvolume_m[depressionnumber] += (depressionsize[depressionnumber] * (dtm[rowlowestnb[lowestneighbourcounter], collowestnb[lowestneighbourcounter]] - depressionlevel[depressionnumber]));  //
                                                                                                                                                                                                                                           //if (diagnostic_mode == 1) { Debug.WriteLine(" added " + depressionsize[depressionnumber] * (dtm[rowlowestnb[lowestneighbourcounter], collowestnb[lowestneighbourcounter]] - depressionlevel[depressionnumber]) + " to depressionvolume of depression " + depressionnumber + ". Dtm " + dtm[rowlowestnb[lowestneighbourcounter], collowestnb[lowestneighbourcounter]] + ", depressionlevel " + depressionlevel[depressionnumber]); }
                                        depressionvolume_m[otherdepression] = 0;
                                        depressionlevel[depressionnumber] = dtm[rowlowestnb[lowestneighbourcounter], collowestnb[lowestneighbourcounter]];
                                        //if (diagnostic_mode == 1) { Debug.WriteLine(" first: ilo = " + iloradius + " , iup = " + iupradius + " , jlo = " + jloradius + " , jup = " + jupradius + "  around sink " + row + " " + col); }
                                        //if (diagnostic_mode == 1) { Debug.WriteLine(" depression " + otherdepression + " : iloedge " + iloedge[otherdepression] + " , iupedge " + iupedge[otherdepression] + " , jloedge " + jloedge[otherdepression] + " , jupedge " + jupedge[otherdepression]); }
                                        if (jloedge[otherdepression] < (col - jloradius)) { jloradius = Math.Abs(jloedge[otherdepression] - col) + 1; jloedge[depressionnumber] = col - jloradius; }     // we enlarge the area around the sink of the current depression that is to be checked
                                        if (iloedge[otherdepression] < (row - iloradius)) { iloradius = Math.Abs(iloedge[otherdepression] - row) + 1; iloedge[depressionnumber] = row - iloradius; }     // it now includes the complete area of the depression that has been touched upon
                                        if (jupedge[otherdepression] > (col + jupradius)) { jupradius = Math.Abs(jupedge[otherdepression] - col) + 1; jupedge[depressionnumber] = col + jupradius; }
                                        if (iupedge[otherdepression] > (row + iupradius)) { iupradius = Math.Abs(iupedge[otherdepression] - row) + 1; iupedge[depressionnumber] = row + iupradius; }
                                        //if (diagnostic_mode == 1) { Debug.WriteLine(" now: ilo = " + iloradius + " , iup = " + iupradius + " , jlo = " + jloradius + " , jup = " + jupradius + " around sink " + row + "  " + col); }
                                        //if (diagnostic_mode == 1) { Debug.WriteLine(" begonnen met gebied om sink om cellen uit ander meer om te nummeren"); }
                                        for (alpha = (-1 * iloradius); alpha <= iupradius; alpha++)
                                        {       // move around in this square and change depressionnumber
                                            for (beta = (-1 * jloradius); beta <= jupradius; beta++)
                                            {
                                                if (((row + alpha) >= 0) && ((row + alpha) < nr) &&   // insofar that the circle is within the boundaries, excluding the centre cell itself
                                                    ((col + beta) >= 0) && ((col + beta) < nc) && !((alpha == 0) && (beta == 0)) && dtm[row + alpha, col + beta] != nodata_value)
                                                {
                                                    if (depression[row + alpha, col + beta] == otherdepression)
                                                    {
                                                        depression[row + alpha, col + beta] = depressionnumber;
                                                        otherdepressionsize++;
                                                        if (diagnostic_mode == 1) { Debug.WriteLine(" moved cell " + (row + alpha) + " , " + (col + beta) + "(" + dtm[row + alpha, col + beta] + ") from depression " + otherdepression + " to depression " + depressionnumber + " "); }
                                                    } // end if cell belonged to other depression
                                                }
                                            }
                                        } // double end for second alpha-beta square : around the sink to change depressionnumber of previously checked depression
                                        if (diagnostic_mode == 1) { Debug.WriteLine(" All " + otherdepressionsize + "  cells of depression " + otherdepression + "  were added to depression " + depressionnumber + "  (prvsly " + depressionsize[depressionnumber] + " ), level now " + depressionlevel[depressionnumber]); }
                                        depressionsize[depressionnumber] += otherdepressionsize;
                                        depressionsize[otherdepression] = 0;
                                        totaldepressionsize -= otherdepressionsize;
                                        if (diagnostic_mode == 1) { Debug.WriteLine("B totaldepressionsize " + totaldepressionsize + " , otherdepressionnsize " + otherdepression + "  = " + otherdepressionsize + "  depressionnumber " + depressionnumber + " "); }

                                        // it is theoretically possible that the outlet that connects the 'depression' and the 'otherdepression' , drains to a third side. In that case, the new, combined depression
                                        // should be declared ready after added 'otherdepression' to 'depression' ...

                                        for (alpha = (-1); alpha <= 1; alpha++)
                                        {
                                            for (beta = -1; beta <= 1; beta++)
                                            {
                                                if (((rowlowestnb[lowestneighbourcounter] + alpha) >= 0) && ((rowlowestnb[lowestneighbourcounter] + alpha) < nr) &&
                                                    ((collowestnb[lowestneighbourcounter] + beta) >= 0) && ((collowestnb[lowestneighbourcounter] + beta) < nc) &&
                                                    !((alpha == 0) && (beta == 0)) && dtm[rowlowestnb[lowestneighbourcounter] + alpha, collowestnb[lowestneighbourcounter] + beta] != nodata_value)
                                                {  // insofar that the circle is within the boundaries, excluding the centre cell itself
                                                    if (depression[rowlowestnb[lowestneighbourcounter] + alpha, collowestnb[lowestneighbourcounter] + beta] != depressionnumber)
                                                    {
                                                        if (dtm[rowlowestnb[lowestneighbourcounter] + alpha, collowestnb[lowestneighbourcounter] + beta] < depressionlevel[depressionnumber])
                                                        {
                                                            depressionready = 1;
                                                            drainingoutlet_row[depressionnumber, 0] = rowlowestnb[lowestneighbourcounter];
                                                            drainingoutlet_col[depressionnumber, 0] = collowestnb[lowestneighbourcounter];
                                                            status_map[rowlowestnb[lowestneighbourcounter], collowestnb[lowestneighbourcounter]] = 2;
                                                            if (diagnostic_mode == 1)
                                                            {
                                                                Debug.WriteLine(" depression " + depressionnumber + " drains to a third side and is ready ");
                                                                //displayonscreen(rowlowestnb[lowestneighbourcounter] + alpha, collowestnb[lowestneighbourcounter] + beta);
                                                            }
                                                        } // end if lower than outlet = depressionlevel
                                                    }  // end if depression != depression
                                                } // end if boundaries
                                            } // end for beta
                                        } // end for alpha
                                    }  // end if touched another depression with lowest nb

                                    //or it can be any other non-depression cell

                                    else
                                    {      // so we did not touch another depression with our lowest nb , but it was a higher or equally high nb so the depression is not yet ready
                                        if (diagnostic_mode == 1) { Debug.WriteLine(" this lowest neighbour: second option: no depression "); }
                                        depression[rowlowestnb[lowestneighbourcounter], collowestnb[lowestneighbourcounter]] = depressionnumber;
                                        if (diagnostic_mode == 1) { Debug.WriteLine(" depressionvolume of depression " + depressionnumber + " is " + depressionvolume_m[depressionnumber]); }
                                        depressionvolume_m[depressionnumber] += (depressionsize[depressionnumber] * (dtm[rowlowestnb[lowestneighbourcounter], collowestnb[lowestneighbourcounter]] - depressionlevel[depressionnumber]));  // add the amount of water added to the surface already part of depression
                                        if (diagnostic_mode == 1) { Debug.WriteLine(" added " + (depressionsize[depressionnumber] * (dtm[rowlowestnb[lowestneighbourcounter], collowestnb[lowestneighbourcounter]] - depressionlevel[depressionnumber])) + " to depressionvolume of depression " + depressionnumber + ". depressionsize: " + depressionsize[depressionnumber] + ", dtm %6.6f, depressionlevel " + dtm[rowlowestnb[lowestneighbourcounter], collowestnb[lowestneighbourcounter]], depressionlevel[depressionnumber]); }
                                        depressionsize[depressionnumber]++;
                                        depressionlevel[depressionnumber] = dtm[rowlowestnb[lowestneighbourcounter], collowestnb[lowestneighbourcounter]];
                                        if (diagnostic_mode == 1) { Debug.WriteLine(" added " + rowlowestnb[lowestneighbourcounter] + ", " + collowestnb[lowestneighbourcounter] + "  with level " + dtm[rowlowestnb[lowestneighbourcounter], collowestnb[lowestneighbourcounter]] + " to depression " + depressionnumber + " , size now " + depressionsize[depressionnumber] + " "); }
                                        if (diagnostic_mode == 1) { Debug.WriteLine(" lowest neighbour of depression " + depressionnumber + "  is " + rowlowestnb[lowestneighbourcounter] + " , " + collowestnb[lowestneighbourcounter] + " ," + depressionlevel[depressionnumber] + "  "); }
                                        if (diagnostic_mode == 1) { Debug.WriteLine(" depressionlevel for this depression is currently: " + depressionlevel[depressionnumber]); }  // laagste buur is nog niet betrokken bij een ander meer

                                        if (status_map[rowlowestnb[lowestneighbourcounter], collowestnb[lowestneighbourcounter]] == 3)
                                        { // then this depression drains out of the DEM...
                                            depressionready = 1;
                                            if (diagnostic_mode == 1) { Debug.WriteLine(" depression " + depressionnumber + " drains out of the DEM and is ready "); }
                                            depressiondrainsout = 1;
                                            drainingoutlet_row[depressionnumber, 0] = rowlowestnb[lowestneighbourcounter];
                                            drainingoutlet_col[depressionnumber, 0] = collowestnb[lowestneighbourcounter];
                                        }
                                        else
                                        { // depression does not drain out of DEM
                                            if ((rowlowestnb[lowestneighbourcounter] - row) == iupradius) { iupradius++; }     //in that case we will now change searchradius
                                            if ((row - rowlowestnb[lowestneighbourcounter]) == iloradius) { iloradius++; }
                                            if ((collowestnb[lowestneighbourcounter] - col) == jupradius) { jupradius++; }
                                            if ((col - collowestnb[lowestneighbourcounter]) == jloradius) { jloradius++; }
                                        } // end else
                                    } //end else
                                } // end if cell was higher than depressionlevel

                                else
                                {  // apparently it was lower than depressionlevel
                                   //Debug.WriteLine(" found lower neighbour " + rowlowestnb[lowestneighbourcounter] + "  " + collowestnb[lowestneighbourcounter] + "  alt " + dtm[rowlowestnb[lowestneighbourcounter], collowestnb[lowestneighbourcounter]] + "  for depression " + depressionnumber + "   level " + depressionlevel[depressionnumber] + " "); 
                                    outletfound = 1;
                                    // find for this cell, that should not be part of the depression, the necessarily present depression nb @ depressionlevel and call it a outlet
                                    for (alpha = (-1); alpha <= 1; alpha++)
                                    {
                                        for (beta = -1; beta <= 1; beta++)
                                        {
                                            if (((rowlowestnb[lowestneighbourcounter] + alpha) >= 0) && ((rowlowestnb[lowestneighbourcounter] + alpha) < nr) &&
                                                    ((collowestnb[lowestneighbourcounter] + beta) >= 0) && ((collowestnb[lowestneighbourcounter] + beta) < nc) &&
                                                    !((alpha == 0) && (beta == 0)) && dtm[rowlowestnb[lowestneighbourcounter] + alpha, collowestnb[lowestneighbourcounter] + beta] != nodata_value)
                                            {  // insofar that the circle is within the boundaries, excluding the centre cell itself
                                                if (depression[rowlowestnb[lowestneighbourcounter] + alpha, collowestnb[lowestneighbourcounter] + beta] == depressionnumber)
                                                {
                                                    if (dtm[rowlowestnb[lowestneighbourcounter] + alpha, collowestnb[lowestneighbourcounter] + beta] == depressionlevel[depressionnumber])
                                                    {
                                                        //Debug.WriteLine(" At lower cell  " + rowlowestnb[lowestneighbourcounter] + " " + collowestnb[lowestneighbourcounter] + ", looking back at " + (rowlowestnb[lowestneighbourcounter] + alpha) + " " + (collowestnb[lowestneighbourcounter] + beta));
                                                        if (drainingoutlet_col[depressionnumber, outletnumber] != -1) //ArT is outletnumber put to zero before here somewhere?
                                                        {
                                                            // Then we found an earlier outlet (somewhere), and we have to define an extra one here, IF that earlier one was not this one.      
                                                            bool this_is_an_earlier_outlet = false;
                                                            for (int outletcounter = 0; outletcounter < 5; outletcounter++)
                                                            {
                                                                if (rowlowestnb[lowestneighbourcounter] + alpha == drainingoutlet_row[depressionnumber, outletcounter] && collowestnb[lowestneighbourcounter] + beta == drainingoutlet_col[depressionnumber, outletcounter])
                                                                {
                                                                    this_is_an_earlier_outlet = true;
                                                                    //Debug.WriteLine(" A prior outlet definition for depression " + depressionnumber + " exists at " + drainingoutlet_row[depressionnumber, outletcounter] + " " + drainingoutlet_col[depressionnumber, outletcounter] + " (" + (lowestneighbourcounter + 1) + "/" + numberoflowestneighbours + ")");
                                                                }
                                                            }
                                                            if (this_is_an_earlier_outlet == false)
                                                            {
                                                                //Debug.WriteLine(" A new outlet was found for depression " + depressionnumber + " at " + (rowlowestnb[lowestneighbourcounter] + alpha) + " " + (collowestnb[lowestneighbourcounter] + beta) + " (" + (lowestneighbourcounter + 1) + "/" + numberoflowestneighbours + ")");
                                                                outletnumber++;
                                                                numberofoutlets++;
                                                                if (outletnumber > 4)
                                                                {
                                                                    //displayonscreen(drainingoutlet_row[depressionnumber, 1], drainingoutlet_col[depressionnumber, 1]);
                                                                    if (diagnostic_mode == 1) { Debug.WriteLine(" Warning: LORICA seeks to define more than five outlets for depression " + depressionnumber + ". This request is denied"); }
                                                                    outletnumber--; numberofoutlets--;
                                                                }
                                                                else
                                                                {
                                                                    drainingoutlet_row[depressionnumber, outletnumber] = rowlowestnb[lowestneighbourcounter] + alpha;
                                                                    drainingoutlet_col[depressionnumber, outletnumber] = collowestnb[lowestneighbourcounter] + beta;
                                                                    status_map[rowlowestnb[lowestneighbourcounter] + alpha, collowestnb[lowestneighbourcounter] + beta] = 2;
                                                                    //Debug.WriteLine(" made " + (rowlowestnb[lowestneighbourcounter] + alpha) + "  " + (collowestnb[lowestneighbourcounter] + beta) + "  the nr " + outletnumber + " outlet for depression " + depressionnumber + " ");
                                                                }
                                                            }

                                                        }
                                                        else  // then this is the first outlet, and we will define it as such
                                                        {
                                                            //Debug.WriteLine(" First definition of outlet for depression " + depressionnumber + " at " + (rowlowestnb[lowestneighbourcounter] + alpha) + " " + (collowestnb[lowestneighbourcounter] + beta) + " (" + (lowestneighbourcounter+1) + "/" + numberoflowestneighbours + ")");
                                                            if (outletnumber > 4)
                                                            {
                                                                //displayonscreen(drainingoutlet_row[depressionnumber, 1], drainingoutlet_col[depressionnumber, 1]);
                                                                if (diagnostic_mode == 1) { Debug.WriteLine(" Warning: LORICA seeks to define more than five outlets for depression " + depressionnumber + ". This request is denied"); }
                                                                outletnumber--; numberofoutlets--;
                                                            }
                                                            else
                                                            {
                                                                drainingoutlet_row[depressionnumber, outletnumber] = rowlowestnb[lowestneighbourcounter] + alpha;
                                                                drainingoutlet_col[depressionnumber, outletnumber] = collowestnb[lowestneighbourcounter] + beta;
                                                                status_map[rowlowestnb[lowestneighbourcounter] + alpha, collowestnb[lowestneighbourcounter] + beta] = 2;
                                                                //Debug.WriteLine(" made " + (rowlowestnb[lowestneighbourcounter] + alpha) + "  " + (collowestnb[lowestneighbourcounter] + beta) + "  the nr " + outletnumber + " outlet for depression " + depressionnumber + " ");
                                                            }
                                                        }

                                                    } // end if dtm = depressionlevel
                                                } // end if depression = depressionnumber
                                            } // end if bnd
                                        } // end for beta
                                    } // end for alpha
                                } // end else
                            } //end for all lowestneighbours
                              // if outlet(s) defined ==> depressionready
                            if (outletfound == 1) { depressionready = 1; }
                        } // end while depressionready != 1

                        //if (depressionnumber == 180) {diagnostic_mode = 1;}
                        if (diagnostic_mode == 1)
                        {
                            Debug.WriteLine(" depression " + depressionnumber + "  (" + depressionlevel[depressionnumber] + "  m) is ready and contains " + depressionsize[depressionnumber] + "  cells. Volume = " + depressionvolume_m[depressionnumber] + " ");
                            if (depressiondrainsout == 1) { Debug.WriteLine(" depression " + depressionnumber + "  (" + depressionlevel[depressionnumber] + "  m) drains outside the DEM "); }
                            minimaps(row, col);
                        }
                        totaldepressionsize += depressionsize[depressionnumber];
                        totaldepressionvolume += depressionvolume_m[depressionnumber];
                        if (maxsize < depressionsize[depressionnumber]) { maxsize = depressionsize[depressionnumber]; largestdepression = depressionnumber; }
                        if (maxdepressionnumber < depressionnumber) { maxdepressionnumber = depressionnumber; }
                        if (diagnostic_mode == 1)
                        {
                            Debug.WriteLine(" Defined depression " + depressionnumber + ". Now size " + depressionsize[depressionnumber] + " and volume " + depressionvolume_m[depressionnumber]);
                        }
                    } // end if sink
                      //Debug.WriteLine("now at row " + row + " and col " + col);
                }  // end for  col
            } // end for   row
            /*
            Debug.WriteLine("\n\n--depressions overview--");
            if (totaldepressions != 0)
            {
                Debug.WriteLine("found " + totaldepressions + "  depressions containing " + totaldepressionsize + "  cells, with a volume of " + totaldepressionvolume);
                Debug.WriteLine(" " + totaldepressions + " depressions with a volume of " + totaldepressionvolume);
                //Debug.WriteLine("depression " + largestdepression + "  is largest by area: " + maxsize + " cells " + depressionlevel[largestdepression] + " m " + depressionvolume[largestdepression] + "m3");
                //if (depressionvolume[largestdepression] < 0) { Debug.WriteLine("negative depressionvolume found"); }
            }
            else
            {
                Debug.WriteLine(" no depressions found ");

            } */
            //out_integer("lakes.asc",depression);
        }

        void define_fillheight_new()  //calculates where depressions must be filled how high
        {
            // when completely filling a depression, we need to know - for each constituent cell and even for its neighbours - the altitude we
            // can fill it to. Since the depression must still drain towards the outlet, we add a very small value
            // to membercells so they drain towards the outlet.
            // we cannot simply use distance_to_outlet for each member cell, since depressions can round corners....

            //this.InfoStatusPanel.Text = "def fillheight has been entered";
            //Debug.WriteLine("defining fillheight\n");
            int notyetdone, done, depressiontt;

            once_dtm_fill = 0;

            for (int row = 0; row < nr; row++)
            {
                for (int col = 0; col < nc; col++)
                {
                    dtmfill_A[row, col] = -1;
                } //for
            } //for

            depressiontt = 0;
            for (depressiontt = 1; depressiontt <= maxdepressionnumber; depressiontt++)
            {  // for all possible depressions

                if (depressionsize[depressiontt] > 0)
                {      // if they exist, so have not been intermediate depressions
                    dtmfill_A[drainingoutlet_row[depressiontt, 0], drainingoutlet_col[depressiontt, 0]] = depressionlevel[depressiontt];
                    if (drainingoutlet_row[depressiontt, 1] != -1) { dtmfill_A[drainingoutlet_row[depressiontt, 1], drainingoutlet_col[depressiontt, 1]] = depressionlevel[depressiontt]; }
                    if (drainingoutlet_row[depressiontt, 2] != -1) { dtmfill_A[drainingoutlet_row[depressiontt, 2], drainingoutlet_col[depressiontt, 2]] = depressionlevel[depressiontt]; }
                    if (drainingoutlet_row[depressiontt, 3] != -1) { dtmfill_A[drainingoutlet_row[depressiontt, 3], drainingoutlet_col[depressiontt, 3]] = depressionlevel[depressiontt]; }
                    if (drainingoutlet_row[depressiontt, 4] != -1) { dtmfill_A[drainingoutlet_row[depressiontt, 4], drainingoutlet_col[depressiontt, 4]] = depressionlevel[depressiontt]; }

                    notyetdone = 1; done = 0;
                    while (notyetdone > 0)
                    {
                        notyetdone = 0;
                        //if (diagnostic_mode == 1) { Debug.WriteLine("depressioncells depression " + depressiontt + " size " + depressionsize[depressiontt] + " " +drainingoutlet_row[depressiontt]+ " " + drainingoutlet_col[depressiontt]); }
                        //diagnostic_mode = 0;
                        for (int row = iloedge[depressiontt]; row <= iupedge[depressiontt]; row++)
                        {
                            for (int col = jloedge[depressiontt]; col <= jupedge[depressiontt]; col++)
                            {
                                if (((row) >= 0) && ((row) < nr) && ((col) >= 0) && ((col) < nc) && dtm[row, col] != nodata_value)
                                {  //bnd
                                    if (t > 1000000) { diagnostic_mode = 1; } else { diagnostic_mode = 0; }
                                    if (diagnostic_mode == 1) { Debug.WriteLine("dtmfill_A of " + row + " " + col + " = " + dtmfill_A[row, col] + ", checking on behalf of depression " + depressiontt); }
                                    if (dtmfill_A[row, col] == -1 && depression[row, col] == depressiontt)
                                    {  // if this is a cell of the current depression that has not yet got a dtmfill
                                       // and remember that this is the only place where this cell could have gotten that DTMfill
                                        notyetdone++;       // then we are not yet ready
                                        if (diagnostic_mode == 1) { Debug.WriteLine(row + " " + col + ": notyetdone =  " + notyetdone); }
                                        //if (diagnostic_mode == 1) { displayonscreen(row, col); }
                                        for (int i = -1; i <= 1; i++)
                                        {      // go and see if it has a depression-nb that does have a dtmfill and that is lower than the previous possible dep-nb's dtmfill
                                            for (int j = -1; j <= 1; j++)
                                            {
                                                if (((row + i) >= 0) && ((row + i) < nr) && ((col + j) >= 0) && ((col + j) < nc) && !((i == 0) && (j == 0)) && dtm[row + i, col + j] != nodata_value)
                                                {  //bnd
                                                    if (depression[row + i, col + j] == depressiontt && dtmfill_A[row + i, col + j] > 0.0)
                                                    { // if it IS a depression-nb and DOES have a dtmfill
                                                        if ((i == 0 || j == 0) && (dtmfill_A[row, col] == -1 || (dtmfill_A[row, col] > dtmfill_A[row + i, col + j] + epsilon * dx)))
                                                        {
                                                            dtmfill_A[row, col] = (dtmfill_A[row + i, col + j] + epsilon * dx);
                                                        } // end if
                                                        if ((i != 0 && j != 0) && (dtmfill_A[row, col] == -1 || (dtmfill_A[row, col] > dtmfill_A[row + i, col + j] + epsilon * dx * Math.Sqrt(2))))
                                                        {
                                                            dtmfill_A[row, col] = (dtmfill_A[row + i, col + j] + epsilon * dx * Math.Sqrt(2));
                                                        } // end if
                                                    } // end if
                                                }//end if within boundaries
                                            }//end for circle (first part)
                                        }//end for circle (second part)
                                        if (dtmfill_A[row, col] > 0) { notyetdone--; done++; }
                                        //if (diagnostic_mode == 1) { Debug.WriteLine(" notyetdone =  " + notyetdone + " done = " + done); }
                                    } // end if depression
                                      //if (diagnostic_mode == 1) { Debug.WriteLine("-> " + notyetdone); }
                                } // end if bnd
                            } // end for
                        } // end for
                    } // end while
                } // end if they exist
            } //end for all possible depressions
            //Debug.WriteLine("\n--dtmfill determination finished--");
        }

        void cleardelta(int iloradius, int iupradius, int jloradius, int jupradius, int clear_row, int clear_col)   //clears a delta
        {
            int epsilon, eta;
            if (diagnostic_mode == 1) { Debug.WriteLine(" clearing delta lake " + Math.Abs(depression[clear_row, clear_col]) + " around " + clear_row + " " + clear_col); }
            for (epsilon = -(iloradius + 3); epsilon <= iupradius + 3; epsilon++)
            {
                for (eta = -(jloradius + 3); eta <= jupradius + 3; eta++)
                {
                    if (((clear_row + epsilon) >= 0) && ((clear_row + epsilon) < nr) && ((clear_col + eta) >= 0) && ((clear_col + eta) < nc))
                    { // boundaries
                        if (depression[clear_row + epsilon, clear_col + eta] < 0)
                        {
                            depression[clear_row + epsilon, clear_col + eta] = Math.Abs(depression[clear_row, clear_col]);
                            if (diagnostic_mode == 1)
                            {
                                Debug.WriteLine(" membership of delta has been cancelled for " + (clear_row + epsilon) + " " + (clear_col + eta));
                            }
                        }  // end if depression < 0
                    }   // end if boundary
                }   // end for eta
            } // end for epsilon
              //Debug.WriteLine("cleared delta\n");
        }

        void update_depression(int number)   //updates depressions when the erosion/deposition process has reached them to include cells that have been eroded to below lakelevel
        {
            /*  a.	First estimate of required sediment is fillheight  dtm for all lake cells. Test whether dz_ero_m and dz_sed_m for these cells are zero (they should be).
                b.	Starting from every lake cell, look for cells around it that are not part of the lake and had dtm above lakelevel.
                    	For such a cell, look around it for all lake-neighbours and determine the one that would yield the lowest fillheight. 
                    	Assign that fillheight to the cell, see  if its current altitude (corrected for already calculated ero and sed) is lower than fillheight.
                    	Add the difference to the sediment needed to fill the (now larger) lake.
                    	Add the cell to the lake and update the size and volume  of the lake
                c.	Continue until no more cells around the lake are lower 
                This would potentially clash with lakes that have more than two outlets. The third and later outlets would be 
                seen as lake cells and their lower neighbours on the non-lake-side would be added to the lake, leading to errors. */
            int urow = 0, ucol = 0, size = 0;
            int depressionnumber = number;
            //if (depressionnumber > 1) diagnostic_mode = 1;
            if (diagnostic_mode == 1) { Debug.WriteLine(" now updating depression " + depressionnumber); }
            depressionsum_water_m = 0;
            depressionsum_sediment_m = 0;
            depressionsum_texture_kg[0] = 0; depressionsum_texture_kg[1] = 0; depressionsum_texture_kg[2] = 0; depressionsum_texture_kg[3] = 0; depressionsum_texture_kg[4] = 0; depressionsum_YOM_kg = 0; depressionsum_OOM_kg = 0;
            needed_to_fill_depression_m = 0;

            for (urow = iloedge[depressionnumber]; urow <= iupedge[depressionnumber]; urow++)
            {
                for (ucol = jloedge[depressionnumber]; ucol <= jupedge[depressionnumber]; ucol++)
                {
                    if (((urow) >= 0) && ((urow) < nr) && ((ucol) >= 0) && ((ucol) < nc) && dtm[urow, ucol] != nodata_value)
                    {  //bnd
                        if (depression[urow, ucol] == depressionnumber)
                        {
                            if (only_waterflow_checkbox.Checked == false)
                            {
                                if (!(dz_ero_m[urow, ucol] == 0 && dz_sed_m[urow, ucol] == 0))
                                {
                                    Debug.WriteLine(" error in depression " + depressionnumber);
                                    // this should not happen: erosion or sedimentation into lake cells is not allowed - only the provision of those cells with sediment in transport.
                                    minimaps(urow, ucol);
                                }
                            }
                            // if they are member of the lake : add the diff between fillheight and dtm to the volume that must be filled, and add the sedintrans to what is available for filling
                            depressionsum_water_m += waterflow_m3[urow, ucol] / dx / dx;
                            if (only_waterflow_checkbox.Checked == false)
                            {
                                for (size = 0; size < n_texture_classes; size++)
                                {
                                    depressionsum_texture_kg[size] += sediment_in_transport_kg[urow, ucol, size];
                                }
                                depressionsum_OOM_kg += old_SOM_in_transport_kg[urow, ucol];
                                depressionsum_YOM_kg += young_SOM_in_transport_kg[urow, ucol];
                            }
                            needed_to_fill_depression_m += dtmfill_A[urow, ucol] - dtm[urow, ucol];
                            //if (diagnostic_mode == 1) { Debug.WriteLine(" dep_cell " + row + " " + col + " fillheight " + dtmfill_A[urow, ucol] + " dtm " + dtm[urow, ucol] + " needed now " + needed_to_fill_depression  ); }
                        }
                    }
                }
            }

            //now that we know how much kgs of every material are available for lake filling, we can calculate how much thickness [m3/m2 = m] that means.
            depressionsum_sediment_m = calc_thickness_from_mass(depressionsum_texture_kg, depressionsum_YOM_kg, depressionsum_OOM_kg);

            int updating_lake = 1;
            while (updating_lake == 1)
            {
                // while there are potential cells to be added to the lake
                updating_lake = 0;
                for (urow = iloedge[depressionnumber] - 1; urow <= iupedge[depressionnumber] + 1; urow++)
                {
                    for (ucol = jloedge[depressionnumber] - 1; ucol <= jupedge[depressionnumber] + 1; ucol++)
                    {
                        if (((urow) >= 0) && ((urow) < nr) && ((ucol) >= 0) && ((ucol) < nc) && dtm[urow, ucol] != nodata_value)
                        {  //bnd
                            if (depression[urow, ucol] != depressionnumber && dtm[urow, ucol] > depressionlevel[depressionnumber]) // the second part of the condition should ensure that no cells on the downstream side of outlets are added. 
                            {
                                double lowest_dtm_fill = 9999;
                                // we have now found a cell that should potentiall added to the lake - however, it should have a lake-neighbour for that to be really true. We look around to check this.
                                for (alpha = -1; alpha <= 1; alpha++)
                                {
                                    for (beta = -1; beta <= 1; beta++)
                                    {
                                        if ((urow + alpha) >= 0 && (urow + alpha) < nr && (ucol + beta) >= 0 && (ucol + beta) < nc && !((alpha) == 0 && (beta) == 0) && dtm[urow + alpha, ucol + beta] != nodata_value)
                                        {
                                            if ((urow != urow + alpha) && (ucol != ucol + beta)) { d_x = dx * Math.Sqrt(2); } else { d_x = dx; }
                                            if (only_waterflow_checkbox.Checked == false)
                                            {
                                                if (depression[urow + alpha, ucol + beta] == depressionnumber && (dtm[urow, ucol] + dz_ero_m[urow, ucol] + dz_sed_m[urow, ucol]) < (dtmfill_A[urow + alpha, ucol + beta] + epsilon * d_x))
                                                // if this cell has a lake neighbour and has a current altitude lower than the fillheight resulting from that neighbour, we store that fillheight until we find the lowest fillheight resulting from any lake-nbs of this cell
                                                {
                                                    if ((dtmfill_A[urow + alpha, ucol + beta] + epsilon * d_x) < lowest_dtm_fill)
                                                    {
                                                        lowest_dtm_fill = (dtmfill_A[urow + alpha, ucol + beta] + epsilon * d_x);

                                                    }
                                                }
                                            }
                                            else
                                            {
                                                if (depression[urow + alpha, ucol + beta] == depressionnumber && (dtm[urow, ucol]) < (dtmfill_A[urow + alpha, ucol + beta] + epsilon * d_x))
                                                // if this cell has a lake neighbour and has a current altitude lower than the fillheight resulting from that neighbour, we store that fillheight until we find the lowest fillheight resulting from any lake-nbs of this cell
                                                {
                                                    if ((dtmfill_A[urow + alpha, ucol + beta] + epsilon * d_x) < lowest_dtm_fill)
                                                    {
                                                        lowest_dtm_fill = (dtmfill_A[urow + alpha, ucol + beta] + epsilon * d_x);

                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                // after checking, if this cell had any lake-nb and was low enough itself, it is added to the lake and the search window is enlarged.
                                if (lowest_dtm_fill < 9999)
                                {
                                    depression[urow, ucol] = depressionnumber;
                                    depressionsize[depressionnumber]++;
                                    dtmfill_A[urow, ucol] = lowest_dtm_fill;
                                    if (only_waterflow_checkbox.Checked == false)
                                    {
                                        needed_to_fill_depression_m += dtmfill_A[urow, ucol] - (dtm[urow, ucol] + dz_ero_m[urow, ucol] + dz_sed_m[urow, ucol]);
                                        for (size = 0; size < n_texture_classes; size++)
                                        {
                                            sediment_in_transport_kg[urow, ucol, size] = 0;
                                        }
                                    }
                                    else { needed_to_fill_depression_m += dtmfill_A[urow, ucol] - (dtm[urow, ucol]); }
                                    updating_lake = 1;
                                    if (urow == iloedge[depressionnumber] - 1) { iloedge[depressionnumber]--; }
                                    if (urow == iupedge[depressionnumber] + 1) { iupedge[depressionnumber]++; }
                                    if (ucol == jloedge[depressionnumber] - 1) { jloedge[depressionnumber]--; }
                                    if (ucol == jupedge[depressionnumber] + 1) { jupedge[depressionnumber]++; }

                                    // setting sed in trans to 0 is required to avoid double-counting when building delta's later
                                    // no addition to depressionsum_sed or _water is required because flows from this cell have already been considered earlier, and any dz_ero_m or dz_sed_m have been considered, and sed_in_trans has arrived at the lake to be counted
                                    //if (diagnostic_mode == 1) { Debug.WriteLine(" non_dep_cell " + urow + " " + ucol + " fillheight " + dtmfill_A[urow, ucol] + " new dtm " + (dtm[urow, ucol] + dz_ero_m[urow, ucol] + dz_sed_m[urow, ucol]) + " needed now " + needed_to_fill_depression); }
                                }
                            }
                        }
                    }
                }  // end for

            } // end while
            if (diagnostic_mode == 1) { Debug.WriteLine(" Updated depression " + depressionnumber + ". Now size " + depressionsize[depressionnumber] + ", sum_water " + depressionsum_water_m + " sum_sed " + depressionsum_sediment_m + " Needed: " + needed_to_fill_depression_m); }

        }

        void fill_depression(int number, double fraction_sediment_used_for_this_dep)  // completely fills updated depressions (because enough sediment was available)
        {
            int this_depression = number;
            int fillrow = 0, size, fillcol = 0;
            //Debug.WriteLine(" now filling depression " + this_depression);
            sediment_filled_m += needed_to_fill_depression_m;

            for (fillrow = iloedge[this_depression]; fillrow <= iupedge[this_depression]; fillrow++)
            {
                for (fillcol = jloedge[this_depression]; fillcol <= jupedge[this_depression]; fillcol++)
                {
                    if (((fillrow) >= 0) && ((fillrow) < nr) && ((fillcol) >= 0) && ((fillcol) < nc) && dtm[fillrow, fillcol] != nodata_value)
                    {  //bnd
                        if (depression[fillrow, fillcol] == this_depression)
                        {
                            // add the actual kgs of different textures and OM to the top layer in this location: 

                            double fraction_sediment_used_for_this_cell = (dtmfill_A[fillrow, fillcol] - dz_ero_m[fillrow, fillcol] - dz_sed_m[fillrow, fillcol] - dtm[fillrow, fillcol]) / needed_to_fill_depression_m;
                            if (fraction_sediment_used_for_this_cell > 1) { Debug.WriteLine(" dumping too much soil into lake"); }
                            if (fraction_sediment_used_for_this_cell < 0) { Debug.WriteLine(" dumping negative soil into lake"); }
                            for (size = 0; size < n_texture_classes; size++)
                            {
                                texture_kg[fillrow, fillcol, 0, size] += fraction_sediment_used_for_this_dep * fraction_sediment_used_for_this_cell * depressionsum_texture_kg[size];
                            }
                            young_SOM_kg[fillrow, fillcol, 0] += fraction_sediment_used_for_this_dep * fraction_sediment_used_for_this_cell * depressionsum_YOM_kg;
                            old_SOM_kg[fillrow, fillcol, 0] += fraction_sediment_used_for_this_dep * fraction_sediment_used_for_this_cell * depressionsum_OOM_kg;

                            //also update dtm (although it will again be updated soon when we update soils)
                            lake_sed_m[fillrow, fillcol] += dtmfill_A[fillrow, fillcol] - dz_ero_m[fillrow, fillcol] - dz_sed_m[fillrow, fillcol] - dtm[fillrow, fillcol];
                            dtmchange_m[fillrow, fillcol] += dtmfill_A[fillrow, fillcol] - dz_ero_m[fillrow, fillcol] - dz_sed_m[fillrow, fillcol] - dtm[fillrow, fillcol];
                            soildepth_m[fillrow, fillcol] += dtmfill_A[fillrow, fillcol] - dz_ero_m[fillrow, fillcol] - dz_sed_m[fillrow, fillcol] - dtm[fillrow, fillcol];
                            dtm[fillrow, fillcol] = dtmfill_A[fillrow, fillcol] - dz_ero_m[fillrow, fillcol] - dz_sed_m[fillrow, fillcol];
                            if (dtm[fillrow, fillcol] == -1) { Debug.WriteLine("C cell " + (fillrow) + " " + (fillcol) + " has an altitude of -1 now"); minimaps(fillrow, fillcol); }
                        }
                    }
                }
            }
            int outletcounter = 0;
            while (drainingoutlet_col[this_depression, outletcounter] != -1)
            {
                outletcounter++;
                if (outletcounter == 5) { break; }
            }
            for (int i = 0; i < outletcounter; i++)
            {
                waterflow_m3[drainingoutlet_row[this_depression, i], drainingoutlet_col[this_depression, i]] += dx * dx * (depressionvolume_m[this_depression]) / outletcounter;
                //we need a rule to determine whether any sediment size stays behind preferentially if we fill a depression. For the moment: no preference
                for (size = 0; size < n_texture_classes; size++)
                {
                    //sediment_in_transport_kg[drainingoutlet_row[this_depression, i], drainingoutlet_col[this_depression, i]] += (depressionsum_sediment_m - needed_to_fill_depression_m) / outletcounter * ;
                    //we need to calculate how much of which fraction there is in transport, and then deposit by weight-ratio (or anything else).                
                }
            }
            //Debug.WriteLine(" Filled depression " + this_depression);
        }

        void leave_depression_alone(int number)  // only updates counters and sentinels
        {
            /*int this_depression = number;
            int leaverow = 0, leavecol = 0;
            //Debug.WriteLine(" leaving depression " + this_depression + " alone");

            for (leaverow = iloedge[this_depression];
            leaverow <= iupedge[this_depression]; leaverow++)
            {
                for (leavecol = jloedge[this_depression]; leavecol <= jupedge[this_depression]; leavecol++)
                {
                    if (((leaverow) >= 0) && ((leaverow) < nr) && ((leavecol) >= 0) && ((leavecol) < nc) && dtm[leaverow, leavecol] != nodata_value)
                    {  //bnd
                        if (depression[leaverow, leavecol] == this_depression)
                        {
                            considered[leaverow, leavecol] = 1;
                        }
                    }
                }
            }
            int outletcounter = 0;
            while (drainingoutlet_col[this_depression, outletcounter] != -1)
            {
                outletcounter++;
                if (outletcounter == 5) { break; }
            }
            for (i = 0; i < outletcounter; i++)
            {
                considered[drainingoutlet_row[this_depression, i], drainingoutlet_col[this_depression, i]] = 0;
            }
            //Debug.WriteLine(" Left depression " + this_depression + " alone"); */
        }

        void bottom_depression(int depnumber)
        /* this code is intended as an alternative for delta_depression, to save calculation time and increase model stability
         * instead of building deltas from each of a lake's side cells, we simply take the depressionsum_sediment_kg and put it in the lowest parts of the lake,
         * aiming to raise the lakebottom higher and higher until depressionsum_sed_kg runs  out. 
         * Lake bottom will be raised parallel to fillheight, not lakelevel, to ensure that we will ultimately have a non-flat surface. 
         * the code will 
         * a) calculate the difference between dtm+ero+sed and fillheight for each cell
         * b) sort that list in descending order to get the 'deepest' cell first.
         * c) calculate how many of the available cells can be raised how high with the available sediment
         * d) raise those cells
         */
        //a: calculate difference, populate list
        {
            int active_depression = depnumber;
            for (int startrow = iloedge[active_depression]; startrow <= iupedge[active_depression]; startrow++)
            {
                for (int startcol = jloedge[active_depression]; startcol <= jupedge[active_depression]; startcol++)
                {
                    if (((startrow) >= 0) && ((startrow) < nr) && ((startcol) >= 0) && ((startcol) < nc) && dtm[startrow, startcol] != nodata_value)
                    {  //bnd
                        if (depression[startrow, startcol] == active_depression)
                        {
                            double sed_thickness_needed_m = dtmfill_A[startrow, startcol] - (dtm[startrow, startcol] + dtm[startrow, startcol] + dtm[startrow, startcol]);
                            L_lakecells.Add(new Lakecell(startrow, startcol, sed_thickness_needed_m, 0));
                        }
                    }
                }
            }
            //b: now sort the list on the difference:
            L_lakecells.OrderBy(x => x.t_sed_needed_m).ToList();
            Console.WriteLine(String.Join(Environment.NewLine, L_lakecells));
            L_lakecells.Reverse();
            //c: walk through the ordered list and use up the depressionsum_sediment_m.
            // We know that there is less than we'd need to fill the entire lake.
            double sum_to_be_filled_m = 0;
            double depth_under_dtmfill_m = L_lakecells.ElementAt(0).t_sed_needed_m;
            for (int i = 0; i < L_lakecells.Count - 1; i++)
            {
                double extra_fill_m = L_lakecells.ElementAt(i).t_sed_needed_m - L_lakecells.ElementAt(i + 1).t_sed_needed_m;
                sum_to_be_filled_m += extra_fill_m * i; //this increases from 0 to beyond sum_to_be_filled. The outlet cell (which is part of the lake),
                                                        //has a t_sed_needed_m === 0, so if we get that far, sum_to_be_filled_m now is equal to the volume of the
                                                        //lake (to dtmfill), which is more than depression_sum_sediment_m
                depth_under_dtmfill_m -= extra_fill_m; // this diminishes from the initially lowest depth of any cell of the lake to 0 (if we made it to the last cell pair)            
                if (sum_to_be_filled_m > depressionsum_sediment_m)
                {
                    depth_under_dtmfill_m += (sum_to_be_filled_m - depressionsum_sediment_m) / i;
                    break;
                }
            }
            //d: we now know how far under dtmfill all lakecells can reach (although some may already be higher, those won't get filled);
            // let's fill then!
            for (int startrow = iloedge[active_depression]; startrow <= iupedge[active_depression]; startrow++)
            {
                for (int startcol = jloedge[active_depression]; startcol <= jupedge[active_depression]; startcol++)
                {
                    if (((startrow) >= 0) && ((startrow) < nr) && ((startcol) >= 0) && ((startcol) < nc) && dtm[startrow, startcol] != nodata_value)
                    {  //bnd
                        if (depression[startrow, startcol] == active_depression)
                        {
                            if (dtmfill_A[startrow, startcol] - depth_under_dtmfill_m > dtm[startrow, startcol])
                            {
                                dtm[startrow, startcol] = dtmfill_A[startrow, startcol] - depth_under_dtmfill_m;
                                young_SOM_kg[startrow, startcol, 0] = (dtmfill_A[startrow, startcol] - depth_under_dtmfill_m) / depressionsum_sediment_m * depressionsum_YOM_kg;
                                old_SOM_kg[startrow, startcol, 0] = (dtmfill_A[startrow, startcol] - depth_under_dtmfill_m) / depressionsum_sediment_m * depressionsum_OOM_kg;
                                for (int texxie = 0; texxie < 5; texxie++)
                                {
                                    texture_kg[startrow, startcol, 0, texxie] += (dtmfill_A[startrow, startcol] - depth_under_dtmfill_m) / depressionsum_sediment_m * depressionsum_texture_kg[texxie];
                                }
                            }
                        }

                    }

                }
            }
            depressionsum_sediment_m = 0;
            depressionsum_OOM_kg = 0;
            depressionsum_YOM_kg = 0;
            depressionsum_texture_kg[0] = 0; depressionsum_texture_kg[1] = 0; depressionsum_texture_kg[2] = 0; depressionsum_texture_kg[3] = 0; depressionsum_texture_kg[4] = 0;
            L_lakecells.Clear();
        }
        void delta_depression(int number)  // builds deltas in an updated depression (because not enough sed)
        {
            /*When there is not enough sediment to fill the lake, fill the lake from each of its initial side-cells that have a non-zero sediment in transport, excluding outlet cells (their sediment in transport gets moved outside  they will never have to be raised higher than lakelevel which they already have  - they will remain having the lakenumber even when the whole lake would have been filled). 

                c. For each of these cells, while there is sediment in transport /dx left:
                    	Find a potentially lower oblique cell (in the lake, but there are  no others)
                    	Find the first higher oblique cell relative to that cell
                    	Raise the oblique deepest cell to that level
                    	Reduce the remaining amount of sediment in transport with the raised amount
                    	Test whether the cell has now been raised above fillheight, in which case: 
                            it must be lowered to its fillheight, 
                            remaining amount of sed in trans must be increased again
                            lakecell must be removed from the lake (potentially fragmenting the original lake) 
            
            filling happens for the moment with perfectly mixed sediment for the entire lake. 
            The alternative, where different sides of a lake provide differently textured sediment to differently-textured deltas, is a 
            possible development
             */

            double fraction_of_depression_filled = depressionsum_sediment_m / needed_to_fill_depression_m;
            int active_depression = number, size;
            if (diagnostic_mode == 1 && number > 2300000) { Debug.WriteLine(" building deltas in depression " + number + " sed needed " + needed_to_fill_depression_m + " sed available " + depressionsum_sediment_m); }
            //else { diagnostic_mode = 0; }

            //if (number == 30001 && t == 1) { diagnostic_mode = 1; } else { diagnostic_mode = 0; }

            for (startrow = iloedge[active_depression]; startrow <= iupedge[active_depression]; startrow++)
            {
                for (startcol = jloedge[active_depression]; startcol <= jupedge[active_depression]; startcol++)
                {
                    if (((startrow) >= 0) && ((startrow) < nr) && ((startcol) >= 0) && ((startcol) < nc) && dtm[startrow, startcol] != nodata_value)
                    {  //bnd
                        int sediment_present = 0;
                        for (size = 0; size < n_texture_classes; size++)
                        {
                            if (sediment_in_transport_kg[startrow, startcol, size] > 0)
                            { sediment_present = 1; }
                        }
                        if (depression[startrow, startcol] == active_depression && sediment_present == 1)
                        // so, for all cells in the depression that have a non-zero sediment in transport (and EXCLUDING the outlets) 
                        {
                            if (!(drainingoutlet_row[active_depression, 0] == startrow && drainingoutlet_col[active_depression, 0] == startcol) &&
                                !(drainingoutlet_row[active_depression, 1] == startrow && drainingoutlet_col[active_depression, 1] == startcol) &&
                                !(drainingoutlet_row[active_depression, 2] == startrow && drainingoutlet_col[active_depression, 2] == startcol) &&
                                !(drainingoutlet_row[active_depression, 3] == startrow && drainingoutlet_col[active_depression, 3] == startcol) &&
                                !(drainingoutlet_row[active_depression, 4] == startrow && drainingoutlet_col[active_depression, 4] == startcol))
                            {
                                deltasize = 0;
                                dhobliquemax1 = 0;
                                iloradius3 = 1; iupradius3 = 1; jloradius3 = 1; jupradius3 = 1;
                                iloradius2 = 1; iupradius2 = 1; jloradius2 = 1; jupradius2 = 1;
                                rowlowestobnb = startrow; collowestobnb = startcol;
                                if (diagnostic_mode == 1) { Debug.WriteLine(" building a delta from " + startrow + " " + startcol + ", dtm " + dtm[startrow, startcol]); minimaps(startrow, startcol); }
                                double[] local_s_i_t_kg = new double[5] { 0, 0, 0, 0, 0 };
                                for (size = 0; size < n_texture_classes; size++)
                                {
                                    local_s_i_t_kg[size] = sediment_in_transport_kg[startrow, startcol, size];
                                    sediment_in_transport_kg[startrow, startcol, size] = 0;
                                }
                                available_for_delta_m += calc_thickness_from_mass(local_s_i_t_kg, 0, 0);
                                sediment_delta_m += available_for_delta_m;
                                // we will completely use all of this sed in trans now, so let's add it to the overall counter of the volume used in deltas

                                while (available_for_delta_m > 0)
                                {
                                    find_lowest_oblique_neighbour(active_depression);  // to start in the right location with building the delta
                                                                                       // this may be lower in the lake than at the side-cell, at location rowlowestobnb,collowestobnb
                                    depression[rowlowestobnb, collowestobnb] = -active_depression;   // we have found the start of this delta
                                    deltasize = 1;    // therefore we give delta a value of 1
                                    dhobliquemax2 = 99999.99;
                                    II = 0; JJ = 0;
                                    iloradius3 = 1; iupradius3 = 1; jloradius3 = 1; jupradius3 = 1;

                                    while (dhobliquemax2 > 0 && available_for_delta_m > 0)
                                    // starting in the lowest cell, we will now find the lowest higher nb
                                    // we will continue looking for lowest higher nbs as long as we find one and have sediment left
                                    // while doing this, we will raise our delta with us
                                    {
                                        find_lowest_higher_oblique_neighbour(active_depression);

                                        if (dhobliquemax2 < 0)
                                        {   // if we have found a lower neighbour
                                            // leave the current delta and bring the remaining sediment to the lower oblique neighbour
                                            if (diagnostic_mode == 1) { Debug.WriteLine(" lowest oblique neighbour is lower - moving sediment down "); }
                                            cleardelta(iloradius3, iupradius3, jloradius3, jupradius3, rowlowestobnb, collowestobnb);
                                            if (iloradius2 < -(rowlowestobnb + II - startrow)) { iloradius2 = -(rowlowestobnb + II - startrow); }
                                            if (iupradius2 < (rowlowestobnb + II - startrow)) { iupradius2 = (rowlowestobnb + II - startrow); }
                                            if (jloradius2 < -(collowestobnb + JJ - startcol)) { jloradius2 = -(collowestobnb + JJ - startcol); }
                                            if (jupradius2 < (collowestobnb + JJ - startcol)) { jupradius2 = (collowestobnb + JJ - startcol); }
                                            dhobliquemax1 = ((dtm[startrow, startcol] + dz_ero_m[startrow, startcol] + +dz_sed_m[startrow, startcol]) - (dtm[rowlowestobnb + II, collowestobnb + JJ] + dz_ero_m[rowlowestobnb + II, collowestobnb + JJ] + dz_sed_m[rowlowestobnb + II, collowestobnb + JJ]) - (Math.Sqrt((startrow - rowlowestobnb - II) * (startrow - rowlowestobnb - II) + (startcol - collowestobnb - JJ) * (startcol - collowestobnb - JJ)) * dx * tangent_of_delta)) - 0.0000001;
                                        }
                                        if (dhobliquemax2 == 0)
                                        {
                                            // this is not supposed to happen because in this case the search in find_lowest_higher_oblique_nb must go on.
                                            if (diagnostic_mode == 1) { Debug.WriteLine("Warning. Found dhobliquemax = 0 outside of find_lowest_higher_ob_nb"); }
                                        }
                                        if (dhobliquemax2 > 0)
                                        {
                                            if (diagnostic_mode == 1) { Debug.WriteLine(" lowest higher oblique neighbour is higher - raising delta"); }
                                            if (diagnostic_mode == 1) { Debug.WriteLine(" available " + available_for_delta_m + "m and space for " + (deltasize * dhobliquemax2) + " m"); }
                                            if (available_for_delta_m >= deltasize * dhobliquemax2)
                                            {
                                                if (diagnostic_mode == 1) { Debug.WriteLine(" raising delta to higher oblique level "); }
                                                raise_delta_completely(active_depression);
                                            }
                                            else
                                            {
                                                if (diagnostic_mode == 1) { Debug.WriteLine(" raising delta as far as possible given sediment "); }
                                                raise_delta_partly(active_depression);
                                                if (diagnostic_mode == 1) { minimaps(row, col); }
                                                if (obnbchanged == 0) { cleardelta(iloradius3, iupradius3, jloradius3, jupradius3, rowlowestobnb, collowestobnb); }
                                                // if the starting cell was raised above lakelevel, it is no longer member of the lake, and we have taken care of that in raise_delta_partly by changing obnb. 
                                                // this must not be removed, so if obnbchanged != 0, we do not clear the delta.
                                            }

                                        } // end if dhoblmax2 > 0
                                    } // end while dhobliquemax2 > 0
                                } // end while sediment_available
                                cleardelta(iloradius3, iupradius3, jloradius3, jupradius3, rowlowestobnb, collowestobnb);
                            } //end if not outlet 
                        } // end if depressio
                    } // end if boundaries
                } // end for col
            } //end for row 

            // we now divide the total amount of extra water (the amount replaced by sediment in the lake) over the (max 5) outlets.
            int outletcounter = 0;
            while (drainingoutlet_col[active_depression, outletcounter] != -1)
            {
                outletcounter++;
                if (outletcounter == 5) { break; }
            }
            for (i = 0; i < outletcounter; i++)
            {
                waterflow_m3[drainingoutlet_row[active_depression, i], drainingoutlet_col[active_depression, i]] += dx * dx * (depressionsum_sediment_m) / outletcounter;
            }
            //diagnostic_mode = 1;
        }

        void find_lowest_oblique_neighbour(int this_depression) // to determine where to start or continue with current delta 
        {
            if (t > 300000) { diagnostic_mode = 1; }
            if (diagnostic_mode == 1) { Debug.WriteLine(" entered find_lowest_oblique_neighbour"); }
            // finds the lowest oblique neighbour of the current delta
            // affects (changes) global doubles dhobliquemax1 et al
            //int this_depression = Math.Abs(depression[startrow, startcol]);

            int readysearching = 0;
            while (readysearching == 0)
            {
                readysearching = 1;      // we expect to be ready searching, but when not, we will set this to 0
                if (diagnostic_mode == 1) { Debug.WriteLine(" dhobliquemax1 : " + dhobliquemax1); }
                if (diagnostic_mode == 1) { Debug.WriteLine(" ilo " + iloradius2 + ", iup " + iupradius2 + ", jlo " + jloradius2 + ", jup " + jupradius2 + ", row: " + startrow + ", col " + startcol); }
                for (i = -iloradius2; i <= iupradius2; i++)
                {
                    for (j = -jloradius2; j <= jupradius2; j++)
                    {
                        if ((startrow + i >= 0) && (startrow + i < nr) && (startcol + j >= 0) && (startcol + j < nc) && !((i == 0) && (j == 0)) && dtm[startrow + i, startcol + j] != nodata_value) //&& !((startrow + i == row) && (startcol + j == col))
                        { // boundary check while looking around startrow startcol for the neighbours of the entire current delta
                            if (diagnostic_mode == 1) { Debug.WriteLine(" oblique neighbour now " + (startrow + i) + " " + (startcol + j) + ", depression " + depression[startrow + i, startcol + j] + " dtm " + (dtm[startrow + i, startcol + j] + dz_ero_m[startrow + i, startcol + j] + dz_sed_m[startrow + i, startcol + j])); }
                            if (depression[startrow + i, startcol + j] == this_depression || depression[startrow + i, startcol + j] == -this_depression)
                            {  // if you are a member of this depression
                                dhoblique = (dtm[startrow, startcol] + dz_ero_m[startrow, startcol] + dz_sed_m[startrow, startcol]) - (dtm[startrow + i, startcol + j] + dz_ero_m[startrow + i, startcol + j] + dz_sed_m[startrow + i, startcol + j]) - (Math.Sqrt((i * i) + (j * j)) * dx * tangent_of_delta);
                                if (diagnostic_mode == 1) { Debug.WriteLine(" oblique neighbour now " + (startrow + i) + " " + (startcol + j) + " :" + dhoblique); }
                                //if (diagnostic_mode == 1) { Debug.WriteLine(" dhobliquemax1 : " + dhobliquemax1); }
                                if ((dhoblique > dhobliquemax1))
                                {      // vanwege afkortinsverschillen 0.000000000 etc 1
                                    dhobliquemax1 = dhoblique;
                                    rowlowestobnb = startrow + i;
                                    collowestobnb = startcol + j;
                                    //deltasize = 1;
                                    if (this_depression > 0)
                                    {
                                        if (diagnostic_mode == 1) { Debug.WriteLine(" lowest oblique neighbour now " + rowlowestobnb + " " + collowestobnb + " dhobliquemax1 : " + dhobliquemax1); }
                                    }
                                    lower_nb_exists = 1;
                                    readysearching = 0;
                                    if (i == -1 * iloradius2) { iloradius2++; }
                                    if (i == iupradius2) { iupradius2++; }
                                    if (j == -1 * jloradius2) { jloradius2++; }
                                    if (j == jupradius2) { jupradius2++; }
                                    if (diagnostic_mode == 1) { Debug.WriteLine(" ilo " + iloradius2 + ", iup " + iupradius2 + ", jlo " + jloradius2 + ", ju2 " + jupradius2); }
                                } // end if dhoblique  < dhobliquemax1)
                                if (dhoblique < 0.0000000001 && dhobliquemax1 < 0.0000000001 && dhoblique > -0.0000000001)
                                {   // in this case, we may have filled the present neighbour
                                    // in an earlier stage, but have had to clear the delta because
                                    // a lower obnb was found. We must look beyond this equally-high
                                    // oblique nb to be able to find this lower obnb.....
                                    if (diagnostic_mode == 1) { Debug.WriteLine("found previous delta"); }
                                    if (i == -1 * iloradius2) { iloradius2++; readysearching = 0; }
                                    if (i == iupradius2) { iupradius2++; readysearching = 0; }
                                    if (j == -1 * jloradius2) { jloradius2++; readysearching = 0; }
                                    if (j == jupradius2) { jupradius2++; readysearching = 0; }
                                } // end if dhoblique and dhobliquemax1 are zero
                            } // end if depression = depression
                        } // end if boundaries
                    } // end for j
                } // end if
            } // end while readysearching = 0
            if (diagnostic_mode == 1) { Debug.WriteLine(" ready searching - dhobliquemax1: " + dhobliquemax1 + ", dx: " + dx + ", row " + rowlowestobnb + ", col " + collowestobnb); }
        }

        void find_lowest_higher_oblique_neighbour(int here_depression) // to determine to which level (and cell) the current delta can be raised 
        {
            if (t > 1000000) { diagnostic_mode = 1; }
            if (diagnostic_mode == 1) { Debug.WriteLine(" entered find_lowest_higher_oblique_neighbour"); }
            if (diagnostic_mode == 1) { Debug.WriteLine(" rowlow = " + rowlowestobnb + " collow = " + collowestobnb + " range " + iloradius3 + iupradius3 + jloradius3 + jupradius3); }
            if (diagnostic_mode == 1 && rowlowestobnb == 224) { minimaps(rowlowestobnb, collowestobnb); }
            readysearching = 0;
            while (readysearching == 0)
            {
                dhobliquemax2 = 99999.99;
                for (i = -1 * iloradius3; i <= iupradius3; i++)
                {
                    for (j = -1 * jloradius3; j <= jupradius3; j++)
                    {
                        if (((rowlowestobnb + i) >= 0) && ((rowlowestobnb + i) < nr) && ((collowestobnb + j) >= 0) && ((collowestobnb + j) < nc) && !((i == 0) && (j == 0)))
                        { // boundaries
                            if (depression[rowlowestobnb + i, collowestobnb + j] == here_depression)
                            {
                                dhoblique = -((dtm[rowlowestobnb, collowestobnb]) + dz_ero_m[rowlowestobnb, collowestobnb] + dz_sed_m[rowlowestobnb, collowestobnb]) + (dtm[rowlowestobnb + i, collowestobnb + j] + dz_ero_m[rowlowestobnb + i, collowestobnb + j] + dz_sed_m[rowlowestobnb + i, collowestobnb + j]) + (Math.Sqrt(Math.Pow((rowlowestobnb + i - startrow), 2) + Math.Pow((collowestobnb + j - startcol), 2)) - Math.Sqrt(Math.Pow((rowlowestobnb - startrow), 2) + Math.Pow((collowestobnb - startcol), 2))) * dx * tangent_of_delta;
                                if (dhoblique != 0 && dhoblique < dhobliquemax2)
                                {
                                    readysearching = 1;
                                    dhobliquemax2 = dhoblique;
                                    II = i; JJ = j;
                                    if (diagnostic_mode == 1 && dhobliquemax2 < 0) { Debug.WriteLine("cell " + (rowlowestobnb + i) + " " + (collowestobnb + j) + " is a lower obnb (dhoblmax2 = " + dhobliquemax2 + ") from " + rowlowestobnb + collowestobnb); }
                                    // 
                                } // end if
                                if (dhoblique < 0.0000000001 && dhoblique > -0.0000000001)
                                {       //this means we have transferred to a 'new delta' after encountering a negative dhobliquemax2 first,
                                        //then filled this new delta to the point that we are as high as the previous one
                                        // we therefore encounter one or more cells with dhoblique = 0
                                        //we must incorporate these cells into the delta and increase the searchradius
                                    depression[rowlowestobnb + i, collowestobnb + j] = -here_depression;
                                    deltasize++;
                                    readysearching = 0;
                                    if (diagnostic_mode == 1) { Debug.WriteLine("added " + (rowlowestobnb + i) + " " + (collowestobnb + j) + " (dhoblique 0.0000) to the delta and will increase searchradius for lowest higher nbour"); }
                                } //end if dhoblmax2 == 0
                            } // end if depression == depression
                        } // end if bndries
                    } // end for j
                } // end for i
                if (readysearching == 0)
                {
                    // If no neighbour within the original radius3 from obnb was member of a lake (which can happen if they were raised to dtm_fill earlier), then 
                    // we will increase the searchradius (there can be another lake cell somewhere, because the lake is not being filled completely).
                    iloradius3++; if (diagnostic_mode == 1) { Debug.WriteLine("ilo is higher"); }
                    iupradius3++; if (diagnostic_mode == 1) { Debug.WriteLine("iup is higher"); }
                    jloradius3++; if (diagnostic_mode == 1) { Debug.WriteLine("jlo is higher"); }
                    jupradius3++; if (diagnostic_mode == 1) { Debug.WriteLine("jup is higher"); }
                    //however, no higher ob nb is necessarily present. The current obnb, and possibly its delta, need not have a lake-neighbour with a higher dhoblique
                    //this is for instance the case when we are currently looking at the last cell in the lake.
                    //In that case, we must fill the current delta as much as possible with the existing sediment. A corresponding dhoblique must be sent back to control.
                    if (rowlowestobnb - iloradius3 <= iloedge[here_depression] && rowlowestobnb + iupradius3 >= iupedge[here_depression] &&
                        collowestobnb - jloradius3 <= jloedge[here_depression] && collowestobnb + jupradius3 >= jupedge[here_depression])
                    {
                        readysearching = 1;
                        dhobliquemax2 = (dtmfill_A[rowlowestobnb, collowestobnb] - (dtm[rowlowestobnb, collowestobnb] + dz_ero_m[rowlowestobnb, collowestobnb] + dz_sed_m[rowlowestobnb, collowestobnb]));
                        if (dhobliquemax2 == 0) { dhobliquemax2 = 1; }//ArT If the dhoblique is zero, because the obnb was the outlet with dtmfill== dtm, then we give an emergency value to dhobliquemax2  
                        if (diagnostic_mode == 1) { Debug.WriteLine(" search for lower ob nb finished - not found - sending dhobliquemax as equal to fillspace of " + dhobliquemax2); }
                    }
                }
            } //end while readysearching == 1
        }

        void raise_delta_completely(int this_depression) // raise delta completely (and then go on raising it to higher obl heights)  
        {
            int size;
            // the amount of sed_in_trans left is enough to raise the entire delta with dhobliquemax1
            if (diagnostic_mode == 1) { Debug.WriteLine(" 2: to be added: " + dhobliquemax2 + ", sed_for_delta: " + available_for_delta_m + "m, deltasize: " + deltasize); }
            if (diagnostic_mode == 1) { Debug.WriteLine(" rowlowestobnb " + rowlowestobnb + " collowestobnb " + collowestobnb + " and " + iloradius3 + iupradius3 + jloradius3 + jupradius3); }
            for (i = -1 * iloradius3; i <= iupradius3; i++)
            {
                for (j = -1 * jloradius3; j <= jupradius3; j++)
                {
                    if (((rowlowestobnb + i) >= 0) && ((rowlowestobnb + i) < nr) && ((collowestobnb + j) >= 0) && ((collowestobnb + j) < nc) && !(rowlowestobnb + i == row && collowestobnb + j == col) && dtm[rowlowestobnb + i, collowestobnb + j] != nodata_value)
                    { // boundaries, note that i==0 && j==0 is allowed  ;we can raise rowlowestobnb,colloewsobnb when it is part of the delta.
                        if (depression[rowlowestobnb + i, collowestobnb + j] == -this_depression) // i.e. if cell is part of present delta
                        {
                            for (int text_class = 0; text_class < 5; text_class++)
                            {
                                texture_kg[rowlowestobnb + i, collowestobnb + j, 0, text_class] += depressionsum_texture_kg[text_class] * (dhobliquemax2 / depressionsum_sediment_m);
                            }
                            old_SOM_kg[rowlowestobnb + i, collowestobnb + j, 0] += depressionsum_OOM_kg * (dhobliquemax2 / depressionsum_sediment_m);
                            young_SOM_kg[rowlowestobnb + i, collowestobnb + j, 0] += depressionsum_YOM_kg * (dhobliquemax2 / depressionsum_sediment_m);
                            dtm[rowlowestobnb + i, collowestobnb + j] += dhobliquemax2;
                            dtmchange_m[rowlowestobnb + i, collowestobnb + j] += dhobliquemax2;
                            lake_sed_m[rowlowestobnb + i, collowestobnb + j] += dhobliquemax2;
                            available_for_delta_m -= dhobliquemax2;
                            if (dtm[rowlowestobnb + i, collowestobnb + j] == -1) { Debug.WriteLine("A1 cell " + (rowlowestobnb + i) + " " + (collowestobnb + j) + " has an altitude of -1 now"); minimaps((rowlowestobnb + i), (collowestobnb + j)); }
                            if (available_for_delta_m < 0) { Debug.WriteLine(" Error: negative sediment for delta " + available_for_delta_m + " m"); minimaps(rowlowestobnb + i, collowestobnb + j); }
                            if (diagnostic_mode == 1) { Debug.WriteLine(" raised " + (rowlowestobnb + i) + " " + (collowestobnb + j) + " with " + dhobliquemax2 + " to " + (dtm[rowlowestobnb + i, collowestobnb + j] + dz_ero_m[rowlowestobnb + i, collowestobnb + j] + dz_sed_m[rowlowestobnb + i, collowestobnb + j]) + " sed_for_delta now " + available_for_delta_m); }
                            if (diagnostic_mode == 1 && dhobliquemax2 < 0) { Debug.WriteLine(" warning: dhobliquemax2 is less than zero at " + (rowlowestobnb + i) + " " + (collowestobnb + j) + " " + dhobliquemax2); }
                            //if (diagnostic_mode == 1) { MessageBox.Show("warning - extremely high coarse sed_in_trans:" + sediment_in_transport_kg[startrow, startcol,0]); }
                            if ((dtm[rowlowestobnb + i, collowestobnb + j] + dz_ero_m[rowlowestobnb + i, collowestobnb + j] + dz_sed_m[rowlowestobnb + i, collowestobnb + j]) > dtmfill_A[rowlowestobnb + i, collowestobnb + j])
                            {   // then we have raised this cell too high
                                double take_back_m = (dtm[rowlowestobnb + i, collowestobnb + j] + dz_ero_m[rowlowestobnb + i, collowestobnb + j] + dz_sed_m[rowlowestobnb + i, collowestobnb + j]) - dtmfill_A[rowlowestobnb + i, collowestobnb + j];
                                if (diagnostic_mode == 1) { Debug.WriteLine("1 we change the too-high altitude of " + (rowlowestobnb + i) + " " + (collowestobnb + j) + " (depressionlevel " + depressionlevel[this_depression] + ") from " + dtm[rowlowestobnb + i, collowestobnb + j] + " to " + dtmfill_A[rowlowestobnb + i, collowestobnb + j]); }
                                available_for_delta_m += take_back_m;
                                for (size = 0; size < n_texture_classes; size++)
                                {
                                    //any sediment in transport that was possibly waiting for consideration later than the current startrow, startcol is taken into this startrow startcol to make a bigger delta here
                                    local_s_i_t_kg[size] += sediment_in_transport_kg[rowlowestobnb + i, collowestobnb + j, size];
                                    sediment_in_transport_kg[rowlowestobnb + i, collowestobnb + j, size] = 0;
                                    texture_kg[rowlowestobnb + i, collowestobnb + j, 0, size] -= depressionsum_texture_kg[size] * (take_back_m / depressionsum_sediment_m);
                                }
                                old_SOM_kg[rowlowestobnb + i, collowestobnb + j, 0] -= depressionsum_OOM_kg * (take_back_m / depressionsum_sediment_m);
                                young_SOM_kg[rowlowestobnb + i, collowestobnb + j, 0] -= depressionsum_YOM_kg * (take_back_m / depressionsum_sediment_m);
                                if (available_for_delta_m < 0) { Debug.WriteLine("5 negative sediment for delta " + available_for_delta_m + " m"); }
                                lake_sed_m[rowlowestobnb + i, collowestobnb + j] -= take_back_m;
                                dtmchange_m[rowlowestobnb + i, collowestobnb + j] -= take_back_m;
                                if (lake_sed_m[rowlowestobnb + i, collowestobnb + j] < -0.0000001) { Debug.WriteLine("1 Warning: negative lake deposition in " + (rowlowestobnb + i) + " " + (collowestobnb + j) + " of " + lake_sed_m[rowlowestobnb + i, collowestobnb + j] + " dtm " + dtm[rowlowestobnb + i, collowestobnb + j] + " fill " + dtmfill_A[rowlowestobnb + i, collowestobnb + j]); minimaps(rowlowestobnb + i, collowestobnb + j); }
                                dtm[rowlowestobnb + i, collowestobnb + j] = dtmfill_A[rowlowestobnb + i, collowestobnb + j] - dz_ero_m[rowlowestobnb + i, collowestobnb + j] - dz_sed_m[rowlowestobnb + i, collowestobnb + j];
                                if (dtm[rowlowestobnb + i, collowestobnb + j] == -1) { Debug.WriteLine("A2 cell " + (rowlowestobnb + i) + " " + (collowestobnb + j) + " has an altitude of -1 now"); minimaps((rowlowestobnb + i), (collowestobnb + j)); }
                                depression[rowlowestobnb + i, collowestobnb + j] = 0;
                                deltasize--;
                                if (diagnostic_mode == 1) { Debug.WriteLine("1: " + (rowlowestobnb + i) + " " + (collowestobnb + j) + " (depressionlevel " + depressionlevel[depression[row, col]] + ") now at " + dtm[rowlowestobnb + i, collowestobnb + j] + " = fill_A " + dtmfill_A[rowlowestobnb + i, collowestobnb + j] + " sed for delta " + available_for_delta_m); }
                                if (diagnostic_mode == 1) { Debug.WriteLine("decreased deltasize with 1 to " + deltasize); }
                            } // end if dtm > depressionlevel
                        }   // end if member of delta
                    }  // end if boundaries
                }  // end for j
            }  // end for i
            if (diagnostic_mode == 1)
            {
                Debug.WriteLine(" raised and now move on to enlarge delta with " + (rowlowestobnb + II) + " " + (collowestobnb + JJ) + " available for delta now " + available_for_delta_m + "m");
                Debug.WriteLine(" by changing " + (rowlowestobnb + II) + "," + (collowestobnb + JJ) + " from " + depression[rowlowestobnb + II, collowestobnb + JJ] + " into " + (-this_depression));
            }
            depression[rowlowestobnb + II, collowestobnb + JJ] = -this_depression;
            deltasize++;
            if (diagnostic_mode == 1) { Debug.WriteLine(" raised deltasize with 1 to " + deltasize + ". " + (rowlowestobnb + II) + " " + (collowestobnb + JJ) + " now negative lakevalue"); }
            //dhobliquemax2 = 0;
            if (II == -iloradius3) { iloradius3++; }
            if (II == iupradius3) { iupradius3++; }
            if (JJ == -jloradius3) { jloradius3++; }
            if (JJ == jupradius3) { jupradius3++; }
        }

        void raise_delta_partly(int this_depression) // raise partly (and then go to new cells on border of lake)  
        {

            int size;
            //Debug.WriteLine( "raising delta partly for dep " + this_depression);
            mem_m = available_for_delta_m / deltasize;
            available_for_delta_m = 0;
            if (diagnostic_mode == 1) { Debug.WriteLine(" 1: to be added: " + mem_m + ", deltasize: " + deltasize); }
            tempx = rowlowestobnb; tempy = collowestobnb;
            obnbchanged = 0;
            for (i = -1 * iloradius3; i <= iupradius3; i++)
            {
                for (j = -1 * jloradius3; j <= jupradius3; j++)
                {
                    if (((tempx + i) >= 0) && ((tempx + i) < nr) && ((tempy + j) >= 0) && ((tempy + j) < nc) && !(tempx + i == row && tempy + j == col) && dtm[tempx + i, tempy + j] != nodata_value)
                    { // boundaries
                        if (depression[tempx + i, tempy + j] == -this_depression)
                        {
                            for (int text_class = 0; text_class < 5; text_class++)
                            {
                                texture_kg[tempx + i, tempy + j, 0, text_class] += depressionsum_texture_kg[text_class] * (mem_m / depressionsum_sediment_m);
                            }
                            old_SOM_kg[tempx + i, tempy + j, 0] += depressionsum_OOM_kg * (mem_m / depressionsum_sediment_m);
                            young_SOM_kg[tempx + i, tempy + j, 0] += depressionsum_YOM_kg * (mem_m / depressionsum_sediment_m);
                            dtm[tempx + i, tempy + j] += mem_m;
                            dtmchange_m[tempx + i, tempy + j] += mem_m;
                            lake_sed_m[tempx + i, tempy + j] += mem_m;
                            if (dtm[tempx + i, tempy + j] == -1) { Debug.WriteLine("B cell " + (tempx + i) + " " + (tempy + j) + " has an altitude of -1 now"); minimaps((rowlowestobnb + i), (collowestobnb + j)); }
                            if (lake_sed_m[tempx + i, tempy + j] < -0.0000001) { Debug.WriteLine("4 Warning: negative lake deposition in " + (tempx + i) + " " + (tempy + j) + " of " + lake_sed_m[tempx + i, tempy + j]); minimaps(tempx + i, tempy + j); }
                            if (diagnostic_mode == 1) { Debug.WriteLine(" added " + mem_m + " to cell " + (tempx + i) + " " + (tempy + j)); }
                            if ((dtm[tempx + i, tempy + j] + dz_ero_m[tempx + i, tempy + j] + dz_sed_m[tempx + i, tempy + j]) > dtmfill_A[tempx + i, tempy + j])
                            {
                                double take_back_m = (dtm[tempx + i, tempy + j] + dz_ero_m[tempx + i, tempy + j] + dz_sed_m[tempx + i, tempy + j]) - dtmfill_A[tempx + i, tempy + j];
                                if (diagnostic_mode == 1) { Debug.WriteLine(" cell " + (tempx + i) + " " + (tempy + j) + " raised above filllevel " + dtmfill_A[tempx + i, tempy + j] + ", to " + (dtm[tempx + i, tempy + j] + dz_ero_m[tempx + i, tempy + j] + dz_sed_m[tempx + i, tempy + j])); }
                                available_for_delta_m += take_back_m;
                                for (size = 0; size < n_texture_classes; size++)
                                {
                                    local_s_i_t_kg[size] += sediment_in_transport_kg[tempx + i, tempy + j, size];
                                    sediment_in_transport_kg[tempx + i, tempy + j, size] = 0;   //ART recently changed, should solve a bug (this line did not exist, violation mass balance
                                    texture_kg[tempx + i, tempy + j, 0, size] -= depressionsum_texture_kg[size] * (take_back_m / depressionsum_sediment_m);
                                }
                                old_SOM_kg[tempx + i, tempy + j, 0] -= depressionsum_OOM_kg * (take_back_m / depressionsum_sediment_m);
                                young_SOM_kg[tempx + i, tempy + j, 0] -= depressionsum_YOM_kg * (take_back_m / depressionsum_sediment_m);
                                if (available_for_delta_m < 0) { Debug.WriteLine("9 negative sediment in transport (m) remaining for delta " + available_for_delta_m + "m"); }
                                if (diagnostic_mode == 1) { Debug.WriteLine(" A we change the altitude of " + (tempx + i) + " " + (tempy + j) + " (depressionlevel " + depressionlevel[this_depression] + ") from " + (dtm[tempx + i, tempy + j] + dz_ero_m[tempx + i, tempy + j] + dz_sed_m[tempx + i, tempy + j]) + " to " + dtmfill_A[tempx + i, tempy + j]); }
                                if (tempx + i == row && tempy + j == col) { Debug.WriteLine("we are changing outlet " + tempx + " " + tempy + " into 0"); }
                                lake_sed_m[tempx + i, tempy + j] -= take_back_m;
                                dtmchange_m[tempx + i, tempy + j] -= take_back_m;
                                if (lake_sed_m[tempx + i, tempy + j] < -0.0000001) { Debug.WriteLine("3 Warning: negative lake deposition in " + (tempx + i) + " " + (tempy + j) + " of " + lake_sed_m[tempx + i, tempy + j] + " alt " + (dtm[tempx + i, tempy + j] + dz_ero_m[tempx + i, tempy + j] + dz_sed_m[tempx + i, tempy + j]) + " fill " + dtmfill_A[tempx + i, tempy + j]); minimaps(tempx + i, tempy + j); }
                                dtm[tempx + i, tempy + j] = (dtmfill_A[tempx + i, tempy + j] - dz_ero_m[tempx + i, tempy + j] - dz_sed_m[tempx + i, tempy + j]); //so that with ero and sed, it equals dtmfill
                                if (dtm[tempx + i, tempy + j] == -1) { Debug.WriteLine("C cell " + (tempx + i) + " " + (tempy + j) + " has an altitude of -1 now"); minimaps(tempx + i, tempy + j); } //
                                if (diagnostic_mode == 1) { Debug.WriteLine(" will change depressionmembership of " + (tempx + i) + " " + (tempy + j) + " from " + depression[tempx + i, tempy + j] + " to 0"); }
                                if (diagnostic_mode == 1) { Debug.WriteLine(" II = " + II + ", JJ = " + JJ); }
                                depression[tempx + i, tempy + j] = 0;
                                if (diagnostic_mode == 1) { Debug.WriteLine(" obnbchanged? - row " + row + " col " + col + " startrow " + startrow + " startcol " + startcol + " rowobnb " + rowlowestobnb + " colobnb " + collowestobnb + " tempx+i " + (tempx + i) + " tempy+j " + (tempy + j)); }
                                obnbchanged = 1;  // if there is at least one cell that has been raised above dtmfill, then that is the lowest oblique neighbour: the cell with rowlowestobnb, collowestobnb. 
                                                  // In that case, we must move to a new rowlowestobnb collowestobnb to build the remainder of the delta from there. There is a remainder because some of the sediment used to raise the original
                                                  // lowest oblique neighbour above dtmfill has been added to available_for_delta again.
                                deltasize--;
                                if (diagnostic_mode == 1) { Debug.WriteLine("decreased deltasize with 1 to " + deltasize); }
                                break;
                            } // end if higher than depressionlevel
                        }  //end if member of delta
                    } // end for boundaries
                } //end for j
            }  // end for i
            if (obnbchanged == 1)
            {
                // this code makes sure that the starting situation for the delta to be built with the remainder of the sediment is correct.
                // this desired situation is: 
                // a) correct amount of sediment (already guaranteed)
                // b) correct cells member of delta (already correct if deltasize > 0, but if deltasize == 0 after removal of the too-high cell, then not correct).
                // c) correct starting cell for delta building (rowlowestobnb and collowestobnb). Not correct because it may be that cell which was just removed from lake and delta. 
                // In case deltasize > 0 and rowlobnb,collobnb is no longer part of the delta, we select any delta-neighbour of the old starting cell.
                // In case deltasize = 0, we select the lowest higher oblique nb for this.
                // Nov 2021: If deltasize = 0, rowlobnb, collownb also is possibly no longer part of the lake. Then what? Then any lake-neighbour of the old starting cell, but we'll need to look harder.

                if (deltasize == 0)
                {
                    if (depression[tempx + II, tempy + JJ] == this_depression)
                    {
                        deltasize++;
                        if (diagnostic_mode == 1) { Debug.WriteLine("increased deltasize with 1 to " + deltasize); }
                        if (diagnostic_mode == 1) { Debug.WriteLine("lowest oblique neighbour now " + rowlowestobnb + " " + collowestobnb + ", will be: " + (tempx + II) + " " + (tempy + JJ)); }
                        rowlowestobnb = tempx + II;
                        collowestobnb = tempy + JJ;
                        if (diagnostic_mode == 1) { Debug.WriteLine(" will change depressionmembership of " + rowlowestobnb + " " + collowestobnb + " from " + depression[rowlowestobnb, collowestobnb] + " to " + (-this_depression)); }
                        depression[rowlowestobnb, collowestobnb] = -this_depression;
                    }
                    else
                    {
                        //we have no delta cells left, but we do have sed_in_trans left. We don't know from where to continue delta-building. We do know from where we started delta building, but that cell may no longer be part of the lake.
                        //we also know that there are at least one lake cells left (because we are in fill_delta_partly here), and that all lake cells are within iupradius3,iloradius3, etc from tempx, tempy
                        if (diagnostic_mode == 1)
                        {
                            Debug.WriteLine("row" + row + "col " + col + " depression " + depression[row, col]);
                            Debug.WriteLine("rowlowestobnb" + rowlowestobnb + "collowestobnb " + collowestobnb + " depression " + depression[rowlowestobnb, collowestobnb]);
                            Debug.WriteLine("tempx" + tempx + "tempy " + tempy + " depression " + depression[tempx, tempy]);
                            Debug.WriteLine("tempx + II" + (tempx + II) + "tempy + JJ " + (tempy + JJ) + " depression " + depression[tempx + II, tempy + JJ]);
                            minimaps(row, col);
                        }
                        bool found_a_delta_starter = false;
                        int tprowlowestobnb = large_negative_number;
                        int tpcollowestobnb = large_negative_number;
                        for (i = -1 * iloradius3; i <= iupradius3; i++)
                        {
                            for (j = -1 * jloradius3; j <= jupradius3; j++)
                            {
                                if (((tempx + i) >= 0) && ((tempx + i) < nr) && ((tempy + j) >= 0) && ((tempy + j) < nc) && !(tempx + i == row && tempy + j == col) && dtm[tempx + i, tempy + j] != nodata_value)
                                { // boundaries
                                    if (depression[tempx + i, tempy + j] == this_depression)
                                    {
                                        //Debug.WriteLine(" found a possible delta-starter in cell" + (tempx + i) + " " + (tempy + j) + " depression " + depression[tempx + i, tempy + j] + " for depression " + this_depression);
                                        found_a_delta_starter = true;
                                        tprowlowestobnb = tempx + i;
                                        tpcollowestobnb = tempy + j;

                                    }
                                }
                            }
                        }
                        if (found_a_delta_starter == true)
                        {
                            depression[tprowlowestobnb, tpcollowestobnb] = -this_depression;
                            rowlowestobnb = tprowlowestobnb;
                            collowestobnb = tpcollowestobnb;
                            deltasize++;
                            if (diagnostic_mode == 1) { Debug.WriteLine("increased deltasize with 1 to " + deltasize); }
                        }
                        else
                        {
                            Debug.WriteLine("did not find an alternative start for this delta  " + deltasize + "in lake " + this_depression + " at time " + t);
                            //this means that no lake cells are left, even though there is sediment left. We are wrong about the former or the latter
                            //development needed
                            //in meantime, let's try to simply end this lake and move on
                            depressionsum_sediment_m = 0;
                            for (size = 0; size < n_texture_classes; size++)
                            {
                                local_s_i_t_kg[size] = 0;
                                depressionsum_texture_kg[size] = 0;
                            }
                            depressionsum_OOM_kg = 0;
                            depressionsum_YOM_kg = 0;
                            available_for_delta_m = 0;
                            dhobliquemax2 = 0;
                            //Debug.WriteLine("trying to break free and move on");

                        }

                    }
                }
                if (deltasize > 0 && depression[rowlowestobnb, collowestobnb] != -this_depression)
                {
                    if (depression[rowlowestobnb + 1, collowestobnb + 1] == -this_depression) { rowlowestobnb = rowlowestobnb + 1; collowestobnb = collowestobnb + 1; }
                    if (depression[rowlowestobnb + 1, collowestobnb] == -this_depression) { rowlowestobnb = rowlowestobnb + 1; }
                    if (depression[rowlowestobnb + 1, collowestobnb - 1] == -this_depression) { rowlowestobnb = rowlowestobnb + 1; collowestobnb = collowestobnb - 1; }
                    if (depression[rowlowestobnb, collowestobnb + 1] == -this_depression) { collowestobnb = collowestobnb + 1; }
                    if (depression[rowlowestobnb, collowestobnb - 1] == -this_depression) { collowestobnb = collowestobnb - 1; }
                    if (depression[rowlowestobnb - 1, collowestobnb + 1] == -this_depression) { rowlowestobnb = rowlowestobnb - 1; collowestobnb = collowestobnb + 1; }
                    if (depression[rowlowestobnb - 1, collowestobnb] == -this_depression) { rowlowestobnb = rowlowestobnb - 1; }
                    if (depression[rowlowestobnb - 1, collowestobnb - 1] == -this_depression) { rowlowestobnb = rowlowestobnb - 1; collowestobnb = collowestobnb - 1; }
                }
                if (deltasize > 0 && depression[rowlowestobnb, collowestobnb] == -this_depression)
                {
                    //no action needed, just reporting
                    if (diagnostic_mode == 1) { Debug.WriteLine(" no action needed, lowestobnb is part of the delta " + rowlowestobnb + " " + collowestobnb + " lake " + depression[rowlowestobnb, collowestobnb]); }
                }
            }
            if (diagnostic_mode == 1) { Debug.WriteLine(" sed_for_delta is now " + available_for_delta_m + " and deltasize = " + deltasize); }
            //diagnostic_mode = 0;
        }

        #endregion

    }
}
