using System.Threading;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace Superluminal
{
	public class BakeDispatcher
	{

		private Lightbaker baker;

		private Thread backgroundThread;

		private IEnumerator<BakeCommand> bakeEnumerator;

		private bool disposed;

		private bool continueInBackground;

		private ManualResetEvent backgroundEvent;

		public BakeDispatcher()
		{
			backgroundEvent = new ManualResetEvent(false);

			backgroundThread = new Thread(UpdateBackground);
			backgroundThread.Start();
		}

		public void Dispose()
		{
			if (bakeEnumerator != null)
				CancelBake();

			disposed = true;

			backgroundEvent.Set();
		}
		
		public void StartBake(Lightbaker baker)
		{
			this.baker = baker;

			bakeEnumerator = baker.BakeRoutine();
		}

		public void CancelBake()
		{
			baker.CancelBake();
			bakeEnumerator = null;
		}

		public bool UpdateForeground()
		{
			if (bakeEnumerator != null && !continueInBackground)
			{
				ExecuteBake();
				return true;
			}

			return false;
		}

		private void UpdateBackground()
		{
			while (!disposed)
			{
				backgroundEvent.WaitOne();
				backgroundEvent.Reset();

				if (bakeEnumerator != null)
					ExecuteBake();
			}
		}

		private void ExecuteBake()
		{
			if (bakeEnumerator.MoveNext())
			{
				if (bakeEnumerator.Current != null)
					continueInBackground = bakeEnumerator.Current.continueInBackground;

				if (continueInBackground)
					backgroundEvent.Set();
			}
			else
				bakeEnumerator = null;
		}
		
	}

	public class BakeCommand
	{
		public readonly bool continueInBackground;

		public BakeCommand(bool continueInBackground)
		{
			this.continueInBackground = continueInBackground;
		}
	}
}
