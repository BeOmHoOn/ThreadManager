using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ThreadManager
{
    public class ThreadManager : IDisposable
    {
        private static ThreadManager _instance;

        /// <summary>
        /// 테스크의 정보를 담고있는 클래스입니다.
        /// </summary>
        public class TaskObject
        {
            public object Key;
            public ParameterizedThreadStart threadMethod;
            public bool IsBackground { get; private set; }

            public TaskObject(object Key, ParameterizedThreadStart threadMethod, bool IsBackground = false)
            {
                this.Key = Key;
                this.threadMethod = threadMethod;
                this.IsBackground = IsBackground;
            }
        }

        /// <summary>
        /// 테스크를 추가함에 대한 결과값을 담고 있는 클래스 입니다.
        /// </summary>
        public class AddingResult
        {
            public object Key;
            public bool IsSuccess;
            public string Reason { get; private set; }

            public AddingResult(object Key, bool IsSuccess, string Reason = "Success")
            {
                this.Key = Key;
                this.IsSuccess = IsSuccess;
                this.Reason = Reason;
            }
        }

        private Thread _thread;
        private Queue<TaskObject> _threadQueue;
        private TaskObject _standByTask;
        private bool _flag;
        private bool _stopFlag;
        private bool _keepProcessing;
        public bool IsBusy { get; private set; }
        public object Key { get; private set; }

        public ThreadManager()
        {
            _threadQueue = new Queue<TaskObject>();
        }

        /// <summary>
        /// 쓰레드 매니져의 인스턴스를 반환합니다.
        /// </summary>
        /// <returns></returns>
        public static ThreadManager Instance()
        {
            if (_instance == null)
            {
                _instance = new ThreadManager();
            }

            return _instance;
        }

        /// <summary>
        /// 쓰레드 매니져에 테스크를 추가합니다.
        /// </summary>
        /// <param name="Key">테스크의 고유 키입니다.</param>
        /// <param name="threadMethod">쓰레드가 작동할 메소드입니다.</param>
        /// <param name="IsBackground">백그라운드 작동 여부입니다.</param>
        /// <returns></returns>
        public AddingResult AddTask(object Key, ParameterizedThreadStart threadMethod, bool IsBackground = false)
        {
            if (_threadQueue != null)
            {
                if (_threadQueue.Select(t => t.Key).Contains(Key)) return new AddingResult(Key, false, "같은 값을 가진 키가 이미 존재합니다.");
                else
                    _threadQueue.Enqueue(new TaskObject(Key, threadMethod, IsBackground));

                return new AddingResult(Key, true);
            }

            return new AddingResult(Key, false, "객체가 초기화 되지 않았습니다.");
        }

        /// <summary>
        /// 테스크를 한번에 추가시킵니다.
        /// 제네릭 형식의 List<TaskObject>를 인자로 필요로 합니다.
        /// </summary>
        /// <param name="taskList">추가시킬 테스크의 목록입니다.</param>
        /// <returns></returns>
        public List<AddingResult> AddTaskAll(List<TaskObject> taskList)
        {
            List<AddingResult> result = new List<AddingResult>();

            foreach (var task in taskList)
            {
                result.Add(AddTask(task.Key, task.threadMethod, task.IsBackground));
            }

            return result;
        }

        /// <summary>
        /// 쓰레드 매니져에서 고유 키를 검색하여 해당 테스크를 삭제합니다.
        /// </summary>
        /// <param name="Key">삭제할 테스크의 키입니다.</param>
        public void DeleteTask(object Key)
        {
            var taskObjects = from item in _threadQueue
                              where !item.Key.Equals(Key)
                              select item;

            var clone = new Queue<TaskObject>();

            foreach (TaskObject item in taskObjects)
            {
                clone.Enqueue(item);
            }

            _threadQueue = clone;
        }

        /// <summary>
        /// 쓰레드 매니져에서 테스크 목록을 비웁니다.
        /// </summary>
        /// <param name="stopProcessing">현재 동작중인 쓰레드를 종료할 것에 대한 여부입니다.</param>
        public void ClearTask(bool stopProcessing)
        {
            if (stopProcessing)
            {
                _stopFlag = true;
            }

            if (_standByTask != null)
                _standByTask = null;

            _threadQueue.Clear();
        }

        /// <summary>
        /// 현재 동작중인 스레드를 멈춥니다.
        /// keepProcessing의 값을 true로 줄 경우 동작중이던 테스크를 저장합니다.
        /// 재 시작할 경우 저장한 테스크를 실행하되 처음부터 실행됩니다.
        /// </summary>
        /// <param name="keepProcessing"></param>
        public void Stop(bool keepProcessing)
        {
            if (!_stopFlag)
                _stopFlag = true;

            this._keepProcessing = keepProcessing;
        }

        /// <summary>
        /// 테스크 처리를 시작합니다.
        /// </summary>
        public void DoWork()
        {
            if (_stopFlag)
                _stopFlag = false;

            if (_threadQueue.Count > 0)
            {
                if (!_flag)
                    _flag = true;
            }
            else
                _flag = false;

            if (_flag)
            {
                Process();
            }
        }

        private void Process()
        {
            TaskObject t = null;

            if (_standByTask == null)
                t = _threadQueue.Dequeue();
            else
                t = _standByTask;

            _thread = new Thread(t.threadMethod);
            _thread.IsBackground = t.IsBackground;
            _thread.Start();

            IsBusy = true;
            Key = t.Key;

            while (_thread.IsAlive)
            {
                if (_stopFlag)
                {
                    if (_keepProcessing)
                        _standByTask = t;

                    IsBusy = false;
                    Key = null;
                    _flag = false;
                    _thread.Abort();

                    return;
                }
            }

            IsBusy = false;
            Key = null;
            if (_standByTask != null)
                _standByTask = null;

            _thread.Abort();

            if (_threadQueue.Count > 0)
                Process();
        }

        /// <summary>
        /// 쓰레드 매니져의 인스턴스를 null값으로 초기화합니다.
        /// </summary>
        public void Dispose()
        {
            _instance = null;
        }
    }
}