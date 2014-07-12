using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xintric.MUtil.LinAlg
{
    public class LUP
    {


        double[][] data;

        public LUP(Matrix src) 
        {
            if (src.RowCount != src.ColumnCount) throw new ArgumentException("matrix must be square");

//            data = Enumerable.Range(0, src.RowCount).Select(x => new double[src.ColumnCount]).ToArray();

            data = src.Rows.Select(row=>row.ToArray()).ToArray();
            matrix = Matrix.FromRows(data);

            pivot = Enumerable.Range(0, matrix.RowCount).ToArray();

            int Rows = src.RowCount;
            int Columns = src.ColumnCount;


            for (int k = 0; k < Rows; ++k)
            {

                int maxi = k;
                for (int i = k + 1; i < Rows; ++i)
                {
                    double v = data[i][k];// value(i, k);
                    v *= v;
                    double maxv = data[maxi][k];// value(maxi, k);
                    maxv *= maxv;
                    if (v > maxv)
                        maxi = i;
                }
//                            maxi = k; //force pivot to identity matrix
                if (maxi != k)
                {
                    var tmp = data[maxi];
                    data[maxi] = data[k];
                    data[k] = tmp;

                    int ptmp = pivot[maxi];
                    pivot[maxi] = pivot[k];
                    pivot[k] = ptmp;
                }

                for (int i = k + 1; i < Rows; ++i)
                {
                    double mod = data[i][k] / data[k][k];// value(i, k) / value(k, k);
                    data[i][k] = mod;// setValue(i, k, mod);
                    for (int j = k + 1; j < Columns; ++j)
                        data[i][j] = data[i][j] - mod * data[k][j];// setValue(i, j, value(i, j) - mod * value(k, j));
                }
            }
        }



        public Matrix L
        {
            get
            {

                return Matrix.FromRows(
                    matrix.Rows.Select((row, count) =>
                        row.Select((element, index) =>
                            {
                                if (index < count) return element;
                                else if (index == count) return 1.0;
                                else return 0.0;
                            })));
            }
        }

        public Matrix U
        {
            get
            {
                return Matrix.FromRows(
                    matrix.Rows.Select((row, count) =>
                        row.Select((element, index) =>
                            {
                                if (index < count) return 0.0;
                                else return element;
                            })));                        
            }
        }


        public Matrix P
        {
            get
            {
                return Matrix.FromRows(pivot.Select(x =>
                        Enumerable.Range(0, matrix.ColumnCount).Select(i => x == i ? 1.0 : 0.0)
                    ));
            }
        }

        public Matrix Pivoted(Matrix v)
        {
            if (pivot.Count() != v.RowCount) throw new ArgumentException("Matrix must be of same dimensionality");

            var rows = v.Rows.ToArray();

            return Matrix.FromRows(Enumerable.Range(0, pivot.Count()).Select(index => rows[pivot[index]]));

        }


        public Matrix Solve(Matrix v, Action<int> feedback = null)
        {
            if (v.RowCount != matrix.ColumnCount) throw new ArgumentException();

            if (feedback != null) feedback(-3);

            v = Pivoted(v);
            if (feedback != null) feedback(-2);

            var dst = v.Rows.Select(row=>row.ToArray()).ToArray();
            var src = matrix.Rows.Select(row=>row.ToArray()).ToArray();

            int vrowcount = v.RowCount;
            int vcolumncount = v.ColumnCount;

            if (feedback != null) feedback(-1);

            for (int c = 0; c < vcolumncount; ++c)
            {
                if (feedback != null) feedback(c);
                for (int num = 0; num < vrowcount; ++num)
                {
                    double val = dst[num][c];

                    for (int i = 0; i < num; ++i)
                    {
                        val -= src[num][i] * dst[i][c];
                    }

                    dst[num][c] = val;
                }
            }


            for (int c = 0; c < vcolumncount; ++c)
            {
                if (feedback != null) feedback(v.ColumnCount + c);
                for (int num = vrowcount - 1; num >= 0; --num)
                {
                    double val = dst[num][c];
                    for (int i = num + 1; i < vrowcount; ++i)
                    {
                        val -= src[num][i] * dst[i][c];
                    }
                    val /= src[num][num];
                    dst[num][c] = val;
                }
            }

            return Matrix.FromRows(dst);
        }



        public Matrix matrix;
        public int[] pivot;
    }
}
