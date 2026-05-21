using UnityEngine;

/// <summary>
/// Row-major sprite sheets for Character0 (5 direction rows per animation).
/// Built by JingJu → Import Character0 Sprites.
/// </summary>
[CreateAssetMenu(fileName = "Character0SpriteLibrary", menuName = "JingJu/Character0 Sprite Library")]
public sealed class Character0SpriteLibrary : ScriptableObject
{
    public const int DirectionRows = 5;

    [Header("Core locomotion (5 rows × N columns)")]
    public Sprite[] idle;
    public Sprite[] walk;
    public Sprite[] run;

    public int idleColumns = 8;
    public int walkColumns = 6;
    public int runColumns = 4;

    public bool IsReady => idle != null && idle.Length > 0 && walk != null && walk.Length > 0;

    public Sprite GetFrame(Sprite[] sheet, int directionRow, int column, int columnsPerRow)
    {
        if (sheet == null || sheet.Length == 0 || columnsPerRow <= 0)
            return null;

        directionRow = Mathf.Clamp(directionRow, 0, DirectionRows - 1);
        column = Mathf.Clamp(column, 0, columnsPerRow - 1);
        int index = directionRow * columnsPerRow + column;
        if (index < 0 || index >= sheet.Length)
            return null;

        return sheet[index];
    }
}
