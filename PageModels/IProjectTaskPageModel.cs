using CommunityToolkit.Mvvm.Input;
using Loyercam.Models;

namespace Loyercam.PageModels
{
    public interface IProjectTaskPageModel
    {
        IAsyncRelayCommand<ProjectTask> NavigateToTaskCommand { get; }
        bool IsBusy { get; }
    }
}