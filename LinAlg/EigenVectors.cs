using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xintric.MUtil.LinAlg
{
    public class EigenVectors
    {

        public EigenVectors(Matrix src)
        {
            if (src.ColumnCount != src.RowCount) throw new ArgumentException("Matrix must be square");
            this.src = src;
        }

        Matrix src;

        public IEnumerable<IEnumerable<double>> Columns
        {
            get
            {
                Random r = new Random();

                List<Matrix> prevvectors = new List<Matrix>();

                for (int columnindex = 0; columnindex < src.ColumnCount; ++columnindex)
                {

                    var vdata = Enumerable.Range(0, src.RowCount).Select(x => r.NextDouble()).ToArray();
                    for (int num = 0; num < 50; ++num)
                    {
                        var normalv = Matrix.FromColumns(Matrix.FromColumns(Enumerable.Repeat(vdata, 1)).NormalizedColumns);

                        var newv = (src.Mult(normalv)).Columns.First();
                        newv = Reduce(newv,prevvectors);

                        var srcenum = newv.GetEnumerator();
                        for (int i = 0; i < vdata.Count(); ++i)
                        {
                            srcenum.MoveNext();
                            vdata[i] = srcenum.Current;
                        }

                    }
                    yield return vdata;
                    prevvectors.Add(Matrix.FromColumns(Enumerable.Repeat(vdata,1)));

                }

            }
        }

        IEnumerable<double> Reduce(IEnumerable<double> src, List<Matrix> prevvectors)
        {
            foreach (var m in prevvectors)
            {
                var normv = m.NormalizedColumns.First();
                var scalar = src.Zip(normv, (a, b) => a * b).Sum();
                src = src.Zip(normv, (ce, ve) => ce - scalar * ve);
            }
            return src;
/*
            var nextcolumns = Columns.Select(col =>
            {
                var scalar = col.Zip(principalvector, (a, b) => a * b).Sum();
                return col.Zip(principalvector, (ce, ve) => ce - scalar * ve).ToArray();
            }).ToArray();

            return FromColumns(nextcolumns);
*/
        }



/*
        public Matrix EigenVectors(int returnedvectors)
        {
            if (ColumnCount != RowCount) throw new InvalidOperationException("Matrix must be square");


            Random r = new Random();




            Matrix v = FromColumns(Enumerable.Repeat(
                    Enumerable.Range(0, RowCount).Select(x => r.NextDouble()).ToArray()
                , 1)).Cached();

            for (int num = 0; num < 50; ++num)
            {
                var dbg = v.Columns.Select(x => x.ToArray()).ToArray();

                var normalv = FromColumns(v.NormalizedColumns);

                v = (this * normalv).Cached();



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
                return this * v;
            }


        }
*/


    }
}
