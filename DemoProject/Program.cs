using NServiceBus;

namespace DemoProject
{
    // This class should trigger the analyzer warning  
    class MyClass(IMessageSession session)
    {
        private IMessageSession _session = session;
        
        public IMessageSession SessionProperty { get; set; } = session;
        
        public Task SomeAction()
        {
            // Local variable should also trigger warning
            IMessageSession localSession = _session;
            
            //use session here
            return Task.CompletedTask;
        }
        
        // Method parameter should trigger warning
        public Task AnotherAction(IMessageSession methodSession)
        {
            return Task.CompletedTask;
        }
    }

    // TODO: Uncomment this once NServiceBus.TransactionalSession package is properly configured
    // This class should NOT trigger warnings (using ITransactionalSession)
    // class GoodClass(ITransactionalSession session)
    // {
    //     private ITransactionalSession _session = session;
    //     
    //     public ITransactionalSession SessionProperty { get; set; } = session;
    //     
    //     public Task SomeAction()
    //     {
    //         ITransactionalSession localSession = _session;
    //         return Task.CompletedTask;
    //     }
    // }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("This is a demo project to test the TransactionalSessionAnalyzer.");
            Console.WriteLine("Check the compiler warnings/errors for analyzer suggestions.");
            Console.WriteLine("The analyzer will suggest replacing IMessageSession with ITransactionalSession.");
            Console.WriteLine("Note: ITransactionalSession requires the NServiceBus.TransactionalSession NuGet package.");
        }
    }
}
