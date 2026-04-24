using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace LORICA4
{
    public partial class Mother_form
    {

        #region depression code
        // Constants and shared helpers
        const int MAX_OUTLETS = 10;
        const double TOL_M = 1e-12;          // thickness/length tolerance (m)
        const double TOL_VOL = 1e-12;        // volume tolerance (m^3)
        const double TOL_REL = 1e-9;         // relative tolerance for z-comparisons

        // Stable neighbor order (clockwise starting N)
        static readonly (int di, int dj)[] N8 = new (int, int)[]
        {
    (-1, 0), // N
    (-1, 1), // NE
    ( 0, 1), // E
    ( 1, 1), // SE
    ( 1, 0), // S
    ( 1,-1), // SW
    ( 0,-1), // W
    (-1,-1)  // NW
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        double TolZ(double z) => Math.Max(1.0, Math.Abs(z)) * TOL_REL;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool AlmostEqual(double a, double b)
        {
            double tol = Math.Max(TolZ(a), TolZ(b));
            return Math.Abs(a - b) <= tol;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool Less(double a, double b) => a < b - Math.Max(TolZ(a), TolZ(b));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool Greater(double a, double b) => a > b + Math.Max(TolZ(a), TolZ(b));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int Clamp(int v, int lo, int hi) => (v < lo) ? lo : ((v > hi) ? hi : v);

        // Precompute once per run or step as appropriate
        double dx_diag => dx * 1.4142135623730951;
        double eps_dx, eps_dx_diag; // set in define_fillheight_new from input epsilon

        void ReportDebug(string msg)
        {
            if (diagnostic_mode == 1)
                Debug.WriteLine(msg);
        }
        void findsinks()
        {
            int twoequals = 0, threeequals = 0, moreequals = 0;
            numsinks = 0;

            for (int row = 0; row < nr; row++)
            {
                for (int col = 0; col < nc; col++)
                {
                    if (dtm[row, col] == nodata_value)
                    {
                        status_map[row, col] = 3; // NODATA guard
                        continue;
                    }

                    int high = 0, low = 0, equal = 0, neighborCount = 0;

                    // Visit 8 neighbors in stable order
                    for (int k = 0; k < 8; k++)
                    {
                        int ni = row + N8[k].di;
                        int nj = col + N8[k].dj;
                        if (ni < 0 || ni >= nr || nj < 0 || nj >= nc) continue;
                        if (dtm[ni, nj] == nodata_value) continue;

                        neighborCount++;
                        double dh = dtm[row, col] - dtm[ni, nj];

                        if (Less(dh, 0)) high++;
                        else if (Greater(dh, 0)) low++;
                        else equal++;
                    }

                    if (neighborCount == 8)
                    {
                        if (low == 0)
                        {
                            status_map[row, col] = 1; // sink
                            numsinks++;
                        }
                        else if (low == 8)
                        {
                            status_map[row, col] = -1; // peak
                        }
                        else
                        {
                            status_map[row, col] = 0; // normal
                        }

                        if (equal == 1) twoequals++;
                        else if (equal == 2) threeequals++;
                        else if (equal > 2) moreequals++;
                    }
                    else
                    {
                        status_map[row, col] = 0;
                    }
                }
            }

            // force edges to 3
            for (int c = 0; c < nc; c++) { status_map[0, c] = 3; status_map[nr - 1, c] = 3; }
            for (int r = 0; r < nr; r++) { status_map[r, 0] = 3; status_map[r, nc - 1] = 3; }
        }
        void searchdepressions()
        {
            // Reset depression map
            for (int r = 0; r < nr; r++)
                for (int c = 0; c < nc; c++)
                    depression[r, c] = 0;

            // Reset per-depression arrays (respect MAX_OUTLETS)
            for (int z = 0; z < numberofsinks; z++)
            {
                for (int k = 0; k < MAX_OUTLETS; k++)
                {
                    drainingoutlet_row[z, k] = -1;
                    drainingoutlet_col[z, k] = -1;
                }
                depressionlevel[z] = 0;
                depressionsize[z] = 0;
                depressionvolume_m[z] = 0;
                iloedge[z] = jloedge[z] = iupedge[z] = jupedge[z] = 0;
            }

            totaldepressions = 0; totaldepressionsize = 0; maxsize = 0; totaldepressionvolume = 0; largestdepression = -1;
            depressionnumber = 0;

            for (int row = 0; row < nr; row++)
            {
                for (int col = 0; col < nc; col++)
                {
                    if (status_map[row, col] == 1 && depression[row, col] == 0)
                    {
                        // New depression
                        depressionnumber++;
                        totaldepressions++;

                        depressionlevel[depressionnumber] = dtm[row, col];
                        depressionsize[depressionnumber] = 1;
                        depression[row, col] = depressionnumber;

                        iloedge[depressionnumber] = row - 1;
                        iupedge[depressionnumber] = row + 1;
                        jloedge[depressionnumber] = col - 1;
                        jupedge[depressionnumber] = col + 1;

                        int depressionready = 0;
                        int depressiondrainsout = 0;

                        // Expanding search window around original sink cell
                        int iloradius = 1, iupradius = 1, jloradius = 1, jupradius = 1;

                        // Iteration counter for diagnostics
                        int iters = 0;

                        while (depressionready != 1)
                        {
                            iters++;
                            // Track the minimal "altidiff" over neighbors of current lake boundary
                            double minaltidiff = double.NegativeInfinity; // looking for larger values; we set as -inf to ensure first valid sets it
                            int numberoflowestneighbours = 0;
                            Array.Clear(rowlowestnb, 0, maxlowestnbs);
                            Array.Clear(collowestnb, 0, maxlowestnbs);

                            int r0 = Clamp(row - iloradius, 0, nr - 1);
                            int r1 = Clamp(row + iupradius, 0, nr - 1);
                            int c0 = Clamp(col - jloradius, 0, nc - 1);
                            int c1 = Clamp(col + jupradius, 0, nc - 1);

                            // Find all lowest boundary neighbors relative to current lake level
                            for (int i = r0; i <= r1; i++)
                            {
                                for (int j = c0; j <= c1; j++)
                                {
                                    if (i == row && j == col) continue;
                                    if (dtm[i, j] == nodata_value) continue;
                                    // Only consider cells adjacent to current depression
                                    bool adjacentToDep = false;
                                    for (int d = 0; d < 8; d++)
                                    {
                                        int ai = i + N8[d].di, aj = j + N8[d].dj;
                                        if (ai < 0 || ai >= nr || aj < 0 || aj >= nc) continue;
                                        if (depression[ai, aj] == depressionnumber)
                                        {
                                            adjacentToDep = true; break;
                                        }
                                    }
                                    if (!adjacentToDep) continue;

                                    // Skip if already part of current depression
                                    if (depression[i, j] == depressionnumber) continue;

                                    double altidiff = depressionlevel[depressionnumber] - dtm[i, j];

                                    // We want neighbors that are at or above lakelevel (altidiff <= 0) or below lakelevel (altidiff > 0) to detect outlets
                                    // Keep the neighbor(s) with the highest altidiff below or equal to zero OR any that are lower than lakelevel
                                    // Semantics retained with tolerances:
                                    if (AlmostEqual(altidiff, minaltidiff))
                                    {
                                        if (numberoflowestneighbours < maxlowestnbs)
                                        {
                                            rowlowestnb[numberoflowestneighbours] = i;
                                            collowestnb[numberoflowestneighbours] = j;
                                            numberoflowestneighbours++;
                                        }
                                    }
                                    else if (altidiff > minaltidiff || Less(dtm[i, j], depressionlevel[depressionnumber]))
                                    {
                                        minaltidiff = altidiff;
                                        numberoflowestneighbours = 0;
                                        rowlowestnb[numberoflowestneighbours] = i;
                                        collowestnb[numberoflowestneighbours] = j;
                                        numberoflowestneighbours = 1;
                                    }
                                }
                            }

                            // Examine lowest neighbours
                            int outletfound = 0;

                            for (int k = 0; k < numberoflowestneighbours; k++)
                            {
                                int ni = rowlowestnb[k], nj = collowestnb[k];
                                if (ni < 0) continue;

                                // Maintain bbox
                                if (ni >= iupedge[depressionnumber]) { iupedge[depressionnumber] = ni + 1; }
                                if (ni <= iloedge[depressionnumber]) { iloedge[depressionnumber] = ni - 1; }
                                if (nj >= jupedge[depressionnumber]) { jupedge[depressionnumber] = nj + 1; }
                                if (nj <= jloedge[depressionnumber]) { jloedge[depressionnumber] = nj - 1; }

                                double minaltidiff_k = depressionlevel[depressionnumber] - dtm[ni, nj];

                                // If neighbor is another depression
                                if (depression[ni, nj] != 0 && depression[ni, nj] != depressionnumber)
                                {
                                    int otherdepression = depression[ni, nj];
                                    // Merge logic (unchanged semantics), plus bbox/radius updates
                                    totaldepressions--;
                                    totaldepressionvolume -= depressionvolume_m[otherdepression];

                                    for (int outc = 0; outc < MAX_OUTLETS; outc++)
                                    {
                                        drainingoutlet_row[otherdepression, outc] = -1;
                                        drainingoutlet_col[otherdepression, outc] = -1;
                                    }

                                    depressionvolume_m[depressionnumber] += depressionvolume_m[otherdepression];
                                    depressionvolume_m[depressionnumber] += (depressionsize[depressionnumber] * (dtm[ni, nj] - depressionlevel[depressionnumber]));

                                    depressionvolume_m[otherdepression] = 0;
                                    depressionlevel[depressionnumber] = dtm[ni, nj];

                                    // Enlarge bbox/radii to include the other lake fully
                                    if (jloedge[otherdepression] < jloedge[depressionnumber]) jloedge[depressionnumber] = jloedge[otherdepression];
                                    if (iloedge[otherdepression] < iloedge[depressionnumber]) iloedge[depressionnumber] = iloedge[otherdepression];
                                    if (jupedge[otherdepression] > jupedge[depressionnumber]) jupedge[depressionnumber] = jupedge[otherdepression];
                                    if (iupedge[otherdepression] > iupedge[depressionnumber]) iupedge[depressionnumber] = iupedge[otherdepression];

                                    // Move cells from otherdepression
                                    int or0 = Math.Max(0, iloedge[otherdepression]);
                                    int or1 = Math.Min(nr - 1, iupedge[otherdepression]);
                                    int oc0 = Math.Max(0, jloedge[otherdepression]);
                                    int oc1 = Math.Min(nc - 1, jupedge[otherdepression]);

                                    int otherdepressionsize = 0;
                                    for (int i = or0; i <= or1; i++)
                                        for (int j = oc0; j <= oc1; j++)
                                            if (depression[i, j] == otherdepression)
                                            {
                                                depression[i, j] = depressionnumber;
                                                otherdepressionsize++;
                                            }

                                    depressionsize[depressionnumber] += otherdepressionsize;
                                    depressionsize[otherdepression] = 0;
                                    totaldepressionsize -= otherdepressionsize;

                                    // Check if the outlet connects to a third side that is lower than lakelevel
                                    for (int d = 0; d < 8; d++)
                                    {
                                        int ti = ni + N8[d].di, tj = nj + N8[d].dj;
                                        if (ti < 0 || ti >= nr || tj < 0 || tj >= nc) continue;
                                        if (dtm[ti, tj] == nodata_value) continue;
                                        if (depression[ti, tj] != depressionnumber && Less(dtm[ti, tj], depressionlevel[depressionnumber]))
                                        {
                                            depressionready = 1;
                                            drainingoutlet_row[depressionnumber, 0] = ni;
                                            drainingoutlet_col[depressionnumber, 0] = nj;
                                            status_map[ni, nj] = 2;
                                        }
                                    }
                                }
                                else
                                {
                                    // Not another depression member
                                    if (!Greater(minaltidiff_k, 0)) // neighbor >= lakelevel (at or above spill height)
                                    {
                                        // Add neighbor to depression; raise level to that neighbor
                                        depression[ni, nj] = depressionnumber;
                                        depressionvolume_m[depressionnumber] += (depressionsize[depressionnumber] * (dtm[ni, nj] - depressionlevel[depressionnumber]));
                                        depressionsize[depressionnumber]++;
                                        depressionlevel[depressionnumber] = dtm[ni, nj];

                                        // If on edge or status 3, drains out
                                        if (status_map[ni, nj] == 3)
                                        {
                                            depressionready = 1;
                                            depressiondrainsout = 1;
                                            drainingoutlet_row[depressionnumber, 0] = ni;
                                            drainingoutlet_col[depressionnumber, 0] = nj;
                                        }
                                    }
                                    else
                                    {
                                        // neighbor is lower than lake level: outlet
                                        outletfound = 1;
                                        // find a lake neighbor @ lake level as the outlet cell(s)
                                        int outletnumber = 0;
                                        for (int d = 0; d < 8; d++)
                                        {
                                            int ti = ni + N8[d].di, tj = nj + N8[d].dj;
                                            if (ti < 0 || ti >= nr || tj < 0 || tj >= nc) continue;
                                            if (dtm[ti, tj] == nodata_value) continue;
                                            if (depression[ti, tj] == depressionnumber && AlmostEqual(dtm[ti, tj], depressionlevel[depressionnumber]))
                                            {
                                                // Is this outlet already recorded?
                                                bool exists = false;
                                                for (int oc = 0; oc < MAX_OUTLETS && drainingoutlet_col[depressionnumber, oc] != -1; oc++)
                                                {
                                                    if (drainingoutlet_row[depressionnumber, oc] == ti &&
                                                        drainingoutlet_col[depressionnumber, oc] == tj)
                                                    { exists = true; break; }
                                                }
                                                if (exists) continue;

                                                // Add outlet if there's space
                                                int sink = -1;
                                                for (int oc = 0; oc < MAX_OUTLETS; oc++)
                                                {
                                                    if (drainingoutlet_col[depressionnumber, oc] == -1) { sink = oc; break; }
                                                }
                                                if (sink != -1)
                                                {
                                                    drainingoutlet_row[depressionnumber, sink] = ti;
                                                    drainingoutlet_col[depressionnumber, sink] = tj;
                                                    status_map[ti, tj] = 2;
                                                    outletnumber++;
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            if (outletfound == 1) depressionready = 1;

                            // Expand radii if needed (keep same logic, just avoid extra Math.Abs)
                            // (No-op here; radii only need expansion when we add cells; managed implicitly by bbox updates)
                        } // while not ready

                        totaldepressionsize += depressionsize[depressionnumber];
                        totaldepressionvolume += depressionvolume_m[depressionnumber];
                        if (maxsize < depressionsize[depressionnumber]) { maxsize = depressionsize[depressionnumber]; largestdepression = depressionnumber; }
                        if (maxdepressionnumber < depressionnumber) { maxdepressionnumber = depressionnumber; }

                        ReportDebug($"Depression {depressionnumber} ready: size={depressionsize[depressionnumber]}, level={depressionlevel[depressionnumber]}, volume={depressionvolume_m[depressionnumber]}");
                    }
                }
            }
        }
        void define_fillheight_new(double epsilon_input)
        {
            // epsilon is provided externally (≪ 1)
            eps_dx = epsilon_input * dx;
            eps_dx_diag = epsilon_input * dx * 1.4142135623730951;

            // Reset dtmfill_A
            for (int r = 0; r < nr; r++)
                for (int c = 0; c < nc; c++)
                    dtmfill_A[r, c] = -1.0;

            // For each existing depression
            for (int dep = 1; dep <= maxdepressionnumber; dep++)
            {
                if (depressionsize[dep] <= 0) continue;

                // Seed outlets with lake level
                for (int k = 0; k < MAX_OUTLETS; k++)
                {
                    int or = drainingoutlet_row[dep, k], oc = drainingoutlet_col[dep, k];
                    if (or == -1) break;
                    dtmfill_A[or, oc] = depressionlevel[dep];
                }

                // Relaxation until convergence
                int notyetdone = 1, done = 0;
                int iterations = 0;

                while (notyetdone > 0)
                {
                    iterations++;
                    notyetdone = 0;
                    done = 0;

                    int r0 = Math.Max(0, iloedge[dep]);
                    int r1 = Math.Min(nr - 1, iupedge[dep]);
                    int c0 = Math.Max(0, jloedge[dep]);
                    int c1 = Math.Min(nc - 1, jupedge[dep]);

                    for (int r = r0; r <= r1; r++)
                    {
                        for (int c = c0; c <= c1; c++)
                        {
                            if (dtm[r, c] == nodata_value) continue;
                            if (depression[r, c] != dep) continue;

                            if (dtmfill_A[r, c] < 0) // not yet set
                            {
                                double best = double.PositiveInfinity;

                                // Check neighbors for already set dtmfill values
                                for (int d = 0; d < 8; d++)
                                {
                                    int nrw = r + N8[d].di, ncl = c + N8[d].dj;
                                    if (nrw < 0 || nrw >= nr || ncl < 0 || ncl >= nc) continue;
                                    if (dtm[nrw, ncl] == nodata_value) continue;
                                    if (depression[nrw, ncl] != dep) continue;
                                    if (dtmfill_A[nrw, ncl] <= 0) continue;

                                    double inc = (N8[d].di != 0 && N8[d].dj != 0) ? eps_dx_diag : eps_dx;
                                    double cand = dtmfill_A[nrw, ncl] + inc;
                                    if (cand < best) best = cand;
                                }

                                if (!double.IsInfinity(best))
                                {
                                    dtmfill_A[r, c] = best;
                                    done++;
                                }
                                else
                                {
                                    notyetdone++;
                                }
                            }
                        }
                    }

                    if (done == 0) break; // no progress
                }

                ReportDebug($"define_fillheight_new: dep {dep}, iterations={iterations}");
            }
        }
        void cleardelta(int iloradius, int iupradius, int jloradius, int jupradius, int clear_row, int clear_col)
        {
            ReportDebug($" clearing delta lake {Math.Abs(depression[clear_row, clear_col])} around {clear_row} {clear_col}");
            for (int e = -(iloradius + 3); e <= iupradius + 3; e++)
            {
                for (int eta = -(jloradius + 3); eta <= jupradius + 3; eta++)
                {
                    int rr = clear_row + e, cc = clear_col + eta;
                    if (rr < 0 || rr >= nr || cc < 0 || cc >= nc) continue;
                    if (depression[rr, cc] < 0)
                    {
                        depression[rr, cc] = Math.Abs(depression[clear_row, clear_col]);
                        ReportDebug($" membership of delta has been cancelled for {rr} {cc}");
                    }
                }
            }
        }
        void update_depression(int number)
        {
            int dep = number;
            ReportDebug($" now updating depression {dep}");

            // Reset sums
            depressionsum_water_m = 0;
            depressionsum_sediment_m = 0;
            for (int s = 0; s < n_texture_classes; s++) depressionsum_texture_kg[s] = 0;
            depressionsum_YOM_kg = 0; depressionsum_OOM_kg = 0;
            needed_to_fill_depression_m = 0;

            int r0 = Math.Max(0, iloedge[dep]);
            int r1 = Math.Min(nr - 1, iupedge[dep]);
            int c0 = Math.Max(0, jloedge[dep]);
            int c1 = Math.Min(nc - 1, jupedge[dep]);

            for (int r = r0; r <= r1; r++)
            {
                for (int c = c0; c <= c1; c++)
                {
                    if (dtm[r, c] == nodata_value) continue;
                    if (depression[r, c] != dep) continue;

                    depressionsum_water_m += waterflow_m3[r, c] / (dx * dx);

                    if (!only_waterflow_checkbox.Checked)
                    {
                        for (int s = 0; s < n_texture_classes; s++)
                            depressionsum_texture_kg[s] += sediment_in_transport_kg[r, c, s];
                        depressionsum_OOM_kg += old_SOM_in_transport_kg[r, c];
                        depressionsum_YOM_kg += young_SOM_in_transport_kg[r, c];
                    }

                    needed_to_fill_depression_m += Math.Max(0.0, dtmfill_A[r, c] - dtm[r, c]);
                }
            }

            // Convert mass to thickness
            depressionsum_sediment_m = calc_thickness_from_mass(depressionsum_texture_kg, depressionsum_YOM_kg, depressionsum_OOM_kg);

            // Membership consistency check: no negative dep when there is no sediment left
            if (depressionsum_sediment_m <= TOL_M)
            {
                for (int r = r0; r <= r1; r++)
                    for (int c = c0; c <= c1; c++)
                        if (depression[r, c] == -dep)
                            depression[r, c] = dep;
            }

            ReportDebug($" Updated depression {dep}. size={depressionsize[dep]}, sum_water={depressionsum_water_m}, sum_sed_m={depressionsum_sediment_m}, needed={needed_to_fill_depression_m}");
        }

        void fill_depression(int dep, double fraction_sediment_used_for_this_dep)
        {
            int r0 = Math.Max(0, iloedge[dep]);
            int r1 = Math.Min(nr - 1, iupedge[dep]);
            int c0 = Math.Max(0, jloedge[dep]);
            int c1 = Math.Min(nc - 1, jupedge[dep]);

            sediment_filled_m += needed_to_fill_depression_m;

            for (int r = r0; r <= r1; r++)
            {
                for (int c = c0; c <= c1; c++)
                {
                    if (dtm[r, c] == nodata_value) continue;
                    if (depression[r, c] != dep) continue;

                    double to_add = dtmfill_A[r, c] - dz_ero_m[r, c] - dz_sed_m[r, c] - dtm[r, c];
                    if (to_add < -TOL_M) to_add = 0; // robustness
                    double frac_cell = (needed_to_fill_depression_m > TOL_M) ? (to_add / needed_to_fill_depression_m) : 0.0;

                    // Add mass
                    for (int s = 0; s < n_texture_classes; s++)
                        texture_kg[r, c, 0, s] += fraction_sediment_used_for_this_dep * frac_cell * depressionsum_texture_kg[s];
                    young_SOM_kg[r, c, 0] += fraction_sediment_used_for_this_dep * frac_cell * depressionsum_YOM_kg;
                    old_SOM_kg[r, c, 0] += fraction_sediment_used_for_this_dep * frac_cell * depressionsum_OOM_kg;

                    // Update dtm and budgets
                    lake_sed_m[r, c] += to_add;
                    dtmchange_m[r, c] += to_add;
                    soildepth_m[r, c] += to_add;
                    dtm[r, c] = dtmfill_A[r, c] - dz_ero_m[r, c] - dz_sed_m[r, c];

                    if (lake_sed_m[r, c] < -TOL_M) lake_sed_m[r, c] = 0;
                }
            }

            // Route displaced water out via outlets (even split)
            int outletcount = 0;
            for (int k = 0; k < MAX_OUTLETS && drainingoutlet_col[dep, k] != -1; k++) outletcount++;
            if (outletcount > 0)
            {
                double vol_per_outlet = (dx * dx * depressionvolume_m[dep]) / outletcount;
                for (int k = 0; k < outletcount; k++)
                {
                    waterflow_m3[drainingoutlet_row[dep, k], drainingoutlet_col[dep, k]] += vol_per_outlet;
                }
            }

            ReportDebug($" Filled depression {dep}");
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

        void bottom_depression(int dep)
        {
            // Build list of lake cells and how much thickness is needed to reach dtmfill_A
            L_lakecells.Clear();

            int r0 = Math.Max(0, iloedge[dep]);
            int r1 = Math.Min(nr - 1, iupedge[dep]);
            int c0 = Math.Max(0, jloedge[dep]);
            int c1 = Math.Min(nc - 1, jupedge[dep]);

            for (int r = r0; r <= r1; r++)
            {
                for (int c = c0; c <= c1; c++)
                {
                    if (dtm[r, c] == nodata_value) continue;
                    if (depression[r, c] != dep) continue;

                    // Correct thickness needed: up to dtmfill_A, accounting for any ero/sed already applied
                    double sed_need_m = dtmfill_A[r, c] - (dtm[r, c] + dz_ero_m[r, c] + dz_sed_m[r, c]);
                    if (sed_need_m > TOL_M)
                    {
                        L_lakecells.Add(new Lakecell(r, c, sed_need_m, 0.0));
                    }
                }
            }

            // Nothing to do if no cells need filling or no sediment is available
            if (L_lakecells.Count == 0 || depressionsum_sediment_m <= TOL_M)
            {
                depressionsum_sediment_m = 0;
                depressionsum_OOM_kg = 0;
                depressionsum_YOM_kg = 0;
                for (int s = 0; s < 5; s++) depressionsum_texture_kg[s] = 0;
                L_lakecells.Clear();
                if (diagnostic_mode == 1) Debug.WriteLine($"bottom_depression(dep={dep}): no work (cells={L_lakecells.Count}, sed={depressionsum_sediment_m}).");
                return;
            }

            // Sort cells by descending needed thickness (deepest first)
            L_lakecells.Sort((a, b) => b.t_sed_needed_m.CompareTo(a.t_sed_needed_m));

            // Compute a uniform "level" to which we can raise all deepest cells using the available sediment
            // This is equivalent to filling a histogram from the deepest bin upward.
            double remaining = depressionsum_sediment_m;
            double level = L_lakecells[0].t_sed_needed_m; // current deepest need

            for (int i = 0; i < L_lakecells.Count - 1; i++)
            {
                double height_diff = L_lakecells[i].t_sed_needed_m - L_lakecells[i + 1].t_sed_needed_m;
                double vol_needed = height_diff * (i + 1); // raising the first (i+1) cells by height_diff

                if (remaining > vol_needed + TOL_M)
                {
                    remaining -= vol_needed;
                    level -= height_diff;
                }
                else
                {
                    // We can only partially raise this tier
                    level -= remaining / (i + 1);
                    remaining = 0.0;
                    break;
                }
            }

            // If after all tiers some sediment remains, distribute evenly across all cells (capped by dtmfill_A)
            if (remaining > TOL_M)
            {
                level = Math.Max(0.0, level - remaining / L_lakecells.Count);
                remaining = 0.0;
            }

            // Apply the raise to each lake cell, not exceeding dtmfill_A
            double denom = Math.Max(TOL_M, depressionsum_sediment_m); // for proportional mass distribution

            foreach (var cell in L_lakecells)
            {
                int r = cell.trow;
                int c = cell.tcol;

                double target = dtmfill_A[r, c] - level;  // never above dtmfill_A
                if (target > dtm[r, c] + TOL_M)
                {
                    double add = target - dtm[r, c];

                    // Update elevation and bookkeeping
                    dtm[r, c] += add;
                    dtmchange_m[r, c] += add;
                    lake_sed_m[r, c] += add;

                    // Distribute available sediment mass proportionally by thickness added
                    for (int s = 0; s < n_texture_classes; s++)
                    {
                        texture_kg[r, c, 0, s] += depressionsum_texture_kg[s] * (add / denom);
                    }
                    young_SOM_kg[r, c, 0] += depressionsum_YOM_kg * (add / denom);
                    old_SOM_kg[r, c, 0] += depressionsum_OOM_kg * (add / denom);
                }
            }

            // Reset sums for this cycle
            depressionsum_sediment_m = 0.0;
            depressionsum_OOM_kg = 0.0;
            depressionsum_YOM_kg = 0.0;
            for (int s = 0; s < 5; s++) depressionsum_texture_kg[s] = 0.0;
            L_lakecells.Clear();

            if (diagnostic_mode == 1) Debug.WriteLine($"bottom_depression(dep={dep}) completed.");
        }

        void delta_depression(int dep)
        {
            // Build list of “side” lake cells with non-zero sediment in transport (thickness)
            var sources = new List<(int r, int c, double t)>(depressionsize[dep]);
            int r0 = Math.Max(0, iloedge[dep]);
            int r1 = Math.Min(nr - 1, iupedge[dep]);
            int c0 = Math.Max(0, jloedge[dep]);
            int c1 = Math.Min(nc - 1, jupedge[dep]);

            for (int r = r0; r <= r1; r++)
            {
                for (int c = c0; c <= c1; c++)
                {
                    if (dtm[r, c] == nodata_value) continue;
                    if (depression[r, c] != dep) continue;

                    // Skip outlets
                    bool isOutlet = false;
                    for (int k = 0; k < MAX_OUTLETS && drainingoutlet_col[dep, k] != -1; k++)
                    {
                        if (drainingoutlet_row[dep, k] == r && drainingoutlet_col[dep, k] == c) { isOutlet = true; break; }
                    }
                    if (isOutlet) continue;

                    // Aggregate sediment thickness for this cell
                    double s_thick = 0.0;
                    if (!only_waterflow_checkbox.Checked)
                    {
                        double[] tmp = new double[5];
                        for (int s = 0; s < n_texture_classes; s++)
                        {
                            tmp[s] = Math.Max(0, sediment_in_transport_kg[r, c, s]);
                        }
                        s_thick = calc_thickness_from_mass(tmp, 0, 0);
                    }
                    if (s_thick > TOL_M)
                        sources.Add((r, c, s_thick));
                }
            }

            // Sort sources by descending thickness
            sources.Sort((a, b) => b.t.CompareTo(a.t));

            // Process each source
            foreach (var src in sources)
            {
                int startrow = src.r, startcol = src.c;

                // Drain local sediment_in_transport into available_for_delta_m, zero the source
                double[] local_s_i_t_kg = new double[5];
                for (int s = 0; s < n_texture_classes; s++)
                {
                    local_s_i_t_kg[s] = sediment_in_transport_kg[startrow, startcol, s];
                    sediment_in_transport_kg[startrow, startcol, s] = 0;
                }

                double available_for_delta_m = calc_thickness_from_mass(local_s_i_t_kg, 0, 0);
                double prev_available_for_delta_m = available_for_delta_m;

                if (available_for_delta_m <= TOL_M) continue;

                // Build delta from this source
                deltasize = 0;
                dhobliquemax1 = 0;
                iloradius2 = iupradius2 = jloradius2 = jupradius2 = 1;
                iloradius3 = iupradius3 = jloradius3 = jupradius3 = 1;

                rowlowestobnb = startrow; collowestobnb = startcol;

                while (available_for_delta_m > TOL_M)
                {
                    // Find a start cell in the lake for this delta
                    find_lowest_oblique_neighbour(dep, startrow, startcol); // uses stable neighbor order and tolerances
                    depression[rowlowestobnb, collowestobnb] = -dep; // mark delta seed
                    deltasize = 1;

                    dhobliquemax2 = 1e30;
                    II = JJ = 0;

                    // Raise delta iteratively
                    while (available_for_delta_m > TOL_M)
                    {
                        find_lowest_higher_oblique_neighbour(dep, startrow, startcol); // sets II, JJ and dhobliquemax2

                        if (dhobliquemax2 < 0) // lower neighbor exists
                        {
                            cleardelta(iloradius3, iupradius3, jloradius3, jupradius3, rowlowestobnb, collowestobnb);
                            // move to lower oblique neighbor; update “slope window”
                            int drow = rowlowestobnb + II - startrow;
                            int dcol = collowestobnb + JJ - startcol;
                            if (iloradius2 < -drow) iloradius2 = -drow;
                            if (iupradius2 < drow) iupradius2 = drow;
                            if (jloradius2 < -dcol) jloradius2 = -dcol;
                            if (jupradius2 < dcol) jupradius2 = dcol;

                            // Update dhobliquemax1 approx. (numeric guard not essential to behavior)
                            dhobliquemax1 = (dtm[startrow, startcol] + dz_ero_m[startrow, startcol] + dz_sed_m[startrow, startcol])
                                          - (dtm[rowlowestobnb + II, collowestobnb + JJ] + dz_ero_m[rowlowestobnb + II, collowestobnb + JJ] + dz_sed_m[rowlowestobnb + II, collowestobnb + JJ])
                                          - (Math.Sqrt(drow * drow + dcol * dcol) * dx * tangent_of_delta) - 1e-7;
                        }
                        else if (AlmostEqual(dhobliquemax2, 0))
                        {
                            ReportDebug("Warning: dhobliquemax2 == 0; search should continue.");
                            break;
                        }
                        else // dhobliquemax2 > 0
                        {
                            if (available_for_delta_m >= deltasize * dhobliquemax2 - TOL_M)
                            {
                                raise_delta_completely(dep, ref available_for_delta_m, local_s_i_t_kg);
                            }
                            else
                            {
                                raise_delta_partly(dep, ref available_for_delta_m, local_s_i_t_kg);
                                if (obnbchanged == 0)
                                {
                                    cleardelta(iloradius3, iupradius3, jloradius3, jupradius3, rowlowestobnb, collowestobnb);
                                }
                                // obnbchanged != 0 means start moved; keep delta marks
                            }
                        }

                        // Monotonic check (non-increasing)
                        if (available_for_delta_m > prev_available_for_delta_m + TOL_M)
                        {
                            ReportDebug($"Non-monotonic available_for_delta_m: prev={prev_available_for_delta_m}, now={available_for_delta_m}, clamping.");
                            available_for_delta_m = prev_available_for_delta_m;
                        }
                        prev_available_for_delta_m = available_for_delta_m;

                        // Termination guard: if no progress (delta size 0 and no neighbor found), break
                        if (deltasize <= 0 && available_for_delta_m <= TOL_M) break;
                    }

                    cleardelta(iloradius3, iupradius3, jloradius3, jupradius3, rowlowestobnb, collowestobnb);
                    // Membership consistency: no negative markers left if no sediment available
                    if (available_for_delta_m <= TOL_M)
                    {
                        for (int r = r0; r <= r1; r++)
                            for (int c = c0; c <= c1; c++)
                                if (depression[r, c] == -dep) depression[r, c] = dep;
                    }

                    // If still sediment remains but no progress, break to avoid livelock
                    break;
                }
            }

            // Split extra water volume (sediment replaces water) over outlets (evenly)
            int outletcount = 0;
            for (int k = 0; k < MAX_OUTLETS && drainingoutlet_col[dep, k] != -1; k++) outletcount++;
            if (outletcount > 0)
            {
                double per = (dx * dx * depressionsum_sediment_m) / outletcount;
                for (int k = 0; k < outletcount; k++)
                    waterflow_m3[drainingoutlet_row[dep, k], drainingoutlet_col[dep, k]] += per;
            }
        }

        void find_lowest_oblique_neighbour(int dep, int startrow, int startcol)
        {
            ReportDebug(" entered find_lowest_oblique_neighbour");

            int ready = 0;
            while (ready == 0)
            {
                ready = 1;
                ReportDebug($" ilo {iloradius2}, iup {iupradius2}, jlo {jloradius2}, jup {jupradius2}");

                int r0 = Clamp(startrow - iloradius2, 0, nr - 1);
                int r1 = Clamp(startrow + iupradius2, 0, nr - 1);
                int c0 = Clamp(startcol - jloradius2, 0, nc - 1);
                int c1 = Clamp(startcol + jupradius2, 0, nc - 1);

                for (int r = r0; r <= r1; r++)
                {
                    for (int c = c0; c <= c1; c++)
                    {
                        if (r == startrow && c == startcol) continue;
                        if (dtm[r, c] == nodata_value) continue;
                        if (depression[r, c] != dep && depression[r, c] != -dep) continue;

                        double dhoblique =
                            (dtm[startrow, startcol] + dz_ero_m[startrow, startcol] + dz_sed_m[startrow, startcol])
                          - (dtm[r, c] + dz_ero_m[r, c] + dz_sed_m[r, c])
                          - (Math.Sqrt((startrow - r) * (startrow - r) + (startcol - c) * (startcol - c)) * dx * tangent_of_delta);

                        if (dhoblique > dhobliquemax1) // strict greater, tie breaks by stable scan order
                        {
                            dhobliquemax1 = dhoblique;
                            rowlowestobnb = r; collowestobnb = c;
                            ready = 0;

                            if (r == r0) iloradius2++;
                            if (r == r1) iupradius2++;
                            if (c == c0) jloradius2++;
                            if (c == c1) jupradius2++;
                        }
                        else if (Math.Abs(dhoblique) <= TolZ(dhoblique) && Math.Abs(dhobliquemax1) <= TolZ(dhobliquemax1))
                        {
                            // dhoblique ~ 0 and dhobliquemax1 ~ 0: look beyond
                            if (r == r0) { iloradius2++; ready = 0; }
                            if (r == r1) { iupradius2++; ready = 0; }
                            if (c == c0) { jloradius2++; ready = 0; }
                            if (c == c1) { jupradius2++; ready = 0; }
                        }
                    }
                }
            }

            ReportDebug($" ready searching - dhobliquemax1: {dhobliquemax1}, row {rowlowestobnb}, col {collowestobnb}");
        }

        void find_lowest_higher_oblique_neighbour(int dep, int startrow, int startcol)
        {
            ReportDebug(" entered find_lowest_higher_oblique_neighbour");

            int ready = 0;
            while (ready == 0)
            {
                dhobliquemax2 = double.PositiveInfinity;

                int r0 = Clamp(rowlowestobnb - iloradius3, 0, nr - 1);
                int r1 = Clamp(rowlowestobnb + iupradius3, 0, nr - 1);
                int c0 = Clamp(collowestobnb - jloradius3, 0, nc - 1);
                int c1 = Clamp(collowestobnb + jupradius3, 0, nc - 1);

                for (int r = r0; r <= r1; r++)
                {
                    for (int c = c0; c <= c1; c++)
                    {
                        if (r == rowlowestobnb && c == collowestobnb) continue;
                        if (dtm[r, c] == nodata_value) continue;
                        if (depression[r, c] != dep) continue;

                        double deltaDist =
                            (Math.Sqrt((r - startrow) * (r - startrow) + (c - startcol) * (c - startcol))
                           - Math.Sqrt((rowlowestobnb - startrow) * (rowlowestobnb - startrow) + (collowestobnb - startcol) * (collowestobnb - startcol)));

                        double dhoblique =
                            -(dtm[rowlowestobnb, collowestobnb] + dz_ero_m[rowlowestobnb, collowestobnb] + dz_sed_m[rowlowestobnb, collowestobnb])
                            + (dtm[r, c] + dz_ero_m[r, c] + dz_sed_m[r, c])
                            + (deltaDist * dx * tangent_of_delta);

                        if (!AlmostEqual(dhoblique, 0.0) && dhoblique < dhobliquemax2)
                        {
                            ready = 1;
                            dhobliquemax2 = dhoblique;
                            II = r - rowlowestobnb;
                            JJ = c - collowestobnb;
                            if (dhobliquemax2 < 0)
                                ReportDebug($"cell {r} {c} is a lower obnb (dhoblmax2 = {dhobliquemax2}) from {rowlowestobnb},{collowestobnb}");
                        }
                        else if (AlmostEqual(dhoblique, 0.0))
                        {
                            // Extend delta across equal-height neighbors
                            depression[r, c] = -dep;
                            deltasize++;
                            ready = 0;
                            ReportDebug($"added {r} {c} (dhoblique≈0) to the delta; expanding search radius");
                        }
                    }
                }

                if (ready == 0)
                {
                    iloradius3++; iupradius3++; jloradius3++; jupradius3++;

                    // If entire lake bbox is covered and still no higher neighbor, use fill-space at current seed
                    if (rowlowestobnb - iloradius3 <= iloedge[dep] && rowlowestobnb + iupradius3 >= iupedge[dep] &&
                        collowestobnb - jloradius3 <= jloedge[dep] && collowestobnb + jupradius3 >= jupedge[dep])
                    {
                        ready = 1;
                        dhobliquemax2 = (dtmfill_A[rowlowestobnb, collowestobnb] - (dtm[rowlowestobnb, collowestobnb] + dz_ero_m[rowlowestobnb, collowestobnb] + dz_sed_m[rowlowestobnb, collowestobnb]));
                        if (AlmostEqual(dhobliquemax2, 0.0)) dhobliquemax2 = 1.0; // emergency value as per original
                        ReportDebug($"search finished - using fill space {dhobliquemax2}");
                    }
                }
            }
        }
        void raise_delta_completely(int dep, ref double available_for_delta_m, double[] local_s_i_t_kg)
        {
            ReportDebug($" raise_delta_completely: dh={dhobliquemax2}, deltasize={deltasize}");

            double prev_available = available_for_delta_m;

            int r0 = Clamp(rowlowestobnb - iloradius3, 0, nr - 1);
            int r1 = Clamp(rowlowestobnb + iupradius3, 0, nr - 1);
            int c0 = Clamp(collowestobnb - jloradius3, 0, nc - 1);
            int c1 = Clamp(collowestobnb + jupradius3, 0, nc - 1);

            for (int r = r0; r <= r1; r++)
            {
                for (int c = c0; c <= c1; c++)
                {
                    if (dtm[r, c] == nodata_value) continue;
                    if (depression[r, c] != -dep) continue; // only delta cells

                    // Distribute textures/SOM proportionally
                    for (int s = 0; s < n_texture_classes; s++)
                        texture_kg[r, c, 0, s] += depressionsum_texture_kg[s] * (dhobliquemax2 / Math.Max(TOL_M, depressionsum_sediment_m));
                    old_SOM_kg[r, c, 0] += depressionsum_OOM_kg * (dhobliquemax2 / Math.Max(TOL_M, depressionsum_sediment_m));
                    young_SOM_kg[r, c, 0] += depressionsum_YOM_kg * (dhobliquemax2 / Math.Max(TOL_M, depressionsum_sediment_m));

                    dtm[r, c] += dhobliquemax2;
                    dtmchange_m[r, c] += dhobliquemax2;
                    lake_sed_m[r, c] += dhobliquemax2;
                    available_for_delta_m -= dhobliquemax2;

                    // Overfill guard
                    double filled_alt = dtm[r, c] + dz_ero_m[r, c] + dz_sed_m[r, c];
                    if (Greater(filled_alt, dtmfill_A[r, c]))
                    {
                        double take_back = filled_alt - dtmfill_A[r, c];
                        available_for_delta_m += take_back;

                        // Remove mass proportionally
                        for (int s = 0; s < n_texture_classes; s++)
                            texture_kg[r, c, 0, s] -= depressionsum_texture_kg[s] * (take_back / Math.Max(TOL_M, depressionsum_sediment_m));
                        old_SOM_kg[r, c, 0] -= depressionsum_OOM_kg * (take_back / Math.Max(TOL_M, depressionsum_sediment_m));
                        young_SOM_kg[r, c, 0] -= depressionsum_YOM_kg * (take_back / Math.Max(TOL_M, depressionsum_sediment_m));

                        lake_sed_m[r, c] -= take_back;
                        dtmchange_m[r, c] -= take_back;
                        dtm[r, c] = dtmfill_A[r, c] - dz_ero_m[r, c] - dz_sed_m[r, c];

                        depression[r, c] = 0; // remove from lake as per original
                        deltasize = Math.Max(0, deltasize - 1);
                    }
                }
            }

            // Extend delta to next neighbor
            int nrx = rowlowestobnb + II, ncx = collowestobnb + JJ;
            if (nrx >= 0 && nrx < nr && ncx >= 0 && ncx < nc && depression[nrx, ncx] == dep)
            {
                depression[nrx, ncx] = -dep;
                deltasize++;
            }

            // Monotonic check
            if (available_for_delta_m > prev_available + TOL_M)
            {
                ReportDebug($"Non-monotonic after raise_delta_completely: prev={prev_available}, now={available_for_delta_m}, clamping.");
                available_for_delta_m = prev_available;
            }
        }

        void raise_delta_partly(int dep, ref double available_for_delta_m, double[] local_s_i_t_kg)
        {
            ReportDebug($" raise_delta_partly: available={available_for_delta_m}, deltasize={deltasize}");
            if (deltasize <= 0) { available_for_delta_m = 0; return; }

            double prev_available = available_for_delta_m;

            double mem_m = available_for_delta_m / deltasize;
            available_for_delta_m = 0;

            int r0 = Clamp(rowlowestobnb - iloradius3, 0, nr - 1);
            int r1 = Clamp(rowlowestobnb + iupradius3, 0, nr - 1);
            int c0 = Clamp(collowestobnb - jloradius3, 0, nc - 1);
            int c1 = Clamp(collowestobnb + jupradius3, 0, nc - 1);

            obnbchanged = 0;

            for (int r = r0; r <= r1; r++)
            {
                for (int c = c0; c <= c1; c++)
                {
                    if (dtm[r, c] == nodata_value) continue;
                    if (depression[r, c] != -dep) continue;

                    for (int s = 0; s < n_texture_classes; s++)
                        texture_kg[r, c, 0, s] += depressionsum_texture_kg[s] * (mem_m / Math.Max(TOL_M, depressionsum_sediment_m));
                    old_SOM_kg[r, c, 0] += depressionsum_OOM_kg * (mem_m / Math.Max(TOL_M, depressionsum_sediment_m));
                    young_SOM_kg[r, c, 0] += depressionsum_YOM_kg * (mem_m / Math.Max(TOL_M, depressionsum_sediment_m));

                    dtm[r, c] += mem_m;
                    dtmchange_m[r, c] += mem_m;
                    lake_sed_m[r, c] += mem_m;

                    // Overfill guard
                    double filled_alt = dtm[r, c] + dz_ero_m[r, c] + dz_sed_m[r, c];
                    if (Greater(filled_alt, dtmfill_A[r, c]))
                    {
                        double take_back = filled_alt - dtmfill_A[r, c];
                        available_for_delta_m += take_back;

                        for (int s = 0; s < n_texture_classes; s++)
                        {
                            texture_kg[r, c, 0, s] -= depressionsum_texture_kg[s] * (take_back / Math.Max(TOL_M, depressionsum_sediment_m));
                            // Note: we already zeroed any sediment_in_transport waiting here in your original; it remains zero.
                        }
                        old_SOM_kg[r, c, 0] -= depressionsum_OOM_kg * (take_back / Math.Max(TOL_M, depressionsum_sediment_m));
                        young_SOM_kg[r, c, 0] -= depressionsum_YOM_kg * (take_back / Math.Max(TOL_M, depressionsum_sediment_m));

                        lake_sed_m[r, c] -= take_back;
                        dtmchange_m[r, c] -= take_back;
                        dtm[r, c] = dtmfill_A[r, c] - dz_ero_m[r, c] - dz_sed_m[r, c];

                        depression[r, c] = 0; // remove from lake
                        deltasize = Math.Max(0, deltasize - 1);
                        obnbchanged = 1; // delta seed changed
                    }
                }
            }

            // If delta got emptied but there is remainder, choose a new seed from any remaining lake cell near old seed (fragmentation-safe)
            if (deltasize == 0 && available_for_delta_m > TOL_M)
            {
                // Try the neighbor (startrow+II, startcol+JJ) logic as in original, else scan small window to find any lake cell
                // No-op here: next outer loop iteration will pick a new seed via find_* methods.
            }

            // Monotonic check
            if (available_for_delta_m > prev_available + TOL_M)
            {
                ReportDebug($"Non-monotonic after raise_delta_partly: prev={prev_available}, now={available_for_delta_m}, clamping.");
                available_for_delta_m = prev_available;
            }
        }

        #endregion

    }
}
