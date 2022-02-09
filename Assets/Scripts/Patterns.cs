using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

public class PatternManager
{
    public static PatternManager Instance { get; } = new PatternManager();

    private static List<Pattern> _patterns;
    public static Dictionary<string, Pattern> _patternsByName;

    public static System.Collections.ObjectModel.ReadOnlyCollection<Pattern> Patterns => new ReadOnlyCollection<Pattern>(_patterns);

    public static ReadOnlyDictionary<string, Pattern> PatternsByName => new ReadOnlyDictionary<string, Pattern>(_patternsByName);

    #region CONSTRUCTOR
    private PatternManager()
    {
        _patterns = new List<Pattern>();
        _patternsByName = new Dictionary<string, Pattern>();

        #region 01
        List<Vector3Int> part01Indices = new List<Vector3Int>();

        for (int x = 0; x < 8; x++)
        {
            for (int z = 0; z < 2; z++)
            {
                part01Indices.Add(new Vector3Int(x, 0, z));
            }
        }

        part01Indices.Add(new Vector3Int(5, 0, 2));
        part01Indices.Add(new Vector3Int(6, 0, 2));

        AddPattern(part01Indices, "pattern Part_01P");
        #endregion


        #region 02
        List<Vector3Int> part02Indices = new List<Vector3Int>();

        for (int x = 1; x < 7; x++)
        {
            for (int z = 0; z < 3; z++)
            {
                part02Indices.Add(new Vector3Int(x, 0, z));
            }
        }

        part02Indices.Add(new Vector3Int(0, 0, 0));
        part02Indices.Add(new Vector3Int(0, 0, 1));
        part02Indices.Add(new Vector3Int(7, 0, 2));

        AddPattern(part02Indices, "pattern Part_02P");
        #endregion


        #region 03
        List<Vector3Int> part03Indices = new List<Vector3Int>();

        for (int x = 0; x < 7; x++)
        {
            for (int z = 0; z < 2; z++)
            {
                part03Indices.Add(new Vector3Int(x, 0, z));
            }
        }

        part03Indices.Add(new Vector3Int(0, 0, 2));
        part03Indices.Add(new Vector3Int(1, 0, 2));
        part03Indices.Add(new Vector3Int(2, 0, 2));
        part03Indices.Add(new Vector3Int(7, 0, 1));

        AddPattern(part03Indices, "pattern Part_03P");

        #endregion


        #region 04
        List<Vector3Int> part04Indices = new List<Vector3Int>();

        for (int x = 0; x < 9; x++)
        {
            for (int z = 0; z < 2; z++)
            {
                part04Indices.Add(new Vector3Int(x, 0, z));
            }
        }

        part04Indices.Add(new Vector3Int(4, 0, 2));
        part04Indices.Add(new Vector3Int(5, 0, 2));
        part04Indices.Add(new Vector3Int(6, 0, 2));
        part04Indices.Add(new Vector3Int(7, 0, 2));

        AddPattern(part04Indices, "pattern Part_04P");

        #endregion


        #region 05
        List<Vector3Int> part05Indices = new List<Vector3Int>();

        for (int x = 0; x < 5; x++)
        {
            for (int z = 0; z < 2; z++)
            {
                part05Indices.Add(new Vector3Int(x, 0, z));
            }
        }

        for (int x = -1; x < 2; x++)
        {
            for (int z = 6; z < 8; z++)
            {
                part05Indices.Add(new Vector3Int(x, 0, z));
            }
        }

        for (int x = -1; x < 10; x++)
        {
            for (int z = 2; z < 5; z++)
            {
                part05Indices.Add(new Vector3Int(x, 0, z));
            }
        }

        for (int x = 7; x < 10; x++)
        {
            for (int z = 6; z < 8; z++)
            {
                part05Indices.Add(new Vector3Int(x, 0, z));
            }
        }

        for (int x = 8; x < 10; x++)
        {
            for (int z = 0; z < 2; z++)
            {
                part05Indices.Add(new Vector3Int(x, 0, z));
            }
        }

        for (int x = -1; x < 3; x++)
        {
            part05Indices.Add(new Vector3Int(x, 0, 5));
        }

        for (int x = 6; x < 10; x++)
        {
            part05Indices.Add(new Vector3Int(x, 0, 5));
        }

        part05Indices.Add(new Vector3Int(-2, 0, 4));
        part05Indices.Add(new Vector3Int(-2, 0, 5));
        part05Indices.Add(new Vector3Int(7, 0, 1));
        part05Indices.Add(new Vector3Int(10, 0, 4));
        part05Indices.Add(new Vector3Int(10, 0, 5));
        part05Indices.Add(new Vector3Int(10, 0, 6));

        AddPattern(part05Indices, "pattern Part_05P");
        #endregion


        #region 06
        List<Vector3Int> part06Indices = new List<Vector3Int>();

        //y=0
        for (int x = 0; x < 3; x++)
        {
            for (int z = 0; z < 4; z++)
            {
                part06Indices.Add(new Vector3Int(x, 0, z));
            }
        }

        for (int x = 7; x < 10; x++)
        {
            for (int z = 0; z < 4; z++)
            {
                part06Indices.Add(new Vector3Int(x, 0, z));
            }
        }

        for (int x = 3; x < 7; x++)
        {
            for (int z = 3; z < 6; z++)
            {
                part06Indices.Add(new Vector3Int(x, 0, z));
            }
        }

        for (int x = 7; x < 9; x++)
        {
            for (int z = 4; z < 6; z++)
            {
                part06Indices.Add(new Vector3Int(x, 0, z));
            }
        }

        part06Indices.Add(new Vector3Int(-1, 0, 0));
        part06Indices.Add(new Vector3Int(-1, 0, 1));
        part06Indices.Add(new Vector3Int(3, 0, 1));
        part06Indices.Add(new Vector3Int(3, 0, 2));
        part06Indices.Add(new Vector3Int(4, 0, 2));
        part06Indices.Add(new Vector3Int(6, 0, 2));
        part06Indices.Add(new Vector3Int(10, 0, 1));
        part06Indices.Add(new Vector3Int(10, 0, 2));
        part06Indices.Add(new Vector3Int(1, 0, 4));
        part06Indices.Add(new Vector3Int(2, 0, 4));
        part06Indices.Add(new Vector3Int(2, 0, 5));

        //columns on y layer
        part06Indices.Add(new Vector3Int(2, 1, 2));
        part06Indices.Add(new Vector3Int(2, 2, 2));

        for (int x = 7; x < 9; x++)
        {
            for (int y = 1; y < 6; y++)
            {
                part06Indices.Add(new Vector3Int(x, y, 4));
            }
        }

        AddPattern(part06Indices, "pattern Part_06P");
        #endregion


        #region 07
        List<Vector3Int> part07Indices = new List<Vector3Int>();

        //y=0
        for (int x = 1; x < 9; x++)
        {
            for (int z = 0; z < 4; z++)
            {
                part07Indices.Add(new Vector3Int(x, 0, z));
            }
        }

        for (int x = 2; x < 8; x++)
        {
            part07Indices.Add(new Vector3Int(x, 0, 4));
        }

        part07Indices.Add(new Vector3Int(0, 0, 0));
        part07Indices.Add(new Vector3Int(0, 0, 1));
        part07Indices.Add(new Vector3Int(0, 0, 2));
        part07Indices.Add(new Vector3Int(9, 0, 0));
        part07Indices.Add(new Vector3Int(9, 0, 1));
        part07Indices.Add(new Vector3Int(9, 0, 2));

        //columns on y layer
        for (int x = 4; x < 6; x++)
        {
            for (int y = 1; y < 3; y++)
            {
                part07Indices.Add(new Vector3Int(x, y, 3));
            }
        }

        AddPattern(part07Indices, "pattern Part_07P");
        #endregion


        #region 08
        List<Vector3Int> part08Indices = new List<Vector3Int>();

        for (int x = 2; x < 7; x++)
        {
            for (int z = 0; z < 8; z++)
            {
                part08Indices.Add(new Vector3Int(x, 0, z));
            }
        }

        part08Indices.Add(new Vector3Int(0, 0, 0));
        part08Indices.Add(new Vector3Int(1, 0, 0));
        part08Indices.Add(new Vector3Int(1, 0, 1));
        part08Indices.Add(new Vector3Int(7, 0, 0));
        part08Indices.Add(new Vector3Int(4, 0, 8));
        part08Indices.Add(new Vector3Int(5, 0, 8));

        AddPattern(part08Indices, "pattern Part_08P");
        #endregion
    }

    //check if the pattern is valid. if yes, it can be added to the list
    public bool AddPattern(List<Vector3Int> indices, string name)
    {
        //only add valid patterns
        if (indices == null) return false;
        if (indices[0] != Vector3Int.zero) return false;
        if (_patterns.Count(p => p.Name == name) > 0) return false;
        _patterns.Add(new Pattern(new List<Vector3Int>(indices), _patterns.Count, name));
        _patternsByName.Add(name, _patterns.Last());
        return true;
    }

    //return pattern linked to the index
    public static Pattern GetPatternByIndex(int index) => Patterns[(int)index];

    public static Pattern GetPatternByName(string name) => PatternsByName[name];
    #endregion
}


public class Pattern
{
    public ReadOnlyCollection<Vector3Int> Indices { get; }
    public int Index { get; }
    public string Name { get; }

    public Pattern(List<Vector3Int> indices, int index, string name)
    {
        Indices = new ReadOnlyCollection<Vector3Int>(indices);
        Index = index;
        Name = name;
    }
}
