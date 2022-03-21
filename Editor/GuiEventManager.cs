using System;

namespace Unordinal.Editor
{
    public class GuiEventManager: IGuiEventManager
    {
        public event Action OnBeforeCreated;

        public event Action OnBeforeUpdated;

        public event Action OnAfterUpdated;

        public event Action OnBeforeEnabled;

        public event Action OnAfterEnabled;

        public void BeforeCreated()
        {
            OnBeforeCreated?.Invoke();
        }

        public void AfterUpdated()
        {
            OnAfterUpdated?.Invoke();
        }

        public void BeforeUpdated()
        {
            OnBeforeUpdated?.Invoke();
        }

        public void BeforeEnabled()
        {
            OnBeforeEnabled?.Invoke();
        }

        public void AfterEnabled()
        {
            OnAfterEnabled?.Invoke();
        }
    }

    public interface IGuiEventManager : IGuiEventPublisher, IGuiEventSource
    { }

    public interface IGuiEventPublisher
    {
        void BeforeCreated();

        void AfterUpdated();

        void BeforeUpdated();

        void BeforeEnabled();

        void AfterEnabled();
    }

    public interface IGuiEventSource
    {
        event Action OnBeforeCreated;

        event Action OnBeforeUpdated;

        event Action OnAfterUpdated;

        event Action OnBeforeEnabled;

        event Action OnAfterEnabled;
    }
}
