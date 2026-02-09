using System.ComponentModel;

namespace SmartLogReader
{
    public enum WorkerProgressCode
    {
        StartedWork,
        StillWorking,
        FinishedWork,
        CustomCode
    }

    public class Worker
    {
        public Worker()
        {
            worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.WorkerSupportsCancellation = true;
            worker.DoWork += WorkerDoWork;
            worker.ProgressChanged += WorkerProgressChanged;
            worker.RunWorkerCompleted += WorkerRunWorkerCompleted;
        }
        protected BackgroundWorker worker;

        public delegate void WorkerProgressChangedEventHandler(object sender, WorkerProgressCode code, string text);

        public event WorkerProgressChangedEventHandler ProgressChanged;

        protected virtual void ReportProgress(WorkerProgressCode code, string text = null)
        {
            if (worker.IsBusy)
            {
                try
                {
                    worker.ReportProgress((int)code, text);
                    return;
                }
                catch
                {
                }
            }

            ProgressChanged?.Invoke(this, code, text);
        }

        void WorkerProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ProgressChanged?.Invoke(this, (WorkerProgressCode)e.ProgressPercentage, e.UserState as string);
        }

        void WorkerRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            AfterWork(e);
            ProgressChanged?.Invoke(this, WorkerProgressCode.FinishedWork, e.Cancelled ? "Cancelled" : e.Error != null ? ("Error: " + e.Error.Message) : null);
        }

        void WorkerDoWork(object sender, DoWorkEventArgs e)
        {
            doWorkEventArgs = e;
            AtWork();
        }
        protected DoWorkEventArgs doWorkEventArgs;

        public virtual bool IsBusy
        {
            get { return worker.IsBusy; }
        }

        public virtual bool Start()
        {
            if (IsBusy)
                return false;

            worker.RunWorkerAsync();
            return true;
        }

        public virtual void Stop()
        {
            if (IsBusy)
                worker.CancelAsync();
        }

        protected virtual void AtWork()
        {
            ReportProgress(WorkerProgressCode.StartedWork);

            if (worker.CancellationPending)
            {
                doWorkEventArgs.Cancel = true;
                return;
            }

            ReportProgress(WorkerProgressCode.StillWorking);
        }

        protected virtual void AfterWork(RunWorkerCompletedEventArgs e)
        {
        }
    }
}
