using NServiceBus;

namespace DemoProject.Examples
{
    /// <summary>
    /// Example showing how to add NServiceBus.TransactionalSession package and use ITransactionalSession
    /// 
    /// To use ITransactionalSession, you need to:
    /// 1. Install the NServiceBus.TransactionalSession NuGet package
    /// 2. Replace IMessageSession with ITransactionalSession
    /// 
    /// Add this to your .csproj file:
    /// <PackageReference Include="NServiceBus.TransactionalSession" Version="2.0.0" />
    /// </summary>
    public class TransactionalSessionExample
    {
        // Before: This would trigger analyzer warnings
        // private readonly IMessageSession messageSession;
        
        // After: This is the recommended approach
        // private readonly ITransactionalSession transactionalSession;
        
        // public TransactionalSessionExample(ITransactionalSession transactionalSession)
        // {
        //     this.transactionalSession = transactionalSession;
        // }
        
        // public async Task HandleMessage()
        // {
        //     using var transaction = await transactionalSession.Open();
        //     
        //     // Your business logic here
        //     
        //     // Send messages within the transaction
        //     await transaction.Send(new MyMessage());
        //     
        //     // Commit the transaction
        //     await transaction.Commit();
        // }
    }
    
    public class MyMessage
    {
        public string Content { get; set; } = string.Empty;
    }
}
