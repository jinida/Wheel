using Microsoft.AspNetCore.Components;
using System.ComponentModel.DataAnnotations;

namespace WheelApp.Components
{
    /// <summary>
    /// EditProjectModal component - for editing existing projects
    /// </summary>
    public partial class EditProjectModal : ComponentBase
    {
        [Parameter]
        public bool IsVisible { get; set; }

        [Parameter]
        public EventCallback<Model> OnSubmit { get; set; }

        [Parameter]
        public EventCallback OnCancel { get; set; }

        private Model _model = new();
        private string? _validationError = null;

        /// <summary>
        /// Initialize the modal with existing project data
        /// </summary>
        public void Initialize(int id, string name, string? description)
        {
            _model = new Model
            {
                Id = id,
                ProjectName = name,
                Description = description
            };
            _validationError = null;
            StateHasChanged();
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
            public int Id { get; set; }

            [Required(ErrorMessage = "Project name is required.")]
            public string? ProjectName { get; set; }

            public string? Description { get; set; }
        }
    }
}
