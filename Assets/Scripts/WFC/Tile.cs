using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Tile
{
    #region Variables
    public int TileOpenness = 0;
    public List<TilePattern> PossiblePatterns;
    public Vector3Int Index;
    public Vector3 RealWorldPosition;
    public TilePattern CurrentTile;
    public GameObject CurrentGo;

    private bool _emptySet = false;
    private bool _showconnections = true;
    private readonly ConstraintSolver _solver;
    private readonly ScreenRecorder _screenRecorder;

    //A tile is set if there is only one possible pattern
    public bool Set
    {
        get
        {
            return (PossiblePatterns != null && PossiblePatterns.Count == 1) || _emptySet;
        }
    }

    public int NumberOfPossiblePatterns
    {
        get
        {
            if (PossiblePatterns != null) return PossiblePatterns.Count; else return -1;
        }
    }
    #endregion

    #region constructors
    public Tile(ScreenRecorder screenRecorder, Vector3Int index, List<TilePattern> tileLibrary, ConstraintSolver solver, Vector3 tileSize)
    {
        PossiblePatterns = tileLibrary;
        Index = index;
        RealWorldPosition = Util.IndexToRealPosition(index, tileSize);
        _solver = solver;
        _screenRecorder = screenRecorder;
    }
    #endregion

    #region public functions
    /// <summary>
    /// Attempt to 'set' this tile with whichever of it's PossiblePatterns allows it's neighbours to have > 0 PossiblePatterns
    /// </summary>
    /// <returns>true if it managed to place the tile in a valid way, false if not</returns>
    public bool AssignRandomPossiblePattern()
    {
        if (PossiblePatterns.Count == 0)
        {
            _emptySet = true;
            Debug.Log($"No pattern available for tile {Index} ");
        }
        else if (PossiblePatterns.Count == 1)
        {
            Debug.Log($"Tile {Index} is already set");
        }
        //At the moment we will set the tile. This will allow for empty tiles. Better to create a generic tile and assign this one. Even better to keep track of the former option and select on of those
        else
        {
            PossiblePatterns.Shuffle();
            var openPatterns = PossiblePatterns.Where(p => p.GOTilePrefab.CompareTag("OpenTile"));
            var openClosedPatterns = PossiblePatterns.Where(p => p.GOTilePrefab.CompareTag("OpenClosedTile"));
            var closedPatterns = PossiblePatterns.Where(p => p.GOTilePrefab.CompareTag("ClosedTile"));
            PossiblePatterns = new List<TilePattern>();
            if (GetCurrentBuildingOpenness() > _solver.TargetOpenness)
            {
                PossiblePatterns.AddRange(closedPatterns);
                PossiblePatterns.AddRange(openClosedPatterns);
                PossiblePatterns.AddRange(openPatterns);
            }
            else
            {
                PossiblePatterns.AddRange(openPatterns);
                PossiblePatterns.AddRange(openClosedPatterns);
                PossiblePatterns.AddRange(closedPatterns);
            }

            foreach (var pattern in PossiblePatterns)
            {
                if (AssignPattern(pattern))
                {
                    PossiblePatterns = new List<TilePattern>() { pattern };
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Check through this Tile's neighbours, using the TilePattern passed as a parameter, ensuring no neighbours have their PossiblePatterns.Count reduced to 0
    /// </summary>
    /// <param name="patternToAssign">The TilePattern we are trying to set this Tile to</param>
    /// <returns>True if every neighbour still has PossiblePatterns.Count > 0 after assigning the TilePattern, false if not</returns>
    public bool AssignPattern(TilePattern patternToAssign)
    {
        CurrentGo = GameObject.Instantiate(_solver.GOPatternPrefabs[patternToAssign.Index]);
        CurrentGo.name = $"Tile {_solver.GOPatternPrefabs[patternToAssign.Index].name} [{Index.x}, {Index.y}, {Index.z}]";
        CurrentGo.transform.position = RealWorldPosition;
        var neighbours = GetNeighbours();
        var exteriorWallGOsFound = new List<GameObject>();

        // set neighbour.PossiblePatterns to match what this tile defines
        var neighboursPossiblePatterns = new List<TilePattern>[6];
        for (int i = 0; i < neighbours.Length; i++)
        {
            var neighbour = neighbours[i];
            if (neighbour != null && neighbour.NumberOfPossiblePatterns != 0)
            {
                int opposite;
                if (i == 0) opposite = 1;
                else if (i == 1) opposite = 0;
                else if (i == 2) opposite = 3;
                else if (i == 3) opposite = 2;
                else if (i == 4) opposite = 5;
                else opposite = 4;

                neighboursPossiblePatterns[i] = neighbour.PossiblePatterns.Where(p =>
                {
                    foreach (var connection in p.Connections[opposite])
                    {
                        if (p.HasFaceWithConnectionType(opposite, ConnectionType.WFC_connPink) && patternToAssign.HasFaceWithConnectionType(i, ConnectionType.WFC_connPink))
                        {
                            return false;
                        }
                        if (p.HasFaceWithConnectionType(opposite, connection.Type) && patternToAssign.HasFaceWithConnectionType(i, connection.Type))
                        {
                            // If we matched to an exterior wall, store the connection
                            if (neighbour.PossiblePatterns.Contains(_solver._patternLibrary[0]))
                            {
                                var wallTag = Util.GetWallTagForConnection(i);
                                if (wallTag != null)
                                {
                                    for (int i = 0; i < CurrentGo.transform.childCount; i++)
                                    {
                                        var child = CurrentGo.transform.GetChild(i);
                                        if (child.gameObject.layer == 7)
                                        {
                                            for (int j = 0; j < child.transform.childCount; j++)
                                            {
                                                var wall = child.GetChild(j);
                                                if (wall.CompareTag(wallTag))
                                                {
                                                    exteriorWallGOsFound.Add(wall.gameObject);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            return true; // found a matching pair of connections, so return true (meaning this pattern will stay in the PossiblePatterns list for that face)
                        }
                    }
                    return false; // If we couldn't find any matching pairs of connections, so return false (meaning this pattern WILL NOT stay in the PossiblePatterns list for that face)
                }).ToList();

                // If we made any of the neighbours have no possible patterns, we need to reverse this action, so return false here
                if (neighboursPossiblePatterns[i].Count == 0)
                {
                    GameObject.Destroy(CurrentGo);
                    return false;
                }
            }
        }

        // Everything went well, so set the tile patterns for each of the neighbours
        for (int i = 0; i < neighbours.Length; i++)
        {
            var neighbour = neighbours[i];
            if (neighbour != null)
            {
                neighbour.PossiblePatterns = neighboursPossiblePatterns[i];
            }
        }
        _solver.ExteriorWallsByYLayer[Index.y].AddRange(exteriorWallGOsFound);
        _solver.TileGOs.Add(CurrentGo);
        CurrentTile = patternToAssign;

        var connections = Util.GetChildObjectsByLayer(CurrentGo.transform, LayerMask.NameToLayer("Connections"));
        foreach (var child in connections)
        {
            var renderers = child.GetComponentsInChildren<MeshRenderer>();
            foreach (var renderer in renderers)
            {
                if (!child.CompareTag("WFC_connApple") && !child.CompareTag("WFC_connBlack"))
                {
                    renderer.enabled = false;
                }
            }
        }
        //_screenRecorder.SaveScreen();

        TileOpenness = GetTilePatternOpenness();
        return true;
    }

    public Tile[] GetNeighbours()
    {
        Tile[] neighbours = new Tile[6];
        for (int i = 0; i < Util.Directions.Count; i++)
        {
            Vector3Int nIndex = Index + Util.Directions[i];
            if (nIndex.ValidateIndex(_solver.GridDimensions)) neighbours[i] = _solver.TileGrid[nIndex.x, nIndex.y, nIndex.z];
        }
        return neighbours;
    }

    public void ToggleVisibility()
    {
        _showconnections = !_showconnections;
        if (CurrentGo == null) return;
        for (int i = 0; i < CurrentGo.transform.childCount; i++)
        {
            var child = CurrentGo.transform.GetChild(i);
            if (child.gameObject.layer == 3)
            {
                child.GetComponentInChildren<MeshRenderer>().enabled = _showconnections;
            }
        }
    }
    #endregion

    #region private functions
    private int GetTilePatternOpenness()
    {
        if (CurrentGo.CompareTag("OpenTile"))
        {
            return 1;
        }
        else if (CurrentGo.CompareTag("ClosedTile"))
        {
            return -1;
        }
        else
        {
            return 0;
        }
    }

    private float GetCurrentBuildingOpenness()
    {
        int openTileCount = _solver.TileGOs.Where(GO => GO.CompareTag("OpenTile")).Count();
        int closedTileCount = _solver.TileGOs.Where(GO => GO.CompareTag("ClosedTile")).Count();
        int openClosedTileCount = _solver.TileGOs.Where(GO => GO.CompareTag("OpenClosedTile")).Count();
        if (openTileCount == 0 || closedTileCount == 0 || openClosedTileCount == 0) return 0;
        int totalTileCount = openTileCount + closedTileCount + openClosedTileCount;
        return (float)((-1 * closedTileCount) + openTileCount) / totalTileCount;
    }
    #endregion
}
