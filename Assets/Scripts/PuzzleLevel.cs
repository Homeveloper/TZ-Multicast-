using UnityEngine;

[CreateAssetMenu(menuName = "Puzzle/Puzzle Level")]
public class PuzzleLevel : ScriptableObject
{
    public string levelName;
    public Texture2D image;
    public int rows = 2;
    public int columns = 2;
}