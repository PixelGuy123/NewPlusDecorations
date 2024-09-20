using System.Collections;

namespace NewPlusDecorations.Components
{
	internal class EnvironmentObjectDistributor : EnvironmentObject
	{
		void Awake() =>
			StartCoroutine(AwaitForControlDistribution());

		IEnumerator AwaitForControlDistribution()
		{
			while (!ec) yield return null;

			var envs = GetComponentsInChildren<EnvironmentObject>();
			for (int i = 0; i < envs.Length; i++)
				envs[i].Ec = ec;

			yield break;
		}
	}
}
