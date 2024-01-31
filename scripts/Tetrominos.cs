using Godot;
using System;
using System.Collections.Generic;

public class Tetrominos : Node2D
{
    [Export] public float degrees = 0;
    public float _degrees {
        get{return degrees;}
        set{degrees = value;
        rotateTetromino(degrees);}
    }


    public List<Sprite> tiles = new List<Sprite>();

    public override void _Ready() {
        addTiles();
    }

    public void addTiles() {
        Control rotationPoint = (Control)GetNode("Control");
        for(int i = 0; i < rotationPoint.GetChildCount(); i++) {
            Sprite s = (Sprite)rotationPoint.GetChild(i);

            tiles.Add(s);
        }
    }

    private void rotateTetromino(float value) {
        degrees = value;
        degrees = Mathf.Wrap(degrees, 0, 360);

        Control c = (Control)GetNode("Control");

        c.RectRotation = degrees;

        for(int i = 0; i < tiles.Count; i++) { // Rotate the sprites so they always remain upright.
            Sprite s = (Sprite)tiles[i];

            s.RotationDegrees = -degrees;
        }
    }
}
