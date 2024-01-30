using Godot;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;

public class Main : Node2D {

    private Tetrominos active;
    private TileMap back;

    private enum States {NULL, INIT, NEXT, TICKDOWN, MOVE, PLACETILE, LINECHECK, CLEAR, DROP, GAMEOVER};
    private States currentState;
    private States previousState;

    private const int COLS = 10;
    private const int ROWS = 20;
    private Vector2 startPos = new Vector2(12, 6);
    private Vector2 spawnPos = new Vector2(15, 2);
    private int moveTicker = 0;
    private int moveTickerMax = 15;
    private int clearStep = 0;

    private List<string> tetrominoPaths = new List<string>();
    private List<int> linesToCLear = new List<int>();

    public override void _Ready() {
        back = (TileMap)GetNode("Back");

        setState(States.INIT);
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
        switch(currentState) {
            case States.TICKDOWN:
                moveTicker--;
                break;
            
            case States.CLEAR:
                if(moveTicker == 0) {
                    for(int i = 0; i < linesToCLear.Count; i++) {
                        int lineY = linesToCLear[i];

                        switch(clearStep) {
                            case 0:
                                
                                break;
                        }
                    }
                }
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
                    return States.MOVE;
                }
                break;
            
            case States.MOVE:
                return States.TICKDOWN;
        }

        return States.NULL;
    }

    private void setState(States newState) {
        previousState = currentState;
        currentState = newState;

        enterState(currentState, previousState);
        exitState(previousState, currentState);
    }

    private void enterState(States newState, States oldState) {
        switch(newState) {
            case States.INIT:
                // Populate the allowed pieces table.
                tetrominoPaths.Add("res://scenes/Line.tscn");
                break;
            
            case States.NEXT:
                // Choose next piece at random
                Random RNGesus = new Random();

                string pathToLoad = tetrominoPaths[RNGesus.Next(0, tetrominoPaths.Count)];
                PackedScene loader = (PackedScene)ResourceLoader.Load(pathToLoad);
                Tetrominos result = (Tetrominos)loader.Instance();

                back.AddChild(result);
                result.Position = back.MapToWorld(spawnPos);
                active = result;
                break;
            
            case States.TICKDOWN:
                moveTicker = moveTickerMax;
                break;
            
            case States.MOVE:
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
                        setState(States.TICKDOWN);
                        break;
                }
                break;
            
            case States.PLACETILE:
                for(int i = 0; i < active.tiles.Count; i++) {
                    Sprite s = (Sprite)active.tiles[i];
                    Vector2 tilepos = back.WorldToMap(s.GlobalPosition);

                    back.SetCellv(tilepos, s.Frame);
                }

                active.QueueFree();
                setState(States.LINECHECK);
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
                        // Store the Y coordinates
                        linesToCLear.Add((int)startPos.y + i);
                        GD.Print("Clear line ",startPos.y + i);
                    }
                }
                break;
            
            case States.CLEAR:
                moveTicker = 0;
                clearStep = 0;
                break;
        }
    }

    private void exitState(States oldState, States newState) {
        
    }
}
