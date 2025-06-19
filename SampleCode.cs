using NServiceBus;
namespace MyProject;

class MyClass(IMessageSession session)
{
    public Task SomeAction()
    {
        //use session here
        return Task.CompletedTask;
    }
}
