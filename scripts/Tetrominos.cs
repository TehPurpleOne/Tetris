using Godot;
using System;
using System.Collections.Generic;

public class Tetrominos : Node2D
{
    [Export] public int maxRotations = 0;
    public int currentRotation = 0;
    public int lastRotation = 0;

    public List<Sprite> tiles = new List<Sprite>();

    public void addTiles() {
        for(int i = 0; i < GetChildCount(); i++) {
            Sprite s = (Sprite)GetChild(i);

            tiles.Add(s);
        }
    }
}
