using System;
using System.Collections.Generic;
using System.Linq;

namespace WB_GCAD25
{
    public class IntervalFinder
    {
        private static bool ArraysEqual(double[] a, double[] b)
        {
            return a.Length == b.Length
                   && a.Zip(b, (x, y) => Math.Abs(x - y) <= 1e-9).All(equal => equal);
        }

        private static (int n_625, int n_500, int n_160)? FindOptimalNumberOfIntervals625(double width, double[] offset_start, double[] offset_end)
        {
            int n_625 = (int)Math.Floor(width / 625.0);
            double B_625 = n_625 * 625.0;

            int n_500 = (int)Math.Floor((width - B_625) / 500.0);
            double B_500 = n_500 * 500.0;

            double B_prov = n_625 * 625.0 + n_500 * 500.0;

            double B_min = B_prov + offset_start[0] + offset_end[0];
            double B_max = B_prov + offset_start[1] + offset_end[1];

            bool solved = true;
            while (!(B_min <= width && width <= B_max))
            {
                n_625 -= 1;
                B_625 = n_625 * 625.0;
                n_500 = (int)Math.Floor((width - B_625) / 500.0);
                B_500 = n_500 * 500.0;

                B_prov = n_625 * 625.0 + n_500 * 500.0;
                B_min = B_prov + offset_start[0] + offset_end[0];
                B_max = B_prov + offset_start[1] + offset_end[1];

                if (n_625 < 0 || n_500 < 0)
                {
                    Console.WriteLine("Something went wrong!");
                    solved = false;
                    break;
                }
            }

            if (solved)
                return (n_625, n_500, 0);
            else
                return null;
        }
        private static (int n_625, int n_500, int n_160)? FindOptimalNumberOfIntervals625DoubledBeams(double width, double[] offset_start, double[] offset_end)
        {
            int n_160_add = 1;
            double width_for_B = width * 1;
            if (ArraysEqual(offset_start, new double[] { 0.0, 80.0 }))
            {
                width_for_B += 160.0;
                n_160_add -= 1;
            }
            if (ArraysEqual(offset_end, new double[] { 0.0, 80.0 }))
            {
                width_for_B += 160.0;
                n_160_add -= 1;
            }

            int n_625 = (int)Math.Floor(width_for_B / (625.0 + 160.0));
            double B_625 = n_625 * 625.0;

            int n_500 = (int)Math.Floor((width_for_B - (B_625 + n_625 * 160.0)) / (500.0 + 160.0));
            double B_500 = n_500 * 500.0;

            int n_160 = n_625 + n_500 + n_160_add;

            double B_prov = n_625 * 625.0 + n_500 * 500.0 + n_160 * 160.0;

            double B_min = B_prov + offset_start[0] + offset_end[0];
            double B_max = B_prov + offset_start[1] + offset_end[1];

            bool solved = true;
            while (!(B_min <= width && width <= B_max))
            {
                n_625 -= 1;
                B_625 = n_625 * 625.0;
                n_500 = (int)Math.Floor((width_for_B - (B_625 + n_625 * 160.0)) / (500.0 + 160.0));
                B_500 = n_500 * 500.0;

                n_160 = n_625 + n_500 + n_160_add;

                B_prov = n_625 * 625.0 + n_500 * 500.0 + n_160 * 160.0;
                B_min = B_prov + offset_start[0] + offset_end[0];
                B_max = B_prov + offset_start[1] + offset_end[1];

                if (n_625 < 0 || n_500 < 0)
                {
                    Console.WriteLine("Something went wrong!");
                    solved = false;
                    break;
                }
            }

            if (solved)
                return (n_625, n_500, n_160);
            else
                return null;
        }
        private static (int n_625, int n_500, int n_160)? FindOptimalNumberOfIntervals625_500(double width, double[] offset_start, double[] offset_end)
        {
            int n_625 = (int)Math.Floor(width / 625.0);
            double B_625 = n_625 * 625.0;

            int n_500 = (int)Math.Floor((width - B_625) / 500.0);
            double B_500 = n_500 * 500.0;

            double B_prov = n_625 * 625.0 + n_500 * 500.0;

            double B_min = B_prov + offset_start[0] + offset_end[0];
            double B_max = B_prov + offset_start[1] + offset_end[1];

            bool solved = true;
            while (!(B_min <= width && width <= B_max) || (n_500 < n_625))
            {
                n_625 -= 1;
                B_625 = n_625 * 625.0;
                n_500 = (int)Math.Floor((width - B_625) / 500.0);
                B_500 = n_500 * 500.0;

                B_prov = n_625 * 625.0 + n_500 * 500.0;
                B_min = B_prov + offset_start[0] + offset_end[0];
                B_max = B_prov + offset_start[1] + offset_end[1];

                if (n_625 < 0 || n_500 < 0)
                {
                    Console.WriteLine("Something went wrong!");
                    solved = false;
                    break;
                }
            }

            if (solved)
                return (n_625, n_500, 0);
            else
                return null;
        }
        private static (int n_625, int n_500, int n_160)? FindOptimalNumberOfIntervals625_500DoubledBeams(double width, double[] offset_start, double[] offset_end)
        {
            double width_for_B = width * 1;
            int n_160_add = 1;
            if (ArraysEqual(offset_start, new double[] { 0.0, 80.0 }))
            {
                width_for_B += 160.0;
                n_160_add -= 1;
            }
            if (ArraysEqual(offset_end, new double[] { 0.0, 80.0 }))
            {
                width_for_B += 160.0;
                n_160_add -= 1;
            }

            int n_625 = (int)Math.Floor(width_for_B / (625.0 + 160.0));
            double B_625 = n_625 * 625.0;

            int n_500 = (int)Math.Floor((width_for_B - (B_625 + n_625 * 160.0)) / (500.0 + 160.0));
            double B_500 = n_500 * 500.0;

            int n_160 = n_625 + n_500 + n_160_add;

            double B_prov = n_625 * 625.0 + n_500 * 500.0 + n_160 * 160.0;

            double B_min = B_prov + offset_start[0] + offset_end[0];
            double B_max = B_prov + offset_start[1] + offset_end[1];

            bool solved = true;
            while (!(B_min <= width && width <= B_max) || (n_500 < n_625))
            {
                n_625 -= 1;
                B_625 = n_625 * 625.0;

                n_500 = (int)Math.Floor((width_for_B - (B_625 + n_625 * 160.0)) / (500.0 + 160.0));
                B_500 = n_500 * 500.0;

                n_160 = n_625 + n_500 + n_160_add;

                B_prov = n_625 * 625.0 + n_500 * 500.0 + n_160 * 160.0;
                B_min = B_prov + offset_start[0] + offset_end[0];
                B_max = B_prov + offset_start[1] + offset_end[1];

                if (n_625 < 0 || n_500 < 0)
                {
                    Console.WriteLine("Something went wrong!");
                    solved = false;
                    break;
                }
            }

            if (solved)
                return (n_625, n_500, n_160);
            else
                return null;
        }
        private static (int n_625, int n_500, int n_160)? FindOptimalNumberOfIntervals500(double width, double[] offset_start, double[] offset_end)
        {
            int n_500 = (int)Math.Floor(width / 500.0);
            double B_500 = n_500 * 500.0;

            double B_prov = n_500 * 500.0;

            double B_min = B_prov + offset_start[0] + offset_end[0];
            double B_max = B_prov + offset_start[1] + offset_end[1];

            if (B_min <= width && width <= B_max)
            {
                return (0, n_500, 0);
            }
            else
            {
                return null;
            }
        }
        private static (int n_625, int n_500, int n_160)? FindOptimalNumberOfIntervals500DoubledBeams(double width, double[] offset_start, double[] offset_end)
        {
            double width_for_B = width * 1;
            int n_160_add = 1;
            if (ArraysEqual(offset_start, new double[] { 0.0, 80.0 }))
            {
                width_for_B += 160.0;
                n_160_add -= 1;
            }
            if (ArraysEqual(offset_end, new double[] { 0.0, 80.0 }))
            {
                width_for_B += 160.0;
                n_160_add -= 1;
            }

            int n_500 = (int)Math.Floor(width_for_B / (500.0 + 160.0));
            double B_500 = n_500 * 500.0;

            int n_160 = n_500 + n_160_add;

            double B_prov = n_500 * 500.0 + n_160 * 160.0;

            double B_min = B_prov + offset_start[0] + offset_end[0];
            double B_max = B_prov + offset_start[1] + offset_end[1];

            if (B_min <= width && width <= B_max)
            {
                return (0, n_500, n_160);
            }
            else
            {
                return null;
            }
        }
        public IntervalResult FindOptimalIntervals(
            double max_ovn,
            double width,
            bool doubled_beams = false,
            List<double> x_loads = null,
            bool omit_first_beam = false,
            bool omit_last_beam = false
        )
        {
            x_loads = x_loads ?? new List<double>();
            int n_added = x_loads.Count;

            var first_offset = new List<double[]>() { new double[] {0.0, 80.0} };
            var last_offset = new List<double[]>() { new double[] {0.0, 80.0} };

            Func<double, double[], double[], (int,int,int)?> func;

            if (max_ovn == 625.0)
            {
                func = doubled_beams 
                    ? new Func<double, double[], double[], (int, int, int)?>(FindOptimalNumberOfIntervals625DoubledBeams) 
                    : FindOptimalNumberOfIntervals625;
                if (omit_first_beam)
                {
                    first_offset.Insert(0, new double[] { 505.0, 515.0 });
                    first_offset.Insert(0, new double[] { 380.0, 390.0 });
                }
                if (omit_last_beam)
                {
                    last_offset.Insert(0, new double[] { 505.0, 515.0 });
                    last_offset.Insert(0, new double[] { 380.0, 390.0 });
                }
            }
            else if (Math.Abs(max_ovn - ((625.0 + 500.0) / 2)) < 1e-9)
            {
                func = doubled_beams 
                    ? new Func<double, double[], double[], (int, int, int)?>(FindOptimalNumberOfIntervals625_500DoubledBeams) 
                    : FindOptimalNumberOfIntervals625_500;
                if (omit_first_beam)
                {
                    first_offset.Insert(0, new double[] { 505.0, 515.0 });
                    first_offset.Insert(0, new double[] { 380.0, 390.0 });
                }
                if (omit_last_beam)
                {
                    last_offset.Insert(0, new double[] { 505.0, 515.0 });
                    last_offset.Insert(0, new double[] { 380.0, 390.0 });
                }
            }
            else
            {
                func = doubled_beams
                    ? new Func<double, double[], double[], (int, int, int)?>(FindOptimalNumberOfIntervals500DoubledBeams) 
                    : FindOptimalNumberOfIntervals500;
                if (omit_first_beam)
                {
                    first_offset.Insert(0, new double[] { 380.0, 390.0 });
                }
                if (omit_last_beam)
                {
                    last_offset.Insert(0, new double[] { 380.0, 390.0 });
                }
            }

            double B = width - n_added * 160.0;

            (int n_625, int n_500, int n_160)? result = null;
            double[] s_off = null;
            double[] end_off = null;
        
            bool found = false;
            foreach (var so in first_offset)
            {
                foreach (var eo in last_offset)
                {
                    result = func(B, so, eo);
                    if (result.HasValue)
                    {
                        s_off = so;
                        end_off = eo;
                        found = true;
                        break; // breaks out of the inner loop
                    }
                }
                if (found)
                {
                    break; // breaks out of the outer loop
                }
            }

            if (!result.HasValue)
            {
                throw new Exception("Did not found solution");
            }

            var (final_n_625, final_n_500, final_n_160) = result.Value;
            int n_intervals = final_n_625 + final_n_500;
            int n_beams = n_intervals + 1;

            double[] intervals = new double[n_intervals];

            int shiftIndex;
            if (n_intervals % 2 == 0)
            {
                shiftIndex = (n_intervals / 2) - (int)Math.Ceiling(Math.Min(final_n_625, final_n_500) / 2.0) - 1;
            }
            else
            {
                shiftIndex = (n_intervals / 2) - (int)Math.Floor(Math.Min(final_n_625, final_n_500) / 2.0) - 1;
            }

            if (final_n_625 < final_n_500)
            {
                for (int i = 0; i < final_n_625; i++)
                {
                    intervals[i * 2 + shiftIndex] = 625.0;
                }
                for (int i = 0; i < n_intervals; i++)
                {
                    if (intervals[i] == 0.0)
                    {
                        intervals[i] = 500.0;
                    }
                }
            }
            else
            {
                for (int i = 0; i < final_n_500; i++)
                {
                    intervals[i * 2 + shiftIndex] = 500.0;
                }
                for (int i = 0; i < n_intervals; i++)
                {
                    if (intervals[i] == 0.0)
                    {
                        intervals[i] = 625.0;
                    }
                }
            }

            if (doubled_beams)
            {
                var intervals_copy = intervals.ToArray();
                var new_intervals = new List<double>();
                if (!ArraysEqual(s_off, new double[] { 0.0, 80.0 }))
                {
                    new_intervals.Add(160.0);
                }
                for (int i = 0; i < intervals_copy.Length; i++)
                {
                    new_intervals.Add(intervals_copy[i]);
                    if (i != intervals_copy.Length - 1)
                    {
                        new_intervals.Add(160.0);
                    }
                    else
                    {
                        if (!ArraysEqual(end_off, new double[] { 0.0, 80.0 }))
                        {
                            new_intervals.Add(160.0);
                        }
                    }
                }
                intervals = new_intervals.ToArray();
            }

            double B_provided = final_n_500 * 500.0 + final_n_625 * 625.0 + (final_n_160 + n_added) * 160.0;
            double remainder = width - B_provided;

            double shift;
            double end_off_last = end_off[end_off.Length - 1];
            if (ArraysEqual(s_off, new double[] { 0.0, 80.0 }))
            {
                if (ArraysEqual(end_off, new double[] { 0.0, 80.0 }))
                {
                    shift = remainder / 2.0;
                }
                else
                {
                    if (remainder - end_off_last < 0)
                    {
                        shift = 0.0;
                    }
                    else
                    {
                        shift = remainder - end_off_last;
                    }
                }
            }
            else
            {
                if (remainder - end_off_last < 0)
                {
                    shift = 0.0;
                }
                else
                {
                    shift = remainder - end_off_last;
                }
            }

            shift = Utils.RoundToBase(shift, 5.0);
            var x_coords = new List<double>() { shift };
            foreach (var interval in intervals)
            {
                x_coords.Add(x_coords[x_coords.Count - 1] + interval);
            }

            x_loads.Sort();
            foreach (var x_double in x_loads)
            {
                var distances = x_coords.Select(x => Math.Abs(x - x_double)).ToList();
                double minDist = distances.Min();
                int min_ind = distances.IndexOf(minDist);

                // Insert a 160 beam after min_ind
                for (int i = min_ind + 1; i < x_coords.Count; i++)
                {
                    x_coords[i] += 160.0;
                }
                x_coords.Insert(min_ind + 1, x_coords[min_ind] + 160.0);

                var intervalsList = intervals.ToList();
                intervalsList.Insert(min_ind + 1, 160.0);
                intervals = intervalsList.ToArray();
            }

            Console.WriteLine($"width={width}");
            Console.WriteLine($"doubled_beams={doubled_beams}");
            Console.WriteLine($"max_ovn={max_ovn}");
            Console.WriteLine($"result=({final_n_625}, {final_n_500}, {final_n_160})");
            Console.WriteLine($"s_off=[{string.Join(", ", s_off)}]");
            Console.WriteLine($"end_off=[{string.Join(", ", end_off)}]");
            Console.WriteLine($"intervals=[{string.Join(", ", intervals)}]");
            Console.WriteLine($"x_coords=[{string.Join(", ", x_coords)}]");
            Console.WriteLine();

            IntervalResult res = new IntervalResult
            {
                XCoords = x_coords,
                Intervals = intervals.ToList(),
                StartOffset = s_off,
                EndOffset = end_off,
            };

            return res;
        }
    }
}