namespace LinuxTcpServerDotnetCore.TemplateClass
{
    public class Singleton<T> where T : new()
    {
        static protected T sInstance;
        static protected bool IsCreate = false;

        public static T Instance
        {
            get
            {
                if (IsCreate == false)
                {
                    CreateInstance();
                }

                return sInstance;
            }
        }

        public static void CreateInstance()
        {
            if (IsCreate == true)
                return;

            IsCreate = true;
            sInstance = new T();
        }

        public static void ReleaseInstance()
        {
            sInstance = default(T);
            IsCreate = false;
        }
    }
}
