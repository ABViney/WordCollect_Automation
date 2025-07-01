namespace WordCollect_Automated.Services.WordCollect.InputSimulation;

/// <summary>
/// A graph data structure used to represent a central node surrounded by outer nodes.
///
///     , - ~  -  ,
///    /     |     \
///   /   \  |  /   \
///  |     \ | /     |
///  |------ + ------| - Wagon wheel ASCII art courtesy of ChatGPT
///  |     / | \     |
///  \   /  |  \    /
///   \     |     /
///     ' - ~ - '
/// 
/// This variation is specific to solving a pathfinding problem faced by this project: Creating a path among elements
/// of the outer rim, in which the hub node acts as a bridge between non-adjacent rim nodes.
/// </summary>
/// <typeparam name="T">Any valid type</typeparam>
public class WagonWheelPathingGraph<T>
{
    private readonly WagonWheelPathingGraphNode<T> _hubNode;
    private readonly HashSet<WagonWheelPathingGraphNode<T>> _rimNodes;

    private readonly Dictionary<T, WagonWheelPathingGraphNode<T>> _lookup = new();

    /// <summary>
    /// Creates a wagon wheel graph with a center <paramref name="hubValue"/> and surrounding
    /// <paramref name="rimValues"/>. The adjacency of <paramref name="rimValues"/> is assumed from the provided
    /// parameter: each element is adjacent to its previous and next index. The first and last elements are considered
    /// adjacent. 
    /// </summary>
    /// <param name="hubValue">The middle of the graph</param>
    /// <param name="rimValues">The outer region of the graph</param>
    public WagonWheelPathingGraph(
        T hubValue,
        IEnumerable<T> rimValues)
    {
        _hubNode = new WagonWheelPathingGraphNode<T>(hubValue);
        _lookup.Add(hubValue, _hubNode);
        
        List<WagonWheelPathingGraphNode<T>>
            rimNodes = rimValues.Select(rv => new WagonWheelPathingGraphNode<T>(rv)).ToList();
        
        // Setting up relationships
        for (int i = 0; i < rimNodes.Count-1; i++)
        {
            var currentNode = rimNodes[i];
            currentNode.AddNeighbor(_hubNode);
            currentNode.AddNeighbor(rimNodes[i + 1]); // Next in iteration
            _lookup.Add(currentNode.Value, currentNode);
        }
        
        rimNodes.Last().AddNeighbor(_hubNode);
        rimNodes.Last().AddNeighbor(rimNodes.First()); // Complete wheel -- connect last to first
        _lookup.Add(rimNodes.Last().Value, rimNodes.Last());
    }

    /// <summary>
    /// Creates a path of <typeparam name="T"/> that passes through each <paramref name="values"/>, adding trips to the
    /// <see cref="_hubNode">hub</see> when elements of <paramref name="values"/> are not adjacent.
    /// </summary>
    /// <param name="values"></param>
    /// <returns>An enumeration of values construing a path between all the values provided.</returns>
    /// <exception cref="ArgumentException">A value not in the graph was supplied.</exception>
    public IEnumerable<T> ResolvePath(IEnumerable<T> values)
    {
         WagonWheelPathingGraphNode<T> prev = null;
        
         foreach (var value in values)
         {
             WagonWheelPathingGraphNode<T>? current;
             if (!_lookup.TryGetValue(value, out current))
                 throw new ArgumentException($"Node for value '{value}' not found");

             // The first value is always reachable
             if (prev is null)
             {
                 prev = current;
                 yield return current.Value;
                 continue;
             }

             // If the current value is not next to the previous value, then we must pass through the hub 
             if (!prev.HasNeighbor(current))
             {
                 yield return _hubNode.Value;
             }

             prev = current;
             yield return current.Value;
        }
    }
    
    private class WagonWheelPathingGraphNode<T>
    {
        private HashSet<WagonWheelPathingGraphNode<T>> _neighbors { get; } = new();
        public T Value { get; }

        public WagonWheelPathingGraphNode(T value)
        {
            Value = value;
        }

        /// <summary>
        /// Creates a neighborly relationship with another node.
        /// </summary>
        /// <param name="node"></param>
        public void AddNeighbor(WagonWheelPathingGraphNode<T> node)
        {
            if (node == this) return; // Prevent recursion
            _neighbors.Add(node);
            node._neighbors.Add(this);
        }
        
        /// <summary>
        /// Checks if this node is neighbors with another.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public bool HasNeighbor(WagonWheelPathingGraphNode<T> node)
        {
            return _neighbors.Contains(node);
        }
    }
}