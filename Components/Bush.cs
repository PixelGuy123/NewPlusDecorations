using System.Collections;
using UnityEngine;

namespace NewPlusDecorations.Components
{
	public class Bush : MonoBehaviour, IClickable<int>
	{
		public void Clicked(int player)
		{
			if (isHidingPlayer)
				return;

			isHidingPlayer = true;

			Singleton<CoreGameManager>.Instance.GetPlayer(player).plm.Entity.SetHidden(true);
			Singleton<CoreGameManager>.Instance.GetPlayer(player).plm.Entity.SetFrozen(true);
			Singleton<CoreGameManager>.Instance.GetCamera(player).SetControllable(false);
			Singleton<CoreGameManager>.Instance.GetCamera(player).UpdateTargets(cameraTransform, 20);

			StartCoroutine(HideWait(Singleton<CoreGameManager>.Instance.GetPlayer(player)));
			audMan.PlaySingle(audEnter);
			hud.gameObject.SetActive(true);
			hud.worldCamera = Singleton<CoreGameManager>.Instance.GetCamera(player).canvasCam;
		}
		public void ClickableSighted(int player) { }
		public void ClickableUnsighted(int player) { }
		public bool ClickableRequiresNormalHeight() => false;
		public bool ClickableHidden() => isHidingPlayer;

		bool isHidingPlayer = false;

		[SerializeField]
		internal Canvas hud;

		[SerializeField]
		internal Transform cameraTransform;

		[SerializeField]
		internal AudioManager audMan;

		[SerializeField]
		internal SoundObject audEnter, audLeave;

		IEnumerator HideWait(PlayerManager player)
		{
			Vector3 pos = player.transform.position;
			yield return null;

			while (true)
			{
				cameraTransform.rotation = player.cameraBase.rotation;

				if (!Singleton<CoreGameManager>.Instance.Paused && (Singleton<InputManager>.Instance.GetDigitalInput("Interact", true) || player.transform.position != pos))
				{
					player.SetHidden(false);
					player.plm.Entity.SetFrozen(false);
					break;
				}
				if (Singleton<CoreGameManager>.Instance.Paused)
					hud.gameObject.SetActive(false);
				else
					hud.gameObject.SetActive(true);

				yield return null;
			}

			Singleton<CoreGameManager>.Instance.GetCamera(player.playerNumber).UpdateTargets(null, 20);
			Singleton<CoreGameManager>.Instance.GetCamera(player.playerNumber).SetControllable(true);
			hud.gameObject.SetActive(false);
			isHidingPlayer = false;
			audMan.PlaySingle(audLeave);
		}
	}
}
