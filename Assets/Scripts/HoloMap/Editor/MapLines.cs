using System.Collections.Generic;
using UnityEngine;

public static class MapLines
{
    const float HardEdgeAngle = 25f;

    const float WeldShare = 0.0005f;

    const float MinGridCell = 0.02f;

    private class PointGrid
    {
        private readonly float cellSize;
        private readonly Dictionary<Vector3Int, List<int>> cells = new Dictionary<Vector3Int, List<int>>();

        public PointGrid(float cellSize)
        {
            this.cellSize = cellSize;
        }

        public void Add(int id, Vector3 point)
        {
            Vector3Int cell = GetCell(point);
            if (!cells.TryGetValue(cell, out List<int> ids))
            {
                ids = new List<int>();
                cells[cell] = ids;
            }
            ids.Add(id);
        }

        public void CollectNear(Vector3 point, HashSet<int> found)
        {
            Vector3Int center = GetCell(point);
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        if (cells.TryGetValue(center + new Vector3Int(x, y, z), out List<int> ids))
                        {
                            found.UnionWith(ids);
                        }
                    }
                }
            }
        }

        private Vector3Int GetCell(Vector3 point)
        {
            return new Vector3Int(Mathf.FloorToInt(point.x / cellSize), Mathf.FloorToInt(point.y / cellSize), Mathf.FloorToInt(point.z / cellSize));
        }
    }

    public static List<Vector3> GetHardEdges(Mesh source)
    {
        Vector3[] vertices = source.vertices;
        int[] triangles = source.triangles;

        // Weld vertices split along UV seams
        Vector3 size = source.bounds.size;
        float cell = Mathf.Max(Mathf.Max(size.x, size.y, size.z) * WeldShare, 0.000001f);
        var welded = new Dictionary<Vector3Int, int>();
        var points = new List<Vector3>();
        var ids = new int[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 v = vertices[i];
            var key = new Vector3Int(Mathf.FloorToInt(v.x / cell), Mathf.FloorToInt(v.y / cell), Mathf.FloorToInt(v.z / cell));
            if (!welded.TryGetValue(key, out int id))
            {
                id = points.Count;
                welded[key] = id;
                points.Add(v);
            }
            ids[i] = id;
        }

        var edges = new Dictionary<(int, int), List<Vector3>>();
        for (int i = 0; i + 2 < triangles.Length; i += 3)
        {
            int a = ids[triangles[i]], b = ids[triangles[i + 1]], c = ids[triangles[i + 2]];
            if (a == b || b == c || a == c) continue;

            // normalized returns zero for vectors this small
            Vector3 cross = Vector3.Cross(points[b] - points[a], points[c] - points[a]);
            float length = cross.magnitude;
            if (length <= 0f) continue;

            Vector3 normal = cross / length;
            AddEdge(edges, a, b, normal);
            AddEdge(edges, b, c, normal);
            AddEdge(edges, c, a, normal);
        }

        var lines = new List<Vector3>();
        float minDot = Mathf.Cos(HardEdgeAngle * Mathf.Deg2Rad);
        foreach (KeyValuePair<(int, int), List<Vector3>> edge in edges)
        {
            if (!IsHardEdge(edge.Value, minDot)) continue;
            lines.Add(points[edge.Key.Item1]);
            lines.Add(points[edge.Key.Item2]);
        }
        return lines;
    }

    public static List<Vector3> Merge(List<Vector3> segments, float distance)
    {
        float cellSize = Mathf.Max(distance, MinGridCell);
        var points = new List<Vector3>();
        var counts = new List<int>();
        var totals = new List<Vector3>();
        var grid = new PointGrid(cellSize);
        var nearby = new HashSet<int>();

        var edges = new HashSet<(int, int)>();
        for (int i = 0; i + 1 < segments.Count; i += 2)
        {
            int a = Snap(segments[i], distance, points, totals, counts, grid, nearby);
            int b = Snap(segments[i + 1], distance, points, totals, counts, grid, nearby);
            if (a != b) edges.Add(GetEdgeKey(a, b));
        }

        var settled = new PointGrid(cellSize);
        for (int i = 0; i < points.Count; i++)
        {
            points[i] = totals[i] / counts[i];
            settled.Add(i, points[i]);
        }

        var split = new HashSet<(int, int)>();
        foreach ((int a, int b) in edges)
        {
            SplitEdge(a, b, distance, cellSize, points, settled, nearby, split);
        }

        var lines = new List<Vector3>(split.Count * 2);
        foreach ((int a, int b) in split)
        {
            lines.Add(points[a]);
            lines.Add(points[b]);
        }
        return lines;
    }

    public static Mesh ToMesh(List<Vector3> lines, List<Vector3> faceVertices = null, List<int> faceTriangles = null)
    {
        var vertices = new List<Vector3>(lines);
        var lineIndices = new int[lines.Count];
        for (int i = 0; i < lineIndices.Length; i++) lineIndices[i] = i;

        var mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        if (faceTriangles == null)
        {
            mesh.SetVertices(vertices);
            mesh.SetIndices(lineIndices, MeshTopology.Lines, 0);
            return mesh;
        }

        vertices.AddRange(faceVertices);
        var faceIndices = new int[faceTriangles.Count];
        for (int i = 0; i < faceIndices.Length; i++) faceIndices[i] = faceTriangles[i] + lines.Count;

        mesh.SetVertices(vertices);
        mesh.subMeshCount = 2;
        mesh.SetIndices(lineIndices, MeshTopology.Lines, 0);
        mesh.SetIndices(faceIndices, MeshTopology.Triangles, 1);
        return mesh;
    }

    static int Snap(Vector3 point, float distance, List<Vector3> points, List<Vector3> totals, List<int> counts, PointGrid grid, HashSet<int> nearby)
    {
        nearby.Clear();
        grid.CollectNear(point, nearby);

        int best = -1;
        float bestDistance = distance * distance;
        foreach (int id in nearby)
        {
            float candidate = (points[id] - point).sqrMagnitude;
            if (candidate <= bestDistance)
            {
                bestDistance = candidate;
                best = id;
            }
        }

        if (best < 0)
        {
            best = points.Count;
            points.Add(point);
            totals.Add(Vector3.zero);
            counts.Add(0);
            grid.Add(best, point);
        }

        totals[best] += point;
        counts[best]++;
        return best;
    }

    static void SplitEdge(int a, int b, float distance, float cellSize, List<Vector3> points, PointGrid grid, HashSet<int> nearby, HashSet<(int, int)> split)
    {
        Vector3 from = points[a];
        Vector3 direction = points[b] - from;
        float length = direction.magnitude;
        if (length <= 0f) return;
        direction /= length;

        nearby.Clear();
        for (float t = 0f; t < length; t += cellSize)
        {
            grid.CollectNear(from + direction * t, nearby);
        }
        grid.CollectNear(points[b], nearby);

        var along = new List<(float position, int id)>();
        foreach (int id in nearby)
        {
            if (id == a || id == b) continue;

            Vector3 offset = points[id] - from;
            float position = Vector3.Dot(offset, direction);
            if (position <= distance || position >= length - distance) continue;
            if ((offset - direction * position).sqrMagnitude <= distance * distance)
            {
                along.Add((position, id));
            }
        }
        along.Sort((left, right) => left.position.CompareTo(right.position));

        int previous = a;
        foreach ((float _, int id) in along)
        {
            split.Add(GetEdgeKey(previous, id));
            previous = id;
        }
        split.Add(GetEdgeKey(previous, b));
    }

    static (int, int) GetEdgeKey(int a, int b)
    {
        return a < b ? (a, b) : (b, a);
    }

    static void AddEdge(Dictionary<(int, int), List<Vector3>> edges, int a, int b, Vector3 normal)
    {
        (int, int) key = GetEdgeKey(a, b);
        if (!edges.TryGetValue(key, out List<Vector3> normals))
        {
            normals = new List<Vector3>();
            edges[key] = normals;
        }
        normals.Add(normal);
    }

    static bool IsHardEdge(List<Vector3> normals, float minDot)
    {
        if (normals.Count == 1) return true;

        for (int i = 1; i < normals.Count; i++)
        {
            if (Mathf.Abs(Vector3.Dot(normals[0], normals[i])) < minDot) return true;
        }
        return false;
    }
}
