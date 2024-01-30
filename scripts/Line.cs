using Godot;
using System;

public class Line : Tetrominos
{
    public override void _Ready() {
        addTiles();
        setTilePos();
    }

    public override void _PhysicsProcess(float delta) {
        currentRotation = Mathf.Wrap(currentRotation, 0, maxRotations);

        if(lastRotation != currentRotation) {
            // Arrange the blocks as needed.
            setTilePos();
            lastRotation = currentRotation;
        }
    }

    private void setTilePos() {
        for(int i = 0; i < tiles.Count; i++) {
                Sprite s = (Sprite)tiles[i];

                switch(i) {
                    case 0:
                        switch(currentRotation) {
                            case 0:
                                s.Position = new Vector2(20, 4);
                                break;
                            
                            case 1:
                                s.Position = new Vector2(4, 20);
                                break;
                        }
                        break;
                    
                    case 1:
                        switch(currentRotation) {
                            case 0:
                                s.Position = new Vector2(20, 12);
                                break;
                            
                            case 1:
                                s.Position = new Vector2(12, 20);
                                break;
                        }
                        break;
                    
                    case 2:
                        s.Position = new Vector2(20, 20);
                        break;
                    
                    case 3:
                        switch(currentRotation) {
                            case 0:
                                s.Position = new Vector2(20, 28);
                                break;
                            
                            case 1:
                                s.Position = new Vector2(28, 20);
                                break;
                        }
                        break;
                }
            }
    }
}
