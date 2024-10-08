using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Theme data container", fileName = "ThemeDataContainer")]
public class ThemeDataContainer : ScriptableObject
{
    [SerializeField] private ThemeTileData snowThemeData;
    [SerializeField] private ThemeTileData fireThemeData;
    [SerializeField] private ThemeTileData grassThemeData;
    
    public ThemeTileData GetThemeTileData(ThemesEnum targetTheme)
    {
        switch (targetTheme)
        {
            case ThemesEnum.None:
                return null;
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
}
