namespace MockHttpDemo.Application.UseCases;

public interface ICommandHandler<in TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    public Task<TResult> HandleAsync(TCommand command);
}