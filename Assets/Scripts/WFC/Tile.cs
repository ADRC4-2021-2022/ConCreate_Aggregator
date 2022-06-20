using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Tile
{
    #region Variables
    public List<TilePattern> PossiblePatterns;
    public Vector3Int Index;
    public Vector3 RealWorldPosition;
    public TilePattern CurrentTile;
    public GameObject CurrentGo;

    private bool _emptySet = false;
    private bool _showconnections = true;
    private Vector3 _tileSize;
    private readonly ConstraintSolver _solver;

    //A tile is set if there is only one possible pattern
    public bool Set
    {
        get
        {
            return (PossiblePatterns.Count == 1) || _emptySet;
        }
    }

    public int NumberOfPossiblePatterns
    {
        get
        {
            return PossiblePatterns.Count;
        }
    }
    #endregion

    #region constructors
    public Tile(Vector3Int index, List<TilePattern> tileLibrary, ConstraintSolver solver, Vector3 tileSize)
    {
        PossiblePatterns = tileLibrary;
        Index = index;
        RealWorldPosition = Util.IndexToRealPosition(index, tileSize);
        _solver = solver;
        _tileSize = tileSize;
    }
    #endregion

    #region public functions
    public void AssignRandomPossiblePattern()
    {
        if (PossiblePatterns.Count == 0)
        {
            _emptySet = true;
            Debug.Log($"No pattern available for tile {Index} ");
        }
        //At the moment we will set the tile. This will allow for empty tiles. Better to create a generic tile and assign this one. Even better to keep track of the former option and select on of those
        else
        {
            //Select a random pattern out of the list of possible patterns
            int rndPatternIndex = Random.Range(0, PossiblePatterns.Count);

            PossiblePatterns = new List<TilePattern>() { PossiblePatterns[rndPatternIndex] };
            AssignPattern(PossiblePatterns[0]);
        }
    }

    public void AssignPattern(TilePattern pattern)
    {
        if (CurrentGo != null)
        {
            GameObject.Destroy(CurrentGo);
        }

        CurrentGo = GameObject.Instantiate(_solver.GOPatternPrefabs[pattern.Index]);
        CurrentGo.name = $"{_solver.GOPatternPrefabs[pattern.Index].name} [{Index.x}, {Index.y}, {Index.z}]";
        CurrentGo.transform.position = RealWorldPosition;
        _solver.TileGOs.Add(CurrentGo);
        CurrentTile = pattern;
        var neighbours = GetNeighbours();

        // set neighbour.PossiblePatterns to match what this tile defines
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
                neighbour.PossiblePatterns = neighbour.PossiblePatterns.Where(p => {
                    // CATCH THESE CASES FIRST

                    // ALLOW RED TO RED (must be done before checking pink to pink, so we enforce exterior walls)
                    if (p.HasFaceWithConnectionType(opposite, ConnectionType.WFC_connRed) && p.Connections[opposite].Count == 1 && CurrentTile.HasFaceWithConnectionType(i, ConnectionType.WFC_connRed))
                    {
                        return true;
                    }
                    else if (p.HasFaceWithConnectionType(opposite, ConnectionType.WFC_connRed) && CurrentTile.HasFaceWithConnectionType(i, ConnectionType.WFC_connRed) && CurrentTile.Connections[i].Count == 1)
                    {
                        return true;
                    }
                    // DON'T ALLOW PINK TO PINK
                    else if (p.HasFaceWithConnectionType(opposite, ConnectionType.WFC_conn0) && CurrentTile.HasFaceWithConnectionType(i, ConnectionType.WFC_conn0))
                    {
                        return false;
                    }

                    // THEN WE CAN SAFELY CATCH THESE

                    //// ALLOW BLUE TO PINK
                    //else if (p.HasFaceWithConnectionType(opposite, ConnectionType.WFC_connBlue) && CurrentTile.HasFaceWithConnectionType(i, ConnectionType.WFC_conn0))
                    //{
                    //    return true;
                    //}
                    //// ALLOW PINK TO BLUE
                    //else if (p.HasFaceWithConnectionType(opposite, ConnectionType.WFC_conn0) && CurrentTile.HasFaceWithConnectionType(i, ConnectionType.WFC_connBlue))
                    //{
                    //    return true;
                    //}
                    // ALLOW YELLOW TO YELLOW
                    else if (p.HasFaceWithConnectionType(opposite, ConnectionType.WFC_connYellow) && CurrentTile.HasFaceWithConnectionType(i, ConnectionType.WFC_connYellow))
                    {
                        return true;
                    }
                    // ALLOW BLUE TO BLUE
                    else if (p.HasFaceWithConnectionType(opposite, ConnectionType.WFC_connBlue) && CurrentTile.HasFaceWithConnectionType(i, ConnectionType.WFC_connBlue))
                    {
                        return true;
                    }
                    // ALLOW GREEN TO GREEN
                    else if (p.HasFaceWithConnectionType(opposite, ConnectionType.WFC_connGreen) && CurrentTile.HasFaceWithConnectionType(i, ConnectionType.WFC_connGreen))
                    {
                        return true;
                    }
                    // ALLOW TOP TO BOTTOM
                    else if (p.HasFaceWithConnectionType(opposite, ConnectionType.WFC_connTop) && CurrentTile.HasFaceWithConnectionType(i, ConnectionType.WFC_connBottom))
                    {
                        return true;
                    }
                    // ALLOW BOTTOM TO TOP
                    else if (p.HasFaceWithConnectionType(opposite, ConnectionType.WFC_connBottom) && CurrentTile.HasFaceWithConnectionType(i, ConnectionType.WFC_connTop))
                    {
                        return true;
                    }
                    // ALLOW PINK TO GREEN
                    else if (p.HasFaceWithConnectionType(opposite, ConnectionType.WFC_conn0) && CurrentTile.HasFaceWithConnectionType(i, ConnectionType.WFC_connGreen))
                    {
                        return true;
                    }
                    // ALLOW GREEN TO PINK
                    else if (p.HasFaceWithConnectionType(opposite, ConnectionType.WFC_connGreen) && CurrentTile.HasFaceWithConnectionType(i, ConnectionType.WFC_conn0))
                    {
                        return true;
                    }
                    // ALLOW BLUE TO GREEN
                    else if (p.HasFaceWithConnectionType(opposite, ConnectionType.WFC_connBlue) && CurrentTile.HasFaceWithConnectionType(i, ConnectionType.WFC_connGreen))
                    {
                        return true;
                    }
                    // ALLOW GREEN TO BLUE
                    else if (p.HasFaceWithConnectionType(opposite, ConnectionType.WFC_connGreen) && CurrentTile.HasFaceWithConnectionType(i, ConnectionType.WFC_connBlue))
                    {
                        return true;
                    }
                    else return false;
                }).ToList();

                //Debug.Log($"Possible patterns for tile {neighbour.Index}: " + neighbour.PossiblePatterns.Count);
            }
        }
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
}
