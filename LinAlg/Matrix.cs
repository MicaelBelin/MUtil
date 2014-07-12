using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xintric.MUtil.LinAlg
{
    public class Matrix
    {

        IEnumerable<IEnumerable<double>> data; //[row][column]

        public IEnumerable<IEnumerable<double>> Rows
        {
            get
            {
                return data;
            }
        }

        public IEnumerable<IEnumerable<double>> Columns
        {
            get
            {
                return Transposed(data);
            }
        }


        public int ColumnCount
        {
            get
            {
                return data.First().Count();
            }
        }

        public int RowCount
        {
            get
            {
                return data.Count();
            }
        }

        private Matrix()
        {
        }

        public Matrix Cached()
        {
            var cdata = data.Select(x => x.ToArray()).ToArray();
            return FromRows(cdata);
        }

/*
        public static IEnumerable<IEnumerable<double>> Transposed(IEnumerable<IEnumerable<double>> t)
        {
            var enums = t.Select(x => x.GetEnumerator()).ToArray();
            while (enums.All(e => e.MoveNext()))
            {
                yield return enums.Select(e => e.Current);
            }
        }
*/

        public static IEnumerable<IEnumerable<double>> Transposed(IEnumerable<IEnumerable<double>> source)
        {
         return source
             .Select(a => a.Select(b => Enumerable.Repeat(b, 1)))
             .Aggregate((a, b) => a.Zip(b, Enumerable.Concat));
        }
 


        public static Matrix FromColumns(IEnumerable<IEnumerable<double>> columns)
        {
            return FromRows(Transposed(columns));
        }

        public static Matrix FromRows(IEnumerable<IEnumerable<double>> rows)
        {
            var ret = new Matrix();
            ret.data = rows;

            //check all rows are of equal length
            if (ret.data.Select(row => row.Count()).Distinct().Count() != 1)
            {
                throw new ArgumentException("All rows must be of equal length.");
            }

            return ret;
        }

        public static Matrix Identity(int dimensions)
        {
            return Matrix.FromRows(Enumerable.Range(0, dimensions).Select(rowindex => Enumerable.Range(0, dimensions).Select(columnindex => rowindex == columnindex ? 1.0 : 0.0)));
        }

        public Matrix ColumnMatrix(int index)
        {
            return Matrix.FromColumns(Enumerable.Repeat(
                Column(index)
                ,1));
        }

        public IEnumerable<double> Column(int index)
        {
            return data.Select(row => row.Skip(index).First());
        }

        public Matrix RowMatrix(int index)
        {
            return Matrix.FromRows(Enumerable.Repeat(Row(index),1));
        }

        public IEnumerable<double> Row(int index)
        {
            return data.Skip(index).First();
        }



        public Matrix Mult(Matrix other)
        {
            if (ColumnCount != other.RowCount) throw new ArgumentException("ColumnCount must equal other.RowCount");
            return FromRows(Rows.Select(arow => arow.Zip(other.Columns, (arowelement, bcolumn) => arow.Zip(bcolumn, (ae, be) => ae * be).Sum())));
        }

        public Matrix MultCached(Matrix other)
        {
            if (ColumnCount != other.RowCount) throw new ArgumentException("ColumnCount must equal other.RowCount");
            other = other.Transposed().Cached();
            return FromRows(Rows.Select(arow => arow.Zip(other.Rows, (arowelement, bcolumn) => arow.Zip(bcolumn, (ae, be) => ae * be).Sum())));
        }

        public Matrix Transposed()
        {
            return Matrix.FromColumns(data);
        }

        public Matrix Inversed()
        {
            if (RowCount != ColumnCount) throw new InvalidOperationException("Matrix is not square");
            var lup = new LUP(this);
            return lup.Solve(Matrix.Identity(RowCount));
        }


        public Matrix EigenVectors()
        {
            return EigenVectors(ColumnCount);
        }

        public IEnumerable<double> ColumnLengths
        {
            get
            {
                return Columns.Select(col => Math.Sqrt(
                    col.Aggregate((src, next) => src + next * next)
                    ));
            }
        }

        public IEnumerable<IEnumerable<double>> NormalizedColumns
        {
            get
            {
                return Columns.Zip(ColumnLengths, (col, l) => col.Select(x => x / l));
            }
        }


        public Matrix ReducedBy(IEnumerable<double> principalvector)
        {

            var nextcolumns = Columns.Select(col =>
            {
                var scalar = col.Zip(principalvector, (a, b) => a * b).Sum();
                return col.Zip(principalvector, (ce, ve) => ce - scalar * ve).ToArray();
            }).ToArray();

            return FromColumns(nextcolumns);

        }

        public Matrix EigenVectors(int returnedvectors)
        {
            if (ColumnCount != RowCount) throw new InvalidOperationException("Matrix must be square");


            Random r = new Random();




            Matrix v = FromColumns(Enumerable.Repeat(
                    Enumerable.Range(0,RowCount).Select(x=> r.NextDouble()).ToArray()
                ,1)).Cached();

            for (int num = 0; num < 50; ++num)
            {
                var dbg = v.Columns.Select(x => x.ToArray()).ToArray();

                var normalv = FromColumns(v.NormalizedColumns);

                v = (Mult(normalv)).Cached();



//                v = FromColumns((this * v).NormalizedColumns).Cached();
            }


            var eigenvalue = v.ColumnLengths.First();



            if (returnedvectors > 1)
            {
                var eigenvector = v.Cached().NormalizedColumns.First();

                var nextmat = ReducedBy(eigenvector).Cached();

                var rest = nextmat.EigenVectors(returnedvectors - 1);

                return FromColumns(v.Columns.Concat(rest.Columns));


            }
            else
            {
                return Mult(v);
            }


        }


    }
}
