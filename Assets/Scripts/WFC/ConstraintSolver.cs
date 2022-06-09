using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;
//using Eppy; //ADDED ??DO WE NEED THIS??

public class ConstraintSolver : MonoBehaviour
{
    #region Serialized fields
    [SerializeField]
    //Prefabs linked to the manager through the inspector
    private List<GameObject> _goPatterns;
    [SerializeField]
    private Vector3Int _gridDimensions;
    [SerializeField]
    public float TileSize;
    #endregion

    #region private fields
    Tile[,,] _tileGrid;
    List<TilePattern> _patternLibrary; //prefabs
    List<TileConnection> _connections;
    readonly int _maxSteps = 5000;
    #endregion

    #region constructors
    void Start()
    {
        //Add all connections
        _connections = new List<TileConnection>();

        _connections.Add(new TileConnection("conPink"));
        _connections.Add(new TileConnection("conYellow"));
        _connections.Add(new TileConnection("conBlue"));
        _connections.Add(new TileConnection("conOrange"));
        _connections.Add(new TileConnection("conCyan"));    
        _connections.Add(new TileConnection("conGreen"));    
        _connections.Add(new TileConnection("conBlack"));    

        //Add all patterns
        _patternLibrary = new List<TilePattern>();
        foreach (var goPattern in _goPatterns)
        {
            _patternLibrary.Add(new TilePattern(_patternLibrary.Count, goPattern, _connections));
        }

        MakeTiles();
        //FillGridRandom();
        for (int i = 0; i < _maxSteps; i++)
        {
            WaveFunctionCollapseStep();
        }
    }
    #endregion

    #region private functions
    /// <summary>
    /// Create the tile grid
    /// </summary>
    private void MakeTiles()
    {
        _tileGrid = new Tile[_gridDimensions.x, _gridDimensions.y, _gridDimensions.z];
        for (int x = 0; x < _gridDimensions.x; x++)
        {
            for (int y = 0; y < _gridDimensions.y; y++)
            {
                for (int z = 0; z < _gridDimensions.z; z++)
                {
                    _tileGrid[x, y, z] = new Tile(new Vector3Int(x, y, z), _patternLibrary, this);
                }
            }
        }
    }

    private void FillGridRandom()
    {
        //Loop over all the tiles
        ////Assign a random pattern per tile
        GetTilesFlattened().ForEach(t => t.AssignRandomPossiblePattern());
    }

    /// <summary>
    /// Run one step of the WaveFunctionCollapse. Put this in a loop to solve the entire grid.
    /// </summary>
    private void WaveFunctionCollapseStep()
    {
        List<Tile> unsetTiles = GetUnsetTiles();
        //Return if there are no tiles left
        if (unsetTiles.Count == 0)
        {
            Debug.Log("all tiles are set");
            return;
        }

        //Count how many possible patterns there are
        //Find all the tiles with the least amount of possible patterns
        //Select a random tile out of this list
        //Get the tiles with the least amount of possible patterns
        List<Tile> leastTiles = new List<Tile>();
        int leastTile = int.MaxValue;

        foreach (Tile tile in unsetTiles)
        {
            if (tile.NumberOfPossiblePatterns < leastTile)
            {
                leastTiles = new List<Tile>();

                leastTile = tile.NumberOfPossiblePatterns;
            }
            if (tile.NumberOfPossiblePatterns == leastTile)
            {
                leastTiles.Add(tile);
            }
        }

        //Select a random tile out of the list
        int rndIndex = Random.Range(0, leastTiles.Count);
        Tile tileToSet = leastTiles[rndIndex];

        //Assign one of the possible patterns to the tile
        tileToSet.AssignRandomPossiblePattern();

        //PropogateGrid on the set tile
        PropagateGrid(tileToSet);
    }

    public void PropagateGrid(Tile setTile)
    {
        //Loop over all cartesian directions (list is in Util)
        ////Get the neighbour of the set tile in the direction
        ////Get the connection of the set tile in the direction
        ////Get all the tilepatterns with the same connection in opposite direction
        ////Remove all the possiblePatterns from neighbour tile that are not in the connection list. 
        ////Run the CrossreferenceConnectionPatterns() on the neighbour tile
        ////If a tile has only one possiblePattern
        //////Set the tile
        //////PropogateGrid for this tile
        ///

        for (int i = 0; i < Util.Directions.Count; i++)
        {
            var neighbour = GetNeighbour(setTile, Util.Directions[i]);
            if (neighbour == null) Debug.Log($"No neighbour for {setTile} in direction {Util.Directions[i]}");

            var connection = setTile.PossiblePatterns.First().Connections[i];
            var oppositeDirectionIndex = Util.InversedDirections[i];
            List<TilePattern> sameConnectionTiles = neighbour.PossiblePatterns.Where(p => p.Connections[oppositeDirectionIndex] == connection).ToList();
            neighbour.CrossreferenceConnectionPatterns(sameConnectionTiles);
            if (neighbour.IsSet)
            {
                neighbour.AssignPattern(neighbour.PossiblePatterns.First());
            }
        }
    }

    private Tile GetNeighbour(Tile tile, Vector3Int direction)
    {
        //Get the neighbour of a tile in a certain direction
        Vector3Int neighbourIndex = tile.Index + direction;
        return GetTileByIndex(neighbourIndex);
    }

    private Tile GetTileByIndex(Vector3Int index)
    {
        if (!Util.CheckInBounds(_gridDimensions, index) || _tileGrid[index.x, index.y, index.z] == null)
        {
            Debug.Log($"A tile at {index} doesn't exist");
            return null;
        }
        return _tileGrid[index.x, index.y, index.z];
    }

    private List<Tile> GetUnsetTiles()
    {
        List<Tile> unsetTiles = new List<Tile>();

        //Loop over all the tiles and check which ones are not set
        foreach (var tile in GetTilesFlattened())
        {
            if (!tile.IsSet) unsetTiles.Add(tile);
        }
        return unsetTiles;
    }

    /// <summary>
    /// Get a flattened list of tiles
    /// </summary>
    /// <returns>list of tiles</returns>
    private List<Tile> GetTilesFlattened()
    {
        List<Tile> tiles = new List<Tile>();
        for (int x = 0; x < _gridDimensions.x; x++)
        {
            for (int y = 0; y < _gridDimensions.y; y++)
            {
                for (int z = 0; z < _gridDimensions.z; z++)
                {
                    tiles.Add(_tileGrid[x, y, z]);
                }
            }
        }
        return tiles;
    }
}


//private void PropogateGrid(Tile changedTile)            //ADDED
//{
//    //Loop over the connections of the changedTile
//    //Per connection: go to the neighbour tile in the connection direction
//    //Crossreference the list of possible connections in the neighbour tile with the list of possible patterns in the connection

//    //If one or multiple of the neighbours has no more possible tilepattern, solving failed, start over
//    //you could assign a possible tile of the previous propogation, this will cause impurities but might make it easier to solve

//    //If one or multiple of the neighbours has only one possible tilepattern, set the tile pattern
//    //propogate the grid for the new set tile
//}
#endregion

