using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Theme data container", fileName = "ThemeDataContainer")]
public class ThemeDataContainer : ScriptableObject
{
    public static List<ThemesEnum> Themes = new List<ThemesEnum> { 
        ThemesEnum.Snow,
        ThemesEnum.Fire,
        ThemesEnum.Grass,
    };

    [SerializeField] private ThemeTileData noneThemeData;
    [SerializeField] private ThemeTileData snowThemeData;
    [SerializeField] private ThemeTileData fireThemeData;
    [SerializeField] private ThemeTileData grassThemeData;
    
    public ThemeTileData GetThemeTileData(ThemesEnum targetTheme)
    {
        switch (targetTheme)
        {
            case ThemesEnum.None:
                return noneThemeData;
            case ThemesEnum.Snow:
                return snowThemeData;
            case ThemesEnum.Fire:
                return fireThemeData;
            case ThemesEnum.Grass:
                return grassThemeData;
            default:
                break;
        }
        return null;
    }

    public static Color GetThemeColor(ThemesEnum targetTheme)
    {
        switch (targetTheme)
        {
            case ThemesEnum.None:
                return Color.white;
            case ThemesEnum.Snow:
                return Color.cyan;
            case ThemesEnum.Fire:
                return Color.red;
            case ThemesEnum.Grass:
                return Color.green;
            default:
                break;
        }

        return Color.black;
    }
}
