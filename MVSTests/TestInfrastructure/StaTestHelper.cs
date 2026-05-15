using System;
using System.Threading;

namespace MVSTests.TestInfrastructure
{
    /// <summary>
    /// Runs an action on a dedicated STA thread and propagates any exception
    /// back to the calling test thread. WPF DependencyObjects (TextBlock,
    /// Camera, Visual3D, …) require an STA apartment, so any test that
    /// touches them must use this helper or set up its own STA thread.
    /// </summary>
    internal static class StaTestHelper
    {
        public static void Run(Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            Exception captured = null;
            var thread = new Thread(() =>
            {
                try { action(); }
                catch (Exception ex) { captured = ex; }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Start();
            thread.Join();

            if (captured != null) throw new InvalidOperationException(
                "STA test action threw: " + captured.Message, captured);
        }

        public static T Run<T>(Func<T> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            T result = default;
            Run(() => { result = func(); });
            return result;
        }
    }
}
