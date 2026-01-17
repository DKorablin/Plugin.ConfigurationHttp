using System;
using System.Threading;
using System.Threading.Tasks;

namespace Plugin.ConfigurationHttp.Ipc
{
	/// <summary>Exclusive execution of a piece of code in a process (For IPC)</summary>
	internal class IpcSingleton
	{
		private readonly String _name;
		private readonly TimeSpan _timeout;

		public IpcSingleton(String name, TimeSpan timeout)
		{
			if(String.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			this._name = name;
			this._timeout = timeout;
		}

		public void Mutex<T>(T state, Action<T> func)
		{
			String mutexId = this._name;

			using(Mutex mutex = new Mutex(false, mutexId, out Boolean _))
			{
				Boolean hasHandle = false;
				try
				{
					try
					{
						hasHandle = mutex.WaitOne(this._timeout, false);
						if(!hasHandle)
							throw new TimeoutException("Timeout waiting for exclusive access");
					} catch(AbandonedMutexException)
					{// Log the fact that the mutex was abandoned in another process, it will still get acquired
						hasHandle = true;
					}

					func?.Invoke(state);
				} finally
				{
					if(hasHandle)//If we got handle, then close mutex
						mutex.ReleaseMutex();
				}
			}
		}

		public async Task MutexAsync<T>(T state, Func<T, Task> func)
		{
			String mutexId = this._name;

			using(Mutex mutex = new Mutex(false, mutexId, out Boolean _))
			{
				Boolean hasHandle = false;
				try
				{
					try
					{
						hasHandle = mutex.WaitOne(this._timeout, false);
						if(!hasHandle)
							throw new TimeoutException("Timeout waiting for exclusive access");
					} catch(AbandonedMutexException)
					{// Log the fact that the mutex was abandoned in another process, it will still get acquired
						hasHandle = true;
					}

					if(func != null)
						await func.Invoke(state);
				} finally
				{
					if(hasHandle)//If we got handle, then close mutex
						mutex.ReleaseMutex();
				}
			}
		}

		public void EventWaitHandle<T>(T state, Action<T> func)
		{
			String ewhId = this._name;
			using(EventWaitHandle ewh = new EventWaitHandle(false, EventResetMode.AutoReset, ewhId, out Boolean isNew))
			{
				Boolean hasHandle = false;
				try
				{
					if(!isNew)
					{
						hasHandle = ewh.WaitOne(this._timeout, false);
						if(!hasHandle)
							throw new TimeoutException("Timeout waiting for exclusive access");
					}

					func?.Invoke(state);
				} finally
				{
					if(hasHandle)
						ewh.Set();
				}
			}
		}

		public void Semaphore<T>(Int32 initialCount, Int32 maximumCount, T state, Action<T> func)
		{
			String semaphoreId = this._name;

			using(Semaphore s = new Semaphore(initialCount, maximumCount, semaphoreId, out Boolean isNew))
			{
				Boolean hasHandle = false;
				try
				{
					if(!isNew)
					{
						hasHandle = s.WaitOne(this._timeout, false);
						if(!hasHandle)
							throw new TimeoutException("Timeout waiting for exclusive access");
					}

					func?.Invoke(state);
				} finally
				{
					if(hasHandle)
						s.Release();
				}
			}
		}
	}
}