internal class DeadlockGraph
{
    private readonly Dictionary<string, HashSet<string>> _graph = new();

    public void AddEdge(string from, string to)
    {
        if (!_graph.ContainsKey(from))
        {
            _graph[from] = new HashSet<string>();
        }
        _graph[from].Add(to);
    }

    public void RemoveEdge(string from, string to)
    {
        if (_graph.ContainsKey(from))
        {
            _graph[from].Remove(to);
            if (_graph[from].Count == 0)
            {
                _graph.Remove(from);
            }
        }
    }

    public List<string> GetCycleNodes()
    {
        var visited = new HashSet<string>();
        var stack = new HashSet<string>();
        foreach (var node in _graph.Keys)
        {
            if (HasCycle(node, visited, stack, out var cycleNodes))
            {
                return cycleNodes;
            }
        }
        return null;
    }

    private bool HasCycle(string node, HashSet<string> visited, HashSet<string> stack, out List<string> cycleNodes)
    {
        visited.Add(node);
        stack.Add(node);

        foreach (var neighbor in _graph[node])
        {
            if (!visited.Contains(neighbor) && HasCycle(neighbor, visited, stack, out cycleNodes))
            {
                cycleNodes.Add(node);
                return true;
            }
            else if (stack.Contains(neighbor))
            {
                cycleNodes = new List<string> { neighbor, node };
                return true;
            }
        }

        stack.Remove(node);
        cycleNodes = null;
        return false;
    }

    public bool HasCycle()
    {
        var visited = new HashSet<string>();
        var stack = new HashSet<string>();

        foreach (var node in _graph.Keys)
        {
            if (HasCycle(node, visited, stack))
            {
                return true;
            }
        }
        return false;
    }

    private bool HasCycle(string node, HashSet<string> visited, HashSet<string> stack)
    {
        if (stack.Contains(node))
        {
            return true;
        }

        if (visited.Contains(node))
        {
            return false;
        }

        visited.Add(node);
        stack.Add(node);

        if (_graph.ContainsKey(node))
        {
            foreach (var neighbor in _graph[node])
            {
                if (HasCycle(neighbor, visited, stack))
                {
                    return true;
                }
            }
        }

        stack.Remove(node);
        return false;
    }
}
