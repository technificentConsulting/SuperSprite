using Godot;

public partial class Player : PhysicsActor
{
	//Input manager for this player instance
	public InputManager InputManager;

	//Movement parameters
	[Export]
	public float WalkAcceleration = 2;
	[Export]
	public float WalkSpeed = 7.4f;
	[Export]
	public float RunAcceleration = 2;
	[Export]
	public float RunSpeed = 12.8f;
	[Export]
	public float LongRunAcceleration = 2;
	[Export]
	public float LongRunSpeed = 18.2f;
	[Export]
	public float LongRunTime = 1;
	[Export]
	public float IdleJumpForce = 25;
	[Export]
	public float WalkJumpForce = 27.6f;
	[Export]
	public float LongRunJumpForce = 30;
	[Export]
	public float MaxJumpSustainTime = 0.45f;
	[Export]
	public float JumpSustainGravityMultiplier = 0.55f;
	[Export]
	public float AirHorizontalAcceleration = 1.5f;
	[Export(PropertyHint.Range, "-1,1")]
	public float CrouchInputThreshold = -0.5f;
	[Export(PropertyHint.Range, "0, 180")]
	public float SlideMinAngle = 5;
	[Export]
	public float SlideAcceleration = 1.4f;
	[Export]
	public float SlideSpeed = 12.8f;
	[Export]
	public Vector2 CrouchBoostForce = new Vector2(6,6);

	[Export]
	public float FloorFriction = 1;
	[Export]
	public float AirFriction = 0.4f;
	[Export]
	public float SlideFriction = 0.4f;


	//Node references
	public AnimatedSprite3D PlayerSprite;
	public Area3D ActorDetectorArea;
	public RayCast3D FloorRayCast;

	public PhysicsActor Actor;

	//Players states
	public ActorState StandState, WalkState, RunState, LongRunState, JumpState, SpinJumpState, FallState, CrouchState, SlideState;

	//Speed limit handling
	private float SpeedLimit;
	private float SpeedLerpDuration = 0.5f;
	private float SpeedLerpStartTime;
	private float SpeedLerpStartVelocity;

	//Controls whether player facing is set automatically based on player input
	private bool autoPlayerFacing = true;

	//Controls which PlayerCollisionShape is the default for the current form
	protected virtual PlayerCollisionShape GetDefaultCollisionShape() {
		return PlayerCollisionShape.BIG;
	}

	public override ActorState GetDefaultState() {return StandState;}
	
	
  public void GetInput()
    {

        Vector2 inputDirection = Input.GetVector("p1_left", "p1_right", "p1_up", "p1_down");
        Velocity.X = (float)(inputDirection.X * SPEED);
    }

	public virtual void SetupPlayer(InputManager input, Transform3D transform, Vector3 velocity, SpriteFrames spriteFrames, PhysicsActor theActor, bool spriteFlipped) 
	{
		PlayerSprite = GetNodeOrNull<AnimatedSprite3D>(new NodePath("PlayerSprite"));
		ActorDetectorArea = GetNodeOrNull<Area3D>(new NodePath("ActorDetector"));
		FloorRayCast = GetNodeOrNull<RayCast3D>(new NodePath("FloorRayCast"));

		if (PlayerSprite == null || ActorDetectorArea == null || FloorRayCast == null) GD.Print("One or multiple required child nodes could not be found! Some features won't work!");

		InputManager = input;
		GlobalTransform = transform;
		Velocity = velocity;
		PlayerSprite.SpriteFrames = spriteFrames;
		PlayerSprite.FlipH = spriteFlipped;
		Actor = theActor;


		SetPlayerCollisionShape(GetDefaultCollisionShape());
	}

	public override void AEnterTree() 
	{
		GD.Print("Player enter AEnterTree line 101");

		StandState = new ActorState(() =>
		{ //Enter State
			SnapToGround = true;
		}, (double delta) =>
		{ //Process State
			if(Mathf.Abs(Velocity.X) > 0.5f) 
			{
				PlayerSprite.Animation = PlayerAnimation.WALK;
				PlayerSprite.SpriteFrames.SetAnimationSpeed(PlayerAnimation.WALK, (Mathf.Abs(Velocity.X)) + 5);
				//		PlayerSprite.Play(PlayerAnimation.WALK, (Mathf.Abs(Velocity.X)) + 5);
			} 
			else 
			{
				PlayerSprite.Animation = PlayerAnimation.IDLE;
				//		PlayerSprite.Play(PlayerAnimation.IDLE);
			}
		}, (double delta) =>
		{ //State Physics Processing
			if(Lifetime == delta) return; //Cancel if it's this player's first physics update since IsOnFloor will always return false
			if(InputManager.DirectionalInput.X != 0) ChangeState(WalkState);
			CanFall();
			CanJump();
			CanCrouch();

		}, () =>
		{ //Exit State

		});

		WalkState = new ActorState(() =>
		{ //Enter State
			GD.Print("Player enter WalkState line 127");
			
			SetSpeedLimit(WalkSpeed);
			SnapToGround = true;
		}, (double delta) =>
		{ //Process State
			if(Mathf.Sign(Velocity.X) == Mathf.Sign(InputManager.DirectionalInput.X) || Velocity.X == 0) 
			{
				///PlayerSprite.Animation = PlayerAnimation.WALK;
				//PlayerSprite.SpriteFrames.SetAnimationSpeed(PlayerAnimation.WALK, (Mathf.Abs(Velocity.X)) + 5);
				PlayerSprite.Play(PlayerAnimation.WALK, (Mathf.Abs(Velocity.X)) + 5);
							ApplyForce2D(new Vector2(4, 0));
			//PlayerSprite._PhysicsProcess(delta);


			} 
			else 
			{
				PlayerSprite.Animation = PlayerAnimation.TURN;
				PlayerSprite.Play(PlayerAnimation.TURN);
			
			}
				MoveAndSlide();


		}, (double delta) =>
		{ //State Physics Processing
			if(InputManager.DirectionalInput.X == 0) ChangeState(StandState);
			if(InputManager.RunPressed || InputManager.AltRunPressed) ChangeState(RunState);
			CanFall();
			CanJump();
			CanCrouch();

			float force = InputManager.DirectionalInput.X * WalkAcceleration;
			ApplyForce2D(new Vector2(force, 0));
			MoveAndSlide();
			//ApplyForce2D(InputManager.DirectionalInput, WalkAcceleration);
			//			ApplyForce3D(new Vector3(0,force, 0));

				GD.Print("Player move and slide WalkState");
			
		}, () =>
		{ //Exit State

		});

		RunState = new ActorState(() =>
		{ //Enter State
				GD.Print("Player enter RunState line 160");
			SetSpeedLimit(RunSpeed);
			SnapToGround = true;
		}, (double delta) =>
		{ //Process State
			if(Mathf.Sign(Velocity.X) == Mathf.Sign(InputManager.DirectionalInput.X) || Velocity.X == 0) 
			{
				PlayerSprite.Animation = PlayerAnimation.WALK;
				//PlayerSprite.SpriteFrames.SetAnimationSpeed(PlayerAnimation.WALK, (Mathf.Abs(Velocity.X)) + 7);
				PlayerSprite.Play(PlayerAnimation.WALK, (Mathf.Abs(Velocity.X)) + 7);
			} 
			else 
			{
				PlayerSprite.Animation = PlayerAnimation.TURN;
				//	PlayerSprite.Play(PlayerAnimation.TURN);
			}
		}, (double delta) =>
		{ //State Physics Processing
			if(InputManager.DirectionalInput.X == 0) ChangeState(StandState);
			if(!InputManager.RunPressed && !InputManager.AltRunPressed) ChangeState(WalkState);
			if(GetElapsedTimeInState() > LongRunTime) ChangeState(LongRunState);
			CanFall();
			CanJump();
			CanCrouch();

			float force = InputManager.DirectionalInput.X * RunAcceleration;
			ApplyForce2D(new Vector2(force, 0));

		}, () =>
		{ //Exit State

		});

	LongRunState = new ActorState(() =>
		{ //Enter State
				GD.Print("Player enter LongRun line 194");
			PlayerSprite.Animation = PlayerAnimation.LONG_RUN;
			SetSpeedLimit(LongRunSpeed);
			SnapToGround = true;
		}, (double delta) =>
		{ //Process State
			if(Mathf.Sign(Velocity.X) == Mathf.Sign(InputManager.DirectionalInput.X) || Velocity.X == 0) 
			{
				PlayerSprite.Animation = PlayerAnimation.LONG_RUN;
				PlayerSprite.SpriteFrames.SetAnimationSpeed(PlayerAnimation.LONG_RUN, (Mathf.Abs(Velocity.X)) + 10);
				//	PlayerSprite.Play(PlayerAnimation.LONG_RUN, (Mathf.Abs(Velocity.X)) + 10);
			} 
			else 
			{
				PlayerSprite.Animation = PlayerAnimation.TURN;
				//	PlayerSprite.Play(PlayerAnimation.TURN);
			}
		}, (double delta) =>
		{ //State Physics Processing
			if(InputManager.DirectionalInput.X == 0) ChangeState(StandState);
			if(!InputManager.RunPressed && !InputManager.AltRunPressed || Mathf.Sign(Velocity.X) != Mathf.Sign(InputManager.DirectionalInput.X)) ChangeState(WalkState);
			CanFall();
			CanJump();
			CanCrouch();

			float force = InputManager.DirectionalInput.X * LongRunAcceleration;
			ApplyForce2D(new Vector2(force, 0));

		}, () =>
		{ //Exit State

		});

		JumpState = new ActorState(() =>
		{ //Enter State
				GD.Print("Player enter JumpState line 227");
			SnapToGround = false;
			
			float speed = Mathf.Abs(Velocity.X);
			if(speed > RunSpeed) 
			{
				ApplyForce2D(new Vector2(0, LongRunJumpForce));
				//ApplyForce3D(new Vector3(0, LongRunJumpForce, 0));
				//PlayersSprite.Animation = PlayerAnimation.HIGH_JUMP;
				//PlayerSprite.Play(PlayerAnimation.HIGH_JUMP, speed);
			} 
			else if(speed > 0.5f) 
			{
				ApplyForce2D(new Vector2(0, WalkJumpForce));
				//ApplyForce3D(new Vector3(0, WalkJumpForce, 0));
				//PlayersSprite.Animation = PlayerAnimation.JUMP;
				//	PlayerSprite.Play(PlayerAnimation.JUMP, speed);
			} 
			else if(PreviousState == CrouchState) 
			{
				ApplyForce2D(new Vector2(0, IdleJumpForce));
				//ApplyForce3D(new Vector3(0, IdleJumpForce,0));
				//PlayersSprite.Animation = PlayerAnimation.CROUCH;
				SetPlayerCollisionShape(PlayerCollisionShape.SMALL);
				//PlayerSprite.Play(PlayerAnimation.CROUCH);
			} 
			else 
			{
				ApplyForce2D(new Vector2(0, IdleJumpForce));
				//ApplyForce3D(new Vector3(0, IdleJumpForce,0));
				//PlayersSprite.Animation = PlayerAnimation.JUMP;
				//PlayerSprite.Play(PlayerAnimation.JUMP, speed);
			}

		}, (double delta) =>
		{ //Process State
			//DebugText.Display("P" + PlayerNumber + "_JumpSusTime", "P" + PlayerNumber + " Jump Sustain Time: " + (MaxJumpSustainTime - GetElapsedTimeInState()).ToString());
		}, (double delta) =>
		{ //State Physics Processingslide
			if(IsOnFloor()) ChangeState(StandState);
			if((!InputManager.JumpPressed && !InputManager.AltJumpPressed) || GetElapsedTimeInState() > MaxJumpSustainTime) ChangeState(FallState);

			if(SpeedLimit >= LongRunSpeed && (InputManager.RunPressed || InputManager.AltRunPressed))
			{
				SetSpeedLimit(LongRunSpeed);
			} 
			else if(InputManager.RunPressed || InputManager.AltRunPressed) 
			{
				SetSpeedLimit(RunSpeed);
			} 
			else 
			{
				SetSpeedLimit(WalkSpeed);
			}

			//Add some force for extra air time if the jump button is held
			ApplyForce2D(Vector2.Up, Gravity.Y * (1-JumpSustainGravityMultiplier));

			float force = InputManager.DirectionalInput.X * AirHorizontalAcceleration;


		}, () =>
		{ //Exit State
			//DebugText.Remove("P" + PlayerNumber + "_JumpSusTime");
			SetPlayerCollisionShape(GetDefaultCollisionShape());
		});

		SpinJumpState = new ActorState(() =>
		{ //Enter State
				GD.Print("Player enter SpinJump line 292");
			SnapToGround = false;
			PlayerSprite.Animation = PlayerAnimation.SPIN_JUMP;

			float speed = Mathf.Abs(Velocity.X);
			if(speed > RunSpeed) 
			{
				ApplyForce2D(new Vector2(0, LongRunJumpForce));
			}
			else if(speed > 0.5f) 
			{
				ApplyForce2D(new Vector2(0, WalkJumpForce));
			} 
			else if(PreviousState == CrouchState) 
			{
				ApplyForce2D(new Vector2(0, IdleJumpForce));
			} 
			else 
			{
				ApplyForce2D(new Vector2(0, IdleJumpForce));
			}
		}, (double delta) =>
		{ //Process State
			//DebugText.Display("P" + PlayerNumber + "_JumpSusTime", "P" + PlayerNumber + " Jump Sustain Time: " + (MaxJumpSustainTime - GetElapsedTimeInState()).ToString());
		}, (double delta) =>
		{ //State Physics Processing
			if(IsOnFloor()) ChangeState(StandState);
			if((!InputManager.JumpPressed && !InputManager.AltJumpPressed) || GetElapsedTimeInState() > MaxJumpSustainTime) ChangeState(FallState);

			if(SpeedLimit >= LongRunSpeed && (InputManager.RunPressed || InputManager.AltRunPressed))
			{
				SetSpeedLimit(LongRunSpeed);
			} 
			else if(InputManager.RunPressed || InputManager.AltRunPressed) 
			{
				SetSpeedLimit(RunSpeed);
			} 
			else 
			{
				SetSpeedLimit(WalkSpeed);
			}

			//Add some force for extra air time if the jump button is held
			ApplyForce2D(Vector2.Up, Gravity.Y * (1-JumpSustainGravityMultiplier));

			float force = InputManager.DirectionalInput.X * AirHorizontalAcceleration;
			ApplyForce2D(new Vector2(force, 0));
		}, () =>
		{ //Exit State
			//DebugText.Remove("P" + PlayerNumber + "_JumpSusTime");
		});

		FallState = new ActorState(() =>
		{ //Enter State
				GD.Print("Player enter FallState line 350");
			if(InputManager.DirectionalInput.Y < CrouchInputThreshold) 
			{
				PlayerSprite.Animation = PlayerAnimation.CROUCH;
				SetPlayerCollisionShape(PlayerCollisionShape.SMALL);
				//		PlayerSprite.Play("Crouch");
			} 
			else if (PreviousState == SpinJumpState) 
			{
				PlayerSprite.Animation = PlayerAnimation.SPIN_JUMP;
				//		PlayerSprite.Play("SpinJump");
			} 
			else 
			{
				PlayerSprite.Animation = PlayerAnimation.FALL;
				//	PlayerSprite.Play("Fall");
			}
			SnapToGround = false;
			Velocity += new Vector3(2, 0, 0);
			MoveAndSlide();

		}, (double delta) =>
		{ //Process State

		}, (double delta) =>
		{ //State Physics Processing
			if(IsOnFloor()) 
			{
				if(InputManager.DirectionalInput.Y < CrouchInputThreshold) 
				{
					ChangeState(CrouchState);
				} 
				else
				{
					ChangeState(StandState);
				}
			}

			if(SpeedLimit >= LongRunSpeed && (InputManager.RunPressed || InputManager.AltRunPressed))
			{
				SetSpeedLimit(LongRunSpeed);
			} 
			else if(InputManager.RunPressed || InputManager.AltRunPressed) 
			{
				SetSpeedLimit(RunSpeed);
			} 
			else
			 {
				SetSpeedLimit(WalkSpeed);
			}

			float force = InputManager.DirectionalInput.X * AirHorizontalAcceleration;
			ApplyForce2D(new Vector2(force, 0));

		}, () =>
		{ //Exit State
			SetPlayerCollisionShape(GetDefaultCollisionShape());
		});

		CrouchState = new ActorState(() =>
		{ //Enter State
				GD.Print("Player enter CrouchState line 405");
			SnapToGround = true;

			var angle = Mathf.Acos(FloorNormal.Dot(Vector3.Up));
			if (angle >= SlideMinAngle)
			{
				ChangeState(SlideState);
				return;
			}

			PlayerSprite.Animation = PlayerAnimation.CROUCH;
			SetPlayerCollisionShape(PlayerCollisionShape.SMALL);
			//	PlayerSprite.Play("Crouch");
		}, (double delta) =>
		{ //Process State

		}, (double delta) =>
		{ //State Physics Processing
			//Check if we're on a slope. If yes, start sliding.
			var angle = Mathf.Acos(FloorNormal.Dot(Vector3.Up));
			if(angle >= SlideMinAngle)
			{
				ChangeState(SlideState);
				return;
			}

			Transform3D originalTransform = GlobalTransform;

			Transform3D slightlyRight;
			slightlyRight.Origin = originalTransform.Origin;
			slightlyRight.Basis = originalTransform.Basis;
			slightlyRight.Origin.X += 0.3f;

			Transform3D slightlyLeft;
			slightlyLeft.Origin = originalTransform.Origin;
			slightlyLeft.Basis = originalTransform.Basis;
			slightlyLeft.Origin.X -= 0.3f;

			if(GetDefaultCollisionShape() == PlayerCollisionShape.SMALL || !TestMove(originalTransform, new Vector3(0, 0.61f, 0))){
				if(InputManager.DirectionalInput.Y >= CrouchInputThreshold) ChangeState(StandState);
				CanJump();
				CanFall();
			} 
			else if(!TestMove(slightlyRight, new Vector3(0, 0.61f, 0))) 
			{
				ApplyForce2D(new Vector2(0.6f, 0));
			} 
			else if(!TestMove(slightlyLeft, new Vector3(0, 0.61f, 0)))
			 {
				ApplyForce2D(new Vector2(-0.6f, 0));
			} 
			else
			 {
				if(InputManager.JumpJustPressed || InputManager.AltJumpJustPressed) { ApplyForce2D(new Vector2(CrouchBoostForce.X * InputManager.DirectionalInput.X, CrouchBoostForce.Y)); }
			}
			
		}, () =>
		{ //Exit State
			SetPlayerCollisionShape(GetDefaultCollisionShape());
		});
	
		SlideState = new ActorState(() =>
		{ //Enter State
				GD.Print("Player enter SlideState line 470");
			SetSpeedLimit(SlideSpeed);
			SnapToGround = true;

			PlayerSprite.Animation = PlayerAnimation.SLIDE;
			SetPlayerCollisionShape(PlayerCollisionShape.SMALL);
			autoPlayerFacing = false;
			//		PlayerSprite.Play("Slide");
		}, (double delta) =>
		{ //Process State
			//Manually flip the sprite according to the movement direction
			if (Velocity.X > 0)
			{
				PlayerSprite.FlipH = false;
			}
			else if (Velocity.X < 0)
			{
				PlayerSprite.FlipH = true;
			}
		}, (double delta) =>
		{ //State Physics Processing
			float angle = Mathf.Acos(FloorNormal.Dot(Vector3.Up));
			if (angle < SlideMinAngle) ChangeState(InputManager.DirectionalInput.Y < CrouchInputThreshold ? CrouchState : StandState );
			CanFall();
			CanJump();

			float force = Mathf.Sign(FloorNormal.X) * SlideAcceleration;
			ApplyForce2D(new Vector2(force, 0));

		}, () =>
		{ //Exit State
			autoPlayerFacing = true;
			SetPlayerCollisionShape(GetDefaultCollisionShape());
		});

	
	}

	protected void CanJump(){
		//		GD.Print("Player enter CanJump line 506");
		if(InputManager.JumpJustPressed) 
		{
			ChangeState(JumpState);
		} 
		else if (InputManager.AltJumpJustPressed) 
		{
			ChangeState(SpinJumpState);
		}
	}

	protected void CanFall() 
	{
		//GD.Print("Player enter CanFall line 519");
		if(!IsOnFloor()) ChangeState(FallState);
	}

	protected void CanCrouch() 
	{
		//GD.Print("Player enter CanCrouch line 525");
		if(IsOnFloor() && InputManager.DirectionalInput.Y < CrouchInputThreshold) ChangeState(CrouchState);
	}

	public override void APhysicsPreProcess(double delta)
	{
		InputManager.UpdateInputs();
	}

	public override void APhysicsPostProcess(double delta)
	{
		//Apply friction
		if(CurrentState == CrouchState || CurrentState == SlideState)
		{
			Velocity.X = Mathf.Max(Mathf.Abs(Velocity.X) - SlideFriction, 0) * Mathf.Sign(Velocity.X);
		} 
		else 
		{
			Velocity.X = Mathf.Max((Mathf.Abs(Velocity.X) - (IsOnFloor() ? FloorFriction : AirFriction)), 0) * Mathf.Sign(Velocity.X);
		}

		//Lerp to speed limit if above
		if(Mathf.Abs(Velocity.X) > SpeedLimit) { Velocity.X = ClampedInterpolation.Lerp(SpeedLerpStartVelocity, SpeedLimit, (Lifetime - SpeedLerpStartTime) * (1/SpeedLerpDuration)) * Mathf.Sign(Velocity.X); }

		//DebugText.Display("P" + PlayerNumber + "_SpeedLimit", "P" + PlayerNumber + " Speed Limit: " + SpeedLimit.ToString());

	}

	public override void APostProcess(double delta) 
	{
		//Flip the sprite correctly
		if(autoPlayerFacing)
		{
			if (InputManager.DirectionalInput.X > 0)
			{
				PlayerSprite.FlipH = false;
			}
			else if (InputManager.DirectionalInput.X < 0)
			{
				PlayerSprite.FlipH = true;
			}
		}
		
		//DebugText.Display("P1_Position", "P1 Position: " + GlobalTransform.origin.ToString());
		//DebugText.Display("P" + PlayerNumber + "_Velocity", "P" + PlayerNumber + " CharVelocity: " + CharVelocity.ToString());
		//DebugText.Display("P" + PlayerNumber + "_StateTime", "P" + PlayerNumber + " Time in State: " + GetElapsedTimeInState().ToString());
	}

	protected void SetSpeedLimit(float limit)
	{
		if(limit == SpeedLimit) return;

		SpeedLerpStartTime = Lifetime;
		SpeedLerpStartVelocity = Mathf.Abs(Velocity.X);
		SpeedLimit = limit;
	}

	protected enum PlayerCollisionShape {SMALL, BIG}
	protected void SetPlayerCollisionShape(PlayerCollisionShape shape) 
	{
		switch (shape) 
		{
			case PlayerCollisionShape.SMALL:
			ShapeOwnerSetDisabled(0, false);
			ShapeOwnerSetDisabled(1, true);
			break;

			case PlayerCollisionShape.BIG:
			ShapeOwnerSetDisabled(0, true);
			ShapeOwnerSetDisabled(1, false);
			break;
		}
	}

	public struct PlayerAnimation 
	{
		public const string IDLE = "Idle";
		public const string WALK = "Walk";
		public const string LONG_RUN = "LongRun";
		public const string TURN = "Turn";
		public const string JUMP = "Jump";
		public const string HIGH_JUMP = "HighJump";
		public const string SPIN_JUMP = "SpinJump";
		public const string FALL = "Fall";
		public const string CROUCH = "Crouch";
		public const string SLIDE = "Slide";
	}
}
