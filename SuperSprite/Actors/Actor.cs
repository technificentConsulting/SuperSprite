using Godot;


public partial class Actor : CharacterBody3D
{
	public float Lifetime {get; protected set;}
	public ActorState CurrentState {get; protected set;}
	public ActorState PreviousState  {get; protected set;}
	protected float StateChangeTimer;

	//Actor methods - override these instead of Godot's!
	public virtual void AEnterTree() { }
	public virtual void APhysicsPreProcess(double delta) { }
	public virtual void APhysicsPostProcess(double delta) { }
	public virtual void AProcess(double delta) { }
	public virtual void APostProcess(double delta) { }

	public virtual ActorState GetDefaultState() { return null; }
	public virtual void Damage(Actor other) { }
	public virtual void Kill(Actor other) { }

	//Behind-the-scenes implementation of certain systems that call the A-prefixed methods at the appropriate time

	public override void _EnterTree()
	{
		AEnterTree();

		//AxisLockAngularZ = true;

		CurrentState = GetDefaultState();
		PreviousState = CurrentState;
		CurrentState?.OnEnterState();
	}

	public override void _PhysicsProcess(double delta)
	{
		Lifetime += (float)delta;
		StateChangeTimer += (float)delta;

		APhysicsPreProcess(delta);
		CurrentState?.OnPhysicsProcessState(delta);
		APhysicsPostProcess(delta);

	}

	public override void _Process(double delta)
	{
		AProcess(delta);
		CurrentState?.OnProcessState(delta);
		APostProcess(delta);
	}

	//Helper methods for handling states and transitions between them
	public void ChangeState(ActorState newState)
	{
		CurrentState.OnExitState();
		PreviousState = CurrentState;
		CurrentState = newState;
		StateChangeTimer = 0;
		CurrentState.OnEnterState();
	}

	public float GetElapsedTimeInState()
	{
		return StateChangeTimer;
	}
}
