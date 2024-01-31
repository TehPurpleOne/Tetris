using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public class Main : Node2D
{
    private Tetrominos active;
    private Tetrominos next;
    private Tetrominos hold;
    private TileMap back;
    private ColorRect flash;

    private enum States {NULL, INIT, NEXT, MOVE, HOLD, PLACETILE, LINECHECK, CLEAR, DROP, GAMEOVER};
    private States currentState;
    private States previousState;

    private const int COLS = 10;
    private const int ROWS = 20;
    private Vector2 startPos = new Vector2(11, 6);
    private Vector2 spawnPos = new Vector2(25, 13);
    private Vector2 topPos = new Vector2(14, 3);
    private float fallSpeedMax = 1.0f; // Adjust for higher levels.
    private float fallSpeed = 1.0f;
    private float fallTimer = 0.0f;
    private int clearStep = 0;
    private bool canHold = true;
    private int lastLinesCleared = 0;
    private int linesToNextLevel = 10;

    private int level = 1;
    private int lines = 0;
    private int topScore = 0;
    private int score = 0;
    private int statsI = 0;
    private int statsO = 0;
    private int statsL = 0;
    private int statsJ = 0;
    private int statsS = 0;
    private int statsZ = 0;
    private int statsT = 0;

    private List<string> tetrominoPaths = new List<string>();
    private List<int> linesToClear = new List<int>();

    public override void _Ready() {
        back = (TileMap)GetNode("Back");
        flash = (ColorRect)GetNode("ColorRect");

        updateTexts();

        setState(States.INIT);
    }

    public override void _Input(InputEvent @event) {
        if(currentState != States.MOVE) {
            return;
        }

        if(Input.GetActionStrength("left") > 0.2)
            moveTetromino(Vector2.Left);
        else if (Input.GetActionStrength("right") > 0.2)
            moveTetromino(Vector2.Right);
        else if (Input.GetActionStrength("down") > 0.2)
            fallTetromino();

        if(Input.GetActionStrength("rotateLeft") > 0.2 && Input.IsActionJustPressed("rotateLeft"))
            rotateTetromino(Vector2.Left);
        else if(Input.GetActionStrength("rotateRight") > 0.2 && Input.IsActionJustPressed("rotateRight"))
            rotateTetromino(Vector2.Right);

        if(Input.GetActionStrength("hold") > 0.2 && Input.IsActionJustPressed("hold") && canHold) {
            setState(States.HOLD);
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
            case States.MOVE:
                fallTimer += delta;

                if(fallTimer >= fallSpeed) {
                    fallTetromino();
                    fallTimer = 0.0f;
                }
                break;
            
            case States.CLEAR:
                if(fallTimer < fallSpeed && flash.Color != Color.Color8(0, 0, 0)) {
                    flash.Color = Color.Color8(0, 0, 0);
                }

                if(fallTimer >= fallSpeed) {
                    if(linesToClear.Count >= 4) {
                        flash.Color = Color.Color8(255, 255, 255);
                    }

                    if(clearStep < 6) {
                        for(int i = 0; i < linesToClear.Count; i++) {
                            int a = 15 - clearStep;
                            int b = 16 + clearStep;

                            back.SetCellv(new Vector2(a, linesToClear[i]), -1);
                            back.SetCellv(new Vector2(b, linesToClear[i]), -1);
                        }
                    }
                    
                    clearStep++;
                    fallTimer = 0.0f;
                }

                fallTimer += delta;
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
                    return States.MOVE;
                }
                break;
            
            case States.HOLD:
                if(active == null) {
                    return States.NEXT;
                } else {
                    return States.MOVE;
                }

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

        fallTimer = 0;

        GD.Print("Entering state: ",newState);

        enterState(currentState, previousState);
        exitState(previousState, currentState);
    }

    private void enterState(States newState, States oldState) {
        switch(newState) {
            case States.INIT:
                // Populate the allowed pieces table.
                tetrominoPaths.Add("res://scenes/I.tscn");
                tetrominoPaths.Add("res://scenes/O.tscn");
                tetrominoPaths.Add("res://scenes/L.tscn");
                tetrominoPaths.Add("res://scenes/J.tscn");
                tetrominoPaths.Add("res://scenes/S.tscn");
                tetrominoPaths.Add("res://scenes/Z.tscn");
                tetrominoPaths.Add("res://scenes/T.tscn");
                break;
            
            case States.NEXT:
                if(linesToClear.Count > 0) {
                    int totalLinesCleared = linesToClear.Count;

                    switch(totalLinesCleared) {
                        case 1:
                            score += 100;
                            break;
                        
                        case 2:
                            score += 200;
                            break;
                        
                        case 3:
                            score += 400;
                            break;
                        
                        case 4:
                            if(lastLinesCleared == 4) score += 1200; else score += 800;
                            break;
                    }
                    score = Mathf.Clamp(score, 0, 9999900);

                    if(topScore < score) {
                        topScore = score;
                    }

                    lines += totalLinesCleared;
                    lines = Mathf.Clamp(lines, 0, 999);
                    
                    if(lines >= linesToNextLevel) {
                        level++;
                        level = Mathf.Clamp(level, 0, 99);
                        linesToNextLevel += 10;
                    }

                    updateTexts();

                    linesToClear.Clear();
                }
                
                // Choose next piece at random
                if(next == null) {
                    next = spawnTetromino();

                    next.GlobalPosition = back.MapToWorld(new Vector2(25, 13));

                    if(next.Name != "I" && next.Name != "O") {
                        next.GlobalPosition += Vector2.One * 4;
                    }
                }
                
                if(next != null) {
                    active = next;
                    next = spawnTetromino();

                    next.GlobalPosition = back.MapToWorld(new Vector2(25, 13));

                    if(next.Name != "I" && next.Name != "O") {
                        next.GlobalPosition += Vector2.One * 4;
                    }

                    // Make the tiles in the sprite invisible so they don't bleed through the background.
                    for(int i = 0; i < active.tiles.Count; i++) {
                        Sprite s = (Sprite)active.tiles[i];

                        s.Hide();
                    }

                    active.GlobalPosition = back.MapToWorld(topPos);
                }
                break;
            
            case States.MOVE:
                fallSpeedMax = 1.2f - (0.2f * level);
                fallSpeedMax = Mathf.Clamp(fallSpeedMax, 0, 2);

                fallSpeed = fallSpeedMax; // Adjust for game level and speed.
                break;
            
            case States.HOLD:
                if(hold == null) {
                    hold = active;
                    active = null;
                } else {
                    Tetrominos swap = active;
                    Vector2 swapPos = swap.GlobalPosition;
                    active = hold;
                    hold = swap;

                    active.GlobalPosition = swapPos;
                }

                hold.GlobalPosition = back.MapToWorld(new Vector2(25, 23));

                for(int i = 0; i < hold.tiles.Count; i++) { // Make all tiles visible.
                    Sprite s = (Sprite)hold.tiles[i];

                    s.Show();
                }

                if(hold.Name != "I" && hold.Name != "O") {
                    hold.GlobalPosition += Vector2.One * 4;
                }

                canHold = false;
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

                updateStats(active.Name);

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

                canHold = true;
                break;
            
            case States.CLEAR:
                fallSpeed = (1.0f / Engine.GetFramesPerSecond()) * 4;
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

        if(result.Name != "I" && result.Name != "O") {
            result.GlobalPosition += Vector2.One * 4;
        }

        return result;
    }

    private void spawnDeathBlock(Vector2 pos, int value) {
        string pathToLoad = "res://scenes/DeadTile.tscn";
        PackedScene loader = (PackedScene)ResourceLoader.Load(pathToLoad);
        DeadTile result = (DeadTile)loader.Instance();

        AddChild(result);
        result.s.Frame = value;
    }

    private void fallTetromino() {
        bool check = false;

        for(int i = 0; i < active.tiles.Count; i++) {
            Sprite s = (Sprite)active.tiles[i];
            Vector2 tilePos = back.WorldToMap(s.GlobalPosition) + Vector2.Down;

            if(back.GetCellv(tilePos) != -1 || tilePos.y >= 26) {
                check = true;
            }
        }

        switch(check) {
            case true:
                setState(States.PLACETILE);
                break;
            
            case false:
                active.GlobalPosition += Vector2.Down * 8;

                // This is to ensure that tiles below the top of the gameboard are visible.
                for(int i = 0; i < active.tiles.Count; i++) {
                    Sprite s = (Sprite)active.tiles[i];
                    Vector2 tilePos = back.WorldToMap(s.GlobalPosition) + Vector2.Down;

                    if(tilePos.y > 6) {
                        s.Show();
                    }
                }
                break;
        }
    }

    private void moveTetromino(Vector2 dir) {
        bool check = false;

        for(int i = 0; i < active.tiles.Count; i++) {
            Sprite s = (Sprite)active.tiles[i];
            Vector2 tilePos = back.WorldToMap(s.GlobalPosition) + dir;

            if(back.GetCellv(tilePos) != -1 || tilePos.x < 11 || tilePos.x > 20) {
                check = true;
            }
        }

        if(!check) {
            active.GlobalPosition += dir * 8;
        }
    }

    private void rotateTetromino(Vector2 dir) {
        bool check = false;
        List<Vector2> tilePoses = new List<Vector2>();
        Vector2 newTPos = Vector2.Zero;

        // First, rotate the tetromino so we can store accurate position information
        active._degrees += dir.x * 90;

        for(int i = 0; i < active.tiles.Count; i++) { // Add the values to the position table
            Sprite s = (Sprite)active.tiles[i];
            Vector2 tilePos = back.WorldToMap(s.GlobalPosition);

            tilePoses.Add(tilePos);
        }
        GD.Print("Desired tile positions updated.");

        // Reset the original tetromino orientation
        active._degrees -= dir.x * 90;

        // Now we'll see if the piece needs to be pushed towards the middle of the board if the tiles are out of bounds.
        for(int i = 0; i < tilePoses.Count; i++) {
            Vector2 tilePos = tilePoses[i];

            if(tilePos.x < 11) {
                newTPos += Vector2.Right;
            }

            if(tilePos.x > 20) {
                newTPos += Vector2.Left;
            }
        }

        // With the new position found, let's add that to the saved values.
        if(newTPos != Vector2.Zero) {
            for(int i = 0; i < tilePoses.Count; i++) {
                tilePoses[i] += newTPos;
            }
        }

        // Finally, we can cross reference these values with whats on the game board to see if a rotation is possible.
        for(int i =0; i < tilePoses.Count; i++) {
            Vector2 tilePos = tilePoses[i];

            if(back.GetCellv(tilePos) != -1) {
                check = true;
            }
        }
        
        if(!check) { // All checks were successful! Rotate the tetromino!
            active.GlobalPosition += newTPos * 8;
            active._degrees += dir.x * 90;

            // Show tetromino tiles if they are below the top of the play field. Hide those that are not.
            for(int i = 0; i < active.tiles.Count; i++) {
                Sprite s = (Sprite)active.tiles[i];
                Vector2 tilePos = back.WorldToMap(s.GlobalPosition) + Vector2.Down;

                if(tilePos.y > 6) {
                    s.Show();
                } else {
                    s.Hide();
                }
            }
        }
    }

    private void updateStats(string which) {
        switch(which) {
            case "I":
                statsI++;
                statsI = Mathf.Clamp(statsI, 0, 999);
                break;
            
            case "O":
                statsO++;
                statsO = Mathf.Clamp(statsO, 0, 999);
                break;
            
            case "L":
                statsL++;
                statsL = Mathf.Clamp(statsL, 0, 999);
                break;
            
            case "J":
                statsJ++;
                statsJ = Mathf.Clamp(statsJ, 0, 999);
                break;
            
            case "S":
                statsS++;
                statsS = Mathf.Clamp(statsS, 0, 999);
                break;
            
            case "Z":
                statsZ++;
                statsZ = Mathf.Clamp(statsZ, 0, 999);
                break;
            
            case "T":
                statsT++;
                statsT = Mathf.Clamp(statsT, 0, 999);
                break;
        }

        updateTexts();
    }

    private void updateTexts() {
        Label main = (Label)GetNode("Main");

        for(int i = 0; i < main.GetChildCount(); i++) {
            Label update = (Label)main.GetChild(i);

            switch(i) {
                case 0:
                    // Depicts game type. Update with additional game modes as necessary.
                    break;
                
                case 1:
                    update.Text = Convert.ToString(level).PadZeros(2);
                    break;
                
                case 2:
                    update.Text = Convert.ToString(lines).PadZeros(3);
                    break;
                
                case 3:
                    update.Text = Convert.ToString(topScore).PadZeros(7);
                    break;
                
                case 4:
                    update.Text = Convert.ToString(score).PadZeros(7);
                    break;
                
                case 5:
                    update.Text = Convert.ToString(statsI).PadZeros(3);
                    break;
                
                case 6:
                    update.Text = Convert.ToString(statsO).PadZeros(3);
                    break;

                case 7:
                    update.Text = Convert.ToString(statsL).PadZeros(3);
                    break;
                
                case 8:
                    update.Text = Convert.ToString(statsJ).PadZeros(3);
                    break;
                
                case 9:
                    update.Text = Convert.ToString(statsS).PadZeros(3);
                    break;
                
                case 10:
                    update.Text = Convert.ToString(statsZ).PadZeros(3);
                    break;
                
                case 11:
                    update.Text = Convert.ToString(statsT).PadZeros(3);
                    break;
            }
        }
    }
}