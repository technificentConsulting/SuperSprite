using Godot;
using System;

public partial class PhysicsActor : Actor
{
	[Export]
	public bool EnablePhysics = true;
	[Export]
	public Vector3 Gravity = new Vector3(0, -2, 0);
	public new Vector3 Velocity = Vector3.Zero;
	public  Vector3 MoveToward = Vector3.Zero;
	public const double SPEED = 6.0;
	public const double JUMP_VELOCITY = 4.5;

	[Export]
	public float MaxFloorAngle = 0.95f;
	protected Vector3 FloorNormal = new Vector3(0, 1, 0);

	public bool SnapToGround = true;

	protected Vector3 SnapVector = new Vector3(0, -0.3f, 0);
	protected Vector3 PreviousVelocity = Vector3.Zero;

	public override void _PhysicsProcess(double delta)
	{
		if (EnablePhysics)
		{
			Lifetime += (float)delta;
			StateChangeTimer += (float)delta;

			APhysicsPreProcess(delta);
			CurrentState?.OnPhysicsProcessState(delta);

			//If we're not on the ground, add gravity
			if (!IsOnFloor() || !SnapToGround){
				Velocity += Gravity;
			} else {
				Velocity.Y = 0;
			}

			//If we're running into a wall, don't build up force
			if (IsOnWall() && Mathf.Sign(Velocity.X) == Mathf.Sign(PreviousVelocity.X)) {Velocity.X = 0;}

			//If we're running into a ceiling, don't build up force
			if (IsOnCeiling() && Mathf.Sign(Velocity.Y) == Mathf.Sign(PreviousVelocity.Y)) {Velocity.Y = 0;}

			APhysicsPostProcess(delta);

			if(SnapToGround)
			{		
				ApplyFloorSnap();
			MoveAndSlide();
			
				//MoveAndSlideWithSnap(CharVelocity, SnapVector, upDirection: Vector3.Up, floorMaxAngle: 0.9f);
			}
			else {
				
				
					MoveAndSlide();
			
				//  MoveAndSlide(CharVelocity, upDirection: Vector3.Up, floorMaxAngle: 0.9f);
			
			}

			if(IsOnFloor()) FloorNormal = GetFloorNormal();

		}
		else
		{
			_PhysicsProcess(delta);
		}

	}

	//Helper methods to assist handling 2D forces in a 3D world
	public void ApplyForce2D(Vector2 force)
	{
		Velocity.X += force.X;
		Velocity.Y += force.Y;
	}


	public void ApplyForce2D(Vector2 direction, float speed)
	{
		Vector2 inputDirection = direction.Normalized();
		Vector2 force = inputDirection * speed;

		Velocity.X += force.X;
		Velocity.Y += force.Y;

	}
}
