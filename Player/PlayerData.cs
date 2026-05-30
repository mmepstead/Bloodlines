using System.Collections;
using System.Collections.Generic;
public static class PlayerData
{
    public static int baseMaxHealth = 6;
    public static int currentHealth = baseMaxHealth;
    public static int medalCount = 3;
    public static int medallionsCollected = 0;
    public static int medalExtraHealth = 0;
    public static int maxHealth = baseMaxHealth + medalExtraHealth;
    public static int pesetas = 0;
    public static List<string> talismans = new List<string>();
    public static List<string> journalPages = new List<string>();
    public static List<string> abilities = new List<string>();

    public static void gainMedal()
    {
        medalCount += 1;
        if (medalCount % 4 == 0)
        {
            medalExtraHealth += 1;
            maxHealth = baseMaxHealth + medalExtraHealth;
            currentHealth = maxHealth;
        }
    }
}