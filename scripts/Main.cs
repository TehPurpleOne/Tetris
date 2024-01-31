using Godot;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;

public class Main : Node2D {

    private Tetrominos active;
    private Tetrominos next;
    private TileMap back;
    private ColorRect flash;

    private enum States {NULL, INIT, NEXT, TICKDOWN, MOVEDOWN, PLACETILE, LINECHECK, CLEAR, DROP, GAMEOVER};
    private States currentState;
    private States previousState;

    private const int COLS = 10;
    private const int ROWS = 20;
    private Vector2 startPos = new Vector2(11, 6);
    private Vector2 spawnPos = new Vector2(25, 13);
    private Vector2 topPos = new Vector2(14, 2);
    private int moveTicker = 0;
    private int moveTickerMax = 15;
    private int clearStep = 0;
    private Vector2 dir = Vector2.Zero;
    private bool fastDrop = false;

    private List<string> tetrominoPaths = new List<string>();
    private List<int> linesToClear = new List<int>();

    public override void _Ready() {
        back = (TileMap)GetNode("Back");
        flash = (ColorRect)GetNode("ColorRect");

        setState(States.INIT);
    }

    public override void _Input(InputEvent @event) {
        if(currentState == States.GAMEOVER) {
            return;
        }

        if(Input.GetActionStrength("left") > 0.2) {
            moveTetromino(-1);
        } else if (Input.GetActionStrength("right") > 0.2) {
            moveTetromino(1);
        }

        fastDrop = Input.GetActionStrength("down") > 0.2;

        if(Input.GetActionStrength("rotateLeft") > 0.2 && Input.IsActionJustPressed("rotateLeft")) {
            rotateTetromino(-1);
        } else if(Input.GetActionStrength("rotateRight") > 0.2 && Input.IsActionJustPressed("rotateRight")) {
            rotateTetromino(1);
        }

        if(Input.GetActionStrength("start") > 0.2) {
            GetTree().Paused = !GetTree().Paused;
        }
    }

    public override void _PhysicsProcess(float delta) {
        if(currentState != States.NULL) {
            stateLogic(delta);
            States t = getTransition(delta);
            if(t != States.NULL) {
                setState(t);
            }
        }
    }

    public void stateLogic(float delta) {
        if(GetTree().Paused) {
            return;
        }

        switch(currentState) {
            case States.TICKDOWN:

                // Move the pieces left or right
                if(fastDrop && moveTicker > 2) {
                    moveTicker = 2;
                }

                moveTicker--;
                break;
            
            case States.CLEAR:
                if(moveTicker == 0 && clearStep < 6) {
                    if(linesToClear.Count >= 4) {
                        flash.Color = Color.Color8(255, 255, 255);
                    }

                    for(int i = 0; i < linesToClear.Count; i++) {
                        int a = 15 - clearStep;
                        int b = 16 + clearStep;

                        back.SetCellv(new Vector2(a, linesToClear[i]), -1);
                        back.SetCellv(new Vector2(b, linesToClear[i]), -1);
                    }
                    clearStep++;
                }

                if(moveTicker != 0 && flash.Color != Color.Color8(0, 0, 0)) {
                    flash.Color = Color.Color8(0, 0, 0);
                }

                moveTicker++;
                moveTicker = Mathf.Wrap(moveTicker, 0, 4);
                break;
        }
    }

    private States getTransition(float delta) {
        switch(currentState) {
            case States.INIT:
                if(tetrominoPaths.Count > 0) {
                    return States.NEXT;
                }
                break;
            
            case States.NEXT:
                if(active != null) {
                    return States.TICKDOWN;
                }
                break;

            case States.TICKDOWN:
                if(moveTicker == 0) {
                    return States.MOVEDOWN;
                }
                break;
            
            case States.MOVEDOWN:
                return States.TICKDOWN;

            case States.LINECHECK:
                if(linesToClear.Count > 0) return States.CLEAR; else return States.NEXT;
        
            case States.CLEAR:
                if(clearStep == 6) {
                    return States.DROP;
                }
                break;
        }

        return States.NULL;
    }

    private void setState(States newState) {
        previousState = currentState;
        currentState = newState;

        GD.Print("Entering state: ",newState);

        enterState(currentState, previousState);
        exitState(previousState, currentState);
    }

    private void enterState(States newState, States oldState) {
        switch(newState) {
            case States.INIT:
                // Populate the allowed pieces table.
                tetrominoPaths.Add("res://scenes/Line.tscn");
                tetrominoPaths.Add("res://scenes/Square.tscn");
                tetrominoPaths.Add("res://scenes/L.tscn");
                tetrominoPaths.Add("res://scenes/J.tscn");
                tetrominoPaths.Add("res://scenes/S.tscn");
                tetrominoPaths.Add("res://scenes/Z.tscn");
                tetrominoPaths.Add("res://scenes/T.tscn");
                break;
            
            case States.NEXT:
                linesToClear.Clear();
                // Choose next piece at random
                if(next == null) {
                    next = spawnTetromino();
                }
                
                if(next != null) {
                    active = next;
                    next = spawnTetromino();

                    // Make the tiles in the sprite invisible so they don't bleed through the background.
                    for(int i = 0; i < active.tiles.Count; i++) {
                        Sprite s = (Sprite)active.tiles[i];

                        s.Hide();
                    }

                    active.GlobalPosition = back.MapToWorld(topPos);
                }
                break;
            
            case States.TICKDOWN:
                moveTicker = moveTickerMax;
                break;
            
            case States.MOVEDOWN:
                bool check = false;

                for(int i = 0; i < active.tiles.Count; i++) {
                    // Check one tile below each tile to see if there's something in the way.
                    Sprite s = (Sprite)active.tiles[i];
                    Vector2 tilePosCheck = back.WorldToMap(s.GlobalPosition) + Vector2.Down;

                    if(back.GetCellv(tilePosCheck) != -1 || tilePosCheck.y >= 26) {
                        check = true;
                    }
                }

                switch(check) {
                    case true:
                        setState(States.PLACETILE);
                        break;
                    
                    case false:
                        active.Position += Vector2.Down * 8;

                        //Make blocks that are on the board visible.
                        for(int i = 0; i < active.tiles.Count; i++) {
                            Sprite s = (Sprite)active.tiles[i];
                            Vector2 tilePos = back.WorldToMap(s.GlobalPosition);

                            if(tilePos.y >= 6) {
                                s.Show();
                            }
                        }

                        setState(States.TICKDOWN);
                        break;
                }
                break;
            
            case States.PLACETILE:
                bool gameover = false;

                for(int i = 0; i < active.tiles.Count; i++) {
                    Sprite s = (Sprite)active.tiles[i];
                    Vector2 tilepos = back.WorldToMap(s.GlobalPosition);
                    
                    if(tilepos.y >= 6) {
                        back.SetCellv(tilepos, s.Frame);
                    }

                    if(tilepos.y < 6) {
                        gameover = true;
                    }
                }

                active.QueueFree();

                if(gameover) setState(States.GAMEOVER); else setState(States.LINECHECK);
                break;
            
            case States.LINECHECK:
                for(int i = 0; i < ROWS; i++) {
                    int usedTiles = 0;
                    for(int j = 0; j < COLS; j++) {
                        if(back.GetCellv(startPos + new Vector2(j, i)) != -1) {
                            usedTiles++;

                            
                        }
                    }

                    if(usedTiles == 10) {
                        // Store the Y coordinates of the lines to be erased.
                        linesToClear.Add((int)startPos.y + i);
                    }
                }
                break;
            
            case States.CLEAR:
                moveTicker = 0;
                clearStep = 0;
                break;
            
            case States.DROP:
                flash.Color = Color.Color8(0, 0, 0);

                for(int i = 0; i < linesToClear.Count; i++) {
                    for(int j = back.GetUsedCells().Count - 1; j > -1; j--) {
                        Vector2 tilePos = (Vector2)back.GetUsedCells()[j];
                        int tileID = back.GetCellv(tilePos);

                        if(tilePos.y < linesToClear[i]) {
                            back.SetCellv(tilePos + Vector2.Down, tileID);
                            back.SetCellv(tilePos, -1);
                        }
                    }
                }

                setState(States.NEXT);
                break;
            
            case States.GAMEOVER:
                for(int i = 0; i < back.GetUsedCells().Count; i++) {
                    Vector2 tilePos = (Vector2)back.GetUsedCells()[i];
                    int tileID = back.GetCellv(tilePos);

                    spawnDeathBlock(tilePos, tileID);
                }

                back.Clear();
                break;
        }
    }

    private void exitState(States oldState, States newState) {

    }

    private Tetrominos spawnTetromino() {
        GD.Randomize();
        Random RNGesus = new Random();

        string pathToLoad = tetrominoPaths[RNGesus.Next(0, tetrominoPaths.Count)];
        PackedScene loader = (PackedScene)ResourceLoader.Load(pathToLoad);
        Tetrominos result = (Tetrominos)loader.Instance();

        back.AddChild(result);
        result.GlobalPosition = back.MapToWorld(spawnPos);

        if(result.Name != "Line" && result.Name != "Square") {
            result.GlobalPosition += Vector2.One * 4;
        }

        return result;
    }

    private void spawnDeathBlock(Vector2 pos, int value) {
        string pathToLoad = "res://scenes/DeadTile.tscn";
        PackedScene loader = (PackedScene)ResourceLoader.Load(pathToLoad);
        DeadTile result = (DeadTile)loader.Instance();

        AddChild(result);
        result.GlobalPosition = back.MapToWorld(pos) + Vector2.One * 4;
        result.s.Frame = value;
    }

    private void moveTetromino(float dir) {
        // First, check to see if the place to move the tetromino to is clear.
        Vector2 desired = Vector2.Zero;

        switch(dir) {
            case -1:
                desired.x = 99;
                for(int i = 0; i < active.tiles.Count; i++) {
                    Sprite s = (Sprite)active.tiles[i];
                    Vector2 checkPos = back.WorldToMap(s.GlobalPosition) + new Vector2(dir, 0);

                    if(checkPos.x < desired.x) {
                        desired = checkPos;
                    }
                }
                break;
            
            case 1:
                for(int i = 0; i < active.tiles.Count; i++) {
                    Sprite s = (Sprite)active.tiles[i];
                    Vector2 checkPos = back.WorldToMap(s.GlobalPosition) + new Vector2(dir, 0);

                    if(checkPos.x > desired.x) {
                        desired = checkPos;
                    }
                }
                break;
        }
        
        int tileID = back.GetCellv(desired);

        if(tileID == -1 && desired.x >= 11 && desired.x <= 20) {
            active.Position += new Vector2(dir, 0) * 8;
        }
    }

    private void rotateTetromino(float dir) {
        bool check = true;

        for(int i = 0; i < active.tiles.Count; i++) { // Check to see if the tetromino has the room to rotate.
            Sprite s = (Sprite)active.tiles[i];
            Vector2 tilePos = back.WorldToMap(s.GlobalPosition);
            Vector2 left = tilePos + Vector2.Left;
            Vector2 right = tilePos + Vector2.Right;

            if(back.GetCellv(left) != -1 || back.GetCellv(right) != -1) {
                check = false;
            }
        }

        if(check) {
            active._degrees += dir * 90;
        }

        // Tetromino placement correction.
        for(int i = 0; i < active.tiles.Count; i++) {
            Sprite s = (Sprite)active.tiles[i];
            Vector2 tilePos = back.WorldToMap(s.GlobalPosition);
            int tileID = back.GetCellv(tilePos);

            if(tilePos.x < 11) {
                active.GlobalPosition = back.MapToWorld(new Vector2(11, tilePos.y));
            }

            if(tilePos.x > 20) {
                switch(active.Name) {
                    case "Line":
                    case "Square":
                        active.GlobalPosition = back.MapToWorld(new Vector2(17, tilePos.y));
                        break;
                    
                    case "L":
                    case "J":
                    case "S":
                    case "Z":
                    case "T":
                        active.GlobalPosition = back.MapToWorld(new Vector2(18, tilePos.y));
                        break;
                }
                
            }

            if(tileID != -1) {
                active.GlobalPosition += new Vector2(-dir, 0) * 8;
            }
        }

        // Hide tiles above the play field.
        for(int i = 0; i < active.tiles.Count; i++) {
            Sprite s = (Sprite)active.tiles[i];
            Vector2 tilePos = back.WorldToMap(s.GlobalPosition);

            if(tilePos.y < 6) {
                s.Hide();
            }
        }
    }
}
