using Godot;
using System;
using System.Collections.Generic;

public partial class PlayerCamera : Camera3D
{
	[Export]
	public Vector3 TargetPositionOffset;
	private Vector3 TargetPosition;

	public override void _Process(double delta) {
		SetCameraPosition();
	}

	private void SetCameraPosition() {
		List<Vector3> playerPositions = new List<Vector3>();
		foreach (PlayerSpawner spawner in PlayerSpawner.PlayerSpawners)
		{
			if(spawner != null) playerPositions.Add(spawner.CurrentPlayerPosition);
		}

		Vector3 targetPosition = GetAverageVector(playerPositions);
		targetPosition += TargetPositionOffset;

		Position = targetPosition;
		
	}

	private Vector3 GetAverageVector(List<Vector3> inputs) {
		float xSum = 0, ySum = 0, zSum = 0;
		int count = inputs.Count;

		foreach (Vector3 vector in inputs)
		{
			xSum += vector.X;
			ySum += vector.Y;
			zSum += vector.Z;
		}

		return new Vector3(xSum/count, ySum/count, zSum/count);
	}
}
