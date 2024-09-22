using System.Collections;
using UnityEngine;

namespace NewPlusDecorations.Components
{
	public class Couch : EnvironmentObject, IClickable<int>
	{
		public void Clicked(int player)
		{
			if (beingUsed) return;

			this.player = Singleton<CoreGameManager>.Instance.GetPlayer(player);
			Singleton<CoreGameManager>.Instance.GetPlayer(player).plm.Entity.SetFrozen(true);
			Singleton<CoreGameManager>.Instance.GetCamera(player).SetControllable(false);

			beingUsed = true;
			camTarget.position = Singleton<CoreGameManager>.Instance.GetCamera(player).transform.position;
			camTarget.rotation = Singleton<CoreGameManager>.Instance.GetCamera(player).transform.rotation;
			Singleton<CoreGameManager>.Instance.GetCamera(player).UpdateTargets(camTarget, camVal);

			StartCoroutine(PlayerInCouch(Singleton<CoreGameManager>.Instance.GetPlayer(player)));
		}
		public void ClickableSighted(int player) { }
		public void ClickableUnsighted(int player) { }
		public bool ClickableHidden() => beingUsed;
		public bool ClickableRequiresNormalHeight() => true;

		IEnumerator PlayerInCouch(PlayerManager pm)
		{
			Vector3 tarPos = transform.position + transform.forward * 4.5f;
			Vector3 rotation = Quaternion.LookRotation(tarPos - transform.position).eulerAngles;
			pm.transform.eulerAngles = rotation;
			Vector3 prevPlayerPos = player.transform.position;
			player.Teleport(tarPos);
			yield return null;
			Vector3 pos = player.transform.position;

			tarPos.y = pos.y;

			Vector3 expectedDir = rotation;
			expectedDir.x += 5f;

			Vector3 ogDir = camTarget.eulerAngles;

			float t = 0;
			float t2 = 0;
			do
			{
				t = Mathf.Min(1f, t + (Mathf.Abs(Mathf.Cos(t) * 1.75f) * ec.EnvironmentTimeScale * Time.deltaTime));
				t2 = Mathf.Min(1f, t2 + (Mathf.Abs(Mathf.Cos(t2) * 3.5f) * ec.EnvironmentTimeScale * Time.deltaTime));

				camTarget.eulerAngles = Vector3.Lerp(ogDir, expectedDir, t);
				camTarget.position = Vector3.Lerp(prevPlayerPos, tarPos, t2);

				if (player.transform.position != pos)
					goto endAnimation;


				yield return null;
			} while (t < 1f || t2 < 1f);

			float delay = 0.4f;
			while (delay > 0f)
			{
				delay -= ec.EnvironmentTimeScale * Time.deltaTime;
				yield return null;
			}

			if (player.transform.position != pos)
				goto endAnimation;

			expectedDir = transform.eulerAngles;
			ogDir = camTarget.eulerAngles;
			tarPos = transform.position + Vector3.up * 0.5f;
			t = 0;
			t2 = 0;
			do
			{
				t = Mathf.Min(1f, t + (Mathf.Abs(Mathf.Cos(t) * 1.75f) * ec.EnvironmentTimeScale * Time.deltaTime));
				t2 = Mathf.Min(1f, t2 + (Mathf.Abs(Mathf.Cos(t2) * 3.5f) * ec.EnvironmentTimeScale * Time.deltaTime));

				camTarget.eulerAngles = Vector3.Lerp(ogDir, expectedDir, t);
				camTarget.position = Vector3.Lerp(pos, tarPos, t2);
				if (player.transform.position != pos)
					goto endAnimation;


				yield return null;
			} while (t < 1f || t2 < 1f);

			while (true)
			{
				if (!Singleton<CoreGameManager>.Instance.Paused && (Singleton<InputManager>.Instance.GetDigitalInput("Interact", true) || player.transform.position != pos))
				{
					player = null;
					break;
				}
				player.plm.AddStamina(player.plm.staminaMax * staminaIncreaseRate, true);
				yield return null;
			}

			if (pm)
			{
				if (pm.transform.position == pos)
				{
					expectedDir = rotation;
					expectedDir.x = 15f;
					ogDir = camTarget.eulerAngles;

					prevPlayerPos = camTarget.position;
					tarPos = camTarget.position + camTarget.forward * 2.35f;
					tarPos.y = camTarget.position.y - 0.78f;


					t = 0;
					t2 = 0;
					do
					{
						t = Mathf.Min(1f, t + (Mathf.Abs(Mathf.Cos(t) * 2.1f) * ec.EnvironmentTimeScale * Time.deltaTime));
						t2 = Mathf.Min(1f, t2 + (Mathf.Abs(Mathf.Cos(t2) * 2.5f) * ec.EnvironmentTimeScale * Time.deltaTime));

						camTarget.eulerAngles = Vector3.Lerp(ogDir, expectedDir, t);
						camTarget.position = Vector3.Lerp(prevPlayerPos, tarPos, t2);

						if (pm.transform.position != pos)
							goto endAnimation;

						yield return null;
					} while (t < 1f || t2 < 1f);

					expectedDir = rotation;
					ogDir = camTarget.eulerAngles;
					tarPos = camTarget.position;
					t = 0;
					t2 = 0;
					do
					{
						t = Mathf.Min(1f, t + (Mathf.Abs(Mathf.Cos(t) * 1.75f) * ec.EnvironmentTimeScale * Time.deltaTime));
						t2 = Mathf.Min(1f, t2 + (Mathf.Abs(Mathf.Cos(t2) * 3.5f) * ec.EnvironmentTimeScale * Time.deltaTime));

						camTarget.eulerAngles = Vector3.Lerp(ogDir, expectedDir, t);
						camTarget.position = Vector3.Lerp(tarPos, pos, t2);
						if (pm.transform.position != pos)
							goto endAnimation;

						yield return null;
					} while (t < 1f || t2 < 1f);
				}
			}
		endAnimation:
			if (pm)
			{
				pm.plm.Entity.SetFrozen(false);
				Singleton<CoreGameManager>.Instance.GetCamera(pm.playerNumber).UpdateTargets(null, camVal);
				Singleton<CoreGameManager>.Instance.GetCamera(pm.playerNumber).SetControllable(true);
				pm.transform.eulerAngles = rotation;
			}
			beingUsed = false;
			yield break;
		}

		bool beingUsed = false;

		const int camVal = 26; // Higher the target index, less the priority is

		PlayerManager player = null;

		[SerializeField]
		internal Transform camTarget;

		[SerializeField]
		[Range(0f, 1f)]
		internal float staminaIncreaseRate = 0.05f;
	}
}
