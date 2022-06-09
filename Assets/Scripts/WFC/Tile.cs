using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile
{
    #region public fields
    public List<TilePattern> PossiblePatterns;
    public Vector3Int Index;

    //A tile is set if there is only one possible pattern
    public bool IsSet
    {
        get
        {
            return (PossiblePatterns.Count == 1);
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

    #region private fields
    private ConstraintSolver _solver;
    #endregion

    #region constructors
    public Tile(Vector3Int index, List<TilePattern> tileLibrary, ConstraintSolver solver)
    {
        PossiblePatterns = tileLibrary;
        Index = index;
        _solver = solver;
    }
    #endregion

    #region public functions
    public void AssignRandomPossiblePattern()
    {
        int randomPattern = Random.Range(0, PossiblePatterns.Count);
        AssignPattern(PossiblePatterns[randomPattern]);
    }

    public void AssignPattern(TilePattern selectedPattern)
    {
        //Create a gameobject based on the prefab of the selected pattern using the index and the tilesize as position
        GameObject GOSelectedPattern = GameObject.Instantiate(selectedPattern.GOTilePrefab, GetWorldPosition(), Quaternion.identity);
        //Remove all possible patterns out of the list

        //You could add some weighted randomness in here

        //propogate the grid
        _solver.PropagateGrid(this);
    }

    public Vector3 GetWorldPosition()
    {
        return (Vector3)Index * _solver.TileSize + Vector3.one * 0.5f * _solver.TileSize;
    }

    public void CrossreferenceConnectionPatterns(List<TilePattern> patterns)
    {
        //Return which tilePatterns are shared by 2 tiles (the geometries (walls) inside the blank tiles haven't been placed yet)
        List<TilePattern> newPossiblePatterns = new List<TilePattern>();
        foreach (var pattern in patterns)
        {
            if(PossiblePatterns.Contains(pattern))
            {
                newPossiblePatterns.Add(pattern); 
            }
        }
        PossiblePatterns = newPossiblePatterns;
    }
    #endregion
}
