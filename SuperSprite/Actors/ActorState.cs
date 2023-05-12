using System;

public partial class ActorState {

	private readonly Action EnterState;
	private readonly Action<double> ProcessState;
	private readonly Action<double> PhysicsProcessState;
	private readonly Action ExitState;

	public ActorState(Action OnEnterState, Action<double> OnProcessState, Action<double> OnPhysicsProcessState, Action OnExitState){
		this.EnterState = OnEnterState;
		this.ProcessState = OnProcessState;
		this.PhysicsProcessState = OnPhysicsProcessState;
		this.ExitState = OnExitState;
	}

	public void OnEnterState() => EnterState();
	public void OnProcessState(double delta) => ProcessState(delta);
	public void OnPhysicsProcessState(double delta) => PhysicsProcessState(delta);
	public void OnExitState() => ExitState();
}
