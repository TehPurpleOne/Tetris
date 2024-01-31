using Godot;
using System;

public class DeadTile : KinematicBody2D
{
    public Sprite s;

    private Vector2 velocity = Vector2.Zero;
    private float gravity = 900;

    public override void _Ready() {
        s = (Sprite)GetNode("Sprite");

        Random RNGesus = new Random();

        velocity.x = RNGesus.Next(-200, 200);
        velocity.y = RNGesus.Next(-350, -50);
    }

    public override void _PhysicsProcess(float delta) {
        
        velocity.y += gravity * delta;

        velocity = MoveAndSlide(velocity, Vector2.Up);
    }
}
