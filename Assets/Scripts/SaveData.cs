using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public int rows;
    public int cols;
    public int score;
    public int seed;
    public bool gameOver;
    public List<CardSaveEntry> cards = new List<CardSaveEntry>();
}

[Serializable]
public class CardSaveEntry
{
    public int index;     // position in grid, 0..n-1
    public int cardId;    // which face id
    public bool isMatched;
}
