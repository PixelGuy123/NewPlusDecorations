using PixelInternalAPI.Extensions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NewPlusDecorations.Components
{
	public class Bird : EnvironmentObject
	{
#pragma warning disable IDE0051 // Remover membros privados não utilizados
		public override void LoadingFinished()
		{
			base.LoadingFinished();
			ogPos = transform.position;
			roomControl = ec.CellFromPosition(transform.position).room;
			StartCoroutine(Fly(true));
		}


		void OnTriggerStay(Collider other)
#pragma warning restore IDE0051 // Remover membros privados não utilizados
		{
			if (isIdle && other.GetComponent<Entity>())
			{
				ray.origin = transform.position;
				ray.direction = other.transform.position - transform.position;
				if (Physics.Raycast(ray, out hit, 999f) && hit.transform == other.transform)
				{
					isIdle = false;
					if (idleCor != null)
						StopCoroutine(idleCor);
					collider.enabled = false;

					if (flyCor != null)
						StopCoroutine(flyCor);
					flyCor = StartCoroutine(Fly(false));
				}
			}
		}

		IEnumerator Idle()
		{
			rotator.targetSprite = sprIdle[Random.Range(0, sprIdle.Length)];
			isIdle = true;
			collider.enabled = true;
			rotator.BypassRotation(true);
			while (true)
			{
				float delay = Random.Range(minIdleDelay, maxIdleDelay);
				while (delay > 0f)
				{
					delay -= ec.EnvironmentTimeScale * Time.deltaTime;
					yield return null;
				}

				if (Random.value <= eatSomethingChance)
				{
					delay = eatDelay;
					float frame = 0;
					while (delay > 0f)
					{
						delay -= ec.EnvironmentTimeScale * Time.deltaTime;

						frame += ec.EnvironmentTimeScale * Time.deltaTime * Random.Range(minEatSpeed, maxEatSpeed);
						frame %= sprFloorEat.Length;

						rotator.targetSprite = sprFloorEat[Mathf.FloorToInt(frame)];
						yield return null;
					}
				}

				rotator.targetSprite = sprIdle[Random.Range(0, sprIdle.Length)];
				yield return null;
			}
		}

		IEnumerator Fly(bool spawn)
		{
			var target = roomControl.TileAtIndex(Random.Range(0, roomControl.TileCount));
			var targetPos = target.FloorWorldPosition;
			ec.FindPath(ec.CellFromPosition(transform.position), target, PathType.Nav, out var pathToCopy, out bool success);
			if (success)
				pathToFollow.AddRange(pathToCopy);

			rotator.BypassRotation(false);
			float frame = 0f;

			if (!spawn)
				audMan.PlaySingle(audFlyAway);
			Vector3 pos = transform.position;

			while (true) // Flying away
			{
				frame += flySpriteSpeed * ec.EnvironmentTimeScale * Time.deltaTime;
				frame %= sprFlyingAway.Length;
				rotator.targetSprite = sprFlyingAway[Mathf.FloorToInt(frame)];

				bool hasPath = pathToFollow.Count != 0;
				Vector3 positionToGo = hasPath ?
					pathToFollow[0].FloorWorldPosition : targetPos;

				Vector3 dir = (positionToGo - pos.ZeroOutY()).normalized;
				transform.rotation = Quaternion.LookRotation(dir);
				pos += dir * flySpeed * ec.EnvironmentTimeScale * Time.deltaTime;

				if (hasPath && ec.CellFromPosition(pos) == pathToFollow[0])
					pathToFollow.RemoveAt(0);

				float extraSpeed = hasPath ? 1f : 5f;

				pos.y += flySpeed * ec.EnvironmentTimeScale * Time.deltaTime * extraSpeed;
				if (pos.y > maxFlyHeightToDespawn)
					break;


				transform.position = pos;


				yield return null;
			}

			pathToFollow.Clear();
			renderer.enabled = false;
			if (!spawn)
			{
				float delay = timeOutside;
				while (delay > 0f)
				{
					delay -= ec.EnvironmentTimeScale * Time.deltaTime;
					yield return null;
				}
			}
			renderer.enabled = true;

			bool goToOgPos = ogPos.y != this.groundHeight && Random.value <= chanceToGoToOgSpot;
			float groundHeight = goToOgPos ?
				ogPos.y : this.groundHeight;

			target = goToOgPos ? ec.CellFromPosition(ogPos) : roomControl.RandomEventSafeCellNoGarbage();
			targetPos = goToOgPos ? target.FloorWorldPosition : 
				target.FloorWorldPosition + new Vector3(Random.Range(-landOffset, landOffset), 0f, Random.Range(-landOffset, landOffset));

			var startCell = roomControl.TileAtIndex(Random.Range(0, roomControl.TileCount));
			pos = startCell.FloorWorldPosition + Vector3.up * maxFlyHeightToDespawn;

			ec.FindPath(startCell, target, PathType.Nav, out pathToCopy, out success);
			if (success)
				pathToFollow.AddRange(pathToCopy);

			while (true) // Flying away
			{
				frame += flySpriteSpeed * ec.EnvironmentTimeScale * Time.deltaTime;
				frame %= sprFlyingAway.Length;
				rotator.targetSprite = sprFlyingAway[Mathf.FloorToInt(frame)];

				bool hasPath = pathToFollow.Count != 0;
				Vector3 positionToGo = hasPath ?
					pathToFollow[0].FloorWorldPosition : targetPos;

				Vector3 dir = (positionToGo - pos.ZeroOutY()).normalized;
				transform.rotation = Quaternion.LookRotation(dir);
				pos += dir * flySpeed * ec.EnvironmentTimeScale * Time.deltaTime * (pos.y - groundHeight < 7.5f && goToOgPos ? Mathf.Max(1f, Vector3.Distance(pos, ogPos)) : 1f);

				if (hasPath && ec.CellFromPosition(pos) == pathToFollow[0])
					pathToFollow.RemoveAt(0);

				float extraSpeed = hasPath ? 1f : 5f;

				pos.y -= flySpeed * ec.EnvironmentTimeScale * Time.deltaTime * extraSpeed;
				if (pos.y <= groundHeight)
				{
					pathToFollow.Clear();

					pos.y = groundHeight;
					transform.position = pos;

					idleCor = StartCoroutine(Idle());
					yield break;
				}

				transform.position = pos;


				yield return null;
			}
		}

		[SerializeField]
		internal AudioManager audMan;

		[SerializeField]
		internal SoundObject audFlyAway;

		[SerializeField]
		internal SpriteRenderer renderer;

		[SerializeField]
		internal Sprite[] sprIdle, sprFloorEat, sprFlyingAway;

		[SerializeField]
		internal AnimatedSpriteRotator rotator;

		[SerializeField]
		internal CapsuleCollider collider;

		[SerializeField]
		internal float minIdleDelay = 0.25f, maxIdleDelay = 1.25f, flySpeed = 18f, maxFlyHeightToDespawn = 70f, flySpriteSpeed = 19f, timeOutside = 8.5f, groundHeight = 0f, eatDelay = 1.5f, minEatSpeed = 10f, maxEatSpeed = 45f,
			landOffset = 2.25f;

		[SerializeField]
		[Range(0f, 1f)]
		internal float eatSomethingChance = 0.15f, chanceToGoToOgSpot = 0.45f;

		Coroutine idleCor, flyCor;
		bool isIdle = false;
		RoomController roomControl;
		readonly List<Cell> pathToFollow = [];
		Vector3 ogPos;

		Ray ray = new();
		RaycastHit hit;
	}
}
