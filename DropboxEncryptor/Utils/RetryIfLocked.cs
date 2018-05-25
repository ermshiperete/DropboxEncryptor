// // Copyright (c) 2018 SIL International
// // This software is licensed under the MIT License (http://opensource.org/licenses/MIT)

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace DropboxEncryptor.Utils
{
	public static class RetryIfLocked
	{
		// code comes from https://stackoverflow.com/a/1563234

		public static void Do(Action action, TimeSpan retryInterval, int maxAttemptCount = 3)
		{
			Do<object>(() =>
			{
				action();
				return null;
			}, retryInterval, maxAttemptCount);
		}

		public static T Do<T>(Func<T> action, TimeSpan retryInterval, int maxAttemptCount = 3)
		{
			var exceptions = new List<Exception>();

			for (var attempt = 0; attempt < maxAttemptCount; attempt++)
			{
				try
				{
					return action();
				}
				catch (IOException ex)
				{
					exceptions.Add(ex);
					if (attempt < maxAttemptCount - 1)
						Thread.Sleep(retryInterval);
				}
			}

			throw new AggregateException(exceptions);
		}
	}
}