using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Tile
{
    #region Variables
    public List<TilePattern> PossiblePatterns;
    public Vector3Int Index;
    public TilePattern CurrentTile;
    public GameObject CurrentGo;

    private Dictionary<int, GameObject> _goTilePatternPrefabs;
    private bool _emptySet = false;
    private bool _showconnections = true;
    private Vector3 _tileSize;
    private ConstraintSolver _solver;

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

    public MeshCollider Collider
    {
        get
        {
            if (CurrentGo == null)
                return null;
            else
                return CurrentGo.GetComponentInChildren<MeshCollider>();
        }
    }
    #endregion

    #region constructors
    public Tile(Vector3Int index, List<TilePattern> tileLibrary, ConstraintSolver solver, Vector3 tileSize)
    {
        PossiblePatterns = tileLibrary;
        Index = index;
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

            AssignPattern(PossiblePatterns[rndPatternIndex]);

            PossiblePatterns = new List<TilePattern>() { PossiblePatterns[rndPatternIndex] };
        }
    }

    public void AssignPattern(TilePattern pattern)
    {
        if (CurrentGo != null)
        {
            GameObject.Destroy(CurrentGo);
        }

        CurrentGo = GameObject.Instantiate(_solver.GOPatternPrefabs[pattern.Index]);
        CurrentGo.transform.position = Util.IndexToRealPosition(Index, _tileSize);
        _solver.TileGOs.Add(CurrentGo);
        CurrentTile = pattern;
        var neighbours = GetNeighbours();

        // set neighbour.PossiblePatters to match what this tile defines
        for (int i = 0; i < neighbours.Length; i++)
        {
            var neighbour = neighbours[i];
            var connection = CurrentTile.Connections[i].Type;
            if (neighbour != null)
            {
                int opposite;
                if (i == 0) opposite = 1;
                else if (i == 1) opposite = 0;
                else if (i == 2) opposite = 3;
                else if (i == 3) opposite = 2;
                else if (i == 4) opposite = 5;
                else opposite = 4;
                neighbour.PossiblePatterns = neighbour.PossiblePatterns.Where(p => p.Connections[opposite].Type == connection).ToList();

                Debug.Log("Possible Neighbors: " + opposite);

            }
        }




        //Create a prefab of the selected pattern using the index and the voxelsize as position
        //creating a prefab of a SELECTED pattern. Where is this pattern being selected?
        //in TilePattern
        //using _goTilePrefab
        //Index
        //TileSize
        //Remove all possible patterns out of the list
        //will be using List<TilePattern> PossiblePatterns
        //remove the possible patterns that have not been selected


        //You could add some weighted randomness in here - IGNORE THIS FOR NOW UNTIL WE FIGURE OUT REST OF PROJECT

        //propogate the grid
        //_solver.PropogateGrid(this);
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


    //Contains all Connections
    public void CrossReferenceConnectionPatterns(List<TilePattern> patterns)
    {
        //Check if the patterns exist in both lists
        List<TilePattern> newPossiblePatterns = new List<TilePattern>();
        foreach (var pattern in patterns)
        {
            if (PossiblePatterns.Contains(pattern))  //if the pattern is contained in both lists...
            {
                newPossiblePatterns.Add(pattern);   //add the pattern
            }
        }

        PossiblePatterns = newPossiblePatterns;
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

    public Transform GetComponentCollider()
    {
        if (CurrentGo != null)
        {
            for (int i = 0; i < CurrentGo.transform.childCount; i++)
            {
                var child = CurrentGo.transform.GetChild(i);
                if (child.CompareTag("Component"))
                {

                    return child;
                }
            }
        }

        return null;
    }
    #endregion
}
