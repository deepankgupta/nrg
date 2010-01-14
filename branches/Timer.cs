using System;
using System.Windows.Forms;
using System.Collections.Generic;
using OpenNETCF;
using OpenNETCF.Threading;
using System.Threading;
using System.Text;
using System.Data;
using System.Collections;
using System.Xml;
using System.Runtime.CompilerServices;

namespace SmartDeviceApplication
{
    public class Timer
    {
        public int countTimer;
        public Thread SetTimerThread;
        public string storedStreamSent;
        public string storedStreamId;
        public Node node;
        public RouteTable routeTable;
        public object synchronizedLockOnThreads;
        public Semaphore semaphoreObject;
        public Dictionary<string, Thread> threadWindowForStreamSent;
        public Dictionary<string, string> bufferWindowForStreamSent;

        public Timer(int count)
        {
            countTimer = count;
            node = Node.nodeInstance;
            routeTable = RouteTable.routeTableInstance;
            storedStreamId = string.Empty;
            synchronizedLockOnThreads = new object();
            semaphoreObject = new Semaphore(1, 1);
            bufferWindowForStreamSent = new Dictionary<string, string>();
            threadWindowForStreamSent = new Dictionary<string, Thread>();
        }

        public virtual void SetTimer() { }

        public virtual void SetTimer(string sentStreamId) { }

        public virtual void SetTimerStart()
        {
            int deltaIncrement = 0;
            while (deltaIncrement <= countTimer)
            {
                Thread.Sleep(countTimer);
                deltaIncrement += countTimer;
            }

            lock (synchronizedLockOnThreads)
            {
                Thread currentThread = Thread.CurrentThread;
                IDictionaryEnumerator ide = threadWindowForStreamSent.GetEnumerator();
                while (ide.MoveNext())
                {
                    if (((Thread)ide.Value).Equals(currentThread))
                        ReleaseTimer(ide.Key.ToString());
                }
            }
        }

        public void ReleaseTimer(string storedStreamId)
        {
            lock (synchronizedLockOnThreads)
            {
                UpdateStreamBufferWindow(storedStreamId);
                UpdateThreadWindow(storedStreamId);
            }
        }

        public void ReleaseAllTimerThread()
        {
            WaitBufferWindow();
            bufferWindowForStreamSent.Clear();
            IDictionaryEnumerator ide = threadWindowForStreamSent.GetEnumerator();
            while (ide.MoveNext())
            {
                Thread storedThread = (Thread)ide.Value;
                storedThread.Abort();
            }
            threadWindowForStreamSent.Clear();
            SignalBufferWindow();
        }

        public void UpdateThreadWindow(string storedStreamId)
        {
            IDictionaryEnumerator ide = threadWindowForStreamSent.GetEnumerator();
            while (ide.MoveNext())
            {
                if (ide.Key.ToString().Equals(storedStreamId))
                {
                    Thread storedThread = (Thread)ide.Value;
                    threadWindowForStreamSent.Remove(storedStreamId);
                    storedThread.Abort();
                    break;
                }
            }
        }

        public void UpdateStreamBufferWindow(string storedStreamId)
        {
            try
            {
                WaitBufferWindow();
                if (bufferWindowForStreamSent.ContainsKey(storedStreamId))
                {
                    bufferWindowForStreamSent.Remove(storedStreamId);
                }
                SignalBufferWindow();
            }
            catch (Exception e)
            {
                MessageBox.Show("Semaphore WaitOne() Exception in UpdateStreamBufferWindow() :" + e.Message);
            }
        }

        public string FindStoredStreamInBufferWindow(string storedStreamId)
        {
            string storedStream = string.Empty;
            try
            {
                WaitBufferWindow();
                IDictionaryEnumerator ide = bufferWindowForStreamSent.GetEnumerator();
                while (ide.MoveNext())
                {
                    if (ide.Key.ToString().Equals(storedStreamId))
                    {
                        storedStream = ide.Value.ToString();
                        break;
                    }
                }
                SignalBufferWindow();
            }
            catch (Exception e)
            {
                MessageBox.Show("Semaphore WaitOne() in FindStoredStreamInBufferWindow() :" + e.Message);
            }
            return storedStream;
        }

        public void SaveInBufferWindow(string streamId,string sentStream)
        {
            try
            {
                WaitBufferWindow();
                bufferWindowForStreamSent.Add(streamId, sentStream);
                SignalBufferWindow();
            }
            catch (Exception e)
            {
                MessageBox.Show("Exception in SaveInReceivedRouteRequestBuffer() :" + e.Message);
            }
        }

        public bool IsPresentInBufferWindow(string storedStreamId)
        {
            bool flag = false;
            try
            {
                WaitBufferWindow();
                if (bufferWindowForStreamSent.ContainsKey(storedStreamId))
                    flag = true;
                SignalBufferWindow();
            }
            catch (Exception e)
            {
                MessageBox.Show("Exception in IsPresentInRouteRequestBuffer() :" + e.Message);
            }
            return flag;
        }

        public void WaitBufferWindow()
        {
            semaphoreObject.WaitOne();
        }

        public void SignalBufferWindow()
        {
            semaphoreObject.Release();
        }
    }
}
