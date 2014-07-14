using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xintric.MUtil
{
    public static class AStar
    {
        public interface INode
        {
            /// <summary>
            /// Specifies the connected nodes to this node, and the cost to traverse to the node
            /// </summary>
            IEnumerable<Tuple<INode, double>> ConnectedNodes { get; }

            double EstimatedDistanceTo(INode other);

        }

        public static IEnumerable<INode> Find(INode from, INode to)
        {

            List<Tuple<List<INode>, double>> queue = new List<Tuple<List<INode>, double>>();
            queue.Add(new Tuple<List<INode>, double>(Enumerable.Repeat(from,1).ToList(), 0));


            while (queue.Count > 0)
            {
                var best = queue.OrderBy(x => x.Item2 + x.Item1.Last().EstimatedDistanceTo(to)).First();

                if (best.Item1.Last() == to) return best.Item1;
                queue.Remove(best);

                foreach (var e in best.Item1.Last().ConnectedNodes)
                {
                    var newlist = best.Item1.Concat(Enumerable.Repeat(e.Item1,1)).ToList();
                    var newvalue = best.Item2 + e.Item2;
                    queue.Add(new Tuple<List<INode>, double>(newlist, newvalue));
                }

            }

            throw new InvalidOperationException("No path available between from and to");
        }

    }
}
