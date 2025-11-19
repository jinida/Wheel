using MediatR;
using Microsoft.AspNetCore.Components;
using System.ComponentModel.DataAnnotations;
using WheelApp.Application.Common.Interfaces;
using WheelApp.Application.UseCases.ProjectTypes.Queries.GetProjectTypes;

namespace WheelApp.Components
{
    /// <summary>
    /// ProjectModal component - for creating new projects
    /// Pure presentation component - no business logic
    /// </summary>
    public partial class ProjectModal : ComponentBase
    {
        [Parameter]
        public bool IsVisible { get; set; }

        [Parameter]
        public EventCallback<Model> OnSubmit { get; set; }

        [Parameter]
        public EventCallback OnCancel { get; set; }

        [Inject]
        private IMediator Mediator { get; set; } = default!;

        private Model _model = new();
        private Dictionary<int, string> _projectTypes = new();
        private string? _validationError = null;

        protected override async Task OnInitializedAsync()
        {
            await LoadProjectTypes();
        }

        private async Task LoadProjectTypes()
        {
            // Load project types from Application layer via MediatR
            var query = new GetProjectTypesQuery();
            var result = await Mediator.Send(query);

            if (result.IsSuccess && result.Value != null)
            {
                _projectTypes = result.Value.ToDictionary(pt => pt.Value, pt => pt.Name);
            }
            else
            {
                // Fallback to empty dictionary
                _projectTypes = new Dictionary<int, string>();
                // Error already logged in Application layer
            }
        }

        private async Task HandleValidSubmit()
        {
            await OnSubmit.InvokeAsync(_model);
        }

        /// <summary>
        /// Set validation error - called from parent when duplicate name error occurs
        /// </summary>
        public void SetValidationError(string error)
        {
            _validationError = error;
            StateHasChanged();
        }

        public void ClearFiles()
        {
            _model = new();
            _validationError = null;
        }

        /// <summary>
        /// Clear validation error when user types
        /// </summary>
        private void OnNameInput(ChangeEventArgs e)
        {
            _validationError = null;
        }

        /// <summary>
        /// Get inline style for input - add red border when validation error exists
        /// </summary>
        private string GetInputStyle()
        {
            return string.IsNullOrEmpty(_validationError) ? "" : "border-color: #dc3545 !important;";
        }

        private async Task Cancel()
        {
            _model = new();
            _validationError = null;
            await OnCancel.InvokeAsync();
        }

        public class Model
        {
            [Required(ErrorMessage = "Project name is required.")]
            public string? ProjectName { get; set; }

            public string? Description { get; set; }

            [Required(ErrorMessage = "Project type is required.")]
            public int? ProjectType { get; set; }
        }
    }
}
