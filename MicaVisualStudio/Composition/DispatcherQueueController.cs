namespace MicaVisualStudio.Composition;

public class DispatcherQueueController
{
    #region PInvoke

    [DllImport("CoreMessaging.dll")]
    private static extern int CreateDispatcherQueueController(DispatcherQueueOptions options, [MarshalAs(UnmanagedType.IUnknown)] out object dispatcherQueueController);

    [StructLayout(LayoutKind.Sequential)]
    private struct DispatcherQueueOptions
    {
        public int dwSize;

        [MarshalAs(UnmanagedType.I4)]
        public DISPATCHERQUEUE_THREAD_TYPE threadType;

        [MarshalAs(UnmanagedType.I4)]
        public DISPATCHERQUEUE_THREAD_APARTMENTTYPE apartmentType;
    };

    private enum DISPATCHERQUEUE_THREAD_APARTMENTTYPE
    {
        DQTAT_COM_NONE = 0,
        DQTAT_COM_ASTA = 1,
        DQTAT_COM_STA = 2
    };

    private enum DISPATCHERQUEUE_THREAD_TYPE
    {
        DQTYPE_THREAD_DEDICATED = 1,
        DQTYPE_THREAD_CURRENT = 2,
    };

    #endregion

    readonly object controller;

    private DispatcherQueueController(object controller) => this.controller = controller;

    /// <summary>
    /// Tries to create a <see cref="DispatcherQueueController"/> for the current thread.
    /// </summary>
    /// <param name="controller">The <see cref="DispatcherQueueController"/> created.</param>
    /// <returns>Whether <paramref name="controller"/> was created successfully.</returns>
    public static bool TryCreate(out DispatcherQueueController controller)
    {
        DispatcherQueueOptions options = new()
        {
            apartmentType = DISPATCHERQUEUE_THREAD_APARTMENTTYPE.DQTAT_COM_STA,
            threadType = DISPATCHERQUEUE_THREAD_TYPE.DQTYPE_THREAD_CURRENT,
            dwSize = Marshal.SizeOf<DispatcherQueueOptions>()
        };

        int result = CreateDispatcherQueueController(options, out object dispatcher);
        controller = new(dispatcher);
        return result == 0;
    }
}
