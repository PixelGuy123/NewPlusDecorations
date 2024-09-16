namespace NewPlusDecorations.Components
{
	internal class EnvironmentObjectDistributor : EnvironmentObject
	{
		public override void LoadingFinished()
		{
			base.LoadingFinished();
			var envs = GetComponentsInChildren<EnvironmentObject>();
			for (int i = 0; i < envs.Length; i++)
				envs[i].Ec = ec;
		}
	}
}
